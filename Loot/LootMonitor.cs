using System;
using System.Linq;
using System.Reflection;
using TShockAPI;

namespace TerraTopia.Loot
{
    public class LootMonitor
    {
        private bool _dumped;

        public void Register()
        {
            TShockAPI.Hooks.PlayerHooks.ChestOpen += e => OnChestOpen(e);
            TShock.Log.ConsoleInfo("[TerraTopia] LootMonitor aktif (tahap 1: hanya mencatat).");
        }

        public void Unregister()
        {
            // Handler berupa lambda tidak bisa dilepas secara terpisah.
            // Hook ikut berhenti saat server mati, jadi ini tidak masalah.
        }

        private void OnChestOpen(object e)
        {
            try
            {
                if (!_dumped)
                {
                    _dumped = true;
                    var names = e.GetType().GetProperties().Select(p => p.Name);
                    TShock.Log.ConsoleInfo("[LootMonitor] Field event: " + string.Join(", ", names));
                }

                object player = GetValue(e, "Player");
                string name = (player as TSPlayer)?.Name ?? "?";
                object x = GetValue(e, "X");
                object y = GetValue(e, "Y");

                TShock.Log.ConsoleInfo($"[LootMonitor] {name} membuka chest di X={x} Y={y}");
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] LootMonitor gagal: " + ex.Message);
            }
        }

        private static object GetValue(object obj, string prop)
        {
            return obj.GetType()
                .GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?.GetValue(obj);
        }
    }
}
