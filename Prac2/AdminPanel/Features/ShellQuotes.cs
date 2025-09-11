namespace AdminPanel.Features;

public static class ShellQuotes
{
    public static string BashQuote(string s)
    {
        string t = s.Replace("'", "'\"'\"'");
        return "'" + t + "'";
    }

    public static string PwshQuote(string s)
    {
        string t = s.Replace("'", "''");
        return "'" + t + "'";
    }
}