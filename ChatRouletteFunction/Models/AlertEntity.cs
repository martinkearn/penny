using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRouletteFunction.Models
{
    public class AlertEntity : TableEntity
    {
        public AlertEntity(string partition, string row)
        {
            this.PartitionKey = partition;
            this.RowKey = row;
        }
        public AlertEntity() { }

        public string ChatId { get; set; }

        public string ChatMessageId { get; set; }

        public string AlertCategory { get; set; }

        public string AlertText { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

    }
}
