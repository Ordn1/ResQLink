using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResQLink.Data;

namespace ResQLink.Services.Communications;

/// <summary>
/// Communication service for email and SMS (mock implementation - integrate with real providers)
/// </summary>
public class CommunicationService : ICommunicationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<CommunicationService> _logger;
    private readonly AuditService _auditService;

    public CommunicationService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<CommunicationService> logger,
        AuditService auditService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<CommunicationResult> SendEmailAsync(EmailMessage message)
    {
        try
        {
            // TODO: Integrate with actual email provider (SendGrid, AWS SES, etc.)
            _logger.LogInformation($"Sending email to {string.Join(", ", message.To)} - Subject: {message.Subject}");

            // Mock implementation
            await Task.Delay(100); // Simulate sending

            await _auditService.LogAsync(
                action: "EMAIL_SENT",
                entityType: "Communication",
                description: $"Email sent to {message.To.Count} recipient(s): {message.Subject}",
                severity: "Info",
                isSuccessful: true
            );

            return new CommunicationResult
            {
                Success = true,
                SuccessCount = message.To.Count,
                FailureCount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");
            
            await _auditService.LogAsync(
                action: "EMAIL_FAILED",
                entityType: "Communication",
                description: $"Failed to send email: {message.Subject}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );

            return new CommunicationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                FailureCount = message.To.Count
            };
        }
    }

    public async Task<CommunicationResult> SendSmsAsync(SmsMessage message)
    {
        try
        {
            // TODO: Integrate with SMS provider (Twilio, Nexmo, etc.)
            _logger.LogInformation($"Sending SMS to {message.PhoneNumbers.Count} number(s)");

            // Mock implementation
            await Task.Delay(100); // Simulate sending

            await _auditService.LogAsync(
                action: "SMS_SENT",
                entityType: "Communication",
                description: $"SMS sent to {message.PhoneNumbers.Count} recipient(s)",
                severity: "Info",
                isSuccessful: true
            );

            return new CommunicationResult
            {
                Success = true,
                SuccessCount = message.PhoneNumbers.Count,
                FailureCount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS");
            
            await _auditService.LogAsync(
                action: "SMS_FAILED",
                entityType: "Communication",
                description: "Failed to send SMS",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );

            return new CommunicationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                FailureCount = message.PhoneNumbers.Count
            };
        }
    }

    public async Task<CommunicationResult> SendBulkEmailAsync(List<string> recipients, string subject, string body)
    {
        var message = new EmailMessage
        {
            To = recipients,
            Subject = subject,
            Body = body,
            IsHtml = true
        };

        return await SendEmailAsync(message);
    }

    public async Task<CommunicationResult> SendBulkSmsAsync(List<string> phoneNumbers, string message)
    {
        var smsMessage = new SmsMessage
        {
            PhoneNumbers = phoneNumbers,
            Message = message
        };

        return await SendSmsAsync(smsMessage);
    }

    public async Task<CommunicationResult> SendEmailFromTemplateAsync(
        string templateName, 
        Dictionary<string, string> parameters, 
        List<string> recipients)
    {
        var template = await GetEmailTemplateAsync(templateName);
        
        var body = template;
        foreach (var param in parameters)
        {
            body = body.Replace($"{{{{{param.Key}}}}}", param.Value);
        }

        var message = new EmailMessage
        {
            To = recipients,
            Subject = ExtractSubjectFromTemplate(template),
            Body = body,
            IsHtml = true
        };

        return await SendEmailAsync(message);
    }

    public async Task<CommunicationResult> SendDisasterAlertAsync(int disasterId, List<string> recipients)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var disaster = await context.Disasters.FindAsync(disasterId);
        if (disaster == null)
        {
            return new CommunicationResult
            {
                Success = false,
                ErrorMessage = "Disaster not found"
            };
        }

        var subject = $"DISASTER ALERT: {disaster.Title}";
        var body = $@"
            <h2>Disaster Alert Notification</h2>
            <p><strong>Type:</strong> {disaster.DisasterType}</p>
            <p><strong>Severity:</strong> {disaster.Severity}</p>
            <p><strong>Location:</strong> {disaster.Location}</p>
            <p><strong>Start Date:</strong> {disaster.StartDate:MMM dd, yyyy}</p>
            <p><strong>Status:</strong> {disaster.Status}</p>
            <p>Please take necessary precautions and follow official instructions.</p>
            <p><em>ResQLink Disaster Management System</em></p>
        ";

        return await SendBulkEmailAsync(recipients, subject, body);
    }

    public async Task<CommunicationResult> SendEvacuationNoticeAsync(int shelterId, List<string> phoneNumbers)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var shelter = await context.Shelters
            .Include(s => s.Disaster)
            .FirstOrDefaultAsync(s => s.ShelterId == shelterId);
        
        if (shelter == null)
        {
            return new CommunicationResult
            {
                Success = false,
                ErrorMessage = "Shelter not found"
            };
        }

        var message = $"EVACUATION NOTICE: Please proceed to {shelter.Name} at {shelter.Location}. " +
                     $"Current capacity: {shelter.CurrentOccupancy}/{shelter.Capacity}. " +
                     $"For assistance, contact local disaster response team.";

        return await SendBulkSmsAsync(phoneNumbers, message);
    }

    public async Task<CommunicationResult> SendVolunteerAssignmentAsync(int volunteerId, string email)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var volunteer = await context.Volunteers
            .Include(v => v.AssignedShelter)
            .Include(v => v.AssignedDisaster)
            .FirstOrDefaultAsync(v => v.VolunteerId == volunteerId);
        
        if (volunteer == null)
        {
            return new CommunicationResult
            {
                Success = false,
                ErrorMessage = "Volunteer not found"
            };
        }

        var subject = "Volunteer Assignment Confirmation";
        var body = $@"
            <h2>Dear {volunteer.FirstName} {volunteer.LastName},</h2>
            <p>You have been assigned to assist with disaster response operations.</p>
            <p><strong>Disaster:</strong> {volunteer.AssignedDisaster?.Title ?? "Not assigned"}</p>
            <p><strong>Shelter:</strong> {volunteer.AssignedShelter?.Name ?? "Not assigned"}</p>
            <p><strong>Status:</strong> {volunteer.Status}</p>
            <p>Thank you for your service!</p>
            <p><em>ResQLink Disaster Management Team</em></p>
        ";

        return await SendBulkEmailAsync(new List<string> { email }, subject, body);
    }

    private Task<string> GetEmailTemplateAsync(string templateName)
    {
        // TODO: Load from database or file system
        var templates = new Dictionary<string, string>
        {
            ["disaster-alert"] = "<h2>Disaster Alert</h2><p>{{message}}</p>",
            ["evacuation-notice"] = "<h2>Evacuation Notice</h2><p>{{message}}</p>",
            ["volunteer-assignment"] = "<h2>Volunteer Assignment</h2><p>{{message}}</p>"
        };

        return Task.FromResult(templates.GetValueOrDefault(templateName, "<p>{{message}}</p>"));
    }

    private string ExtractSubjectFromTemplate(string template)
    {
        // Simple extraction - could be enhanced
        var titleMatch = System.Text.RegularExpressions.Regex.Match(template, @"<h2>(.*?)</h2>");
        return titleMatch.Success ? titleMatch.Groups[1].Value : "ResQLink Notification";
    }
}
