-- Structure of table modelbuilder.supplyorderline (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `supplyorderline` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Supplyorder_Id` int DEFAULT '0',
  `Product_Id` int DEFAULT '0',
  `SupplierProductName` varchar(150) DEFAULT '',
  `Amount` double DEFAULT '0',
  `OpenAmount` double DEFAULT '0',
  `Price` decimal(11,6) DEFAULT '0.000000',
  `RealRowTotal` decimal(11,6) DEFAULT '0.000000',
  `Closed` tinyint DEFAULT '0',
  `ClosedDate` date DEFAULT NULL,
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_OrderRow_Supplyorder_Id` (`Supplyorder_Id`),
  KEY `FK_OrderRow_Product_Id` (`Product_Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;
