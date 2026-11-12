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

        // Batas atas damage. Nilai Damage di atas ini akan dipotong ke MaxDamage.
        public int MaxDamage { get; set; } = 5000;

        public bool DisableCrit { get; set; } = true;

        public static string PathFile
        {
            get
            {
                string dir = Path.Combine(TShock.SavePath, "TerraTopia");
                return Path.Combine(dir, "luminite.json");
            }
        }

        // Damage yang benar-benar dipakai, sudah dibatasi oleh MaxDamage.
        public int EffectiveDamage
        {
            get
            {
                if (MaxDamage > 0 && Damage > MaxDamage)
                {
                    return MaxDamage;
                }
                return Damage;
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
