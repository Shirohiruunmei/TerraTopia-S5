using System;
using System.Data;
using TShockAPI;

namespace TerraTopia.Rank
{
    public class BossLimitService
    {
        private readonly IDbConnection _db;
        private readonly object _lock = new object();
        private bool _ready;

        public BossLimitService(IDbConnection db)
        {
            _db = db;
        }

        // Mengembalikan true kalau hadiah boss masih boleh diberikan hari ini.
        // used = jumlah hadiah boss yang sudah dipakai hari ini (termasuk yang baru).
        public bool TryUse(string account, int limit, out int used)
        {
            used = 0;
            lock (_lock)
            {
                EnsureTable();
                string day = DateTime.Now.ToString("yyyy-MM-dd");

                using var tx = _db.BeginTransaction();
                try
                {
                    using (var ins = Cmd(
                        "INSERT OR IGNORE INTO tt_boss_daily (account, day, count) VALUES (@a, @d, 0)",
                        tx, ("@a", account), ("@d", day)))
                    {
                        ins.ExecuteNonQuery();
                    }

                    long current;
                    using (var rd = Cmd(
                        "SELECT count FROM tt_boss_daily WHERE account = @a AND day = @d",
                        tx, ("@a", account), ("@d", day)))
                    {
                        current = Convert.ToInt64(rd.ExecuteScalar());
                    }

                    if (current >= limit)
                    {
                        tx.Rollback();
                        used = (int)current;
                        return false;
                    }

                    using (var up = Cmd(
                        "UPDATE tt_boss_daily SET count = count + 1 WHERE account = @a AND day = @d",
                        tx, ("@a", account), ("@d", day)))
                    {
                        up.ExecuteNonQuery();
                    }

                    tx.Commit();
                    used = (int)current + 1;
                    return true;
                }
                catch (Exception ex)
                {
                    SafeRollback(tx);
                    TShock.Log.Error("[TerraTopia] BossLimit gagal: " + ex.Message);
                    return false;
                }
            }
        }

        private void EnsureTable()
        {
            if (_ready)
            {
                return;
            }

            if (_db.State != System.Data.ConnectionState.Open)
            {
                _db.Open();
            }

            using (var c = Cmd(
                "CREATE TABLE IF NOT EXISTS tt_boss_daily (account TEXT NOT NULL, day TEXT NOT NULL, count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (account, day))",
                null))
            {
                c.ExecuteNonQuery();
            }

            _ready = true;
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

        private static void SafeRollback(IDbTransaction tx)
        {
            try { tx.Rollback(); } catch { }
        }
    }
}
