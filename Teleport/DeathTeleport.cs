using System;
using System.Data;
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
                if (_db.State != System.Data.ConnectionState.Open)
                {
                    _db.Open();
                }

                using (var c = Cmd("CREATE TABLE IF NOT EXISTS tt_last_death (account TEXT PRIMARY KEY, x INTEGER NOT NULL, y INTEGER NOT NULL)", null))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        public void SaveDeath(string account, int x, int y)
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
