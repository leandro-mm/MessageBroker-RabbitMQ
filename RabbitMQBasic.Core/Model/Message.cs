namespace MessageBrokerRabbitMQ.Core.Model;

public class Message
{
    public int MessageId { get; set; }

    /// <summary>
    /// Data de Criação da Mensagem
    /// </summary>
    public DateTimeOffset Stored { get; set; }

    /// <summary>
    /// Data de Processamento da Mensagem
    /// </summary>
    public DateTimeOffset? Processed { get; set; }

    
    public TimeSpan? TimeSpent() => this.Processed?.Subtract(this.Stored) ?? null;
}
