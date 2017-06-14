namespace Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;

    public sealed class Telemetry
    {
        private static readonly Lazy<Telemetry> Lazy = new Lazy<Telemetry>(() => new Telemetry());
        public static Telemetry Instance => Lazy.Value;
        private static readonly string ConfigFilePath = GetTelemetryConfigPath();
        private static readonly XDocument Doc = XDocument.Load(ConfigFilePath);
        private const string ConfigFileName = "telemetry.config";
        private const string TelemetryKey = "telemetry";
        private const string InstrumentationKey = "instrumentationKey";
        private const string SimulatedDevice = "simulated device";

        private const string PromptText =
            "Microsoft would like to collect data about how users use Azure IoT samples and some problems they encounter.\r\n" +
            "Microsoft uses this information to improve our tooling experience.\r\n" +
            "Participation is voluntary and when you choose to participate, your device will automatically sends information to Microsoft about how you use Azure IoT samples";

        private static readonly TelemetryClient Client = new TelemetryClient
        {
            InstrumentationKey = ReadConfig(InstrumentationKey)
        };

        private static string GetTelemetryConfigPath()
        {
            var projectPath = Directory.GetParent(Environment.CurrentDirectory).Parent;
            var temp = projectPath;
            var configPath = "";
            do
            {
                configPath = Path.Combine(temp.Parent.FullName, ConfigFileName);
            } while (!File.Exists(configPath) && temp.Parent != null);

            return configPath;
        }

        private static string ReadConfig(string key)
        {
            return !string.IsNullOrEmpty(ConfigFilePath) ? Doc.Descendants(key).First()?.Value : null;
        }

        private static void SetConfig(string key, string value)
        {
            if (!string.IsNullOrEmpty(ConfigFilePath))
            {
                var element = Doc.Descendants(key).First();
                if (element != null)
                {
                    element.Value = value;
                    Doc.Save(ConfigFilePath);
                }
            }
        }

        public static void AskForPermission()
        {
            if (!string.IsNullOrEmpty(ReadConfig(TelemetryKey)))
            {
                return;
            }
            string response;
            Console.WriteLine(PromptText);
            do
            {
                Console.Write("Select y to enable data collection :(y/n, default is y) ");
                response = Console.ReadLine();
            } while (response != "" && response.ToLower() != "y" && response.ToLower() != "n");
            SetConfig(TelemetryKey, (response == "" || response.ToLower() == "y") ? "true" : "false");
            Instance.TrackUserChoice(response);
        }

        private static string SHA256Hash(string value)
        {
            using (var hash = SHA256.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(item => item.ToString("x2")));
            }
        }

        private static string GetMac()
        {
            return
                NetworkInterface.GetAllNetworkInterfaces()
                    .Where(p => p.OperationalStatus == OperationalStatus.Up)
                    .Select(p => p.GetPhysicalAddress().ToString())
                    .FirstOrDefault();
        }

        private void TrackUserChoice(string choice)
        {
            if (string.IsNullOrEmpty(choice))
            {
                choice = "y";
            }
            Client.TrackEvent("success", new Dictionary<string, string>
            {
                {"language", "C#"},
                {"device", SimulatedDevice},
                {"userchoice", choice}
            });
        }

        public void Track(string eventName, string connectionstring, string projectName, string message)
        {
            bool telemetrySwitch;
            bool.TryParse(ReadConfig(TelemetryKey), out telemetrySwitch);
            if (!telemetrySwitch)
            {
                return;
            }
            try
            {
                Client.TrackEvent(eventName, new Dictionary<string, string>
                {
                    {"iothub", SHA256Hash(connectionstring.Split('.').FirstOrDefault())},
                    {"language", "C#"},
                    {"device", SimulatedDevice},
                    {"project_name", projectName},
                    {"osversion", Environment.OSVersion.ToString()},
                    {"mac", SHA256Hash(GetMac())},
                    {"message", message}
                });
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
