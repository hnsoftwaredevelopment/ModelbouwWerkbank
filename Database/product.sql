-- Structure of table modelbuilder.product (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `product` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Code` varchar(30) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Name` varchar(150) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Dimensions` varchar(150) COLLATE utf8mb4_general_ci DEFAULT '',
  `Price` double DEFAULT '0',
  `MinimalStock` double DEFAULT '0',
  `StandardOrderQuantity` double DEFAULT '0',
  `ProjectCosts` int DEFAULT '0',
  `Unit_Id` int DEFAULT NULL,
  `ImageRotationAngle` varchar(4) COLLATE utf8mb4_general_ci DEFAULT '0',
  `Image` longblob,
  `Brand_Id` int DEFAULT NULL,
  `Category_Id` int DEFAULT NULL,
  `Storage_Id` int DEFAULT '1',
  `Memo` longtext COLLATE utf8mb4_general_ci,
  `Hide` tinyint(1) DEFAULT '0',
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Code` (`Code`),
  KEY `Brand_Id` (`Brand_Id`),
  KEY `Category_Id` (`Category_Id`),
  KEY `Unit_Id` (`Unit_Id`),
  KEY `Storage_Id` (`Storage_Id`),
  CONSTRAINT `product_brand_FK` FOREIGN KEY (`Brand_Id`) REFERENCES `brand` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `product_category_FK` FOREIGN KEY (`Category_Id`) REFERENCES `category` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `product_storage_FK` FOREIGN KEY (`Storage_Id`) REFERENCES `storage` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `product_unit_FK` FOREIGN KEY (`Unit_Id`) REFERENCES `unit` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
