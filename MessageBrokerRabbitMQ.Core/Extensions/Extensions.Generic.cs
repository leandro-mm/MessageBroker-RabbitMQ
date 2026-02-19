using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace MessageBrokerRabbitMQ.Core.Infrastructure.Extensions
{
    public static partial class Extensions
    {

        public static TimeSpan AsMessageRateToSleepTimeSpan(this int messagesPerSecond)
        {
            if (messagesPerSecond < 1)
                throw new ArgumentOutOfRangeException(nameof(messagesPerSecond));

            long ticksPerSecond = TimeSpan.FromSeconds(1).Ticks;

            //int factor = Convert.ToInt32(TimeSpan.FromMilliseconds(messagesPerSecond).Ticks * 0.005);
            //int sleepTimer = Convert.ToInt32((ticksPerSecond / messagesPerSecond) - factor);



            int sleepTimer = Convert.ToInt32((ticksPerSecond / messagesPerSecond) - (messagesPerSecond / 12 ));
            // Algorithm accuracy
            //Publish 10 m/s = 98% | 100 m/s = 96% | 500 m/s = 87% |  
            //Consume 10 m/s = 98% | 100 m/s = 94% | 500 m/s = 78% |  

            //int sleepTimer = Convert.ToInt32(ticksPerSecond / messagesPerSecond);
            //Algorithm accuracy
            //Publish 10 m/s = 98% | 100 m/s = 88% | 500 m/s = 61% |  
            //Consume 10 m/s = 98% | 100 m/s = 87% | 500 m/s = 54% |  


            return TimeSpan.FromTicks(Math.Max(sleepTimer, 0));
        }

        public static IServiceCollection AddTransientWithRetry<TService, TKnowException>(this IServiceCollection services, Func<IServiceProvider, Task<TService>> implementationFactory)
            where TKnowException : Exception
            where TService : class
        {
            return services.AddTransient(sp =>
            {
                TService returnValue = default;

                BuildPolicy<TKnowException>().Execute((context) =>
                {
                    returnValue = implementationFactory(sp).GetAwaiter().GetResult();
                });

                return returnValue;

            });
        }

        public static IServiceCollection AddSingletonWithRetry<TService, TKnowException>(this IServiceCollection services, Func<IServiceProvider, Task<TService>> implementationFactory)
            where TKnowException : Exception
            where TService : class
        {
            return services.AddSingleton(sp =>
            {
                TService returnValue = default;

                BuildPolicy<TKnowException>().Execute(() =>
                {
                    returnValue = implementationFactory(sp).GetAwaiter().GetResult();
                });

                return returnValue;

            });
        }

        public static IServiceCollection AddScopedWithRetry<TService, TKnowException>(this IServiceCollection services, Func<IServiceProvider, Task<TService>> implementationFactory)
           where TKnowException : Exception
           where TService : class
        {
            return services.AddScoped(sp =>
            {
                TService returnValue = default;

                BuildPolicy<TKnowException>().Execute(() =>
                {
                    returnValue = implementationFactory(sp).GetAwaiter().GetResult();
                });

                return returnValue;

            });
        }


        private static ResiliencePipeline BuildPolicy<TKnowException>(int retryCount = 5)
            where TKnowException : Exception
        {
            RetryStrategyOptions optionsComplex = new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TKnowException>(),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,  // Adds a random factor to the delay
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(3),
            };

            ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
                .AddRetry(optionsComplex)
                .Build();

            return pipeline;
        }
    }
}
