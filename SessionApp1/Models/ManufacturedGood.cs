using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionApp1.Models
{
    public class ManufacturedGood
    {
        public string Article { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int WidthMm { get; set; }
        public int LengthMm { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}

