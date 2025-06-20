using Npgsql;
using NpgsqlTypes;
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
            _connectionString = "Host=localhost;Database=postgres;Username=postgres;Password=00000000;Port=5432;Search Path=public";
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
                        Customer = reader.IsDBNull("customer") ? "" : reader.GetString("customer"),
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
                        Customer = reader.IsDBNull("customer_fullname") ?
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
                // Исправленный SQL запрос с правильными параметрами
                const string insertOrderSql = @"
            INSERT INTO orders (customer_user_id, status, order_date, customer, manager, total_amount)
            VALUES (@customerUserId, @status, @orderDate, @customer, @manager, @totalAmount)
            RETURNING id";

                using var orderCommand = new NpgsqlCommand(insertOrderSql, connection, transaction);

                // ИСПРАВЛЕНО: Правильная настройка параметров
                orderCommand.Parameters.Add(new NpgsqlParameter("@customerUserId", NpgsqlDbType.Integer)
                {
                    Value = order.CustomerUserId
                });
                orderCommand.Parameters.Add(new NpgsqlParameter("@status", NpgsqlDbType.Text)
                {
                    Value = order.Status ?? "Новый"
                });
                orderCommand.Parameters.Add(new NpgsqlParameter("@orderDate", NpgsqlDbType.Timestamp)
                {
                    Value = order.OrderDate ?? DateTime.Now
                });
                orderCommand.Parameters.Add(new NpgsqlParameter("@customer", NpgsqlDbType.Text)
                {
                    Value = order.Customer ?? ""
                });
                orderCommand.Parameters.Add(new NpgsqlParameter("@manager", NpgsqlDbType.Text)
                {
                    Value = order.Manager ?? ""
                });
                orderCommand.Parameters.Add(new NpgsqlParameter("@totalAmount", NpgsqlDbType.Numeric)
                {
                    Value = order.TotalAmount
                });

                var orderId = (int)await orderCommand.ExecuteScalarAsync();

                // Добавление позиций заказа
                foreach (var item in order.Items)
                {
                    const string insertItemSql = @"
                INSERT INTO order_items (order_id, item_article, quantity, product_name, price)
                VALUES (@orderId, @itemArticle, @quantity, @productName, @price)";

                    using var itemCommand = new NpgsqlCommand(insertItemSql, connection, transaction);
                    itemCommand.Parameters.Add(new NpgsqlParameter("@orderId", NpgsqlDbType.Integer)
                    {
                        Value = orderId
                    });
                    itemCommand.Parameters.Add(new NpgsqlParameter("@itemArticle", NpgsqlDbType.Text)
                    {
                        Value = item.ProductArticle ?? ""
                    });
                    itemCommand.Parameters.Add(new NpgsqlParameter("@quantity", NpgsqlDbType.Integer)
                    {
                        Value = item.Quantity
                    });
                    itemCommand.Parameters.Add(new NpgsqlParameter("@productName", NpgsqlDbType.Text)
                    {
                        Value = item.ProductName ?? ""
                    });
                    itemCommand.Parameters.Add(new NpgsqlParameter("@price", NpgsqlDbType.Numeric)
                    {
                        Value = item.Price
                    });

                    await itemCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return orderId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
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

        /// <summary>
        /// Получает список заказов за указанный период для страницы списка заказов
        /// </summary>
        public async Task<List<OrderListItem>> GetOrdersAsync(DateTime startDate, DateTime endDate)
        {
            var orders = new List<OrderListItem>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT o.id, o.order_date, o.status, 
                           u.fullname as customer_name, o.manager as manager_name,
                           (SELECT COUNT(*) FROM order_items WHERE order_id = o.id) as total_items
                    FROM orders o
                    LEFT JOIN users u ON o.customer_user_id = u.id
                    WHERE o.order_date BETWEEN @startDate AND @endDate
                    ORDER BY o.order_date DESC";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("startDate", startDate);
                command.Parameters.AddWithValue("endDate", endDate);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var orderId = reader.GetInt32(reader.GetOrdinal("id"));
                    var orderStatus = reader.IsDBNull(reader.GetOrdinal("status")) ? 
                        OrderStatus.New : 
                        OrderStatusHelper.GetStatusFromString(reader.GetString(reader.GetOrdinal("status")));
                    
                    var order = new OrderListItem
                    {
                        Id = orderId,
                        OrderNumber = orderId.ToString(), // Используем ID как номер заказа
                        OrderDate = reader.GetDateTime(reader.GetOrdinal("order_date")),
                        Status = orderStatus,
                        CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) ? 
                            "Не указан" : 
                            reader.GetString(reader.GetOrdinal("customer_name")),
                        ManagerName = reader.IsDBNull(reader.GetOrdinal("manager_name")) ? 
                            "Не назначен" : 
                            reader.GetString(reader.GetOrdinal("manager_name")),
                        TotalItems = reader.GetInt32(reader.GetOrdinal("total_items"))
                    };

                    orders.Add(order);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки списка заказов: {ex.Message}", ex);
            }

            return orders;
        }

        /// <summary>
        /// Получает детальную информацию о заказе по его ID для страницы списка заказов
        /// </summary>
        public async Task<OrderListItem> GetOrderByIdForListAsync(int orderId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT o.id, o.order_date, o.status, 
                           u.fullname as customer_name, o.manager as manager_name,
                           (SELECT COUNT(*) FROM order_items WHERE order_id = o.id) as total_items
                    FROM orders o
                    LEFT JOIN users u ON o.customer_user_id = u.id
                    WHERE o.id = @orderId";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("orderId", orderId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var orderStatus = reader.IsDBNull(reader.GetOrdinal("status")) ? 
                        OrderStatus.New : 
                        OrderStatusHelper.GetStatusFromString(reader.GetString(reader.GetOrdinal("status")));
                    
                    return new OrderListItem
                    {
                        Id = orderId,
                        OrderNumber = orderId.ToString(), // Используем ID как номер заказа
                        OrderDate = reader.GetDateTime(reader.GetOrdinal("order_date")),
                        Status = orderStatus,
                        CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) ? 
                            "Не указан" : 
                            reader.GetString(reader.GetOrdinal("customer_name")),
                        ManagerName = reader.IsDBNull(reader.GetOrdinal("manager_name")) ? 
                            "Не назначен" : 
                            reader.GetString(reader.GetOrdinal("manager_name")),
                        TotalItems = reader.GetInt32(reader.GetOrdinal("total_items"))
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки информации о заказе: {ex.Message}", ex);
            }

            return null;
        }
    }
}
