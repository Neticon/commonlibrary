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
                var log = new LogsErr
                {
                    log_level = logLevel.ToString(),
                    event_type = "APPLICATION",
                    operation_type = operationType.ToString(),
                    message = message,
                    stage_name = "logging",
                    entity = entity
                };

                var resp = await _entityRepository.SaveEntity(log, returnError: true);
                if (!resp.success)
                    Console.WriteLine($"Error in WriteSystemLogProcess , error : {resp.message}, payload : {JsonConvert.SerializeObject(log)}");
            }
            catch(Exception ex) 
            {
                Console.WriteLine($"Error in WriteSystemLogProcess , exception : {ex.Message},{ex.StackTrace}");
            }
        }
    }
}
