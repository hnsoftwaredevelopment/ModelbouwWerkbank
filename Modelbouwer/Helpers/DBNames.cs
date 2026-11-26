namespace Modelbouwer.Helpers;

public class DBNames
{
	#region Sql commands
	public static readonly string SqlAnd = " AND ";
	public static readonly string SqlAsc = " ASC ";
	public static readonly string SqlAs = " AS ";
	public static readonly string SqlBetween = " BETWEEN ";
	public static readonly string SqlCall = "CALL ";
	public static readonly string SqlCast = "CAST( ";
	public static readonly string SqlConcat = "CONCAT( ";
	public static readonly string SqlCount = " COUNT( ";
	public static readonly string SqlCountAll = " COUNT(*) ";
	public static readonly string SqlCountUnique = " COUNT(DISTINCT ";
	public static readonly string SqlCurrentDate = " CURDATE() ";
	public static readonly string SqlCurrentDateTime = " NOW() ";
	public static readonly string SqlCurrentTime = " CURTIME() ";
	public static readonly string SqlDelete = "DELETE ";
	public static readonly string SqlDeleteFrom = "DELETE FROM ";
	public static readonly string SqlDesc = " DESC ";
	public static readonly string SqlFrom = " FROM ";
	public static readonly string SqlIn = "IN ";
	public static readonly string SqlInnerJoin = "INNER JOIN ";
	public static readonly string SqlInsert = "INSERT INTO ";
	public static readonly string SqlIsNull = " IS NULL ";
	public static readonly string SqlLimit = " LIMIT ";
	public static readonly string SqlLimit1 = " LIMIT 1 ";
	public static readonly string SqlLike = " LIKE ";
	public static readonly string SqlLower = " LOWER ( ";
	public static readonly string SqlMax = "MAX( ";
	public static readonly string SqlMin = "MIN( ";
	public static readonly string SqlOn = " ON ";
	public static readonly string SqlOr = " OR ";
	public static readonly string SqlOrder = " ORDER BY ";
	public static readonly string SqlOrderBy = " ORDER BY ";
	public static readonly string SqlSelect = "SELECT ";
	public static readonly string SqlSelectAll = "SELECT *";
	public static readonly string SqlSelectDistinct = "SELECT DISTINCT ";
	public static readonly string SqlSelectLastId = "SELECT LAST_INSERT_ID();";
	public static readonly string SqlSet = " SET ";
	public static readonly string SqlSum = " SUM( ";
	public static readonly string SqlSubString = " SUBSTRING( ";
	public static readonly string SqlUnsigned = " as UNSIGNED) ";
	public static readonly string SqlUnionAll = "UNION ALL";
	public static readonly string SqlUpdate = "UPDATE ";
	public static readonly string SqlValues = " VALUES ";
	public static readonly string SqlWhere = " WHERE ";
	public static readonly string SqlWithRecursive = "WITH RECURSIVE ";
	#endregion

	#region Stored Procedures

	public static readonly string SPGetProductsUsageByProject = "GetProductsByProject";
	public static readonly string SPGetProductsUsageByProjectInputParameter = "?SelectedProjectId"; // The ? is added to work correctly

	#endregion

	#region Database
	public static readonly string Database = "modelbuilder";
	#endregion

	#region Settings table

	public static readonly string SettingsTable = "settings";
	public static readonly string SettingsFieldNameCulture = "Culture";
	public static readonly string SettingsFieldNameLanguage = "Language";
	public static readonly string SettingsFieldNameHourRate = "HourRate";

	public static readonly string SettingsFieldTypeCulture = "string";
	public static readonly string SettingsFieldTypeLanguage = "string";
	public static readonly string SettingsFieldTypeHourRate = "double";

	#endregion Settings table

	#region Brand table

	public static readonly string BrandTable = "brand";
	public static readonly string BrandFieldNameId = "Id";
	public static readonly string BrandFieldNameName = "Name";

	public static readonly string BrandFieldTypeId = "int";
	public static readonly string BrandFieldTypeName = "string";

	#endregion Brand table

	#region Category table

