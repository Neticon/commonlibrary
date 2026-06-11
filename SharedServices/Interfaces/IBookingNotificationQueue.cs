namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IBookingNotificationQueue
    {
        Task RemoveBookingNotificationsAsync(string bookingId);
    }
}
