using System;
using System.Collections.Generic;
using Terraria;
using TShockAPI;

namespace TerraTopia.Spawn
{
    public class SpawnGuard
    {
        private readonly SpawnGuardConfig _cfg;
        private readonly HashSet<string> _regions;
        private int _tick;

        public SpawnGuard(SpawnGuardConfig cfg)
        {
            _cfg = cfg;
            _regions = new HashSet<string>(cfg.RegionNames ?? new List<string>());
        }

        public void OnGameUpdate(EventArgs args)
        {
            if (!_cfg.Enabled || _regions.Count == 0)
            {
                return;
            }

            _tick++;
            if (_tick < _cfg.CheckEveryTicks)
            {
                return;
            }
            _tick = 0;

            try
            {
                for (int i = 0; i < Main.npc.Length; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc == null || !npc.active || npc.friendly || npc.townNPC)
                    {
                        continue;
                    }

                    int tileX = (int)(npc.position.X / 16f);
                    int tileY = (int)(npc.position.Y / 16f);

                    if (!InProtectedRegion(tileX, tileY))
                    {
                        continue;
                    }

                    npc.active = false;
                    NetMessage.SendData(23, -1, -1, null, i);
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] SpawnGuard gagal: " + ex.Message);
            }
        }

        private bool InProtectedRegion(int x, int y)
        {
            var names = TShock.Regions.InAreaRegionName(x, y);
            if (names == null)
            {
                return false;
            }

            foreach (var name in names)
            {
                if (_regions.Contains(name))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
