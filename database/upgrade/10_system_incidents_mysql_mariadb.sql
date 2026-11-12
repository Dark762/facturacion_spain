CREATE TABLE IF NOT EXISTS SystemIncidents (
 Id char(36) NOT NULL PRIMARY KEY,
 CreatedAt datetime(6) NOT NULL,
 UpdatedAt datetime(6) NOT NULL,
 Application varchar(80) NOT NULL,
 Operation varchar(120) NOT NULL,
 Severity varchar(16) NOT NULL,
 ErrorCode varchar(80) NOT NULL,
 Status varchar(24) NOT NULL,
 TraceId varchar(128) NULL,
 TenantId char(36) NULL,
 Occurrences int NOT NULL DEFAULT 1,
 ResolvedBy varchar(128) NULL,
 INDEX IX_SystemIncidents_CreatedAt (CreatedAt),
 INDEX IX_SystemIncidents_Status_CreatedAt (Status, CreatedAt),
 INDEX IX_SystemIncidents_ErrorCode (ErrorCode)
);
