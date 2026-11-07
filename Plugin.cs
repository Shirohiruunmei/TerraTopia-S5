using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TerraTopia.Chat;
using TerraTopia.Economy;
using TerraTopia.Help;
using TerraTopia.Home;
using TerraTopia.Loot;
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
        public override string Description => "TerraTopia SMP S5 - Economy, Shop, Rank, Chat, Player Shop, Spawn Guard, Home & Loot";
        public override Version Version => new Version(0, 9, 0);

        private EconomyConfig _config;
        private EconomyService _economy;
        private KillRewards _kills;
        private ChatPrefix _chat;
        private SpawnGuard _spawnGuard;
        private LootMonitor _loot;

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
