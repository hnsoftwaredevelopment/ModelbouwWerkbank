-- KAN-137: remove receipts whose order no longer exists.
-- Orders 19, 20 and 21 were test orders (decision Herbert 2026-11-25). They were deleted, but their receipts
-- stayed in the stocklog and still count as stock. From this version on an order with receipts cannot be
-- deleted, so these rows cannot appear again.
--
-- Run once per database, after a backup. Safe to run again. Compatible with MySQL 8 and MariaDB 10.3.
-- After running, correct the stock of the products involved in the app if needed.

-- 1. Preview: receipts and corrections linked to an order line of an order that no longer exists.
SELECT sl.Id, sl.supplyorder_Id, sl.supplyorderline_Id, sl.product_Id, p.Name,
       sl.AmountReceived, sl.AmountCorrection, sl.LogDate
FROM stocklog sl
LEFT JOIN product p ON p.Id = sl.product_Id
WHERE sl.supplyorderline_Id IS NOT NULL
  AND NOT EXISTS ( SELECT 1 FROM supplyorder so WHERE so.Id = sl.supplyorder_Id )
ORDER BY sl.Id;

-- 2. Remove them.
START TRANSACTION;

DELETE FROM stocklog
WHERE supplyorderline_Id IS NOT NULL
  AND NOT EXISTS ( SELECT 1 FROM supplyorder so WHERE so.Id = stocklog.supplyorder_Id );

-- 3. Check: this must return 0 rows before COMMIT.
SELECT sl.Id
FROM stocklog sl
WHERE sl.supplyorderline_Id IS NOT NULL
  AND NOT EXISTS ( SELECT 1 FROM supplyorder so WHERE so.Id = sl.supplyorder_Id );

COMMIT;
