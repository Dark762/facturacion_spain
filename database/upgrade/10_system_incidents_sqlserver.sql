IF OBJECT_ID(N'dbo.SystemIncidents', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SystemIncidents (
 Id uniqueidentifier NOT NULL PRIMARY KEY,
 CreatedAt datetimeoffset NOT NULL,
 UpdatedAt datetimeoffset NOT NULL,
 Application nvarchar(80) NOT NULL,
 Operation nvarchar(120) NOT NULL,
 Severity nvarchar(16) NOT NULL,
 ErrorCode nvarchar(80) NOT NULL,
 Status nvarchar(24) NOT NULL,
 TraceId nvarchar(128) NULL,
 TenantId uniqueidentifier NULL,
 Occurrences int NOT NULL DEFAULT 1,
 ResolvedBy nvarchar(128) NULL
 );
 CREATE INDEX IX_SystemIncidents_CreatedAt ON dbo.SystemIncidents(CreatedAt);
 CREATE INDEX IX_SystemIncidents_Status_CreatedAt ON dbo.SystemIncidents(Status, CreatedAt);
 CREATE INDEX IX_SystemIncidents_ErrorCode ON dbo.SystemIncidents(ErrorCode);
END;
