using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace WebApp.API.Controllers.Helper
{
    public class CommonHelperFunctions
    {
        private static readonly Regex sWhitespace = new Regex(@"\s+");
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }

        public static string GeneratePassword(int length = 16)
        {
            if (length < 4)
                throw new ArgumentException("Password length must be at least 4.");

            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string special = "!@#$%^&*()-_=+";

            const string all = upper + lower + numbers + special;

            var chars = new List<char>
    {
        upper[RandomNumberGenerator.GetInt32(upper.Length)],
        lower[RandomNumberGenerator.GetInt32(lower.Length)],
        numbers[RandomNumberGenerator.GetInt32(numbers.Length)],
        special[RandomNumberGenerator.GetInt32(special.Length)]
    };

            for (int i = 4; i < length; i++)
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

            // Secure shuffle
            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }
    }
}
