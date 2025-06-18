using Npgsql;
using SessionApp1.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SessionApp1.Services
{
    public class OrderService
    {
        private readonly string _connectionString;

        public OrderService()
        {
            _connectionString = "Host=localhost;Database=ff;Username=postgres;Password=00000000;Port=5432;Search Path=public";
        }

        // Получить заказы конкретного заказчика
        public async Task<List<Order>> GetOrdersByCustomerAsync(int customerUserId)
        {
            var orders = new List<Order>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    SELECT id, customer_user_id, status, order_date, customer, manager, total_amount
                    FROM orders 
                    WHERE customer_user_id = @userId 
                    ORDER BY order_date DESC", connection);
                cmd.Parameters.AddWithValue("userId", customerUserId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    orders.Add(new Order
                    {
                        Id = reader.GetInt32("id"),
                        CustomerUserId = reader.GetInt32("customer_user_id"),
                        Status = reader.IsDBNull("status") ? "Новый" : reader.GetString("status"),
                        OrderDate = reader.GetDateTime("order_date"),
                        CustomerName = reader.IsDBNull("customer") ? "" : reader.GetString("customer"),
                        Manager = reader.IsDBNull("manager") ? "" : reader.GetString("manager"),
                        Items = new List<OrderItem>()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки заказов: {ex.Message}", ex);
            }

            // Загружаем позиции для каждого заказа
            await LoadOrderItemsAsync(orders);
            return orders;
        }

        // Получить все заказы (для менеджера)
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    SELECT o.id, o.customer_user_id, o.status, o.order_date, o.customer, o.manager, o.total_amount,
                           u.fullname as customer_fullname
                    FROM orders o
                    LEFT JOIN users u ON o.customer_user_id = u.id
                    ORDER BY o.order_date DESC", connection);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    orders.Add(new Order
                    {
                        Id = reader.GetInt32("id"),
                        CustomerUserId = reader.IsDBNull("customer_user_id") ? 0 : reader.GetInt32("customer_user_id"),
                        Status = reader.IsDBNull("status") ? "Новый" : reader.GetString("status"),
                        OrderDate = reader.GetDateTime("order_date"),
                        CustomerName = reader.IsDBNull("customer_fullname") ?
                            (reader.IsDBNull("customer") ? "" : reader.GetString("customer")) :
                            reader.GetString("customer_fullname"),
                        Manager = reader.IsDBNull("manager") ? "" : reader.GetString("manager"),
                        Items = new List<OrderItem>()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки заказов: {ex.Message}", ex);
            }

            await LoadOrderItemsAsync(orders);
            return orders;
        }

        // Создать новый заказ
        public async Task<int> CreateOrderAsync(Order order)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Создаем заголовок заказа
                var cmd = new NpgsqlCommand(@"
                    INSERT INTO orders (customer_user_id, status, order_date, customer, manager, total_amount) 
                    VALUES (@userId, @status, @date, @customer, @manager, @total) 
                    RETURNING id", connection, transaction);

                cmd.Parameters.AddWithValue("userId", order.CustomerUserId);
                cmd.Parameters.AddWithValue("status", order.Status);
                cmd.Parameters.AddWithValue("date", order.OrderDate);
                cmd.Parameters.AddWithValue("customer", order.CustomerName);
                cmd.Parameters.AddWithValue("manager", (object)order.Manager ?? DBNull.Value);
                cmd.Parameters.AddWithValue("total", order.TotalAmount);

                var orderId = (int)await cmd.ExecuteScalarAsync();

                // Добавляем позиции заказа
                foreach (var item in order.Items)
                {
                    var itemCmd = new NpgsqlCommand(@"
                        INSERT INTO order_items (order_id, item_article, quantity, product_name, price) 
                        VALUES (@orderId, @article, @qty, @name, @price)", connection, transaction);

                    itemCmd.Parameters.AddWithValue("orderId", orderId);
                    itemCmd.Parameters.AddWithValue("article", item.ProductArticle);
                    itemCmd.Parameters.AddWithValue("qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("name", item.ProductName);
                    itemCmd.Parameters.AddWithValue("price", item.Price);
                    await itemCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return orderId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Ошибка создания заказа: {ex.Message}", ex);
            }
        }

        // Обновить статус заказа
        public async Task UpdateOrderStatusAsync(int orderId, string status, string manager = null)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    UPDATE orders 
                    SET status = @status, manager = COALESCE(@manager, manager)
                    WHERE id = @orderId", connection);

                cmd.Parameters.AddWithValue("orderId", orderId);
                cmd.Parameters.AddWithValue("status", status);
                cmd.Parameters.AddWithValue("manager", (object)manager ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обновления статуса заказа: {ex.Message}", ex);
            }
        }

        // Вспомогательный метод для загрузки позиций заказов
        private async Task LoadOrderItemsAsync(List<Order> orders)
        {
            if (!orders.Any()) return;

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                foreach (var order in orders)
                {
                    var itemCmd = new NpgsqlCommand(@"
                        SELECT id, item_article, quantity, product_name, price
                        FROM order_items 
                        WHERE order_id = @orderId", connection);
                    itemCmd.Parameters.AddWithValue("orderId", order.Id);

                    using var itemReader = await itemCmd.ExecuteReaderAsync();
                    while (await itemReader.ReadAsync())
                    {
                        order.Items.Add(new OrderItem
                        {
                            Id = itemReader.GetInt32("id"),
                            OrderId = order.Id,
                            ProductArticle = itemReader.GetString("item_article"),
                            Quantity = itemReader.GetInt32("quantity"),
                            ProductName = itemReader.IsDBNull("product_name") ? "" : itemReader.GetString("product_name"),
                            Price = itemReader.IsDBNull("price") ? 0 : itemReader.GetDecimal("price")
                        });
                    }
                    itemReader.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки позиций заказов: {ex.Message}");
            }
        }
    }
}
