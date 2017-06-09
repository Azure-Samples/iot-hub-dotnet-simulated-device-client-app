namespace CreateDeviceIdentity
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Telemetry;

    class Program
    {
        private static RegistryManager _registryManager;
        private const string ConnectionString = "{iot hub connection string}";

        private const string Name = "createdeviceidentity";

        private static void Main(string[] args)
        {
            _registryManager = RegistryManager.CreateFromConnectionString(ConnectionString);
            AddDeviceAsync().Wait();
            Console.ReadLine();
        }

        private static async Task AddDeviceAsync()
        {
            string deviceId = "myFirstDevice";
            Device device;
            try
            {
                device = await _registryManager.AddDeviceAsync(new Device(deviceId));
                Telemetry.Instance.Track("success", ConnectionString, Name, "register new device");
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await _registryManager.GetDeviceAsync(deviceId);
                Telemetry.Instance.Track("success", ConnectionString, Name, "device existed");
            }
            catch (Exception e)
            {
                Telemetry.Instance.Track("failed", ConnectionString, Name, $"register device failed: {e.Message}");
                Console.WriteLine($"register device failed: {e.Message}");
                return;
            }

            Console.WriteLine($"device key : {device.Authentication.SymmetricKey.PrimaryKey}");
        }
    }
}
