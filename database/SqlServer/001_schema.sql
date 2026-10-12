-- SQL Server 2019+ | Base nueva exclusivamente

IF DB_ID(N'facturacion_saas') IS NULL CREATE DATABASE [facturacion_saas];

GO

USE [facturacion_saas];

GO

CREATE TABLE [Tenants] (
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [Name] NVARCHAR(200) NOT NULL,
  [IsActive] BIT NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [Companys] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [Nif] NVARCHAR(20) NOT NULL,
  [LegalName] NVARCHAR(250) NOT NULL,
  [TradeName] NVARCHAR(250) NULL,
  [Address] NVARCHAR(500) NOT NULL,
  [PostalCode] NVARCHAR(16) NOT NULL,
  [City] NVARCHAR(120) NOT NULL,
  [Province] NVARCHAR(120) NOT NULL,
  [CountryCode] NVARCHAR(2) NOT NULL,
  [Email] NVARCHAR(254) NULL,
  [IsActive] BIT NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [Customers] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [CompanyId] UNIQUEIDENTIFIER NOT NULL,
  [TaxId] NVARCHAR(20) NOT NULL,
  [LegalName] NVARCHAR(250) NOT NULL,
  [Address] NVARCHAR(500) NOT NULL,
  [PostalCode] NVARCHAR(16) NOT NULL,
  [City] NVARCHAR(120) NOT NULL,
  [CountryCode] NVARCHAR(2) NOT NULL,
  [Email] NVARCHAR(254) NULL,
  [IsBusiness] BIT NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [Invoices] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [CompanyId] UNIQUEIDENTIFIER NOT NULL,
  [CustomerId] UNIQUEIDENTIFIER NOT NULL,
  [Series] NVARCHAR(32) NOT NULL,
  [Number] NVARCHAR(40) NOT NULL,
  [Type] INT NOT NULL,
  [Status] INT NOT NULL,
  [IssueDate] DATE NOT NULL,
  [OperationDate] DATE NULL,
  [DueDate] DATE NULL,
  [Currency] NVARCHAR(3) NOT NULL,
  [TaxableBase] DECIMAL(18,2) NOT NULL,
  [TaxAmount] DECIMAL(18,2) NOT NULL,
  [WithholdingAmount] DECIMAL(18,2) NOT NULL,
  [Total] DECIMAL(18,2) NOT NULL,
  [IssuerNif] NVARCHAR(20) NOT NULL,
  [IssuerName] NVARCHAR(250) NOT NULL,
  [CustomerTaxId] NVARCHAR(20) NOT NULL,
  [CustomerName] NVARCHAR(250) NOT NULL,
  [CorrectedInvoiceId] UNIQUEIDENTIFIER NULL,
  [IssuedAt] DATETIME2(6) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [InvoiceLines] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
  [LineNumber] INT NOT NULL,
  [Description] NVARCHAR(1000) NOT NULL,
  [Quantity] DECIMAL(18,6) NOT NULL,
  [UnitPrice] DECIMAL(18,6) NOT NULL,
  [DiscountPercent] DECIMAL(9,4) NOT NULL,
  [VatRate] DECIMAL(9,4) NOT NULL,
  [NetAmount] DECIMAL(18,2) NOT NULL,
  [VatAmount] DECIMAL(18,2) NOT NULL,
  [TotalAmount] DECIMAL(18,2) NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [InvoiceTaxBreakdowns] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
  [TaxCode] NVARCHAR(20) NOT NULL,
  [RegimeKey] NVARCHAR(20) NULL,
  [OperationClassification] NVARCHAR(60) NULL,
  [ExemptionCode] NVARCHAR(30) NULL,
  [TaxRate] DECIMAL(9,4) NOT NULL,
  [TaxableBase] DECIMAL(18,2) NOT NULL,
  [TaxAmount] DECIMAL(18,2) NOT NULL,
  [SurchargeRate] DECIMAL(9,4) NULL,
  [SurchargeAmount] DECIMAL(18,2) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [InvoiceSequences] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [CompanyId] UNIQUEIDENTIFIER NOT NULL,
  [Series] NVARCHAR(32) NOT NULL,
  [FiscalYear] INT NOT NULL,
  [NextNumber] BIGINT NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [FiscalChains] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [CompanyId] UNIQUEIDENTIFIER NOT NULL,
  [InstallationId] NVARCHAR(100) NOT NULL,
  [LastRecordId] UNIQUEIDENTIFIER NULL,
  [LastHash] NVARCHAR(64) NOT NULL,
  [UpdatedAt] DATETIME2(6) NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [FiscalRecords] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
  [RecordType] INT NOT NULL,
  [PreviousRecordId] UNIQUEIDENTIFIER NULL,
  [PreviousHash] NVARCHAR(64) NOT NULL,
  [Hash] NVARCHAR(64) NOT NULL,
  [GeneratedAt] DATETIME2(6) NOT NULL,
  [SoftwareName] NVARCHAR(200) NOT NULL,
  [SoftwareVersion] NVARCHAR(50) NOT NULL,
  [InstallationId] NVARCHAR(100) NOT NULL,
  [XmlPayload] NVARCHAR(MAX) NOT NULL,
  [SubmissionStatus] INT NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [AeatSubmissions] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [FiscalRecordId] UNIQUEIDENTIFIER NOT NULL,
  [Environment] NVARCHAR(20) NOT NULL,
  [SubmittedAt] DATETIME2(6) NOT NULL,
  [Status] INT NOT NULL,
  [Csv] NVARCHAR(150) NULL,
  [ErrorCode] NVARCHAR(80) NULL,
  [ErrorDescription] NVARCHAR(1000) NULL,
  [RequestXml] NVARCHAR(MAX) NOT NULL,
  [ResponseXml] NVARCHAR(MAX) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [ElectronicDocuments] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [CompanyId] UNIQUEIDENTIFIER NOT NULL,
  [InvoiceId] UNIQUEIDENTIFIER NULL,
  [Direction] INT NOT NULL,
  [Format] NVARCHAR(50) NOT NULL,
  [FormatVersion] NVARCHAR(30) NOT NULL,
  [DocumentNumber] NVARCHAR(80) NOT NULL,
  [XmlPayload] NVARCHAR(MAX) NOT NULL,
  [ContentHash] NVARCHAR(128) NOT NULL,
  [CreatedAt] DATETIME2(6) NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [B2BTransmissions] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [ElectronicDocumentId] UNIQUEIDENTIFIER NOT NULL,
  [Provider] NVARCHAR(100) NOT NULL,
  [ExternalId] NVARCHAR(150) NULL,
  [Status] INT NOT NULL,
  [SubmittedAt] DATETIME2(6) NULL,
  [AcknowledgedAt] DATETIME2(6) NULL,
  [RequestPayload] NVARCHAR(MAX) NULL,
  [ResponsePayload] NVARCHAR(MAX) NULL,
  [ErrorCode] NVARCHAR(80) NULL,
  [ErrorDescription] NVARCHAR(1000) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [InvoiceStatusEvents] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
  [EventType] NVARCHAR(100) NOT NULL,
  [Source] NVARCHAR(100) NOT NULL,
  [OccurredAt] DATETIME2(6) NOT NULL,
  [ReportedAt] DATETIME2(6) NULL,
  [ExternalReference] NVARCHAR(200) NULL,
  [Payload] NVARCHAR(MAX) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [InvoicePayments] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
  [Amount] DECIMAL(18,2) NOT NULL,
  [PaymentDate] DATE NOT NULL,
  [PaymentMethod] NVARCHAR(60) NOT NULL,
  [ExternalReference] NVARCHAR(200) NULL,
  [CreatedAt] DATETIME2(6) NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [OutboxMessages] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [Type] NVARCHAR(100) NOT NULL,
  [AggregateId] NVARCHAR(100) NOT NULL,
  [Payload] NVARCHAR(MAX) NOT NULL,
  [Status] NVARCHAR(32) NOT NULL,
  [Attempts] INT NOT NULL,
  [CreatedAt] DATETIME2(6) NOT NULL,
  [NextAttemptAt] DATETIME2(6) NULL,
  [ProcessedAt] DATETIME2(6) NULL,
  [LastError] NVARCHAR(MAX) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [AuditEvents] (
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [CompanyId] UNIQUEIDENTIFIER NOT NULL,
  [EntityType] NVARCHAR(100) NOT NULL,
  [EntityId] NVARCHAR(100) NOT NULL,
  [Action] NVARCHAR(100) NOT NULL,
  [UserId] NVARCHAR(450) NULL,
  [OccurredAt] DATETIME2(6) NOT NULL,
  [PreviousValues] NVARCHAR(MAX) NULL,
  [NewValues] NVARCHAR(MAX) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
  [Id] NVARCHAR(128) NOT NULL,
  [UserName] NVARCHAR(256) NULL,
  [NormalizedUserName] NVARCHAR(256) NULL,
  [Email] NVARCHAR(256) NULL,
  [NormalizedEmail] NVARCHAR(256) NULL,
  [EmailConfirmed] BIT NOT NULL,
  [PasswordHash] NVARCHAR(MAX) NULL,
  [SecurityStamp] NVARCHAR(MAX) NULL,
  [ConcurrencyStamp] NVARCHAR(MAX) NULL,
  [PhoneNumber] NVARCHAR(MAX) NULL,
  [PhoneNumberConfirmed] BIT NOT NULL,
  [TwoFactorEnabled] BIT NOT NULL,
  [LockoutEnd] DATETIME2(6) NULL,
  [LockoutEnabled] BIT NOT NULL,
  [AccessFailedCount] INT NOT NULL,
  [DisplayName] NVARCHAR(MAX) NOT NULL,
  [IsActive] BIT NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoles] (
  [Id] NVARCHAR(128) NOT NULL,
  [Name] NVARCHAR(256) NULL,
  [NormalizedName] NVARCHAR(256) NULL,
  [ConcurrencyStamp] NVARCHAR(MAX) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUserClaims] (
  [Id] INT IDENTITY(1,1) NOT NULL,
  [UserId] NVARCHAR(128) NOT NULL,
  [ClaimType] NVARCHAR(MAX) NULL,
  [ClaimValue] NVARCHAR(MAX) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUserLogins] (
  [LoginProvider] NVARCHAR(128) NOT NULL,
  [ProviderKey] NVARCHAR(128) NOT NULL,
  [ProviderDisplayName] NVARCHAR(MAX) NULL,
  [UserId] NVARCHAR(128) NOT NULL,
  PRIMARY KEY ([LoginProvider],[ProviderKey])
);

CREATE TABLE [AspNetUserRoles] (
  [UserId] NVARCHAR(128) NOT NULL,
  [RoleId] NVARCHAR(128) NOT NULL,
  PRIMARY KEY ([UserId],[RoleId])
);

CREATE TABLE [AspNetRoleClaims] (
  [Id] INT IDENTITY(1,1) NOT NULL,
  [RoleId] NVARCHAR(128) NOT NULL,
  [ClaimType] NVARCHAR(MAX) NULL,
  [ClaimValue] NVARCHAR(MAX) NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUserTokens] (
  [UserId] NVARCHAR(128) NOT NULL,
  [LoginProvider] NVARCHAR(128) NOT NULL,
  [Name] NVARCHAR(128) NOT NULL,
  [Value] NVARCHAR(MAX) NULL,
  PRIMARY KEY ([UserId],[LoginProvider],[Name])
);

CREATE TABLE [TenantUsers] (
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [UserId] NVARCHAR(128) NOT NULL,
  [IsActive] BIT NOT NULL,
  [CreatedAt] DATETIME2(6) NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE TABLE [CompanyUsers] (
  [Id] UNIQUEIDENTIFIER NOT NULL,
  [TenantId] UNIQUEIDENTIFIER NOT NULL,
  [CompanyId] UNIQUEIDENTIFIER NOT NULL,
  [UserId] NVARCHAR(128) NOT NULL,
  [Role] NVARCHAR(32) NOT NULL,
  [IsActive] BIT NOT NULL,
  PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [AK_Companys_TenantId_Id] ON [Companys] ([TenantId],[Id]);

CREATE UNIQUE INDEX [UX_Companys_0] ON [Companys] ([TenantId],[Nif]);

CREATE UNIQUE INDEX [AK_Customers_TenantId_Id] ON [Customers] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_Invoices_TenantId_Id] ON [Invoices] ([TenantId],[Id]);

CREATE UNIQUE INDEX [UX_Invoices_0] ON [Invoices] ([TenantId],[CompanyId],[Series],[Number]);

CREATE UNIQUE INDEX [AK_InvoiceLines_TenantId_Id] ON [InvoiceLines] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_InvoiceTaxBreakdowns_TenantId_Id] ON [InvoiceTaxBreakdowns] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_InvoiceSequences_TenantId_Id] ON [InvoiceSequences] ([TenantId],[Id]);

