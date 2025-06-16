using Npgsql;
using SessionApp1.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SessionApp1.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _masterConnectionString;

        public DatabaseService()
        {
            _connectionString = "Host=localhost;Database=ff;Username=postgres;Password=00000000;Port=5432";
            _masterConnectionString = "Host=localhost;Database=postgres;Username=postgres;Password=00000000;Port=5432";
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await CreateDatabaseIfNotExistsAsync();
                await CreateTablesAndFunctionsAsync();
                await ImportCsvDataAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка инициализации базы данных: {ex.Message}", ex);
            }
        }

        private async Task CreateDatabaseIfNotExistsAsync()
        {
            using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();

            var checkDbCommand = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'ff'", connection);
            var dbExists = await checkDbCommand.ExecuteScalarAsync();

            if (dbExists == null)
            {
                var createDbCommand = new NpgsqlCommand("CREATE DATABASE ff", connection);
                await createDbCommand.ExecuteNonQueryAsync();
            }
        }

        private async Task CreateTablesAndFunctionsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
-- Включение расширения для хеширования паролей
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Удаление существующих таблиц
DROP TABLE IF EXISTS orderitems, orders, users, roles, specificationsfabric, specificationsfitting, 
fabricstock, fittingstock, fabrics, manufacturedgoods, fittings, lookupfabricnames, lookupcolors, 
lookuppatterns, lookupcompositions, lookupfittingtypes CASCADE;

-- Создание таблиц
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name TEXT UNIQUE NOT NULL
);

CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    roleid INT NOT NULL REFERENCES roles(id),
    fullname TEXT NOT NULL,
    login TEXT UNIQUE NOT NULL,
    passwordhash TEXT NOT NULL
);

-- Справочные таблицы
CREATE TABLE lookupfabricnames (
    id INT PRIMARY KEY,
    name TEXT
);

CREATE TABLE lookupcolors (
    id INT PRIMARY KEY,
    name TEXT
);

CREATE TABLE lookuppatterns (
    id INT PRIMARY KEY,
    name TEXT
);

CREATE TABLE lookupcompositions (
    id INT PRIMARY KEY,
    name TEXT
);

CREATE TABLE lookupfittingtypes (
    id INT PRIMARY KEY,
    name TEXT
);

-- Основные таблицы
CREATE TABLE fabrics (
    article VARCHAR(50) PRIMARY KEY,
    namecode INT,
    colorcode INT,
    patterncode INT,
    imagepath TEXT,
    compositioncode INT,
    widthmm INT,
    lengthmm INT,
    unit VARCHAR(20),
    price NUMERIC(10,2)
);

CREATE TABLE fittings (
    article VARCHAR(50) PRIMARY KEY,
    name TEXT,
    widthmm NUMERIC(10,2),
    lengthmm NUMERIC(10,2),
    dimensionunit VARCHAR(20),
    weightvalue NUMERIC(10,2),
    weightunit VARCHAR(20),
    typecode INT,
    imagepath TEXT,
    price NUMERIC(10,2)
);

CREATE TABLE manufacturedgoods (
    article VARCHAR(50) PRIMARY KEY,
    name TEXT,
    widthmm INT,
    lengthmm INT,
    unit VARCHAR(20),
    price NUMERIC(10,2),
    imagepath TEXT,
    comment TEXT
);

CREATE TABLE fabricstock (
    rollid TEXT PRIMARY KEY,
    fabricarticle VARCHAR(50),
    lengthmm INT,
    widthmm INT,
    unit VARCHAR(20)
);

CREATE TABLE fittingstock (
    batchid TEXT PRIMARY KEY,
    fittingarticle VARCHAR(50),
    quantity INT
);

-- Вставка ролей
INSERT INTO roles (name) VALUES 
('Заказчик'),
('Менеджер'),
('Кладовщик'),
('Дирекция');

-- Создание тестовых пользователей
INSERT INTO users (roleid, fullname, login, passwordhash) VALUES
((SELECT id FROM roles WHERE name = 'Менеджер'), 'Менеджер Тестовый', 'manager', crypt('123456', gen_salt('bf'))),
((SELECT id FROM roles WHERE name = 'Кладовщик'), 'Кладовщик Тестовый', 'warehouse', crypt('123456', gen_salt('bf'))),
((SELECT id FROM roles WHERE name = 'Дирекция'), 'Директор Тестовый', 'director', crypt('123456', gen_salt('bf')));

-- Функции
CREATE OR REPLACE FUNCTION authenticate_user(p_login TEXT, p_password TEXT)
RETURNS TABLE(id INT, roleid INT, fullname TEXT, login TEXT, rolename TEXT)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.roleid, u.fullname, u.login, r.name as rolename
    FROM users u
    JOIN roles r ON u.roleid = r.id
    WHERE u.login = p_login 
    AND u.passwordhash = crypt(p_password, u.passwordhash);
END;
$$;

CREATE OR REPLACE FUNCTION register_customer(p_fullname TEXT, p_login TEXT, p_password TEXT)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
DECLARE
    customer_role_id INT;
