#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.BLL.Plugins;
using CoworkingSpace.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Data;

using static System.Net.Mime.MediaTypeNames;

namespace CoworkingSpace.BLL.Services
{
    public class AiAgentService : IAiAgentService
    {

        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ChatHistory _chatHistory;
        private readonly string _companyInfo;

        public  AiAgentService(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            var modelId = "gpt-4o-mini";

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured.");
            }

            var builder = Kernel.CreateBuilder();

            builder.AddOpenAIChatCompletion(modelId, apiKey);

            builder.AddOpenAITextEmbeddingGeneration("text - embedding - 3 - small", apiKey);


            _kernel = builder.Build();

            


            //add plugins
            _kernel.Plugins.AddFromType<EventBookingPlugin>("EventTools");
            _kernel.Plugins.AddFromType<CoworkingBookingPlugin>("BookingTools");
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            _companyInfo = LoadCompanyInfo();

            string systemPrompt = $@"You are a helpful and polite AI assistant for Vantage Coworking Space in Barcelona. 
                    Always use the following official company information and guidelines to answer user questions accurately:
                    
                    {_companyInfo}";

            _chatHistory = new ChatHistory(systemPrompt);
        }


        private string LoadCompanyInfo()
        {
            try
            {
                string filePath = "company-info.txt";
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

        public async Task<string> GetResponseAsync(string userInput)
        {
            _chatHistory.AddUserMessage(userInput);

            var executionSettings = new OpenAIPromptExecutionSettings()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var result = await _chatCompletionService.GetChatMessageContentAsync(_chatHistory, executionSettings, _kernel);

            _chatHistory.AddAssistantMessage(result.Content);

            return result.Content;
        }



    }
}
