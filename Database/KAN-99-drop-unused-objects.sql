-- KAN-99: remove database objects that nothing uses anymore.
-- Analysis 2026-11-25: not used by the desktop app, the mobile app or any object that is still in use.
-- Still in use (kept): all other tables, views view_supplyorder and view_time, functions GetCategoryPath and
-- GetStockLocationPath, procedures GetProductsByProject and GetProjectEndDate.
--
-- Run once per database, after a backup. Safe to run again. Compatible with MySQL 8 and MariaDB 10.3.

-- Views (first: some of them read the tables below)
DROP VIEW IF EXISTS view_country;
DROP VIEW IF EXISTS view_groupedtimetotals;
DROP VIEW IF EXISTS view_stock;
DROP VIEW IF EXISTS view_instock;
DROP VIEW IF EXISTS view_product;
DROP VIEW IF EXISTS view_productinventory;
DROP VIEW IF EXISTS view_productinventory1;
DROP VIEW IF EXISTS view_productsupplier;
DROP VIEW IF EXISTS view_productusage;
DROP VIEW IF EXISTS view_projectcosts;
DROP VIEW IF EXISTS view_projectexport;
DROP VIEW IF EXISTS view_projecttotals;
DROP VIEW IF EXISTS view_receiptsreport;
DROP VIEW IF EXISTS view_supplier;
DROP VIEW IF EXISTS view_suppliercontact;
DROP VIEW IF EXISTS view_supplyopenorder;
DROP VIEW IF EXISTS view_supplyopenorderdetails;
DROP VIEW IF EXISTS view_supplyopenorderline;
DROP VIEW IF EXISTS view_supplyorderline;
DROP VIEW IF EXISTS view_supplyorderlineshort;
DROP VIEW IF EXISTS view_timeexport;
DROP VIEW IF EXISTS view_timereport;

-- Procedures
DROP PROCEDURE IF EXISTS DeleteProductId;
DROP PROCEDURE IF EXISTS GetDaylyWorkedHours;
DROP PROCEDURE IF EXISTS GetLatestAddedRecord;
DROP PROCEDURE IF EXISTS GetMonthlyWorkedHours;
DROP PROCEDURE IF EXISTS GetOrderHistoryBySupplier;
DROP PROCEDURE IF EXISTS GetProductsBySupplier;
DROP PROCEDURE IF EXISTS GetProductStockMovements;
DROP PROCEDURE IF EXISTS GetProductsUsage;
DROP PROCEDURE IF EXISTS GetShortListProductsBySupplier;
DROP PROCEDURE IF EXISTS GetSupplyOrderDetails;
DROP PROCEDURE IF EXISTS GetTotalCostByProjectId;
DROP PROCEDURE IF EXISTS GetTotalMinutesByProjectId;
DROP PROCEDURE IF EXISTS GetWorkedHoursByWorktype;
DROP PROCEDURE IF EXISTS GetYearlyWorkedHours;
DROP PROCEDURE IF EXISTS ResetDefaultSupplier;
DROP PROCEDURE IF EXISTS SetDefaultSupplier;

-- Tables: months and weekdays were only read by view_timereport; stock was never filled.
DROP TABLE IF EXISTS months;
DROP TABLE IF EXISTS weekdays;
DROP TABLE IF EXISTS stock;

-- Check: the remaining objects.
SELECT TABLE_TYPE, COUNT(*) AS Objects FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() GROUP BY TABLE_TYPE;
SELECT ROUTINE_TYPE, ROUTINE_NAME FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = DATABASE() ORDER BY ROUTINE_TYPE, ROUTINE_NAME;
