using System;
using Terraria;
using TShockAPI;

namespace TerraTopia.Boss
{
    // Mengganti projectile Queen Bee menjadi projectile Zenith dengan damage Zenith.
    public class QueenBeeMonitor
    {
        private readonly QueenBeeConfig _cfg;
        private int _tick;

        public QueenBeeMonitor()
        {
            _cfg = QueenBeeConfig.Load();
        }

        public void OnGameUpdate(EventArgs args)
        {
            if (!_cfg.Enabled)
            {
                return;
            }

            _tick++;
            if (_tick < 2)
            {
                return;
            }
            _tick = 0;

            try
            {
                for (int i = 0; i < Main.projectile.Length; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || !p.hostile || p.type != _cfg.FromProjectileId)
                    {
                        continue;
                    }

                    int owner = p.owner;
                    if (owner < 0 || owner >= Main.npc.Length)
                    {
                        continue;
                    }

                    NPC npc = Main.npc[owner];
                    if (npc == null || !npc.active || npc.netID != _cfg.BossNpcId)
                    {
                        continue;
                    }

                    var pos = p.position;
                    var vel = p.velocity;
                    int timeLeft = p.timeLeft;

                    p.SetDefaults(_cfg.ToProjectileId);
                    p.position = pos;
                    p.velocity = vel;
                    p.timeLeft = timeLeft;
                    p.hostile = true;
                    p.friendly = false;
                    p.owner = owner;
                    p.damage = _cfg.Damage;
                    p.netUpdate = true;

                    NetMessage.SendData(27, -1, -1, null, i);
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] QueenBeeMonitor gagal: " + ex.Message);
            }
        }
    }
}
