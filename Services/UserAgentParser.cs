namespace itpayroll.Services
{
    public static class UserAgentParser
    {
        public static string GetBrowser(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return "Unknown";

            if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
                return "Microsoft Edge";
            if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Chromium", StringComparison.OrdinalIgnoreCase))
                return "Chrome";
            if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
                return "Firefox";
            if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
                return "Safari";

            return "Unknown";
        }

        public static string GetOperatingSystem(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return "Unknown";

            if (userAgent.Contains("Windows NT 10.0", StringComparison.OrdinalIgnoreCase))
                return "Windows 10/11";
            if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                return "Windows";
            if (userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
                return "macOS";
            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
                return "Android";
            if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
                return "iOS";
            if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
                return "Linux";

            return "Unknown";
        }
    }
}
