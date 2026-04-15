using System.Collections;

namespace PracticalWork.Library.Models;

public class EmailMessage
{
    public string RecipientName { get; set; }
    public string EmailTo { get; }
    public string Subject { get; }
    public string Body { get; private set; }
    public bool IsBodyHtml { get; set; }
    public string BodyEncoding { get; set; } = "UTF-8";
    public string SubjectEncoding { get; set; } = "UTF-8";
    
    public EmailMessage(string emailTo, string subject, string body, bool bodyIsHtml)
    {
        EmailTo = emailTo;
        Subject = subject;
        Body = body;
        IsBodyHtml = bodyIsHtml;
    }
    
    public void Personalize<T>(T personalizationObject) where T : class
    {
        if (!IsBodyHtml)
        {
            return;
        }
        foreach (var property in personalizationObject.GetType().GetProperties())
        {
            if (property.GetValue(personalizationObject) is IEnumerable<string> personalizationCollection)
            {
                var replaceValue = string.Join(", ", personalizationCollection);
                Body = Body.Replace("{{" + property.Name + "}}", replaceValue);
            }
            else if (property.PropertyType == typeof(DateOnly))
            {
                var replaceValue = $"{(DateOnly)property.GetValue(personalizationObject)!:dd-MM-yyyy}";
                Body = Body.Replace("{{" + property.Name + "}}", replaceValue);
            }
            else
            {
                Body = Body
                    .Replace("{{" + property.Name + "}}", property
                        .GetValue(personalizationObject)?
                        .ToString());
            }
            
        }
        
    }
}