namespace ReadDeviceToCloudMessages
{
    using System;
    using System.Threading.Tasks;
    using System.Text;
    using Microsoft.ServiceBus.Messaging;
    using System.Threading;
    using System.Linq;

    class Program
    {
        private const string ConnectionString = "{iothub connection string}";
        private const string IotHubD2CEndpoint = "messages/events";
        private static EventHubClient _eventHubClient;

        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = _eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                var eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                var data = Encoding.UTF8.GetString(eventData.GetBytes());
                Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Receive messages. Ctrl-C to exit.\n");
            _eventHubClient = EventHubClient.CreateFromConnectionString(ConnectionString, IotHubD2CEndpoint);
            var partitions = _eventHubClient.GetRuntimeInformation().PartitionIds;
            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            var tasks = partitions.Select(partition => ReceiveMessagesFromDeviceAsync(partition, cts.Token)).ToList();
            Task.WaitAll(tasks.ToArray());
        }
    }
}
