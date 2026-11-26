-- Structure of table modelbuilder.project (schema only, no data). Generated from the local database (KAN-79).

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `project` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Code` varchar(20) COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `Name` varchar(150) COLLATE utf8mb4_general_ci DEFAULT '',
  `StartDate` date DEFAULT NULL,
  `EndDate` date DEFAULT NULL,
  `ExpectedTime` int DEFAULT NULL,
  `Image` longblob,
  `ImageRotationAngle` varchar(4) COLLATE utf8mb4_general_ci DEFAULT '0',
  `Closed` tinyint DEFAULT '0',
  `Memo` longtext COLLATE utf8mb4_general_ci,
  `Created` datetime DEFAULT CURRENT_TIMESTAMP,
  `Modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
