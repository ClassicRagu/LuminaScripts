using System.Reflection.Metadata.Ecma335;
using Lumina;
using Lumina.Data.Files;
using Lumina.Excel.Sheets.Experimental;
using Newtonsoft.Json;
using OCSpecialShop;
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
#pragma warning disable PendingExcelSchema // Non-experimental doesn't have defined offsets
        var specialShops = lumina.GetExcelSheet<SpecialShop>();
        var enlightenedCoins = specialShops?.Where(x => x.Name.ExtractText().ToLowerInvariant().Contains("enlightenment"));
        var sanguini = specialShops?.Where(x => x.Name.ExtractText().ToLowerInvariant().Contains("sanguinite"));
        var arcanautAugmentation = specialShops?.Where(x => x.Name.ExtractText().ToLowerInvariant().Contains("arcanaut"));

        ProcessSpecialShop(directoryPath, enlightenedCoins);
        ProcessSpecialShop(directoryPath, sanguini);
        ProcessSpecialShop(directoryPath, arcanautAugmentation);
    }

    static void ProcessSpecialShop (string directoryPath, IEnumerable<SpecialShop> specialShops)
    {
        foreach (var shop in specialShops)
        {
            ShopObject shopObject = new ShopObject();
            shopObject.Name = shop.Name.ExtractText();
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
                        ItemCostName = x.ItemCost.Value.Name.ExtractText(),
                        ItemCostValue = x.CurrencyCost
                    }).ToList();

                    shopItems.Add(shopItem);
                }
            }
            
            shopObject.ShopItems = shopItems;
            
            File.WriteAllText(Path.Combine(directoryPath, $"{shopObject.Name}.json"), JsonConvert.SerializeObject(shopItems, Formatting.Indented));
        }
    }
}