BEGIN
    IF EXISTS (SELECT 1 FROM users WHERE login = p_login) THEN
        RETURN FALSE;
    END IF;
    
    SELECT id INTO customer_role_id FROM roles WHERE name = 'Заказчик';
    
    INSERT INTO users (roleid, fullname, login, passwordhash)
    VALUES (customer_role_id, p_fullname, p_login, crypt(p_password, gen_salt('bf')));
    
    RETURN TRUE;
EXCEPTION
    WHEN OTHERS THEN
        RETURN FALSE;
END;
$$;

CREATE OR REPLACE FUNCTION get_fabrics_with_details()
RETURNS TABLE(
    article VARCHAR(50),
    namecode INT,
    colorcode INT,
    patterncode INT,
    imagepath TEXT,
    compositioncode INT,
    widthmm INT,
    lengthmm INT,
    unit VARCHAR(20),
    price NUMERIC(10,2),
    fabricname TEXT,
    colorname TEXT,
    patternname TEXT,
    compositionname TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        f.article,
        f.namecode,
        f.colorcode,
        f.patterncode,
        f.imagepath,
        f.compositioncode,
        f.widthmm,
        f.lengthmm,
        f.unit,
        f.price,
        COALESCE(fn.name, '') as fabricname,
        COALESCE(c.name, '') as colorname,
        COALESCE(p.name, '') as patternname,
        COALESCE(comp.name, '') as compositionname
    FROM fabrics f
    LEFT JOIN lookupfabricnames fn ON f.namecode = fn.id
    LEFT JOIN lookupcolors c ON f.colorcode = c.id
    LEFT JOIN lookuppatterns p ON f.patterncode = p.id
    LEFT JOIN lookupcompositions comp ON f.compositioncode = comp.id
    ORDER BY f.article;
END;
$$;

CREATE OR REPLACE FUNCTION get_fittings_with_details()
RETURNS TABLE(
    article VARCHAR(50),
    name TEXT,
    widthmm NUMERIC(10,2),
    lengthmm NUMERIC(10,2),
    dimensionunit VARCHAR(20),
    weightvalue NUMERIC(10,2),
    weightunit VARCHAR(20),
    typecode INT,
    imagepath TEXT,
    price NUMERIC(10,2),
    typename TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        f.article,
        f.name,
        f.widthmm,
        f.lengthmm,
        f.dimensionunit,
        f.weightvalue,
        f.weightunit,
        f.typecode,
        f.imagepath,
        f.price,
        COALESCE(ft.name, '') as typename
    FROM fittings f
    LEFT JOIN lookupfittingtypes ft ON f.typecode = ft.id
    ORDER BY f.article;
END;
$$;
";

            using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task ImportCsvDataAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var csvPath = GetCsvPath();

            // Импорт данных из CSV файлов
            await ImportCsvFileAsync(connection, csvPath, "lookup_fabric_names.csv", "lookupfabricnames", new[] { "id", "name" });
            await ImportCsvFileAsync(connection, csvPath, "lookup_patterns.csv", "lookuppatterns", new[] { "id", "name" });
            await ImportCsvFileAsync(connection, csvPath, "fabric.csv", "fabrics", new[] { "article", "namecode", "colorcode", "patterncode", "imagepath", "compositioncode", "widthmm", "lengthmm", "unit", "price" });
            await ImportCsvFileAsync(connection, csvPath, "fabric_stock.csv", "fabricstock", new[] { "rollid", "fabricarticle", "lengthmm", "widthmm", "unit" });
            await ImportCsvFileAsync(connection, csvPath, "fitting_stock.csv", "fittingstock", new[] { "batchid", "fittingarticle", "quantity" });
        }

        private string GetCsvPath()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(assemblyDirectory, "Data");
        }

        private async Task ImportCsvFileAsync(NpgsqlConnection connection, string csvPath, string fileName, string tableName, string[] columns)
        {
            var filePath = Path.Combine(csvPath, fileName);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл {fileName} не найден, пропускаем импорт");
                return;
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                if (lines.Length <= 1) return; // Только заголовок или пустой файл

                for (int i = 1; i < lines.Length; i++) // Пропускаем заголовок
                {
                    var values = lines[i].Split(',');
                    if (values.Length >= columns.Length)
                    {
                        var placeholders = string.Join(",", Enumerable.Range(1, columns.Length).Select(x => $"${x}"));
                        var sql = $"INSERT INTO {tableName} ({string.Join(",", columns)}) VALUES ({placeholders}) ON CONFLICT DO NOTHING";

                        using var cmd = new NpgsqlCommand(sql, connection);
                        for (int j = 0; j < columns.Length; j++)
                        {
                            cmd.Parameters.AddWithValue(values[j].Trim());
                        }
                        await cmd.ExecuteNonQueryAsync();
                        cmd.Parameters.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка импорта файла {fileName}: {ex.Message}");
            }
        }

        // Остальные методы остаются без изменений...
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
            try
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки изделий: {ex.Message}", ex);
            }
            return goods;
        }
    }
}
