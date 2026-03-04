using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using BooksGPT.Models;

namespace BooksGPT.Handlers
{
    public class ChatHistoryHandler
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _accessor;

        public ChatHistoryHandler(AppDbContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _accessor = accessor;
        }

        public (bool success, bool redirect, List<string> userQuestions, List<string> botAnswers) GetChatHistoryById(int id, string email)
        {
            var chat = _context.ChatHistory.FirstOrDefault(c => c.Id == id && c.Email == email);
            if (chat == null)
                return (false, false, new List<string>(), new List<string>());

            // If book is not selected, treat as incomplete chat - start new chat instead
            if (!chat.IsBookSelected)
                return (false, true, new List<string>(), new List<string>());

            var http = _accessor.HttpContext;
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_IS_OLD_CHAT, "yes");

            // Update session values
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_IS_BOOK_SELECTED, chat.IsBookSelected ? "True" : "");
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOOK_TITLE, chat.Title ?? "");
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOOK_AUTHOR, chat.Author ?? "");
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_CURRENT_CHAT_ID, chat.Id.ToString());
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_USER_QUESTIONS, JsonSerializer.Serialize(chat.UserQuestions ?? new List<string>()));
            http.Session.SetString(BooksGPT.Constants.AppConstants.SESSION_BOT_ANSWERS, JsonSerializer.Serialize(chat.BotAnswers ?? new List<string>()));

            // Update cookies with chat data
            var cookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict };
            http.Response.Cookies.Append(BooksGPT.Constants.AppConstants.COOKIE_BOOK_TITLE, chat.Title ?? "", cookieOptions);
            http.Response.Cookies.Append(BooksGPT.Constants.AppConstants.COOKIE_BOOK_AUTHOR, chat.Author ?? "", cookieOptions);
            http.Response.Cookies.Append(BooksGPT.Constants.AppConstants.COOKIE_IS_BOOK_SELECTED, chat.IsBookSelected ? true.ToString() : false.ToString(), cookieOptions);
            http.Response.Cookies.Append(BooksGPT.Constants.AppConstants.COOKIE_CAN_SAY_NO, "no", cookieOptions);

            var userQuestions = chat.UserQuestions ?? new List<string>();
            var botAnswers = chat.BotAnswers ?? new List<string>();

            return (true, false, userQuestions, botAnswers);
        }

        public bool DeleteChat(int id, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var chat = _context.ChatHistory.FirstOrDefault(c => c.Id == id && c.Email == email);
            if (chat == null)
                return false;

            try
            {
                _context.ChatHistory.Remove(chat);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
