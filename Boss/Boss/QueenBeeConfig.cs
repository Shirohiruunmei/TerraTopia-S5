using System.IO;
using System.Text.Json;
using TShockAPI;

namespace TerraTopia.Boss
{
    public class QueenBeeConfig
    {
        public bool Enabled { get; set; } = true;
        public int BossNpcId { get; set; } = 222;
        public int FromProjectileId { get; set; } = 719;
        public int ToProjectileId { get; set; } = 933;
        public int Damage { get; set; } = 190;

        public static QueenBeeConfig Load()
        {
            string dir = Path.Combine(TShock.SavePath, "TerraTopia");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "queenbee.json");

            if (!File.Exists(path))
            {
                var def = new QueenBeeConfig();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(def, opts));
                return def;
            }

            return JsonSerializer.Deserialize<QueenBeeConfig>(File.ReadAllText(path)) ?? new QueenBeeConfig();
        }
    }
}
