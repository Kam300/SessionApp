using Npgsql;
using SessionApp1.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SessionApp1.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = "Host=localhost;Database=postgres;Username=postgres;Password=00000000;Port=5432";
        }

        public async Task<User?> AuthenticateUserAsync(string login, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Простой запрос без функций
                using var command = new NpgsqlCommand(@"
                    SELECT u.id, u.roleid, u.fullname, u.login, r.name as rolename
                    FROM users u
                    JOIN roles r ON u.roleid = r.id
                    WHERE u.login = @login 
                    AND u.passwordhash = crypt(@password, u.passwordhash)", connection);

                command.Parameters.AddWithValue("login", login);
                command.Parameters.AddWithValue("password", password);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        Id = reader.GetInt32("id"),
                        RoleId = reader.GetInt32("roleid"),
                        FullName = reader.GetString("fullname"),
                        Login = reader.GetString("login"),
                        RoleName = reader.GetString("rolename")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка аутентификации: {ex.Message}", ex);
            }
        }

        public async Task<bool> RegisterUserAsync(string fullName, string login, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Проверяем существование пользователя
                using var checkCommand =
                    new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE login = @login", connection);
                checkCommand.Parameters.AddWithValue("login", login);
                var userExists = (long)await checkCommand.ExecuteScalarAsync() > 0;

                if (userExists)
                    return false;

                // Получаем ID роли заказчика
                using var roleCommand = new NpgsqlCommand("SELECT id FROM roles WHERE name = 'Заказчик'", connection);
                var customerRoleId = await roleCommand.ExecuteScalarAsync();

                if (customerRoleId == null)
                    return false;

                // Создаем пользователя
                using var insertCommand = new NpgsqlCommand(@"
                    INSERT INTO users (roleid, fullname, login, passwordhash)
                    VALUES (@roleid, @fullname, @login, crypt(@password, gen_salt('bf')))", connection);

                insertCommand.Parameters.AddWithValue("roleid", customerRoleId);
                insertCommand.Parameters.AddWithValue("fullname", fullName);
                insertCommand.Parameters.AddWithValue("login", login);
                insertCommand.Parameters.AddWithValue("password", password);

                await insertCommand.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка регистрации: {ex.Message}", ex);
            }
        }

        public async Task<List<Fabric>> GetFabricsAsync()
        {
            var fabrics = new List<Fabric>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    SELECT 
                        f.article,
                        f.name_code,
                        f.color_code,
                        f.pattern_code,
                        f.image_path,
                        f.composition_code,
                        f.width_mm,
                        f.length_mm,
                        f.unit,
                        f.price,
                        COALESCE(fn.name, '') as fabricname,
                        COALESCE(c.name, '') as colorname,
                        COALESCE(p.name, '') as patternname,
                        COALESCE(comp.name, '') as compositionname
                    FROM fabrics f
                    LEFT JOIN lookup_fabric_names fn ON f.name_code = fn.id
                    LEFT JOIN lookup_colors c ON f.color_code = c.id
                    LEFT JOIN lookup_patterns p ON f.pattern_code = p.id
                    LEFT JOIN lookup_compositions comp ON f.composition_code = comp.id
                    ORDER BY f.article", connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    fabrics.Add(new Fabric
                    {
                        Article = reader.GetString("article"),
                        NameCode = reader.GetInt32("name_code"),
                        ColorCode = reader.GetInt32("color_code"),
                        PatternCode = reader.GetInt32("pattern_code"),
                        ImagePath = reader.GetString("image_path"),
                        CompositionCode = reader.GetInt32("composition_code"),
                        WidthMm = reader.GetInt32("width_mm"),
                        LengthMm = reader.GetInt32("length_mm"),
                        Unit = reader.GetString("unit"),
                        Price = reader.GetDecimal("price"),
                        FabricName = reader.IsDBNull("fabricname") ? "" : reader.GetString("fabricname"),
                        ColorName = reader.IsDBNull("colorname") ? "" : reader.GetString("colorname"),
                        PatternName = reader.IsDBNull("patternname") ? "" : reader.GetString("patternname"),
                        CompositionName = reader.IsDBNull("compositionname") ? "" : reader.GetString("compositionname")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки тканей: {ex.Message}", ex);
            }

            return fabrics;
        }

        public async Task<List<Fitting>> GetFittingsAsync()
        {
            var fittings = new List<Fitting>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
            SELECT 
                f.article,
                f.name,
                f.width_mm,
                f.length_mm,
                COALESCE(f.dimension_unit, 'мм') as dimension_unit,
                f.weight_value,
                COALESCE(f.weight_unit, 'г') as weight_unit,
                f.type_code,
                f.image_path,
                f.price,
                COALESCE(ft.name, '') as typename
            FROM fittings f
            LEFT JOIN lookup_fitting_types ft ON f.type_code = ft.id
            ORDER BY f.article", connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    fittings.Add(new Fitting
                    {
                        Article = reader.GetString("article"),
                        Name = reader.GetString("name"),
                        WidthMm = reader.GetDecimal("width_mm"),
                        LengthMm = reader.GetDecimal("length_mm"),
                        DimensionUnit = reader.GetString("dimension_unit"),
                        WeightValue = reader.GetDecimal("weight_value"),
                        WeightUnit = reader.GetString("weight_unit"),
                        TypeCode = reader.GetInt32("type_code"),
                        ImagePath = reader.GetString("image_path"), // Используем путь как есть из БД
                        Price = reader.GetDecimal("price"),
                        TypeName = reader.GetString("typename")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки фурнитуры: {ex.Message}", ex);
            }

            return fittings;
        }


        public async Task<List<ManufacturedGood>> GetManufacturedGoodsAsync()
        {
            var goods = new List<ManufacturedGood>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Используем COALESCE для обработки NULL значений
                using var command = new NpgsqlCommand(@"
            SELECT 
                article,
                name,
                width_mm,
                length_mm,
                unit,
                price,
                image_path,
                COALESCE(comment, '') as comment
            FROM manufactured_goods 
            ORDER BY article", connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    goods.Add(new ManufacturedGood
                    {
                        Article = reader.GetString("article"),
                        Name = reader.GetString("name"),
                        WidthMm = reader.GetInt32("width_mm"),
                        LengthMm = reader.GetInt32("length_mm"),
                        Unit = reader.GetString("unit"),
                        Price = reader.GetDecimal("price"),
                        ImagePath = reader.GetString("image_path"),
                        Comment = reader.GetString("comment") // Теперь всегда будет строка, не NULL
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки изделий: {ex.Message}", ex);
            }

            return goods;
        }

        // НОВЫЙ МЕТОД: Для загрузки остатков ткани из таблицы fabric_stock
        public async Task<List<FabricStock>> GetFabricStockAsync()
        {
            var stock = new List<FabricStock>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Простой запрос к таблице остатков ткани
                using var command = new NpgsqlCommand(@"
                    SELECT roll_id, fabric_article, length_mm, width_mm, unit 
                    FROM fabric_stock", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    stock.Add(new FabricStock
                    {
                        RollId = reader.GetString("roll_id"),
                        FabricArticle = reader.GetString("fabric_article"),
                        LengthMm = reader.GetInt32("length_mm"),
                        WidthMm = reader.GetInt32("width_mm"),
                        Unit = reader.GetString("unit")
                    });
                }
            }
            catch (Exception ex)
            {
                // Это сообщение об ошибке, которое вы видели
                throw new Exception($"Ошибка загрузки остатков тканей: {ex.Message}", ex);
            }

            return stock;
        }

        // НОВЫЙ МЕТОД: Для загрузки остатков фурнитуры из таблицы fitting_stock
        public async Task<List<FittingStock>> GetFittingStockAsync()
        {
            var stock = new List<FittingStock>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Простой запрос к таблице остатков фурнитуры
                using var command = new NpgsqlCommand(@"
                    SELECT batch_id, fitting_article, quantity 
                    FROM fitting_stock", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    stock.Add(new FittingStock
                    {
                        BatchId = reader.GetString("batch_id"),
                        FittingArticle = reader.GetString("fitting_article"),
                        Quantity = reader.GetInt32("quantity")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки остатков фурнитуры: {ex.Message}", ex);
            }

            return stock;
        }


        // Метод для получения списка всех материалов (тканей и фурнитуры)


        // Метод для сохранения документа поступления и обновления остатков
        public async Task SaveReceiptDocumentAsync(ReceiptDocument document, List<ReceiptDocumentItem> items)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 1. Сохраняем заголовок документа
                var docCommand = new NpgsqlCommand(
                    "INSERT INTO receipt_documents (document_number, document_date, supplier) VALUES (@number, @date, @supplier) RETURNING id",
                    connection, transaction);
                docCommand.Parameters.AddWithValue("number", document.DocumentNumber);
                docCommand.Parameters.AddWithValue("date", document.DocumentDate);
                docCommand.Parameters.AddWithValue("supplier", (object)document.Supplier ?? DBNull.Value);
                var documentId = (int)await docCommand.ExecuteScalarAsync();

                System.Diagnostics.Debug.WriteLine($"Создан документ с ID: {documentId}");

                // 2. Обрабатываем каждую позицию документа
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];

                    // ИСПРАВЛЕНО: Убираем поле unit из INSERT запроса
                    var itemCommand = new NpgsqlCommand(
                        "INSERT INTO receipt_document_items (document_id, material_article, quantity, price) VALUES (@doc_id, @article, @qty, @price)",
                        connection, transaction);
                    itemCommand.Parameters.AddWithValue("doc_id", documentId);
                    itemCommand.Parameters.AddWithValue("article", item.MaterialArticle);
                    itemCommand.Parameters.AddWithValue("qty", item.Quantity);
                    itemCommand.Parameters.AddWithValue("price", item.Price);

                    await itemCommand.ExecuteNonQueryAsync();

                    // 3. Обновляем остатки (логика остается прежней)
                    await UpdateMaterialStockAsync(connection, transaction, item, documentId, i);
                }

                await transaction.CommitAsync();
                System.Diagnostics.Debug.WriteLine("Транзакция успешно завершена");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении документа: {ex}");
                throw new Exception($"Ошибка при проведении документа: {ex.Message}", ex);
            }
        }


        // НОВЫЙ ВСПОМОГАТЕЛЬНЫЙ МЕТОД для обновления остатков
        private async Task UpdateMaterialStockAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
            ReceiptDocumentItem item, int documentId, int itemIndex)
        {
            // Проверяем, является ли материал тканью
            var fabricCheckCmd = new NpgsqlCommand(
                "SELECT article, width_mm, unit FROM fabrics WHERE article = @article",
                connection, transaction);
            fabricCheckCmd.Parameters.AddWithValue("article", item.MaterialArticle);

            using var fabricReader = await fabricCheckCmd.ExecuteReaderAsync();
            bool isFabric = await fabricReader.ReadAsync();

            if (isFabric)
            {
                // ЭТО ТКАНЬ - добавляем новый рулон
                var width = fabricReader.GetInt32("width_mm");
                var unit = fabricReader.GetString("unit");
                fabricReader.Close();

                var rollId = $"ПРИХ-{documentId}-{itemIndex + 1}";
                var lengthMm = (int)(item.Quantity * 1000); // Преобразуем метры в мм

                var fabricStockCmd = new NpgsqlCommand(@"
            INSERT INTO fabric_stock (roll_id, fabric_article, length_mm, width_mm, unit) 
            VALUES (@roll_id, @article, @length, @width, @unit)",
                    connection, transaction);

                fabricStockCmd.Parameters.AddWithValue("roll_id", rollId);
                fabricStockCmd.Parameters.AddWithValue("article", item.MaterialArticle);
                fabricStockCmd.Parameters.AddWithValue("length", lengthMm);
                fabricStockCmd.Parameters.AddWithValue("width", width);
                fabricStockCmd.Parameters.AddWithValue("unit", unit);

                await fabricStockCmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"Добавлен рулон ткани: {rollId}, длина: {lengthMm}мм");
            }
            else
            {
                fabricReader.Close();

                // Проверяем, является ли материал фурнитурой
                var fittingCheckCmd = new NpgsqlCommand(
                    "SELECT article FROM fittings WHERE article = @article",
                    connection, transaction);
                fittingCheckCmd.Parameters.AddWithValue("article", item.MaterialArticle);

                using var fittingReader = await fittingCheckCmd.ExecuteReaderAsync();
                bool isFitting = await fittingReader.ReadAsync();
                fittingReader.Close();

                if (isFitting)
                {
                    // ЭТО ФУРНИТУРА - обновляем количество
                    var batchId = $"ПРИХ-{documentId}";
                    var quantity = (int)item.Quantity;

                    // ИСПРАВЛЕНО: Правильная логика INSERT или UPDATE
                    var fittingStockCmd = new NpgsqlCommand(@"
                INSERT INTO fitting_stock (batch_id, fitting_article, quantity) 
                VALUES (@batch_id, @article, @qty)
                ON CONFLICT (batch_id, fitting_article) 
                DO UPDATE SET quantity = fitting_stock.quantity + EXCLUDED.quantity",
                        connection, transaction);

                    fittingStockCmd.Parameters.AddWithValue("batch_id", batchId);
                    fittingStockCmd.Parameters.AddWithValue("article", item.MaterialArticle);
                    fittingStockCmd.Parameters.AddWithValue("qty", quantity);

                    await fittingStockCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"Обновлена фурнитура: {item.MaterialArticle}, +{quantity} шт");
                }
                else
                {
                    throw new Exception($"Материал '{item.MaterialArticle}' не найден ни в тканях, ни в фурнитуре!");
                }
            }
        }
        // Замените метод GetAllMaterialsAsync в DatabaseService.cs на этот:

        public async Task<Dictionary<string, MaterialInfo>> GetAllMaterialsWithUnitsAsync()
        {
            var materials = new Dictionary<string, MaterialInfo>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Загружаем ткани с их единицами измерения
                using (var cmd = new NpgsqlCommand(@"
            SELECT DISTINCT f.article, COALESCE(fn.name, f.article) as name, f.unit
            FROM fabrics f 
            LEFT JOIN lookup_fabric_names fn ON f.name_code = fn.id 
            WHERE f.article IS NOT NULL AND f.article != ''
            ORDER BY f.article", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var article = reader.GetString("article");
                        var name = reader.GetString("name");
                        var unit = reader.IsDBNull("unit") ? "м" : reader.GetString("unit");

                        materials[article] = new MaterialInfo
                        {
                            Name = $"[ТКАНЬ] {name} ({article})",
                            Unit = unit
                        };
                    }
                }

                // Загружаем фурнитуру с их единицами измерения
                using (var cmd = new NpgsqlCommand(@"
            SELECT DISTINCT article, name, dimension_unit 
            FROM fittings 
            WHERE article IS NOT NULL AND article != ''
            ORDER BY article", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var article = reader.GetString("article");
                        var name = reader.GetString("name");
                        var unit = reader.IsDBNull("dimension_unit") ? "шт" : reader.GetString("dimension_unit");

                        materials[article] = new MaterialInfo
                        {
                            Name = $"[ФУРНИТУРА] {name} ({article})",
                            Unit = unit
                        };
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Загружено материалов: {materials.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки материалов: {ex.Message}");
                throw new Exception($"Ошибка загрузки списка материалов: {ex.Message}", ex);
            }

            return materials;
        }

        // Добавьте также старый метод для совместимости
        public async Task<Dictionary<string, string>> GetAllMaterialsAsync()
        {
            var materialsWithUnits = await GetAllMaterialsWithUnitsAsync();
            return materialsWithUnits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name);
        }

        public async Task<int> CreateOrderAsync(Order order)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
            INSERT INTO orders (order_number, stage, order_date, customer, manager, total_amount)
            VALUES (@order_number, @stage, @order_date, @customer, @manager, @total_amount)
            RETURNING id", connection);

                command.Parameters.AddWithValue("order_number", (object)order.OrderNumber ?? DBNull.Value);
                command.Parameters.AddWithValue("stage", (object)order.Stage ?? DBNull.Value);
                command.Parameters.AddWithValue("order_date", (object)order.OrderDate ?? DBNull.Value);
                command.Parameters.AddWithValue("customer", (object)order.Customer ?? DBNull.Value);
                command.Parameters.AddWithValue("manager", (object)order.Manager ?? DBNull.Value);
                command.Parameters.AddWithValue("total_amount", order.TotalAmount);

                var orderId = (int)await command.ExecuteScalarAsync();
                return orderId;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания заказа: {ex.Message}", ex);
            }
        }




    }


}
