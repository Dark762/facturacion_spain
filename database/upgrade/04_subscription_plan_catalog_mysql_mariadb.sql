-- Actualizacion NO destructiva para MySQL/MariaDB.
-- Ejecutar UNA SOLA VEZ sobre la BD local existente (no ejecutar 01_full_schema.sql).
-- Sustituir cada price_REEMPLAZAR_* por el Price ID real de Stripe Sandbox.
-- Comprobar que SubscriptionPlans existe y realizar copia de seguridad antes.
ALTER TABLE `subscriptionplans`
  ADD COLUMN `MonthlyPrice` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  ADD COLUMN `Currency` VARCHAR(3) NOT NULL DEFAULT 'EUR',
  ADD COLUMN `MaxCompanies` INT NOT NULL DEFAULT 0,
  ADD COLUMN `MaxUsers` INT NOT NULL DEFAULT 0,
  ADD COLUMN `MaxInvoicesPerMonth` INT NOT NULL DEFAULT 0,
  ADD COLUMN `Description` VARCHAR(500) NOT NULL DEFAULT '',
  ADD COLUMN `DisplayOrder` INT NOT NULL DEFAULT 0;

-- Reemplaza estos cuatro valores ANTES de ejecutar el script.
SET @autonomo_price = 'price_REEMPLAZAR_AUTONOMO';
SET @negocio_price = 'price_REEMPLAZAR_NEGOCIO';
SET @empresa_price = 'price_REEMPLAZAR_EMPRESA';
SET @asesoria_price = 'price_REEMPLAZAR_ASESORIA';

-- El plan Gratis se deja inactivo: el flujo actual de Checkout requiere
-- un precio Stripe válido; no debe ofrecerse hasta implementar alta gratuita.
INSERT INTO `subscriptionplans`
(`Code`, `Name`, `StripePriceId`, `IsActive`, `MonthlyPrice`, `Currency`, `MaxCompanies`, `MaxUsers`, `MaxInvoicesPerMonth`, `Description`, `DisplayOrder`)
VALUES
('gratis','Gratis','',0,0.00,'EUR',1,1,5,'Para empezar sin coste',0),
('autonomo','Autónomo',@autonomo_price,1,5.90,'EUR',1,1,50,'Para profesionales independientes',1),
('negocio','Negocio',@negocio_price,1,12.90,'EUR',2,3,300,'Para negocios en crecimiento',2),
('empresa','Empresa',@empresa_price,1,24.90,'EUR',5,10,1000,'Para equipos y empresas',3),
('asesoria','Asesoría',@asesoria_price,1,39.00,'EUR',10,0,0,'Hasta 10 empresas gestionadas',4)
ON DUPLICATE KEY UPDATE
 `Name`=VALUES(`Name`), `MonthlyPrice`=VALUES(`MonthlyPrice`),
 `Currency`=VALUES(`Currency`), `MaxCompanies`=VALUES(`MaxCompanies`),
 `MaxUsers`=VALUES(`MaxUsers`), `MaxInvoicesPerMonth`=VALUES(`MaxInvoicesPerMonth`),
 `Description`=VALUES(`Description`), `DisplayOrder`=VALUES(`DisplayOrder`);

-- Si ya existían filas con estos códigos, actualizar sus StripePriceId
-- explícitamente tras verificar que no colisionan con el índice único.
UPDATE `subscriptionplans` SET `StripePriceId`=@autonomo_price WHERE `Code`='autonomo';
UPDATE `subscriptionplans` SET `StripePriceId`=@negocio_price WHERE `Code`='negocio';
UPDATE `subscriptionplans` SET `StripePriceId`=@empresa_price WHERE `Code`='empresa';
UPDATE `subscriptionplans` SET `StripePriceId`=@asesoria_price WHERE `Code`='asesoria';
UPDATE `subscriptionplans` SET `IsActive`=0 WHERE `Code`='gratis';

SELECT `Code`,`Name`,`StripePriceId`,`MonthlyPrice`,`IsActive` FROM `subscriptionplans` ORDER BY `DisplayOrder`;
