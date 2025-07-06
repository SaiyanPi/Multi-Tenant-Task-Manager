namespace MultiTenantTaskManager.Helpers;
public static class StringExtensions
{
    public static string Truncate(this string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text!.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
    }
}