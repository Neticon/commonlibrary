namespace CommonLibrary.Helpers
{
    public static class Constants
    {
        public static string PSQLTimestampWithTZFromat = "yyyy-MM-dd HH:mm:sszz";
    }

    public static class ImageUploadTypes
    {
        public static string Service = "service";
        public static string Cover = "cover";
        public static string Logo = "logo";
    }

    public static class CommonConstants
    {
        public static string Org_Code_Conventus = "CONVENTUS";
        public static List<string> ValidGetOperations = ["list", "detail", null];
        public static string Booking_Mode_Service = "SERVICE";
    }

    public static class ReminderStreamConstants
    {
        public const string StreamName = "reminders_stream";
        public const string GroupName = "reminder_workers";
    }

}
