namespace SimulatedDevice
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using Telemetry;

    class Program
    {
        private static DeviceClient _deviceClient;
        private const string IoTHubUri = "{iot hub hostname}";
        private const string DeviceKey = "{device key}";

        private const string DeviceId = "myFirstDevice";
        private const string Name = "simulateddevice";

        private static void Main(string[] args)
        {
            Telemetry.AskForPermission();
            Console.WriteLine("Simulated device\n");
            try
            {
                _deviceClient = DeviceClient.Create(IoTHubUri,new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey), TransportType.Mqtt);
            }
            catch (Exception e)
            {
                Telemetry.Instance.Track("failed", IoTHubUri, Name, $"create device client failed: {e.Message}");
                return;
            }
            Telemetry.Instance.Track("success", IoTHubUri, Name, "device client created");
            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }

        private static async void SendDeviceToCloudMessagesAsync()
        {
            double minTemperature = 20;
            double minHumidity = 60;
            int messageId = 1;
            Random rand = new Random();
            try
            {
                while (true)
                {
                    double currentTemperature = minTemperature + rand.NextDouble() * 15;
                    double currentHumidity = minHumidity + rand.NextDouble() * 20;

                    var telemetryDataPoint = new
                    {
                        messageId = messageId++,
                        deviceId = DeviceId,
                        temperature = currentTemperature,
                        humidity = currentHumidity
                    };
                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                    await _deviceClient.SendEventAsync(message);
                    Console.WriteLine($"{DateTime.Now} > Sending message: {messageString}");
                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                Telemetry.Instance.Track("failed", IoTHubUri, Name, $"send message to cloud exception: {e.Message}");
            }
        }
    }
}