	public static readonly string CategoryTable = "category";
	public static readonly string CategoryFieldNameId = "Id";
	public static readonly string CategoryFieldNameParentId = "ParentId";
	public static readonly string CategoryFieldNameName = "Name";
	public static readonly string CategoryFieldTypeId = "int";
	public static readonly string CategoryFieldTypeParentId = "int";
	public static readonly string CategoryFieldTypeName = "string";

	#endregion Category table

	#region Contacttype table

	public static readonly string ContactTypeTable = "contacttype";
	public static readonly string ContactTypeFieldNameId = "Id";
	public static readonly string ContactTypeFieldTypeId = "Int";
	public static readonly string ContactTypeFieldNameName = "Name";
	public static readonly string ContactTypeFieldTypeName = "string";

	#endregion Contacttype table

	#region Currency table

	public static readonly string CurrencyTable = "currency";
	public static readonly string CurrencyFieldNameId = "Id";
	public static readonly string CurrencyFieldTypeId = "int";
	public static readonly string CurrencyFieldNameCode = "Code";
	public static readonly string CurrencyFieldTypeCode = "string";
	public static readonly string CurrencyFieldNameSymbol = "Symbol";
	public static readonly string CurrencyFieldTypeSymbol = "string";
	public static readonly string CurrencyFieldNameName = "Name";
	public static readonly string CurrencyFieldTypeName = "string";
	public static readonly string CurrencyFieldNameRate = "ConversionRate";
	public static readonly string CurrencyFieldTypeRate = "double";

	#endregion Currency table

	#region Country table

	public static readonly string CountryTable = "country";
	public static readonly string CountryFieldNameId = "Id";
	public static readonly string CountryFieldTypeId = "int";
	public static readonly string CountryFieldNameCode = "Code";
	public static readonly string CountryFieldTypeCode = "string";
	public static readonly string CountryFieldNameCurrencySymbol = "Defaultcurrency_Symbol";
	public static readonly string CountryFieldTypeCurrencySymbol = "string";
	public static readonly string CountryFieldNameName = "Name";
	public static readonly string CountryFieldTypeName = "string";
	public static readonly string CountryFieldNameCurrencyId = "Defaultcurrency_Id";
	public static readonly string CountryFieldTypeCurrencyId = "int";

	#endregion Country table

	#region Product table
	public static readonly string ProductFieldNameBrandId = "Brand_Id";
	public static readonly string ProductFieldNameCategoryId = "Category_Id";
	public static readonly string ProductFieldNameCode = "Code";
	public static readonly string ProductFieldNameDimensions = "Dimensions";
	public static readonly string ProductFieldNameHide = "Hide";
	public static readonly string ProductFieldNameId = "Id";
	public static readonly string ProductFieldNameImage = "Image";
	public static readonly string ProductFieldNameImageRotationAngle = "ImageRotationAngle";
	public static readonly string ProductFieldNameMemo = "Memo";
	public static readonly string ProductFieldNameMinimalStock = "MinimalStock";
	public static readonly string ProductFieldNameName = "Name";
	public static readonly string ProductFieldNamePrice = "Price";
	public static readonly string ProductFieldNameProjectCosts = "ProjectCosts";
	public static readonly string ProductFieldNameStandardOrderQuantity = "StandardOrderQuantity";
	public static readonly string ProductFieldNameStorageId = "Storage_Id";
	public static readonly string ProductFieldNameUnitId = "Unit_Id";
	public static readonly string ProductFieldTypeBrandId = "int";
	public static readonly string ProductFieldTypeCategoryId = "int";
	public static readonly string ProductFieldTypeCode = "string";
	public static readonly string ProductFieldTypeDimensions = "string";
	public static readonly string ProductFieldTypeHide = "int";
	public static readonly string ProductFieldTypeId = "int";
	public static readonly string ProductFieldTypeImage = "blob";
	public static readonly string ProductFieldTypeImageRotationAngle = "string";
	public static readonly string ProductFieldTypeMemo = "longtext";
	public static readonly string ProductFieldTypeMinimalStock = "double";
	public static readonly string ProductFieldTypeName = "string";
	public static readonly string ProductFieldTypePrice = "double";
	public static readonly string ProductFieldTypeProjectCosts = "int";
	public static readonly string ProductFieldTypeStandardOrderQuantity = "double";
	public static readonly string ProductFieldTypeStorageId = "int";
	public static readonly string ProductFieldTypeUnitId = "int";
	public static readonly string ProductTable = "product";

