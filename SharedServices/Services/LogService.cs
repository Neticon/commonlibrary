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

        public LogService(IGenericRepository<JObject> repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResponse> WriteLog(LogModel payload)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.WRITE_ERROR_LOG);
            query.Parameters.AddWithValue("payload", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload.data));

            var resp = await _repository.ExecuteStandardCommand(query);

            return new ServiceResponse { Result = resp };
        }
    }
}
