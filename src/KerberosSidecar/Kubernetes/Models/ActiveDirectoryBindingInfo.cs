namespace KerberosSidecar.Kubernetes.Models;

public class ActiveDirectoryBindingInfo
{
    public string Type { get; set; } = null!;
    public string? Host { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}