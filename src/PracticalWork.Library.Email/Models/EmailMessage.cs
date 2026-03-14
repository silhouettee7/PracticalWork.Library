namespace PracticalWork.Library.Email.Models;

public class EmailMessage
{
    public required string EmailTo { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public bool IsHtml { get; set; }
    public string BodyEncoding { get; set; } = "UTF-8";
    public string SubjectEncoding { get; set; } = "UTF-8";
}