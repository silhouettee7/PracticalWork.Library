namespace PracticalWork.Library.MessageBroker.Configuration.Models;

public class ReportsConfig
{
    public string QueueName { get; set; } = default!;
    public string Exchange { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
}