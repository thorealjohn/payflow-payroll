namespace itpayroll.Utilities
{
    public static class DisplayHelper
    {
        public static string MaskAccountNumber(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Length <= 4)
                return new string('*', value.Length);

            return new string('*', value.Length - 4) + value[^4..];
        }
    }
}
