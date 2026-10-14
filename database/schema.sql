/*
  Changelog.io - SQL Server schema
  Idempotent: safe to run repeatedly against the Changelog database.
  The API runs this automatically on startup when Database:AutoMigrate = true.
*/

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Users_Id        DEFAULT NEWSEQUENTIALID(),
        CreatedAt    DATETIMEOFFSET(3) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        Username     NVARCHAR(100)     NOT NULL,
        PasswordHash NVARCHAR(100)     NOT NULL,
        CONSTRAINT PK_Users          PRIMARY KEY (Id),
        CONSTRAINT UQ_Users_Username UNIQUE (Username)
    );
END
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Products_Id        DEFAULT NEWSEQUENTIALID(),
        CreatedAt   DATETIMEOFFSET(3) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        Name        NVARCHAR(255)     NOT NULL,
        BelongsToId UNIQUEIDENTIFIER  NOT NULL,
        CONSTRAINT PK_Products       PRIMARY KEY (Id),
        CONSTRAINT FK_Products_Users FOREIGN KEY (BelongsToId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Products_BelongsToId ON dbo.Products (BelongsToId);
END
GO

IF OBJECT_ID(N'dbo.Updates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Updates
    (
        Id        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Updates_Id        DEFAULT NEWSEQUENTIALID(),
        CreatedAt DATETIMEOFFSET(3) NOT NULL CONSTRAINT DF_Updates_CreatedAt DEFAULT SYSUTCDATETIME(),
        Title     NVARCHAR(255)     NOT NULL,
        Body      NVARCHAR(MAX)     NOT NULL,
        Status    NVARCHAR(20)      NOT NULL CONSTRAINT DF_Updates_Status    DEFAULT N'IN_PROGRESS',
        Version   NVARCHAR(50)      NULL,
        Asset     NVARCHAR(2048)    NULL,
        ProductId UNIQUEIDENTIFIER  NOT NULL,
        CONSTRAINT PK_Updates          PRIMARY KEY (Id),
        CONSTRAINT CK_Updates_Status   CHECK (Status IN (N'IN_PROGRESS', N'SHIPPED', N'DEPRECATED')),
        CONSTRAINT FK_Updates_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Updates_ProductId ON dbo.Updates (ProductId);
END
GO

IF OBJECT_ID(N'dbo.UpdatePoints', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UpdatePoints
    (
        Id          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_UpdatePoints_Id        DEFAULT NEWSEQUENTIALID(),
        CreatedAt   DATETIMEOFFSET(3) NOT NULL CONSTRAINT DF_UpdatePoints_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt   DATETIMEOFFSET(3) NOT NULL CONSTRAINT DF_UpdatePoints_UpdatedAt DEFAULT SYSUTCDATETIME(),
        Name        NVARCHAR(255)     NOT NULL,
        Description NVARCHAR(MAX)     NOT NULL,
        UpdateId    UNIQUEIDENTIFIER  NOT NULL,
        CONSTRAINT PK_UpdatePoints         PRIMARY KEY (Id),
        CONSTRAINT FK_UpdatePoints_Updates FOREIGN KEY (UpdateId) REFERENCES dbo.Updates (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_UpdatePoints_UpdateId ON dbo.UpdatePoints (UpdateId);
END
GO
