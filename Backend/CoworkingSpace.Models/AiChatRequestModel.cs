namespace CoworkingSpace.Models
{
    public class AiChatRequestModel
    {
        // The user's message to the assistant.
        public string Message { get; set; } = string.Empty;

        // Client-generated GUID that identifies one chat conversation, so the assistant remembers earlier turns.
        public string? ConversationId { get; set; }
    }
}
