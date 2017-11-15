namespace CreateDeviceIdentity
{
    using System;
    using System.Configuration;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Telemetry;

    public class Program
    {
        private static RegistryManager _registryManager;
        private const string ConnectionString = "{iot hub connection string}";
        private const string Name = "createdeviceidentity";
        private const string DeviceId = "myFirstDevice";
        private const string TelemetryKey = "telemetry";
        private const string InstrumentationKey = "instrumentationKey";
        private static Telemetry TelemetryClient;
        private static readonly Configuration Config = ConfigurationManager.OpenExeConfiguration(System.IO.Path.Combine(
            Environment.CurrentDirectory, System.Reflection.Assembly.GetExecutingAssembly().ManifestModule.Name));

        private static void Main(string[] args)
        {
            OptIn();
            _registryManager = RegistryManager.CreateFromConnectionString(ConnectionString);
            AddDeviceAsync().Wait();
            Console.ReadLine();
        }

        private static async Task AddDeviceAsync()
        {
            Device device;
            try
            {
                device = await _registryManager.AddDeviceAsync(new Device(DeviceId));
                SendTelemetry("success", "register new device");
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await _registryManager.GetDeviceAsync(DeviceId);
                SendTelemetry("success", "device existed");
            }
            catch (Exception e)
            {
                SendTelemetry("failed", $"register device failed: {e.Message}");
                Console.WriteLine($"register device failed: {e.Message}");
                throw;
            }

            Console.WriteLine($"device key : {device.Authentication.SymmetricKey.PrimaryKey}");
        }

        private static void OptIn()
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings[TelemetryKey]))
            {
                return;
            }

            try
            {
                string response;
                TelemetryClient = new Telemetry(ConfigurationManager.AppSettings[InstrumentationKey]);
                Console.WriteLine(Telemetry.PromptText);
                do
                {
                    Console.Write("Select y to enable data collection :(y/n, default is y) ");
                    response = Console.ReadLine();
                } while (response != "" && response.ToLower() != "y" && response.ToLower() != "n");

                var choice = response == "" || response.ToLower() == "y";
                Config.AppSettings.Settings.Remove(TelemetryKey);
                Config.AppSettings.Settings.Add(TelemetryKey, choice.ToString());
                Config.Save(ConfigurationSaveMode.Modified);
                TelemetryClient.TrackUserChoice(response);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private static void SendTelemetry(string eventName, string message)
        {
            if(TelemetryClient != null)
            {
                bool shouldSend;
                bool.TryParse(Config.AppSettings.Settings[TelemetryKey].Value, out shouldSend);
                if (shouldSend)
                {
                    TelemetryClient.Track(eventName, ConnectionString, Name, message);
                }
            }

        }
    }
}
