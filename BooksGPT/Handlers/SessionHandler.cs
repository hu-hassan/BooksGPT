using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using BooksGPT.Constants;

namespace BooksGPT.Handlers
{
    public class SessionHandler
    {
        private readonly IHttpContextAccessor _accessor;

        public SessionHandler(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public (string AiReply, string IsBookSelected, List<string> UserQuestions, List<string> BotAnswers, string BookTitle, string CurrentChatId, string author) GetSessionState()
        {
            var http = _accessor.HttpContext;
            return (
                AiReply: http.Session.GetString(AppConstants.SESSION_AI_REPLY) ?? "",
                IsBookSelected: http.Session.GetString(AppConstants.SESSION_IS_BOOK_SELECTED) ?? "",
                UserQuestions: JsonSerializer.Deserialize<List<string>>(http.Session.GetString(AppConstants.SESSION_USER_QUESTIONS) ?? "[]"),
                BotAnswers: JsonSerializer.Deserialize<List<string>>(http.Session.GetString(AppConstants.SESSION_BOT_ANSWERS) ?? "[]"),
                BookTitle: http.Session.GetString(AppConstants.SESSION_BOOK_TITLE) ?? "",
                CurrentChatId: http.Session.GetString(AppConstants.SESSION_CURRENT_CHAT_ID) ?? "",
                author: http.Session.GetString(AppConstants.SESSION_BOOK_AUTHOR) ?? ""
            );
        }

        /// <summary>
        /// Resets the session to initial state for a new chat
        /// </summary>
        /// <param name="initialBotMessage">Optional custom initial message, defaults to DEFAULT_CHAT_MESSAGE</param>
        public void ResetSession(string initialBotMessage = null)
        {
            var message = initialBotMessage ?? AppConstants.DEFAULT_CHAT_MESSAGE;
            var http = _accessor.HttpContext;

            http.Session.SetString(AppConstants.SESSION_AI_REPLY, "");
            http.Session.SetString(AppConstants.SESSION_USER_QUESTIONS, JsonSerializer.Serialize(new List<string>()));
            http.Session.SetString(AppConstants.SESSION_BOT_ANSWERS, JsonSerializer.Serialize(new List<string> { message }));
            http.Session.SetString(AppConstants.SESSION_BOOK_TITLE, "");
            http.Session.SetString(AppConstants.SESSION_IS_BOOK_SELECTED, "");
            http.Session.SetString(AppConstants.SESSION_CURRENT_CHAT_ID, "");
            http.Session.SetString(AppConstants.SESSION_IS_OLD_CHAT, "no");
            http.Session.SetString(AppConstants.SESSION_BOOK_AUTHOR, "");
        }

        /// <summary>
        /// Resets the canSayNo cookie to empty
        /// </summary>
        public void ResetCanSayNoCookie()
        {
            var http = _accessor.HttpContext;
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            http.Response.Cookies.Append(AppConstants.COOKIE_CAN_SAY_NO, "", cookieOptions);
        }

        /// <summary>
        /// Sets a specific session value
        /// </summary>
        public void SetSessionValue(string key, string value)
        {
            _accessor.HttpContext.Session.SetString(key, value);
        }

        /// <summary>
        /// Gets a specific session value
        /// </summary>
        public string GetSessionValue(string key)
        {
            return _accessor.HttpContext.Session.GetString(key) ?? "";
        }
    }
}
