-- Structure of table modelbuilder.supplyorder (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `supplyorder` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Supplier_Id` int DEFAULT '0',
  `Currency_Id` int DEFAULT '0',
  `OrderNumber` varchar(50) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `OrderDate` date DEFAULT NULL,
  `CurrencySymbol` varchar(2) COLLATE utf8mb4_general_ci DEFAULT '€',
  `CurrencyConversionRate` double(6,4) DEFAULT '0.0000',
  `ShippingCosts` double DEFAULT '0',
  `OrderCosts` double DEFAULT '0',
  `Memo` longtext COLLATE utf8mb4_general_ci,
  `Closed` tinyint DEFAULT '0',
  `ClosedDate` date DEFAULT NULL,
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `OrderNumber` (`OrderNumber`),
  KEY `FK_Order_Supplier_Id` (`Supplier_Id`),
  KEY `supplyorder_currency_FK` (`Currency_Id`),
  CONSTRAINT `FK_Order_Supplier_Id` FOREIGN KEY (`Supplier_Id`) REFERENCES `supplier` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `supplyorder_currency_FK` FOREIGN KEY (`Currency_Id`) REFERENCES `currency` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;
