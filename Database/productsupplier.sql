-- Structure of table modelbuilder.productsupplier (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `productsupplier` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Product_Id` int NOT NULL DEFAULT '0',
  `Supplier_Id` int NOT NULL DEFAULT '0',
  `ProductNumber` varchar(150) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ProductName` varchar(150) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Price` decimal(11,6) DEFAULT '0.000000',
  `Url` varchar(1024) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `PreferredSupplier` tinyint(1) NOT NULL DEFAULT '0',
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Product_Id` (`Product_Id`),
  KEY `Supplier_Id` (`Supplier_Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='List for all products per supplier';
/*!40101 SET character_set_client = @saved_cs_client */;
