using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BooksGPT.Models;

namespace BooksGPT.Handlers
{
    public class GeminiHandler
    {
        private readonly AppDbContext _context;
        private readonly Utitlities _utils;
        private readonly IHttpContextAccessor _accessor;

        public GeminiHandler(AppDbContext context, Utitlities utils, IHttpContextAccessor accessor)
        {
            _context = context;
            _utils = utils;
            _accessor = accessor;
        }

        public async Task<string> HandleGeminiQuery(string email, bool hasChatId, int chatId, 
            (string AiReply, string IsBookSelected, List<string> UserQuestions, List<string> BotAnswers, string BookTitle, string CurrentChatId, string author) session, 
            string userInput)
        {
            var http = _accessor.HttpContext;
            http.Session.SetString("IsBookSelected", true.ToString());
            http.Response.Cookies.Append("canSayNo", "no", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            var userQuestions = new List<string>(session.UserQuestions ?? new List<string>());
            var botAnswers = new List<string>(session.BotAnswers ?? new List<string>());
            string title = session.BookTitle ?? "";
            string author = session.author ?? "";

            string aiReply = await _utils.GeminiCaller(userInput, userQuestions, botAnswers, author, title);

            userQuestions.Add(userInput);
            botAnswers.Add(aiReply);

            http.Session.SetString("userQuestions", JsonSerializer.Serialize(userQuestions));
            http.Session.SetString("botAnswers", JsonSerializer.Serialize(botAnswers));

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

            return aiReply;
        }
    }
}
