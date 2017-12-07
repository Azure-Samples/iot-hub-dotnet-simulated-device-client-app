namespace SimulatedDevice
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;

    public class Program
    {
        private const string IotHubUri = "{iot hub hostname}";
        private const string DeviceKey = "{device key}";
        private const string DeviceId = "myFirstDevice";
        private const double MinTemperature = 20;
        private const double MinHumidity = 60;
        private static readonly Random Rand = new Random();
        private static DeviceClient _deviceClient;
        private static int _messageId = 1;

        private static async void SendDeviceToCloudMessagesAsync()
        {
            while (true)
            {
                var currentTemperature = MinTemperature + Rand.NextDouble() * 15;
                var currentHumidity = MinHumidity + Rand.NextDouble() * 20;

                var telemetryDataPoint = new
                {
                    messageId = _messageId++,
                    deviceId = DeviceId,
                    temperature = currentTemperature,
                    humidity = currentHumidity
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                await _deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000);
            }
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Simulated device\n");
            _deviceClient = DeviceClient.Create(IotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey), TransportType.Mqtt);
            _deviceClient.ProductInfo = "HappyPath_Simulated-CSharp";

            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }
    }
}
