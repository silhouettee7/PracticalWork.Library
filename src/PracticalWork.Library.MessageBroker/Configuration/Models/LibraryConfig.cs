namespace PracticalWork.Library.MessageBroker.Configuration.Models;

public class LibraryConfig
{
    public string ExchangeName { get; set; } = default!;
    
    public QueueBindingConfig BookCreate { get; set; } = default!;
    public QueueBindingConfig BookArchive { get; set; } = default!;
    public QueueBindingConfig BookBorrow { get; set; } = default!;
    public QueueBindingConfig BookReturn { get; set; } = default!;
    public QueueBindingConfig ReaderCreate { get; set; } = default!;
    public QueueBindingConfig ReaderClose { get; set; } = default!;
}