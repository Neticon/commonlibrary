using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace CommonLibrary.SharedServices.Services
{
    public class LogService : ILogService
    {
        private readonly IGenericRepository<JObject> _repository;
        private readonly IGenericEntityRepository<LogsErr> _entityRepository;

        public LogService(IGenericRepository<JObject> repository, IGenericEntityRepository<LogsErr> entityRepository)
        {
            _repository = repository;
            _entityRepository = entityRepository;
        }

        public async Task<ServiceResponse> WriteLog(LogModel payload)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.WRITE_ERROR_LOG);
            query.Parameters.AddWithValue("payload", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload));

            var resp = await _repository.ExecuteStandardCommand(query);

            return new ServiceResponse { Result = resp };
        }

        public async Task WriteSystemLog(LogLevel logLevel, OperationType operationType, string message, string entity)
        {
            try
            {
                var query = new NpgsqlCommand(PredefinedQueryPatterns.WRITE_ERROR_LOG);

                var log = new LogsErr
                {
                    log_level = logLevel.ToString(),
                    operation_type = operationType.ToString(),
                    message = message,
                    entity = entity
                };
                var payload = new GraphApiPayload { data = log, filters = query };
                var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

                query.Parameters.AddWithValue("payload", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload, settings));

                var resp = await _repository.ExecuteStandardCommand(query);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error in WriteSystemLogProcess , exception : {ex.Message},{ex.StackTrace}");
            }
        }
    }
}
