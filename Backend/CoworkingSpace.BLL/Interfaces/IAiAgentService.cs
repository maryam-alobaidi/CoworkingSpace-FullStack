namespace CoworkingSpace.BLL.Interfaces
{
    public interface IAiAgentService
    {
        /// <summary>
        /// Sends a message within a conversation and returns the assistant's reply.
        /// History is kept per <paramref name="conversationKey"/> for a limited time.
        /// </summary>
        Task<string> GetResponseAsync(string conversationKey, string userInput);
    }
}
