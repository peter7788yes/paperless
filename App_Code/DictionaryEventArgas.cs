using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    public class DictionaryEventArgas : EventArgs
    {
        public Dictionary<string, object> dict { get; set; }
    }
