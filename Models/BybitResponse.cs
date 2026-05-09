using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BybitApp1.Models
{
    public class BybitResponse
    {

        public string RetCode { get; set; }
        public string RetMsg { get; set; }
        public JObject Result { get; set; }

    }
}
