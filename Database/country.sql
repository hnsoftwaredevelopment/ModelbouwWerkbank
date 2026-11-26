-- Structure of table modelbuilder.country (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `country` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Code` varchar(4) DEFAULT '',
  `Defaultcurrency_Symbol` varchar(2) NOT NULL DEFAULT '€',
  `Name` varchar(45) DEFAULT NULL,
  `Defaultcurrency_Id` int DEFAULT NULL,
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `country_Code_UNIQUE` (`Code`) USING BTREE,
  KEY `FK_Country_Currency_Id` (`Defaultcurrency_Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
