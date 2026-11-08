using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TShockAPI;

namespace TerraTopia.BossBlock
{
    public class BossBlockConfig
    {
        public bool Enabled { get; set; } = true;
        public string Message { get; set; } = "Boss ini dinonaktifkan di server ini.";

        public List<int> BlockedNpcIds { get; set; } = new List<int>
        {
            4, 13, 14, 15, 35, 50, 113, 125, 126, 127, 134,
            222, 245, 262, 266, 370, 396, 439, 636, 657
        };

        public static BossBlockConfig Load()
        {
            string dir = Path.Combine(TShock.SavePath, "TerraTopia");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "bossblock.json");

            if (!File.Exists(path))
            {
                var def = new BossBlockConfig();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(def, opts));
                return def;
            }

            return JsonSerializer.Deserialize<BossBlockConfig>(File.ReadAllText(path)) ?? new BossBlockConfig();
        }
    }
}
