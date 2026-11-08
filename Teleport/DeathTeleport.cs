using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using TShockAPI;
using TerraTopia.Economy;

namespace TerraTopia.Teleport
{
    public class DeathTeleport
    {
        private const long Cost = 5;

        private readonly IDbConnection _db;
        private readonly EconomyService _economy;
        private readonly EconomyConfig _econ;
        private readonly object _lock = new object();
        private readonly List<KeyValuePair<EventInfo, Delegate>> _subs = new List<KeyValuePair<EventInfo, Delegate>>();

        public DeathTeleport(IDbConnection db, EconomyService economy, EconomyConfig econ)
        {
            _db = db;
            _economy = economy;
            _econ = econ;
        }

        public void Init()
        {
            lock (_lock)
            {
                if (_db.State != ConnectionState.Open)
                {
                    _db.Open();
                }

                using (var c = Cmd("CREATE TABLE IF NOT EXISTS tt_last_death (account TEXT PRIMARY KEY, x INTEGER NOT NULL, y INTEGER NOT NULL)", null))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

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
                        if (!ev.Name.Contains("PlayerDeath"))
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
                        TShock.Log.ConsoleInfo($"[TpD] Terhubung ke {t.FullName}.{ev.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] DeathTeleport gagal mencari event: " + ex.Message);
            }

            if (_subs.Count == 0)
            {
                TShock.Log.ConsoleInfo("[TpD] Event kematian tidak ditemukan. /tpd tidak akan punya data.");
            }
        }

        public void Unregister()
        {
            foreach (var s in _subs)
            {
                try { s.Key.RemoveEventHandler(null, s.Value); } catch { }
            }
            _subs.Clear();
        }

        public void TpD(CommandArgs args)
        {
            var p = args.Player;
            string account = p.Account?.Name;
            if (account == null)
            {
                p.SendErrorMessage("Kamu harus login dulu.");
                return;
            }

            int x, y;
            if (!GetLastDeath(account, out x, out y))
            {
                p.SendErrorMessage("Belum ada catatan lokasi mati untuk akunmu.");
                return;
            }

            if (!_economy.Adjust(account, -Cost, out string error))
            {
                p.SendErrorMessage(error);
                return;
            }

            try
            {
                p.Teleport(x * 16f, y * 16f);
            }
            catch (Exception ex)
            {
                _economy.Adjust(account, Cost, out _);
                TShock.Log.Error("[TerraTopia] TpD gagal: " + ex.Message);
                p.SendErrorMessage("Gagal teleport, koin dikembalikan.");
                return;
            }

            p.SendSuccessMessage($"Kamu dipindahkan ke lokasi mati terakhir. Biaya {Cost} {_econ.CurrencyName}.");
        }

        private void SaveDeath(string account, int x, int y)
        {
            lock (_lock)
            {
                using (var c = Cmd(
                    "INSERT INTO tt_last_death (account, x, y) VALUES (@a, @x, @y) ON CONFLICT(account) DO UPDATE SET x = @x, y = @y",
                    null, ("@a", account), ("@x", x), ("@y", y)))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        private bool GetLastDeath(string account, out int x, out int y)
        {
            x = 0;
            y = 0;
            lock (_lock)
            {
                using (var c = Cmd("SELECT x, y FROM tt_last_death WHERE account = @a", null, ("@a", account)))
                using (var r = c.ExecuteReader())
                {
                    if (!r.Read())
                    {
                        return false;
                    }
                    x = Convert.ToInt32(r[0]);
                    y = Convert.ToInt32(r[1]);
                    return true;
                }
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
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnDeath2), BindingFlags.NonPublic | BindingFlags.Instance));
                }
                if (count == 1)
                {
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnDeath1), BindingFlags.NonPublic | BindingFlags.Instance));
                }
            }
            catch { }
            return null;
        }

        private void OnDeath2(object sender, object e) => HandleDeath(e);

        private void OnDeath1(object e) => HandleDeath(e);

        private void HandleDeath(object e)
        {
            try
            {
                if (e == null)
                {
                    return;
                }

                var player = GetValue(e, "Player") as TSPlayer;
                if (player?.Account == null)
                {
                    return;
                }

                SaveDeath(player.Account.Name, player.TileX, player.TileY);
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] Catat lokasi mati gagal: " + ex.Message);
            }
        }

        private static object GetValue(object obj, string prop)
        {
            return obj.GetType()
                .GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?.GetValue(obj);
        }

        private IDbCommand Cmd(string sql, IDbTransaction tx, params (string, object)[] parameters)
        {
            IDbCommand c = _db.CreateCommand();
            c.CommandText = sql;
            c.Transaction = tx;
            foreach (var (name, value) in parameters)
            {
                IDbDataParameter p = c.CreateParameter();
                p.ParameterName = name;
                p.Value = value;
                c.Parameters.Add(p);
            }
            return c;
        }
    }
}
