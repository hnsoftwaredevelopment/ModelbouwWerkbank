-- Structure of table modelbuilder.product (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `product` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Code` varchar(30) DEFAULT NULL,
  `Name` varchar(150) DEFAULT NULL,
  `Dimensions` varchar(150) DEFAULT '',
  `Price` double DEFAULT '0',
  `MinimalStock` double DEFAULT '0',
  `StandardOrderQuantity` double DEFAULT '0',
  `ProjectCosts` int DEFAULT '0',
  `Unit_Id` int DEFAULT NULL,
  `ImageRotationAngle` varchar(4) DEFAULT '0',
  `Image` longblob,
  `Brand_Id` int DEFAULT NULL,
  `Category_Id` int DEFAULT NULL,
  `Storage_Id` int DEFAULT '1',
  `Memo` longtext,
  `Hide` tinyint(1) DEFAULT '0',
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Code` (`Code`),
  KEY `Brand_Id` (`Brand_Id`),
  KEY `Category_Id` (`Category_Id`),
  KEY `Unit_Id` (`Unit_Id`),
  KEY `Storage_Id` (`Storage_Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
