using CommonLibrary.Helpers;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;
using StackExchange.Redis;

namespace CommonLibrary.SharedServices.Services
{
    public class BookingNotificationQueue : IBookingNotificationQueue
    {
        private readonly IDatabase _redisDB;

        public BookingNotificationQueue(IRedisService redisService)
        {
            _redisDB = redisService.GetDatabase();
        }

        public async Task RemoveBookingNotificationsAsync(string bookingId)
        {
            var entries = await _redisDB.StreamRangeAsync(ReminderStreamConstants.StreamName, "-", "+");
            var idsToRemove = entries
                .Where(e => e.Values.Any(v => v.Name == "booking_id" && v.Value == bookingId))
                .Select(e => e.Id)
                .ToArray();

            foreach (var id in idsToRemove)
            {
                try
                {
                    await _redisDB.StreamAcknowledgeAsync(ReminderStreamConstants.StreamName, ReminderStreamConstants.GroupName, id);
                    await _redisDB.StreamDeleteAsync(ReminderStreamConstants.StreamName, new[] { id });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RemoveBookingNotificationsAsync failed for id {id}: {ex.Message}");
                }
            }
        }
    }
}
