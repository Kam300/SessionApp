namespace SessionApp1.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int? OrderNumber { get; set; }
        public string Stage { get; set; } = "";
        public DateTime? OrderDate { get; set; } = DateTime.Now;
        public string Customer { get; set; } = "";
        public string CustomerName { get; set; } = ""; // ДОБАВЬТЕ ЭТО СВОЙСТВО
        public string Manager { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int CustomerUserId { get; set; }
        public string Status { get; set; } = "Новый";
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}