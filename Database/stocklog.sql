-- Structure of table modelbuilder.stocklog (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `stocklog` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `product_Id` int NOT NULL DEFAULT '0',
  `supplyorder_Id` int DEFAULT NULL,
  `supplyorderline_Id` int DEFAULT NULL,
  `productusage_Id` int DEFAULT NULL,
  `AmountReceived` double DEFAULT '0',
  `AmountUsed` double DEFAULT '0',
  `AmountCorrection` double DEFAULT '0',
  `LogDate` date DEFAULT NULL,
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_StockLog_Product_Id` (`product_Id`) USING BTREE,
  KEY `stocklog_productusage_FK` (`productusage_Id`),
  KEY `stocklog_supplyorder_FK` (`supplyorder_Id`),
  KEY `stocklog_supplyorderline_FK` (`supplyorderline_Id`),
  CONSTRAINT `FK_StockLog_Product_Id` FOREIGN KEY (`product_Id`) REFERENCES `product` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `stocklog_productusage_FK` FOREIGN KEY (`productusage_Id`) REFERENCES `productusage` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `stocklog_supplyorder_FK` FOREIGN KEY (`supplyorder_Id`) REFERENCES `supplyorder` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `stocklog_supplyorderline_FK` FOREIGN KEY (`supplyorderline_Id`) REFERENCES `supplyorderline` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Registartion of ordered and received goods';
/*!40101 SET character_set_client = @saved_cs_client */;
