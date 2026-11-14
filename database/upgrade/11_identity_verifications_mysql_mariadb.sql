CREATE TABLE IF NOT EXISTS IdentityVerifications (
 Id char(36) NOT NULL PRIMARY KEY,
 UserId varchar(128) NOT NULL,
 TenantId char(36) NOT NULL,
 Method varchar(24) NOT NULL,
 Status varchar(24) NOT NULL,
 SubjectKind varchar(24) NOT NULL,
 EvidenceReference varchar(256) NULL,
 CertificateThumbprint varchar(128) NULL,
 CreatedAt datetime(6) NOT NULL,
 UpdatedAt datetime(6) NOT NULL,
 ReviewedAt datetime(6) NULL,
 ReviewedBy varchar(128) NULL,
 DecisionCode varchar(80) NULL,
 INDEX IX_IdentityVerifications_TenantId_UserId_CreatedAt (TenantId,UserId,CreatedAt),
 INDEX IX_IdentityVerifications_Status_CreatedAt (Status,CreatedAt)
);
