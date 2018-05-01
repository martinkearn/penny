using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Alert
    {
        public string ChatId { get; set; }
        public string ChatMessageId { get; set; }

        public string AlertCategory { get; set; }

        public string AlertText { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }
        public string RowKey { get; set; }
    }
}
