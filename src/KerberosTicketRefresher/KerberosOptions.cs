using Kerberos.NET.Credentials;

namespace KerberosTicketRefresher;

public class KerberosOptions
{
    public string Kerb5ConfigFile { get; set; } = null!;
    public string ServiceAccount { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string CacheFile { get; set; } = null!;
    
    // public KerberosPasswordCredential KerberosCredentials { get; set; }
    public bool RunOnce { get; set; }
}