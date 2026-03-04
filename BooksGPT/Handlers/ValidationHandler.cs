using System;
using System.Collections.Generic;
using System.Linq;

namespace BooksGPT.Handlers
{
    public class ValidationHandler
    {
        public List<string> ListOfNo { get; } = new List<string>() { "no", "naah", "not" };
        public List<string> ListOfYes { get; } = new List<string>() { "yes", "yeah", "this is the one", "one" };

        public bool MatchesAny(IEnumerable<string> list, string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            var normalized = input.Trim().ToLowerInvariant();
            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var e = item.Trim().ToLowerInvariant();
                if (normalized == e) return true;
                if (e.Contains(' '))
                {
                    if (normalized.Contains(e)) return true;
                }
                else
                {
                    var tokens = System.Text.RegularExpressions.Regex.Split(normalized, "\\W+");
                    if (tokens.Contains(e)) return true;
                }
            }
            return false;
        }

        public bool IsRestartOrBookNotFound(
            (string AiReply, string IsBookSelected, List<string> UserQuestions, List<string> BotAnswers, string BookTitle, string CurrentChatId, string author) session,
            string userInput,
            string canSayNo)
        {
            var reply1 = session.AiReply;
            if (canSayNo == "no") return false;
            return reply1 == "Book not found." || reply1 == "" || MatchesAny(ListOfNo, userInput);
        }

        public bool IsUserRestarting(string userInput, string canSayNo, bool hasChatId)
        {
            return (MatchesAny(ListOfNo, userInput) &&
                    !string.IsNullOrWhiteSpace(canSayNo) &&
                    canSayNo.ToLower() == "yes" &&
                    hasChatId);
        }

        public bool IsBookConfirmed(string userInput, string isBookSelected)
        {
            return MatchesAny(ListOfYes, userInput) &&
                   (isBookSelected.ToLower() == "yes" || isBookSelected.ToLower() == "");
        }
    }
}
