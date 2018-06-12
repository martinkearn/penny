using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ChatRouletteFunction.Models;
using Microsoft.Azure; //Namespace for CloudConfigurationManager
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRouletteFunction
{
    public static class MessageProcessingFunction
    {
        [FunctionName("MessageProcessingFunction")]
        public static async Task Run([QueueTrigger("messagesqueue", Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
        {
            log.Info($"Processing: {myQueueItem}");

            //variables
            var azureWebJobsStorage = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");
            var messagesTableContainerName = CloudConfigurationManager.GetSetting("MessagesTableContainerName");
            var messagesTablePartitionKey = CloudConfigurationManager.GetSetting("MessagesTablePartitionKey");
            var alertsTableContainerName = CloudConfigurationManager.GetSetting("AlertsTableContainerName");
            var alertsTablePartitionKey = CloudConfigurationManager.GetSetting("AlertsTablePartitionKey");
            var textAnalyticsApiKey = CloudConfigurationManager.GetSetting("TextAnalyticsApiKey");
            var textAnalyticsApiUrl = CloudConfigurationManager.GetSetting("TextAnalyticsApiUrl");
            var luisApiKey = CloudConfigurationManager.GetSetting("LuisApiKey");
            var luisApiUrl = CloudConfigurationManager.GetSetting("LuisApiUrl");
            var bullyingApiKey = CloudConfigurationManager.GetSetting("BullyingApiKey");
            var bullyingApiUrl = CloudConfigurationManager.GetSetting("BullyingApiUrl");
            var storageAccount = CloudStorageAccount.Parse(azureWebJobsStorage);
            var tableClient = storageAccount.CreateCloudTableClient();

            //parse the message JSON to a dynamic object, then entity
            dynamic queueItem = JObject.Parse(myQueueItem);
            var chatMessageEntity = new ChatMessageEntity(messagesTablePartitionKey, Guid.NewGuid().ToString());
            chatMessageEntity.Message = queueItem.Message;
            chatMessageEntity.Time = queueItem.Time;
            chatMessageEntity.UserId = queueItem.UserId;
            chatMessageEntity.ChatId = queueItem.ChatId;

            //Get Text Analytics Sentiment data and add to entity
            var sentimentData = await GetTextAnalyticsData(textAnalyticsApiUrl, textAnalyticsApiKey, queueItem.Message.ToString(), "sentiment");
            SentimentResponse sentiment = JsonConvert.DeserializeObject<SentimentResponse>(sentimentData);
            chatMessageEntity.Sentiment = sentiment.documents[0].score;
            log.Info($"Sentiment: {chatMessageEntity.Sentiment}");

            //Get Text Analytics key phrase data and add to entity
            var keyPhrasesData = await GetTextAnalyticsData(textAnalyticsApiUrl, textAnalyticsApiKey, queueItem.Message.ToString(), "keyPhrases");
            KeyPhrasesResponse keyPhrases = JsonConvert.DeserializeObject<KeyPhrasesResponse>(keyPhrasesData);
            chatMessageEntity.KeyPhrases = string.Join(",", keyPhrases.documents[0].keyPhrases);
            log.Info($"Key Phrases: {chatMessageEntity.KeyPhrases }");

            //Do LUIS entity and intent extraction here
            var luisData = await GetLUISData(luisApiUrl, luisApiKey, queueItem.Message.ToString());
            LuisResponse luis = JsonConvert.DeserializeObject<LuisResponse>(luisData);
            if (luis.topScoringIntent.intent != "None")
            {
                //create an alert
                var alertEntity = new AlertEntity(alertsTablePartitionKey, Guid.NewGuid().ToString());
                alertEntity.AlertCategory = ResolveCategory(luis.topScoringIntent.intent);
                alertEntity.AlertText = queueItem.Message.ToString();
                alertEntity.ChatMessageId = chatMessageEntity.RowKey.ToString();
                alertEntity.StartIndex = -1;
                alertEntity.EndIndex = -1;
                alertEntity.ChatId = chatMessageEntity.ChatId;

                await LogAlert(tableClient, alertsTableContainerName, alertEntity);
            }
            if (luis.entities.Count > 0)
            {
                //create an alert for each entity
                foreach (var entity in luis.entities)
                {
                    //create an alert
                    var alertEntity = new AlertEntity(alertsTablePartitionKey, Guid.NewGuid().ToString());
                    alertEntity.AlertCategory = ResolveCategory(entity.type);
                    alertEntity.AlertText = entity.entity;
                    alertEntity.ChatMessageId = chatMessageEntity.RowKey.ToString();
                    alertEntity.StartIndex = entity.startIndex;
                    alertEntity.EndIndex = entity.endIndex;
                    alertEntity.ChatId = chatMessageEntity.ChatId;

                    await LogAlert(tableClient, alertsTableContainerName, alertEntity);
                }
            }

            //bullying detection
            var bullyingData = await GetBullyingData(bullyingApiUrl, bullyingApiKey, queueItem.Message.ToString());

            // Create the TableOperation object that inserts the entity
            var messagesTable = tableClient.GetTableReference(messagesTableContainerName);
            var messageInsertOperation = TableOperation.Insert(chatMessageEntity);
            messagesTable.Execute(messageInsertOperation);

            log.Info($"Processed: {myQueueItem}");
        }

        public static async Task<string> GetTextAnalyticsData(string apiUrl, string apiKey, string utterance, string apiOperation)
        {
            using (var client = new HttpClient())
            {
                //setup HttpClient
                var fullApiUrl = apiUrl + apiOperation;
                client.BaseAddress = new Uri(fullApiUrl);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //setup data object
                var documents = new List<TextAnalyticsRequestDocument>();
                documents.Add(new TextAnalyticsRequestDocument()
                {
                    id = Guid.NewGuid().ToString(),
                    language = "en",
                    text = utterance
                });
                var dataObject = new TextAnalyticsRequest()
                {
                    documents = documents
                };

                //setup httpContent object
                var dataJson = JsonConvert.SerializeObject(dataObject);
                HttpResponseMessage response;
                using (HttpContent content = new StringContent(dataJson))
                {
                    content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    response = await client.PostAsync(fullApiUrl, content);
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public static async Task<string> GetLUISData(string apiUrl, string apiKey, string utterance)
        {
            using (var client = new HttpClient())
            {
                //setup HttpClient
                var fullApiUrl = $"{apiUrl}subscription-key={apiKey}&verbose=true&timezoneOffset=0&q={utterance}";
                client.BaseAddress = new Uri(fullApiUrl);

                //setup httpContent object
                var response = await client.GetAsync(fullApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public static async Task<string> GetBullyingData(string apiUrl, string apiKey, string utterance)
        {
            return string.Empty;
        }

        public static async Task<bool> LogAlert(CloudTableClient client, string alertsTableContainerName, AlertEntity alertEntity)
        {
            var alertsTable = client.GetTableReference(alertsTableContainerName);
            var alertInsertOperation = TableOperation.Insert(alertEntity);
            await alertsTable.ExecuteAsync(alertInsertOperation);
            return true;
        }

        public static string ResolveCategory(string rawCategory)
        {
            switch (rawCategory.ToLower())
            {
                case "builtin.age":
                    return "Age";
                case "communication.sendername":
                    return "Name";
                case "builtin.email":
                    return "Email";
                default:
                    return rawCategory;
            }
        }
    }

}
