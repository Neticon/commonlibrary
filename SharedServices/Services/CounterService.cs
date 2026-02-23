using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;

namespace CommonLibrary.SharedServices.Services
{
    public class CounterService : ICounterService
    {
        private readonly IRedisService _redisService;
        private readonly IGenericEntityRepository<AppCounters> _genericRepo;
        private readonly List<string> ValidApiActions = new List<string> { "mod", "mod_sel", "block_sel", "form_dis" };

        public CounterService(IRedisService redisService, IGenericEntityRepository<AppCounters> genericRepo)
        {
            _redisService = redisService;
            _genericRepo = genericRepo;
        }

        public async Task ProcessCounter(string tenantId, string venueId, string path, int code, string method = "")
        {
            var field = GetFieldName(path, code, method);
            await TriggerCounter(tenantId, venueId, field);
        }

        public async Task<bool> TriggerCounter(string tenantId, string venueId, string field, bool apiCall = false)
        {
            if (apiCall)
            {
                var validAction = IsActionValid(field);
                if (!validAction)
                    return false;
            }
            var dateNow = DateTime.UtcNow.ToString("yyyy:MM:dd");
            var key = $"{tenantId ?? "null"}:{venueId ?? "null"}:{dateNow}";
            await _redisService.ExecuteHashIncrement(key, field, 1);
            return true;
        }

        public async Task SyncRedisToDatabase()
        {
            var searchDate = DateTime.UtcNow.AddDays(-1);
            var dateRedis = searchDate.ToString("yyyy:MM:dd");
            var dateDb = searchDate.ToString("yyyy.MM.dd");
            var hashValues = await _redisService.GetHashValuesByPattern($"*{dateRedis}");
            foreach (var value in hashValues)
            {
                var parts = value.Key.Split(':');
                var tenantId = parts[0];
                if (tenantId == "null")
                    continue;
                var venueId = parts[1];
                var appCounter = new AppCounters { id = Guid.NewGuid(), date = dateDb, tenant_id = new Guid(tenantId), venue_id = venueId != "null" ? new Guid(venueId) : null };
                foreach (var item in value.Value)
                {
                    try
                    {
                        appCounter.SetPropertyValue(item.Name, item.Value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                try
                {
                    await _genericRepo.SaveEntity(appCounter);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private bool IsActionValid(string action)
        {
            return ValidApiActions.Contains(action);
        }

        private string GetFieldName(string path, int code, string method)
        {
            if (method == "PUT")
            {
                path = $"{path}-put";
            }
            var key = PathKeys[path];
            var finalCode = code.ToString();
            var field = key;
            if (code >= 500)
            {
                finalCode = "5xx";
            }
            if (code != 200)
            {
                field = $"{field}_err_{finalCode}";
            }
            var addOk = AddOk(path);
            return addOk ? $"{field}_ok" : field;
        }

        private Dictionary<string, string> PathKeys = new Dictionary<string, string>()
        {
            {"vas","key" },
            {"res","renew" },
            {"blk", "venue" },
            {"book", "book" },
            {"book-put","mod" }
        };

        private bool AddOk(string api)
        {
            var res = false;
            if (api == "book" || api == "blk")
                res = true;
            return res;
        }
    }
}
