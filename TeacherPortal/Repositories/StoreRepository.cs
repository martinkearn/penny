using Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeacherPortal.Interfaces;
using TeacherPortal.Models;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using TeacherPortal.Extensions;

namespace TeacherPortal.Repositories
{
    public class StoreRepository : IStoreRepository
    {
        private readonly AppSettings _appSettings;

        public StoreRepository(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<List<ChatMessage>> GetChatMessages(string ChatId, bool IncludeAlerts)
        {
            var table = await GetCloudTable(_appSettings.StorageConnectionString, _appSettings.MessagesTableContainerName);

            TableQuery<TableEntityAdapter<ChatMessage>> query = new TableQuery<TableEntityAdapter<ChatMessage>>();
            if (!string.IsNullOrEmpty(ChatId))
            {
                query = new TableQuery<TableEntityAdapter<ChatMessage>>()
                    .Where(TableQuery.GenerateFilterCondition("ChatId", QueryComparisons.Equal, ChatId));
            }

            TableContinuationToken token = null;
            var entities = new List<TableEntityAdapter<ChatMessage>>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            // create list of objects from the storage entities with alerts
            var chatMessages = new List<ChatMessage>();
            foreach (var entity in entities)
            {
                var chatMessage = entity.OriginalEntity;
                chatMessage.RowKey = entity.RowKey;

                if (IncludeAlerts)
                {
                    chatMessage.Alerts = await GetAlerts(entity.RowKey, null);
                }

                chatMessages.Add(chatMessage);
            }

            return chatMessages;
        }

        public async Task<List<Chat>> GetChats()
        {
            var allChatMessages = await GetChatMessages(null, false);

            //get distinct chat IDs
            var chatIds = allChatMessages
                .Select(x => x.ChatId)
                .Distinct()
                .ToList();

            //get distinct chats
            var chats = new List<Chat>();
            foreach (var chatId in chatIds)
            {
                var chatParticipants = allChatMessages
                    .Where(x => x.ChatId == chatId)
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToList();

                var chatMessages = allChatMessages.Where(x => x.ChatId == chatId).ToList();

                //get average sentiment of all messages in chat
                var averageSentiment = chatMessages.DefaultIfEmpty().Average(x => x.Sentiment);

                //get alert count
                var alertsForChat = await GetAlerts(null, chatId);
                
                var chat = new Chat()
                {
                    Id = chatId,
                    Participants = chatParticipants,
                    Messages = chatMessages,
                    Sentiment = averageSentiment,
                    AlertCount = alertsForChat.Count
                };

                chats.Add(chat);
            }

            return chats;
        }

        public async Task<Chat> GetChat(string ChatId)
        {
            var allChatMessagesForChat = await GetChatMessages(ChatId, true);

            var chatParticipants = allChatMessagesForChat
                .Select(x => x.UserId)
                .Distinct()
                .ToList();

            var chatMessages = allChatMessagesForChat
                .OrderBy(x => x.Time)
                .ToList();

            var chat = new Chat()
            {
                Id = ChatId,
                Participants = chatParticipants,
                Messages = chatMessages
            };

            return chat;
        }

        public async Task<bool> DeleteChat(string ChatId, string partitionKey)
        {
            var messages = await GetChatMessages(ChatId, true);

            //first delete any alerts stored against messages in chat
            var alerts = new List<Alert>();
            foreach (var message in messages.Where(x => x.Alerts.Count > 0).ToList())
            {
                foreach (var alertForMessage in message.Alerts)
                {
                    alerts.Add(alertForMessage);
                }
            }
            var allAlertIds = alerts.Select(x => x.RowKey).ToList();
            var alertIdsChunk = allAlertIds.ChunkBy(100);
            var alertsTable = await GetCloudTable(_appSettings.StorageConnectionString, _appSettings.AlertsTableContainerName);
            foreach (var alertIds in alertIdsChunk)
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (var alertId in alertIds)
                {
                    TableOperation retrieveOperation = TableOperation.Retrieve<TableEntityAdapter<Alert>>(partitionKey, alertId);
                    TableResult retrievedResult = await alertsTable.ExecuteAsync(retrieveOperation);
                    var result = (TableEntityAdapter<Alert>)retrievedResult.Result;
                    batchOperation.Delete(result);
                }
                await alertsTable.ExecuteBatchAsync(batchOperation);
            }


            //now delete messages
            var messagesTable = await GetCloudTable(_appSettings.StorageConnectionString, _appSettings.MessagesTableContainerName);
            var allMessageIds = messages.Select(x => x.RowKey).ToList();
            var messageIdsChunk = allMessageIds.ChunkBy(100);
            foreach (var messageIds in messageIdsChunk)
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (var messageId in messageIds)
                {
                    TableOperation retrieveOperation = TableOperation.Retrieve<TableEntityAdapter<ChatMessage>>(partitionKey, messageId);
                    TableResult retrievedResult = await messagesTable.ExecuteAsync(retrieveOperation);
                    var result = (TableEntityAdapter<ChatMessage>)retrievedResult.Result;
                    batchOperation.Delete(result);
                }
                await messagesTable.ExecuteBatchAsync(batchOperation);
            }

