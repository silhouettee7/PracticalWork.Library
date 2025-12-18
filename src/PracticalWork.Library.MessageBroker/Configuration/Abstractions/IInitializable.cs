namespace PracticalWork.Library.MessageBroker.Configuration.Abstractions;

public interface IInitializable
{
    Task InitializeAsync();
    bool IsInit { get; set; }
}