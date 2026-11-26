-- Structure of table modelbuilder.currency (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `currency` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Code` varchar(4) NOT NULL DEFAULT '',
  `Symbol` varchar(2) NOT NULL DEFAULT '',
  `Name` varchar(45) NOT NULL,
  `ConversionRate` double(6,4) DEFAULT '1.0000',
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `currency_Code` (`Code`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