	#endregion Product table

	#region Product Inventory table and view
	public static readonly string ProductInventoryViewFieldNameProductId = "Product_Id";
	public static readonly string ProductInventoryViewFieldNameProductCode = "Code";
	public static readonly string ProductInventoryViewFieldNameProductName = "Name";
	public static readonly string ProductInventoryViewFieldNameProductPrice = "Price";
	public static readonly string ProductInventoryViewFieldNameProductMinimalStock = "MinimalStock";
	public static readonly string ProductInventoryViewFieldNameProductCategory = "Category";
	public static readonly string ProductInventoryViewFieldNameProductStorage = "Location";
	public static readonly string ProductInventoryViewFieldNameProductMinimalInventory = "InventoryOrder";
	public static readonly string ProductInventoryViewFieldNameProductMinimalValue = "Value";
	public static readonly string ProductInventoryViewFieldNameProductMinimalOrder = "InOrder";
	public static readonly string ProductInventoryViewFieldNameProductMinimalVirtualInventory = "VirtualInventory";
	public static readonly string ProductInventoryViewFieldNameProductMinimalVirtualValue = "VirtualValue";
	public static readonly string ProductInventoryViewFieldNameProductShort = "Short";
	public static readonly string ProductInventoryViewFieldNameProductTempShort = "TempShort";

	#endregion

	#region ProductSupplier table

	public static readonly string ProductSupplierTable = "productsupplier";
	public static readonly string ProductSupplierFieldNameId = "Id";
	public static readonly string ProductSupplierFieldTypeId = "int";
	public static readonly string ProductSupplierFieldNameProductId = "Product_Id";
	public static readonly string ProductSupplierFieldTypeProductId = "int";
	public static readonly string ProductSupplierFieldNameProductCode = "Code";          // Not in table only needed for searching the Product table on import
	public static readonly string ProductSupplierFieldTypeProductCode = "string";        // Not in table only needed for searching the Product table on import
	public static readonly string ProductSupplierFieldNameSupplierId = "Supplier_Id";
	public static readonly string ProductSupplierFieldTypeSupplierId = "int";
	public static readonly string ProductSupplierFieldNameCurrencyId = "Currency_Id";
	public static readonly string ProductSupplierFieldTypeCurrencyId = "int";
	public static readonly string ProductSupplierFieldNameProductNumber = "ProductNumber";
	public static readonly string ProductSupplierFieldTypeProductNumber = "string";
	public static readonly string ProductSupplierFieldNameProductName = "ProductName";
	public static readonly string ProductSupplierFieldTypeProductName = "string";
	public static readonly string ProductSupplierFieldNamePrice = "Price";
	public static readonly string ProductSupplierFieldTypePrice = "double";
	public static readonly string ProductSupplierFieldNameProductUrl = "Url";
	public static readonly string ProductSupplierFieldTypeProductUrl = "string";
	public static readonly string ProductSupplierFieldNameDefaultSupplier = "DefaultSupplier";
	public static readonly string ProductSupplierFieldTypeDefaultSupplier = "string";
	public static readonly string ProductSupplierFieldNameSupplierName = "SupplierName";
	public static readonly string ProductSupplierFieldTypeSupplierName = "string";

	#endregion ProductSupplier table

	#region Project table
	public static readonly string ProjectTable = "project";
	public static readonly string ProjectFieldNameId = "Id";
	public static readonly string ProjectFieldTypeId = "int";
	public static readonly string ProjectFieldNameName = "Name";
	public static readonly string ProjectFieldTypeName = "string";
	public static readonly string ProjectFieldNameCode = "Code";
	public static readonly string ProjectFieldTypeCode = "string";
	public static readonly string ProjectFieldNameStartDate = "StartDate";
	public static readonly string ProjectFieldTypeStartDate = "date";
	public static readonly string ProjectFieldNameEndDate = "EndDate";
	public static readonly string ProjectFieldTypeEndDate = "date";
	public static readonly string ProjectFieldNameClosed = "Closed";
	public static readonly string ProjectFieldTypeClosed = "int";
	public static readonly string ProjectFieldNameExpectedTime = "ExpectedTime";
	public static readonly string ProjectFieldTypeExpectedTime = "int";
	public static readonly string ProjectFieldNameMemo = "Memo";
	public static readonly string ProjectFieldTypeMemo = "longtext";
	public static readonly string ProjectFieldNameImageRotationAngle = "ImageRotationAngle";
	public static readonly string ProjectFieldTypeImageRotationAngle = "string";
	public static readonly string ProjectFieldNameImage = "Image";
	public static readonly string ProjectFieldTypeImage = "blob";

