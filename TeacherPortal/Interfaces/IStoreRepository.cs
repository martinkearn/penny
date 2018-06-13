using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeacherPortal.Interfaces
{
    public interface IStoreRepository
    {

        Task<List<ChatMessage>> GetChatMessages(string ChatId, bool IncludeAlerts);

        Task<List<Chat>> GetChats();

        Task<Chat> GetChat(string ChatId);

        Task<bool> DeleteChat(string ChatId, string partitionKey);

        Task<bool> DeleteAllChats(string partitionKey);

        Task<bool> InsertUserMapping(UserMapping userMapping, string partitionKey);

        Task<bool> DeleteUserMapping(string partitionKey, string rowKey);
        
        Task<UserMapping> GetUserMapping(string partitionKey, string rowKey);

        Task<List<UserMapping>> GetUserMappings();
    }
}
