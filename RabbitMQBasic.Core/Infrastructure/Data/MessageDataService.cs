using Npgsql;
using MessageBrokerRabbitMQ.Core.Model;


namespace RabbitMQBasic.Core.Infrastructure.Data;

public class MessageDataService
{
    public MessageDataService()
    {

    }
    public Message CreateMessage(NpgsqlTransaction transaction, NpgsqlConnection sqlConnection)
    {
        Message message = new()
        {
            MessageId = new Random().Next(1, 1000),
            Stored = DateTimeOffset.UtcNow
        };

        //new Message(sqlConnection, transaction);

        return message;
    }

    public void MarkAsProcessed(Message message, NpgsqlConnection sqlConnection, NpgsqlTransaction sqlTransaction)
    {
        message.Processed = DateTimeOffset.UtcNow;
        //message.MarkAsProcessed(sqlConnection, sqlTransaction);
    }

}