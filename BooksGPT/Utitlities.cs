using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Configuration;
using Python.Runtime;

namespace BooksGPT
{
    public class Utitlities
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<string> ReturnedTitles = new List<string>();
        private string gtitle, gauthors, gdescription;

        private readonly string _geminiKey;
        private readonly string _googleBooksKey;

        public Utitlities(IConfiguration configuration)
        {
            try { log4net.Config.XmlConfigurator.Configure(new FileInfo("web.config")); } catch { }

            // Load keys from configuration, fall back to environment variables if not present.
            _geminiKey = configuration["ApiKeys:Gemini"] ?? Environment.GetEnvironmentVariable("APIKEY_GEMINI") ?? string.Empty;
            _googleBooksKey = configuration["ApiKeys:GoogleBooks"] ?? Environment.GetEnvironmentVariable("APIKEY_GOOGLEBOOKS") ?? string.Empty;
        }

        public async Task<string> GeminiCaller(string userInput, List<string> userquestions, List<string> botanswers, string author, string title)
        {
            // read API key from configuration (injected) -- default empty to avoid leaking secrets
            string apiKey = _geminiKey;
            string model = "gemini-2.5-flash";
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            if ((userquestions != null && userquestions.Count > 0) || (botanswers != null && botanswers.Count > 0))
            {
                gtitle = title;
                gauthors = author;
            }

            var historyParts = new List<object>
            {
                new { text = $"In the context of the book {gtitle} by {gauthors}. Use the following chat history and answer the latest question. Also if chat history is available look for what book user said ok to."}
            };

            int userCount = userquestions?.Count ?? 0;
            int botCount = botanswers?.Count ?? 0;
            int count = Math.Max(userCount, botCount);
            var msglmt = 5;


            // Add up to `msglmt` last messages in chronological order
            if (userquestions != null && userquestions.Count > 0)
            {
                int start = Math.Max(0, userquestions.Count - msglmt);
                for (int i = start; i < userquestions.Count; i++)
                {
                    var u = userquestions[i] ?? "";
                    var b = (i < botanswers.Count) ? (botanswers[i] ?? "") : "";
                    historyParts.Add(new { text = $"User: {u}" });
                    if (!string.IsNullOrEmpty(b)) historyParts.Add(new { text = $"Bot: {b}" });
                }
            }

            // finally add the current user input
            historyParts.Add(new { text = $"User: {userInput}" });

            var requestData = new
            {
                contents = new[]
                {
                    new {
                        parts = historyParts
                    }
                }
            };

            using var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var contentElement) &&
                contentElement.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textElement))
            {
                return textElement.GetString();
            }
            else
            {
                Log.Error(responseString);
                return "Sorry, I couldn't generate a response. Please try again with a different input.";
            }
        }

        //public (string Title, string Authors, string Description) GetBookDescription(string title)
        //{
        //    using var httpClient = new HttpClient();
        //    int maxResults = 20;
        //    int startIndex = 0;
        //    int maxTries = 4; // 20, 30, 40, 50 (Google Books API max is 40 per request, but you can paginate)
        //    int tries = 0;

        //    while (tries < maxTries)
        //    {
        //        var url = $"https://www.googleapis.com/books/v1/volumes?q={Uri.EscapeDataString(title)}&maxResults={maxResults}&startIndex={startIndex}";
        //        var response = httpClient.GetAsync(url).Result;
        //        if (!response.IsSuccessStatusCode)
        //            return (null, null, null);

        //        var json = response.Content.ReadAsStringAsync().Result;
        //        using var doc = JsonDocument.Parse(json);

        //        if (doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        //        {
        //            foreach (var item in items.EnumerateArray())
        //            {
        //                var volumeInfo = item.GetProperty("volumeInfo");
        //                string bookTitle = volumeInfo.TryGetProperty("title", out var t) ? t.GetString() : "";

        //                // Use title for duplicate check
        //                if (ReturnedTitles.Contains(bookTitle))
        //                    continue;

        //                string authors = volumeInfo.TryGetProperty("authors", out var a) && a.ValueKind == JsonValueKind.Array
        //                    ? string.Join(", ", a.EnumerateArray().Select(x => x.GetString()))
        //                    : "Unknown author";
        //                string description = volumeInfo.TryGetProperty("description", out var d)
        //                    ? d.GetString()
        //                    : "No description available.";

        //                ReturnedTitles.Add(bookTitle);

        //                return (bookTitle, authors, description);
        //            }
        //        }

        //        // If not found, increase startIndex and maxResults for next batch
        //        startIndex += maxResults;
        //        maxResults = 10; // After the first batch, fetch 10 at a time
        //        tries++;
        //    }

        //    // If no matching book is found after all tries, return nulls
        //    return (null, null, null);
        //}

        public (string Title, string Authors, string Description) GetBookDescription(string title)
        {
            Log.Info($"Searching for book: {title}");
            using var httpClient = new HttpClient();
            var url = $"https://www.googleapis.com/books/v1/volumes?q={Uri.EscapeDataString(title)}&maxResults=10";
            if (!string.IsNullOrEmpty(_googleBooksKey)) url += $"&key={_googleBooksKey}";

            var response = httpClient.GetAsync(url).Result;
            Log.Info($"Google Books API response status: {response}");
            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Google Books API request failed with status code: {response.StatusCode}");
                return (null, null, null);
            }

            var json = response.Content.ReadAsStringAsync().Result;
            using var doc = JsonDocument.Parse(json);


            if (doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
            {
                foreach (var item in items.EnumerateArray())
                {
                    var volumeInfo = item.GetProperty("volumeInfo");
                    string bookTitle = volumeInfo.TryGetProperty("title", out var t) ? t.GetString() : "";

                    if (ReturnedTitles.Contains(bookTitle))
                        continue;

                    string authors = volumeInfo.TryGetProperty("authors", out var a) && a.ValueKind == JsonValueKind.Array
                        ? string.Join(", ", a.EnumerateArray().Select(x => x.GetString()))
                        : "Unknown author";
                    string description = volumeInfo.TryGetProperty("description", out var d)
                        ? d.GetString()
                        : "No description available.";

                    ReturnedTitles.Add(bookTitle);
                    return (bookTitle, authors, description);
                }
            }
            return (null, null, null);
        }

        public string GetBook(string bookTitle)
        {
            var bookDescriptionTask = GetBookDescription(bookTitle);
            var (title, authors, description) = bookDescriptionTask;
            gtitle = title;
            gauthors = authors;
            gdescription = description;
            if (string.IsNullOrEmpty(title))
            {
                return "Book not found or excluded.";
            }

            return $"Is your book {title}? Written by: {authors}";
        }
    }
}
