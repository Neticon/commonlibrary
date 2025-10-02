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
    }
}
