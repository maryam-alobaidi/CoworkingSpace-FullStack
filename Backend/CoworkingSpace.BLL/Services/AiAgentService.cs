using System.Collections.Concurrent;
using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.BLL.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CoworkingSpace.BLL.Services
{
    /// <summary>
    /// Registered as a singleton: the kernel is built once, and each conversation keeps its own
    /// ChatHistory in memory (trimmed to the most recent turns, evicted after inactivity).
    /// </summary>
    public class AiAgentService : IAiAgentService
    {
        private const string ModelId = "gpt-4o-mini";
        private const int MaxHistoryMessages = 20;            // user + assistant messages kept per conversation
        private const int MaxConversations = 1000;
        private static readonly TimeSpan ConversationTtl = TimeSpan.FromMinutes(30);

        private readonly Lazy<(Kernel Kernel, IChatCompletionService Chat)> _kernel;
        private readonly string _systemPrompt;
        private readonly ConcurrentDictionary<string, Conversation> _conversations = new();

        private sealed class Conversation
        {
            public required ChatHistory History { get; init; }
            public SemaphoreSlim Lock { get; } = new(1, 1);
            public DateTime LastUsedUtc { get; set; } = DateTime.UtcNow;
        }

        public AiAgentService(IConfiguration configuration)
        {
            // Built lazily so a missing API key only breaks the chat endpoint, not app startup or DI.
            _kernel = new Lazy<(Kernel, IChatCompletionService)>(() =>
            {
                var apiKey = configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    throw new InvalidOperationException("OpenAI API key is not configured.");

                var builder = Kernel.CreateBuilder();
                builder.AddOpenAIChatCompletion(ModelId, apiKey);
                var kernel = builder.Build();

                kernel.Plugins.AddFromType<EventBookingPlugin>("EventTools");
                kernel.Plugins.AddFromType<CoworkingBookingPlugin>("BookingTools");

                return (kernel, kernel.GetRequiredService<IChatCompletionService>());
            });

            _systemPrompt = $@"You are a helpful and polite AI assistant for Vantage Coworking Space in Barcelona.
                    Always use the following official company information and guidelines to answer user questions accurately:

                    {LoadCompanyInfo()}";
        }

        private static string LoadCompanyInfo()
        {
            try
            {
                // Resolve next to the app binaries: under IIS the working directory is not the app folder.
                string filePath = Path.Combine(AppContext.BaseDirectory, "company-info.txt");
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading company info: {ex.Message}");
            }
            return "Vantage Coworking Space in Barcelona, Spain.";
        }

        public async Task<string> GetResponseAsync(string conversationKey, string userInput)
        {
            var (kernel, chat) = _kernel.Value;

            EvictStaleConversations();
            var conversation = _conversations.GetOrAdd(conversationKey,
                _ => new Conversation { History = new ChatHistory(_systemPrompt) });

            // One request at a time per conversation so turns don't interleave.
            await conversation.Lock.WaitAsync();
            try
            {
                conversation.LastUsedUtc = DateTime.UtcNow;
                conversation.History.AddUserMessage(userInput);

                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                var result = await chat.GetChatMessageContentAsync(conversation.History, executionSettings, kernel);
                string reply = result.Content ?? string.Empty;

                conversation.History.AddAssistantMessage(reply);
                TrimHistory(conversation.History);

                return reply;
            }
            finally
            {
                conversation.Lock.Release();
            }
        }

        // Keep the system prompt (index 0) plus only the most recent plain user/assistant messages.
        private static void TrimHistory(ChatHistory history)
        {
            // Drop tool-call plumbing; the final assistant text already contains what was learned.
            for (int i = history.Count - 1; i > 0; i--)
            {
                var m = history[i];
                bool isToolPlumbing = m.Role == AuthorRole.Tool
                    || m.Items.OfType<FunctionCallContent>().Any()
                    || m.Items.OfType<FunctionResultContent>().Any()
                    || (m.Role == AuthorRole.Assistant && string.IsNullOrEmpty(m.Content));
                if (isToolPlumbing)
                    history.RemoveAt(i);
            }

            while (history.Count - 1 > MaxHistoryMessages)
            {
                history.RemoveAt(1);
            }
        }

        private void EvictStaleConversations()
        {
            var cutoff = DateTime.UtcNow - ConversationTtl;
            foreach (var pair in _conversations)
            {
                if (pair.Value.LastUsedUtc < cutoff)
                    _conversations.TryRemove(pair.Key, out _);
            }

            // Hard cap so memory can't grow without bound: drop the least recently used.
            int excess = _conversations.Count - MaxConversations;
            if (excess > 0)
            {
                foreach (var key in _conversations.OrderBy(p => p.Value.LastUsedUtc).Take(excess).Select(p => p.Key))
                    _conversations.TryRemove(key, out _);
            }
        }
    }
}
