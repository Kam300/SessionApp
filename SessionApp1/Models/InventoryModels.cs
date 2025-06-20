using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SessionApp1.Models
{
    // Модель документа инвентаризации
    public class InventoryDocument : INotifyPropertyChanged
    {
        private string _documentNumber;
        private DateTime _documentDate;
        private string _warehouseKeeper;
        private decimal _totalAccountingAmount;
        private decimal _totalActualAmount;
        private decimal _differenceAmount;
        private decimal _differencePercent;
        private bool _isApproved;
        private string _approvedBy;
        private DateTime? _approvedDate;
        private bool _isProcessed;
        private string _createdBy;
        private DateTime _createdDate;

        public int Id { get; set; }

        public string DocumentNumber
        {
            get => _documentNumber;
            set
            {
                _documentNumber = value;
                OnPropertyChanged();
            }
        }

        public DateTime DocumentDate
        {
            get => _documentDate;
            set
            {
                _documentDate = value;
                OnPropertyChanged();
            }
        }

        public string WarehouseKeeper
        {
            get => _warehouseKeeper;
            set
            {
                _warehouseKeeper = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalAccountingAmount
        {
            get => _totalAccountingAmount;
            set
            {
                _totalAccountingAmount = value;
                OnPropertyChanged();
                CalculateDifference();
            }
        }

        public decimal TotalActualAmount
        {
            get => _totalActualAmount;
            set
            {
                _totalActualAmount = value;
                OnPropertyChanged();
                CalculateDifference();
            }
        }

        public decimal DifferenceAmount
        {
            get => _differenceAmount;
            set
            {
                _differenceAmount = value;
                OnPropertyChanged();
            }
        }

        public decimal DifferencePercent
        {
            get => _differencePercent;
            set
            {
                _differencePercent = value;
                OnPropertyChanged();
            }
        }

        public bool IsApproved
        {
            get => _isApproved;
            set
            {
                _isApproved = value;
                OnPropertyChanged();
            }
        }

        public string ApprovedBy
        {
            get => _approvedBy;
            set
            {
                _approvedBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime? ApprovedDate
        {
            get => _approvedDate;
            set
            {
                _approvedDate = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessed
        {
            get => _isProcessed;
            set
            {
                _isProcessed = value;
                OnPropertyChanged();
            }
        }

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                _createdBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<InventoryDocumentItem> Items { get; set; } = new ObservableCollection<InventoryDocumentItem>();

        private void CalculateDifference()
        {
            DifferenceAmount = TotalActualAmount - TotalAccountingAmount;
            
            if (TotalAccountingAmount != 0)
            {
                DifferencePercent = Math.Abs(DifferenceAmount / TotalAccountingAmount * 100);
            }
            else
            {
                DifferencePercent = 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Модель позиции документа инвентаризации
    public class InventoryDocumentItem : INotifyPropertyChanged
    {
        private string _materialArticle;
        private string _materialName;
        private string _materialType;
        private decimal _accountingQuantity;
        private decimal _actualQuantity;
        private decimal _differenceQuantity;
        private string _unit;
        private decimal _price;
        private decimal _accountingAmount;
        private decimal _actualAmount;
        private decimal _differenceAmount;

        public int Id { get; set; }
        public int DocumentId { get; set; }

        public string MaterialArticle
        {
            get => _materialArticle;
            set
            {
                _materialArticle = value;
                OnPropertyChanged();
            }
        }

        public string MaterialName
        {
            get => _materialName;
            set
            {
                _materialName = value;
                OnPropertyChanged();
            }
        }

        public string MaterialType
        {
            get => _materialType;
            set
            {
                _materialType = value;
                OnPropertyChanged();
            }
        }

        public decimal AccountingQuantity
        {
            get => _accountingQuantity;
            set
            {
                _accountingQuantity = value;
                OnPropertyChanged();
                CalculateAmounts();
            }
        }

        public decimal ActualQuantity
        {
            get => _actualQuantity;
            set
            {
                _actualQuantity = value;
                OnPropertyChanged();
                CalculateAmounts();
            }
        }

        public decimal DifferenceQuantity
        {
            get => _differenceQuantity;
            set
            {
                _differenceQuantity = value;
                OnPropertyChanged();
            }
        }

        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                OnPropertyChanged();
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
                CalculateAmounts();
            }
        }

        public decimal AccountingAmount
        {
            get => _accountingAmount;
            set
            {
                _accountingAmount = value;
                OnPropertyChanged();
            }
        }

        public decimal ActualAmount
        {
            get => _actualAmount;
            set
            {
                _actualAmount = value;
                OnPropertyChanged();
            }
        }

        public decimal DifferenceAmount
        {
            get => _differenceAmount;
            set
            {
                _differenceAmount = value;
                OnPropertyChanged();
            }
        }

        private void CalculateAmounts()
        {
            AccountingAmount = AccountingQuantity * Price;
            ActualAmount = ActualQuantity * Price;
            DifferenceQuantity = ActualQuantity - AccountingQuantity;
            DifferenceAmount = ActualAmount - AccountingAmount;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Модель для отчета по остаткам материалов
    public class MaterialStockReport
    {
        public string Article { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
    }

    // Модель для отчета по движению материалов
    public class MaterialMovementReport
    {
        public string Article { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public decimal InitialQuantity { get; set; }
        public decimal InitialAmount { get; set; }
        public decimal ReceiptQuantity { get; set; }
        public decimal ReceiptAmount { get; set; }
        public decimal ExpenseQuantity { get; set; }
        public decimal ExpenseAmount { get; set; }
        public decimal FinalQuantity { get; set; }
        public decimal FinalAmount { get; set; }
    }

    // Модель для списка заказов
    public class OrderListItem
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public int TotalItems { get; set; }
        public OrderStatus Status { get; set; }
        public string CustomerName { get; set; }
        public string ManagerName { get; set; }

        // Вычисляемые свойства для отображения
        public string StatusName => OrderStatusHelper.GetStatusName(Status);
    }

    // Перечисление статусов заказа
    public enum OrderStatus
    {
        New = 0,           // Новый
        Waiting = 1,       // Ожидает
        Processing = 2,    // Обработка
        Rejected = 3,      // Отклонен
        WaitingForPayment = 4, // К оплате
        Paid = 5,          // Оплачен
        InProduction = 6,  // Раскрой
        Ready = 7          // Готов
    }

    // Класс для работы со статусами заказов
    public static class OrderStatusHelper
    {
        public static string GetStatusName(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "Новый",
                OrderStatus.Waiting => "Ожидает",
                OrderStatus.Processing => "Обработка",
                OrderStatus.Rejected => "Отклонен",
                OrderStatus.WaitingForPayment => "К оплате",
                OrderStatus.Paid => "Оплачен",
                OrderStatus.InProduction => "Раскрой",
                OrderStatus.Ready => "Готов",
                _ => "Неизвестный"
            };
        }

        public static OrderStatus GetNextStatus(OrderStatus currentStatus)
        {
            return currentStatus switch
            {
                OrderStatus.New => OrderStatus.Waiting,
                OrderStatus.Waiting => OrderStatus.Processing,
                OrderStatus.Processing => OrderStatus.WaitingForPayment, // Или Rejected
                OrderStatus.WaitingForPayment => OrderStatus.Paid,
                OrderStatus.Paid => OrderStatus.InProduction,
                OrderStatus.InProduction => OrderStatus.Ready,
                _ => currentStatus
            };
        }

        public static OrderStatus GetStatusFromString(string statusString)
        {
            return statusString?.ToLower() switch
            {
                "новый" => OrderStatus.New,
                "ожидает" => OrderStatus.Waiting,
                "обработка" => OrderStatus.Processing,
                "отклонен" => OrderStatus.Rejected,
                "к оплате" => OrderStatus.WaitingForPayment,
                "оплачен" => OrderStatus.Paid,
                "раскрой" => OrderStatus.InProduction,
                "готов" => OrderStatus.Ready,
                _ => OrderStatus.New
            };
        }
    }
}