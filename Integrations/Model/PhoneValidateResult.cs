using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Integrations.Model
{
    public class PhoneValidateResult
    {
        public string status { get; set; }
        public string linetype { get; set; }
        public string location { get; set; }
        public string countrycode { get; set; }
        public string formatnational { get; set; }
        public string formatinternational { get; set; }
        public string mcc { get; set; }
        public string mnc { get; set; }
    }

}
