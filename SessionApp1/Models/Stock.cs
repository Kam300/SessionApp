using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionApp1.Models
{
    public class FabricStock
    {
        public string RollId { get; set; } = string.Empty;
        public string FabricArticle { get; set; } = string.Empty;
        public int LengthMm { get; set; }
        public int WidthMm { get; set; }
        public string Unit { get; set; } = string.Empty;
        
        // Свойства для совместимости с InventoryService.cs
        public string Article { get => FabricArticle; set => FabricArticle = value; }
        public decimal Quantity { get => LengthMm; set => LengthMm = (int)value; }
    }

    public class FittingStock
    {
        public string BatchId { get; set; } = string.Empty;
        public string FittingArticle { get; set; } = string.Empty;
        public int Quantity { get; set; }
        
        // Свойства для совместимости с InventoryService.cs
        public string Article { get => FittingArticle; set => FittingArticle = value; }
    }
}