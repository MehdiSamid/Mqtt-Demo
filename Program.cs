using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

using MQTTnet.Protocol;

class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithClientId("LaptopClient")
                .WithTcpServer("10.5.222.253", 1883) 
                .Build();

            mqttClient.ConnectedAsync += async (MqttClientConnectedEventArgs e) =>
            {
                Console.WriteLine("Connected to broker.");
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f => { f.WithTopic("test/topic"); })
                    .Build();

                await mqttClient.SubscribeAsync(subscribeOptions);
                Console.WriteLine("Subscribed to topic.");
            };

            mqttClient.DisconnectedAsync += e =>
            {
                Console.WriteLine("Disconnected from broker.");
                return Task.CompletedTask;
            };

            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine($"Received message: {message}");
                return Task.CompletedTask;
            };

            try
            {
                await mqttClient.ConnectAsync(options, System.Threading.CancellationToken.None);
                Console.WriteLine("Press 'q' to quit, or type a message to publish.");

                string input;
                while ((input = Console.ReadLine()) != "q")
                {
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("test/topic")
                        .WithPayload(input)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                        .WithRetainFlag()
                        .Build();

                    await mqttClient.PublishAsync(message, System.Threading.CancellationToken.None);
                    Console.WriteLine("Message published.");
                }

                await mqttClient.DisconnectAsync();
            }
            catch (MQTTnet.Exceptions.MqttCommunicationException ex)
            {
                Console.WriteLine($"MqttCommunicationException: {ex.Message}");
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An initialization error occurred: {ex.Message}");
            Console.WriteLine(ex);
        }
    }
}
