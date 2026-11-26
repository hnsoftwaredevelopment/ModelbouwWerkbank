-- Structure of table modelbuilder.supplier (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `supplier` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Code` varchar(20) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Name` varchar(150) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Address1` varchar(150) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Address2` varchar(150) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Zip` varchar(15) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `City` varchar(40) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Url` varchar(255) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ShippingCosts` double DEFAULT '0',
  `MinShippingCosts` double DEFAULT '0',
  `OrderCosts` double DEFAULT '0',
  `MinOrderCosts` double DEFAULT '0',
  `GeneralEmail` varchar(250) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `GeneralPhone` varchar(20) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Memo` longtext COLLATE utf8mb4_general_ci,
  `Currency_Id` int DEFAULT '1',
  `Country_Id` int DEFAULT '1',
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `supplier_Code` (`Code`) USING BTREE,
  KEY `Currency_Id` (`Currency_Id`) USING BTREE,
  KEY `Country_Id` (`Country_Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
