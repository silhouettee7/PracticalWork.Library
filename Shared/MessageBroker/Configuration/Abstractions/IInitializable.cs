namespace MessageBroker.Configuration.Abstractions;

public interface IInitializable
{
    Task InitializeAsync();
    bool IsInitialized { get; set; }
}