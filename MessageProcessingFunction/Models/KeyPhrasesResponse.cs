using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRouletteFunction.Models
{
    public class KeyPhrasesResponse
    {
        public List<KeyPhrasesResponseDocument> documents { get; set; }
        public List<object> errors { get; set; }
    }
}
