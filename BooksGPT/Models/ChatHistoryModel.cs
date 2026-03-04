namespace BooksGPT.Models
{
    public class ChatHistoryModel
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Title { get; set; }
        public List<string> UserQuestions { get; set; } = new();
        public List<string> BotAnswers { get; set; } = new();
        public string? Author { get; set; }
        public bool IsBookSelected { get; set; }

        // Parameterless constructor for EF Core seeding
        public ChatHistoryModel() { }
    }
}
