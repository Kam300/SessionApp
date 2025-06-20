using SessionApp1.Models;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SessionApp1.Helpers
{
    public static class OrderStatusHelper
    {
        /// <summary>
        /// Получает статус заказа из строкового представления
        /// </summary>
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

        /// <summary>
        /// Получает строковое представление статуса заказа
        /// </summary>
        public static string GetStatusString(OrderStatus status)
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

        /// <summary>
        /// Получает цвет для отображения статуса заказа
        /// </summary>
        public static Brush GetStatusColor(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => new SolidColorBrush(Colors.Blue),
                OrderStatus.Waiting => new SolidColorBrush(Colors.Orange),
                OrderStatus.Processing => new SolidColorBrush(Colors.DarkOrange),
                OrderStatus.Rejected => new SolidColorBrush(Colors.Red),
                OrderStatus.WaitingForPayment => new SolidColorBrush(Colors.Purple),
                OrderStatus.Paid => new SolidColorBrush(Colors.Green),
                OrderStatus.InProduction => new SolidColorBrush(Colors.DarkCyan),
                OrderStatus.Ready => new SolidColorBrush(Colors.DarkGreen),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        /// <summary>
        /// Получает список всех статусов заказа
        /// </summary>
        public static List<KeyValuePair<OrderStatus, string>> GetAllStatuses()
        {
            return new List<KeyValuePair<OrderStatus, string>>
            {
                new KeyValuePair<OrderStatus, string>(OrderStatus.New, GetStatusString(OrderStatus.New)),
                new KeyValuePair<OrderStatus, string>(OrderStatus.Waiting, GetStatusString(OrderStatus.Waiting)),
                new KeyValuePair<OrderStatus, string>(OrderStatus.Processing, GetStatusString(OrderStatus.Processing)),
                new KeyValuePair<OrderStatus, string>(OrderStatus.Rejected, GetStatusString(OrderStatus.Rejected)),
                new KeyValuePair<OrderStatus, string>(OrderStatus.WaitingForPayment, GetStatusString(OrderStatus.WaitingForPayment)),
                new KeyValuePair<OrderStatus, string>(OrderStatus.Paid, GetStatusString(OrderStatus.Paid)),
                new KeyValuePair<OrderStatus, string>(OrderStatus.InProduction, GetStatusString(OrderStatus.InProduction)),
                new KeyValuePair<OrderStatus, string>(OrderStatus.Ready, GetStatusString(OrderStatus.Ready))
            };
        }

        /// <summary>
        /// Проверяет, может ли заказ перейти в указанный статус
        /// </summary>
        public static bool CanTransitionTo(OrderStatus currentStatus, OrderStatus newStatus)
        {
            // Проверка последовательности статусов
            return newStatus > currentStatus && (int)newStatus - (int)currentStatus <= 1;
        }
    }
}