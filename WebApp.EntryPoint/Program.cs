using RabbitMQBasic.Core;
using RabbitMQBasic.Core.Infrastructure.Data;
using RabbitMQBasic.Core.Infrastructure.Extensions;
using RabbitMQBasic.Core.Infrastructure.Queue;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using WebApp.EntryPoint.Settings;

static async Task InitRabbitMQAsync(IServiceProvider applicationServices)
{
    using IChannel rabbitMQChannel = applicationServices.GetRequiredService<IChannel>();

    string queueName = "test_queue";
    bool durable = true;
    bool exclusive = false;
    bool autoDelete = false;
    string exchangeName = "test_exchange";
    string exchangeType = "fanout";

    await rabbitMQChannel.QueueDeclareAsync(queueName, durable, exclusive, autoDelete);
    await rabbitMQChannel.ExchangeDeclareAsync(exchangeName, exchangeType, durable, autoDelete);
    await rabbitMQChannel.QueueBindAsync(queueName, exchangeName, string.Empty);
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar RabbitMQSettings a partir do appsettings.json
builder.Services.AddSingleton<RabbitMQSettings>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var rabbitMQSettings = new RabbitMQSettings();
    configuration.GetSection("RabbitMQSettings").Bind(rabbitMQSettings);
    return rabbitMQSettings;
});

builder.Services.AddSingleton<ConnectionFactory>(sp =>
{
    var rabbitMQSettings = sp.GetRequiredService<RabbitMQSettings>();

    return new ConnectionFactory()
    {
        Uri = new Uri(rabbitMQSettings.ConnectionString),
        ConsumerDispatchConcurrency = 1,
    };
});

builder.Services.AddTransientWithRetry<IConnection, BrokerUnreachableException>(
    sp => sp.GetRequiredService<ConnectionFactory>().CreateConnectionAsync());

builder.Services.AddTransientWithRetry<IChannel, BrokerUnreachableException>(
    sp => sp.GetRequiredService<IConnection>().CreateChannelAsync());

builder.Services.AddTransientWithRetry<NpgsqlConnection, NpgsqlException>(async sp =>
{
    NpgsqlConnection connection = new($"Server={HostSettings.GetHost("postgres")};Port=5432;Database=Walkthrough;User Id=WalkthroughUser;Password=WalkthroughPass;");
    await connection.OpenAsync();
    return connection;
});

builder.Services.AddSingleton<MessageDataService>();
builder.Services.AddSingleton<PublisherManager>();
builder.Services.AddTransient<Publisher>();


var app = builder.Build();

InitRabbitMQAsync(app.Services).GetAwaiter().GetResult();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
//app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
