# Experiments to get Container Credentials Manager working with custom plugin. 

### Goal: 

Implement a C# plugin that is invoked by CCM to provide credentials for container launch on non-domain joined machine. The process is documented in the following articles:

https://docs.microsoft.com/en-us/virtualization/windowscontainers/manage-containers/manage-serviceaccounts

https://docs.microsoft.com/en-us/windows/win32/api/ccgplugins/nf-ccgplugins-iccgdomainauthcredentials-getpasswordcredentials

### Current problem: 

Cannot get CCM to activate COM plugin component

### What has been tried:

1. Implemented COM interface as C# project

2. Compile in x64 mode (Debug config)

3. Register to COM from `bin\Debug` folder via `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe CcgPlugin.dll /tlb /codebase`

4. Add COM registration to CCG

   ```
   Windows Registry Editor Version 5.00
   
   [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\CCG\COMClasses\{DEFFF03C-3245-465F-8391-CC586A2D1F32}]
   ```

   

5. Place `ccg.json` into `C:\ProgramData\Docker\credentialspecs`

6. Try to start container with `docker run --security-opt "credentialspec=file://ccg.json" --hostname webapp01 -it mcr.microsoft.com/windows/servercore:ltsc2019 powershell`

### Expected result:

1. Some kinda logs created in `c:\temp` (first on component activation, and method invocation)

### Actual result:

1. No logs in `c:\temp`

## Additional info

### Validate COM registration

COM registration seems to be successful, as it can be activated like this:

```c#
 var otherType = Type.GetTypeFromCLSID(new Guid("DEFFF03C-3245-465F-8391-CC586A2D1F32"));
var otherInstance = Activator.CreateInstance(otherType).Dump();
object[] args = new object[] { "test","","","" };
ParameterModifier pMod = new ParameterModifier(4);
pMod[1] = true;
pMod[2] = true;
pMod[3] = true;
ParameterModifier[] mods = { pMod };

otherType.InvokeMember("GetPasswordCredentials", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public , null, otherInstance, args, mods, null, null);

```

### Confirm correct COM signature

1. Windows SDK installed to get original `IDL` for `ccgplugins`. Necessary files are in `C:\Program Files (x86)\Windows Kits\10\Include\10.0.20348.0\um`

2. IDL converted to `TLB` as following:

   ```
   midl ccgplugins-lib.idl /tlb ccgplugins.tlb
   
   ```

3. `TLB` converted to `.NET` assembly DLL via `tlbimp ccgplugins.tlb /out:ccgplugins-net.dll`

   ```
   tlbimp ccgplugins.tlb /out:ccgplugins-net.dll
   ```

4. Reverse decompilation of the resulting DLL via Jetbrains DotPeek confirms that the correct signature for the interface as:

   ```c#
   namespace ccgplugins\u002Dnet
   {
     [Guid("6ECDA518-2010-4437-8BC3-46E752B7B172")]
     [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
     [ComImport]
     public interface ICcgDomainAuthCredentials
     {
       [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
       void GetPasswordCredentials(
         [MarshalAs(UnmanagedType.LPWStr), In] string pluginInput,
         [MarshalAs(UnmanagedType.LPWStr)] out string domainName,
         [MarshalAs(UnmanagedType.LPWStr)] out string username,
         [MarshalAs(UnmanagedType.LPWStr)] out string password);
     }
   }
   
   ```

   

### AKS plugin

It was confirmed that AKS plugin that implements the above interface does work and is able to be spun up and start container with GMSA creds as per this article. https://docs.microsoft.com/en-us/azure/aks/use-group-managed-service-accounts

### Other observations

Given that COM plugin has constructor logic that writes a marker file to `c:\temp`, the lack of said file when attempting to start container with `--security-opt` would imply that COM method signature conforming to an interface is not a problem, as COM object would be instantiated before the method call. Since it's not being created, seems like CCG subsystem is not picking up the plugin registration properly. 

The experiment was repeated by copying the DLL to an AKS node where Azure's Vault CCG plugin is installed, registering it and adding the necessary CCG registry entries. No COM activation was observed in that environment either.

## Primary test Environment

```
PS C:\projects> [Environment]::OSVersion

Platform ServicePack Version      VersionString
-------- ----------- -------      -------------
 Win32NT             10.0.19043.0 Microsoft Windows NT 10.0.19043.0


PS C:\projects> docker version
Client:
 Cloud integration: v1.0.20
 Version:           20.10.10
 API version:       1.41
 Go version:        go1.16.9
 Git commit:        b485636
 Built:             Mon Oct 25 07:47:53 2021
 OS/Arch:           windows/amd64
 Context:           default
 Experimental:      true

Server: Docker Engine - Community
 Engine:
  Version:          20.10.10
  API version:      1.41 (minimum version 1.24)
  Go version:       go1.16.9
  Git commit:       e2f740d
  Built:            Mon Oct 25 07:43:13 2021
  OS/Arch:          windows/amd64
  Experimental:     false
PS C:\projects>
```

