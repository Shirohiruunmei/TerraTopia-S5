using System.IO;
using System.Text.Json;
using TShockAPI;

namespace TerraTopia.Weapon
{
    public class LuminiteConfig
    {
        public bool Enabled { get; set; } = true;
        public int ProjectileId { get; set; } = 638;
        public int Damage { get; set; } = 400;
        public bool DisableCrit { get; set; } = true;

        public static string PathFile
        {
            get
            {
                string dir = Path.Combine(TShock.SavePath, "TerraTopia");
                return Path.Combine(dir, "luminite.json");
            }
        }

        public static LuminiteConfig Load()
        {
            string dir = Path.GetDirectoryName(PathFile);
            Directory.CreateDirectory(dir);

            if (!File.Exists(PathFile))
            {
                var def = new LuminiteConfig();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(PathFile, JsonSerializer.Serialize(def, opts));
                return def;
            }

            return JsonSerializer.Deserialize<LuminiteConfig>(File.ReadAllText(PathFile)) ?? new LuminiteConfig();
        }
    }
}
