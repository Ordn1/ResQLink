namespace ResQLink.Services.Communications;

/// <summary>
/// Email message model
/// </summary>
public class EmailMessage
{
    public List<string> To { get; set; } = new();
    public List<string>? Cc { get; set; }
    public List<string>? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<EmailAttachment>? Attachments { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// SMS message model
/// </summary>
public class SmsMessage
{
    public List<string> PhoneNumbers { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public DateTime? ScheduledTime { get; set; }
    public string? SenderName { get; set; }
}

/// <summary>
/// Communication result
/// </summary>
public class CommunicationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailedRecipients { get; set; } = new();
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Interface for communication service
/// </summary>
public interface ICommunicationService
{
    Task<CommunicationResult> SendEmailAsync(EmailMessage message);
    Task<CommunicationResult> SendSmsAsync(SmsMessage message);
    Task<CommunicationResult> SendBulkEmailAsync(List<string> recipients, string subject, string body);
    Task<CommunicationResult> SendBulkSmsAsync(List<string> phoneNumbers, string message);
    
    // Template-based sending
    Task<CommunicationResult> SendEmailFromTemplateAsync(string templateName, Dictionary<string, string> parameters, List<string> recipients);
    Task<CommunicationResult> SendDisasterAlertAsync(int disasterId, List<string> recipients);
    Task<CommunicationResult> SendEvacuationNoticeAsync(int shelterId, List<string> phoneNumbers);
    Task<CommunicationResult> SendVolunteerAssignmentAsync(int volunteerId, string email);
}
