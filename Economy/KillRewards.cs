using TerrariaApi.Server;
using TShockAPI;
using TerraTopia.Rank;

namespace TerraTopia.Economy
{
    public class KillRewards
    {
        private readonly EconomyConfig _config;
        private readonly EconomyService _economy;
        private readonly RankService _rank;
        private readonly RankConfig _rankCfg;

        public KillRewards(EconomyConfig config, EconomyService economy, RankService rank, RankConfig rankCfg)
        {
            _config = config;
            _economy = economy;
            _rank = rank;
            _rankCfg = rankCfg;
        }

        public void OnNpcKilled(NpcKilledEventArgs e)
        {
            var npc = e.npc;
            if (npc == null || npc.friendly || npc.townNPC)
            {
                return;
            }

            int index = npc.lastInteraction;
            if (index < 0 || index >= TShock.Players.Length)
            {
                return;
            }

            var player = TShock.Players[index];
            if (player == null || player.Account == null)
            {
                return;
            }

            long reward = npc.boss ? _config.BossReward : _config.MobReward;
            if (reward <= 0)
            {
                return;
            }

            string account = player.Account.Name;

            if (!npc.boss && _rankCfg.Enabled)
            {
                int level = _rank.Get(account);
                reward = RankService.ApplyMultiplier(reward, level, _rankCfg);
            }

            if (_economy.Adjust(account, reward, out _))
            {
                player.SendMessage($"+{reward} {_config.CurrencyName}", 255, 215, 0);
            }
        }
    }
}
