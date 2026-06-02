using System.Text.Json.Serialization;

namespace TaskPlanner.Application.Services.Bale
{
    public class BaleApiResponse<T>
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public T? Result { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("error_code")]
        public int? ErrorCode { get; set; }
    }

    public class BaleClientResult<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public string? ErrorDescription { get; init; }
        public int? ErrorCode { get; init; }

        public static BaleClientResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static BaleClientResult<T> Fail(string? description, int? code = null) =>
            new() { Success = false, ErrorDescription = description, ErrorCode = code };
    }

    public class BaleUpdate
    {
        [JsonPropertyName("update_id")]
        public int UpdateId { get; set; }

        [JsonPropertyName("message")]
        public BaleMessage? Message { get; set; }

        [JsonPropertyName("edited_message")]
        public BaleMessage? EditedMessage { get; set; }

        [JsonPropertyName("channel_post")]
        public BaleMessage? ChannelPost { get; set; }

        [JsonPropertyName("edited_channel_post")]
        public BaleMessage? EditedChannelPost { get; set; }
    }

    public class BaleMessage
    {
        [JsonPropertyName("message_id")]
        public long MessageId { get; set; }

        [JsonPropertyName("from")]
        public BaleUser? From { get; set; }

        [JsonPropertyName("sender_chat")]
        public BaleChat? SenderChat { get; set; }

        [JsonPropertyName("chat")]
        public BaleChat? Chat { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("photo")]
        public List<BalePhotoSize>? Photo { get; set; }

        [JsonPropertyName("document")]
        public BaleDocument? Document { get; set; }

        [JsonPropertyName("voice")]
        public BaleVoice? Voice { get; set; }

        [JsonPropertyName("audio")]
        public BaleAudio? Audio { get; set; }

        [JsonPropertyName("video")]
        public BaleVideo? Video { get; set; }

        [JsonPropertyName("sticker")]
        public BaleSticker? Sticker { get; set; }

        [JsonPropertyName("contact")]
        public BaleContact? Contact { get; set; }

        /// <summary>امضای نویسنده در کانال (نام ادمین یا عنوان ادمین ناشناس).</summary>
        [JsonPropertyName("author_signature")]
        public string? AuthorSignature { get; set; }
    }

    public class BaleContact
    {
        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }
    }

    public class BaleChatMember
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("user")]
        public BaleUser? User { get; set; }
    }

    public class BaleUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("is_bot")]
        public bool IsBot { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    public class BaleChat
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
    }

    public class BaleFile
    {
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        [JsonPropertyName("file_unique_id")]
        public string? FileUniqueId { get; set; }

        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; }

        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }
    }

    public class BalePhotoSize
    {
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        [JsonPropertyName("file_unique_id")]
        public string? FileUniqueId { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("file_size")]
        public int? FileSize { get; set; }
    }

    public class BaleDocument
    {
        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        [JsonPropertyName("mime_type")]
        public string? MimeType { get; set; }
    }

    public class BaleUserProfilePhotos
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("photos")]
        public List<List<BalePhotoSize>>? Photos { get; set; }
    }

    public class BaleVoice
    {
        [JsonPropertyName("duration")]
        public int Duration { get; set; }
    }

    public class BaleAudio
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }
    }

    public class BaleVideo
    {
        [JsonPropertyName("duration")]
        public int Duration { get; set; }
    }

    public class BaleSticker
    {
        [JsonPropertyName("emoji")]
        public string? Emoji { get; set; }
    }

    public class BaleBotInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
    }
}
