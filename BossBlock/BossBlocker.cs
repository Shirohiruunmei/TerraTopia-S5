using System.Collections.Generic;
using TerrariaApi.Server;
using TShockAPI;

namespace TerraTopia.BossBlock
{
    public class BossBlocker
    {
        private readonly BossBlockConfig _cfg;
        private readonly HashSet<int> _ids;

        public BossBlocker(BossBlockConfig cfg)
        {
            _cfg = cfg;
            _ids = new HashSet<int>(cfg.BlockedNpcIds ?? new List<int>());
        }

        public void OnNpcSpawn(NpcSpawnEventArgs e)
        {
            if (!_cfg.Enabled || e.Handled)
            {
                return;
            }

            if (!_ids.Contains(e.NpcId))
            {
                return;
            }

            e.Handled = true;
            TSPlayer.All.SendErrorMessage(_cfg.Message);
        }
    }
}
