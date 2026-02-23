using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models.API
{
    public class AvaliableBlocksModel
    {
        public string d { get; set; }
        public string s { get; set; } = "DEFAULT";
        public Guid venue_id { get; set; }
    }
}
