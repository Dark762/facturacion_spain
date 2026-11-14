IF OBJECT_ID(N'dbo.IdentityVerifications', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.IdentityVerifications (
 Id uniqueidentifier NOT NULL PRIMARY KEY,
 UserId nvarchar(128) NOT NULL,
 TenantId uniqueidentifier NOT NULL,
 Method nvarchar(24) NOT NULL,
 Status nvarchar(24) NOT NULL,
 SubjectKind nvarchar(24) NOT NULL,
 EvidenceReference nvarchar(256) NULL,
 CertificateThumbprint nvarchar(128) NULL,
 CreatedAt datetimeoffset NOT NULL,
 UpdatedAt datetimeoffset NOT NULL,
 ReviewedAt datetimeoffset NULL,
 ReviewedBy nvarchar(128) NULL,
 DecisionCode nvarchar(80) NULL
 );
 CREATE INDEX IX_IdentityVerifications_TenantId_UserId_CreatedAt ON dbo.IdentityVerifications(TenantId,UserId,CreatedAt);
 CREATE INDEX IX_IdentityVerifications_Status_CreatedAt ON dbo.IdentityVerifications(Status,CreatedAt);
END;
