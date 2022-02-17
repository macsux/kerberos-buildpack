#nullable disable
using JetBrains.Annotations;

namespace KerberosSidecar.CloudFoundry;

[PublicAPI]
public class ServiceCredentials
{
    public string ServiceAccount { get; set; }
    public string Password { get; set; }
}