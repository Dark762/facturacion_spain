-- Ejecutar una sola vez en MySQL/MariaDB, tras realizar copia de seguridad.
-- Permite múltiples planes gratuitos con StripeSubscriptionId NULL (índice UNIQUE permite NULL).
ALTER TABLE `subscriptionstates`
    MODIFY COLUMN `StripeCustomerId` varchar(255) NULL,
    MODIFY COLUMN `StripeSubscriptionId` varchar(255) NULL;
