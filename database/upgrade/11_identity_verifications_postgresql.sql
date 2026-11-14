CREATE TABLE IF NOT EXISTS "IdentityVerifications" (
 "Id" uuid PRIMARY KEY,
 "UserId" varchar(128) NOT NULL,
 "TenantId" uuid NOT NULL,
 "Method" varchar(24) NOT NULL,
 "Status" varchar(24) NOT NULL,
 "SubjectKind" varchar(24) NOT NULL,
 "EvidenceReference" varchar(256),
 "CertificateThumbprint" varchar(128),
 "CreatedAt" timestamptz NOT NULL,
 "UpdatedAt" timestamptz NOT NULL,
 "ReviewedAt" timestamptz,
 "ReviewedBy" varchar(128),
 "DecisionCode" varchar(80)
);
CREATE INDEX IF NOT EXISTS "IX_IdentityVerifications_TenantId_UserId_CreatedAt" ON "IdentityVerifications" ("TenantId","UserId","CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_IdentityVerifications_Status_CreatedAt" ON "IdentityVerifications" ("Status","CreatedAt");
