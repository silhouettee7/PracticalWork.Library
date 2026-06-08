
namespace PracticalWork.Library.Models;

/// <summary>
/// Письмо
/// </summary>
public class EmailMessage
{
    /// <summary>
    /// Получатель
    /// </summary>
    public string RecipientName { get; set; }
    /// <summary>
    /// Почта получателя
    /// </summary>
    public string EmailTo { get; }
    /// <summary>
    /// Тема
    /// </summary>
    public string Subject { get; }
    /// <summary>
    /// Тело письма
    /// </summary>
    public string Body { get; private set; }
    /// <summary>
    /// Тело письма в html?
    /// </summary>
    public bool IsBodyHtml { get; set; }
    /// <summary>
    /// кодировка тела письма
    /// </summary>
    public string BodyEncoding { get; set; } = "UTF-8";
    /// <summary>
    /// кодировка темы письма
    /// </summary>
    public string SubjectEncoding { get; set; } = "UTF-8";
    
    public EmailMessage(string emailTo, string subject, string body, bool bodyIsHtml)
    {
        EmailTo = emailTo;
        Subject = subject;
        Body = body;
        IsBodyHtml = bodyIsHtml;
    }
    
    /// <summary>
    /// Нааполнить шаблон письма данными
    /// </summary>
    /// <param name="personalizationObject">объект персонализации</param>
    /// <typeparam name="T">тип объекта</typeparam>
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