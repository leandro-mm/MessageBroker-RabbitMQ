namespace WebApp.EntryPoint.Settings
{
    public static class HostSettings
    {
        public static string GetHost(string defaultHost) => 
            System.Environment.OSVersion.Platform == PlatformID.Unix ? defaultHost : "localhost";
    }
}