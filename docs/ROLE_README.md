**Role Support (Dapper + ASP.NET Identity v2)**

- **파일 위치**: `Sql/IdentityTables.sql` — 역할 관련 테이블 생성 스크립트

- **요약**: 이 저장소는 사용자 저장소를 Dapper로 구현(`Infrastructure/DapperUserStore.cs`)했고, 역할(Role) 지원을 위해 `Models/ApplicationRole`, `Infrastructure/DapperRoleStore`, `App_Start/IdentityConfig.cs`의 `ApplicationRoleManager`, 및 역할 관리 컨트롤러/뷰(`Controllers/RoleAdminController`, `Views/RoleAdmin/*`)를 추가했습니다.

- **테이블**:
  - `AspNetRoles(Id nvarchar(128) PK, Name nvarchar(256))`
  - `AspNetUserRoles(UserId nvarchar(128), RoleId nvarchar(128))` — 복합 PK(UserId, RoleId), `AspNetUsers(Id)` 및 `AspNetRoles(Id)`에 FK

- **DB 적용 방법**:
  1. 데이터베이스 백업.
  2. `Sql/IdentityTables.sql` 파일을 해당 DB에 대해 실행(SSMS 또는 `sqlcmd`).
     예: `sqlcmd -S .\SQLEXPRESS -d MyDatabase -i Sql\IdentityTables.sql -U sa -P YourPassword`

- **역할 생성/확인 예시 (C#)**:
  - OWIN 컨텍스트에서 `ApplicationRoleManager`를 가져와 사용합니다.

```csharp
// RoleManager 가져오기
var roleManager = HttpContext.GetOwinContext().Get<ApplicationRoleManager>();

// 역할 존재 확인 및 생성
if (!await roleManager.RoleExistsAsync("Admin"))
{
    await roleManager.CreateAsync(new WebApp.Models.ApplicationRole("Admin"));
}
```

- **사용자에게 역할 할당 (C#)**:

```csharp
var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
var user = await userManager.FindByNameAsync("someuser@example.com");
if (user != null)
{
    await userManager.AddToRoleAsync(user.Id, "Admin");
}
```

- **시드(관리자 계정) 예시**:
  - 프로젝트 시작 시 Seed 방식으로 관리자 역할과 관리자 계정(및 역할 할당)을 추가하면 편리합니다. 예:

```csharp
// Ensure Admin role exists
var roleManager = HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
if (!await roleManager.RoleExistsAsync("Admin"))
    await roleManager.CreateAsync(new ApplicationRole("Admin"));

// Ensure admin user exists and is in Admin role
var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
var admin = await userManager.FindByNameAsync("admin@contoso.com");
if (admin == null)
{
    admin = new ApplicationUser { UserName = "admin@contoso.com", Email = "admin@contoso.com", EmailConfirmed = true };
    await userManager.CreateAsync(admin, "StrongP@ssw0rd!");
}
if (!await userManager.IsInRoleAsync(admin.Id, "Admin"))
{
    await userManager.AddToRoleAsync(admin.Id, "Admin");
}
```

- **주의사항**:
  - `AspNetUsers` 테이블이 이미 존재해야 합니다. (원본 프로젝트는 이미 생성되어 있어야 함)
  - `AspNetRoles.Id`와 `AspNetUsers.Id` 타입은 `nvarchar(128)`로 가정합니다. 기존 프로젝트에서 `Id` 타입이 다르면 스크립트를 조정하세요.
  - 스크립트의 `NEWID()`는 `AspNetRoles.Id`가 GUID 문자열로 저장될 때 사용합니다. 기존 `AspNetRoles.Id` 길이/형식에 맞춰 변경하세요.

질문이나 추가로 원하는 기능(예: 역할 관리 페이지 확장, 관리자 권한 Seeder 자동 호출 등)이 있으면 알려주세요.
