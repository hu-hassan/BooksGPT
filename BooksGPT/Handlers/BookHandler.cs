using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using BooksGPT.Models;

namespace BooksGPT.Handlers
{
    public class BookHandler
    {
        private readonly AppDbContext _context;
        private readonly Utitlities _utils;
        private readonly IHttpContextAccessor _accessor;

        public BookHandler(AppDbContext context, Utitlities utils, IHttpContextAccessor accessor)
        {
            _context = context;
            _utils = utils;
            _accessor = accessor;
        }

        public string HandleBookSearch(string email, bool hasChatId, int chatId, 
            (string AiReply, string IsBookSelected, List<string> UserQuestions, List<string> BotAnswers, string BookTitle, string CurrentChatId, string author) session, 
            string userInput)
        {
            // If the user said "no" (rejecting a suggested book), re-search using the
            // original book title (the first user question) so the Google Books API
            // returns the next matching result instead of searching for the word "no".
            string searchTitle = userInput;
            if (session.UserQuestions != null && session.UserQuestions.Count > 0)
            {
                string firstQuestion = session.UserQuestions[0];
                var noList = new List<string> { "no", "naah", "not" };
                var normalized = userInput.Trim().ToLowerInvariant();
                if (noList.Any(n => n == normalized))
                {
                    searchTitle = firstQuestion;
                }
            }

            string aiReply = _utils.GetBook(searchTitle);
            var http = _accessor.HttpContext;

            http.Response.Cookies.Append("canSayNo", "yes", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_CAN_SAY, aiReply);
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_IS_BOOK_SELECTED, "yes");
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_AI_REPLY, aiReply);

            string extractedTitle = ExtractBookTitle(aiReply);
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOOK_TITLE, extractedTitle);
            string extractedAuthor = ExtractBookAuthor(aiReply);
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOOK_AUTHOR, extractedAuthor);

            var userQuestions = new List<string>(session.UserQuestions ?? new List<string>());
            userQuestions.Add(userInput);
            var botAnswers = new List<string>(session.BotAnswers ?? new List<string>());
            botAnswers.Add(aiReply);

            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_USER_QUESTIONS, JsonSerializer.Serialize(userQuestions));
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOT_ANSWERS, JsonSerializer.Serialize(botAnswers));

            return aiReply;
        }

        public string HandleBookConfirmation(string email, bool hasChatId, int chatId, 
            (string AiReply, string IsBookSelected, List<string> UserQuestions, List<string> BotAnswers, string BookTitle, string CurrentChatId, string author) session, 
            string userInput)
        {
            var http = _accessor.HttpContext;
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_IS_BOOK_SELECTED, "");

            var userQuestions = new List<string>(session.UserQuestions ?? new List<string>());
            userQuestions.Add(userInput);
            string botReply = BooksGPT.Constants.AppConstants.BOOK_CONFIRMATION_MESSAGE;
            var botAnswers = new List<string>(session.BotAnswers ?? new List<string>());
            botAnswers.Add(botReply);

            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_USER_QUESTIONS, JsonSerializer.Serialize(userQuestions));
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOT_ANSWERS, JsonSerializer.Serialize(botAnswers));

            http.Response.Cookies.Append(BooksGPT.Constants.AppConstants.COOKIE_CAN_SAY_NO, "no", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            http.Session.SetString("IsBookSelected", true.ToString());

            // Now save to database since book is confirmed
            if (!string.IsNullOrEmpty(email))
            {
                if (hasChatId)
                    {
                        // Update existing chat
                        var chat = _context.ChatHistory.FirstOrDefault(c => c.Id == chatId && c.Email == email);
                        if (chat != null)
                        {
                            chat.Title = session.BookTitle;
                            chat.Author = session.author;
                            chat.UserQuestions = userQuestions;
                            chat.BotAnswers = botAnswers;
                            chat.IsBookSelected = true;
                        _context.SaveChanges();
                    }
                }
                else
                    {
                        // Create new chat record (book is now confirmed)
                        var chatHistory = new ChatHistoryModel
                        {
                            Email = email,
                            Title = session.BookTitle,
                            Author = session.author,
                            UserQuestions = userQuestions,
                            BotAnswers = botAnswers,
                            IsBookSelected = true
                        };
                        _context.ChatHistory.Add(chatHistory);
                        _context.SaveChanges();
                        http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_CURRENT_CHAT_ID, chatHistory.Id.ToString());
                    }
            }

            return botReply;
        }

        public string ExtractBookTitle(string aiReply)
        {
            if (string.IsNullOrEmpty(aiReply)) return "";
            var start = aiReply.IndexOf("Is your book ", StringComparison.OrdinalIgnoreCase);
            if (start == -1) return "";
            start += "Is your book ".Length;
            var end = aiReply.IndexOf('?', start);
            if (end == -1) return "";
            return aiReply.Substring(start, end - start).Trim();
        }

        public string ExtractBookAuthor(string aiReply)
        {
            if (string.IsNullOrEmpty(aiReply)) return "";
            var marker = "Written by:";
            var start = aiReply.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start == -1) return "";
            start += marker.Length;
            return aiReply.Substring(start).Trim();
        }
    }
}
