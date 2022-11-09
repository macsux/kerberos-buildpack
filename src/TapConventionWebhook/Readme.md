# Kerberos Convention

[Creating Convention Docs](https://docs.vmware.com/en/VMware-Tanzu-Application-Platform/1.3/tap/GUID-cartographer-conventions-creating-conventions.html)

## Helpful Commands
```bash

tanzu apps workload apply --file config/workload.yaml  \
--namespace cody \
--source-image winterfell2.azurecr.io/supply-chain-basic/kerberos-convention-server-source \
--local-path . \
--tail 

tanzu apps workload get kerberos-convention-server -n cody
```