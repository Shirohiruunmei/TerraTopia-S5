using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TerraTopia.Chat;
using TerraTopia.Economy;
using TerraTopia.Lps;
using TerraTopia.Rank;
using TerraTopia.Shop;

namespace TerraTopia
{
    [ApiVersion(2, 1)]
    public class TerraTopiaPlugin : TerrariaPlugin
    {
        public override string Name => "TerraTopia";
        public override string Author => "TerraTopia";
        public override string Description => "TerraTopia SMP S5 - Economy, Shop, Rank, Chat & Player Shop";
        public override Version Version => new Version(0, 6, 0);

        private EconomyConfig _config;
        private EconomyService _economy;
        private KillRewards _kills;
        private ChatPrefix _chat;

        public TerraTopiaPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilledProxy);
            ServerApi.Hooks.ServerChat.Register(this, OnChatProxy);
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

                TShock.Log.ConsoleInfo("[TerraTopia] Economy, Shop, Rank, Chat, dan Player Shop aktif.");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("[TerraTopia] Gagal memulai: " + ex.Message);
            }
        }

        private void OnNpcKilledProxy(NpcKilledEventArgs e)
        {
            _kills?.OnNpcKilled(e);
        }

        private void OnChatProxy(ServerChatEventArgs e)
        {
            _chat?.OnChat(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilledProxy);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChatProxy);
            }
            base.Dispose(disposing);
        }
    }
}
