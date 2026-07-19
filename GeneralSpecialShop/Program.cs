using System.Reflection.Metadata.Ecma335;
using Lumina;
using Lumina.Data.Files;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using GeneralSpecialShop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class Program
{

    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
    private const string IconHDFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please include a file path");
            return;
        }

        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"json/");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string xivPath = args[0];

        var lumina = new GameData(xivPath, new() { DefaultExcelLanguage = Lumina.Data.Language.English });
        var specialShops = lumina.GetExcelSheet<SpecialShop>();

        ProcessSpecialShop(directoryPath, lumina, specialShops);
    }

    static void ProcessSpecialShop (string directoryPath, GameData lumina, IEnumerable<SpecialShop> specialShops)
    {
        foreach (var shop in specialShops)
        {
            ShopObject shopObject = new ShopObject();
            shopObject.Name = shop.Name.ExtractText().Replace("/", " ").Replace("\\", " ");
            List<ShopItem> shopItems = new List<ShopItem>();
            foreach (var item in shop.Item)
            {
                if (item.ReceiveItems.First().Item.RowId > 0)
                {
                    ShopItem shopItem = new ShopItem();
                    shopItem.ReceiveItems = item.ReceiveItems.Where(x => { return x.Item.RowId > 0; }).Select(x => new ReceiveItem
                    {
                        ItemID = x.Item.RowId,
                        ItemRecievedName = x.Item.Value.Name.ExtractText(),
                        ItemCount = x.ReceiveCount
                    }).ToList();

                    shopItem.ShopCosts = item.ItemCosts.Where(x => { return x.ItemCost.RowId > 0; }).Select(x => new ShopCost
                    {
                        ItemID = x.ItemCost.RowId,
                        // Leaving this in incase something breaks with item name processing
                        // ItemCostName = x.ItemCost.Value.Name.ExtractText(),
                        ItemCostName = ProcessItemName(lumina, x.CostType, x.ItemCost.RowId),
                        ItemCostValue = x.CurrencyCost
                    }).ToList();

                    shopItems.Add(shopItem);
                }
            }
            
            shopObject.ShopItems = shopItems;
            
            if(shopObject.ShopItems.Count > 0)
            File.WriteAllText(Path.Combine(directoryPath, $"{shop.RowId} - {shopObject.Name}.json"), JsonConvert.SerializeObject(shopItems, Formatting.Indented));
        }
    }

    static string ProcessItemName (GameData lumina, uint costType, uint rowId)
    {
        switch (costType)
        {
            case 2:
                return lumina.GetExcelSheet<TomestonesItem>().First(x => x.Tomestones.RowId == rowId).Item.Value.Name.ExtractText();
            case 3:
                // Scrips and similar items have a special id, not clear where this is mapped in the sheets
                switch (rowId){
                    // White Crafters' Scrip
                    case 1:
                        return lumina.GetExcelSheet<Item>().GetRow(25199).Name.ExtractText();
                    // Purple Crafters' Scrip
                    case 2:
                        return lumina.GetExcelSheet<Item>().GetRow(33913).Name.ExtractText();
                    // White Gatherers' Scrip
                    case 3:
                        return lumina.GetExcelSheet<Item>().GetRow(25200).Name.ExtractText();
                    // Purple Gatherers' Scrip
                    case 4:
                        return lumina.GetExcelSheet<Item>().GetRow(33914).Name.ExtractText();
                    // Centurio Seal?
                    case 5:
                        return lumina.GetExcelSheet<Item>().GetRow(10307).Name.ExtractText();
                    // Orange Crafters' Scrip
                    case 6:
                        return lumina.GetExcelSheet<Item>().GetRow(41784).Name.ExtractText();
                    // Orange Gatherers' Scrip
                    case 7:
                        return lumina.GetExcelSheet<Item>().GetRow(41785).Name.ExtractText();
                    default:
                        return lumina.GetExcelSheet<Item>().GetRow(rowId).Name.ExtractText();
                }
            default:
                return lumina.GetExcelSheet<Item>().GetRow(rowId).Name.ExtractText();
        }
    }
}