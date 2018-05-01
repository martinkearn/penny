using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRouletteFunction.Models
{
    public class TextAnalyticsRequestDocument
    {
        public string language { get; set; }
        public string id { get; set; }
        public string text { get; set; }
    }
}
