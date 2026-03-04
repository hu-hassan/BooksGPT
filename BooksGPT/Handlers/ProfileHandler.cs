using System;
using System.Linq;
using BooksGPT.Models;

namespace BooksGPT.Handlers
{
    public class ProfileHandler
    {
        private readonly AppDbContext _context;

        public ProfileHandler(AppDbContext context)
        {
            _context = context;
        }

        public (bool success, string name, string username, string avatarColor) GetProfile(string email)
        {
            if (string.IsNullOrEmpty(email))
                return (false, "", "", "");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return (false, "", "", "");

            return (true, user.Name ?? "", user.Username ?? "", user.AvatarColor ?? "");
        }

        public bool UpdateProfile(string email, string name, string username, string avatarColor)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return false;

            try
            {
                // Update fields
                if (!string.IsNullOrWhiteSpace(name)) user.Name = name;
                if (!string.IsNullOrWhiteSpace(username)) user.Username = username;
                if (!string.IsNullOrWhiteSpace(avatarColor)) user.AvatarColor = avatarColor;

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
