using System;
using TShockAPI;

namespace TerraTopia.Teleport
{
    // Memeriksa setiap frame. Saat pemain berubah dari hidup ke mati, lokasinya dicatat.
    public class DeathWatcher
    {
        private readonly DeathTeleport _tp;
        private readonly bool[] _wasDead = new bool[256];

        public DeathWatcher(DeathTeleport tp)
        {
            _tp = tp;
        }

        public void OnGameUpdate(EventArgs args)
        {
            try
            {
                foreach (var p in TShock.Players)
                {
                    if (p == null || !p.Active || p.Account == null)
                    {
                        continue;
                    }

                    int idx = p.Index;
                    bool dead = p.TPlayer.dead;

                    if (dead && !_wasDead[idx])
                    {
                        _tp.SaveDeath(p.Account.Name, p.TileX, p.TileY);
                    }

                    _wasDead[idx] = dead;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] DeathWatcher gagal: " + ex.Message);
            }
        }
    }
}
