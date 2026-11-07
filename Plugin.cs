using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TerraTopia.Chat;
using TerraTopia.Economy;
using TerraTopia.Home;
using TerraTopia.Lps;
using TerraTopia.Rank;
using TerraTopia.Shop;
using TerraTopia.Spawn;

namespace TerraTopia
{
    [ApiVersion(2, 1)]
    public class TerraTopiaPlugin : TerrariaPlugin
    {
        public override string Name => "TerraTopia";
        public override string Author => "TerraTopia";
        public override string Description => "TerraTopia SMP S5 - Economy, Shop, Rank, Chat, Player Shop, Spawn Guard & Home";
        public override Version Version => new Version(0, 9, 0);

        private EconomyConfig _config;
        private EconomyService _economy;
        private KillRewards _kills;
        private ChatPrefix _chat;
        private SpawnGuard _spawnGuard;

        public TerraTopiaPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilledProxy);
            ServerApi.Hooks.ServerChat.Register(this, OnChatProxy);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdateProxy);
        }

        private void OnGameInitialize(EventArgs args)
        {
            try
            {
                _config = EconomyConfig.Load();
                _economy = new EconomyService(TShock.DB);
                _economy.Init();

                var rankCfg = RankConfig.Load();
                var rank = new RankService(TShock.DB);
                rank.Init();

                _kills = new KillRewards(_config, _economy, rank, rankCfg);
                _chat = new ChatPrefix(ChatConfig.Load(), rank, rankCfg);
                _spawnGuard = new SpawnGuard(SpawnGuardConfig.Load());

                var cmds = new EconomyCommands(_economy, _config);
                TShockAPI.Commands.ChatCommands.Add(new Command(cmds.Bal, "bal"));
                TShockAPI.Commands.ChatCommands.Add(new Command(cmds.Pay, "pay"));
                TShockAPI.Commands.ChatCommands.Add(new Command(cmds.Baltop, "baltop"));
                TShockAPI.Commands.ChatCommands.Add(new Command("terratopia.admin", cmds.GiveBal, "givebal"));
                TShockAPI.Commands.ChatCommands.Add(new Command("terratopia.admin", cmds.TakeBal, "takebal"));
                TShockAPI.Commands.ChatCommands.Add(new Command("terratopia.admin", cmds.SetBal, "setbal"));

                var shopCmds = new ShopCommands(_economy, _config, ShopConfig.Load());
                TShockAPI.Commands.ChatCommands.Add(new Command(shopCmds.Sb, "sb"));
                TShockAPI.Commands.ChatCommands.Add(new Command(shopCmds.Sb, "shop"));

                var rankCmds = new RankCommands(rank, rankCfg, _economy, _config);
                TShockAPI.Commands.ChatCommands.Add(new Command(rankCmds.ShowRank, "rank"));
                TShockAPI.Commands.ChatCommands.Add(new Command(rankCmds.DoRankUp, "rankup"));

                var lps = new LpsService(TShock.DB);
                lps.Init();
                var lpsCmds = new LpsCommands(lps, _economy, _config);
                TShockAPI.Commands.ChatCommands.Add(new Command(lpsCmds.Lps, "lps"));
                TShockAPI.Commands.ChatCommands.Add(new Command(lpsCmds.Lpss, "lpss"));
                TShockAPI.Commands.ChatCommands.Add(new Command(lpsCmds.Lpsb, "lpsb"));
                TShockAPI.Commands.ChatCommands.Add(new Command(lpsCmds.Lpsc, "lpsc"));
                TShockAPI.Commands.ChatCommands.Add(new Command(lpsCmds.Lpsk, "lpsk"));

                var home = new HomeService(TShock.DB);
                home.Init();
                var homeCmds = new HomeCommands(home);
                TShockAPI.Commands.ChatCommands.Add(new Command(homeCmds.SetHome, "sethome"));

                TShockAPI.Commands.ChatCommands.Add(new Command(TtHelp, "tthelp"));

                TShock.Log.ConsoleInfo("[TerraTopia] Semua modul aktif.");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("[TerraTopia] Gagal memulai: " + ex.Message);
            }
        }

        private void TtHelp(CommandArgs args)
        {
            var p = args.Player;
            p.SendInfoMessage("=== TerraTopia Command ===");
            p.SendInfoMessage("Uang: /bal, /pay <nama> <jumlah>, /baltop");
            p.SendInfoMessage("Rank: /rank, /rankup");
            p.SendInfoMessage("Shop resmi (di area Shop): /shop, /shop <nomor> <jumlah>");
            p.SendInfoMessage("Player shop: /lps, /lpss <harga>, /lpsb <nomor>, /lpsc <nomor>, /lpsk");
            p.SendInfoMessage("Rumah: /sethome (berdiri di region rumahmu sendiri)");
            p.SendInfoMessage("Batas: 3 listing player shop aktif per pemain.");
        }

        private void OnNpcKilledProxy(NpcKilledEventArgs e)
        {
            _kills?.OnNpcKilled(e);
        }

        private void OnChatProxy(ServerChatEventArgs e)
        {
            _chat?.OnChat(e);
        }

        private void OnGameUpdateProxy(EventArgs e)
        {
            _spawnGuard?.OnGameUpdate(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilledProxy);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChatProxy);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdateProxy);
            }
            base.Dispose(disposing);
        }
    }
}
