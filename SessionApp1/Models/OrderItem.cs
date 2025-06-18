namespace SessionApp1.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ItemArticle { get; set; } = "";
        public int Quantity { get; set; }
        public string ProductArticle { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public decimal TotalPrice => Quantity * Price;
    }
}