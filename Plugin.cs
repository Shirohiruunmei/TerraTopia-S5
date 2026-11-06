using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;

namespace TerraTopia
{
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {
        public override string Name => "ShiroCoin Economy";
        public override string Author => "Shirohiruunmei";
        public override string Description => "Mata uang ShiroCoin + shop + leaderboard";
        public override Version Version => new Version(1, 0, 0);

        private static string _dbPath;
        private static List<ShopItem> _shopItems = new List<ShopItem>();
        private static readonly Random _rng = new Random();

        public Plugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInit);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInit);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
            }
            base.Dispose(disposing);
        }

        private void OnGameInit(EventArgs args)
        {
            _dbPath = Path.Combine(TShock.SavePath, "shirocoin.db");
            InitDatabase();

            string shopPath = Path.Combine(TShock.SavePath, "shop.json");
            if (File.Exists(shopPath))
            {
                try
                {
                    _shopItems = JsonConvert.DeserializeObject<List<ShopItem>>(File.ReadAllText(shopPath));
                }
                catch
                {
                    TShock.Log.ConsoleError("[ShiroCoin] shop.json rusak, pakai default.");
                    _shopItems = GetDefaultShop();
                }
            }
            else
            {
                File.WriteAllText(shopPath, JsonConvert.SerializeObject(GetDefaultShop(), Formatting.Indented));
                _shopItems = GetDefaultShop();
            }

            Commands.ChatCommands.Add(new Command("shirocoin.balance", BalanceCmd, "bal", "balance"));
            Commands.ChatCommands.Add(new Command("shirocoin.shop", ShopCmd, "shop"));
            Commands.ChatCommands.Add(new Command("shirocoin.leaderboard", LeaderboardCmd, "lb", "leaderboard"));
        }

        private static List<ShopItem> GetDefaultShop() => new List<ShopItem>
        {
            new ShopItem { name = "Ironskin Potion", itemId = 288, stack = 5, price = 10 },
            new ShopItem { name = "Regeneration Potion", itemId = 289, stack = 5, price = 10 },
            new ShopItem { name = "Swiftness Potion", itemId = 290, stack = 5, price = 10 },
            new ShopItem { name = "Gravitation Potion", itemId = 291, stack = 5, price = 25 },
            new ShopItem { name = "Minishark", itemId = 98, stack = 1, price = 100 },
            new ShopItem { name = "Musket Ball", itemId = 97, stack = 50, price = 20 },
            new ShopItem { name = "Shiny Red Balloon", itemId = 46, stack = 1, price = 150 },
            new ShopItem { name = "Hermes Boots", itemId = 53, stack = 1, price = 120 },
            new ShopItem { name = "Magic Missile", itemId = 127, stack = 1, price = 200 },
            new ShopItem { name = "Pharaoh's Mask", itemId = 672, stack = 1, price = 250 }
        };

        private static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        private void InitDatabase()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS ShiroCoin (PlayerName TEXT PRIMARY KEY, Balance INTEGER NOT NULL DEFAULT 0)";
            cmd.ExecuteNonQuery();
        }

        private static int GetBalance(string playerName)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Balance FROM ShiroCoin WHERE PlayerName = $name";
            cmd.Parameters.AddWithValue("$name", playerName);
            var result = cmd.ExecuteScalar();
            return result == null ? 0 : Convert.ToInt32(result);
        }

        private static void SetBalance(string playerName, int balance)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO ShiroCoin (PlayerName, Balance) VALUES ($name, $bal) " +
                               "ON CONFLICT(PlayerName) DO UPDATE SET Balance = $bal";
            cmd.Parameters.AddWithValue("$name", playerName);
            cmd.Parameters.AddWithValue("$bal", balance);
            cmd.ExecuteNonQuery();
        }

        // ---- Drop coin saat mob/bos mati ----
        private void OnNpcKilled(NpcKilledEventArgs args)
        {
            var npc = args.npc;
            if (npc == null) return;

            bool isBoss = npc.boss;
            int min = isBoss ? 50 : 1;
            int max = isBoss ? 150 : 5;
            int drop = _rng.Next(min, max + 1);

            var closest = TShock.Players
                .Where(p => p != null && p.Active && !p.Dead)
                .OrderBy(p => Math.Abs(p.TPlayer.Center.X - npc.Center.X) + Math.Abs(p.TPlayer.Center.Y - npc.Center.Y))
                .FirstOrDefault();

            if (closest == null) return;

            int newBalance = GetBalance(closest.Name) + drop;
            SetBalance(closest.Name, newBalance);

            string type = isBoss ? "boss" : "mob";
            closest.SendSuccessMessage($"[ShiroCoin] +{drop} SC dari membunuh {type} ({npc.FullName})! Saldo: {newBalance} SC");
        }

        // ---- /bal ----
        private void BalanceCmd(CommandArgs args)
        {
            var player = args.Player;
            int balance = GetBalance(player.Name);
            player.SendSuccessMessage($"[ShiroCoin] Saldo kamu: {balance} SC");
        }

        // ---- /shop ----
        private void ShopCmd(CommandArgs args)
        {
            var player = args.Player;
            if (args.Parameters.Count == 0)
            {
                player.SendInfoMessage("[ShiroCoin] Daftar item:");
                for (int i = 0; i < _shopItems.Count; i++)
                {
                    var item = _shopItems[i];
                    player.SendMessage($"{i + 1}. {item.name} x{item.stack} — {item.price} SC", Microsoft.Xna.Framework.Color.Yellow);
                }
                player.SendInfoMessage("Ketik /shop buy <nomor> untuk membeli.");
                return;
            }

            if (args.Parameters[0].ToLower() == "buy" && args.Parameters.Count >= 2)
            {
                if (!int.TryParse(args.Parameters[1], out int idx) || idx < 1 || idx > _shopItems.Count)
                {
                    player.SendErrorMessage("Nomor item tidak valid.");
                    return;
                }

                var item = _shopItems[idx - 1];
                int balance = GetBalance(player.Name);
                if (balance < item.price)
                {
                    player.SendErrorMessage($"[ShiroCoin] Saldo tidak cukup! Butuh {item.price} SC, kamu punya {balance} SC.");
                    return;
                }

                player.GiveItem(item.itemId, item.stack);
                SetBalance(player.Name, balance - item.price);
                player.SendSuccessMessage($"[ShiroCoin] Kamu membeli {item.name} x{item.stack} seharga {item.price} SC! Saldo tersisa: {balance - item.price} SC");
            }
        }

        // ---- /lb ----
        private void LeaderboardCmd(CommandArgs args)
        {
            var player = args.Player;
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PlayerName, Balance FROM ShiroCoin ORDER BY Balance DESC LIMIT 10";
            using var reader = cmd.ExecuteReader();

            var rows = new List<(string name, int bal)>();
            while (reader.Read())
                rows.Add((reader.GetString(0), reader.GetInt32(1)));

            if (rows.Count == 0)
            {
                player.SendInfoMessage("[ShiroCoin] Belum ada pemain dengan ShiroCoin.");
                return;
            }

            player.SendInfoMessage("[ShiroCoin] Leaderboard Top 10:");
            int rank = 1;
            foreach (var row in rows)
            {
                player.SendMessage($"{rank}. {row.name} — {row.bal} SC",
                    rank == 1 ? Microsoft.Xna.Framework.Color.Gold : Microsoft.Xna.Framework.Color.White);
                rank++;
            }
        }
    }

    public class ShopItem
    {
        public string name { get; set; }
        public int itemId { get; set; }
        public int stack { get; set; }
        public int price { get; set; }
    }
}
