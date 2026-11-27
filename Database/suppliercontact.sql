-- Structure of table modelbuilder.suppliercontact (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `suppliercontact` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Supplier_Id` int DEFAULT '0',
  `Name` varchar(150) COLLATE utf8mb4_general_ci DEFAULT '',
  `Contacttype_Id` int DEFAULT '1',
  `Mail` varchar(150) COLLATE utf8mb4_general_ci DEFAULT '',
  `Phone` varchar(150) COLLATE utf8mb4_general_ci DEFAULT '',
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `Supplier_Id` (`Supplier_Id`),
  KEY `Contacttype_Id` (`Contacttype_Id`),
  KEY `Name` (`Name`),
  CONSTRAINT `FK_SupContact_Contacttype_Id` FOREIGN KEY (`Contacttype_Id`) REFERENCES `contacttype` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_SupContact_Supplier_Id` FOREIGN KEY (`Supplier_Id`) REFERENCES `supplier` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
