using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TShockAPI;

namespace TerraTopia.Rank
{
    public class RankTier
    {
        public int MinLevel { get; set; }
        public string Name { get; set; }
    }

    public class RankConfig
    {
        public bool Enabled { get; set; } = true;
        public int MaxLevel { get; set; } = 30;
        public long BaseCost { get; set; } = 100;
        public int PercentPerLevel { get; set; } = 10;

        public List<RankTier> Tiers { get; set; } = new List<RankTier>
        {
            new RankTier { MinLevel = 0, Name = "Newbie" },
            new RankTier { MinLevel = 5, Name = "Beginner" },
            new RankTier { MinLevel = 10, Name = "Apprentice" },
            new RankTier { MinLevel = 20, Name = "Veteran" },
            new RankTier { MinLevel = 30, Name = "Legend" }
        };

        public string GetRankName(int level)
        {
            var tier = Tiers
                .OrderByDescending(t => t.MinLevel)
                .FirstOrDefault(t => t.MinLevel <= level);
            return tier?.Name ?? "Newbie";
        }

        public long GetRankupCost(int currentLevel)
        {
            return BaseCost * (currentLevel + 1);
        }

        public static RankConfig Load()
        {
            string dir = Path.Combine(TShock.SavePath, "TerraTopia");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "rank.json");

            if (!File.Exists(path))
            {
                var def = new RankConfig();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(def, opts));
                return def;
            }

            return JsonSerializer.Deserialize<RankConfig>(File.ReadAllText(path)) ?? new RankConfig();
        }
    }
}
