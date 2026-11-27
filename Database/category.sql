-- Structure of table modelbuilder.category (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `category` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ParentId` int DEFAULT NULL,
  `Name` varchar(55) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Name` (`Name`),
  KEY `category_category_FK` (`ParentId`),
  CONSTRAINT `category_category_FK` FOREIGN KEY (`ParentId`) REFERENCES `category` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
