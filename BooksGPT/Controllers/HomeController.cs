using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BooksGPT.Models;
using BooksGPT;
using BooksGPT.Handlers;
using BooksGPT.Constants;
using Microsoft.AspNetCore.Http;
using System.Linq;
using log4net;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;
using BooksGPT.Models;

namespace BooksGPT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly Utitlities _utils;
        private readonly AppDbContext _context;
        private readonly SessionHandler _sessionHandler;
        private readonly ValidationHandler _validationHandler;
        private readonly BookHandler _bookHandler;
        private readonly ChatHandler _chatHandler;
        private readonly GeminiHandler _geminiHandler;
        private readonly ChatHistoryHandler _chatHistoryHandler;
        private readonly ProfileHandler _profileHandler;
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public List<ChatHistoryModel> chatHistoryList = new();

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, AppDbContext context, Utitlities utils,
            SessionHandler sessionHandler, ValidationHandler validationHandler, BookHandler bookHandler, ChatHandler chatHandler, GeminiHandler geminiHandler,
            ChatHistoryHandler chatHistoryHandler, ProfileHandler profileHandler)
        {
            _logger = logger;
            _configuration = configuration;
            _utils = utils;
            _context = context;
            _sessionHandler = sessionHandler;
            _validationHandler = validationHandler;
            _bookHandler = bookHandler;
            _chatHandler = chatHandler;
            _geminiHandler = geminiHandler;
            _chatHistoryHandler = chatHistoryHandler;
            _profileHandler = profileHandler;
            try { log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("web.config")); } catch { }
        }

        public IActionResult Index()
        {
            string aiReply = AppConstants.DEFAULT_CHAT_MESSAGE;
            var email = Request.Cookies[AppConstants.COOKIE_EMAIL];
            
            // If user is logged in, ensure name and avatarColor cookies are set from DB
            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    var user = _context.Users.FirstOrDefault(u => u.Email == email);
                    if (user != null)
                    {
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict
                        };
                        
                        if (!string.IsNullOrEmpty(user.Name))
                            Response.Cookies.Append(AppConstants.COOKIE_NAME, user.Name, cookieOptions);
                        
                        if (!string.IsNullOrEmpty(user.AvatarColor))
                            Response.Cookies.Append(AppConstants.COOKIE_AVATAR_COLOR, user.AvatarColor, cookieOptions);
                    }
                }
                catch { }
            }

            // Check if there's an existing chat in session (page reload scenario)
            var existingChatId = _sessionHandler.GetSessionValue(AppConstants.SESSION_CURRENT_CHAT_ID);
            var existingUserQuestions = _sessionHandler.GetSessionValue(AppConstants.SESSION_USER_QUESTIONS);
            var existingBotAnswers = _sessionHandler.GetSessionValue(AppConstants.SESSION_BOT_ANSWERS);
            
            // Only reset session if there's no active chat (first visit or after explicit new chat)
            if (string.IsNullOrEmpty(existingChatId) || 
                string.IsNullOrEmpty(existingUserQuestions) || 
                existingUserQuestions == "[]")
            {
                // First visit or new chat - initialize fresh session
                _sessionHandler.ResetSession(aiReply);
                _sessionHandler.ResetCanSayNoCookie();
                
                ViewBag.AIReply = aiReply;
                ViewBag.UserQuestions = new List<string>();
                ViewBag.BotAnswers = new List<string> { aiReply };
                
                _chatHandler.DelRecords(email);
            }
            else
            {
                // Existing chat - preserve session and pass full conversation to view
                var userQuestions = JsonSerializer.Deserialize<List<string>>(existingUserQuestions ?? "[]");
                var botAnswers = JsonSerializer.Deserialize<List<string>>(existingBotAnswers ?? "[]");
                
                ViewBag.AIReply = botAnswers.Count > 0 ? botAnswers[botAnswers.Count - 1] : aiReply;
                ViewBag.UserQuestions = userQuestions;
                ViewBag.BotAnswers = botAnswers;
                ViewBag.IsReload = true; // Flag to indicate this is a reload with existing conversation
            }

            // Fetch chat history for the logged-in user
            if (!string.IsNullOrEmpty(email))
            {
                chatHistoryList = _context.ChatHistory
                    .Where(c => c.Email == email)
                    .OrderByDescending(c => c.Id)
                    .ToList();
            }
            ViewBag.ChatHistory = chatHistoryList;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Post(string userInput)
        {
            try
            {
                var email = Request.Cookies[AppConstants.COOKIE_EMAIL] ?? "";
                Request.Cookies.TryGetValue(AppConstants.COOKIE_CAN_SAY_NO, out var canSayNo);
                var isOldChat = _sessionHandler.GetSessionValue(AppConstants.SESSION_IS_OLD_CHAT);
                if (string.IsNullOrEmpty(isOldChat)) isOldChat = "no";

                var session = _sessionHandler.GetSessionState();
                int chatId;
                bool hasChatId = int.TryParse(session.CurrentChatId, out chatId);

                if (_validationHandler.IsRestartOrBookNotFound(session, userInput, canSayNo))
                {
                    if (_validationHandler.IsUserRestarting(userInput, canSayNo, hasChatId))
                        return Json(new { reply = _chatHandler.HandleUserRestart(email, chatId, session, userInput) });

                    return Json(new { reply = _bookHandler.HandleBookSearch(email, hasChatId, chatId, session, userInput) });
                }

                // Handle book confirmation state - only allow yes/no answers (for NEW chats only)
                if (session.IsBookSelected.ToLower() == "yes" && isOldChat.ToLower() != "yes")
                {
                    // User confirmed the book
                    if (_validationHandler.MatchesAny(_validationHandler.ListOfYes, userInput))
                    {
                        var reply = _bookHandler.HandleBookConfirmation(email, hasChatId, chatId, session, userInput);
                        var newChatId = _sessionHandler.GetSessionValue(AppConstants.SESSION_CURRENT_CHAT_ID);
                        var bookTitle = _sessionHandler.GetSessionValue(AppConstants.SESSION_BOOK_TITLE);
                        
                        // If new chat was created, return chat info for sidebar
                        if (!hasChatId && !string.IsNullOrEmpty(newChatId))
                        {
                            return Json(new { reply, newChat = new { id = newChatId, title = bookTitle } });
                        }
                        
                        return Json(new { reply });
                    }

                    // User rejected the book (says no) - trigger restart/search
                    if (_validationHandler.MatchesAny(_validationHandler.ListOfNo, userInput))
                        return Json(new { reply = _bookHandler.HandleBookSearch(email, hasChatId, chatId, session, userInput) });

                    // Any other input is not understood (NEW CHAT ONLY)
                    return Json(new { reply = _chatHandler.HandleUnconfirmedBook(email, hasChatId, chatId, session, userInput) });
                }

                // For old chats or confirmed books, handle book confirmation if yes is said
                if (session.IsBookSelected.ToLower() == "yes" && _validationHandler.MatchesAny(_validationHandler.ListOfYes, userInput))
                {
                    var reply = _bookHandler.HandleBookConfirmation(email, hasChatId, chatId, session, userInput);
                    var newChatId = _sessionHandler.GetSessionValue(AppConstants.SESSION_CURRENT_CHAT_ID);
                    var bookTitle = _sessionHandler.GetSessionValue(AppConstants.SESSION_BOOK_TITLE);
                    
                    // If new chat was created, return chat info for sidebar
                    if (!hasChatId && !string.IsNullOrEmpty(newChatId))
                    {
                        return Json(new { reply, newChat = new { id = newChatId, title = bookTitle } });
                    }
                    
                    return Json(new { reply });
                }

                return Json(new { reply = await _geminiHandler.HandleGeminiQuery(email, hasChatId, chatId, session, userInput) });
            }
            catch (Exception ex)
            {
                Log.Error("Error in Post method: " + ex.Message, ex);
                return Json(new { reply = "Error: " + ex.Message });
            }
        }

        // Start a new chat: clear session state and remove temporary records
        [HttpPost]
        public IActionResult NewChat()
        {
            var email = Request.Cookies[AppConstants.COOKIE_EMAIL];

            // Reset session and cookie
            _sessionHandler.ResetSession();
            _sessionHandler.ResetCanSayNoCookie();

            // Remove any temporary incomplete chat records for this user
            try
            {
                _chatHandler.DelRecords(email);
            }
            catch (Exception ex)
            {
                Log.Warn("Error while cleaning temporary records for NewChat: " + ex.Message);
            }

            // Return initial reply so client can update UI without full reload
            return Json(new { success = true, aiReply = AppConstants.DEFAULT_CHAT_MESSAGE });
        }

        // Fetch chat history by id for AJAX
        [HttpGet]
        public IActionResult GetChatHistoryById(int id)
        {
            var email = Request.Cookies[AppConstants.COOKIE_EMAIL];
            var (success, redirect, userQuestions, botAnswers) = _chatHistoryHandler.GetChatHistoryById(id, email);
            
            if (!success)
                return Json(new { success = false, redirect });

            return Json(new { success = true, userQuestions, botAnswers });
        }

        // Delete a chat by id for the logged-in user
        [HttpPost]
        public IActionResult DeleteChat(int id)
        {
            var email = Request.Cookies[AppConstants.COOKIE_EMAIL];
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, error = "Not logged in" });

            try
            {
                var success = _chatHistoryHandler.DeleteChat(id, email);
                if (!success)
                    return Json(new { success = false, error = "Chat not found" });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Log.Error("Error deleting chat: " + ex.Message, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Fetch logged-in user's profile
        [HttpGet]
        public IActionResult GetProfile()
        {
            var email = Request.Cookies[AppConstants.COOKIE_EMAIL];
            var (success, name, username, avatarColor) = _profileHandler.GetProfile(email);
            
            if (!success)
                return Json(new { success = false, error = "Not logged in" });

            return Json(new { success = true, name, username, avatarColor });
        }

        // Update logged-in user's profile
        [HttpPost]
        public IActionResult UpdateProfile([FromForm] string name, [FromForm] string username, [FromForm] string avatarColor)
        {
            var email = Request.Cookies[AppConstants.COOKIE_EMAIL];
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, error = "Not logged in" });

            try
            {
                var success = _profileHandler.UpdateProfile(email, name, username, avatarColor);
                if (!success)
                    return Json(new { success = false, error = "User not found" });

                // Update cookies so UI reflects changes immediately
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append(AppConstants.COOKIE_NAME, name ?? "", cookieOptions);
                Response.Cookies.Append(AppConstants.COOKIE_AVATAR_COLOR, avatarColor ?? "", cookieOptions);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Log.Error("Error updating profile: " + ex.Message, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
