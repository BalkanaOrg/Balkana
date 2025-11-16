namespace Balkana.Data.Helpers
{
    public static class HtmlHelpers
    {
        public static string StripHtml(this string input, int maxLength = 150)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Remove tags
            var withoutTags = System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);

            // Decode entities (&nbsp;, &amp;, etc.)
            withoutTags = System.Net.WebUtility.HtmlDecode(withoutTags);

            // Trim length
            if (withoutTags.Length > maxLength)
            {
                return withoutTags.Substring(0, maxLength) + "...";
            }

            return withoutTags;
        }
    }
}
