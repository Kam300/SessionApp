using Npgsql;
using SessionApp1.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SessionApp1.Services
{
    public class MaterialAccountingService
    {
        private readonly string _connectionString;

        public MaterialAccountingService()
        {
            _connectionString = "Host=localhost;Database=ff;Username=postgres;Password=00000000;Port=5432";
        }

        // Получение остатков тканей с пересчетом единиц измерения
        public async Task<List<FabricStockInfo>> GetFabricStockWithUnitsAsync()
        {
            var stocks = new List<FabricStockInfo>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    SELECT 
                        fs.roll_id,
                        fs.fabric_article,
                        f.name_code,
                        fn.name as fabric_name,
                        fs.length_mm,
                        fs.width_mm,
                        fs.unit,
                        f.price,
                        -- Расчет площади в кв.м
                        ROUND((fs.length_mm * fs.width_mm / 1000000.0), 2) as area_sqm,
                        -- Расчет погонных метров
                        ROUND((fs.length_mm / 1000.0), 2) as length_m,
                        -- Расчет стоимости
                        ROUND((f.price * fs.length_mm / 1000.0), 2) as total_cost
                    FROM fabric_stock fs
                    JOIN fabrics f ON fs.fabric_article = f.article
                    LEFT JOIN lookup_fabric_names fn ON f.name_code = fn.id
                    ORDER BY fs.fabric_article", connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    stocks.Add(new FabricStockInfo
                    {
                        RollId = reader.GetString("roll_id"),
                        FabricArticle = reader.GetString("fabric_article"),
                        FabricName = reader.IsDBNull("fabric_name") ? "" : reader.GetString("fabric_name"),
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

        // Получение остатков фурнитуры с пересчетом единиц измерения
        public async Task<List<FittingStockInfo>> GetFittingStockWithUnitsAsync()
        {
            var stocks = new List<FittingStockInfo>();
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(@"
                    SELECT 
                        fs.batch_id,
                        fs.fitting_article,
                        f.name,
                        fs.quantity,
                        f.price,
                        f.weight_value,
                        f.weight_unit,
                        -- Расчет общего веса
                        ROUND((fs.quantity * f.weight_value), 2) as total_weight,
                        -- Расчет стоимости
                        ROUND((f.price * fs.quantity), 2) as total_cost
                    FROM fitting_stock fs
                    JOIN fittings f ON fs.fitting_article = f.article
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

        // Расчет средней стоимости материала для списания
        public async Task<decimal> CalculateAverageCostAsync(string materialArticle, string materialType)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = materialType.ToLower() == "fabric"
                    ? @"SELECT 
                         SUM(fs.length_mm * f.price / 1000.0) / SUM(fs.length_mm / 1000.0) as avg_cost
                       FROM fabric_stock fs
                       JOIN fabrics f ON fs.fabric_article = f.article
                       WHERE fs.fabric_article = @article"
                    : @"SELECT 
                         SUM(fs.quantity * f.price) / SUM(fs.quantity) as avg_cost
                       FROM fitting_stock fs
                       JOIN fittings f ON fs.fitting_article = f.article
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
    }
}
