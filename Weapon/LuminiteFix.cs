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

        public LuminiteFix()
        {
            _cfg = LuminiteConfig.Load();
            _lastWrite = File.GetLastWriteTimeUtc(LuminiteConfig.PathFile);
            TShock.Log.ConsoleInfo($"[LuminiteFix] Aktif: projectileId={_cfg.ProjectileId}, damage={_cfg.Damage}, disableCrit={_cfg.DisableCrit}, enabled={_cfg.Enabled}.");
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

                for (int i = 0; i < Main.projectile.Length; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || !p.friendly || p.type != _cfg.ProjectileId)
                    {
                        continue;
                    }

                    p.damage = _cfg.Damage;

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
            TShock.Log.ConsoleInfo($"[TerraTopia] luminite.json dimuat ulang: damage={_cfg.Damage}, enabled={_cfg.Enabled}.");
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
