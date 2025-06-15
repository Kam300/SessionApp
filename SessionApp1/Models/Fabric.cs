using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionApp1.Models
{
    public class Fabric
    {
        public string Article { get; set; } = string.Empty;
        public int NameCode { get; set; }
        public int ColorCode { get; set; }
        public int PatternCode { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int CompositionCode { get; set; }
        public int WidthMm { get; set; }
        public int LengthMm { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string FabricName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string PatternName { get; set; } = string.Empty;
        public string CompositionName { get; set; } = string.Empty;
    }

    public class FabricStock
    {
        public string RollId { get; set; } = string.Empty;
        public string FabricArticle { get; set; } = string.Empty;
        public int LengthMm { get; set; }
        public int WidthMm { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}