CREATE UNIQUE INDEX [UX_InvoiceSequences_0] ON [InvoiceSequences] ([TenantId],[CompanyId],[Series],[FiscalYear]);

CREATE UNIQUE INDEX [AK_FiscalChains_TenantId_Id] ON [FiscalChains] ([TenantId],[Id]);

CREATE UNIQUE INDEX [UX_FiscalChains_0] ON [FiscalChains] ([TenantId],[CompanyId],[InstallationId]);

CREATE UNIQUE INDEX [AK_FiscalRecords_TenantId_Id] ON [FiscalRecords] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_AeatSubmissions_TenantId_Id] ON [AeatSubmissions] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_ElectronicDocuments_TenantId_Id] ON [ElectronicDocuments] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_B2BTransmissions_TenantId_Id] ON [B2BTransmissions] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_InvoiceStatusEvents_TenantId_Id] ON [InvoiceStatusEvents] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_InvoicePayments_TenantId_Id] ON [InvoicePayments] ([TenantId],[Id]);

CREATE UNIQUE INDEX [AK_OutboxMessages_TenantId_Id] ON [OutboxMessages] ([TenantId],[Id]);

CREATE INDEX [IX_OutboxMessage_Pending] ON [OutboxMessages] ([TenantId],[Status],[NextAttemptAt]);

