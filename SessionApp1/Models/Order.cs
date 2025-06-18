namespace SessionApp1.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int? OrderNumber { get; set; }
        public string Stage { get; set; } = "";
        public DateTime? OrderDate { get; set; }
        public string Customer { get; set; } = "";
        public string Manager { get; set; } = "";
        public decimal TotalAmount { get; set; }

        // ДОБАВЬТЕ ЭТИ СВОЙСТВА:
        public int CustomerUserId { get; set; }

        public string CustomerName { get; set; }
        public string Status { get; set; } = "Новый";
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}