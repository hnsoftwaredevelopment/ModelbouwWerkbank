-- Structure of table modelbuilder.productusage (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `productusage` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `project_Id` int NOT NULL DEFAULT '0',
  `product_Id` int NOT NULL DEFAULT '0',
  `UsageDate` date NOT NULL,
  `AmountUsed` double NOT NULL DEFAULT '0',
  `Comment` varchar(1050) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Modifed` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `FK_Project_Id` (`project_Id`),
  KEY `FK_Product_Id` (`product_Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Store the product usage as entered in the Time Management page. Part of the data is equal to the data in the StockLog, but because of Foreign keys necesary for relating to Project and Product age a separate tabble is needed.\r\nSame data , as needed in the Stocklog table, is also stored in the stocklog.';
/*!40101 SET character_set_client = @saved_cs_client */;
