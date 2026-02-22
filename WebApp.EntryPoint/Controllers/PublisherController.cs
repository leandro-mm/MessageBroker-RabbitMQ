using RabbitMQBasic.Core;
using RabbitMQBasic.Core.Infrastructure.Queue;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.EntryPoint.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PublisherController : ControllerBase
{
    private readonly PublisherManager publisherManager;

    public PublisherController(PublisherManager publisherManager)
    {
        this.publisherManager = publisherManager;
    }

    [HttpPut]
    public void AddPublisher([FromQuery] int size, [FromQuery] int messagesPerSecond)
    {
        this.publisherManager.AddPublisher(size, messagesPerSecond);
    }
    [HttpDelete("{id}")]
    public void RemovePublisher(string id)
    {
        this.publisherManager.RemovePublisher(id);
    }

    [HttpGet()]
    public IEnumerable<Publisher> GetPublishers()
    {
        return this.publisherManager.Publishers;
    }
}