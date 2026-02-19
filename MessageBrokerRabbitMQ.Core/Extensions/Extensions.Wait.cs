namespace MessageBrokerRabbitMQ.Core.Infrastructure.Extensions
{
    public static partial class Extensions
    {
        public static void Wait(this TimeSpan time)
        {
            if (time != TimeSpan.Zero)
            {
                Thread.Sleep(time);
            }
        }


    }
}
