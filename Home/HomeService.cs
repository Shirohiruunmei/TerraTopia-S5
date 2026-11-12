using System;
using System.Data;

namespace TerraTopia.Home
{
    public class HomeService
    {
        private readonly IDbConnection _db;
        private readonly object _lock = new object();

        public HomeService(IDbConnection db)
        {
            _db = db;
        }

        public void Init()
        {
            lock (_lock)
            {
                if (_db.State != System.Data.ConnectionState.Open)
                {
                    _db.Open();
                }

                using (var c = Cmd("CREATE TABLE IF NOT EXISTS tt_homes (account TEXT PRIMARY KEY, region TEXT NOT NULL, x INTEGER NOT NULL, y INTEGER NOT NULL)", null))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        public void Set(string account, string region, int x, int y)
        {
            lock (_lock)
            {
                using (var c = Cmd(
                    "INSERT INTO tt_homes (account, region, x, y) VALUES (@a, @r, @x, @y) ON CONFLICT(account) DO UPDATE SET region = @r, x = @x, y = @y",
                    null, ("@a", account), ("@r", region), ("@x", x), ("@y", y)))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        public void Delete(string account)
        {
            lock (_lock)
            {
                using (var c = Cmd("DELETE FROM tt_homes WHERE account = @a", null, ("@a", account)))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        public bool Get(string account, out int x, out int y)
        {
            x = 0;
            y = 0;
            lock (_lock)
            {
                using (var c = Cmd("SELECT x, y FROM tt_homes WHERE account = @a", null, ("@a", account)))
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
