using CommonLibrary.Domain.PSQL;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using ServicePortal.API.Infrastructure.Repository;
using System.Text.Json;
using WebApp.API.Models;

namespace CommonLibrary.Repository
{
    public class BlocksRepository : GenericRepository<BlockAvailabilityResponse>, IBlocksRepository
    {
        public BlocksRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<BlockAvailabilityResponse> CheckBlocAvailability(int block_start, int block_end, string type, Guid venueId, string date, string service)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new NpgsqlCommand("SELECT utility.assert_blockAvailability(@venue_id, @block_range, @type, @date, @service)", conn))
                {
                    var blockRange = new NpgsqlRange<int>(block_start, true, block_end, true);
                    command.Parameters.AddWithValue("venue_id", NpgsqlDbType.Uuid, venueId );
                    command.Parameters.AddWithValue("block_range", blockRange);
                    command.Parameters.AddWithValue("type", NpgsqlDbType.Char, type);
                    command.Parameters.AddWithValue("date", NpgsqlDbType.Date, DateTime.Parse(date));
                    command.Parameters.AddWithValue("service", NpgsqlDbType.Text, service);
                    try
                    {
                        var commandResult = await command.ExecuteReaderAsync();
                        while (commandResult.Read())
                        {
                            return JsonSerializer.Deserialize<BlockAvailabilityResponse>(commandResult.GetValue(0).ToString());

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
    }
}
