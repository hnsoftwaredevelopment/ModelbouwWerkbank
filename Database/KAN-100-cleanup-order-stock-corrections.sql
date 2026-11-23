-- KAN-100: remove stock corrections that were written when an order line was created, changed or deleted.
-- Orders do not change the inventory; only receipts (AmountReceived) do. These rows caused double counting.
--
-- Before running: back up the stocklog table, for example:
--   mysqldump --user=<user> --host=<host> modelbuilder stocklog > stocklog-backup.sql
--
-- Step 1: preview. Check the rows and the inventory effect before deleting.
SELECT Id, product_Id, supplyorder_Id, supplyorderline_Id, AmountCorrection, LogDate
FROM modelbuilder.stocklog
WHERE supplyorderline_Id IS NOT NULL
  AND AmountCorrection <> 0
  AND AmountReceived = 0
  AND AmountUsed = 0
ORDER BY Id;

-- Step 2: delete. Run only after checking step 1.
START TRANSACTION;

DELETE FROM modelbuilder.stocklog
WHERE supplyorderline_Id IS NOT NULL
  AND AmountCorrection <> 0
  AND AmountReceived = 0
  AND AmountUsed = 0;

SELECT ROW_COUNT() AS deleted_rows;

-- Stop here. Compare deleted_rows with the number of rows from step 1,
-- then run ONE of these statements separately:
-- COMMIT;     -- numbers match
-- ROLLBACK;   -- numbers differ
