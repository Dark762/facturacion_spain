-- ============================================================
-- Facturacion SaaS - Esquema base (baseline) para SQL Server
-- GENERADO DESDE EL MODELO EF CORE (Database First).
--
-- IMPORTANTE: este script NO ha sido ejecutado contra un servidor
-- SQL Server real. Se ha generado con GenerateCreateScript() a
-- partir del modelo, que es la fuente de verdad de Database First,
-- pero debe revisarse y probarse antes de produccion.
--
-- Aplicar sobre una base nueva:
--   sqlcmd -S localhost -d facturacion_saas -i 01_full_schema.sql
-- ============================================================
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [DisplayName] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [LockoutEnabled] bit NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [tenant_invitations] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [InvitedByUserId] nvarchar(255) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [AcceptedAtUtc] datetime2 NULL,
    [AcceptedByUserId] nvarchar(255) NULL,
    CONSTRAINT [PK_tenant_invitations] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CompanyUsers] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [Role] nvarchar(32) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_CompanyUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompanyUsers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TenantUsers] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_TenantUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantUsers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_CompanyUsers_TenantId_CompanyId_UserId] ON [CompanyUsers] ([TenantId], [CompanyId], [UserId]);
GO


CREATE INDEX [IX_CompanyUsers_UserId] ON [CompanyUsers] ([UserId]);
GO


CREATE INDEX [IX_tenant_invitations_TenantId_CompanyId] ON [tenant_invitations] ([TenantId], [CompanyId]);
GO


CREATE UNIQUE INDEX [IX_tenant_invitations_TokenHash] ON [tenant_invitations] ([TokenHash]);
GO


CREATE UNIQUE INDEX [IX_TenantUsers_TenantId_UserId] ON [TenantUsers] ([TenantId], [UserId]);
GO


CREATE INDEX [IX_TenantUsers_UserId] ON [TenantUsers] ([UserId]);
GO



CREATE TABLE [AuditEvents] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [EntityType] nvarchar(100) NOT NULL,
    [EntityId] nvarchar(100) NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [UserId] nvarchar(450) NULL,
    [OccurredAt] datetimeoffset NOT NULL,
    [PreviousValues] longtext NULL,
    [NewValues] longtext NULL,
    CONSTRAINT [PK_AuditEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_AuditEvents_TenantId_Id] UNIQUE ([TenantId], [Id])
);
GO


CREATE TABLE [OutboxMessages] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Type] nvarchar(100) NOT NULL,
    [AggregateId] nvarchar(100) NOT NULL,
    [Payload] longtext NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [Attempts] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [NextAttemptAt] datetimeoffset NULL,
    [ProcessedAt] datetimeoffset NULL,
    [LastError] longtext NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_OutboxMessages_TenantId_Id] UNIQUE ([TenantId], [Id])
);
GO


