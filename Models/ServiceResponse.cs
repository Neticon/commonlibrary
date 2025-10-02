using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    public class ServiceResponse
    {
        public object Result { get; set; }
        public int StatusCode { get; set; }
        public string ResultJson => JsonConvert.SerializeObject(Result);
    }
}
