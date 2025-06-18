using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionApp1.Models
{
    public class Fitting
    {
        public string Article { get; set; } = string.Empty;
        public int NameCode { get; set; }
        public int ColorCode { get; set; }
        public int TypeCode { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int WidthMm { get; set; }
        public int HeightMm { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string FittingName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string CompositionName { get; set; } = string.Empty;
        public string LengthMm { get; set; } = string.Empty;
        public string DimensionUnit { get; set; } = string.Empty;
        public string WeightValue { get; set; } = string.Empty;
        public string WeightUnit { get; set; } = string.Empty;
        
        // Свойство для совместимости с существующим кодом
        public string Name { 
            get { return FittingName; } 
            set { FittingName = value; } 
        }
    }

    public class FittingStock
    {
        public string BatchId { get; set; } = string.Empty;
        public string FittingArticle { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
