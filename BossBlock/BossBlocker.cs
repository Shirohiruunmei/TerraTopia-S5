using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TerraTopia.BossBlock
{
    public class BossBlocker
    {
        private const int SweepEveryTicks = 30;
        private const int MessageCooldownSeconds = 30;

        private readonly BossBlockConfig _cfg;
        private readonly HashSet<int> _ids;
        private readonly Dictionary<int, DateTime> _lastMsg = new Dictionary<int, DateTime>();
        private int _tick;

        public BossBlocker(BossBlockConfig cfg)
        {
            _cfg = cfg;
            _ids = new HashSet<int>(cfg.BlockedNpcIds ?? new List<int>());
        }

        // Dipanggil saat game mencoba memunculkan NPC. Kalau ID-nya diblokir, spawn dibatalkan.
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
            Warn(e.NpcId);
        }

        // Cadangan: menghapus boss yang diblokir kalau masih aktif (misalnya dari item summon).
        public void OnGameUpdate(EventArgs args)
        {
            if (!_cfg.Enabled || _ids.Count == 0)
            {
                return;
            }

            _tick++;
            if (_tick < SweepEveryTicks)
            {
                return;
            }
            _tick = 0;

            try
            {
                for (int i = 0; i < Main.npc.Length; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc == null || !npc.active)
                    {
                        continue;
                    }

                    if (!_ids.Contains(npc.netID))
                    {
                        continue;
                    }

                    npc.active = false;
                    NetMessage.SendData(23, -1, -1, null, i);
                    Warn(npc.netID);
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] BossBlock gagal: " + ex.Message);
            }
        }

        private void Warn(int npcId)
        {
            DateTime now = DateTime.UtcNow;
            if (_lastMsg.TryGetValue(npcId, out DateTime last) && (now - last).TotalSeconds < MessageCooldownSeconds)
            {
                return;
            }

            _lastMsg[npcId] = now;
            TSPlayer.All.SendErrorMessage(_cfg.Message);
            TShock.Log.ConsoleInfo($"[BossBlock] Boss netID={npcId} diblokir.");
        }
    }
}
