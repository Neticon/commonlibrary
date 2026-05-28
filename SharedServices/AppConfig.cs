namespace CommonLibrary.SharedServices
{
    public enum AppType
    {
        ServicePortal,
        Helpdesk
    }

    public static class AppConfig
    {
        public static AppType AppType { get; set; } = AppType.ServicePortal;
    }
}
