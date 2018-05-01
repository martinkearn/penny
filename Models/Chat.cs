using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Chat
    {
        public string Id { get; set; }

        public List<string> Participants { get; set; }

        public List<ChatMessage> Messages { get; set; }

        public double Sentiment { get; set; }

        public double AlertCount { get; set; }
    }
}
