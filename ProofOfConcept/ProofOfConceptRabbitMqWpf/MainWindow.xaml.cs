using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProofOfConceptRabbitMqWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Restarter();
            MainReciever();
        }


        private void MainReciever()
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "NewOrderQueue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Listbox1.Items.Add("Initialize Queue Listner");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    this.Dispatcher.Invoke(() =>
                    {
                        Listbox1.Items.Add($"Received {message}");
                    });

                    using (var _httpClient = new HttpClient())
                    {
                        _httpClient.BaseAddress = new Uri("http://localhost:54032");

                        _httpClient.DefaultRequestHeaders.Accept.Clear();
                        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage response = await _httpClient.PostAsync($"Queues/Add", null);

                        if (response.StatusCode == System.Net.HttpStatusCode.Created)
                        {
                            try
                            {
                                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }

                            this.Dispatcher.Invoke(() =>
                            {
                                Listbox1.Items.Add($"Done {message}");
                            });
                        }
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                Listbox1.Items.Add($"Queue Limit On {message}");
                            });
                            channel.Close();
                        }
                        Console.WriteLine(string.Empty);
                    }

                };
                channel.BasicConsume(queue: "task_queue",
                                     autoAck: false,
                                     consumer: consumer);
            }
        }

        private void Restarter()
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "OtherQueue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Listbox1.Items.Add($"Reseter set up");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Listbox1.Items.Add($"Reset");
                    });
                    MainReciever();

                };
                channel.BasicConsume(queue: "task_queue",
                                     autoAck: false,
                                     consumer: consumer);
            }
        }
    }
}
