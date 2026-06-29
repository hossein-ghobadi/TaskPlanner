namespace TaskPlanner.Application.Services.SMS
{
    public class SmsApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RequestSmsSendDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class RequestSmsCheckDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
