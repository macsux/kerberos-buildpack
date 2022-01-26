This buildpack sets up Kerberos authentication on Linux, allowing Kerberos and Integrated Windows Authentication to work natively in .NET and Java apps. 

## How to use

Set the following environmental variables for your app:

`KRB_SERVICE_ACCOUNT`: service account under which app runs in `username@domain.com` format 
`KRB_PASSWORD`: service account password
`KRB5_KDC`: (optional) Kerberos Key Distribution Center address. Defaults to domain part of `KRB_SERVICE_ACCOUNT`

Ensure that you have right SPNs in place for the service you're trying to authenticate to.

Sample manifest:

```yaml
---
applications:
- name: TestKerberos
  path: bin/Debug/net5.0/publish
  random-route: true
  memory: 512M
  health-check-type: none
  buildpacks: 
    - https://github.com/macsux/kerberos-buildpack/releases/download/WIP/KerberosBuildpack-linux-x64-WIP.zip
    - dotnet_core_buildpack
  env:
    KRB5_KDC: ad.almirex.com
    KRB_SERVICE_ACCOUNT: iwaclient@almirex.dc
    KRB_PASSWORD: P@ssw0rd

```

## How it works

![](./docs/img/architecture.jpg)



Core libraries used by .NET and Java apps use MIT Kerberos to do Kerberos (aka Integrated) authentication when running on Linux. This buildpack configures MIT Kerberos, and obtains the necessary initial TGT tickets necessary for the app to acquire authentication tickets.

A sidecar runs in background that will obtain tickets Kerberos .NET 

## Troubleshooting

Recommendation is to start with sample app included, which exposes the folowing endpoints:
`/user` - which will authenticate incoming HTTP principal and print caller's identity. Simply call this endpoint on domain joined box from browser
`sql` - tests kerberos connection to SQL Server. Set connection string either in `appsettings.json` or via environmental variable `CONNECTIONSTRINGS__SQLSERVER`.
`/testkdc` - verify that connection can be established to KDC server on port 88.

After the app starts up you should see logs emitted from sidecar process that look like this:
```csharp
   2022-01-26T16:04:52.80-0500 [PROXY/0] OUT Exit status 137
   2022-01-26T16:04:52.87-0500 [APP/PROC/WEB/0] OUT info: Microsoft.Hosting.Lifetime[14]
   2022-01-26T16:04:52.87-0500 [APP/PROC/WEB/0] OUT       Now listening on: http://0.0.0.0:9090
   2022-01-26T16:04:52.88-0500 [APP/PROC/WEB/0] OUT info: Microsoft.Hosting.Lifetime[0]
   2022-01-26T16:04:52.88-0500 [APP/PROC/WEB/0] OUT       Application started. Press Ctrl+C to shut down.
   2022-01-26T16:04:52.88-0500 [APP/PROC/WEB/0] OUT info: Microsoft.Hosting.Lifetime[0]
   2022-01-26T16:04:52.88-0500 [APP/PROC/WEB/0] OUT       Hosting environment: Production
   2022-01-26T16:04:52.88-0500 [APP/PROC/WEB/0] OUT info: Microsoft.Hosting.Lifetime[0]
   2022-01-26T16:04:52.88-0500 [APP/PROC/WEB/0] OUT       Content root path: /home/vcap/app/
   2022-01-26T16:04:53.23-0500 [APP/PROC/WEB/0] OUT info: KerberosSidecar.KerberosWorker[0]
   2022-01-26T16:04:53.23-0500 [APP/PROC/WEB/0] OUT       Service authenticated successfully as 'iwaclient'
   2022-01-26T16:04:53.24-0500 [APP/PROC/WEB/0] OUT info: KerberosSidecar.Spn.LoggingSpnClient[0]
   2022-01-26T16:04:53.24-0500 [APP/PROC/WEB/0] OUT       Ensure that the following SPN for the service exists: http/kerberosdemo.apps.longbeach.cf-app.com
```

If you have not received a message similar to `Service authenticated successfully as 'iwaclient'`, it means that the worker sidecar has been unable to obtain ticket from your KDC.

#### Things to check 

- Sidecar process started, as indicated by log entry containing `Now listening on: http://0.0.0.0:9090`. (note port 9090 - there maybe similar log entry but for port 8080 - that one is for the main app).
- Credentials are correct and specified in the right format
- KDC is accessible from the container. Use `/testkdc` endpoint of sample app to test.
- Any other errors coming from the logs