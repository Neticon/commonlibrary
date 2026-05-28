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
    }

}
