Sample app to demonstrate different IWA usage modes

### Prerequisites

- for end user authentication test (`/user` endpoint), create SPN for the account under which current service runs (same as `KRB_SERVICE_ACCOUNT`) to `http/<cloud foundry route>`. Example if this sample app is assigned route of https://kerberos-demo.apps.pcfone.io then SPN would be `http//kerberos-demo.apps.pcfone.io`. 
- for sql server test (`/sql` endpoint), ensure the following:
  - SQL Server is running under AD account
  - SQL Server is routable from cloud foundry
  - SQL Server is assigned correct SPN that matches it's FQDN as resolved by apps running on cloud foundry. **This may be different from FQDN as understood by machines on domain joined machines**. To ensure correct SPN, use the following procedure:
    - SSH into any running app on the cloud foundry: `cf ssh <appname>`
    - Find the IP Address of the sql server host: `nslookup <sqlserver-host>`
    - Do reverse DNS lookup on IP to find FQDN: `nslookup <sqlserver-ip>`    
    - Create SPN for account under which SQL server runs in this format: `MSSQLSvc/<FQDN>`
  - Configure SQL Server to use SSL and install necessary certificate. See [this article](https://www.mssqltips.com/sqlservertip/3299/how-to-configure-ssl-encryption-in-sql-server/) on the procedure.
    - **Note**: Certificate must match the routable address from the platform. Since SQL server SNI is not supported for SQL, certificate must have all all server urls that will be used to access it. Alternatively, disable SSL validation on the client by appending `TrustServerCertificate=True` to connection string.

### Deploying

1. Edit sample `manifest.yaml` with your own settings

2. From inside `KerberosDemo` project, run the following:

   ```
   dotnet publish -r linux-x64 --self-contained false
   cf push
   ```

### Endpoints:

`/user` - authenticates incoming caller via SPNEGO (Kerberos ticket via HTTP header)

`/sql - connects to SQL server using Integrated authentication
