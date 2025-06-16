namespace SessionApp1.Models
{
    public class FabricStockInfo
    {
        public string RollId { get; set; } = string.Empty;
        public string FabricArticle { get; set; } = string.Empty;
        public string FabricName { get; set; } = string.Empty;
        public int LengthMm { get; set; }
        public int WidthMm { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal AreaSqm { get; set; }
        public decimal LengthM { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class FittingStockInfo
    {
        public string BatchId { get; set; } = string.Empty;
        public string FittingArticle { get; set; } = string.Empty;
        public string FittingName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal WeightValue { get; set; }
        public string WeightUnit { get; set; } = string.Empty;
        public decimal TotalWeight { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class MaterialReceipt
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public bool IsProcessed { get; set; }
        public List<MaterialReceiptItem> Items { get; set; } = new List<MaterialReceiptItem>();
    }

    public class MaterialReceiptItem
    {
        public int Id { get; set; }
        public int ReceiptId { get; set; }
        public string MaterialArticle { get; set; } = string.Empty;
        public string MaterialType { get; set; } = string.Empty; // "fabric" или "fitting"
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
    }

    public class ProductSpecification
    {
        public int Id { get; set; }
        public string ProductArticle { get; set; } = string.Empty;
        public string MaterialArticle { get; set; } = string.Empty;
        public string MaterialType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
