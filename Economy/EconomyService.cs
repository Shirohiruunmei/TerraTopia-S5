using System;
using System.Collections.Generic;
using System.Data;
using TShockAPI;

namespace TerraTopia.Economy
{
    public class EconomyService
    {
        private readonly IDbConnection _db;
        private readonly object _lock = new object();

        public EconomyService(IDbConnection db)
        {
            _db = db;
        }

        public void Init()
{
    lock (_lock)
    {
        if (_db.State != ConnectionState.Open)
        {
            _db.Open();
        }

        using var c = Cmd("CREATE TABLE IF NOT EXISTS tt_balances (account TEXT PRIMARY KEY, balance INTEGER NOT NULL DEFAULT 0)", null);
        c.ExecuteNonQuery();
    }
        }

        public long Get(string account)
        {
            lock (_lock)
            {
                return Read(account, null);
            }
        }

        public bool Adjust(string account, long delta, out string error)
        {
            error = null;
            lock (_lock)
            {
                using var tx = _db.BeginTransaction();
                try
                {
                    Ensure(account, tx);
                    if (Read(account, tx) + delta < 0)
                    {
                        tx.Rollback();
                        error = "Saldo tidak cukup.";
                        return false;
                    }
                    Exec("UPDATE tt_balances SET balance = balance + @d WHERE account = @a", tx,
                        ("@d", delta), ("@a", account));
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    SafeRollback(tx);
                    TShock.Log.Error("[TerraTopia] Adjust gagal: " + ex.Message);
                    error = "Terjadi kesalahan database.";
                    return false;
                }
            }
        }

        public bool SetBalance(string account, long value, out string error)
        {
            error = null;
            if (value < 0)
            {
                error = "Saldo tidak boleh negatif.";
                return false;
            }
            lock (_lock)
            {
                using var tx = _db.BeginTransaction();
                try
                {
                    Ensure(account, tx);
                    Exec("UPDATE tt_balances SET balance = @v WHERE account = @a", tx,
                        ("@v", value), ("@a", account));
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    SafeRollback(tx);
                    TShock.Log.Error("[TerraTopia] SetBalance gagal: " + ex.Message);
                    error = "Terjadi kesalahan database.";
                    return false;
                }
            }
        }

        public bool Transfer(string from, string to, long amount, out string error)
        {
            error = null;
            if (amount <= 0)
            {
                error = "Jumlah harus lebih dari 0.";
                return false;
            }
            if (from == to)
            {
                error = "Tidak bisa mengirim ke diri sendiri.";
                return false;
            }

            lock (_lock)
            {
                using var tx = _db.BeginTransaction();
                try
                {
                    Ensure(from, tx);
                    Ensure(to, tx);

                    if (Read(from, tx) < amount)
                    {
                        tx.Rollback();
                        error = "Saldo tidak cukup.";
                        return false;
                    }

                    Exec("UPDATE tt_balances SET balance = balance - @n WHERE account = @a", tx,
                        ("@n", amount), ("@a", from));
                    Exec("UPDATE tt_balances SET balance = balance + @n WHERE account = @a", tx,
                        ("@n", amount), ("@a", to));

                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    SafeRollback(tx);
                    TShock.Log.Error("[TerraTopia] Transfer gagal: " + ex.Message);
                    error = "Terjadi kesalahan database.";
                    return false;
                }
            }
        }

        public List<KeyValuePair<string, long>> Top(int count)
        {
            var list = new List<KeyValuePair<string, long>>();
            lock (_lock)
            {
                using var c = Cmd("SELECT account, balance FROM tt_balances ORDER BY balance DESC LIMIT @n", null,
                    ("@n", count));
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new KeyValuePair<string, long>(r.GetString(0), r.GetInt64(1)));
                }
            }
            return list;
        }

        private void Ensure(string account, IDbTransaction tx)
        {
            Exec("INSERT OR IGNORE INTO tt_balances (account, balance) VALUES (@a, 0)", tx, ("@a", account));
        }

        private long Read(string account, IDbTransaction tx)
        {
            using var c = Cmd("SELECT balance FROM tt_balances WHERE account = @a", tx, ("@a", account));
            object result = c.ExecuteScalar();
            return result == null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
        }

        private void Exec(string sql, IDbTransaction tx, params (string, object)[] parameters)
        {
            using var c = Cmd(sql, tx, parameters);
            c.ExecuteNonQuery();
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
