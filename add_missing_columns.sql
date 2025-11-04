-- Add missing columns to planfeatures table if they don't exist
-- Run this script in your MySQL database

ALTER TABLE `planfeatures` 
ADD COLUMN IF NOT EXISTS `DeliveryTime` VARCHAR(50) NULL DEFAULT NULL AFTER `IconClass`,
ADD COLUMN IF NOT EXISTS `Revisions` VARCHAR(50) NULL DEFAULT NULL AFTER `DeliveryTime`,
ADD COLUMN IF NOT EXISTS `IconClass` VARCHAR(50) NULL DEFAULT NULL AFTER `isActive`,
ADD COLUMN IF NOT EXISTS `OriginalPrice` DECIMAL(10,2) NULL DEFAULT 0.00 AFTER `FeatureRate`;

-- If the columns already exist, this will give a warning but won't fail
-- For MySQL versions that don't support IF NOT EXISTS, use:
-- ALTER TABLE `planfeatures` ADD COLUMN `DeliveryTime` VARCHAR(50) NULL DEFAULT NULL;
-- ALTER TABLE `planfeatures` ADD COLUMN `Revisions` VARCHAR(50) NULL DEFAULT NULL;
-- ALTER TABLE `planfeatures` ADD COLUMN `IconClass` VARCHAR(50) NULL DEFAULT NULL;
-- ALTER TABLE `planfeatures` ADD COLUMN `OriginalPrice` DECIMAL(10,2) NULL DEFAULT 0.00;