	public static readonly string SPProjectExpectedEndDate = "GetProjectEndDate";
	#endregion Project table

	#region Products in stock
	#region Stocklog table
	public static readonly string StockLogTable = "stocklog";
	public static readonly string GetCategoryFunction = "GetCategoryPath";
	public static readonly string GetStockLocationFunction = "GetStockLocationPath";
	public static readonly string StockLogFieldNameId = "Id";
	public static readonly string StockLogFieldTypeId = "int";
	public static readonly string StockLogFieldNameProductId = "product_Id";
	public static readonly string StockLogFieldTypeProductId = "int";
	public static readonly string StockLogFieldNameStorageId = "storage_Id";
	public static readonly string StockLogFieldTypeStorageId = "int";
	public static readonly string StockLogFieldNameSupplyOrderId = "supplyorder_Id";
	public static readonly string StockLogFieldTypeSupplyOrderId = "int";
	public static readonly string StockLogFieldNameProductUsageId = "productusage_Id";
	public static readonly string StockLogFieldTypeProductUsageId = "int";
	public static readonly string StockLogFieldNameSupplyOrderLineId = "supplyorderline_Id";
	public static readonly string StockLogFieldTypeSupplyOrderLineId = "int";
	public static readonly string StockLogFieldNameAmountReceived = "AmountReceived";
	public static readonly string StockLogFieldTypeAmountReceived = "double";
	public static readonly string StockLogFieldNameAmountUsed = "AmountUsed";
	public static readonly string StockLogFieldTypeAmountUsed = "double";
	public static readonly string StockLogFieldNameAmountCorrection = "AmountCorrection";
	public static readonly string StockLogFieldTypeAmountCorrection = "double";
	public static readonly string StockLogFieldNameLogDate = "LogDate";
	public static readonly string StockLogFieldTypeLogDate = "date";
	#endregion Stocklog table

	#endregion

	#region Storage table

	public static readonly string StorageTable = "storage";
	public static readonly string StorageFieldNameId = "Id";
	public static readonly string StorageFieldTypeId = "int";
	public static readonly string StorageFieldNameParentId = "ParentId";
	public static readonly string StorageFieldTypeParentId = "int";
	public static readonly string StorageFieldNameName = "Name";
	public static readonly string StorageFieldTypeName = "string";
	public static readonly string StorageFieldNameFullPath = "Fullpath";
	public static readonly string StorageFieldTypeFullPath = "string";

	#endregion Storage table

	#region Supplier tables
	#region Supplier table

