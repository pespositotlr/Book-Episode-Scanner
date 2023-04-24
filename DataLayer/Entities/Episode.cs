using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Entities
{
    public class Episode
    {
        public int ID { get; set; }
        public int BookID { get; set; }
        public string? Name { get; set; }
        public string? LookupValue { get; set; }
        public bool IsDownloaded { get; set; }
        public DateTime? DateCreated { get; set; }
        public int SequenceNumber { get; set; }
    }
}
