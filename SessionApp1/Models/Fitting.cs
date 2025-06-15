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
        public string Name { get; set; } = string.Empty;
        public decimal WidthMm { get; set; }
        public decimal LengthMm { get; set; }
        public string DimensionUnit { get; set; } = string.Empty;
        public decimal WeightValue { get; set; }
        public string WeightUnit { get; set; } = string.Empty;
        public int TypeCode { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }

    public class FittingStock
    {
        public string BatchId { get; set; } = string.Empty;
        public string FittingArticle { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
