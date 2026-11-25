namespace Modelbouwer.Models;

/// <summary>
/// One product in the product list of the order screen (KAN-103): stock figures with the same shortage
/// rules as the stock overview, plus the supplier data of the supplier selected on the order.
/// </summary>
public partial class StockOrderProductRowModel : ObservableObject
{
	public StockOrderProductRowModel( ProductModel product )
	{
		Product = product;
	}

	public ProductModel Product { get; }

	public int ProductId => Product.ProductId;
	public string? ProductCode => Product.ProductCode;
	public string? ProductName => Product.ProductName;
	public double ProductPrice => Product.ProductPrice;

	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( ShortInventory ), nameof( TempShortInventory ), nameof( SuggestedAmount ) )]
	private double _minimalStock;

	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( ShortInventory ), nameof( TempShortInventory ), nameof( SuggestedAmount ) )]
	private double _inventory;

	/// <summary>Open quantity on saved orders.</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( ShortInventory ), nameof( TempShortInventory ), nameof( SuggestedAmount ), nameof( TotalInOrder ) )]
	private double _inOrder;

	/// <summary>Quantity on lines of the order on screen that are not saved yet.</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( ShortInventory ), nameof( TempShortInventory ), nameof( SuggestedAmount ), nameof( TotalInOrder ) )]
	private double _pendingInOrder;

	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( SuggestedAmount ) )]
	private double _standardOrderQuantity;

	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( GroupName ) )]
	private bool _isFromSelectedSupplier;

	[ObservableProperty] private string? _supplierProductNumber;

	/// <summary>Set when a supplier is selected; decides the group name.</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( GroupName ) )]
	private bool _hasSelectedSupplier;

	public double TotalInOrder => InOrder + PendingInOrder;

	/// <summary>Must be ordered: below minimum even with everything that is on order (same rule as the stock overview).</summary>
	public double ShortInventory => Math.Max( MinimalStock - ( Inventory + TotalInOrder ), 0d );

	/// <summary>Below minimum now, but the open orders make up for it.</summary>
	public double TempShortInventory => ShortInventory > 0d ? 0d : Math.Max( MinimalStock - Inventory, 0d );

	/// <summary>
	/// Shortage rounded up to the order multiple. Without a shortage: one order multiple (or 1).
	/// </summary>
	public double SuggestedAmount
	{
		get
		{
			double multiple = StandardOrderQuantity > 0d ? StandardOrderQuantity : 1d;
			double shortage = ShortInventory;
			return shortage > 0d ? Math.Ceiling( shortage / multiple ) * multiple : multiple;
		}
	}

	public string GroupName => !HasSelectedSupplier
		? Lang.StockOrderGroupAllProducts
		: IsFromSelectedSupplier ? Lang.StockOrderGroupSupplierProducts : Lang.StockOrderGroupOtherProducts;
}
