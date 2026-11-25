-- KAN-136: material usage on projects lowers the stock.
-- From this version on the app writes a stocklog row (AmountUsed, productusage_Id) for every usage.
-- This one-time script adds that row for usage booked before the change.
--
-- Run once per database, after a backup. Safe to run again: usage that already has a stocklog row is skipped.
-- Compatible with MySQL 8 and MariaDB 10.3.

-- 1. Preview: usage without a stocklog row.
SELECT pu.Id AS productusage_Id, pu.product_Id, pu.AmountUsed, pu.UsageDate
FROM productusage pu
WHERE NOT EXISTS ( SELECT 1 FROM stocklog sl WHERE sl.productusage_Id = pu.Id )
ORDER BY pu.Id;

-- 2. Add the missing stocklog rows.
START TRANSACTION;

INSERT INTO stocklog ( product_Id, productusage_Id, AmountReceived, AmountUsed, AmountCorrection, LogDate )
SELECT pu.product_Id, pu.Id, 0, pu.AmountUsed, 0, pu.UsageDate
FROM productusage pu
WHERE NOT EXISTS ( SELECT 1 FROM stocklog sl WHERE sl.productusage_Id = pu.Id );

-- 3. Check: this must return 0 rows before COMMIT.
SELECT pu.Id
FROM productusage pu
WHERE NOT EXISTS ( SELECT 1 FROM stocklog sl WHERE sl.productusage_Id = pu.Id );

COMMIT;
