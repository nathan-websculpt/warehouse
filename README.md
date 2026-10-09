## Prerequisites

- .NET 10 SDK
- SQL Server
- sqlcmd

## Setup

The API needs a `ConnectionStrings:Warehouse` value and database setup

`ConnectionStrings__Warehouse` is the environment-variable alternative

Example (`<database-name>` must match the database created by the setup scripts - currently `WarehouseDev`):

```powershell
dotnet user-secrets set --project .\src\Warehouse.Api `
  "ConnectionStrings:Warehouse" `
  "Server=localhost;Database=<database-name>;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
```

**NOTE:** `TrustServerCertificate=True` is for local-development only

## Starting fresh

In powershell

```powershell
Set-Location -LiteralPath 'C:\Users\<USER>\github\warehouse\database'

sqlcmd -S 'localhost' -E -b -i '.\bootstrap\001_create_database.sql'
if ($LASTEXITCODE -ne 0) {
    throw 'Database creation failed.'
}

sqlcmd -S 'localhost' -E -b -i '.\deploy\001_deploy.sql'
if ($LASTEXITCODE -ne 0) {
    throw 'Deployment failed.'
}
```

## Starting over

### Warning - drops database

In powershell

```powershell
Set-Location -LiteralPath 'C:\Users\<USER>\github\warehouse\database'

sqlcmd -S 'localhost' -E -b -i '.\reset\001_reset_database.sql'
if ($LASTEXITCODE -ne 0) {
    throw 'Database reset failed.'
}

sqlcmd -S 'localhost' -E -b -i '.\bootstrap\001_create_database.sql'
if ($LASTEXITCODE -ne 0) {
    throw 'Database creation failed.'
}

sqlcmd -S 'localhost' -E -b -i '.\deploy\001_deploy.sql'
if ($LASTEXITCODE -ne 0) {
    throw 'Deployment failed.'
}
```