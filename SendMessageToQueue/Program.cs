using Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Configuration;

namespace SendMessageToQueue
{
    class Program
    {
        static void Main(string[] args)
        {
            //settings
            var connectionString = ConfigurationManager.AppSettings["AzureStorageConnection"];
            var queueContainer = ConfigurationManager.AppSettings["AzureStorageMessagesQueueContainer"];

            //get user input
            Console.WriteLine("Enter UserId");
            var userId = Console.ReadLine();
            Console.WriteLine("Enter Message");
            var message = Console.ReadLine();
            var chatId = "05468bad-c1bb-4fbd-9338-40972f50e36e";
            var time = DateTime.UtcNow;

            //create ChatMessage
            var chatMessage = new ChatMessage()
            {
                ChatId = chatId,
                Message = message,
                Time = time, 
                UserId = userId
            };

            var chatMessageJson = JsonConvert.SerializeObject(chatMessage);

            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);


            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference(queueContainer);

            // Create a message and add it to the queue.
            CloudQueueMessage qMessage = new CloudQueueMessage(chatMessageJson);

            // Async enqueue the message
            queue.AddMessageAsync(qMessage).Wait();

        }
    }
}
