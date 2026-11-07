using System;
using TShockAPI;
using TShockAPI.Hooks;

namespace TerraTopia.Loot
{
    public class LootMonitor
    {
        public void Register()
        {
            PlayerHooks.ChestOpen += OnChestOpen;
            TShock.Log.ConsoleInfo("[TerraTopia] LootMonitor aktif (tahap 1: hanya mencatat).");
        }

        public void Unregister()
        {
            PlayerHooks.ChestOpen -= OnChestOpen;
        }

        private void OnChestOpen(ChestOpenEventArgs e)
        {
            try
            {
                string name = e.Player?.Name ?? "?";
                TShock.Log.ConsoleInfo($"[LootMonitor] {name} membuka chest di X={e.X} Y={e.Y}");
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] LootMonitor gagal: " + ex.Message);
            }
        }
    }
}
