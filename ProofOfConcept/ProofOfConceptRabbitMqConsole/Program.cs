using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ProofOfConceptRabbitMqConsole
{
    class Program
    {
        public static void Main()
        {
            MainReciever();
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static void MainReciever()
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "NewOrderQueue",
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            Console.WriteLine("Waiting for messages.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received {message}");

                using (var _httpClient = new HttpClient())
                {
                    _httpClient.BaseAddress = new Uri("http://localhost:54032");

                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = await _httpClient.PostAsync($"Queues/Add", null);

                    if (response.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        Console.WriteLine($"Done {message}");
                    }
                    else
                    {
                        Console.WriteLine($"Queue Limit On {message}");
                        channel.Close();
                    }
                    Console.WriteLine(string.Empty);
                }

            };
            channel.BasicConsume(queue: "task_queue",
                                    autoAck: false,
                                    consumer: consumer);


            var channel2 = connection.CreateModel();

            channel2.ExchangeDeclare(exchange: "QueueReseter", type: ExchangeType.Fanout);

            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: queueName,
                              exchange: "QueueReseter",
                              routingKey: "");


            channel2.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            Console.WriteLine("Reset Waiter");

            var consumer2 = new EventingBasicConsumer(channel2);
            consumer2.Received += (model, ea) =>
            {
                Console.WriteLine("Reset");
                //Reset the reciever channel
                channel.Close();
                channel = connection.CreateModel();
                channel.QueueDeclare(queue: "NewOrderQueue",
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                channel.BasicConsume(queue: "task_queue",
                                        autoAck: false,
                                        consumer: consumer);
            };
            channel2.BasicConsume(queue: queueName,
                                    autoAck: true,
                                    consumer: consumer2);
        }
    }
}
