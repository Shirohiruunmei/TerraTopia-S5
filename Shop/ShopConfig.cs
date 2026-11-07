using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TShockAPI;

namespace TerraTopia.Shop
{
    public class ShopItem
    {
        public int Id { get; set; }
        public int ItemType { get; set; }
        public string Name { get; set; }
        public long Price { get; set; }
        public int MaxStack { get; set; } = 999;
    }

    public class ShopConfig
    {
        public string RegionName { get; set; } = "Shop";

        public List<ShopItem> Items { get; set; } = new List<ShopItem>
        {
            new ShopItem { Id = 1, ItemType = 8, Name = "Torch", Price = 5, MaxStack = 999 },
            new ShopItem { Id = 2, ItemType = 9, Name = "Wood", Price = 2, MaxStack = 999 }
        };

        public static ShopConfig Load()
        {
            string dir = Path.Combine(TShock.SavePath, "TerraTopia");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "shop.json");

            if (!File.Exists(path))
            {
                var def = new ShopConfig();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(def, opts));
                return def;
            }

            return JsonSerializer.Deserialize<ShopConfig>(File.ReadAllText(path)) ?? new ShopConfig();
        }
    }
}
