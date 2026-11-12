CREATE TABLE IF NOT EXISTS "SystemIncidents" (
 "Id" uuid PRIMARY KEY,
 "CreatedAt" timestamp with time zone NOT NULL,
 "UpdatedAt" timestamp with time zone NOT NULL,
 "Application" varchar(80) NOT NULL,
 "Operation" varchar(120) NOT NULL,
 "Severity" varchar(16) NOT NULL,
 "ErrorCode" varchar(80) NOT NULL,
 "Status" varchar(24) NOT NULL,
 "TraceId" varchar(128),
 "TenantId" uuid,
 "Occurrences" integer NOT NULL DEFAULT 1,
 "ResolvedBy" varchar(128)
);
CREATE INDEX IF NOT EXISTS "IX_SystemIncidents_CreatedAt" ON "SystemIncidents" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_SystemIncidents_Status_CreatedAt" ON "SystemIncidents" ("Status","CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_SystemIncidents_ErrorCode" ON "SystemIncidents" ("ErrorCode");
