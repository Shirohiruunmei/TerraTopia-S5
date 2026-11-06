using TerrariaApi.Server;
using TShockAPI;

namespace TerraTopia.Economy
{
    public class KillRewards
    {
        private readonly EconomyConfig _config;
        private readonly EconomyService _economy;

        public KillRewards(EconomyConfig config, EconomyService economy)
        {
            _config = config;
            _economy = economy;
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
            if (_economy.Adjust(account, reward, out _))
            {
                player.SendMessage($"+{reward} {_config.CurrencyName}", 255, 215, 0);
            }
        }
    }
}
