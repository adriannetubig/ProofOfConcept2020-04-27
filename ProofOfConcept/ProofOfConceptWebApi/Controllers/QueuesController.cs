using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace ProofOfConceptWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueuesController : ControllerBase
    {
        public static int Counter = 0;
        public static int CurrentQueued = 0;

        [HttpPost("New")]
        public IActionResult New()
        {
            Counter++;
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "NewOrderQueue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var message = $"Order {Counter} Created On {DateTime.UtcNow}";
                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "",
                                     routingKey: "task_queue",
                                     basicProperties: properties,
                                     body: body);
            }
            return Ok();
        }

        [HttpPost("Add")]
        public IActionResult Add()
        {
            if (CurrentQueued < 10)
            {
                CurrentQueued += 1;

                _ = Task.Run(() =>
                  {
                      Thread.Sleep(10000);
                      string path = @$"D:\Shared\Test.txt";
                      using (StreamWriter sw = System.IO.File.CreateText(path)) ;
                  });

                return new ObjectResult(null)
                {
                    StatusCode = (int)HttpStatusCode.Created
                };
            }
            else
            {
                return new ObjectResult(null)
                {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }
        }

        [HttpPut("Done")]
        public IActionResult Done()
        {
            CurrentQueued -= 1;

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "OtherQueue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var message = $"";
                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "",
                                     routingKey: "task_queue_done",
                                     basicProperties: properties,
                                     body: body);
            }
            return Ok();
        }
    }
}
