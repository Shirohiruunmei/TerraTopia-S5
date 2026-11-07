using System;
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
        public int DailyBossLimit { get; set; } = 3;
    }

    public class RankConfig
    {
        public bool Enabled { get; set; } = true;
        public int MaxLevel { get; set; } = 30;
        public long BaseCost { get; set; } = 100;
        public int GrowthPercent { get; set; } = 200;
        public int PercentPerLevel { get; set; } = 10;

        public List<RankTier> Tiers { get; set; } = new List<RankTier>
        {
            new RankTier { MinLevel = 0, Name = "Newbie", DailyBossLimit = 3 },
            new RankTier { MinLevel = 5, Name = "Beginner", DailyBossLimit = 5 },
            new RankTier { MinLevel = 10, Name = "Apprentice", DailyBossLimit = 8 },
            new RankTier { MinLevel = 20, Name = "Veteran", DailyBossLimit = 12 },
            new RankTier { MinLevel = 30, Name = "Legend", DailyBossLimit = 20 }
        };

        public RankTier GetTier(int level)
        {
            return Tiers
                .OrderByDescending(t => t.MinLevel)
                .FirstOrDefault(t => t.MinLevel <= level);
        }

        public string GetRankName(int level)
        {
            return GetTier(level)?.Name ?? "Newbie";
        }

        public int GetDailyBossLimit(int level)
        {
            return GetTier(level)?.DailyBossLimit ?? 3;
        }

        public long GetRankupCost(int currentLevel)
        {
            double multiplier = Math.Pow(1.0 + GrowthPercent / 100.0, currentLevel);
            double cost = BaseCost * multiplier;

            const double Cap = 1000000000000000.0;
            if (cost > Cap)
            {
                return (long)Cap;
            }

            return (long)Math.Round(cost);
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
