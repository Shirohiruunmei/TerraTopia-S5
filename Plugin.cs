using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TerraTopia.Economy;
using TerraTopia.Shop;

namespace TerraTopia
{
    [ApiVersion(2, 1)]
    public class TerraTopiaPlugin : TerrariaPlugin
    {
        public override string Name => "TerraTopia";
        public override string Author => "TerraTopia";
        public override string Description => "TerraTopia SMP S5 - Economy & Shop";
        public override Version Version => new Version(0, 3, 0);

        private EconomyConfig _config;
        private EconomyService _economy;
        private KillRewards _kills;

        public TerraTopiaPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilledProxy);
        }

        private void OnGameInitialize(EventArgs args)
        {
            try
            {
                _config = EconomyConfig.Load();
                _economy = new EconomyService(TShock.DB);
                _economy.Init();

                _kills = new KillRewards(_config, _economy);

                var cmds = new EconomyCommands(_economy, _config);
                TShockAPI.Commands.ChatCommands.Add(new Command(cmds.Bal, "bal"));
                TShockAPI.Commands.ChatCommands.Add(new Command(cmds.Pay, "pay"));
                TShockAPI.Commands.ChatCommands.Add(new Command(cmds.Baltop, "baltop"));
                TShockAPI.Commands.ChatCommands.Add(new Command("terratopia.admin", cmds.GiveBal, "givebal"));
                TShockAPI.Commands.ChatCommands.Add(new Command("terratopia.admin", cmds.TakeBal, "takebal"));
                TShockAPI.Commands.ChatCommands.Add(new Command("terratopia.admin", cmds.SetBal, "setbal"));

                var shopCmds = new ShopCommands(_economy, _config, ShopConfig.Load());
                TShockAPI.Commands.ChatCommands.Add(new Command(shopCmds.Sb, "sb"));

                TShock.Log.ConsoleInfo("[TerraTopia] Economy dan Shop aktif.");
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilledProxy);
            }
            base.Dispose(disposing);
        }
    }
}
