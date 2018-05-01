using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRouletteFunction.Models
{
    public class TextAnalyticsRequest
    {
        public List<TextAnalyticsRequestDocument> documents { get; set; }
    }
}