CREATE UNIQUE INDEX [AK_AuditEvents_TenantId_Id] ON [AuditEvents] ([TenantId],[Id]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]);

CREATE INDEX [IX_UserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_UserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [IX_RoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [UX_TenantUsers] ON [TenantUsers] ([TenantId],[UserId]);

CREATE UNIQUE INDEX [UX_CompanyUsers] ON [CompanyUsers] ([TenantId],[CompanyId],[UserId]);

ALTER TABLE [Companys] ADD CONSTRAINT [FK_Companys_Tenants_0] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Customers] ADD CONSTRAINT [FK_Customers_Companys_0] FOREIGN KEY ([TenantId],[CompanyId]) REFERENCES [Companys] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_Companys_0] FOREIGN KEY ([TenantId],[CompanyId]) REFERENCES [Companys] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_Customers_1] FOREIGN KEY ([TenantId],[CustomerId]) REFERENCES [Customers] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [InvoiceLines] ADD CONSTRAINT [FK_InvoiceLines_Invoices_0] FOREIGN KEY ([TenantId],[InvoiceId]) REFERENCES [Invoices] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [InvoiceTaxBreakdowns] ADD CONSTRAINT [FK_InvoiceTaxBreakdowns_Invoices_0] FOREIGN KEY ([TenantId],[InvoiceId]) REFERENCES [Invoices] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [InvoiceSequences] ADD CONSTRAINT [FK_InvoiceSequences_Companys_0] FOREIGN KEY ([TenantId],[CompanyId]) REFERENCES [Companys] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [FiscalChains] ADD CONSTRAINT [FK_FiscalChains_Companys_0] FOREIGN KEY ([TenantId],[CompanyId]) REFERENCES [Companys] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [FiscalRecords] ADD CONSTRAINT [FK_FiscalRecords_Invoices_0] FOREIGN KEY ([TenantId],[InvoiceId]) REFERENCES [Invoices] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [AeatSubmissions] ADD CONSTRAINT [FK_AeatSubmissions_FiscalRecords_0] FOREIGN KEY ([TenantId],[FiscalRecordId]) REFERENCES [FiscalRecords] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [ElectronicDocuments] ADD CONSTRAINT [FK_ElectronicDocuments_Invoices_0] FOREIGN KEY ([TenantId],[InvoiceId]) REFERENCES [Invoices] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [B2BTransmissions] ADD CONSTRAINT [FK_B2BTransmissions_ElectronicDocuments_0] FOREIGN KEY ([TenantId],[ElectronicDocumentId]) REFERENCES [ElectronicDocuments] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [InvoiceStatusEvents] ADD CONSTRAINT [FK_InvoiceStatusEvents_Invoices_0] FOREIGN KEY ([TenantId],[InvoiceId]) REFERENCES [Invoices] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [InvoicePayments] ADD CONSTRAINT [FK_InvoicePayments_Invoices_0] FOREIGN KEY ([TenantId],[InvoiceId]) REFERENCES [Invoices] ([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [AspNetUserClaims] ADD FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE;

ALTER TABLE [AspNetUserLogins] ADD FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE;

ALTER TABLE [AspNetUserRoles] ADD FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE;

ALTER TABLE [AspNetUserRoles] ADD FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE;

ALTER TABLE [AspNetRoleClaims] ADD FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE;

ALTER TABLE [AspNetUserTokens] ADD FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE;

ALTER TABLE [TenantUsers] ADD FOREIGN KEY ([TenantId]) REFERENCES [Tenants]([Id]) ON DELETE NO ACTION;

ALTER TABLE [TenantUsers] ADD FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION;

ALTER TABLE [CompanyUsers] ADD FOREIGN KEY ([TenantId],[CompanyId]) REFERENCES [Companys]([TenantId],[Id]) ON DELETE NO ACTION;

ALTER TABLE [CompanyUsers] ADD FOREIGN KEY ([TenantId],[UserId]) REFERENCES [TenantUsers]([TenantId],[UserId]) ON DELETE NO ACTION;

ALTER TABLE [CompanyUsers] ADD FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION;
