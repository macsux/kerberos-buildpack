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
  memory: 256M
  health-check-type: none
  buildpacks: 
    - https://github.com/macsux/kerberos-buildpack/releases/download/v0.1.0/KerberosBuildpack-linux-x64-0.1.0.zip
    - dotnet_core_buildpack
  env:
    KRB5_KDC: ad.almirex.com
    KRB_SERVICE_ACCOUNT: iwaclient@almirex.dc
    KRB_PASSWORD: P@ssw0rd

```

## How it works
Core libraries used by .NET and Java apps use MIT Kerberos to do Kerberos (aka Integrated) authentication when running on Linux. This buildpack configures MIT Kerberos, and obtains the necessary initial TGT tickets necessary for the app to acquire authentication tickets.

A sidecar runs in background that will obtain tickets Kerberos .NET 