public static class ServerConfig
{
#if UNITY_EDITOR
    public static string HostIP = "http://localhost:5000"; // Use localhost in Editor
#else
    public static string HostIP = "http://192.168.0.178:5000"; // Replace with your PC's IP for phone testing
#endif

    public static string Get(string path) => $"{HostIP}/{path}";
}