	public static readonly string SupplierTable = "supplier";
	public static readonly string SupplierFieldNameId = "Id";
	public static readonly string SupplierFieldTypeId = "int";
	public static readonly string SupplierFieldNameCode = "Code";
	public static readonly string SupplierFieldTypeCode = "string";
	public static readonly string SupplierFieldNameName = "Name";
	public static readonly string SupplierFieldTypeName = "string";
	public static readonly string SupplierFieldNameAddress1 = "Address1";
	public static readonly string SupplierFieldTypeAddress1 = "string";
	public static readonly string SupplierFieldNameAddress2 = "Address2";
	public static readonly string SupplierFieldTypeAddress2 = "string";
	public static readonly string SupplierFieldNameZip = "Zip";
	public static readonly string SupplierFieldTypeZip = "string";
	public static readonly string SupplierFieldNameCity = "City";
	public static readonly string SupplierFieldTypeCity = "string";
	public static readonly string SupplierFieldNameUrl = "Url";
	public static readonly string SupplierFieldTypeUrl = "string";
	public static readonly string SupplierFieldNameMemo = "Memo";
	public static readonly string SupplierFieldTypeMemo = "longtext";
	public static readonly string SupplierFieldNameCountryId = "Country_Id";
	public static readonly string SupplierFieldTypeCountryId = "int";
	public static readonly string SupplierFieldNameCurrencyId = "Currency_Id";
	public static readonly string SupplierFieldTypeCurrencyId = "int";
	public static readonly string SupplierFieldNameShippingCosts = "ShippingCosts";
	public static readonly string SupplierFieldTypeShippingCosts = "double";
	public static readonly string SupplierFieldNameMinShippingCosts = "MinShippingCosts";
	public static readonly string SupplierFieldTypeMinShippingCosts = "double";
	public static readonly string SupplierFieldNameOrderCosts = "OrderCosts";
	public static readonly string SupplierFieldTypeOrderCosts = "double";
	public static readonly string SupplierFieldNameMinOrderCosts = "MinOrderCosts";
	public static readonly string SupplierFieldTypeMinOrderCosts = "double";
	public static readonly string SupplierFieldNameGeneralEmail = "GeneralEmail";
	public static readonly string SupplierFieldTypeGeneralEmail = "string";
	public static readonly string SupplierFieldNameGeneralPhone = "GeneralPhone";
	public static readonly string SupplierFieldTypeGeneralPhone = "string";
	#endregion Supplier table

	#region SupplierContact Table
	public static readonly string SupplierContactTable = "suppliercontact";
	public static readonly string SupplierContactFieldNameId = "Id";
	public static readonly string SupplierContactFieldTypeId = "int";
	public static readonly string SupplierContactFieldNameSupplierId = "Supplier_Id";
	public static readonly string SupplierContactFieldTypeSupplierId = "int";
	public static readonly string SupplierContactFieldNameName = "Name";
	public static readonly string SupplierContactFieldTypeName = "string";
	public static readonly string SupplierContactFieldNameTypeId = "Contacttype_Id";
	public static readonly string SupplierContactFieldTypeTypeId = "int";
	public static readonly string SupplierContactFieldNameTypeName = "ContactType";
	public static readonly string SupplierContactFieldTypeTypeName = "string";
	public static readonly string SupplierContactFieldNamePhone = "Phone";
	public static readonly string SupplierContactFieldTypePhone = "string";
	public static readonly string SupplierContactFieldNameMail = "Mail";
	public static readonly string SupplierContactFieldTypeMail = "string";
	#endregion SupplierContact Table

	#endregion Supplier tables

	#region Time

	public static readonly string TimeTable = "time";
	public static readonly string TimeFieldNameId = "Id";
	public static readonly string TimeFieldTypeId = "int";
	public static readonly string TimeFieldNameProjectId = "project_Id";
	public static readonly string TimeFieldTypeProjectId = "int";
	public static readonly string TimeFieldNameWorkTypeId = "Worktype_Id";
	public static readonly string TimeFieldTypeWorkTypeId = "int";
	public static readonly string TimeFieldNameWorkDate = "WorkDate";
	public static readonly string TimeFieldTypeWorkDate = "date";
	public static readonly string TimeFieldNameStartTime = "StartTime";
	public static readonly string TimeFieldTypeStartTime = "time";
	public static readonly string TimeFieldNameEndTime = "EndTime";
	public static readonly string TimeFieldTypeEndTime = "time";
	public static readonly string TimeFieldNameComment = "Comment";
	public static readonly string TimeFieldTypeComment = "string";

