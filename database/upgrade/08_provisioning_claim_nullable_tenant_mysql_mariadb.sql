-- MySQL/MariaDB. Ejecutar una vez, tras copia de seguridad.
-- El claim de aprovisionamiento debe existir antes que el tenant.
-- Se conserva la FK FK_ProvisioningOperations_Tenant: NULL no la infringe.
ALTER TABLE `ProvisioningOperations`
    MODIFY COLUMN `TenantId` char(36) NULL;
