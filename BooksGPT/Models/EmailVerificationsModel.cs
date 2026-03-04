namespace BooksGPT.Models
{
    public class EmailVerificationsModel
    {
        public int Id { get; set; }  // Auto-increment primary key
        public string Email { get; set; }  // Email address to verify
        public string VerificationCode { get; set; }  // Unique verification code
        public DateTime CreatedAt { get; set; }  // Timestamp when the verification was created
        public DateTime? Expiresat { get; set; }  // Timestamp when the email was verified (nullable)
        public bool IsExpired { get; set; } = false;  // Flag to indicate if the email is verified
    }
}
