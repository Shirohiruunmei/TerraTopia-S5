using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TShockAPI;

namespace TerraTopia.Spawn
{
    public class SpawnGuardConfig
    {
        public bool Enabled { get; set; } = true;
        public List<string> RegionNames { get; set; } = new List<string> { "Spawn", "Shop" };
        public int CheckEveryTicks { get; set; } = 60;

        public static SpawnGuardConfig Load()
        {
            string dir = Path.Combine(TShock.SavePath, "TerraTopia");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "spawnguard.json");

            if (!File.Exists(path))
            {
                var def = new SpawnGuardConfig();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(def, opts));
                return def;
            }

            return JsonSerializer.Deserialize<SpawnGuardConfig>(File.ReadAllText(path)) ?? new SpawnGuardConfig();
        }
    }
}