CREATE TABLE [Tenants] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Companys] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Nif] nvarchar(20) NOT NULL,
    [LegalName] nvarchar(250) NOT NULL,
    [TradeName] nvarchar(250) NULL,
    [Address] nvarchar(500) NOT NULL,
    [PostalCode] nvarchar(16) NOT NULL,
    [City] nvarchar(120) NOT NULL,
    [Province] nvarchar(120) NOT NULL,
    [CountryCode] nvarchar(2) NOT NULL,
    [Email] nvarchar(254) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Companys] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_Companys_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_Companys_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Customers] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [TaxId] nvarchar(20) NOT NULL,
    [LegalName] nvarchar(250) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [PostalCode] nvarchar(16) NOT NULL,
    [City] nvarchar(120) NOT NULL,
    [CountryCode] nvarchar(2) NOT NULL,
    [Email] nvarchar(254) NULL,
    [IsBusiness] bit NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_Customers_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_Customers_Companys_TenantId_CompanyId] FOREIGN KEY ([TenantId], [CompanyId]) REFERENCES [Companys] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [FiscalChains] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [InstallationId] nvarchar(100) NOT NULL,
    [LastRecordId] uniqueidentifier NULL,
    [LastHash] nvarchar(64) NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_FiscalChains] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_FiscalChains_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_FiscalChains_Companys_TenantId_CompanyId] FOREIGN KEY ([TenantId], [CompanyId]) REFERENCES [Companys] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InvoiceSequences] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Series] nvarchar(32) NOT NULL,
    [FiscalYear] int NOT NULL,
    [NextNumber] bigint NOT NULL,
    CONSTRAINT [PK_InvoiceSequences] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_InvoiceSequences_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_InvoiceSequences_Companys_TenantId_CompanyId] FOREIGN KEY ([TenantId], [CompanyId]) REFERENCES [Companys] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Invoices] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [Series] nvarchar(32) NULL,
    [Number] nvarchar(40) NULL,
    [Type] int NOT NULL,
    [Status] int NOT NULL,
    [IssueDate] date NOT NULL,
    [OperationDate] date NULL,
    [DueDate] date NULL,
    [Currency] nvarchar(3) NOT NULL,
    [TaxableBase] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [WithholdingAmount] decimal(18,2) NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [IssuerNif] nvarchar(20) NOT NULL,
    [IssuerName] nvarchar(250) NOT NULL,
    [CustomerTaxId] nvarchar(20) NOT NULL,
    [CustomerName] nvarchar(250) NOT NULL,
    [CorrectedInvoiceId] uniqueidentifier NULL,
    [IssuedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_Invoices_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_Invoices_Companys_TenantId_CompanyId] FOREIGN KEY ([TenantId], [CompanyId]) REFERENCES [Companys] ([TenantId], [Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Invoices_Customers_TenantId_CustomerId] FOREIGN KEY ([TenantId], [CustomerId]) REFERENCES [Customers] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ElectronicDocuments] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NULL,
    [Direction] int NOT NULL,
    [Format] nvarchar(50) NOT NULL,
    [FormatVersion] nvarchar(30) NOT NULL,
    [DocumentNumber] nvarchar(80) NOT NULL,
    [XmlPayload] longtext NOT NULL,
    [ContentHash] nvarchar(128) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ElectronicDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_ElectronicDocuments_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_ElectronicDocuments_Invoices_TenantId_InvoiceId] FOREIGN KEY ([TenantId], [InvoiceId]) REFERENCES [Invoices] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [FiscalRecords] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [RecordType] int NOT NULL,
    [PreviousRecordId] uniqueidentifier NULL,
    [PreviousHash] nvarchar(64) NOT NULL,
    [Hash] nvarchar(64) NOT NULL,
    [GeneratedAt] datetimeoffset NOT NULL,
    [SoftwareName] nvarchar(200) NOT NULL,
    [SoftwareVersion] nvarchar(50) NOT NULL,
    [InstallationId] nvarchar(100) NOT NULL,
    [XmlPayload] longtext NOT NULL,
    [SubmissionStatus] int NOT NULL,
    CONSTRAINT [PK_FiscalRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_FiscalRecords_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_FiscalRecords_Invoices_TenantId_InvoiceId] FOREIGN KEY ([TenantId], [InvoiceId]) REFERENCES [Invoices] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InvoiceLines] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [LineNumber] int NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [Quantity] decimal(18,6) NOT NULL,
    [UnitPrice] decimal(18,6) NOT NULL,
    [DiscountPercent] decimal(9,4) NOT NULL,
    [VatRate] decimal(9,4) NOT NULL,
    [NetAmount] decimal(18,2) NOT NULL,
    [VatAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_InvoiceLines_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_InvoiceLines_Invoices_TenantId_InvoiceId] FOREIGN KEY ([TenantId], [InvoiceId]) REFERENCES [Invoices] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InvoicePayments] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentDate] date NOT NULL,
    [PaymentMethod] nvarchar(60) NOT NULL,
    [ExternalReference] nvarchar(200) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_InvoicePayments] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_InvoicePayments_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_InvoicePayments_Invoices_TenantId_InvoiceId] FOREIGN KEY ([TenantId], [InvoiceId]) REFERENCES [Invoices] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InvoiceStatusEvents] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [EventType] nvarchar(100) NOT NULL,
    [Source] nvarchar(100) NOT NULL,
    [OccurredAt] datetimeoffset NOT NULL,
    [ReportedAt] datetimeoffset NULL,
    [ExternalReference] nvarchar(200) NULL,
    [Payload] longtext NULL,
    CONSTRAINT [PK_InvoiceStatusEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_InvoiceStatusEvents_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_InvoiceStatusEvents_Invoices_TenantId_InvoiceId] FOREIGN KEY ([TenantId], [InvoiceId]) REFERENCES [Invoices] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InvoiceTaxBreakdowns] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [TaxCode] nvarchar(20) NOT NULL,
    [RegimeKey] nvarchar(20) NULL,
    [OperationClassification] nvarchar(60) NULL,
    [ExemptionCode] nvarchar(30) NULL,
    [TaxRate] decimal(9,4) NOT NULL,
    [TaxableBase] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [SurchargeRate] decimal(9,4) NULL,
    [SurchargeAmount] decimal(18,2) NULL,
    CONSTRAINT [PK_InvoiceTaxBreakdowns] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_InvoiceTaxBreakdowns_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_InvoiceTaxBreakdowns_Invoices_TenantId_InvoiceId] FOREIGN KEY ([TenantId], [InvoiceId]) REFERENCES [Invoices] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [B2BTransmissions] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ElectronicDocumentId] uniqueidentifier NOT NULL,
    [Provider] nvarchar(100) NOT NULL,
    [ExternalId] nvarchar(150) NULL,
    [Status] int NOT NULL,
    [SubmittedAt] datetimeoffset NULL,
    [AcknowledgedAt] datetimeoffset NULL,
    [RequestPayload] longtext NULL,
    [ResponsePayload] longtext NULL,
    [ErrorCode] nvarchar(80) NULL,
    [ErrorDescription] nvarchar(1000) NULL,
    CONSTRAINT [PK_B2BTransmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_B2BTransmissions_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_B2BTransmissions_ElectronicDocuments_TenantId_ElectronicDocumentId] FOREIGN KEY ([TenantId], [ElectronicDocumentId]) REFERENCES [ElectronicDocuments] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AeatSubmissions] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [FiscalRecordId] uniqueidentifier NOT NULL,
    [Environment] nvarchar(20) NOT NULL,
    [SubmittedAt] datetimeoffset NOT NULL,
    [Status] int NOT NULL,
    [Csv] nvarchar(150) NULL,
    [ErrorCode] nvarchar(80) NULL,
    [ErrorDescription] nvarchar(1000) NULL,
    [RequestXml] longtext NOT NULL,
    [ResponseXml] longtext NULL,
    CONSTRAINT [PK_AeatSubmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_AeatSubmissions_TenantId_Id] UNIQUE ([TenantId], [Id]),
    CONSTRAINT [FK_AeatSubmissions_FiscalRecords_TenantId_FiscalRecordId] FOREIGN KEY ([TenantId], [FiscalRecordId]) REFERENCES [FiscalRecords] ([TenantId], [Id]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_AeatSubmissions_TenantId_FiscalRecordId] ON [AeatSubmissions] ([TenantId], [FiscalRecordId]);
