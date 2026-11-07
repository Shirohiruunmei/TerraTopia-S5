using System;
using System.Data;
using TerraTopia.Economy;

namespace TerraTopia.Rank
{
    public class RankService
    {
        private readonly IDbConnection _db;
        private readonly object _lock = new object();

        public RankService(IDbConnection db)
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

                using var c = Cmd("CREATE TABLE IF NOT EXISTS tt_ranks (account TEXT PRIMARY KEY, level INTEGER NOT NULL DEFAULT 0)", null);
                c.ExecuteNonQuery();
            }
        }

        public int Get(string account)
        {
            lock (_lock)
            {
                using var c = Cmd("SELECT level FROM tt_ranks WHERE account = @a", null, ("@a", account));
                object result = c.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        public static long ApplyMultiplier(long reward, int level, RankConfig cfg)
        {
            long percent = 100 + (long)cfg.PercentPerLevel * level;
            return reward * percent / 100;
        }

        public bool RankUp(string account, RankConfig cfg, EconomyService economy, out string message)
        {
            message = null;
            int level = Get(account);

            if (level >= cfg.MaxLevel)
            {
                message = "Kamu sudah di level maksimal.";
                return false;
            }

            long cost = cfg.GetRankupCost(level);
            if (!economy.Adjust(account, -cost, out string error))
            {
                message = error;
                return false;
            }

            if (!SetLevel(account, level + 1))
            {
                economy.Adjust(account, cost, out _);
                message = "Gagal menyimpan rank, koin dikembalikan.";
                return false;
            }

            message = $"Rankup berhasil! Sekarang level {level + 1} ({cfg.GetRankName(level + 1)}).";
            return true;
        }

        private bool SetLevel(string account, int level)
        {
            lock (_lock)
            {
                using var c = Cmd(
                    "INSERT INTO tt_ranks (account, level) VALUES (@a, @l) ON CONFLICT(account) DO UPDATE SET level = @l",
                    null, ("@a", account), ("@l", level));
                return c.ExecuteNonQuery() > 0;
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
