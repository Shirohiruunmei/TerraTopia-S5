using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using TShockAPI;

namespace TerraTopia.Weapon
{
    public class LuminiteFix
    {
        private const int ReloadCheckTicks = 120;

        private LuminiteConfig _cfg;
        private DateTime _lastWrite;
        private int _tick;
        private bool _warnedClamp;
        private bool _loggedArgs;
        private readonly List<KeyValuePair<EventInfo, Delegate>> _subs = new List<KeyValuePair<EventInfo, Delegate>>();

        public LuminiteFix()
        {
            _cfg = LuminiteConfig.Load();
            _lastWrite = File.GetLastWriteTimeUtc(LuminiteConfig.PathFile);
            LogActive();
            RegisterCreationLog();
        }

        public void OnGameUpdate(EventArgs args)
        {
            try
            {
                _tick++;
                if (_tick >= ReloadCheckTicks)
                {
                    _tick = 0;
                    ReloadIfChanged();
                }

                if (!_cfg.Enabled)
                {
                    return;
                }

                int damage = _cfg.EffectiveDamage;

                for (int i = 0; i < Main.projectile.Length; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || !p.friendly || p.type != _cfg.ProjectileId)
                    {
                        continue;
                    }

                    p.damage = damage;

                    if (_cfg.DisableCrit)
                    {
                        DisableCrit(p);
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] LuminiteFix gagal: " + ex.Message);
            }
        }

        // Diagnostik: mencatat isi event projectile baru. Belum mengubah damage.
        private void RegisterCreationLog()
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
                        if (!ev.Name.Contains("ProjectileNew"))
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
                        TShock.Log.ConsoleInfo($"[LuminiteFix] Terhubung ke {t.FullName}.{ev.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] LuminiteFix gagal mencari event projectile: " + ex.Message);
            }

            if (_subs.Count == 0)
            {
                TShock.Log.ConsoleInfo("[LuminiteFix] Event ProjectileNew tidak ditemukan.");
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
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnNew2), BindingFlags.NonPublic | BindingFlags.Instance));
                }
                if (count == 1)
                {
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnNew1), BindingFlags.NonPublic | BindingFlags.Instance));
                }
            }
            catch { }
            return null;
        }

        private void OnNew2(object sender, object e) => OnNew(e);

        private void OnNew1(object e) => OnNew(e);

        private void OnNew(object e)
        {
            if (e == null || _loggedArgs)
            {
                return;
            }

            _loggedArgs = true;
            var names = new List<string>();
            foreach (var prop in e.GetType().GetProperties())
            {
                names.Add(prop.Name + ":" + prop.PropertyType.Name);
            }
            TShock.Log.ConsoleInfo("[LuminiteFix] Field event ProjectileNew: " + string.Join(", ", names));
        }

        private void ReloadIfChanged()
        {
            DateTime now = File.GetLastWriteTimeUtc(LuminiteConfig.PathFile);
            if (now == _lastWrite)
            {
                return;
            }

            _lastWrite = now;
            _cfg = LuminiteConfig.Load();
            _warnedClamp = false;
            LogActive();
        }

        private void LogActive()
        {
            TShock.Log.ConsoleInfo($"[LuminiteFix] Aktif: projectileId={_cfg.ProjectileId}, damage={_cfg.EffectiveDamage}, maxDamage={_cfg.MaxDamage}, disableCrit={_cfg.DisableCrit}, enabled={_cfg.Enabled}.");

            if (!_warnedClamp && _cfg.MaxDamage > 0 && _cfg.Damage > _cfg.MaxDamage)
            {
                _warnedClamp = true;
                TShock.Log.ConsoleInfo($"[LuminiteFix] Peringatan: Damage={_cfg.Damage} melebihi MaxDamage={_cfg.MaxDamage}, dipotong ke {_cfg.MaxDamage}.");
            }
        }

        private static void DisableCrit(Projectile p)
        {
            var field = typeof(Projectile).GetField("crit", BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                if (field.FieldType == typeof(int))
                {
                    field.SetValue(p, 0);
                }
                else if (field.FieldType == typeof(bool))
                {
                    field.SetValue(p, false);
                }
                return;
            }

            var prop = typeof(Projectile).GetProperty("crit", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                if (prop.PropertyType == typeof(int))
                {
                    prop.SetValue(p, 0);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(p, false);
                }
            }
        }
    }
}
