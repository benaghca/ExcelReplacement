using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Npgsql;
using ExcelReplacement.Models;
using System.Linq;

namespace ExcelReplacement.Services
{
    public interface IDatabaseService
    {
        Task<DatabaseResult> TestConnectionAsync(DataSource dataSource);
        Task<DatabaseResult> GetTableNamesAsync(DataSource dataSource);
        Task<DatabaseResult> GetColumnNamesAsync(DataSource dataSource, string tableName);
        Task<DatabaseResult> ExecuteQueryAsync(DataSource dataSource, string query);
        Task<DatabaseResult> GetSampleDataAsync(DataSource dataSource, string tableName, int limit = 10);
    }

    public class DatabaseResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DataTable Data { get; set; }
        public List<string> StringList { get; set; } = new List<string>();
    }

    public class DatabaseService : IDatabaseService
    {
        public async Task<DatabaseResult> TestConnectionAsync(DataSource dataSource)
        {
            try
            {
                using (var connection = CreateConnection(dataSource))
                {
                    await connection.OpenAsync();
                    return new DatabaseResult { Success = true };
                }
            }
            catch (Exception ex)
            {
                return new DatabaseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DatabaseResult> GetTableNamesAsync(DataSource dataSource)
        {
            try
            {
                using (var connection = CreateConnection(dataSource))
                {
                    await connection.OpenAsync();
                    var tableNames = new List<string>();

                    switch (dataSource.Type)
                    {
                        case DataSourceType.SQLServer:
                            tableNames = await GetSqlServerTablesAsync(connection);
                            break;
                        case DataSourceType.MySQL:
                            tableNames = await GetMySqlTablesAsync(connection);
                            break;
                        case DataSourceType.PostgreSQL:
                            tableNames = await GetPostgreSqlTablesAsync(connection);
                            break;
                        case DataSourceType.SQLite:
                            tableNames = await GetSqliteTablesAsync(connection);
                            break;
                        default:
                            return new DatabaseResult
                            {
                                Success = false,
                                ErrorMessage = "Unsupported database type"
                            };
                    }

                    return new DatabaseResult
                    {
                        Success = true,
                        StringList = tableNames
                    };
                }
            }
            catch (Exception ex)
            {
                return new DatabaseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DatabaseResult> GetColumnNamesAsync(DataSource dataSource, string tableName)
        {
            try
            {
                using (var connection = CreateConnection(dataSource))
                {
                    await connection.OpenAsync();
                    var columnNames = new List<string>();

                    switch (dataSource.Type)
                    {
                        case DataSourceType.SQLServer:
                            columnNames = await GetSqlServerColumnsAsync(connection, tableName);
                            break;
                        case DataSourceType.MySQL:
                            columnNames = await GetMySqlColumnsAsync(connection, tableName);
                            break;
                        case DataSourceType.PostgreSQL:
                            columnNames = await GetPostgreSqlColumnsAsync(connection, tableName);
                            break;
                        case DataSourceType.SQLite:
                            columnNames = await GetSqliteColumnsAsync(connection, tableName);
                            break;
                        default:
                            return new DatabaseResult
                            {
                                Success = false,
                                ErrorMessage = "Unsupported database type"
                            };
                    }

                    return new DatabaseResult
                    {
                        Success = true,
                        StringList = columnNames
                    };
                }
            }
            catch (Exception ex)
            {
                return new DatabaseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DatabaseResult> ExecuteQueryAsync(DataSource dataSource, string query)
        {
            try
            {
                using (var connection = CreateConnection(dataSource))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        using (var adapter = CreateDataAdapter(command))
                        {
                            var dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            
                            return new DatabaseResult
                            {
                                Success = true,
                                Data = dataTable
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new DatabaseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DatabaseResult> GetSampleDataAsync(DataSource dataSource, string tableName, int limit = 10)
        {
            var query = $"SELECT * FROM {tableName} LIMIT {limit}";
            return await ExecuteQueryAsync(dataSource, query);
        }

        private IDbConnection CreateConnection(DataSource dataSource)
        {
            return dataSource.Type switch
            {
                DataSourceType.SQLServer => new SqlConnection(dataSource.ConnectionString),
                DataSourceType.MySQL => new MySqlConnection(dataSource.ConnectionString),
                DataSourceType.PostgreSQL => new NpgsqlConnection(dataSource.ConnectionString),
                DataSourceType.SQLite => new SQLiteConnection(dataSource.ConnectionString),
                _ => throw new NotSupportedException($"Database type {dataSource.Type} is not supported")
            };
        }

        private IDbDataAdapter CreateDataAdapter(IDbCommand command)
        {
            return command.Connection.GetType().Name switch
            {
                nameof(SqlConnection) => new SqlDataAdapter((SqlCommand)command),
                nameof(MySqlConnection) => new MySqlDataAdapter((MySqlCommand)command),
                nameof(NpgsqlConnection) => new NpgsqlDataAdapter((NpgsqlCommand)command),
                nameof(SQLiteConnection) => new SQLiteDataAdapter((SQLiteCommand)command),
                _ => throw new NotSupportedException("Unsupported connection type")
            };
        }

        private async Task<List<string>> GetSqlServerTablesAsync(IDbConnection connection)
        {
            var query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var tables = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                    return tables;
                }
            }
        }

        private async Task<List<string>> GetMySqlTablesAsync(IDbConnection connection)
        {
            var query = "SHOW TABLES";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var tables = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                    return tables;
                }
            }
        }

        private async Task<List<string>> GetPostgreSqlTablesAsync(IDbConnection connection)
        {
            var query = "SELECT tablename FROM pg_tables WHERE schemaname = 'public'";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var tables = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                    return tables;
                }
            }
        }

        private async Task<List<string>> GetSqliteTablesAsync(IDbConnection connection)
        {
            var query = "SELECT name FROM sqlite_master WHERE type='table'";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var tables = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                    return tables;
                }
            }
        }

        private async Task<List<string>> GetSqlServerColumnsAsync(IDbConnection connection, string tableName)
        {
            var query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var columns = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        columns.Add(reader.GetString(0));
                    }
                    return columns;
                }
            }
        }

        private async Task<List<string>> GetMySqlColumnsAsync(IDbConnection connection, string tableName)
        {
            var query = $"SHOW COLUMNS FROM {tableName}";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var columns = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        columns.Add(reader.GetString(0));
                    }
                    return columns;
                }
            }
        }

        private async Task<List<string>> GetPostgreSqlColumnsAsync(IDbConnection connection, string tableName)
        {
            var query = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}'";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var columns = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        columns.Add(reader.GetString(0));
                    }
                    return columns;
                }
            }
        }

        private async Task<List<string>> GetSqliteColumnsAsync(IDbConnection connection, string tableName)
        {
            var query = $"PRAGMA table_info({tableName})";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var columns = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        columns.Add(reader.GetString(1)); // Column name is at index 1
                    }
                    return columns;
                }
            }
        }
    }
}