            return true;
        }

        public async Task<bool> InsertUserMapping(UserMapping userMapping, string partitionKey)
        {
            var table = await GetCloudTable(_appSettings.StorageConnectionString, _appSettings.MappingTableContainerName);

            TableEntityAdapter<UserMapping> entity = new TableEntityAdapter<UserMapping>(userMapping, partitionKey, Guid.NewGuid().ToString());

            TableOperation insertOperation = TableOperation.InsertOrReplace(entity);

            TableResult result = await table.ExecuteAsync(insertOperation);

            return true;
        }

        public async Task<bool> DeleteUserMapping(string partitionKey, string rowKey)
        {
            //get cloudtable
            var table = await GetCloudTable(_appSettings.StorageConnectionString, _appSettings.MappingTableContainerName);

            // Create a retrieve operation that expects a the right entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntityAdapter<UserMapping>>(partitionKey, rowKey);

            // Execute the operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            // Assign the result to an Entity.
            var deleteEntity = (TableEntityAdapter<UserMapping>)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                // Create the Delete TableOperation.
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                await table.ExecuteAsync(deleteOperation);

                return true;
            }
            else
            {
                return false;
            }

        }
        
        public async Task<UserMapping> GetUserMapping(string partitionKey, string rowKey)
        {
            //get cloudtable
            var table = await GetCloudTable(_appSettings.StorageConnectionString, _appSettings.MappingTableContainerName);

            // Create a retrieve operation that expects a the right entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntityAdapter<UserMapping>>(partitionKey, rowKey);

            // Execute the operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            // Assign the result to an Entity.
            var entity = (TableEntityAdapter<UserMapping>)retrievedResult.Result;

            return entity.OriginalEntity;
        }

        public async Task<List<UserMapping>> GetUserMappings()
        {
            var table = await GetCloudTable(_appSettings.StorageConnectionString, _appSettings.MappingTableContainerName);

            TableContinuationToken token = null;

            var entities = new List<TableEntityAdapter<UserMapping>>();

            TableQuery<TableEntityAdapter<UserMapping>> query = new TableQuery<TableEntityAdapter<UserMapping>>();

            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            // create list of objects from the storage entities
            var userMappings = new List<UserMapping>();
            foreach (var entity in entities)
            {
                UserMapping toBeAdded = entity.OriginalEntity;
                toBeAdded.RowKey = entity.RowKey;
                userMappings.Add(toBeAdded);
            }

            return userMappings;
        }

        private async Task<CloudTable> GetCloudTable(string tableConnectionString, string containerName)
        {
            var storageAccount = CloudStorageAccount.Parse(tableConnectionString);
            var blobClient = storageAccount.CreateCloudTableClient();
            var table = blobClient.GetTableReference(containerName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        private async Task<List<Alert>> GetAlerts(string chatMessageId, string chatId)
        {
            var table = await GetCloudTable(_appSettings.StorageConnectionString, _appSettings.AlertsTableContainerName);

            TableContinuationToken token = null;

            var entities = new List<TableEntityAdapter<Alert>>();

            TableQuery<TableEntityAdapter<Alert>> query = new TableQuery<TableEntityAdapter<Alert>>();
            if (!string.IsNullOrEmpty(chatMessageId))
            {
                query = new TableQuery<TableEntityAdapter<Alert>>()
                    .Where(TableQuery.GenerateFilterCondition("ChatMessageId", QueryComparisons.Equal, chatMessageId));
            }
            if (!string.IsNullOrEmpty(chatId))
            {
                query = new TableQuery<TableEntityAdapter<Alert>>()
                    .Where(TableQuery.GenerateFilterCondition("ChatId", QueryComparisons.Equal, chatId));
            }

            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            // create list of objects from the storage entities with alerts
            var alerts = new List<Alert>();
            foreach (var entity in entities)
            {
                var alert = entity.OriginalEntity;
                alert.RowKey = entity.RowKey;
                alerts.Add(alert);
            }

            return alerts;
        }

    }
}
