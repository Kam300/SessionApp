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
                using var checkCommand = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE login = @login", connection);
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
                COALESCE(f.dimension_unit, '') as dimension_unit,
                f.weight_value,
                COALESCE(f.weight_unit, '') as weight_unit,
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
                        DimensionUnit = reader.GetString("dimension_unit"), // Теперь всегда строка
                        WeightValue = reader.GetDecimal("weight_value"),
                        WeightUnit = reader.GetString("weight_unit"), // Теперь всегда строка
                        TypeCode = reader.GetInt32("type_code"),
                        ImagePath = reader.GetString("image_path"),
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
    }
}
