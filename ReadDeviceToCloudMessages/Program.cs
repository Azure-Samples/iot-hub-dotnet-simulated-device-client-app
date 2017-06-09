namespace ReadDeviceToCloudMessages
{
    using System;
    using System.Threading.Tasks;
    using System.Text;
    using Microsoft.ServiceBus.Messaging;
    using System.Threading;
    using System.Collections.Generic;
    using Telemetry;

    class Program
    {
        private const string ConnectionString = "{iothub connection string}";
        private const string IotHubD2CEndpoint = "messages/events";
        private static EventHubClient _eventHubClient;

        private const string Name = "readdevicetocloudmessages";

        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = _eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            try
            {
                while (true)
                {
                    if (ct.IsCancellationRequested) break;
                    var eventData = await eventHubReceiver.ReceiveAsync();
                    if (eventData == null) continue;
                    var data = Encoding.UTF8.GetString(eventData.GetBytes());
                    Console.WriteLine($"Message received. Partition: {partition} Data: '{data}'");
                }
            }
            catch (Exception e)
            {
                Telemetry.Instance.Track("failed", ConnectionString, Name, $"receive device message exception: {e.Message}");
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Receive messages. Ctrl-C to exit.\n");
            try
            {
                _eventHubClient = EventHubClient.CreateFromConnectionString(ConnectionString, IotHubD2CEndpoint);
            }
            catch (Exception e)
            {
                Telemetry.Instance.Track("failed", ConnectionString, Name, $"event hub client failed: {e.Message}");
                return;
            }

            Telemetry.Instance.Track("success", ConnectionString, Name, "event hub client created");
            var d2cPartitions = _eventHubClient.GetRuntimeInformation().PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }
            Task.WaitAll(tasks.ToArray());
        }
    }
}
