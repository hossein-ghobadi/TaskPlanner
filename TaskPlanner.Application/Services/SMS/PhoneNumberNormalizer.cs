namespace TaskPlanner.Application.Services.SMS
{
    public static class PhoneNumberNormalizer
    {
        public static string Normalize(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("98") && digits.Length == 12)
            {
                digits = "0" + digits[2..];
            }
            else if (digits.StartsWith("0098") && digits.Length == 14)
            {
                digits = "0" + digits[4..];
            }

            return digits;
        }

        public static bool IsValidIranianMobile(string? phone)
        {
            var normalized = Normalize(phone);
            return normalized.Length == 11 && normalized.StartsWith("09");
        }
    }
}
