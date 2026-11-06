using System.IO;
using System.Text.Json;
using TShockAPI;

namespace TerraTopia.Economy
{
    public class EconomyConfig
    {
        public string CurrencyName { get; set; } = "ShiroCoin";
        public long MobReward { get; set; } = 1;
        public long BossReward { get; set; } = 50;

        public static EconomyConfig Load()
        {
            string dir = Path.Combine(TShock.SavePath, "TerraTopia");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "economy.json");

            if (!File.Exists(path))
            {
                var def = new EconomyConfig();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(def, opts));
                return def;
            }

            return JsonSerializer.Deserialize<EconomyConfig>(File.ReadAllText(path)) ?? new EconomyConfig();
        }
    }
}
