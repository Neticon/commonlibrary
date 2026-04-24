using CommonLibrary.Domain.Entities;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServicePortal.Application.Interfaces;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.SharedServices.Services
{
    public class BlockService : AppServiceBase, IBlockService
    {
        protected readonly IBlocksRepository _entityRepository;

        public BlockService(IBlocksRepository entityRepository, ICurrentUserService currentUserService) : base(currentUserService) { _entityRepository = entityRepository; }

        public async Task<ServiceResponse> GetBlocks(Object payload)
        {
            var json = JsonConvert.DeserializeObject<JObject>(payload.ToString());
            var resp = await _entityRepository.GetData(json, CurrentUser.OrgSecret);
            return new ServiceResponse { Result = resp };
        }


        public async Task<string> GetAvaliableBlocks(string venueId, string date, string service)
        {
            return await _entityRepository.GetAvaliableBlocks(venueId, date, service);
        }

        public async Task<ServiceResponse> CreateBlocks(JToken payload)
        {
            var array = payload is JToken je && je.Type == JTokenType.Array;
            if (array)
            {
                var blocks = new List<Block>();
                var json = JsonConvert.DeserializeObject<List<JObject>>(payload.ToString());
                foreach (var item in json)
                {
                    var block = JsonConvert.DeserializeObject<Block>(item["data"].ToString());
                    block.block_id = Guid.NewGuid();
                    block.create_bu = CurrentUser.Decr_Email;
                    blocks.Add(block);
                }
                var response = await _entityRepository.SaveEntityBulk(blocks, CurrentUser.OrgSecret, true);

                return new ServiceResponse { Result = response };
            }
            else
            {
                var block = JsonConvert.DeserializeObject<Block>(payload["data"].ToString());
                block.block_id = Guid.NewGuid();
                block.create_bu = CurrentUser.Decr_Email;
                var response = await _entityRepository.SaveEntity(block, CurrentUser.OrgSecret, true);

                return new ServiceResponse { Result = response };
            }
        }

        public async Task<ServiceResponse> UpdateBlocks(JToken payload)
        {
            var array = payload is JToken je && je.Type == JTokenType.Array;
            if (array)
            {
                var json = JsonConvert.DeserializeObject<List<JObject>>(payload.ToString());
                foreach (var item in json)
                {
                    item["data"]["modify_bu"] = CurrentUser.Decr_Email;
                    item["data"]["modify_dt"] = DateTime.UtcNow;
                }
                var response = await _entityRepository.UpdateEntityBulk(json.Cast<object>().ToList(), CurrentUser.OrgSecret, true);

                return new ServiceResponse { Result = response };
            }
            else
            {
                payload["data"]["modify_bu"] = CurrentUser.Decr_Email;
                payload["data"]["modify_dt"] = DateTime.UtcNow;
                var response = await _entityRepository.UpdateEntity(payload, CurrentUser.OrgSecret, true);

                return new ServiceResponse { Result = response };
            }
        }

        public async Task<ServiceResponse> DeleteBlocks(JToken payload)
        {
            var array = payload is JToken je && je.Type == JTokenType.Array;
            if (array)
            {
                var json = JsonConvert.DeserializeObject<List<JObject>>(payload.ToString());
                foreach (var item in json)
                {
                    item["data"]["delete_bu"] = CurrentUser.Decr_Email;
                    item["data"]["delete_dt"] = DateTime.UtcNow;
                    item["data"]["is_deleted"] = true;
                }
                var response = await _entityRepository.UpdateEntityBulk(json.Cast<object>().ToList(), CurrentUser.OrgSecret, true);

                return new ServiceResponse { Result = response };
            }
            else
            {
                payload["data"]["delete_bu"] = CurrentUser.Decr_Email;
                payload["data"]["delete_dt"] = DateTime.UtcNow;
                payload["data"]["is_deleted"] = true;
                var response = await _entityRepository.UpdateEntity(payload, CurrentUser.OrgSecret, true);

                return new ServiceResponse { Result = response };
            }
        }
    }
}
