using System.CodeDom;

namespace Goose
{
    public static class Extensions
    {
        public static string Underscore(this string str)
        {
            return str.Replace(" ", "_");
        }

        public static string DoNotPrint(this string? str, string? check, string before, string after)
        {
            return str == check ? string.Empty : (before + str + after);
        }
    }
}