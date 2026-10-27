ALTER TABLE `registrationintents`
    ADD COLUMN HolderType VARCHAR(24) NOT NULL DEFAULT 'Individual',
    ADD COLUMN TradeName VARCHAR(255) NOT NULL DEFAULT '';