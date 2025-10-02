using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Integrations.Model
{
    public class EmailValidateResult
    {
        public int status { get; set; }
        public string info { get; set; }
        public string details { get; set; }
        public bool freemail { get; set; }
    }
}
