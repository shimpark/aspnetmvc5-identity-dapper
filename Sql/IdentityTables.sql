/*
  Identity table creation script (minimal subset for roles)
  - AspNetRoles: role definitions
  - AspNetUserRoles: many-to-many link between users and roles

  Run this script in the same database where your AspNetUsers table exists.
  Example (sqlcmd / SSMS):
    -- in SSMS: open new query, choose the correct DB, then execute
    -- using sqlcmd:
    sqlcmd -S .\SQLEXPRESS -d YourDatabaseName -i IdentityTables.sql -U sa -P YourPassword
*/

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetRoles] (
        [Id] NVARCHAR(128) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(256) NOT NULL
    );
    CREATE UNIQUE INDEX IX_Role_Name ON [dbo].[AspNetRoles] ([Name]);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles] (
        [UserId] NVARCHAR(128) NOT NULL,
        [RoleId] NVARCHAR(128) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY (UserId, RoleId),
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers] FOREIGN KEY (UserId) REFERENCES [dbo].[AspNetUsers](Id) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles] FOREIGN KEY (RoleId) REFERENCES [dbo].[AspNetRoles](Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserRoles_UserId ON [dbo].[AspNetUserRoles] ([UserId]);
    CREATE INDEX IX_AspNetUserRoles_RoleId ON [dbo].[AspNetUserRoles] ([RoleId]);
END

-- Optional: seed an Admin role
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'Admin')
BEGIN
    INSERT INTO [dbo].[AspNetRoles] (Id, Name) VALUES (NEWID(), N'Admin');
END

-- Role permissions table: simple key-value of role -> permission
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions] (
        [RoleId] NVARCHAR(128) NOT NULL,
        [Permission] NVARCHAR(100) NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY (RoleId, Permission),
        CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY (RoleId) REFERENCES [dbo].[AspNetRoles](Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_RolePermissions_RoleId ON [dbo].[RolePermissions] ([RoleId]);
END
