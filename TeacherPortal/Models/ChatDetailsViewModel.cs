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

        public bool HideParticipantNames { get; set; }

        public bool GradualReveal { get; set; }
    }
}
