using System.Text;
using System.Security.Cryptography;

namespace BooksGPT.Views.Auth
{
    public class PasswordHelper
    {

        public static string CreateSalt(int size)
        {
            // Generate a cryptographic random number using the cryptographic service provider
            var rng = new RNGCryptoServiceProvider();
            var buff = new byte[size];
            rng.GetBytes(buff);

            // Return a Base64 string representation of the random number
            return Convert.ToBase64String(buff);
        }


        public static string GetHashPassword(string input)
        {

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }

        }

        public static (string mixed, string pattern) MixPasswordAndSalt(string password, string salt)
        {
            var passwordChars = password.ToCharArray().ToList();
            var saltChars = salt.ToCharArray().ToList();
            var rnd = new Random();

            var mixed = new StringBuilder();
            var pattern = new StringBuilder(); // P = password, S = salt

            while (passwordChars.Count > 0 || saltChars.Count > 0)
            {
                bool takeFromPassword;

                if (passwordChars.Count == 0)
                    takeFromPassword = false;
                else if (saltChars.Count == 0)
                    takeFromPassword = true;
                else
                    takeFromPassword = rnd.Next(2) == 0;

                if (takeFromPassword)
                {
                    mixed.Append(passwordChars[0]);
                    pattern.Append('P');
                    passwordChars.RemoveAt(0);
                }
                else
                {
                    mixed.Append(saltChars[0]);
                    pattern.Append('S');
                    saltChars.RemoveAt(0);
                }
            }

            return (mixed.ToString(), pattern.ToString());
        }

        public static string RecreateMixedString(string inputPassword, string salt, string pattern)
        {
            int pIndex = 0, sIndex = 0;
            var result = new StringBuilder();

            foreach (char c in pattern)
            {
                if (c == 'P')
                    result.Append(inputPassword[pIndex++]);
                else
                    result.Append(salt[sIndex++]);
            }

            return result.ToString();
        }

    }
}
