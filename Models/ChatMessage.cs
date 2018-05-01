using System;
using System.Collections.Generic;

namespace Models
{
    public class ChatMessage
    {
        public string Message { get; set; }
        public string ChatId { get; set; }
        public string UserId { get; set; }
        public DateTime Time { get; set; }
        public double Sentiment { get; set; }
        public string KeyPhrases { get; set; }
        public List<Alert> Alerts { get; set; }
        public string RowKey { get; set; }
    }
}
