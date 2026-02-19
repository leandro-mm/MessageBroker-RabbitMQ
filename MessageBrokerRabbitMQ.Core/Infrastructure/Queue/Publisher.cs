using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessageBrokerRabbitMQ.Core.Infrastructure.Data;
using MessageBrokerRabbitMQ.Core.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQWalkthrough.Core.Model;

namespace MessageBrokerRabbitMQ.Core.Infrastructure.Queue;

public class Publisher
{
    private readonly IChannel channel;
    private readonly IConnection rabbitMqConnection;
    private readonly NpgsqlConnection sqlConnection;
    private readonly MessageDataService messageDataService;

    private readonly ILogger<Publisher> logger;

     private string exchange;

      private readonly Thread runThread;

      private volatile bool isRunning;

       public int MessagesPerSecond { get; private set; }

        public TimeSpan TimeToWait { get; private set; }

        private bool isInitialized;

         public string Id { get; }

         public Publisher(IChannel channel, IConnection rabbitMqConnection, NpgsqlConnection sqlConnection, MessageDataService messageDataService, ILogger<Publisher> logger)
        {
            this.channel = channel;
            this.rabbitMqConnection = rabbitMqConnection;
            this.sqlConnection = sqlConnection;
            this.messageDataService = messageDataService;
            this.logger = logger;
            this.Id = Guid.NewGuid().ToString("D");
            
            this.runThread = new Thread(() => this.HandlePublishAsync().GetAwaiter().GetResult());
        }

        public void Initialize(string exchange, int messagesPerSecond)
        {
            if (this.isInitialized) throw new InvalidOperationException("Initialize só pode ser chamado uma vez");
            this.exchange = exchange;
            this.MessagesPerSecond = messagesPerSecond;
            this.TimeToWait = messagesPerSecond == 0 ? TimeSpan.Zero : this.MessagesPerSecond.AsMessageRateToSleepTimeSpan();
            this.isInitialized = true;
        }

        private async Task HandlePublishAsync()
        {
            
            //this.channel.ConfirmSelect(); //Ack na publicação.

            long count = 0;
            while (this.isRunning)
            {
                this.TimeToWait.Wait();
                count++;

                //Esse controle transacional deveria ser abstraído
                using NpgsqlTransaction transaction = await this.sqlConnection.BeginTransactionAsync().ConfigureAwait(true);
               
                try
                {
                    //Thread.Sleep(TimeSpan.FromMilliseconds(100));

                    /*Aqui deveria chamar alguma camada de negócio*/
                    Message message = this.messageDataService.CreateMessage(transaction, this.sqlConnection);
                    //fim

                    await this.channel.BasicPublishAsync(
                        exchange: this.exchange,
                        routingKey: string.Empty,
                        mandatory: true,
                        basicProperties: this.channel
                                            .CreatePersistentBasicProperties()
                                            .SetMessageId(Guid.NewGuid()
                                            .ToString("D")), //Extension Method para criar um basic properties com persistência
                        body: message.Serialize().ToByteArray().ToReadOnlyMemory()
                    ).ConfigureAwait(true);

                    //this.channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5)); //Ack na publicação.

                    await transaction.CommitAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    this.logger.LogError(ex, "Erro ao publicar mensagem. Transação com banco foi abortada.");
                }
            }

            //se chegamos aqui, nosso worker foi parado.

            await this.channel.CloseAsync().ConfigureAwait(true);

            await this.channel.DisposeAsync().ConfigureAwait(true);

            await this.rabbitMqConnection.CloseAsync().ConfigureAwait(true);

            await this.rabbitMqConnection.DisposeAsync().ConfigureAwait(true);

            await this.sqlConnection.CloseAsync().ConfigureAwait(true);

        }

        public Publisher Start()
        {
            if (this.isInitialized == false) throw new InvalidOperationException("Instancia não inicializada");

            this.isRunning = true;
            this.runThread.Start();
            return this;
        }

         public Publisher Stop()
        {
            this.isRunning = false;

            return this;
        }
}