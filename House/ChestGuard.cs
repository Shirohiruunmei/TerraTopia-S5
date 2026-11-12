using System;
using System.Collections.Generic;
using System.Reflection;
using TShockAPI;

namespace TerraTopia.House
{
    public class ChestGuard
    {
        private readonly List<KeyValuePair<EventInfo, Delegate>> _subs = new List<KeyValuePair<EventInfo, Delegate>>();

        public void Register()
        {
            try
            {
                foreach (var t in typeof(TShock).Assembly.GetTypes())
                {
                    if (t.Namespace == null || !t.Namespace.StartsWith("TShockAPI"))
                    {
                        continue;
                    }

                    foreach (var ev in t.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        if (!ev.Name.Contains("ChestOpen"))
                        {
                            continue;
                        }

                        var adder = ev.GetAddMethod(true);
                        if (adder == null || !adder.IsStatic)
                        {
                            continue;
                        }

                        Delegate handler = MakeHandler(ev.EventHandlerType);
                        if (handler == null)
                        {
                            continue;
                        }

                        ev.AddEventHandler(null, handler);
                        _subs.Add(new KeyValuePair<EventInfo, Delegate>(ev, handler));
                        TShock.Log.ConsoleInfo($"[ChestGuard] Terhubung ke {t.FullName}.{ev.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] ChestGuard gagal mencari event: " + ex.Message);
            }

            if (_subs.Count == 0)
            {
                TShock.Log.ConsoleInfo("[ChestGuard] Event buka chest tidak ditemukan. Proteksi chest rumah tidak aktif.");
            }
        }

        private void OnChestOpen(object e)
        {
            try
            {
                if (e == null)
                {
                    return;
                }

                var player = GetValue(e, "Player") as TSPlayer;
                if (player == null || player.Account == null)
                {
                    return;
                }

                if (player.HasPermission("tshock.admin"))
                {
                    return;
                }

                object xo = GetValue(e, "X");
                object yo = GetValue(e, "Y");
                if (xo == null || yo == null)
                {
                    TShock.Log.ConsoleInfo("[ChestGuard] Field X/Y tidak ditemukan di event buka chest.");
                    return;
                }

                int x = Convert.ToInt32(xo);
                int y = Convert.ToInt32(yo);

                var names = TShock.Regions.InAreaRegionName(x, y);
                if (names == null)
                {
                    return;
                }

                foreach (var name in names)
                {
                    if (!name.StartsWith("house_"))
                    {
                        continue;
                    }

                    var region = TShock.Regions.GetRegionByName(name);
                    if (region == null)
                    {
                        continue;
                    }

                    if (string.Equals(region.Owner, player.Account.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    SetHandled(e);
                    player.SendErrorMessage("Chest ini ada di rumah pemain lain. Hanya pemilik rumah dan admin yang bisa membukanya.");
                    TShock.Log.ConsoleInfo($"[ChestGuard] {player.Name} mencoba membuka chest di rumah {region.Owner} ({x}, {y}).");
                    return;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] ChestGuard gagal: " + ex.Message);
            }
        }

        private Delegate MakeHandler(Type delegateType)
        {
            var invoke = delegateType.GetMethod("Invoke");
            if (invoke == null)
            {
                return null;
            }

            try
            {
                int count = invoke.GetParameters().Length;
                if (count == 2)
                {
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnChestOpen2), BindingFlags.NonPublic | BindingFlags.Instance));
                }
                if (count == 1)
                {
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnChestOpen1), BindingFlags.NonPublic | BindingFlags.Instance));
                }
            }
            catch { }
            return null;
        }

        private void OnChestOpen2(object sender, object e) => OnChestOpen(e);

        private void OnChestOpen1(object e) => OnChestOpen(e);

        private static object GetValue(object obj, string prop)
        {
            return obj.GetType()
                .GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?.GetValue(obj);
        }

        private static void SetHandled(object obj)
        {
            obj.GetType()
                .GetProperty("Handled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?.SetValue(obj, true);
        }
    }
}