	public static readonly string TimeView = "view_time";
	public static readonly string TimeViewFieldNameId = "Id";
	public static readonly string TimeViewFieldTypeId = "int";
	public static readonly string TimeViewFieldNameProjectId = "ProjectId";
	public static readonly string TimeViewFieldTypeProjectId = "int";
	public static readonly string TimeViewFieldNameProjectName = "ProjectName";
	public static readonly string TimeViewFieldTypeProjectName = "string";
	public static readonly string TimeViewFieldNameWorkTypeId = "WorktypeId";
	public static readonly string TimeViewFieldTypeWorkTypeId = "int";
	public static readonly string TimeViewFieldNameWorkTypeName = "WorktypeName";
	public static readonly string TimeViewFieldTypeWorkTypeName = "string";
	public static readonly string TimeViewFieldNameWorkDate = "WorkDate";
	public static readonly string TimeViewFieldTypeWorkDate = "date";
	public static readonly string TimeViewFieldNameStartTime = "StartTime";
	public static readonly string TimeViewFieldTypeStartTime = "string";
	public static readonly string TimeViewFieldNameEndTime = "EndTime";
	public static readonly string TimeViewFieldTypeEndTime = "string";
	public static readonly string TimeViewFieldNameElapsedTime = "ELapsedTime";
	public static readonly string TimeViewFieldTypeElapsedTime = "string";
	public static readonly string TimeViewFieldNameElapsedMinutes = "ELapsedMinutes";
	public static readonly string TimeViewFieldTypeElapsedMinutes = "double";
	public static readonly string TimeViewFieldNameComment = "Comment";
	public static readonly string TimeViewFieldTypeComment = "string";
	public static readonly string TimeViewFieldNameYear = "Year";
	public static readonly string TimeViewFieldTypeYear = "int";
	public static readonly string TimeViewFieldNameMonth = "Month";
	public static readonly string TimeViewFieldTypeMonth = "int";
	public static readonly string TimeViewFieldNameWorkday = "Workday";
	public static readonly string TimeViewFieldTypeWorkday = "int";
	public static readonly string TimeViewFieldNameYearMonth = "YearMonth";
	public static readonly string TimeViewFieldTypeYearMonth = "string";
	public static readonly string TimeViewFieldNameYearWorkday = "YearWorkday";
	public static readonly string TimeViewFieldTypeYearWorkday = "string";
	public static readonly string TimeViewFieldNameSortIndex = "SortIndex";
	public static readonly string TimeViewFieldTypeSortIndex = "string";

	#endregion Time

	#region ProductUsage

	public static readonly string ProductUsageTable = "productusage";
	public static readonly string ProductUsageFieldNameId = "Id";
	public static readonly string ProductUsageFieldTypeId = "int";
	public static readonly string ProductUsageFieldNameProjectId = "project_Id";
	public static readonly string ProductUsageFieldTypeProjectId = "int";
	public static readonly string ProductUsageFieldNameProductId = "product_Id";
	public static readonly string ProductUsageFieldTypeProductId = "int";
	public static readonly string ProductUsageFieldNameStorageId = "storage_Id";
	public static readonly string ProductUsageFieldTypeStorageId = "int";
	public static readonly string ProductUsageFieldNameAmountUsed = "AmountUsed";
	public static readonly string ProductUsageFieldTypeAmountUsed = "double";
	public static readonly string ProductUsageFieldNameUsageDate = "UsageDate";
	public static readonly string ProductUsageFieldTypeUsageDate = "date";
	public static readonly string ProductUsageFieldNameComment = "Comment";
	public static readonly string ProductUsageFieldTypeComment = "string";

	public static readonly string ProductUsageViewFieldNameProductName = "_productName";
	public static readonly string ProductUsageViewFieldTypeCategoryName = "string";
	public static readonly string ProductUsageViewFieldNameAmountUsed = "AmountUsed";
	public static readonly string ProductUsageViewFieldNamePrice = "Price";
	public static readonly string ProductUsageViewFieldNameTotalCosts = "TotalCost";
	public static readonly string ProductUsageViewFieldNameComment = "Comment";

	#endregion ProductUsage

	#region Order table and view
	public static readonly string OrderTable = "supplyorder";
	public static readonly string OrderFieldNameId = "Id";
	public static readonly string OrderFieldTypeId = "int";
	public static readonly string OrderFieldNameClosed = "Closed";
	public static readonly string OrderFieldTypeClosed = "int";
	public static readonly string OrderFieldNameClosedDate = "ClosedDate";
	public static readonly string OrderFieldTypeClosedDate = "date";
	public static readonly string OrderFieldNameSupplierId = "Supplier_Id";
	public static readonly string OrderFieldTypeSupplierId = "int";
	public static readonly string OrderFieldNameCurrencyId = "Currency_Id";
	public static readonly string OrderFieldTypeCurrencyId = "int";
	public static readonly string OrderFieldNameOrderNumber = "OrderNumber";
	public static readonly string OrderFieldTypeOrderNumber = "string";
	public static readonly string OrderFieldNameOrderDate = "OrderDate";
	public static readonly string OrderFieldTypeOrderDate = "date";
	public static readonly string OrderFieldNameCurrencySymbol = "CurrencySymbol";
	public static readonly string OrderFieldTypeCurrencySymbol = "string";
	public static readonly string OrderFieldNameConversionRate = "CurrencyConversionRate";
	public static readonly string OrderFieldTypeConversionRate = "double";
	public static readonly string OrderFieldNameShippingCosts = "ShippingCosts";
	public static readonly string OrderFieldTypeShippingCosts = "double";
	public static readonly string OrderFieldNameOrderCosts = "OrderCosts";
	public static readonly string OrderFieldTypeOrderCosts = "double";
	public static readonly string OrderFieldNameOrderMemo = "Memo";
	public static readonly string OrderFieldTypeOrderMemo = "longtext";

