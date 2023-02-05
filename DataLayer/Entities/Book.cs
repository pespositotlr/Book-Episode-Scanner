using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Entities
{
    public class Book
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? LookupValue { get; set; }
        public string? MiddleID { get; set; }
    }
}
