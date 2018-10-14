using System;
using System.Collections.Generic;
using System.Text;

namespace DL
{
    public class InvoiceLine
    {
        public int Id { get; set; }
        public string Invoice { get; set; }
        public string Item { get; set; }
        public int Qty { get; set; }
        public double Price { get; set; }
        public double Amount { get; set; }
    }
}
