namespace WebApp.EntryPoint.Settings
{
    public class RabbitMQSettings
    {
        public string? User { get; init; }
        public string? Password { get; init; }
        public string? Host { get; init; }
        public string? VirtualHost { get; init; }
        public string ConnectionString => $"amqp://{User}:{Password}@{HostSettings.GetHost(Host??string.Empty)}/{VirtualHost}";
        
    }
}