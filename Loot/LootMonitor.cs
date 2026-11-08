using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TShockAPI;

namespace TerraTopia.Loot
{
    public class LootMonitor
    {
        private bool _dumped;
        private readonly List<KeyValuePair<EventInfo, Delegate>> _subs = new List<KeyValuePair<EventInfo, Delegate>>();

        public void Register()
        {
            try
            {
                var types = typeof(TShock).Assembly.GetTypes();
                foreach (var t in types)
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
                            TShock.Log.ConsoleInfo($"[LootMonitor] Lewati {t.FullName}.{ev.Name} (bukan static).");
                            continue;
                        }

                        Delegate handler = MakeHandler(ev.EventHandlerType);
                        if (handler == null)
                        {
                            TShock.Log.ConsoleInfo($"[LootMonitor] Lewati {t.FullName}.{ev.Name} (signature tidak cocok).");
                            continue;
                        }

                        ev.AddEventHandler(null, handler);
                        _subs.Add(new KeyValuePair<EventInfo, Delegate>(ev, handler));
                        TShock.Log.ConsoleInfo($"[LootMonitor] Terhubung ke {t.FullName}.{ev.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] LootMonitor gagal mencari event: " + ex.Message);
            }

            if (_subs.Count == 0)
            {
                TShock.Log.ConsoleInfo("[LootMonitor] Tidak menemukan event ChestOpen yang static.");
            }
        }

        public void Unregister()
        {
            foreach (var s in _subs)
            {
                try
                {
                    s.Key.RemoveEventHandler(null, s.Value);
                }
                catch
                {
                }
            }
            _subs.Clear();
        }

        private Delegate MakeHandler(Type delegateType)
        {
            var invoke = delegateType.GetMethod("Invoke");
            if (invoke == null)
            {
                return null;
            }

            int count = invoke.GetParameters().Length;
            try
            {
                if (count == 2)
                {
                    var mi = typeof(LootMonitor).GetMethod(nameof(OnEvent2), BindingFlags.NonPublic | BindingFlags.Instance);
                    return Delegate.CreateDelegate(delegateType, this, mi);
                }
                if (count == 1)
                {
                    var mi = typeof(LootMonitor).GetMethod(nameof(OnEvent1), BindingFlags.NonPublic | BindingFlags.Instance);
                    return Delegate.CreateDelegate(delegateType, this, mi);
                }
            }
            catch
            {
            }
            return null;
        }

        private void OnEvent2(object sender, object e) => Handle(e);

        private void OnEvent1(object e) => Handle(e);

        private void Handle(object e)
        {
            try
            {
                if (e == null)
                {
                    return;
                }

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
