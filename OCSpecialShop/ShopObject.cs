namespace OCSpecialShop
{
	public class ShopObject
	{
		public string Name { get; set; }
        public List<ShopItem> ShopItems {get; set;}

	}

    public class ShopItem
    {
        public List<ReceiveItem> ReceiveItems {get; set;}
        public List<ShopCost> ShopCosts {get; set;}
    }

    public class ReceiveItem
    {
        public uint ItemID {get; set;}
        public string ItemRecievedName {get; set;}
        public uint ItemCount {get; set;}
    }

    public class ShopCost
    {
        public uint ItemID {get; set;}
        public string ItemCostName {get; set;}
        public uint ItemCostValue {get; set;}
    }
}