	public static readonly string OrderView = "view_supplyorder";
	public static readonly string OrderViewFieldNameId = "Id";
	public static readonly string OrderViewFieldTypeId = "int";
	public static readonly string OrderViewFieldNameSupplierId = "Supplier_Id";
	public static readonly string OrderViewFieldTypeSupplierId = "int";
	public static readonly string OrderViewFieldNameSupplierName = "SupplierName";
	public static readonly string OrderViewFieldTypeSupplierName = "string";
	public static readonly string OrderViewFieldNameShippingCosts = "ShippingCosts";
	public static readonly string OrderViewFieldTypeShippingCosts = "double";
	public static readonly string OrderViewFieldNameMinShippingCosts = "MinShippingCosts";
	public static readonly string OrderViewFieldTypeMinShippingCosts = "double";
	public static readonly string OrderViewFieldNameOrderCosts = "OrderCosts";
	public static readonly string OrderViewFieldTypeOrderCosts = "double";
	public static readonly string OrderViewFieldNameDefSupplierCurrencyId = "DefaultCurrencyIdSupplier";
	public static readonly string OrderViewFieldTypeDefSupplierCurrencyId = "int";
	public static readonly string OrderViewFieldNameDefSupplierCurrencySymbol = "DefaultCurrencySymboldSupplier";
	public static readonly string OrderViewFieldTypeDefSupplierCurrencySymbol = "string";
	public static readonly string OrderViewFieldNameDefSupplierConversionRatel = "DefaultConversionRateSupplier";
	public static readonly string OrderViewFieldTypeDefSupplierConversionRatel = "Double";
	public static readonly string OrderViewFieldNameCurrencyId = "Currency_Id";
	public static readonly string OrderViewFieldTypeCurrencyId = "int";
	public static readonly string OrderViewFieldNameDefOrderCurrencySymbol = "DefaultCurrencySymboldOrder";
	public static readonly string OrderViewFieldTypeDefOrderCurrencySymbol = "string";
	public static readonly string OrderViewFieldNameDefOrderConversionRatel = "DefaultConversionRateOrder";
	public static readonly string OrderViewFieldTypeDefOrderConversionRatel = "Double";
	public static readonly string OrderViewFieldNameOrderNumber = "OrderNumber";
	public static readonly string OrderViewFieldTypeOrderNumber = "string";
	public static readonly string OrderViewFieldNameOrderDate = "OrderDate";
	public static readonly string OrderViewFieldTypeOrderDate = "date";
	public static readonly string OrderViewFieldNameCurrencySymbol = "OrderCurrencySymbol";
	public static readonly string OrderViewFieldTypeCurrencySymbol = "string";
	public static readonly string OrderViewFieldNameConversionRate = "OrderCurrencyConversionRate";
	public static readonly string OrderViewFieldTypeConversionRate = "double";
	public static readonly string OrderViewFieldNameOrderShippingCosts = "OrderShippingCosts";
	public static readonly string OrderViewFieldTypeOrderShippingCosts = "double";
	public static readonly string OrderViewFieldNameOrderOrderCosts = "OrderOrderCosts";
	public static readonly string OrderViewFieldTypeOrderOrderCosts = "double";
	public static readonly string OrderViewFieldNameClosed = "Closed";
	public static readonly string OrderViewFieldTypeClosed = "int";
	public static readonly string OrderViewFieldNameClosedDate = "ClosedDate";
	public static readonly string OrderViewFieldTypeClosedDate = "date";
	public static readonly string OrderViewFieldNameOrderMemo = "Memo";
	public static readonly string OrderViewFieldTypeOrderMemo = "longtext";
	public static readonly string OrderViewFieldNameHasStockLog = "HasStockLog";
	public static readonly string OrderViewFieldTypeHasStockLog = "int";

