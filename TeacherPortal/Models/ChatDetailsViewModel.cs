using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeacherPortal.Models
{
    public class ChatDetailsViewModel
    {
        public Chat Chat { get; set; }

        public List<DateTime> MessageTimes { get; set; }

        public bool HideParticipantNames { get; set; }

        public string OnlyShowUser { get; set; }

        public DateTime NewestMessageTimestamp { get; set; }
    }
}
