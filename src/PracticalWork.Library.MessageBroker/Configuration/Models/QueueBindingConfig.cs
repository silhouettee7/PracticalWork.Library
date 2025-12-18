namespace PracticalWork.Library.MessageBroker.Configuration.Models;

public class QueueBindingConfig
{
    public string QueueName { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
}