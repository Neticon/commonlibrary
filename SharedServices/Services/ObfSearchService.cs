using CommonLibrary.Helpers;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace CommonLibrary.SharedServices.Services
{
    public class ObfSearchService : IObfSearchService
    {
        public IGenericRepository<OBFSearchResponse> _repository { get; set; }
        public async Task<OBFSearchResponse> Search(ObfSearchModel searchModel)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.OBF_INDEX_SEARCH_AND_FETCH);
            query.Parameters.AddWithValue("@payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(searchModel));

            var resp = await _repository.ExecuteCommandTyped(query);
            return resp;
        }
    }
}
