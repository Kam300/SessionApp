namespace SessionApp1.Models
{
    public class ReceiptDocumentItem
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string MaterialArticle { get; set; }
        public string MaterialName { get; set; } // Для отображения в DataGrid
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "шт"; // НОВОЕ ПОЛЕ: Единица измерения
        public decimal Price { get; set; }
        public decimal TotalAmount => Quantity * Price; // Вычисляемое свойство
    }
}