/*
    Contract Management - initial SQL Server database schema

    Purpose:
    - Use this script to create the first real database for the phase 1 backend.
    - The backend currently stores data in memory. This schema is the target shape
      for moving CRUD services to EF Core / SQL Server later.

    How to run:
    - Open SQL Server Management Studio or Azure Data Studio.
    - Connect to SQL Server.
    - Run this whole script.
*/

IF DB_ID(N'ContractManagementDb') IS NULL
BEGIN
    CREATE DATABASE [ContractManagementDb];
END
GO

USE [ContractManagementDb];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Users_Id DEFAULT (NEWSEQUENTIALID()) CONSTRAINT PK_Users PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(500) NULL,
        FullName NVARCHAR(200) NOT NULL,
        Email NVARCHAR(255) NULL,
        Phone NVARCHAR(30) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT CK_Users_Username_NotEmpty CHECK (LEN(LTRIM(RTRIM(Username))) > 0),
        CONSTRAINT CK_Users_FullName_NotEmpty CHECK (LEN(LTRIM(RTRIM(FullName))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Roles_Id DEFAULT (NEWSEQUENTIALID()) CONSTRAINT PK_Roles PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Roles_Name UNIQUE (Name),
        CONSTRAINT CK_Roles_Name_NotEmpty CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles
    (
        UserId UNIQUEIDENTIFIER NOT NULL,
        RoleId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserRoles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Projects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Projects
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Projects_Id DEFAULT (NEWSEQUENTIALID()) CONSTRAINT PK_Projects PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        TotalBudget DECIMAL(18,2) NOT NULL CONSTRAINT DF_Projects_TotalBudget DEFAULT (0),
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Projects_Status DEFAULT (N'Planning'),
        IsActive BIT NOT NULL CONSTRAINT DF_Projects_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Projects_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Projects_Code UNIQUE (Code),
        CONSTRAINT CK_Projects_Code_NotEmpty CHECK (LEN(LTRIM(RTRIM(Code))) > 0),
        CONSTRAINT CK_Projects_Name_NotEmpty CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_Projects_TotalBudget_NonNegative CHECK (TotalBudget >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Partners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Partners
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Partners_Id DEFAULT (NEWSEQUENTIALID()) CONSTRAINT PK_Partners PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        TaxCode NVARCHAR(50) NULL,
        Phone NVARCHAR(30) NULL,
        Email NVARCHAR(255) NULL,
        Address NVARCHAR(500) NULL,
        Description NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Partners_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Partners_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Partners_Code UNIQUE (Code),
        CONSTRAINT CK_Partners_Code_NotEmpty CHECK (LEN(LTRIM(RTRIM(Code))) > 0),
        CONSTRAINT CK_Partners_Name_NotEmpty CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.BidPackages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BidPackages
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_BidPackages_Id DEFAULT (NEWSEQUENTIALID()) CONSTRAINT PK_BidPackages PRIMARY KEY,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        EstimatedValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_BidPackages_EstimatedValue DEFAULT (0),
        WarningThresholdPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_BidPackages_WarningThresholdPercent DEFAULT (100),
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_BidPackages_Status DEFAULT (N'Planning'),
        IsActive BIT NOT NULL CONSTRAINT DF_BidPackages_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_BidPackages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_BidPackages_Code UNIQUE (Code),
        CONSTRAINT FK_BidPackages_Projects FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id),
        CONSTRAINT CK_BidPackages_Code_NotEmpty CHECK (LEN(LTRIM(RTRIM(Code))) > 0),
        CONSTRAINT CK_BidPackages_Name_NotEmpty CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_BidPackages_EstimatedValue_NonNegative CHECK (EstimatedValue >= 0),
        CONSTRAINT CK_BidPackages_WarningThresholdPercent_Positive CHECK (WarningThresholdPercent > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Contracts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contracts
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Contracts_Id DEFAULT (NEWSEQUENTIALID()) CONSTRAINT PK_Contracts PRIMARY KEY,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        BidPackageId UNIQUEIDENTIFIER NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        ContractValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_Contracts_ContractValue DEFAULT (0),
        SignedDate DATE NULL,
        EffectiveDate DATE NULL,
        ExpiredDate DATE NULL,
        RenewalReminderDate DATE NULL,
        IsRenewalRequired BIT NOT NULL CONSTRAINT DF_Contracts_IsRenewalRequired DEFAULT (1),
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Contracts_Status DEFAULT (N'Draft'),
        IsActive BIT NOT NULL CONSTRAINT DF_Contracts_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Contracts_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Contracts_Code UNIQUE (Code),
        CONSTRAINT FK_Contracts_Projects FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id),
        CONSTRAINT FK_Contracts_BidPackages FOREIGN KEY (BidPackageId) REFERENCES dbo.BidPackages(Id),
        CONSTRAINT CK_Contracts_Code_NotEmpty CHECK (LEN(LTRIM(RTRIM(Code))) > 0),
        CONSTRAINT CK_Contracts_Name_NotEmpty CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_Contracts_ContractValue_NonNegative CHECK (ContractValue >= 0),
        CONSTRAINT CK_Contracts_DateRange CHECK
        (
            ExpiredDate IS NULL
            OR EffectiveDate IS NULL
            OR ExpiredDate >= EffectiveDate
        ),
        CONSTRAINT CK_Contracts_RenewalReminder CHECK
        (
            RenewalReminderDate IS NULL
            OR ExpiredDate IS NULL
            OR RenewalReminderDate <= ExpiredDate
        )
    );
END
GO

IF OBJECT_ID(N'dbo.ContractPartners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContractPartners
    (
        ContractId UNIQUEIDENTIFIER NOT NULL,
        PartnerId UNIQUEIDENTIFIER NOT NULL,
        Role NVARCHAR(50) NOT NULL CONSTRAINT DF_ContractPartners_Role DEFAULT (N'Primary'),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ContractPartners_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ContractPartners PRIMARY KEY (ContractId, PartnerId, Role),
        CONSTRAINT FK_ContractPartners_Contracts FOREIGN KEY (ContractId) REFERENCES dbo.Contracts(Id),
        CONSTRAINT FK_ContractPartners_Partners FOREIGN KEY (PartnerId) REFERENCES dbo.Partners(Id),
        CONSTRAINT CK_ContractPartners_Role_NotEmpty CHECK (LEN(LTRIM(RTRIM(Role))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documents
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Documents_Id DEFAULT (NEWSEQUENTIALID()) CONSTRAINT PK_Documents PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        DocumentType NVARCHAR(50) NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        FileUrl NVARCHAR(1000) NOT NULL,
        ContentType NVARCHAR(100) NULL,
        FileSize BIGINT NULL,
        ProjectId UNIQUEIDENTIFIER NULL,
        BidPackageId UNIQUEIDENTIFIER NULL,
        ContractId UNIQUEIDENTIFIER NULL,
        UploadedByUserId UNIQUEIDENTIFIER NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Documents_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Documents_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Documents_Code UNIQUE (Code),
        CONSTRAINT FK_Documents_Projects FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id),
        CONSTRAINT FK_Documents_BidPackages FOREIGN KEY (BidPackageId) REFERENCES dbo.BidPackages(Id),
        CONSTRAINT FK_Documents_Contracts FOREIGN KEY (ContractId) REFERENCES dbo.Contracts(Id),
        CONSTRAINT FK_Documents_Users FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Documents_Code_NotEmpty CHECK (LEN(LTRIM(RTRIM(Code))) > 0),
        CONSTRAINT CK_Documents_Name_NotEmpty CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_Documents_DocumentType_NotEmpty CHECK (LEN(LTRIM(RTRIM(DocumentType))) > 0),
        CONSTRAINT CK_Documents_FileName_NotEmpty CHECK (LEN(LTRIM(RTRIM(FileName))) > 0),
        CONSTRAINT CK_Documents_FileUrl_NotEmpty CHECK (LEN(LTRIM(RTRIM(FileUrl))) > 0),
        CONSTRAINT CK_Documents_FileSize_NonNegative CHECK (FileSize IS NULL OR FileSize >= 0),
        CONSTRAINT CK_Documents_HasParent CHECK
        (
            ProjectId IS NOT NULL
            OR BidPackageId IS NOT NULL
            OR ContractId IS NOT NULL
        )
    );
END
GO

IF OBJECT_ID(N'dbo.Resolutions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resolutions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Resolutions_Id DEFAULT (NEWSEQUENTIALID()) CONSTRAINT PK_Resolutions PRIMARY KEY,
        ProjectId UNIQUEIDENTIFIER NULL,
        ContractId UNIQUEIDENTIFIER NULL,
        DocumentId UNIQUEIDENTIFIER NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        IssuedDate DATE NULL,
        EffectiveDate DATE NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Resolutions_Status DEFAULT (N'Active'),
        IsActive BIT NOT NULL CONSTRAINT DF_Resolutions_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Resolutions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Resolutions_Code UNIQUE (Code),
        CONSTRAINT FK_Resolutions_Projects FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id),
        CONSTRAINT FK_Resolutions_Contracts FOREIGN KEY (ContractId) REFERENCES dbo.Contracts(Id),
        CONSTRAINT FK_Resolutions_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.Documents(Id),
        CONSTRAINT CK_Resolutions_Code_NotEmpty CHECK (LEN(LTRIM(RTRIM(Code))) > 0),
        CONSTRAINT CK_Resolutions_Name_NotEmpty CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_Resolutions_HasParent CHECK
        (
            ProjectId IS NOT NULL
            OR ContractId IS NOT NULL
        )
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'Admin')
BEGIN
    INSERT INTO dbo.Roles (Name, Description)
    VALUES
        (N'Admin', N'Full system permission'),
        (N'Manager', N'Manage projects, contracts, and documents'),
        (N'Staff', N'Create and update assigned records'),
        (N'Viewer', N'Read-only access');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BidPackages_ProjectId' AND object_id = OBJECT_ID(N'dbo.BidPackages'))
BEGIN
    CREATE INDEX IX_BidPackages_ProjectId ON dbo.BidPackages(ProjectId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Contracts_ProjectId' AND object_id = OBJECT_ID(N'dbo.Contracts'))
BEGIN
    CREATE INDEX IX_Contracts_ProjectId ON dbo.Contracts(ProjectId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Contracts_BidPackageId' AND object_id = OBJECT_ID(N'dbo.Contracts'))
BEGIN
    CREATE INDEX IX_Contracts_BidPackageId ON dbo.Contracts(BidPackageId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Contracts_ExpiredDate' AND object_id = OBJECT_ID(N'dbo.Contracts'))
BEGIN
    CREATE INDEX IX_Contracts_ExpiredDate ON dbo.Contracts(ExpiredDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContractPartners_PartnerId' AND object_id = OBJECT_ID(N'dbo.ContractPartners'))
BEGIN
    CREATE INDEX IX_ContractPartners_PartnerId ON dbo.ContractPartners(PartnerId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ProjectId' AND object_id = OBJECT_ID(N'dbo.Documents'))
BEGIN
    CREATE INDEX IX_Documents_ProjectId ON dbo.Documents(ProjectId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_BidPackageId' AND object_id = OBJECT_ID(N'dbo.Documents'))
BEGIN
    CREATE INDEX IX_Documents_BidPackageId ON dbo.Documents(BidPackageId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ContractId' AND object_id = OBJECT_ID(N'dbo.Documents'))
BEGIN
    CREATE INDEX IX_Documents_ContractId ON dbo.Documents(ContractId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Resolutions_ProjectId' AND object_id = OBJECT_ID(N'dbo.Resolutions'))
BEGIN
    CREATE INDEX IX_Resolutions_ProjectId ON dbo.Resolutions(ProjectId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Resolutions_ContractId' AND object_id = OBJECT_ID(N'dbo.Resolutions'))
BEGIN
    CREATE INDEX IX_Resolutions_ContractId ON dbo.Resolutions(ContractId);
END
GO

PRINT N'ContractManagementDb schema was initialized successfully.';
GO
