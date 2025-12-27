# aspnetmvc5-identity-dapper

이 저장소는 ASP.NET MVC 5 기본 템플릿의 Microsoft.AspNet.Identity Entity Framework 구현을 Dapper 기반 저장소로 대체한 예제 프로젝트입니다. Dapper로 Identity 사용자(로그인, 역할, 클레임 등)를 직접 SQL로 관리하는 방법을 보여줍니다.

핵심 파일:
- `Models/ApplicationUser.cs`: 사용자 모델
- `Infrastructure/DapperUserStore.cs`: Dapper로 구현한 `IUserStore` 계열 인터페이스
- `App_Start/IdentityConfig.cs`, `App_Start/Startup.Auth.cs`: Dapper 기반으로 구성된 `UserManager`/`SignInManager`

목표: Entity Framework에 대한 의존성을 제거하고, Dapper를 이용해 쿼리 제어와 성능을 향상시키는 방법을 제시합니다.

**요구 사항**
- Visual Studio 2017 이상 또는 MSBuild가 있는 개발 환경
- .NET Framework 4.7.2 (프로젝트의 `TargetFrameworkVersion` 기준)
- SQL Server (LocalDB 포함)
- NuGet 패키지: `Dapper`, `Microsoft.AspNet.Identity.Core`, `Microsoft.AspNet.Identity.Owin`, `Microsoft.Owin.*` (프로젝트에 이미 참조됨)

## 빠른 시작
1. 저장소를 클론합니다 (예):

	git clone https://github.com/shimpark/aspnetmvc5-identity-dapper.git

2. Visual Studio에서 `WebApp.sln`을 엽니다.
3. NuGet 패키지를 복원합니다 (Visual Studio 자동 복원 또는 `nuget restore`).
4. `Web.config`의 `DefaultConnection` 연결 문자열을 실제 환경에 맞게 수정하세요.
	- 기본값은 LocalDB에 있는 `App_Data` 폴더의 `.mdf` 파일을 첨부하는 형태입니다. (예: `AttachDbFilename=|DataDirectory|\aspnet-WebApp-20251125121651.mdf`)
5. 데이터베이스에 Identity 테이블이 없다면 아래 예제 스키마를 참고해 생성하세요.
6. 프로젝트를 빌드하고 `/Account/Register` 등으로 테스트하세요.

## 예제 데이터베이스 스키마
아래는 `DapperUserStore`와 호환되는 최소한의 테이블 예시입니다. 필요에 따라 컬럼/인덱스를 확장하세요.

```sql
CREATE TABLE AspNetUsers (
	Id NVARCHAR(128) NOT NULL PRIMARY KEY,
	UserName NVARCHAR(256) NULL,
	Email NVARCHAR(256) NULL,
	EmailConfirmed BIT NOT NULL DEFAULT 0,
	PasswordHash NVARCHAR(MAX) NULL,
	SecurityStamp NVARCHAR(MAX) NULL,
	PhoneNumber NVARCHAR(50) NULL,
	PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
	TwoFactorEnabled BIT NOT NULL DEFAULT 0,
	LockoutEndDateUtc DATETIME NULL,
	LockoutEnabled BIT NOT NULL DEFAULT 0,
	AccessFailedCount INT NOT NULL DEFAULT 0
);

CREATE TABLE AspNetRoles (
	Id NVARCHAR(128) NOT NULL PRIMARY KEY,
	Name NVARCHAR(256) NOT NULL
);

CREATE TABLE AspNetUserRoles (
	UserId NVARCHAR(128) NOT NULL,
	RoleId NVARCHAR(128) NOT NULL,
	PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE AspNetUserLogins (
	LoginProvider NVARCHAR(128) NOT NULL,
	ProviderKey NVARCHAR(128) NOT NULL,
	UserId NVARCHAR(128) NOT NULL,
	PRIMARY KEY (LoginProvider, ProviderKey, UserId)
);

CREATE TABLE AspNetUserClaims (
	Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	UserId NVARCHAR(128) NOT NULL,
	ClaimType NVARCHAR(MAX) NULL,
	ClaimValue NVARCHAR(MAX) NULL
);

CREATE INDEX IX_AspNetUsers_Email ON AspNetUsers(Email);
CREATE INDEX IX_AspNetUsers_UserName ON AspNetUsers(UserName);
```

## 주의사항
- `DapperUserStore`는 `IDbConnection`을 통해 Dapper로 SQL을 실행합니다. 연결 문자열은 `Web.config`의 `DefaultConnection`을 사용합니다.
- 비밀번호 해시/확인은 `UserManager`/`PasswordHasher`가 담당하며, `DapperUserStore`는 해시를 저장·조회합니다.
- 외부 로그인, 역할, 클레임 등 Identity 기능을 SQL로 직접 구현하므로 테이블/컬럼명이 코드와 일치하는지 확인하세요.
- SQL은 반드시 매개변수화하여 SQL Injection을 방지하세요. 현재 Dapper 호출은 익명 객체로 파라미터화되어 있습니다.

추가 참고:
- `Web.config`의 `targetFramework`는 이 리포지토리의 프로젝트 파일(`WebApp.csproj`)과 일치하도록 `4.7.2`로 설정되어 있습니다. 개발 환경에서 해당 .NET Framework이 설치되어 있는지 확인하세요.
- `Web.config`의 `PasswordHashIterations` 설정은 PBKDF2 해시 반복 횟수를 제어합니다 (기본값: `200000`). 값을 높이면 보안은 강화되지만 암호 처리 속도는 느려집니다.
- 예제 SQL 스크립트는 SQL Server Management Studio(SSMS)에서 실행하거나 `sqlcmd`로 적용할 수 있습니다.

## 보안 권장사항
- 연결 문자열에 민감한 정보를 직접 저장하지 마세요. 가능한 경우 Windows 인증 또는 안전한 시크릿 저장소를 사용하세요.
- 데이터베이스 권한은 최소 권한 원칙을 적용하세요.

## 확장 포인트
- `ApplicationUser`에 프로필 필드를 추가하고 DB 및 `DapperUserStore`를 확장하세요.
- 복잡한 다중 연산은 `IDbTransaction` 또는 `TransactionScope`로 트랜잭션 처리하세요.
- 캐싱(Redis 등)을 추가해 읽기 성능을 개선할 수 있습니다.

## 기여
예제/학습용 프로젝트입니다. 개선 제안이나 이슈는 Pull Request 또는 Issue로 제출해 주세요.

## 라이선스
- 특별한 라이선스 파일이 없는 경우 기본적으로 소스 코드를 자유롭게 확인·수정할 수 있습니다. 상업적 사용 전에는 저작권자와 확인하세요.