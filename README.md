# aspnetmvc5-identity-dapper

ASP.NET MVC 5 템플릿에서 Microsoft.AspNet.Identity의 기본 Entity Framework 구현을 Dapper 기반 사용자 저장소로 대체한 예제 프로젝트입니다. 이 리포지토리는 Dapper를 사용해 Identity 사용자(로그인, 역할, 클레임 등)를 직접 SQL로 관리하는 방법을 보여줍니다.

핵심 구현:
- `Models/ApplicationUser.cs`: `IUser<string>`을 구현한 간단한 사용자 모델
- `Infrastructure/DapperUserStore.cs`: `IUserStore` 계열 인터페이스들을 Dapper로 구현
- `App_Start/IdentityConfig.cs`, `App_Start/Startup.Auth.cs`: Dapper 기반 UserManager/SignInManager 구성.

**목표**: EF DbContext와 마이그레이션 의존성을 제거하고, Dapper로 성능 및 쿼리 제어를 높이는 방법을 제공.

## 요구 사항
- Visual Studio 2017 이상 또는 MSBuild가 있는 개발 환경
- .NET Framework 4.6.2 (프로젝트 파일의 `TargetFrameworkVersion` 참조)
- SQL Server (LocalDB 포함)
- NuGet 패키지: `Dapper`, `Microsoft.AspNet.Identity.Core`, `Microsoft.AspNet.Identity.Owin`, `Microsoft.Owin.*` (프로젝트에 이미 참조되어 있음)

## 빠른 시작
1. 저장소를 클론하세요.

   git clone <repo-url>

2. Visual Studio에서 `WebApp.sln`을 엽니다.
3. NuGet 패키지 복원(Visual Studio에서 자동 복원 또는 `nuget restore`).
4. `Web.config`의 `DefaultConnection` 연결 문자열을 실제 DB 환경에 맞게 수정하세요.
   - 현재 기본값은 LocalDB 파일 기반 `.mdf`로 설정되어 있습니다.
5. 데이터베이스에 Identity 테이블이 없다면 아래 SQL 스크립트를 실행하세요.
6. 프로젝트를 빌드하고 실행한 뒤 `/Account/Register` 등으로 테스트하세요.

## 데이터베이스 스키마 (예제)
다음 SQL은 프로젝트의 `DapperUserStore` 구현과 호환되는 최소 테이블을 생성합니다. 필요에 따라 확장하세요.

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

-- 권장 인덱스
CREATE INDEX IX_AspNetUsers_Email ON AspNetUsers(Email);
CREATE INDEX IX_AspNetUsers_UserName ON AspNetUsers(UserName);

```

## 코드 설명 및 주의사항
- `DapperUserStore`는 `IDbConnection`을 직접 생성하고 Dapper로 쿼리를 실행합니다. 연결 문자열은 `Web.config`의 `DefaultConnection`에서 로드됩니다.
- 비밀번호 관리(해시)는 `UserManager`와 `PasswordHasher`에 의해 처리되며, `DapperUserStore`는 해시를 저장/조회합니다.
- 외부 로그인, 역할, 클레임 등 Identity의 주요 기능을 SQL로 구현합니다. 구현된 쿼리와 컬럼명이 현재 테이블 스키마와 맞는지 확인하세요.
- SQL을 직접 작성하므로 항상 매개변수화된 쿼리를 사용해 SQL Injection을 방지합니다. 현재 Dapper 호출은 익명 객체를 통해 파라미터화되어 있습니다.

## 보안 권장사항
- 연결 문자열에 민감한 정보를 저장하지 말고, 가능하면 Windows 인증 또는 안전한 시크릿 관리 방법을 사용하세요.
- 데이터베이스 접근 권한은 최소 권한 원칙을 적용하세요.
- 이메일/휴대폰 전송을 위해 `EmailService`/`SmsService`를 실제 전송 구현으로 교체하세요.

## 확장 포인트
- `ApplicationUser`에 사용자 프로필 필드를 추가하고 DB와 `DapperUserStore`를 확장하세요.
- 트랜잭션이 필요한 복잡한 작업에는 `IDbTransaction`을 사용하거나 `TransactionScope`를 적용하세요.
- 캐싱(예: Redis)을 추가해 읽기 성능을 개선하세요.

## 기여
- 이 리포지토리는 예제/학습 목적입니다. 개선 제안이나 이슈는 Pull Request 또는 Issue로 제출하세요.

## 라이선스
- 특별한 라이선스 파일이 없는 경우 기본적으로 소스 코드를 자유롭게 확인·수정할 수 있습니다. 상업적 사용 전에는 저작권자와 확인하세요.

---

원하시면 제가 이 README에 더 자세한 설치 스크립트(예: LocalDB로 자동 생성하는 PowerShell 스크립트)나 데모 시나리오(가입 → 로그인 → 역할 부여)를 추가해 드리겠습니다. 변경사항을 적용할까요?