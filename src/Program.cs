using System;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Writes;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.IO;

namespace AzureIotInflux
{
    class Program
    {
        private const string ehubNamespaceConnectionString = "";
        private const string eventHubName = "";
        private const string blobStorageConnectionString = "";
        private const string blobContainerName = "";

        static BlobContainerClient storageClient;

        static InfluxDBClient influxClient;

        // The Event Hubs client types are safe to cache and use as a singleton for the lifetime
        // of the application, which is best practice when events are being published or read regularly.        
        static EventProcessorClient processor;    

        public static async Task Main(string[] args)
        {
            Console.Out.WriteLine("Starting app");

            influxClient = InfluxDBClientFactory.Create("http://influxdb:8086", "W-Ds3r25dF3fws8OoyDH6h35Eru0jSrhMYbIP26GiHV8s46GInbcZFnCK1rW0zEhT7tM3s5Iwq2qVAIHGJ3otw==");

            string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

            // Create a blob container client that the event processor will use 
            storageClient = new BlobContainerClient(blobStorageConnectionString, blobContainerName);

            // Create an event processor client to process events in the event hub
            processor = new EventProcessorClient(storageClient, consumerGroup, ehubNamespaceConnectionString, eventHubName);

            // Register handlers for processing events and handling errors
            processor.ProcessEventAsync += ProcessEventHandler;
            processor.ProcessErrorAsync += ProcessErrorHandler;

            // Start the processing
            await processor.StartProcessingAsync();

            // Wait forever
            await Task.Delay(-1);

            // Stop the processing
            await processor.StopProcessingAsync();
        }

        static async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            
            // Write the body of the event to the console window
            Console.WriteLine("\tReceived event: {0}", Encoding.UTF8.GetString(eventArgs.Data.EventBody.ToArray()));
            
            var body = await JsonSerializer.DeserializeAsync<Payload>(eventArgs.Data.EventBody.ToStream());
            body.deviceId = eventArgs.Data.SystemProperties["iothub-connection-device-id"].ToString();
            
            using (var writeApi = influxClient.GetWriteApi())
            {
                var point = PointData.Measurement("temperature")
                    .Tag("deviceId", body.deviceId)
                    .Field("ambient_temp", body.ambient.temperature)
                    .Field("ambient_pressure", body.ambient.pressure)
                    .Field("machine_temp", body.machine.temperature)
                    .Field("machine_temp", body.machine.temperature)
                    .Timestamp(eventArgs.Data.EnqueuedTime, WritePrecision.Ms);
                
                writeApi.WritePoint("telemetry", "test", point);
                Console.WriteLine("\tWrote event: {0}", Encoding.UTF8.GetString(eventArgs.Data.EventBody.ToArray()));
            }

            // Update checkpoint in the blob storage so that the app receives only new events the next time it's run
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }

        static Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            // Write details about the error to the console window
            Console.WriteLine($"\tPartition '{ eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine(eventArgs.Exception.Message);
            return Task.CompletedTask;
        }    
    }
}
