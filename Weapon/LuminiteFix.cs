using System;
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

        public LuminiteFix()
        {
            _cfg = LuminiteConfig.Load();
            _lastWrite = File.GetLastWriteTimeUtc(LuminiteConfig.PathFile);
            LogActive();
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
