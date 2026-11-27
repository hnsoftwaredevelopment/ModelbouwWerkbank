-- KAN-145: put the foreign keys back.
-- They were dropped in May 2026 (drop_all_foreign_keys.sql) to make manual cleanup of test data easier. Since then
-- the application only guarded the relations itself. Decision (2026-11-27):
--   * RESTRICT for every relation: a row that is still referenced cannot be deleted. The app shows its
--     "is still in use" message for that (MySQL error 1451).
--   * Category, storage location and work type: a parent with children cannot be deleted (was CASCADE for category).
--   * CASCADE for the link rows productsupplier and suppliercontact: they mean nothing without their product or
--     supplier, so they go along when it is deleted.
--
-- Before adding a key the script counts rows that point to a missing parent. If there are any it stops with a message
-- and adds nothing for that relation; clean those rows first. Run once per database, after a backup (Tools\Update-
-- Database.ps1 does both). Safe to run again: existing keys are skipped. Compatible with MySQL 8 and MariaDB 10.3.

DROP PROCEDURE IF EXISTS KAN145_AddForeignKey;

DELIMITER $$

CREATE PROCEDURE KAN145_AddForeignKey(
	IN p_table VARCHAR(64), IN p_name VARCHAR(64), IN p_column VARCHAR(64), IN p_parent VARCHAR(64), IN p_on_delete VARCHAR(16) )
BEGIN
	DECLARE v_message VARCHAR(255);

	IF NOT EXISTS (
		SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS
		WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = p_table AND CONSTRAINT_NAME = p_name ) THEN

		SET @kan145_sql = CONCAT(
			'SELECT COUNT(*) INTO @kan145_orphans FROM `', p_table, '` c LEFT JOIN `', p_parent, '` p ON p.`Id` = c.`', p_column, '` ',
			'WHERE c.`', p_column, '` IS NOT NULL AND p.`Id` IS NULL' );
		PREPARE kan145_stmt FROM @kan145_sql;
		EXECUTE kan145_stmt;
		DEALLOCATE PREPARE kan145_stmt;

		IF @kan145_orphans > 0 THEN
			SET v_message = CONCAT( p_table, '.', p_column, ': ', @kan145_orphans, ' rows point to a missing ', p_parent, '. Clean them first.' );
			SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_message;
		END IF;

		SET @kan145_sql = CONCAT(
			'ALTER TABLE `', p_table, '` ADD CONSTRAINT `', p_name, '` FOREIGN KEY (`', p_column, '`) ',
			'REFERENCES `', p_parent, '` (`Id`) ON DELETE ', p_on_delete, ' ON UPDATE RESTRICT' );
		PREPARE kan145_stmt FROM @kan145_sql;
		EXECUTE kan145_stmt;
		DEALLOCATE PREPARE kan145_stmt;
	END IF;
END$$

DELIMITER ;

-- Trees: a parent with children cannot be deleted
CALL KAN145_AddForeignKey( 'category', 'category_category_FK', 'ParentId', 'category', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'storage', 'storage_storage_FK', 'ParentId', 'storage', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'worktype', 'worktype_worktype_FK', 'ParentId', 'worktype', 'RESTRICT' );

-- Metadata
CALL KAN145_AddForeignKey( 'country', 'FK_Country_Currency_Id', 'Defaultcurrency_Id', 'currency', 'RESTRICT' );

-- Products
CALL KAN145_AddForeignKey( 'product', 'product_brand_FK', 'Brand_Id', 'brand', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'product', 'product_category_FK', 'Category_Id', 'category', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'product', 'product_storage_FK', 'Storage_Id', 'storage', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'product', 'product_unit_FK', 'Unit_Id', 'unit', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'productsupplier', 'FK_ProdSupplier_Product_Id', 'Product_Id', 'product', 'CASCADE' );
CALL KAN145_AddForeignKey( 'productsupplier', 'FK_ProdSupplier_Supplier_Id', 'Supplier_Id', 'supplier', 'CASCADE' );

-- Suppliers
CALL KAN145_AddForeignKey( 'supplier', 'FK_Supplier_Country_Id', 'Country_Id', 'country', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'supplier', 'FK_Supplier_Currency_Id', 'Currency_Id', 'currency', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'suppliercontact', 'FK_SupContact_Contacttype_Id', 'Contacttype_Id', 'contacttype', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'suppliercontact', 'FK_SupContact_Supplier_Id', 'Supplier_Id', 'supplier', 'CASCADE' );

-- Orders
CALL KAN145_AddForeignKey( 'supplyorder', 'FK_Order_Supplier_Id', 'Supplier_Id', 'supplier', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'supplyorder', 'supplyorder_currency_FK', 'Currency_Id', 'currency', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'supplyorderline', 'FK_OrderRow_Product_Id', 'Product_Id', 'product', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'supplyorderline', 'FK_OrderRow_Supplyorder_Id', 'Supplyorder_Id', 'supplyorder', 'RESTRICT' );

-- Stock log
CALL KAN145_AddForeignKey( 'stocklog', 'FK_StockLog_Product_Id', 'product_Id', 'product', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'stocklog', 'stocklog_productusage_FK', 'productusage_Id', 'productusage', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'stocklog', 'stocklog_supplyorder_FK', 'supplyorder_Id', 'supplyorder', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'stocklog', 'stocklog_supplyorderline_FK', 'supplyorderline_Id', 'supplyorderline', 'RESTRICT' );

-- Projects: hours and material usage
CALL KAN145_AddForeignKey( 'productusage', 'FK_ProductProduct_Id', 'product_Id', 'product', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'productusage', 'FK_ProductProject_Id', 'project_Id', 'project', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'time', 'FK_TimeProject_Id', 'project_Id', 'project', 'RESTRICT' );
CALL KAN145_AddForeignKey( 'time', 'FK_TimeWorktype_Id', 'worktype_Id', 'worktype', 'RESTRICT' );

DROP PROCEDURE IF EXISTS KAN145_AddForeignKey;

-- Check: the foreign keys now in place (expected: 26).
SELECT COUNT(*) AS ForeignKeys FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE();
SELECT TABLE_NAME, CONSTRAINT_NAME, REFERENCED_TABLE_NAME, DELETE_RULE
FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() ORDER BY TABLE_NAME, CONSTRAINT_NAME;