GO


CREATE INDEX [IX_B2BTransmissions_TenantId_ElectronicDocumentId] ON [B2BTransmissions] ([TenantId], [ElectronicDocumentId]);
GO


CREATE UNIQUE INDEX [IX_Companys_TenantId_Nif] ON [Companys] ([TenantId], [Nif]);
GO


CREATE INDEX [IX_Customers_TenantId_CompanyId_TaxId] ON [Customers] ([TenantId], [CompanyId], [TaxId]);
GO


CREATE INDEX [IX_ElectronicDocuments_TenantId_InvoiceId] ON [ElectronicDocuments] ([TenantId], [InvoiceId]);
GO


CREATE UNIQUE INDEX [IX_FiscalChains_TenantId_CompanyId_InstallationId] ON [FiscalChains] ([TenantId], [CompanyId], [InstallationId]);
GO


CREATE INDEX [IX_FiscalRecords_TenantId_InvoiceId] ON [FiscalRecords] ([TenantId], [InvoiceId]);
GO


CREATE INDEX [IX_InvoiceLines_TenantId_InvoiceId] ON [InvoiceLines] ([TenantId], [InvoiceId]);
GO


CREATE INDEX [IX_InvoicePayments_TenantId_InvoiceId] ON [InvoicePayments] ([TenantId], [InvoiceId]);
GO


