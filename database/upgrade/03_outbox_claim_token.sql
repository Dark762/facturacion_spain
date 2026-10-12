-- =====================================================================
-- ACTUALIZACION NO DESTRUCTIVA - MariaDB / MySQL 8
--
-- Anade la columna ClaimToken a outboxmessages para permitir la reclamacion
-- atomica y la comprobacion de propiedad del lease entre workers concurrentes.
--
-- Ejecutar:
--   mariadb -u root -p facturacion_saas < upgrade/03_outbox_claim_token.sql
-- o
--   mysql -u root -p facturacion_saas < upgrade/03_outbox_claim_token.sql
--
-- Es idempotente: se puede reaplicar sin error.
--
-- Rollback (si fuera necesario):
--   ALTER TABLE `outboxmessages` DROP COLUMN `ClaimToken`;
-- =====================================================================

SET @col = (SELECT COUNT(*) FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'outboxmessages'
              AND COLUMN_NAME = 'ClaimToken');
SET @s = IF(@col = 0,
  'ALTER TABLE `outboxmessages` ADD COLUMN `ClaimToken` varchar(64) DEFAULT NULL AFTER `LastError`',
  'DO 0');
PREPARE stmt FROM @s; EXECUTE stmt; DEALLOCATE PREPARE stmt;
