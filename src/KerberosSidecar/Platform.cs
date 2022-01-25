namespace KerberosSidecar;

public static class Platform
{
    public static bool IsCloudFoundry => Environment.GetEnvironmentVariable("VCAP_SERVICES") != null;
}