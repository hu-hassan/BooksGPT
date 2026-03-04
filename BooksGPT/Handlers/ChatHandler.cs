using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using BooksGPT.Models;

namespace BooksGPT.Handlers
{
    public class ChatHandler
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _accessor;

        public ChatHandler(AppDbContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _accessor = accessor;
        }

        public string HandleUserRestart(string email, int chatId, 
            (string AiReply, string IsBookSelected, List<string> UserQuestions, List<string> BotAnswers, string BookTitle, string CurrentChatId, string author) session, 
            string userInput)
        {
            var http = _accessor.HttpContext;
            string aiReply2 = BooksGPT.Constants.AppConstants.DEFAULT_CHAT_MESSAGE;
            var userQuestions = new List<string>(session.UserQuestions ?? new List<string>());
            userQuestions.Add(userInput);
            string botReply = aiReply2 + " again";
            var botAnswers = new List<string>(session.BotAnswers ?? new List<string>());
            botAnswers.Add(botReply);

            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_USER_QUESTIONS, JsonSerializer.Serialize(userQuestions));
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOT_ANSWERS, JsonSerializer.Serialize(botAnswers));
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_AI_REPLY, "");
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_IS_BOOK_SELECTED, "");
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOOK_TITLE, "");

            http.Response.Cookies.Append(BooksGPT.Constants.AppConstants.COOKIE_CAN_SAY_NO, "yes", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            if (!string.IsNullOrEmpty(email))
            {
                var chat = _context.ChatHistory.FirstOrDefault(c => c.Id == chatId && c.Email == email);
                if (chat != null)
                {
                    chat.Title = "";
                    chat.UserQuestions = userQuestions;
                    chat.BotAnswers = botAnswers;
                    chat.IsBookSelected = false;
                    _context.SaveChanges();
                }
            }

            return botReply;
        }

        public string HandleUnconfirmedBook(string email, bool hasChatId, int chatId, 
            (string AiReply, string IsBookSelected, List<string> UserQuestions, List<string> BotAnswers, string BookTitle, string CurrentChatId, string author) session, 
            string userInput)
        {
            var http = _accessor.HttpContext;
            var userQuestions = new List<string>(session.UserQuestions ?? new List<string>());
            userQuestions.Add(userInput);
            string botReply = BooksGPT.Constants.AppConstants.BOOK_NOT_UNDERSTOOD_MESSAGE;
            var botAnswers = new List<string>(session.BotAnswers ?? new List<string>());
            botAnswers.Add(botReply);

            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_USER_QUESTIONS, JsonSerializer.Serialize(userQuestions));
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOT_ANSWERS, JsonSerializer.Serialize(botAnswers));

            if (!string.IsNullOrEmpty(email) && hasChatId)
            {
                var chat = _context.ChatHistory.FirstOrDefault(c => c.Id == chatId && c.Email == email);
                if (chat != null)
                {
                    chat.UserQuestions = userQuestions;
                    chat.BotAnswers = botAnswers;
                    _context.SaveChanges();
                }
            }

            return botReply;
        }

        public void DelRecords(string username)
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                var toDelete = _context.ChatHistory
                    .Where(c => c.Email == username && (c.Title == "" || c.IsBookSelected == false))
                    .ToList();
                if (toDelete.Any())
                {
                    _context.ChatHistory.RemoveRange(toDelete);
                    _context.SaveChanges();
                }
            }
        }
    }
}
