using System;
using System.Collections.Generic;
using Terraria;
using TShockAPI;

namespace TerraTopia.Boss
{
    // Tahap 1: hanya mencatat projectile hostile yang muncul. Tidak mengubah apa pun.
    public class QueenBeeMonitor
    {
        private const int CheckEveryTicks = 30;
        private int _tick;
        private readonly HashSet<string> _seen = new HashSet<string>();

        public void OnGameUpdate(EventArgs args)
        {
            _tick++;
            if (_tick < CheckEveryTicks)
            {
                return;
            }
            _tick = 0;

            try
            {
                for (int i = 0; i < Main.projectile.Length; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || !p.hostile)
                    {
                        continue;
                    }

                    string key = p.type + ":" + p.damage;
                    if (_seen.Contains(key))
                    {
                        continue;
                    }
                    _seen.Add(key);

                    TShock.Log.ConsoleInfo(
                        $"[QBMonitor] type={p.type} damage={p.damage} owner={p.owner} aiStyle={p.aiStyle} " +
                        $"x={p.position.X / 16f:0} y={p.position.Y / 16f:0}");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] QBMonitor gagal: " + ex.Message);
            }
        }
    }
}
