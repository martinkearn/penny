using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeacherPortal.Models
{
    public class AppSettings
    {
        public string MessagesTableContainerName { get; set; }

        public string StorageConnectionString { get; set; }

        public string MappingTableContainerName { get; set; }

        public string AlertsTableContainerName { get; set; }

    }
}
