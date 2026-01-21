using CommonLibrary.Domain.Entities;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServicePortal.Application.Interfaces;
using ServicePortal.Domain.PSQL;
using System.Text.Json;

namespace CommonLibrary.SharedServices.Services
{
    public class BlockService : AppServiceBase, IBlockService
    {
        private readonly IGenericEntityRepository<Block> _entityRepository;

        public BlockService(IGenericEntityRepository<Block> entityRepository, ICurrentUserService currentUserService) : base(currentUserService) { _entityRepository = entityRepository; }

        public async Task<ServiceResponse> GetBlocks(Object payload)
        {
            var json = JsonConvert.DeserializeObject<JObject>(payload.ToString());
            var resp = await _entityRepository.GetData(json, CurrentUser.OrgSecret);
            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> DeleteBlocks(Object payload)
        {
            return await CheckIsBulkAndCallFunction(payload, DeleteBlock, CurrentUser.Email);
        }

        public async Task<ServiceResponse> CreateBlocks(Object payload)
        {
            return await CheckIsBulkAndCallFunction(payload, SaveBlock, CurrentUser.Email);
        }


        public async Task<ServiceResponse> UpdateBlocks(Object payload)
        {
            return await CheckIsBulkAndCallFunction(payload, UpdateBlock, CurrentUser.Email);
        }

        private async Task<GraphAPIResponse<Block>> SaveBlock(JObject data, string email)
        {
            var block = JsonConvert.DeserializeObject<Block>(data["data"].ToString());
            block.block_id = Guid.NewGuid();
            block.create_bu = email;
            return await _entityRepository.SaveEntity(block, "", true);
        }

        private async Task<GraphAPIResponse<Block>> UpdateBlock(JObject payload, string email)
        {
            payload["data"]["modify_bu"] = email;
            payload["data"]["modify_dt"] = DateTime.UtcNow;
            return await _entityRepository.UpdateEntity(payload, "", true);
        }

        private async Task<GraphAPIResponse<Block>> DeleteBlock(JObject payload, string email)
        {
            payload["data"]["delete_bu"] = email;
            payload["data"]["delete_dt"] = DateTime.UtcNow;
            payload["data"]["is_deleted"] = true;

            return await _entityRepository.UpdateEntity(payload, "", true);
        }

        private async Task<ServiceResponse> CheckIsBulkAndCallFunction(object payload, Func<JObject, string, Task<GraphAPIResponse<Block>>> function, string email)
        {
            var array = payload is JsonElement je && je.ValueKind == JsonValueKind.Array;
            if (array)
            {
                var response = new List<GraphAPIResponse<Block>>();
                var json = JsonConvert.DeserializeObject<List<JObject>>(payload.ToString());
                foreach (var item in json)
                {
                    response.Add(await function(item, email));
                }

                return new ServiceResponse { Result = response };
            }
            else
            {
                var json = JsonConvert.DeserializeObject<JObject>(payload.ToString());
                return new ServiceResponse { Result = await function(json, email) };
            }
        }
    }
}
