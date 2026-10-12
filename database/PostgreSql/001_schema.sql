-- PostgreSQL 14+ | Conectarse a una base vacía llamada facturacion_saas antes de ejecutar

-- CREATE DATABASE facturacion_saas; -- ejecutar aparte en postgres, y reconectar

CREATE TABLE "Tenants" (
  "Id" UUID NOT NULL,
  "Name" VARCHAR(200) NOT NULL,
  "IsActive" BOOLEAN NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "Companys" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "Nif" VARCHAR(20) NOT NULL,
  "LegalName" VARCHAR(250) NOT NULL,
  "TradeName" VARCHAR(250) NULL,
  "Address" VARCHAR(500) NOT NULL,
  "PostalCode" VARCHAR(16) NOT NULL,
  "City" VARCHAR(120) NOT NULL,
  "Province" VARCHAR(120) NOT NULL,
  "CountryCode" VARCHAR(2) NOT NULL,
  "Email" VARCHAR(254) NULL,
  "IsActive" BOOLEAN NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "Customers" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "CompanyId" UUID NOT NULL,
  "TaxId" VARCHAR(20) NOT NULL,
  "LegalName" VARCHAR(250) NOT NULL,
  "Address" VARCHAR(500) NOT NULL,
  "PostalCode" VARCHAR(16) NOT NULL,
  "City" VARCHAR(120) NOT NULL,
  "CountryCode" VARCHAR(2) NOT NULL,
  "Email" VARCHAR(254) NULL,
  "IsBusiness" BOOLEAN NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "Invoices" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "CompanyId" UUID NOT NULL,
  "CustomerId" UUID NOT NULL,
  "Series" VARCHAR(32) NOT NULL,
  "Number" VARCHAR(40) NOT NULL,
  "Type" INT NOT NULL,
  "Status" INT NOT NULL,
  "IssueDate" DATE NOT NULL,
  "OperationDate" DATE NULL,
  "DueDate" DATE NULL,
  "Currency" VARCHAR(3) NOT NULL,
  "TaxableBase" DECIMAL(18,2) NOT NULL,
  "TaxAmount" DECIMAL(18,2) NOT NULL,
  "WithholdingAmount" DECIMAL(18,2) NOT NULL,
  "Total" DECIMAL(18,2) NOT NULL,
  "IssuerNif" VARCHAR(20) NOT NULL,
  "IssuerName" VARCHAR(250) NOT NULL,
  "CustomerTaxId" VARCHAR(20) NOT NULL,
  "CustomerName" VARCHAR(250) NOT NULL,
  "CorrectedInvoiceId" UUID NULL,
  "IssuedAt" TIMESTAMP(6) NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "InvoiceLines" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "InvoiceId" UUID NOT NULL,
  "LineNumber" INT NOT NULL,
  "Description" VARCHAR(1000) NOT NULL,
  "Quantity" DECIMAL(18,6) NOT NULL,
  "UnitPrice" DECIMAL(18,6) NOT NULL,
  "DiscountPercent" DECIMAL(9,4) NOT NULL,
  "VatRate" DECIMAL(9,4) NOT NULL,
  "NetAmount" DECIMAL(18,2) NOT NULL,
  "VatAmount" DECIMAL(18,2) NOT NULL,
  "TotalAmount" DECIMAL(18,2) NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "InvoiceTaxBreakdowns" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "InvoiceId" UUID NOT NULL,
  "TaxCode" VARCHAR(20) NOT NULL,
  "RegimeKey" VARCHAR(20) NULL,
  "OperationClassification" VARCHAR(60) NULL,
  "ExemptionCode" VARCHAR(30) NULL,
  "TaxRate" DECIMAL(9,4) NOT NULL,
  "TaxableBase" DECIMAL(18,2) NOT NULL,
  "TaxAmount" DECIMAL(18,2) NOT NULL,
  "SurchargeRate" DECIMAL(9,4) NULL,
  "SurchargeAmount" DECIMAL(18,2) NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "InvoiceSequences" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "CompanyId" UUID NOT NULL,
  "Series" VARCHAR(32) NOT NULL,
  "FiscalYear" INT NOT NULL,
  "NextNumber" BIGINT NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "FiscalChains" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "CompanyId" UUID NOT NULL,
  "InstallationId" VARCHAR(100) NOT NULL,
  "LastRecordId" UUID NULL,
  "LastHash" VARCHAR(64) NOT NULL,
  "UpdatedAt" TIMESTAMP(6) NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "FiscalRecords" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "InvoiceId" UUID NOT NULL,
  "RecordType" INT NOT NULL,
  "PreviousRecordId" UUID NULL,
  "PreviousHash" VARCHAR(64) NOT NULL,
  "Hash" VARCHAR(64) NOT NULL,
  "GeneratedAt" TIMESTAMP(6) NOT NULL,
  "SoftwareName" VARCHAR(200) NOT NULL,
  "SoftwareVersion" VARCHAR(50) NOT NULL,
  "InstallationId" VARCHAR(100) NOT NULL,
  "XmlPayload" TEXT NOT NULL,
  "SubmissionStatus" INT NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "AeatSubmissions" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "FiscalRecordId" UUID NOT NULL,
  "Environment" VARCHAR(20) NOT NULL,
  "SubmittedAt" TIMESTAMP(6) NOT NULL,
  "Status" INT NOT NULL,
  "Csv" VARCHAR(150) NULL,
  "ErrorCode" VARCHAR(80) NULL,
  "ErrorDescription" VARCHAR(1000) NULL,
  "RequestXml" TEXT NOT NULL,
  "ResponseXml" TEXT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "ElectronicDocuments" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "CompanyId" UUID NOT NULL,
  "InvoiceId" UUID NULL,
  "Direction" INT NOT NULL,
  "Format" VARCHAR(50) NOT NULL,
  "FormatVersion" VARCHAR(30) NOT NULL,
  "DocumentNumber" VARCHAR(80) NOT NULL,
  "XmlPayload" TEXT NOT NULL,
  "ContentHash" VARCHAR(128) NOT NULL,
  "CreatedAt" TIMESTAMP(6) NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "B2BTransmissions" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "ElectronicDocumentId" UUID NOT NULL,
  "Provider" VARCHAR(100) NOT NULL,
  "ExternalId" VARCHAR(150) NULL,
  "Status" INT NOT NULL,
  "SubmittedAt" TIMESTAMP(6) NULL,
  "AcknowledgedAt" TIMESTAMP(6) NULL,
  "RequestPayload" TEXT NULL,
  "ResponsePayload" TEXT NULL,
  "ErrorCode" VARCHAR(80) NULL,
  "ErrorDescription" VARCHAR(1000) NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "InvoiceStatusEvents" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "InvoiceId" UUID NOT NULL,
  "EventType" VARCHAR(100) NOT NULL,
  "Source" VARCHAR(100) NOT NULL,
  "OccurredAt" TIMESTAMP(6) NOT NULL,
  "ReportedAt" TIMESTAMP(6) NULL,
  "ExternalReference" VARCHAR(200) NULL,
  "Payload" TEXT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "InvoicePayments" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "InvoiceId" UUID NOT NULL,
  "Amount" DECIMAL(18,2) NOT NULL,
  "PaymentDate" DATE NOT NULL,
  "PaymentMethod" VARCHAR(60) NOT NULL,
  "ExternalReference" VARCHAR(200) NULL,
  "CreatedAt" TIMESTAMP(6) NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "OutboxMessages" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "Type" VARCHAR(100) NOT NULL,
  "AggregateId" VARCHAR(100) NOT NULL,
  "Payload" TEXT NOT NULL,
  "Status" VARCHAR(32) NOT NULL,
  "Attempts" INT NOT NULL,
  "CreatedAt" TIMESTAMP(6) NOT NULL,
  "NextAttemptAt" TIMESTAMP(6) NULL,
  "ProcessedAt" TIMESTAMP(6) NULL,
  "LastError" TEXT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "AuditEvents" (
  "TenantId" UUID NOT NULL,
  "Id" UUID NOT NULL,
  "CompanyId" UUID NOT NULL,
  "EntityType" VARCHAR(100) NOT NULL,
  "EntityId" VARCHAR(100) NOT NULL,
  "Action" VARCHAR(100) NOT NULL,
  "UserId" VARCHAR(450) NULL,
  "OccurredAt" TIMESTAMP(6) NOT NULL,
  "PreviousValues" TEXT NULL,
  "NewValues" TEXT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "AspNetUsers" (
  "Id" VARCHAR(255) NOT NULL,
  "UserName" VARCHAR(256) NULL,
  "NormalizedUserName" VARCHAR(256) NULL,
  "Email" VARCHAR(256) NULL,
  "NormalizedEmail" VARCHAR(256) NULL,
  "EmailConfirmed" BOOLEAN NOT NULL,
  "PasswordHash" TEXT NULL,
  "SecurityStamp" TEXT NULL,
  "ConcurrencyStamp" TEXT NULL,
  "PhoneNumber" TEXT NULL,
  "PhoneNumberConfirmed" BOOLEAN NOT NULL,
  "TwoFactorEnabled" BOOLEAN NOT NULL,
  "LockoutEnd" TIMESTAMP(6) NULL,
  "LockoutEnabled" BOOLEAN NOT NULL,
  "AccessFailedCount" INT NOT NULL,
  "DisplayName" TEXT NOT NULL,
  "IsActive" BOOLEAN NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "AspNetRoles" (
  "Id" VARCHAR(255) NOT NULL,
  "Name" VARCHAR(256) NULL,
  "NormalizedName" VARCHAR(256) NULL,
  "ConcurrencyStamp" TEXT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "AspNetUserClaims" (
  "Id" INTEGER GENERATED BY DEFAULT AS IDENTITY NOT NULL,
  "UserId" VARCHAR(255) NOT NULL,
  "ClaimType" TEXT NULL,
  "ClaimValue" TEXT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "AspNetUserLogins" (
  "LoginProvider" VARCHAR(255) NOT NULL,
  "ProviderKey" VARCHAR(255) NOT NULL,
  "ProviderDisplayName" TEXT NULL,
  "UserId" VARCHAR(255) NOT NULL,
  PRIMARY KEY ("LoginProvider","ProviderKey")
);

CREATE TABLE "AspNetUserRoles" (
  "UserId" VARCHAR(255) NOT NULL,
  "RoleId" VARCHAR(255) NOT NULL,
  PRIMARY KEY ("UserId","RoleId")
);

CREATE TABLE "AspNetRoleClaims" (
  "Id" INTEGER GENERATED BY DEFAULT AS IDENTITY NOT NULL,
  "RoleId" VARCHAR(255) NOT NULL,
  "ClaimType" TEXT NULL,
  "ClaimValue" TEXT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "AspNetUserTokens" (
  "UserId" VARCHAR(255) NOT NULL,
  "LoginProvider" VARCHAR(255) NOT NULL,
  "Name" VARCHAR(255) NOT NULL,
  "Value" TEXT NULL,
  PRIMARY KEY ("UserId","LoginProvider","Name")
);

CREATE TABLE "TenantUsers" (
  "Id" UUID NOT NULL,
  "TenantId" UUID NOT NULL,
  "UserId" VARCHAR(255) NOT NULL,
  "IsActive" BOOLEAN NOT NULL,
  "CreatedAt" TIMESTAMP(6) NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE TABLE "CompanyUsers" (
  "Id" UUID NOT NULL,
  "TenantId" UUID NOT NULL,
  "CompanyId" UUID NOT NULL,
  "UserId" VARCHAR(255) NOT NULL,
  "Role" VARCHAR(32) NOT NULL,
  "IsActive" BOOLEAN NOT NULL,
  PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "AK_Companys_TenantId_Id" ON "Companys" ("TenantId","Id");

CREATE UNIQUE INDEX "UX_Companys_0" ON "Companys" ("TenantId","Nif");

CREATE UNIQUE INDEX "AK_Customers_TenantId_Id" ON "Customers" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_Invoices_TenantId_Id" ON "Invoices" ("TenantId","Id");

CREATE UNIQUE INDEX "UX_Invoices_0" ON "Invoices" ("TenantId","CompanyId","Series","Number");

CREATE UNIQUE INDEX "AK_InvoiceLines_TenantId_Id" ON "InvoiceLines" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_InvoiceTaxBreakdowns_TenantId_Id" ON "InvoiceTaxBreakdowns" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_InvoiceSequences_TenantId_Id" ON "InvoiceSequences" ("TenantId","Id");

CREATE UNIQUE INDEX "UX_InvoiceSequences_0" ON "InvoiceSequences" ("TenantId","CompanyId","Series","FiscalYear");

CREATE UNIQUE INDEX "AK_FiscalChains_TenantId_Id" ON "FiscalChains" ("TenantId","Id");

CREATE UNIQUE INDEX "UX_FiscalChains_0" ON "FiscalChains" ("TenantId","CompanyId","InstallationId");

CREATE UNIQUE INDEX "AK_FiscalRecords_TenantId_Id" ON "FiscalRecords" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_AeatSubmissions_TenantId_Id" ON "AeatSubmissions" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_ElectronicDocuments_TenantId_Id" ON "ElectronicDocuments" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_B2BTransmissions_TenantId_Id" ON "B2BTransmissions" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_InvoiceStatusEvents_TenantId_Id" ON "InvoiceStatusEvents" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_InvoicePayments_TenantId_Id" ON "InvoicePayments" ("TenantId","Id");

CREATE UNIQUE INDEX "AK_OutboxMessages_TenantId_Id" ON "OutboxMessages" ("TenantId","Id");

CREATE INDEX "IX_OutboxMessage_Pending" ON "OutboxMessages" ("TenantId","Status","NextAttemptAt");

CREATE UNIQUE INDEX "AK_AuditEvents_TenantId_Id" ON "AuditEvents" ("TenantId","Id");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_UserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_UserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_UserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE INDEX "IX_RoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "UX_TenantUsers" ON "TenantUsers" ("TenantId","UserId");

CREATE UNIQUE INDEX "UX_CompanyUsers" ON "CompanyUsers" ("TenantId","CompanyId","UserId");

ALTER TABLE "Companys" ADD CONSTRAINT "FK_Companys_Tenants_0" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Customers" ADD CONSTRAINT "FK_Customers_Companys_0" FOREIGN KEY ("TenantId","CompanyId") REFERENCES "Companys" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "Invoices" ADD CONSTRAINT "FK_Invoices_Companys_0" FOREIGN KEY ("TenantId","CompanyId") REFERENCES "Companys" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "Invoices" ADD CONSTRAINT "FK_Invoices_Customers_1" FOREIGN KEY ("TenantId","CustomerId") REFERENCES "Customers" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "InvoiceLines" ADD CONSTRAINT "FK_InvoiceLines_Invoices_0" FOREIGN KEY ("TenantId","InvoiceId") REFERENCES "Invoices" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "InvoiceTaxBreakdowns" ADD CONSTRAINT "FK_InvoiceTaxBreakdowns_Invoices_0" FOREIGN KEY ("TenantId","InvoiceId") REFERENCES "Invoices" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "InvoiceSequences" ADD CONSTRAINT "FK_InvoiceSequences_Companys_0" FOREIGN KEY ("TenantId","CompanyId") REFERENCES "Companys" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "FiscalChains" ADD CONSTRAINT "FK_FiscalChains_Companys_0" FOREIGN KEY ("TenantId","CompanyId") REFERENCES "Companys" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "FiscalRecords" ADD CONSTRAINT "FK_FiscalRecords_Invoices_0" FOREIGN KEY ("TenantId","InvoiceId") REFERENCES "Invoices" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "AeatSubmissions" ADD CONSTRAINT "FK_AeatSubmissions_FiscalRecords_0" FOREIGN KEY ("TenantId","FiscalRecordId") REFERENCES "FiscalRecords" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "ElectronicDocuments" ADD CONSTRAINT "FK_ElectronicDocuments_Invoices_0" FOREIGN KEY ("TenantId","InvoiceId") REFERENCES "Invoices" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "B2BTransmissions" ADD CONSTRAINT "FK_B2BTransmissions_ElectronicDocuments_0" FOREIGN KEY ("TenantId","ElectronicDocumentId") REFERENCES "ElectronicDocuments" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "InvoiceStatusEvents" ADD CONSTRAINT "FK_InvoiceStatusEvents_Invoices_0" FOREIGN KEY ("TenantId","InvoiceId") REFERENCES "Invoices" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "InvoicePayments" ADD CONSTRAINT "FK_InvoicePayments_Invoices_0" FOREIGN KEY ("TenantId","InvoiceId") REFERENCES "Invoices" ("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "AspNetUserClaims" ADD FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE "AspNetUserLogins" ADD FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE "AspNetUserRoles" ADD FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE "AspNetUserRoles" ADD FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE;

ALTER TABLE "AspNetRoleClaims" ADD FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE;

ALTER TABLE "AspNetUserTokens" ADD FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE "TenantUsers" ADD FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT;

ALTER TABLE "TenantUsers" ADD FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT;

ALTER TABLE "CompanyUsers" ADD FOREIGN KEY ("TenantId","CompanyId") REFERENCES "Companys"("TenantId","Id") ON DELETE RESTRICT;

ALTER TABLE "CompanyUsers" ADD FOREIGN KEY ("TenantId","UserId") REFERENCES "TenantUsers"("TenantId","UserId") ON DELETE RESTRICT;

ALTER TABLE "CompanyUsers" ADD FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT;
