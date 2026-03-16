namespace PracticalWork.Library.Options;

/// <summary>
/// Настройки SMTP сервера для отправки email уведомлений в системе библиотеки
/// </summary>
public class EmailOptions
{
    /// <summary> Адрес SMTP сервера для отправки email </summary>
    public string SmtpServer { get; set; } = "localhost";
    /// <summary>
    /// Порт SMTP сервера для подключения
    /// </summary>
    /// <remarks>
    /// Для Papercut SMTP используйте порт 25
    /// </remarks>
    public int SmtpPort { get; set; } = 25;
    /// <summary>
    /// Определяет, используется ли SSL/TLS шифрование для подключения к SMTP серверу
    /// </summary>
    /// <remarks>
    /// Для локальной разработки с Papercut SMTP обычно устанавливается false
    /// </remarks>
    public bool UseSsl { get; set; } = false;
    /// <summary> Отображаемое имя отправителя в email сообщениях </summary>
    public string SenderName { get; set; } = "Библиотека";
    /// <summary> Email адрес отправителя для всех исходящих сообщений </summary>
    public string SenderEmail { get; set; } = "noreply@library.local";
    /// <summary>
    /// Пароль для аутентификации отправителя
    /// </summary>
    public required string SenderPassword { get; set; }
    /// <summary>
    /// Список email адресов администраторов библиотеки для
    /// получения системных уведомлений
    /// </summary>
    public List<string> AdminEmails { get; set; } = new();
    public static readonly string SectionName = "EmailSettings";
}