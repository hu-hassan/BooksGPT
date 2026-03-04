namespace BooksGPT.Constants
{
    public static class AppConstants
    {
        // Session Keys
        public const string SESSION_AI_REPLY = "aiReply";
        public const string SESSION_USER_QUESTIONS = "userQuestions";
        public const string SESSION_BOT_ANSWERS = "botAnswers";
        public const string SESSION_BOOK_TITLE = "bookTitle";
        public const string SESSION_IS_BOOK_SELECTED = "IsBookSelected";
        public const string SESSION_CURRENT_CHAT_ID = "CurrentChatId";
        public const string SESSION_IS_OLD_CHAT = "IsOldChat";
        public const string SESSION_CAN_SAY = "canSay";
        public const string SESSION_BOOK_AUTHOR = "bookAuthor";

        // Cookie Names
        public const string COOKIE_IS_LOGIN = "isLogin";
        public const string COOKIE_EMAIL = "email";
        public const string COOKIE_NAME = "name";
        public const string COOKIE_AVATAR_COLOR = "avatarColor";
        public const string COOKIE_CAN_SAY_NO = "canSayNo";
        public const string COOKIE_BOOK_TITLE = "bookTitle";
        public const string COOKIE_BOOK_AUTHOR = "bookAuthor";
        public const string COOKIE_IS_BOOK_SELECTED = "IsBookSelected";

        // Default Messages
        public const string DEFAULT_CHAT_MESSAGE = "Enter the book title to search";
        public const string BOOK_CONFIRMATION_MESSAGE = "Great! Ask me any question about it.";
        public const string BOOK_NOT_UNDERSTOOD_MESSAGE = "Sorry I can't understand. Please answer with yes or no.";
        public const string RESTART_MESSAGE = "Enter the book title to search again";
        public const string DEFAULT_USERNAME = "Guest";
        public const string DEFAULT_AVATAR_COLOR = "#6b7280";

        // Book Search Patterns
        public const string BOOK_TITLE_PATTERN = "Is your book ";
        public const string BOOK_AUTHOR_PATTERN = "Written by:";
        public const string BOOK_NOT_FOUND_MESSAGE = "Book not found.";
    }
}
