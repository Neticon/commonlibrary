using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Integrations.Model
{
    public class RedisDeviceIntel
    {
        public string EmailValidation { get; set; }
        public string PhoneValidation { get; set; }
        public string LocalPhone { get; set; }  
        public string IPValidation { get; set; }
    }
}
