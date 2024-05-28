using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;
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
                .WithTcpServer("192.168.0.100", 1883) //use your ip here 
                .Build();

            mqttClient.ConnectedAsync += async (MqttClientConnectedEventArgs e) =>
            {
                Console.WriteLine("Connected to broker.");
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f => { f.WithTopic("door"); })
                    .WithTopicFilter(f => { f.WithTopic("temperature"); })
                    .Build();

                await mqttClient.SubscribeAsync(subscribeOptions);
                Console.WriteLine("Subscribed to topics: door, temperature.");
            };

            mqttClient.DisconnectedAsync += e =>
            {
                Console.WriteLine("Disconnected from broker.");
                return Task.CompletedTask;
            };

            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine($"Received message on topic '{e.ApplicationMessage.Topic}': {message}");
                return Task.CompletedTask;
            };

            try
            {
                await mqttClient.ConnectAsync(options, System.Threading.CancellationToken.None);
                Console.WriteLine("Press 'q' to quit.");
                Console.WriteLine("Type 'door closed' or 'door opened' to send door status.");
                Console.WriteLine("Type 'temperature 22°C' to send temperature.");

                string input;
                while ((input = Console.ReadLine()) != "q")
                {
                    string topic = "";
                    string payload = "";

                    if (input.StartsWith("door"))
                    {
                        topic = "door";
                        payload = input.Substring(5); // Extract "closed" or "opened"
                    }
                    else if (input.StartsWith("temperature"))
                    {
                        topic = "temperature";
                        payload = input.Substring(12); // Extract temperature value
                    }

                    if (!string.IsNullOrEmpty(topic) && !string.IsNullOrEmpty(payload))
                    {
                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(payload)
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                            .WithRetainFlag()
                            .Build();

                        await mqttClient.PublishAsync(message, System.Threading.CancellationToken.None);
                        Console.WriteLine($"Message published to {topic}: {payload}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please type 'door closed', 'door opened', or 'temperature <value>'.");
                    }
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
