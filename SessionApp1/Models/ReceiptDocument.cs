using System;

namespace SessionApp1.Models
{
    public class ReceiptDocument
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; } = DateTime.Now;
        public string Supplier { get; set; }
    }
}