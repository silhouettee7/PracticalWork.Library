namespace Domain.Options;

public class RabbitOptions
{
    public string Host { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public int Port { get; set; }
    public int MaxPoolSize { get; set; }
    public string AppName { get; set; }
    public LibraryRabbitConfig Library { get; set; }
    public ReportsRabbitConfig Reports { get; set; }
}

public class LibraryRabbitConfig
{
    public string ExchangeName { get; set; }
    public QueueBindingConfig BookCreate { get; set; }
    public QueueBindingConfig BookArchive { get; set; }
    public QueueBindingConfig BookBorrow { get; set; }
    public QueueBindingConfig BookReturn { get; set; }
    public QueueBindingConfig ReaderCreate { get; set; }
    public QueueBindingConfig ReaderClose { get; set; }
}

public class QueueBindingConfig
{
    public string QueueName { get; set; }
    public string RoutingKey { get; set; }
}

public class ReportsRabbitConfig
{
    public string QueueName { get; set; }
    public string Exchange { get; set; }
    public string RoutingKey { get; set; }
}