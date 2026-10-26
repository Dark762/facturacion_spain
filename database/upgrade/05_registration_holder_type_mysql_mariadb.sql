-- Ejecutar una sola vez sobre una base de datos MySQL/MariaDB existente.
-- No elimina registros ni modifica las columnas fiscales actuales.
ALTER TABLE `registrationintents`
    ADD COLUMN HolderType VARCHAR(24) NOT NULL DEFAULT 'Individual',
    ADD COLUMN TradeName VARCHAR(255) NOT NULL DEFAULT '';

-- Los registros anteriores requieren revisión del tipo de titular.
-- El valor por defecto no implica que los registros históricos sean personas físicas.
