using Npgsql;
using SessionApp1.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SessionApp1.Models;

namespace SessionApp1.Services
{
    public class InventoryService
    {
        private readonly DatabaseService _databaseService;
        private readonly string _connectionString;

        public InventoryService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _connectionString = databaseService.ConnectionString;
        }

        // Получение текущих остатков материалов для инвентаризации
        public async Task<Dictionary<string, InventoryDocumentItem>> GetCurrentStockForInventoryAsync()
        {
            var result = new Dictionary<string, InventoryDocumentItem>();

            try
            {
                // Получаем остатки тканей
                var fabricStock = await _databaseService.GetFabricStockAsync();
                var fabrics = await _databaseService.GetFabricsAsync();

                foreach (var stock in fabricStock)
                {
                    var fabric = fabrics.FirstOrDefault(f => f.Article == stock.Article);
                    if (fabric != null)
                    {
                        var item = new InventoryDocumentItem
                        {
                            MaterialArticle = stock.Article,
                            MaterialName = fabric.FabricName,
                            MaterialType = "fabric",
                            AccountingQuantity = stock.Quantity,
                            ActualQuantity = 0, // Будет заполнено при инвентаризации
                            Unit = fabric.Unit,
                            Price = fabric.Price,
                            AccountingAmount = stock.Quantity * fabric.Price
                        };

                        result[stock.Article] = item;
                    }
                }

                // Получаем остатки фурнитуры
                var fittingStock = await _databaseService.GetFittingStockAsync();
                var fittings = await _databaseService.GetFittingsAsync();

                foreach (var stock in fittingStock)
                {
                    var fitting = fittings.FirstOrDefault(f => f.Article == stock.Article);
                    if (fitting != null)
                    {
                        var item = new InventoryDocumentItem
                        {
                            MaterialArticle = stock.Article,
                            MaterialName = fitting.Name,
                            MaterialType = "fitting",
                            AccountingQuantity = stock.Quantity,
                            ActualQuantity = 0, // Будет заполнено при инвентаризации
                            Unit = "шт", // Для фурнитуры обычно используется штуки
                            Price = fitting.Price,
                            AccountingAmount = stock.Quantity * fitting.Price
                        };

                        result[stock.Article] = item;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения текущих остатков для инвентаризации: {ex.Message}", ex);
            }
        }

        // Сохранение документа инвентаризации
        public async Task<int> SaveInventoryDocumentAsync(InventoryDocument document)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Начинаем транзакцию
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Сохраняем документ
                    int documentId;
                    using (var command = new NpgsqlCommand(@"
                        INSERT INTO inventory_documents (
                            document_number, document_date, warehouse_keeper, 
                            total_accounting_amount, total_actual_amount, difference_amount, difference_percent,
                            is_approved, is_processed, created_by, created_date)
                        VALUES (
                            @document_number, @document_date, @warehouse_keeper, 
                            @total_accounting_amount, @total_actual_amount, @difference_amount, @difference_percent,
                            @is_approved, @is_processed, @created_by, @created_date)
                        RETURNING id", connection, transaction))
                    {
                        command.Parameters.AddWithValue("document_number", document.DocumentNumber);
                        command.Parameters.AddWithValue("document_date", document.DocumentDate);
                        command.Parameters.AddWithValue("warehouse_keeper", document.WarehouseKeeper);
                        command.Parameters.AddWithValue("total_accounting_amount", document.TotalAccountingAmount);
                        command.Parameters.AddWithValue("total_actual_amount", document.TotalActualAmount);
                        command.Parameters.AddWithValue("difference_amount", document.DifferenceAmount);
                        command.Parameters.AddWithValue("difference_percent", document.DifferencePercent);
                        command.Parameters.AddWithValue("is_approved", document.IsApproved);
                        command.Parameters.AddWithValue("is_processed", document.IsProcessed);
                        command.Parameters.AddWithValue("created_by", document.CreatedBy);
                        command.Parameters.AddWithValue("created_date", document.CreatedDate);

                        documentId = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }

                    // Сохраняем позиции документа
                    foreach (var item in document.Items)
                    {
                        using var itemCommand = new NpgsqlCommand(@"
                            INSERT INTO inventory_document_items (
                                document_id, material_article, material_type, 
                                accounting_quantity, actual_quantity, difference_quantity, unit,
                                price, accounting_amount, actual_amount, difference_amount)
                            VALUES (
                                @document_id, @material_article, @material_type, 
                                @accounting_quantity, @actual_quantity, @difference_quantity, @unit,
                                @price, @accounting_amount, @actual_amount, @difference_amount)", connection, transaction);

                        itemCommand.Parameters.AddWithValue("document_id", documentId);
                        itemCommand.Parameters.AddWithValue("material_article", item.MaterialArticle);
                        itemCommand.Parameters.AddWithValue("material_type", item.MaterialType);
                        itemCommand.Parameters.AddWithValue("accounting_quantity", item.AccountingQuantity);
                        itemCommand.Parameters.AddWithValue("actual_quantity", item.ActualQuantity);
                        itemCommand.Parameters.AddWithValue("difference_quantity", item.DifferenceQuantity);
                        itemCommand.Parameters.AddWithValue("unit", item.Unit);
                        itemCommand.Parameters.AddWithValue("price", item.Price);
                        itemCommand.Parameters.AddWithValue("accounting_amount", item.AccountingAmount);
                        itemCommand.Parameters.AddWithValue("actual_amount", item.ActualAmount);
                        itemCommand.Parameters.AddWithValue("difference_amount", item.DifferenceAmount);

                        await itemCommand.ExecuteNonQueryAsync();
                    }

                    // Если документ проведен, обновляем остатки
                    if (document.IsProcessed)
                    {
                        await UpdateStockFromInventoryAsync(document, connection, transaction);
                    }

                    // Фиксируем транзакцию
                    await transaction.CommitAsync();

                    return documentId;
                }
                catch (Exception ex)
                {
                    // Откатываем транзакцию в случае ошибки
                    await transaction.RollbackAsync();
                    throw new Exception($"Ошибка сохранения документа инвентаризации: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения документа инвентаризации: {ex.Message}", ex);
            }
        }

        // Обновление остатков на основе инвентаризации
        private async Task UpdateStockFromInventoryAsync(InventoryDocument document, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            foreach (var item in document.Items)
            {
                // Если есть расхождение, обновляем остатки
                if (item.DifferenceQuantity != 0)
                {
                    // Записываем в историю движения материалов
                    using var historyCommand = new NpgsqlCommand(@"
                        INSERT INTO material_movement_history (
                            material_article, material_type, document_type, document_id,
                            movement_date, quantity, unit, price, amount, movement_type)
                        VALUES (
                            @material_article, @material_type, 'inventory', @document_id,
                            @movement_date, @quantity, @unit, @price, @amount, @movement_type)", connection, transaction);

                    historyCommand.Parameters.AddWithValue("material_article", item.MaterialArticle);
                    historyCommand.Parameters.AddWithValue("material_type", item.MaterialType);
                    historyCommand.Parameters.AddWithValue("document_id", document.Id);
                    historyCommand.Parameters.AddWithValue("movement_date", document.DocumentDate);
                    historyCommand.Parameters.AddWithValue("unit", item.Unit);
                    historyCommand.Parameters.AddWithValue("price", item.Price);

                    // Если фактическое количество больше учетного (излишек)
                    if (item.DifferenceQuantity > 0)
                    {
                        historyCommand.Parameters.AddWithValue("quantity", item.DifferenceQuantity);
                        historyCommand.Parameters.AddWithValue("amount", item.DifferenceAmount);
                        historyCommand.Parameters.AddWithValue("movement_type", "in");
                    }
                    // Если фактическое количество меньше учетного (недостача)
                    else
                    {
                        historyCommand.Parameters.AddWithValue("quantity", Math.Abs(item.DifferenceQuantity));
                        historyCommand.Parameters.AddWithValue("amount", Math.Abs(item.DifferenceAmount));
                        historyCommand.Parameters.AddWithValue("movement_type", "out");
                    }

                    await historyCommand.ExecuteNonQueryAsync();

                    // Обновляем остатки в зависимости от типа материала
                    if (item.MaterialType == "fabric")
                    {
                        // Обновляем остатки ткани
                        using var updateCommand = new NpgsqlCommand(@"
                            UPDATE fabric_stock
                            SET quantity = @quantity
                            WHERE article = @article", connection, transaction);

                        updateCommand.Parameters.AddWithValue("quantity", item.ActualQuantity);
                        updateCommand.Parameters.AddWithValue("article", item.MaterialArticle);

                        await updateCommand.ExecuteNonQueryAsync();
                    }
                    else if (item.MaterialType == "fitting")
                    {
                        // Обновляем остатки фурнитуры
                        using var updateCommand = new NpgsqlCommand(@"
                            UPDATE fitting_stock
                            SET quantity = @quantity
                            WHERE article = @article", connection, transaction);

                        updateCommand.Parameters.AddWithValue("quantity", item.ActualQuantity);
                        updateCommand.Parameters.AddWithValue("article", item.MaterialArticle);

                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        // Утверждение документа инвентаризации директором
        public async Task ApproveInventoryDocumentAsync(int documentId, string approvedBy)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    UPDATE inventory_documents
                    SET is_approved = true, approved_by = @approved_by, approved_date = CURRENT_TIMESTAMP
                    WHERE id = @document_id", connection);

                command.Parameters.AddWithValue("document_id", documentId);
                command.Parameters.AddWithValue("approved_by", approvedBy);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка утверждения документа инвентаризации: {ex.Message}", ex);
            }
        }

        // Проведение документа инвентаризации
        public async Task ProcessInventoryDocumentAsync(int documentId)
        {
            try
            {
                // Получаем документ
                var document = await GetInventoryDocumentByIdAsync(documentId);
                if (document == null)
                {
                    throw new Exception("Документ не найден");
                }

                // Проверяем, требуется ли утверждение директором
                if (document.DifferencePercent > 20 && !document.IsApproved)
                {
                    throw new Exception("Документ с расхождением более 20% должен быть утвержден директором");
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Начинаем транзакцию
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Обновляем остатки
                    await UpdateStockFromInventoryAsync(document, connection, transaction);

                    // Отмечаем документ как проведенный
                    using var command = new NpgsqlCommand(@"
                        UPDATE inventory_documents
                        SET is_processed = true
                        WHERE id = @document_id", connection, transaction);

                    command.Parameters.AddWithValue("document_id", documentId);
                    await command.ExecuteNonQueryAsync();

                    // Фиксируем транзакцию
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    // Откатываем транзакцию в случае ошибки
                    await transaction.RollbackAsync();
                    throw new Exception($"Ошибка проведения документа инвентаризации: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка проведения документа инвентаризации: {ex.Message}", ex);
            }
        }

        // Получение документа инвентаризации по ID
        public async Task<InventoryDocument> GetInventoryDocumentByIdAsync(int documentId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Получаем документ
                InventoryDocument document = null;
                using (var command = new NpgsqlCommand(@"
                    SELECT * FROM inventory_documents
                    WHERE id = @document_id", connection))
                {
                    command.Parameters.AddWithValue("document_id", documentId);

                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        document = new InventoryDocument
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            DocumentNumber = reader.GetString(reader.GetOrdinal("document_number")),
                            DocumentDate = reader.GetDateTime(reader.GetOrdinal("document_date")),
                            WarehouseKeeper = reader.GetString(reader.GetOrdinal("warehouse_keeper")),
                            TotalAccountingAmount = reader.GetDecimal(reader.GetOrdinal("total_accounting_amount")),
                            TotalActualAmount = reader.GetDecimal(reader.GetOrdinal("total_actual_amount")),
                            DifferenceAmount = reader.GetDecimal(reader.GetOrdinal("difference_amount")),
                            DifferencePercent = reader.GetDecimal(reader.GetOrdinal("difference_percent")),
                            IsApproved = reader.GetBoolean(reader.GetOrdinal("is_approved")),
                            IsProcessed = reader.GetBoolean(reader.GetOrdinal("is_processed")),
                            CreatedBy = reader.GetString(reader.GetOrdinal("created_by")),
                            CreatedDate = reader.GetDateTime(reader.GetOrdinal("created_date"))
                        };

                        if (!reader.IsDBNull(reader.GetOrdinal("approved_by")))
                        {
                            document.ApprovedBy = reader.GetString(reader.GetOrdinal("approved_by"));
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("approved_date")))
                        {
                            document.ApprovedDate = reader.GetDateTime(reader.GetOrdinal("approved_date"));
                        }
                    }
                }

                if (document != null)
                {
                    // Получаем позиции документа
                    using var itemsCommand = new NpgsqlCommand(@"
                        SELECT * FROM inventory_document_items
                        WHERE document_id = @document_id", connection);

                    itemsCommand.Parameters.AddWithValue("document_id", documentId);

                    using var itemsReader = await itemsCommand.ExecuteReaderAsync();
                    while (await itemsReader.ReadAsync())
                    {
                        var item = new InventoryDocumentItem
                        {
                            Id = itemsReader.GetInt32(itemsReader.GetOrdinal("id")),
                            DocumentId = itemsReader.GetInt32(itemsReader.GetOrdinal("document_id")),
                            MaterialArticle = itemsReader.GetString(itemsReader.GetOrdinal("material_article")),
                            MaterialType = itemsReader.GetString(itemsReader.GetOrdinal("material_type")),
                            AccountingQuantity = itemsReader.GetDecimal(itemsReader.GetOrdinal("accounting_quantity")),
                            ActualQuantity = itemsReader.GetDecimal(itemsReader.GetOrdinal("actual_quantity")),
                            DifferenceQuantity = itemsReader.GetDecimal(itemsReader.GetOrdinal("difference_quantity")),
                            Unit = itemsReader.GetString(itemsReader.GetOrdinal("unit")),
                            Price = itemsReader.GetDecimal(itemsReader.GetOrdinal("price")),
                            AccountingAmount = itemsReader.GetDecimal(itemsReader.GetOrdinal("accounting_amount")),
                            ActualAmount = itemsReader.GetDecimal(itemsReader.GetOrdinal("actual_amount")),
                            DifferenceAmount = itemsReader.GetDecimal(itemsReader.GetOrdinal("difference_amount"))
                        };

                        // Получаем название материала
                        if (item.MaterialType == "fabric")
                        {
                            var fabric = await _databaseService.GetFabricByArticleAsync(item.MaterialArticle);
                            if (fabric != null)
                            {
                                item.MaterialName = fabric.FabricName;
                            }
                        }
                        else if (item.MaterialType == "fitting")
                        {
                            var fitting = await _databaseService.GetFittingByArticleAsync(item.MaterialArticle);
                            if (fitting != null)
                            {
                                item.MaterialName = fitting.Name;
                            }
                        }

                        document.Items.Add(item);
                    }
                }

                return document;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения документа инвентаризации: {ex.Message}", ex);
            }
        }

        // Получение списка документов инвентаризации
        public async Task<List<InventoryDocument>> GetInventoryDocumentsAsync()
        {
            var result = new List<InventoryDocument>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    SELECT * FROM inventory_documents
                    ORDER BY document_date DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var document = new InventoryDocument
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        DocumentNumber = reader.GetString(reader.GetOrdinal("document_number")),
                        DocumentDate = reader.GetDateTime(reader.GetOrdinal("document_date")),
                        WarehouseKeeper = reader.GetString(reader.GetOrdinal("warehouse_keeper")),
                        TotalAccountingAmount = reader.GetDecimal(reader.GetOrdinal("total_accounting_amount")),
                        TotalActualAmount = reader.GetDecimal(reader.GetOrdinal("total_actual_amount")),
                        DifferenceAmount = reader.GetDecimal(reader.GetOrdinal("difference_amount")),
                        DifferencePercent = reader.GetDecimal(reader.GetOrdinal("difference_percent")),
                        IsApproved = reader.GetBoolean(reader.GetOrdinal("is_approved")),
                        IsProcessed = reader.GetBoolean(reader.GetOrdinal("is_processed")),
                        CreatedBy = reader.GetString(reader.GetOrdinal("created_by")),
                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("created_date"))
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("approved_by")))
                    {
                        document.ApprovedBy = reader.GetString(reader.GetOrdinal("approved_by"));
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("approved_date")))
                    {
                        document.ApprovedDate = reader.GetDateTime(reader.GetOrdinal("approved_date"));
                    }

                    result.Add(document);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения списка документов инвентаризации: {ex.Message}", ex);
            }
        }

        // Получение отчета по остаткам материалов
        public async Task<List<MaterialStockReport>> GetMaterialStockReportAsync(List<string> articles = null)
        {
            var result = new List<MaterialStockReport>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Получаем остатки тканей
                string fabricQuery = @"
                    SELECT 
                        f.article,
                        COALESCE(fn.name, 'Неизвестная ткань') as name,
                        'fabric' as type,
                        fs.quantity,
                        COALESCE(f.unit, 'м') as unit,
                        f.price,
                        fs.quantity * f.price as amount
                    FROM fabric_stock fs
                    JOIN fabrics f ON fs.article = f.article
                    LEFT JOIN lookup_fabric_names fn ON f.name_code = fn.id";

                if (articles != null && articles.Count > 0)
                {
                    fabricQuery += " WHERE f.article = ANY(@articles)";
                }

                using (var command = new NpgsqlCommand(fabricQuery, connection))
                {
                    if (articles != null && articles.Count > 0)
                    {
                        command.Parameters.AddWithValue("articles", articles.ToArray());
                    }

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        result.Add(new MaterialStockReport
                        {
                            Article = reader.GetString(reader.GetOrdinal("article")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Type = reader.GetString(reader.GetOrdinal("type")),
                            Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
                            Unit = reader.GetString(reader.GetOrdinal("unit")),
                            Price = reader.GetDecimal(reader.GetOrdinal("price")),
                            Amount = reader.GetDecimal(reader.GetOrdinal("amount"))
                        });
                    }
                }

                // Получаем остатки фурнитуры
                string fittingQuery = @"
                    SELECT 
                        f.article,
                        f.name,
                        'fitting' as type,
                        fs.quantity,
                        'шт' as unit,
                        f.price,
                        fs.quantity * f.price as amount
                    FROM fitting_stock fs
                    JOIN fittings f ON fs.article = f.article";

                if (articles != null && articles.Count > 0)
                {
                    fittingQuery += " WHERE f.article = ANY(@articles)";
                }

                using (var command = new NpgsqlCommand(fittingQuery, connection))
                {
                    if (articles != null && articles.Count > 0)
                    {
                        command.Parameters.AddWithValue("articles", articles.ToArray());
                    }

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        result.Add(new MaterialStockReport
                        {
                            Article = reader.GetString(reader.GetOrdinal("article")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Type = reader.GetString(reader.GetOrdinal("type")),
                            Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
                            Unit = reader.GetString(reader.GetOrdinal("unit")),
                            Price = reader.GetDecimal(reader.GetOrdinal("price")),
                            Amount = reader.GetDecimal(reader.GetOrdinal("amount"))
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения отчета по остаткам материалов: {ex.Message}", ex);
            }
        }

        // Получение отчета по движению материалов за период
        public async Task<List<MaterialMovementReport>> GetMaterialMovementReportAsync(DateTime startDate, DateTime endDate, List<string> articles = null)
        {
            var result = new List<MaterialMovementReport>();

            try
            {
                // Получаем остатки на начало периода и на конец периода
                var initialStockQuery = await GetMaterialStockAtDateAsync(startDate, articles);
                var finalStockQuery = await GetMaterialStockAtDateAsync(endDate.AddDays(1), articles);

                // Получаем движения за период
                var movements = await GetMaterialMovementsForPeriodAsync(startDate, endDate, articles);

                // Формируем отчет
                foreach (var article in initialStockQuery.Keys.Union(finalStockQuery.Keys).Union(movements.Keys).Distinct())
                {
                    // Получаем информацию о материале
                    var materialInfo = await GetMaterialInfoAsync(article);
                    if (materialInfo == null) continue;

                    // Начальные остатки
                    decimal initialQuantity = 0;
                    decimal initialAmount = 0;
                    if (initialStockQuery.ContainsKey(article))
                    {
                        initialQuantity = initialStockQuery[article].Quantity;
                        initialAmount = initialStockQuery[article].Amount;
                    }

                    // Конечные остатки
                    decimal finalQuantity = 0;
                    decimal finalAmount = 0;
                    if (finalStockQuery.ContainsKey(article))
                    {
                        finalQuantity = finalStockQuery[article].Quantity;
                        finalAmount = finalStockQuery[article].Amount;
                    }

                    // Приход и расход за период
                    decimal receiptQuantity = 0;
                    decimal receiptAmount = 0;
                    decimal expenseQuantity = 0;
                    decimal expenseAmount = 0;

                    if (movements.ContainsKey(article))
                    {
                        foreach (var movement in movements[article])
                        {
                            if (movement.MovementType == "in")
                            {
                                receiptQuantity += movement.Quantity;
                                receiptAmount += movement.Amount;
                            }
                            else if (movement.MovementType == "out")
                            {
                                expenseQuantity += movement.Quantity;
                                expenseAmount += movement.Amount;
                            }
                        }
                    }

                    // Добавляем в отчет
                    result.Add(new MaterialMovementReport
                    {
                        Article = article,
                        
                        InitialQuantity = initialQuantity,
                        InitialAmount = initialAmount,
                        ReceiptQuantity = receiptQuantity,
                        ReceiptAmount = receiptAmount,
                        ExpenseQuantity = expenseQuantity,
                        ExpenseAmount = expenseAmount,
                        FinalQuantity = finalQuantity,
                        FinalAmount = finalAmount
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения отчета по движению материалов: {ex.Message}", ex);
            }
        }

        // Вспомогательный метод для получения остатков материалов на определенную дату
        private async Task<Dictionary<string, (decimal Quantity, decimal Amount)>> GetMaterialStockAtDateAsync(DateTime date, List<string> articles = null)
        {
            var result = new Dictionary<string, (decimal Quantity, decimal Amount)>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Получаем все движения материалов до указанной даты
                string query = @"
                    SELECT 
                        material_article,
                        material_type,
                        movement_type,
                        quantity,
                        amount
                    FROM material_movement_history
                    WHERE movement_date < @date";

                if (articles != null && articles.Count > 0)
                {
                    query += " AND material_article = ANY(@articles)";
                }

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("date", date);

                if (articles != null && articles.Count > 0)
                {
                    command.Parameters.AddWithValue("articles", articles.ToArray());
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string article = reader.GetString(reader.GetOrdinal("material_article"));
                    string movementType = reader.GetString(reader.GetOrdinal("movement_type"));
                    decimal quantity = reader.GetDecimal(reader.GetOrdinal("quantity"));
                    decimal amount = reader.GetDecimal(reader.GetOrdinal("amount"));

                    if (!result.ContainsKey(article))
                    {
                        result[article] = (0, 0);
                    }

                    var current = result[article];

                    if (movementType == "in")
                    {
                        result[article] = (current.Quantity + quantity, current.Amount + amount);
                    }
                    else if (movementType == "out")
                    {
                        result[article] = (current.Quantity - quantity, current.Amount - amount);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения остатков материалов на дату: {ex.Message}", ex);
            }
        }

        // Вспомогательный метод для получения движений материалов за период
        private async Task<Dictionary<string, List<(string MovementType, decimal Quantity, decimal Amount)>>> GetMaterialMovementsForPeriodAsync(DateTime startDate, DateTime endDate, List<string> articles = null)
        {
            var result = new Dictionary<string, List<(string MovementType, decimal Quantity, decimal Amount)>>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Получаем все движения материалов за указанный период
                string query = @"
                    SELECT 
                        material_article,
                        material_type,
                        movement_type,
                        quantity,
                        amount
                    FROM material_movement_history
                    WHERE movement_date >= @start_date AND movement_date < @end_date";

                if (articles != null && articles.Count > 0)
                {
                    query += " AND material_article = ANY(@articles)";
                }

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("start_date", startDate);
                command.Parameters.AddWithValue("end_date", endDate.AddDays(1)); // До конца дня

                if (articles != null && articles.Count > 0)
                {
                    command.Parameters.AddWithValue("articles", articles.ToArray());
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string article = reader.GetString(reader.GetOrdinal("material_article"));
                    string movementType = reader.GetString(reader.GetOrdinal("movement_type"));
                    decimal quantity = reader.GetDecimal(reader.GetOrdinal("quantity"));
                    decimal amount = reader.GetDecimal(reader.GetOrdinal("amount"));

                    if (!result.ContainsKey(article))
                    {
                        result[article] = new List<(string MovementType, decimal Quantity, decimal Amount)>();
                    }

                    result[article].Add((movementType, quantity, amount));
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения движений материалов за период: {ex.Message}", ex);
            }
        }

        // Вспомогательный метод для получения информации о материале
        private async Task<(string Name, string Type, string Unit, decimal Price)?> GetMaterialInfoAsync(string article)
        {
            try
            {
                // Проверяем, является ли материал тканью
                var fabric = await _databaseService.GetFabricByArticleAsync(article);
                if (fabric != null)
                {
                    return (fabric.FabricName, "fabric", fabric.Unit, fabric.Price);
                }

                // Проверяем, является ли материал фурнитурой
                var fitting = await _databaseService.GetFittingByArticleAsync(article);
                if (fitting != null)
                {
                    return (fitting.Name, "fitting", "шт", fitting.Price);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения информации о материале: {ex.Message}", ex);
            }
        }

        // Получение списка заказов
        public async Task<List<OrderListItem>> GetOrderListAsync()
        {
            var result = new List<OrderListItem>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    SELECT 
                        o.id,
                        o.order_number,
                        o.order_date,
                        o.status,
                        u.full_name as customer,
                        COALESCE(m.full_name, 'Не назначен') as manager,
                        (SELECT COUNT(*) FROM order_items WHERE order_id = o.id) as total_product_count
                    FROM orders o
                    LEFT JOIN users u ON o.customer_user_id = u.id
                    LEFT JOIN users m ON o.manager_user_id = m.id
                    ORDER BY o.order_date DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var statusCode = reader.GetInt32(reader.GetOrdinal("status"));
                    var status = (OrderStatus)statusCode;

                    result.Add(new OrderListItem
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        OrderNumber = reader.GetString(reader.GetOrdinal("order_number")),
                        OrderDate = reader.GetDateTime(reader.GetOrdinal("order_date")),
                        TotalItems = reader.GetInt32(reader.GetOrdinal("total_product_count")),
                        Status = (OrderStatus)status,
                        CustomerName = reader.GetString(reader.GetOrdinal("customer")),
                        ManagerName = reader.GetString(reader.GetOrdinal("manager"))
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения списка заказов: {ex.Message}", ex);
            }
        }

        // Генерация номера документа инвентаризации
        public async Task<string> GenerateInventoryDocumentNumberAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    SELECT COUNT(*) + 1 as new_number
                    FROM inventory_documents
                    WHERE EXTRACT(YEAR FROM document_date) = EXTRACT(YEAR FROM CURRENT_DATE)", connection);

                var newNumber = await command.ExecuteScalarAsync();
                return $"ИНВ-{DateTime.Now.Year}-{newNumber:D4}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка генерации номера документа инвентаризации: {ex.Message}", ex);
            }
        }
    }
}