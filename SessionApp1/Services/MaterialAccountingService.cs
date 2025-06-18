using Npgsql;
using SessionApp1.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SessionApp1.Services
{
    public class MaterialAccountingService
    {
        private readonly string _connectionString;
        private readonly DatabaseService _databaseService;

        public MaterialAccountingService()
        {
            _connectionString = "Host=localhost;Database=postgres;Username=postgres;Password=00000000;Port=5432";
            _databaseService = new DatabaseService();
        }

        public async Task<List<FabricStockInfo>> GetFabricStockWithUnitsAsync()
        {
            var stocks = new List<FabricStockInfo>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    SELECT 
                        COALESCE(fs.roll_id, '') as roll_id,
                        COALESCE(fs.fabric_article, '') as fabric_article,
                        COALESCE(fn.name, '') as fabric_name,
                        COALESCE(fs.length_mm, 0) as length_mm,
                        COALESCE(fs.width_mm, 0) as width_mm,
                        COALESCE(fs.unit, 'м') as unit,
                        COALESCE(f.price, 0) as price,
                        ROUND((COALESCE(fs.length_mm, 0) * COALESCE(fs.width_mm, 0) / 1000000.0), 2) as area_sqm,
                        ROUND((COALESCE(fs.length_mm, 0) / 1000.0), 2) as length_m,
                        ROUND((COALESCE(f.price, 0) * COALESCE(fs.length_mm, 0) / 1000.0), 2) as total_cost
                    FROM fabric_stock as fs
                    LEFT JOIN fabrics f ON fs.fabric_article = f.article
                    LEFT JOIN lookup_fabric_names fn ON f.name_code = fn.id
                    ORDER BY fs.fabric_article", connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    stocks.Add(new FabricStockInfo
                    {
                        RollId = reader.GetString("roll_id"),
                        FabricArticle = reader.GetString("fabric_article"),
                        FabricName = reader.GetString("fabric_name"),
                        LengthMm = reader.GetInt32("length_mm"),
                        WidthMm = reader.GetInt32("width_mm"),
                        Unit = reader.GetString("unit"),
                        Price = reader.GetDecimal("price"),
                        AreaSqm = reader.GetDecimal("area_sqm"),
                        LengthM = reader.GetDecimal("length_m"),
                        TotalCost = reader.GetDecimal("total_cost")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки остатков тканей: {ex.Message}", ex);
            }
            return stocks;
        }

        public async Task<List<FittingStockInfo>> GetFittingStockWithUnitsAsync()
        {
            var stocks = new List<FittingStockInfo>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    SELECT 
                        COALESCE(fs.batch_id, '') as batch_id,
                        COALESCE(fs.fitting_article, '') as fitting_article,
                        COALESCE(f.name, '') as name,
                        COALESCE(fs.quantity, 0) as quantity,
                        COALESCE(f.price, 0) as price,
                        COALESCE(f.weight_value, 0) as weight_value,
                        COALESCE(f.weight_unit, 'г') as weight_unit,
                        ROUND((COALESCE(fs.quantity, 0) * COALESCE(f.weight_value, 0)), 2) as total_weight,
                        ROUND((COALESCE(f.price, 0) * COALESCE(fs.quantity, 0)), 2) as total_cost
                    FROM fitting_stock fs
                    LEFT JOIN fittings f ON fs.fitting_article = f.article
                    ORDER BY fs.fitting_article", connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    stocks.Add(new FittingStockInfo
                    {
                        BatchId = reader.GetString("batch_id"),
                        FittingArticle = reader.GetString("fitting_article"),
                        FittingName = reader.GetString("name"),
                        Quantity = reader.GetInt32("quantity"),
                        Price = reader.GetDecimal("price"),
                        WeightValue = reader.GetDecimal("weight_value"),
                        WeightUnit = reader.GetString("weight_unit"),
                        TotalWeight = reader.GetDecimal("total_weight"),
                        TotalCost = reader.GetDecimal("total_cost")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки остатков фурнитуры: {ex.Message}", ex);
            }
            return stocks;
        }

        public async Task<decimal> CalculateAverageCostAsync(string materialArticle, string materialType)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = materialType.ToLower() == "fabric"
                    ? @"SELECT 
                         COALESCE(SUM(fs.length_mm * f.price / 1000.0) / NULLIF(SUM(fs.length_mm / 1000.0), 0), 0) as avg_cost
                       FROM fabric_stock fs
                       LEFT JOIN fabrics f ON fs.fabric_article = f.article
                       WHERE fs.fabric_article = @article"
                    : @"SELECT 
                         COALESCE(SUM(fs.quantity * f.price) / NULLIF(SUM(fs.quantity), 0), 0) as avg_cost
                       FROM fitting_stock fs
                       LEFT JOIN fittings f ON fs.fitting_article = f.article
                       WHERE fs.fitting_article = @article";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("article", materialArticle);

                var result = await command.ExecuteScalarAsync();
                return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка расчета средней стоимости: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Получает спецификации для указанного изделия
        /// </summary>
        /// <param name="productArticle">Артикул изделия</param>
        /// <returns>Список спецификаций</returns>
        public async Task<List<ProductSpecification>> GetProductSpecificationsAsync(string productArticle)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // В реальном приложении здесь будет запрос к базе данных
                // Для демонстрации создаем тестовые данные из региона тестовых данных
                var specs = GetSampleSpecifications(productArticle);
                return await Task.FromResult(specs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении спецификаций: {ex.Message}");
                return new List<ProductSpecification>();
            }
        }

        /// <summary>
        /// Создает тестовые данные спецификаций для демонстрации (устаревший метод)
        /// </summary>
        /// <param name="productArticle">Артикул изделия</param>
        /// <returns>Список спецификаций</returns>
        private List<ProductSpecification> GetSampleSpecificationsOld(string productArticle)
        {
            var specs = new List<ProductSpecification>();
            
            // Создаем историю изменений спецификации для демонстрации
            // Первая версия спецификации (3 месяца назад)
            var oldDate = DateTime.Now.AddMonths(-3);
            specs.Add(new ProductSpecification
            {
                Id = "1",
                ProductArticle = productArticle,
                MaterialArticle = "F001",
                MaterialType = "fabric",
                Quantity = 1.5m,
                Unit = "м",
                CreatedDate = oldDate,
                CreatedBy = "Иванов И.И."
            });
            
            specs.Add(new ProductSpecification
            {
                Id = "2",
                ProductArticle = productArticle,
                MaterialArticle = "FT001",
                MaterialType = "fitting",
                Quantity = 5,
                Unit = "шт",
                CreatedDate = oldDate,
                CreatedBy = "Иванов И.И."
            });
            
            // Вторая версия спецификации (1 месяц назад)
            var mediumDate = DateTime.Now.AddMonths(-1);
            specs.Add(new ProductSpecification
            {
                Id = "3",
                ProductArticle = productArticle,
                MaterialArticle = "F001",
                MaterialType = "fabric",
                Quantity = 1.5m,
                Unit = "м",
                CreatedDate = mediumDate,
                CreatedBy = "Петров П.П."
            });
            
            specs.Add(new ProductSpecification
            {
                Id = "4",
                ProductArticle = productArticle,
                MaterialArticle = "FT002", // Изменили фурнитуру
                MaterialType = "fitting",
                Quantity = 6, // Увеличили количество
                Unit = "шт",
                CreatedDate = mediumDate,
                CreatedBy = "Петров П.П."
            });
            
            // Текущая версия спецификации
            var currentDate = DateTime.Now;
            specs.Add(new ProductSpecification
            {
                Id = "5",
                ProductArticle = productArticle,
                MaterialArticle = "F002", // Изменили ткань
                MaterialType = "fabric",
                Quantity = 1.7m, // Увеличили расход ткани
                Unit = "м",
                CreatedDate = currentDate,
                CreatedBy = "Сидоров С.С."
            });
            
            specs.Add(new ProductSpecification
            {
                Id = "6",
                ProductArticle = productArticle,
                MaterialArticle = "FT002",
                MaterialType = "fitting",
                Quantity = 6,
                Unit = "шт",
                CreatedDate = currentDate,
                CreatedBy = "Сидоров С.С."
            });
            
            return specs;
        }

        /// <summary>
        /// Получает спецификации для указанного изделия на определенную дату
        /// </summary>
        /// <param name="productArticle">Артикул изделия</param>
        /// <param name="date">Дата спецификации</param>
        /// <returns>Список спецификаций</returns>
        public async Task<List<ProductSpecification>> GetProductSpecificationsAsync(string productArticle, DateTime date)
        {
            try
            {
                var allSpecs = await GetProductSpecificationsAsync(productArticle);
                return allSpecs.Where(s => s.CreatedDate.Date == date.Date).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении спецификаций по дате: {ex.Message}");
                return new List<ProductSpecification>();
            }
        }

        /// <summary>
        /// Проверяет, является ли остаток материала обрезком
        /// </summary>
        /// <param name="materialArticle">Артикул материала</param>
        /// <param name="materialType">Тип материала</param>
        /// <param name="remainingQuantity">Оставшееся количество</param>
        /// <returns>True, если остаток является обрезком</returns>
        public async Task<bool> IsScrapAsync(string materialArticle, string materialType, decimal remainingQuantity)
        {
            try
            {
                // В реальном приложении здесь будет запрос к базе данных для получения настроек обрезков для материала
                // Для демонстрации используем тестовые данные
                if (materialType == "fabric")
                {
                    // Для тканей обрезком считается остаток менее 0.5 кв.м
                    return remainingQuantity < 0.5m;
                }
                else if (materialType == "fitting")
                {
                    // Для фурнитуры обрезком считается остаток менее 10 штук или 0.1 кг в зависимости от единицы измерения
                    var fitting = await _databaseService.GetFittingByArticleAsync(materialArticle);
                    if (fitting != null)
                    {
                        if (fitting.Unit == "шт")
                            return remainingQuantity < 10;
                        else if (fitting.Unit == "кг")
                            return remainingQuantity < 0.1m;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке обрезков: {ex.Message}");
                return false;
            }
        }

        #region Тестовые данные

        // Метод для создания тестовых данных спецификаций
        private List<ProductSpecification> GetSampleSpecifications(string productArticle)
        {
            var result = new List<ProductSpecification>();

            // Текущая спецификация (сегодня)
            var currentDate = DateTime.Today;
            result.AddRange(GetSpecificationForDate(productArticle, currentDate));

            // Предыдущая версия спецификации (месяц назад)
            var previousDate = DateTime.Today.AddMonths(-1);
            result.AddRange(GetSpecificationForDate(productArticle, previousDate, true));

            // Еще более старая версия (3 месяца назад)
            var oldDate = DateTime.Today.AddMonths(-3);
            result.AddRange(GetSpecificationForDate(productArticle, oldDate, true));

            return result;
        }

        // Метод для создания спецификации на конкретную дату
        private List<ProductSpecification> GetSpecificationForDate(string productArticle, DateTime date, bool isOld = false)
        {
            var specs = new List<ProductSpecification>();

            // Разные спецификации в зависимости от артикула изделия
            if (productArticle == "P001")
            {
                // Полотенце
                specs.Add(new ProductSpecification
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductArticle = productArticle,
                    MaterialArticle = isOld ? "F001" : "F002", // В старой версии использовалась другая ткань
                    MaterialType = "fabric",
                    Quantity = 0.5m,
                    Unit = "м²",
                    CreatedDate = date,
                    CreatedBy = "Иванов И.И."
                });

                specs.Add(new ProductSpecification
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductArticle = productArticle,
                    MaterialArticle = "T001",
                    MaterialType = "fitting",
                    Quantity = 0.2m,
                    Unit = "м",
                    CreatedDate = date,
                    CreatedBy = "Иванов И.И."
                });
            }
            else if (productArticle == "P002")
            {
                // Плед
                specs.Add(new ProductSpecification
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductArticle = productArticle,
                    MaterialArticle = "F003",
                    MaterialType = "fabric",
                    Quantity = 2.0m,
                    Unit = "м²",
                    CreatedDate = date,
                    CreatedBy = "Петров П.П."
                });

                specs.Add(new ProductSpecification
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductArticle = productArticle,
                    MaterialArticle = isOld ? "T002" : "T003", // В старой версии использовалась другая окантовка
                    MaterialType = "fitting",
                    Quantity = 5.0m,
                    Unit = "м",
                    CreatedDate = date,
                    CreatedBy = "Петров П.П."
                });
            }
            else if (productArticle == "P003")
            {
                // Подушка
                specs.Add(new ProductSpecification
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductArticle = productArticle,
                    MaterialArticle = "F004",
                    MaterialType = "fabric",
                    Quantity = 0.8m,
                    Unit = "м²",
                    CreatedDate = date,
                    CreatedBy = "Сидоров С.С."
                });

                specs.Add(new ProductSpecification
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductArticle = productArticle,
                    MaterialArticle = "T004",
                    MaterialType = "fitting",
                    Quantity = isOld ? 0.3m : 0.4m, // В новой версии используется больше наполнителя
                    Unit = "кг",
                    CreatedDate = date,
                    CreatedBy = "Сидоров С.С."
                });
            }
            else
            {
                // Для других артикулов - пример спецификации
                specs.Add(new ProductSpecification
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductArticle = productArticle,
                    MaterialArticle = "F001",
                    MaterialType = "fabric",
                    Quantity = 1.0m,
                    Unit = "м²",
                    CreatedDate = date,
                    CreatedBy = "Администратор"
                });

                specs.Add(new ProductSpecification
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductArticle = productArticle,
                    MaterialArticle = "T001",
                    MaterialType = "fitting",
                    Quantity = 2.0m,
                    Unit = "м",
                    CreatedDate = date,
                    CreatedBy = "Администратор"
                });
            }

            return specs;
        }

        #endregion
    }
}
