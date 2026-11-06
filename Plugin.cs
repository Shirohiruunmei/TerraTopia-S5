using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;
using Terraria;
using Terraria.ID;
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
        private static readonly Dictionary<string, string> _lastDamageBy = new Dictionary<string, string>();

        public Plugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInit);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInit);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            }
            base.Dispose(disposing);
        }

        private void OnGameInit(EventArgs args)
        {
            _dbPath = Path.Combine(TShock.SavePath, "shirocoin.db");
            CreateDatabase();

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

        private void CreateDatabase()
        {
            var db = new SqliteDB(_dbPath);
            db.Query("CREATE TABLE IF NOT EXISTS ShiroCoin (PlayerName TEXT PRIMARY KEY, Balance INT NOT NULL DEFAULT 0)");
        }

        private static int GetBalance(string playerName)
        {
            using (var db = new SqliteDB(_dbPath))
            {
                var result = db.Query("SELECT Balance FROM ShiroCoin WHERE PlayerName = @0", playerName);
                if (result.Count == 0) return 0;
                return Convert.ToInt32(result[0][0]);
            }
        }

        private static void SetBalance(string playerName, int balance)
        {
            using (var db = new SqliteDB(_dbPath))
            {
                db.Query("INSERT OR REPLACE INTO ShiroCoin (PlayerName, Balance) VALUES (@0, @1)", playerName, balance);
            }
        }

        // ---- Drop coin saat mob/bos mati ----
        private void OnNpcKilled(NpcKilledEventArgs args)
        {
            var npc = args.npc;
            bool isBoss = npc.boss || npc.type == NPCID.KingSlime || npc.type == NPCID.EaterofWorldsHead;

            int min = isBoss ? 50 : 1;
            int max = isBoss ? 150 : 5;
            int drop = new Random().Next(min, max + 1);

            // Cari pemain yang paling akhir menyerang NPC ini
            string killerName = null;
            if (_lastDamageBy.TryGetValue(npc.FullName, out var last))
                killerName = last;

            // Fallback: cari pemain terdekat
            if (killerName == null)
            {
                var closest = Main.player
                    .Where(p => p.active && p.statLife > 0)
                    .OrderBy(p => Math.Abs(p.Center.X - npc.Center.X) + Math.Abs(p.Center.Y - npc.Center.Y))
                    .FirstOrDefault();
                if (closest != null) killerName = closest.name;
            }

            if (killerName == null) return;

            // Jangan kasih coin ke bot/NPC player
            var tsPlayer = TShock.Players.FirstOrDefault(p => p?.Name == killerName);
            if (tsPlayer == null) return;

            int newBalance = GetBalance(killerName) + drop;
            SetBalance(killerName, newBalance);

            string type = isBoss ? "boss" : "mob";
            tsPlayer.SendSuccessMessage($"[ShiroCoin] +{drop} SC dari membunuh {type} ({npc.FullName})! Saldo: {newBalance} SC");
        }

        // ---- Track siapa yang menyerang NPC ----
        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.PlayerDamage)
            {
                using (var reader = new BinaryReader(new MemoryStream(args.Data)))
                {
                    var player = Main.player[args.Msg.whoAmI];
                    reader.ReadByte(); // flags
                    int npcIndex = reader.ReadInt16();
                    if (npcIndex >= 0 && npcIndex < Main.npc.Length && Main.npc[npcIndex] != null)
                    {
                        _lastDamageBy[Main.npc[npcIndex].FullName] = player.name;
                    }
                }
            }
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

                int slot = player.TPlayer.FindEmptyInventorySlot(true);
                if (slot < 0)
                {
                    player.SendErrorMessage("Inventory penuh!");
                    return;
                }

                player.TPlayer.inventory[slot] = new Item { netID = item.itemId, stack = item.stack };
                player.SendData(PacketTypes.PlayerSlot, "", player.Index, slot);
                SetBalance(player.Name, balance - item.price);
                player.SendSuccessMessage($"[ShiroCoin] Kamu membeli {item.name} x{item.stack} seharga {item.price} SC! Saldo tersisa: {balance - item.price} SC");
            }
        }

        // ---- /lb ----
        private void LeaderboardCmd(CommandArgs args)
        {
            var player = args.Player;
            using (var db = new SqliteDB(_dbPath))
            {
                var result = db.Query("SELECT PlayerName, Balance FROM ShiroCoin ORDER BY Balance DESC LIMIT 10");
                if (result.Count == 0)
                {
                    player.SendInfoMessage("[ShiroCoin] Belum ada pemain dengan ShiroCoin.");
                    return;
                }

                player.SendInfoMessage("[ShiroCoin] Leaderboard Top 10:");
                int rank = 1;
                foreach (var row in result)
                {
                    player.SendMessage($"{rank}. {row[0]} — {row[1]} SC", rank == 1 ? Microsoft.Xna.Framework.Color.Gold : Microsoft.Xna.Framework.Color.White);
                    rank++;
                }
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
