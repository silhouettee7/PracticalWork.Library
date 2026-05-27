namespace PracticalWork.Library.Options;

public class EmailMessagesOptions
{
    public EmailInfo Notification { get; set; }
    public EmailInfo ReportForAdministration { get; set; }
}

public class EmailInfo
{
    public string TemplateFileName { get; set; }
    public string Subject { get; set; }
}