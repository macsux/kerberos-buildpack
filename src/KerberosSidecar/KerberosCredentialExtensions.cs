using Kerberos.NET;
using Kerberos.NET.Client;
using Kerberos.NET.Credentials;
using Kerberos.NET.Entities;
using Kerberos.NET.Transport;

namespace KerberosSidecar;

public static class KerberosCredentialExtensions
{
    public static async Task LoadSalts(this KerberosCredential credential, CancellationToken cancellationToken)
    {
        if (credential.Configuration == null)
            throw new InvalidOperationException($"Can't load salts when {nameof(credential.Configuration)} is null");
        var asReqMessage = KrbAsReq.CreateAsReq(credential, AuthenticationOptions.Renewable);
        var asReq = asReqMessage.EncodeApplication();

        IEnumerable<KrbPaData>? paData = null;
        var transport = new KerberosTransportSelector(
            new IKerberosTransport[]
            {
                new TcpKerberosTransport(null),
                new UdpKerberosTransport(null),
                new HttpsKerberosTransport(null)
            },
            credential.Configuration,
            null
        )
        {
            ConnectTimeout = TimeSpan.FromSeconds(5)
        };
        try
        {
            var krbAsRep = await transport.SendMessage<KrbAsRep>(credential.Domain, asReq, cancellationToken);
            paData = krbAsRep.PaData;
        }
        catch (KerberosProtocolException pex)
        {
            paData = pex?.Error?.DecodePreAuthentication();
        }   
        if (paData != null)
        {
            credential.IncludePreAuthenticationHints(paData);
        }
    }
}