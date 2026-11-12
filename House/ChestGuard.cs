using System;
using System.Collections.Generic;
using System.Reflection;
using TShockAPI;

namespace TerraTopia.House
{
    public class ChestGuard
    {
        private bool _loggedArgs;
        private readonly List<Action> _unhooks = new List<Action>();

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

                    // Cari sebagai event.
                    foreach (var ev in t.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        if (!ev.Name.Contains("ChestOpen") || !ev.GetAddMethod(true).IsStatic)
                        {
                            continue;
                        }

                        Delegate handler = MakeHandler(ev.EventHandlerType);
                        if (handler == null)
                        {
                            continue;
                        }

                        ev.AddEventHandler(null, handler);
                        _unhooks.Add(() => { try { ev.RemoveEventHandler(null, handler); } catch { } });
                        TShock.Log.ConsoleInfo($"[ChestGuard] Terhubung (event) ke {t.FullName}.{ev.Name}");
                    }

                    // Cari sebagai field delegate statis.
                    foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (!f.Name.Contains("ChestOpen") || !typeof(Delegate).IsAssignableFrom(f.FieldType))
                        {
                            continue;
                        }

                        Delegate handler = MakeHandler(f.FieldType);
                        if (handler == null)
                        {
                            continue;
                        }

                        var current = f.GetValue(null) as Delegate;
                        f.SetValue(null, Delegate.Combine(current, handler));
                        _unhooks.Add(() =>
                        {
                            try
                            {
                                var now = f.GetValue(null) as Delegate;
                                f.SetValue(null, Delegate.Remove(now, handler));
                            }
                            catch { }
                        });
                        TShock.Log.ConsoleInfo($"[ChestGuard] Terhubung (field) ke {t.FullName}.{f.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] ChestGuard gagal mencari event: " + ex.Message);
            }

            if (_unhooks.Count == 0)
            {
                TShock.Log.ConsoleInfo("[ChestGuard] Tidak ada event atau field ChestOpen yang cocok. Proteksi chest rumah tidak aktif.");
            }
        }

        public void Unregister()
        {
            foreach (var u in _unhooks)
            {
                u();
            }
            _unhooks.Clear();
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
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnChest2), BindingFlags.NonPublic | BindingFlags.Instance));
                }
                if (count == 1)
                {
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnChest1), BindingFlags.NonPublic | BindingFlags.Instance));
                }
            }
            catch { }
            return null;
        }

        private void OnChest2(object sender, object e) => OnChest(e);

        private void OnChest1(object e) => OnChest(e);

        private void OnChest(object e)
        {
            try
            {
                if (e == null)
                {
                    return;
                }

                if (!_loggedArgs)
                {
                    _loggedArgs = true;
                    var names = new List<string>();
                    foreach (var prop in e.GetType().GetProperties())
                    {
                        names.Add(prop.Name + ":" + prop.PropertyType.Name);
                    }
                    TShock.Log.ConsoleInfo("[ChestGuard] Field event buka chest: " + string.Join(", ", names));
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
                    TShock.Log.ConsoleInfo("[ChestGuard] Field X/Y tidak ditemukan. Kirim baris 'Field event buka chest' dari log.");
                    return;
                }

                int x = Convert.ToInt32(xo);
                int y = Convert.ToInt32(yo);

                var names2 = TShock.Regions.InAreaRegionName(x, y);
                if (names2 == null)
                {
                    return;
                }

                foreach (var name in names2)
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
}p
