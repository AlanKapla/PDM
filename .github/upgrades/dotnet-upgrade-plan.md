# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade src\Entities\Entities.csproj
4. Upgrade src\Repositiories\Repositiories.csproj
5. Upgrade src\Business\Business.csproj
6. Upgrade src\CQRS\CQRS.csproj
7. Upgrade src\WebApi\WebApi.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                           | Current Version | New Version | Description                                                    |
|:-------------------------------------------------------|:---------------:|:-----------:|:---------------------------------------------------------------|
| Azure.Identity                                         | 1.12.0          | 1.17.1      | Deprecated - takes dependency on deprecated MSAL version       |
| FluentValidation.AspNetCore                            | 11.3.1          | 11.3.1      | Deprecated - should be replaced                                |
| Microsoft.AspNetCore.Authentication.Google             | 8.0.8           | 10.0.1      | Recommended for .NET 10.0                                      |
| Microsoft.AspNetCore.Authentication.JwtBearer          | 8.0.8           | 10.0.1      | Recommended for .NET 10.0                                      |
| Microsoft.AspNetCore.Authorization                     | 8.0.2           | 10.0.1      | Recommended for .NET 10.0                                      |
| Microsoft.AspNetCore.Mvc                               | 2.3.0           |             | Functionality included with framework reference                |
| Microsoft.EntityFrameworkCore                          | 9.0.9           | 10.0.1      | Recommended for .NET 10.0                                      |
| Microsoft.EntityFrameworkCore.Design                   | 9.0.9           | 10.0.1      | Recommended for .NET 10.0                                      |
| Microsoft.EntityFrameworkCore.SqlServer                | 9.0.9           | 10.0.1      | Recommended for .NET 10.0                                      |
| Microsoft.EntityFrameworkCore.Tools                    | 9.0.9           | 10.0.1      | Recommended for .NET 10.0                                      |
| Microsoft.VisualStudio.Azure.Containers.Tools.Targets  | 1.22.1          |             | Incompatible - no supported version found                      |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### src\Entities\Entities.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.Google should be updated from `8.0.8` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Authorization should be updated from `8.0.2` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Design should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.VisualStudio.Azure.Containers.Tools.Targets should be removed (*incompatible - no supported version found*)

#### src\Repositiories\Repositiories.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.Google should be updated from `8.0.8` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Authorization should be updated from `8.0.2` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.VisualStudio.Azure.Containers.Tools.Targets should be removed (*incompatible - no supported version found*)

#### src\Business\Business.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Azure.Identity should be updated from `1.12.0` to `1.17.1` (*deprecated - takes dependency on deprecated MSAL version*)

#### src\CQRS\CQRS.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.Google should be updated from `8.0.8` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Authorization should be updated from `8.0.2` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)

#### src\WebApi\WebApi.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.Google should be updated from `8.0.8` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Authentication.JwtBearer should be updated from `8.0.8` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Authorization should be updated from `8.0.2` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `9.0.9` to `10.0.1` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Mvc should be removed (*functionality included with framework reference*)
  - Microsoft.VisualStudio.Azure.Containers.Tools.Targets should be removed (*incompatible - no supported version found*)
  - FluentValidation.AspNetCore version `11.3.1` is deprecated and should be evaluated for replacement
