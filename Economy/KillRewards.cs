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
        private readonly BossLimitService _bossLimit;

        public KillRewards(EconomyConfig config, EconomyService economy, RankService rank, RankConfig rankCfg)
        {
            _config = config;
            _economy = economy;
            _rank = rank;
            _rankCfg = rankCfg;
            _bossLimit = new BossLimitService(TShock.DB);
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

            long reward;
            bool isSpecial = _config.SpecialRewards != null
                && _config.SpecialRewards.TryGetValue(npc.netID.ToString(), out long special);

            if (isSpecial)
            {
                reward = special;
            }
            else
            {
                reward = npc.boss ? _config.BossReward : _config.MobReward;
            }

            if (reward <= 0)
            {
                return;
            }

            string account = player.Account.Name;

            if (npc.boss)
            {
                int level = _rank.Get(account);
                int limit = _rankCfg.GetDailyBossLimit(level);

                if (!_bossLimit.TryUse(account, limit, out int used))
                {
                    player.SendErrorMessage($"Batas hadiah boss harian kamu sudah habis ({limit}/{limit}). Coba lagi besok.");
                    return;
                }

                player.SendInfoMessage($"Hadiah boss hari ini: {used}/{limit}.");
            }
            else if (_rankCfg.Enabled)
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
