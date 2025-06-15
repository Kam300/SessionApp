using Npgsql;
using SessionApp1.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionApp1.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = "Host=localhost;Database=ff;Username=postgres;Password=00000000";
        }

        public async Task<User?> AuthenticateUserAsync(string login, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand("SELECT * FROM authenticate_user(@login, @password)", connection);
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
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand("SELECT register_customer(@fullname, @login, @password)", connection);
            command.Parameters.AddWithValue("fullname", fullName);
            command.Parameters.AddWithValue("login", login);
            command.Parameters.AddWithValue("password", password);

            var result = await command.ExecuteScalarAsync();
            return (bool)result!;
        }

        public async Task<List<Fabric>> GetFabricsAsync()
        {
            var fabrics = new List<Fabric>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand("SELECT * FROM get_fabrics_with_details()", connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                fabrics.Add(new Fabric
                {
                    Article = reader.GetString("article"),
                    NameCode = reader.GetInt32("namecode"),
                    ColorCode = reader.GetInt32("colorcode"),
                    PatternCode = reader.GetInt32("patterncode"),
                    ImagePath = reader.GetString("imagepath"),
                    CompositionCode = reader.GetInt32("compositioncode"),
                    WidthMm = reader.GetInt32("widthmm"),
                    LengthMm = reader.GetInt32("lengthmm"),
                    Unit = reader.GetString("unit"),
                    Price = reader.GetDecimal("price"),
                    FabricName = reader.IsDBNull("fabricname") ? "" : reader.GetString("fabricname"),
                    ColorName = reader.IsDBNull("colorname") ? "" : reader.GetString("colorname"),
                    PatternName = reader.IsDBNull("patternname") ? "" : reader.GetString("patternname"),
                    CompositionName = reader.IsDBNull("compositionname") ? "" : reader.GetString("compositionname")
                });
            }
            return fabrics;
        }

        public async Task<List<Fitting>> GetFittingsAsync()
        {
            var fittings = new List<Fitting>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand("SELECT * FROM get_fittings_with_details()", connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                fittings.Add(new Fitting
                {
                    Article = reader.GetString("article"),
                    Name = reader.GetString("name"),
                    WidthMm = reader.GetDecimal("widthmm"),
                    LengthMm = reader.GetDecimal("lengthmm"),
                    DimensionUnit = reader.GetString("dimensionunit"),
                    WeightValue = reader.GetDecimal("weightvalue"),
                    WeightUnit = reader.GetString("weightunit"),
                    TypeCode = reader.GetInt32("typecode"),
                    ImagePath = reader.GetString("imagepath"),
                    Price = reader.GetDecimal("price"),
                    TypeName = reader.IsDBNull("typename") ? "" : reader.GetString("typename")
                });
            }
            return fittings;
        }

        public async Task<List<ManufacturedGood>> GetManufacturedGoodsAsync()
        {
            var goods = new List<ManufacturedGood>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand("SELECT * FROM manufacturedgoods ORDER BY article", connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                goods.Add(new ManufacturedGood
                {
                    Article = reader.GetString("article"),
                    Name = reader.GetString("name"),
                    WidthMm = reader.GetInt32("widthmm"),
                    LengthMm = reader.GetInt32("lengthmm"),
                    Unit = reader.GetString("unit"),
                    Price = reader.GetDecimal("price"),
                    ImagePath = reader.GetString("imagepath"),
                    Comment = reader.GetString("comment")
                });
            }
            return goods;
        }
    }
}
