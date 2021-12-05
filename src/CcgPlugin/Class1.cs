using System.Runtime.InteropServices;

namespace CcgPlugin
{
    [Guid("6ecda518-2010-4437-8bc3-46e752b7b172")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]

    public interface ICcgDomainAuthCredentials
    {
        int GetPasswordCredentials (in string pluginInput, out string domainName, out string username, out string password);
    }
    [Guid("defff03c-3245-465f-8391-cc586a2d1f31")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CcgCredProvider")]
    public class CcgCredProvider :ICcgDomainAuthCredentials
    {
        public int GetPasswordCredentials(in string pluginInput, out string domainName, out string username, out string password)
        {
            domainName = "mydomain.com";
            username = "myser";
            password = "mypassword";
            return 0;
        }
    }
}