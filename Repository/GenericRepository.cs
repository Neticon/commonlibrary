using CommonLibrary.Domain.PSQL;
using CommonLibrary.Exceptions;
using CommonLibrary.Helpers;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using ServicePortal.Domain.PSQL;
using System.Text;

namespace ServicePortal.API.Infrastructure.Repository
{
    public class GenericRepository<T> : IGenericRepository<T>
    {
        protected readonly string _connectionString;

        public GenericRepository(IConfiguration config)
        {
            // Console.WriteLine("TEST CONSTRUCTOR=" + Environment.GetEnvironmentVariable("PSQLDB_CONNECTION_STRING"));
            _connectionString = Environment.GetEnvironmentVariable("PSQLDB_CONNECTION_STRING");
        }

        public async Task<GraphAPIResponse<T>> ExecuteStandardCommand(NpgsqlCommand query, bool returnError = false)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = query)
                {
                    try
                    {
                        query.Connection = conn;
                        var commandResult = await command.ExecuteReaderAsync();
                        while (await commandResult.ReadAsync())
                        {
                            var psqlResult = JsonConvert.DeserializeObject<GraphAPIResponse<T>>(commandResult.GetValue(0).ToString());
                            if (!psqlResult.success && !returnError)
                            {
                                if (psqlResult.stage == "validation" || psqlResult.stage == "operation")
                                    throw new PsqlResponseFailException(JsonConvert.SerializeObject(psqlResult));
                                else
                                    throw new Exception("PSQL Error");
                            }
                            return psqlResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return default;
        }

        public async Task<GraphAPIResponse<T>> ExecuteStandardCommand(string query)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new NpgsqlCommand(query))
                {
                    try
                    {
                        var commandResult = await command.ExecuteReaderAsync();
                        while (await commandResult.ReadAsync())
                        {
                            var psqlResult = JsonConvert.DeserializeObject<GraphAPIResponse<T>>(commandResult.GetValue(0).ToString());
                            if (!psqlResult.success)
                            {
                                if (psqlResult.stage == "validation" || psqlResult.stage == "operation")
                                    throw new PsqlResponseFailException(JsonConvert.SerializeObject(psqlResult));
                                else
                                    throw new Exception("PSQL Error");
                            }
                            return psqlResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return default;
        }

        public async Task<DoSelectOperationResponse<T>> ExecuteDoSelectCommand(string query)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new NpgsqlCommand(query, conn))
                {
                    try
                    {
                        var commandResult = await command.ExecuteReaderAsync();
                        while (await commandResult.ReadAsync())
                        {
                            var psqlResult = JsonConvert.DeserializeObject<DoSelectOperationResponse<T>>(commandResult.GetValue(0).ToString());
                            if (!psqlResult.success)
                                throw new PsqlResponseFailException(JsonConvert.SerializeObject(psqlResult));
                            return psqlResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return default;
        }

        public async Task<DoSelectOperationResponse<JObject>> ExecuteDoSelectCommandObject(string query)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new NpgsqlCommand(query, conn))
                {
                    try
                    {
                        var commandResult = await command.ExecuteReaderAsync();
                        while (await commandResult.ReadAsync())
                        {
                            var psqlResult = JsonConvert.DeserializeObject<DoSelectOperationResponse<JObject>>(commandResult.GetValue(0).ToString());
                            if (!psqlResult.success)
                                throw new PsqlResponseFailException(JsonConvert.SerializeObject(psqlResult));
                            return psqlResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return default;
        }

        public async Task ExecuteCommandVoid(string query)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new NpgsqlCommand(query, conn))
                {
                    var commandResult = await command.ExecuteReaderAsync();
                }
            }
        }

        public async Task<string> ExecuteCommandString(string query)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new NpgsqlCommand(query, conn))
                {
                    try
                    {
                        var commandResult = await command.ExecuteReaderAsync();
                        while (commandResult.Read())
                        {
                            return commandResult.GetValue(0).ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return default;
        }

        public async Task<string> ExecuteCommandString(NpgsqlCommand query)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = query)
                {
                    query.Connection = conn;
                    try
                    {
                        var commandResult = await command.ExecuteReaderAsync();
                        while (commandResult.Read())
                        {
                            return commandResult.GetValue(0).ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return default;
        }

        public async Task<T> ExecuteCommandTyped(string query)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new NpgsqlCommand(query, conn))
                {
                    try
                    {
                        var commandResult = await command.ExecuteReaderAsync();
                        while (commandResult.Read())
                        {
                            return JsonConvert.DeserializeObject<T>(commandResult.GetValue(0).ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return default;
        }

        public string GenerateDoOperationsQuery(Dictionary<string, string> fields, Dictionary<string, string> filters, string schema, string table, DoOperationQueryType type)
        {
            var queryList = new List<string>();
            var datastring = new StringBuilder();
            foreach (var field in fields)
            {
                datastring.Append($"\"{field.Key}\":{field.Value},");
            }
            if (datastring.Length > 0)
            {
                var datapart = $"\"data\":{{{datastring.ToString().TrimEnd(',')}}}";
                queryList.Add(datapart);
            }
            var filtersstring = new StringBuilder();
            foreach (var field in filters)
            {
                filtersstring.Append($"\"{field.Key}\":{field.Value},");
            }
            if (filtersstring.Length > 0)
            {
                var datapart = $"\"filters\":{{{filtersstring.ToString().TrimEnd(',')}}}";
                queryList.Add(datapart);
            }

            var query = PredefinedQueryPatterns.DO_OPERATION_QUERY_PATTERN.Replace("-QUERY-", string.Join(",", queryList));
            query = query.Replace("-SCHEMA-", schema);
            query = query.Replace("-TABLE-", table);
            query = query.Replace("-QUERYTYPE-", type.ToString());
            return query;
        }

        public NpgsqlCommand GenerateDoOperationsQuery(Object queryObject, string schema, string table, DoOperationQueryType type)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.DO_OPERATION_QUERY_PATTERN);

            query.Parameters.AddWithValue("@query", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(queryObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            query.Parameters.AddWithValue("@schema", schema);
            query.Parameters.AddWithValue("@table", table);
            query.Parameters.AddWithValue("@querytype", type.ToString());
            return query;
        }

        public string GenerateDoSelectQuery(Dictionary<string, string> fields, Dictionary<string, string> filters, string schema, string table, int pageSize = 1, int page = 1)
        {
            var queryList = new List<string>();
            var datastring = new StringBuilder();
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    datastring.Append($"\"{field.Key}\":{field.Value},");
                }
                if (datastring.Length > 0)
                {
                    var datapart = $"\"data\":{{{datastring.ToString().TrimEnd(',')}}}";
                    queryList.Add(datapart);
                }
            }
            if (filters != null)
            {
                var filtersstring = new StringBuilder();
                foreach (var field in filters)
                {
                    filtersstring.Append($"\"{field.Key}\":{field.Value},");
                }
                if (filtersstring.Length > 0)
                {
                    var datapart = $"\"filters\":{{{filtersstring.ToString().TrimEnd(',')}}}";
                    queryList.Add(datapart);
                }
            }
            var pageSizeQuery = $"\"page_size\":{pageSize}";
            var pageQuery = $"\"page\":{page}";
            queryList.Add(pageSizeQuery);
            queryList.Add(pageQuery);

            var query = PredefinedQueryPatterns.DO_SELECT_QUERY_PATTERN.Replace("-QUERY-", string.Join(",", queryList));
            query = query.Replace("-SCHEMA-", schema);
            query = query.Replace("-TABLE-", table);
            return query;
        }

        public string GenerateDoSelectQuery(Object queryObject, string schema, string table)
        {
            var queryValue = queryObject.GetType() == typeof(string) ? queryObject.ToString() : JsonConvert.SerializeObject(queryObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var query = PredefinedQueryPatterns.DO_SELECT_QUERY_PATTERN.Replace("{-QUERY-}", queryValue);
            query = query.Replace("-SCHEMA-", schema);
            query = query.Replace("-TABLE-", table);
            return query;
        }

    }
}
