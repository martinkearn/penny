using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRouletteFunction.Models
{
    public class ChatMessageEntity : TableEntity
    {
        public ChatMessageEntity(string partition, string row)
        {
            this.PartitionKey = partition;
            this.RowKey = row;
        }
        public ChatMessageEntity() { }
        public string Message { get; set; }
        public string ChatId { get; set; }
        public string UserId { get; set; }
        public DateTime Time { get; set; }
        public double Sentiment { get; set; }
        public string KeyPhrases { get; set; }
    }
}
