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
            _connectionString = "Host=localhost;Database=postgres;Username=postgres;Password=00000000;Port=5432";
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
    }
}