CREATE UNIQUE INDEX [IX_Invoices_TenantId_CompanyId_Series_Number] ON [Invoices] ([TenantId], [CompanyId], [Series], [Number]) WHERE [Series] IS NOT NULL AND [Number] IS NOT NULL;
GO


CREATE INDEX [IX_Invoices_TenantId_CustomerId] ON [Invoices] ([TenantId], [CustomerId]);
GO


CREATE UNIQUE INDEX [IX_InvoiceSequences_TenantId_CompanyId_Series_FiscalYear] ON [InvoiceSequences] ([TenantId], [CompanyId], [Series], [FiscalYear]);
GO


CREATE INDEX [IX_InvoiceStatusEvents_TenantId_InvoiceId] ON [InvoiceStatusEvents] ([TenantId], [InvoiceId]);
GO


CREATE INDEX [IX_InvoiceTaxBreakdowns_TenantId_InvoiceId] ON [InvoiceTaxBreakdowns] ([TenantId], [InvoiceId]);
GO


CREATE INDEX [IX_OutboxMessages_TenantId_Status_NextAttemptAt] ON [OutboxMessages] ([TenantId], [Status], [NextAttemptAt]);
GO

-- --- Alta self-service y suscripciones (SelfServiceDbContext) ---
CREATE TABLE [ProcessedPaymentEvents] (
    [Id] nvarchar(450) NOT NULL,
    [ReceivedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ProcessedPaymentEvents] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RegistrationIntents] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [TenantName] nvarchar(max) NOT NULL,
    [Nif] nvarchar(max) NOT NULL,
    [LegalName] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [PostalCode] nvarchar(max) NOT NULL,
    [City] nvarchar(max) NOT NULL,
    [Province] nvarchar(max) NOT NULL,
    [PlanCode] nvarchar(max) NOT NULL,
    [CheckoutSessionId] nvarchar(450) NULL,
    [CheckoutTokenHash] nvarchar(max) NULL,
    [TenantId] uniqueidentifier NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_RegistrationIntents] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SubscriptionPlans] (
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [StripePriceId] nvarchar(450) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([Code])
);
GO


CREATE TABLE [SubscriptionStates] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [StripeCustomerId] nvarchar(max) NOT NULL,
    [StripeSubscriptionId] nvarchar(450) NOT NULL,
    [PlanCode] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_SubscriptionStates] PRIMARY KEY ([Id])
);
GO


CREATE UNIQUE INDEX [IX_RegistrationIntents_CheckoutSessionId] ON [RegistrationIntents] ([CheckoutSessionId]) WHERE [CheckoutSessionId] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_RegistrationIntents_UserId] ON [RegistrationIntents] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_SubscriptionPlans_StripePriceId] ON [SubscriptionPlans] ([StripePriceId]);
GO


CREATE UNIQUE INDEX [IX_SubscriptionStates_StripeSubscriptionId] ON [SubscriptionStates] ([StripeSubscriptionId]);
GO


CREATE UNIQUE INDEX [IX_SubscriptionStates_TenantId] ON [SubscriptionStates] ([TenantId]);
GO