	public static readonly string OrderLineTable = "supplyorderline";
	public static readonly string OrderLineFieldNameId = "Id";
	public static readonly string OrderLineFieldTypeId = "int";
	public static readonly string OrderLineFieldNameSupplierOrderId = "SupplyOrder_Id";
	public static readonly string OrderLineFieldTypeSupplierOrderId = "int";
	public static readonly string OrderLineFieldNameSupplierId = "Supplier_Id";
	public static readonly string OrderLineFieldTypeSupplierId = "int";
	public static readonly string OrderLineFieldNameProductId = "Product_Id";
	public static readonly string OrderLineFieldTypeProductId = "int";
	public static readonly string OrderLineFieldNameProjectId = "Project_Id";
	public static readonly string OrderLineFieldTypeProjectId = "int";
	public static readonly string OrderLineFieldNameCategoryId = "Category_Id";
	public static readonly string OrderLineFieldTypeCategoryId = "int";
	public static readonly string OrderLineFieldNameSupplierProductName = "SupplierProductName";
	public static readonly string OrderLineFieldTypeSupplierProductName = "string";
	public static readonly string OrderLineFieldNameOrderId = "supplyorder_Id";
	public static readonly string OrderLineFieldTypeOrderId = "int";
	public static readonly string OrderLineFieldNameAmount = "Amount";
	public static readonly string OrderLineFieldTypeAmount = "double";
	public static readonly string OrderLineFieldNameOpenAmount = "OpenAmount";
	public static readonly string OrderLineFieldTypeOpenAmount = "double";
	public static readonly string OrderLineFieldNamePrice = "Price";
	public static readonly string OrderLineFieldTypePrice = "double";
	public static readonly string OrderLineFieldNameRealRowTotal = "RealRowTotal";
	public static readonly string OrderLineFieldTypeRealRowTotal = "double";
	public static readonly string OrderLineFieldNameClosed = "Closed";
	public static readonly string OrderLineFieldTypeClosed = "int";
	public static readonly string OrderLineFieldNameClosedDate = "ClosedDate";
	public static readonly string OrderLineFieldTypeClosedDate = "date";

	public static readonly string OrderLineViewFieldNameOrderId = "Order_Id";
	public static readonly string OrderLineViewFieldNameSupplierId = "SupplierId";
	public static readonly string OrderLineViewFieldNameProductId = "ProductId";
	public static readonly string OrderLineViewFieldNameProductCode = "ProductCode";
	public static readonly string OrderLineViewFieldNameProductName = "ProductName";
	public static readonly string OrderLineViewFieldNameReceived = "Received";
	public static readonly string OrderLineViewFieldNameExpected = "Expect";
	public static readonly string OrderLineViewFieldNameClosed = "Closed";
	public static readonly string OrderLineViewFieldNameClosedDate = "ClosedDate";

	#endregion

	#region Unit Table

	public static readonly string UnitTable = "unit";
	public static readonly string UnitFieldNameUnitId = "Id";
	public static readonly string UnitFieldTypeUnitId = "int";
	public static readonly string UnitFieldNameUnitName = "Name";
	public static readonly string UnitFieldTypeUnitName = "string";

	#endregion Unit Table

	#region Worktype Table and view
	public static readonly string WorkTypeTable = "worktype";
	public static readonly string WorkTypeFieldNameId = "Id";
	public static readonly string WorkTypeFieldTypeId = "int";
	public static readonly string WorkTypeFieldNameParentId = "ParentId";
	public static readonly string WorkTypeFieldTypeParentId = "int";
	public static readonly string WorkTypeFieldNameName = "Name";
	public static readonly string WorkTypeFieldTypeName = "string";
	public static readonly string WorkTypeFieldNameFullPath = "FullPath";
	public static readonly string WorkTypeFieldTypeFullPath = "string";

	#endregion
}
