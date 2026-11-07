using System;
using System.Collections.Generic;
using System.Data;
using TShockAPI;
using TerraTopia.Economy;

namespace TerraTopia.Lps
{
    public class LpsListing
    {
        public long Id { get; set; }
        public string Seller { get; set; }
        public int ItemType { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public long Price { get; set; }
    }

    public class LpsClaim
    {
        public long Id { get; set; }
        public int ItemType { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
    }

    public class LpsService
    {
        public const int MaxActivePerSeller = 3;

        private readonly IDbConnection _db;
        private readonly object _lock = new object();

        public LpsService(IDbConnection db)
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

                using (var c = Cmd("CREATE TABLE IF NOT EXISTS tt_lps_listings (id INTEGER PRIMARY KEY AUTOINCREMENT, seller TEXT NOT NULL, item_type INTEGER NOT NULL, stack INTEGER NOT NULL, prefix INTEGER NOT NULL DEFAULT 0, price INTEGER NOT NULL, status TEXT NOT NULL DEFAULT 'active')", null))
                {
                    c.ExecuteNonQuery();
                }

                using (var c = Cmd("CREATE TABLE IF NOT EXISTS tt_lps_claims (id INTEGER PRIMARY KEY AUTOINCREMENT, account TEXT NOT NULL, item_type INTEGER NOT NULL, stack INTEGER NOT NULL, prefix INTEGER NOT NULL DEFAULT 0)", null))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        // Membuat listing baru. Mengembalikan nomor listing, atau -1 kalau ditolak.
        public long Create(string seller, int itemType, int stack, int prefix, long price, out string error)
        {
            error = null;
            lock (_lock)
            {
                if (CountActive(seller) >= MaxActivePerSeller)
                {
                    error = $"Maksimal {MaxActivePerSeller} listing aktif per pemain.";
                    return -1;
                }

                using (var c = Cmd(
                    "INSERT INTO tt_lps_listings (seller, item_type, stack, prefix, price, status) VALUES (@s, @t, @n, @p, @c, 'active')",
                    null, ("@s", seller), ("@t", itemType), ("@n", stack), ("@p", prefix), ("@c", price)))
                {
                    c.ExecuteNonQuery();
                }

                using (var c = Cmd("SELECT last_insert_rowid()", null))
                {
                    return Convert.ToInt64(c.ExecuteScalar());
                }
            }
        }

        public List<LpsListing> ListActive(int count)
        {
            var list = new List<LpsListing>();
            lock (_lock)
            {
                using (var c = Cmd(
                    "SELECT id, seller, item_type, stack, prefix, price FROM tt_lps_listings WHERE status = 'active' ORDER BY id LIMIT @n",
                    null, ("@n", count)))
                using (var r = c.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(ReadListing(r));
                    }
                }
            }
            return list;
        }

        public LpsListing GetActive(long id)
        {
            lock (_lock)
            {
                using (var c = Cmd(
                    "SELECT id, seller, item_type, stack, prefix, price FROM tt_lps_listings WHERE id = @i AND status = 'active'",
                    null, ("@i", id)))
                using (var r = c.ExecuteReader())
                {
                    return r.Read() ? ReadListing(r) : null;
                }
            }
        }

        // Mengubah status dari 'active' ke status lain. Mengembalikan true kalau berhasil.
        public bool SetStatus(long id, string newStatus)
        {
            lock (_lock)
            {
                using (var c = Cmd(
                    "UPDATE tt_lps_listings SET status = @st WHERE id = @i AND status = 'active'",
                    null, ("@st", newStatus), ("@i", id)))
                {
                    return c.ExecuteNonQuery() > 0;
                }
            }
        }

        // Dipakai kalau pembayaran gagal setelah listing ditandai terjual.
        public void Revert(long id)
        {
            lock (_lock)
            {
                using (var c = Cmd(
                    "UPDATE tt_lps_listings SET status = 'active' WHERE id = @i AND status = 'sold'",
                    null, ("@i", id)))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        public void AddClaim(string account, int itemType, int stack, int prefix)
        {
            lock (_lock)
            {
                using (var c = Cmd(
                    "INSERT INTO tt_lps_claims (account, item_type, stack, prefix) VALUES (@a, @t, @n, @p)",
                    null, ("@a", account), ("@t", itemType), ("@n", stack), ("@p", prefix)))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        public List<LpsClaim> GetClaims(string account)
        {
            var list = new List<LpsClaim>();
            lock (_lock)
            {
                using (var c = Cmd(
                    "SELECT id, item_type, stack, prefix FROM tt_lps_claims WHERE account = @a ORDER BY id",
                    null, ("@a", account)))
                using (var r = c.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LpsClaim
                        {
                            Id = Convert.ToInt64(r[0]),
                            ItemType = Convert.ToInt32(r[1]),
                            Stack = Convert.ToInt32(r[2]),
                            Prefix = Convert.ToInt32(r[3])
                        });
                    }
                }
            }
            return list;
        }

        public void DeleteClaim(long id)
        {
            lock (_lock)
            {
                using (var c = Cmd("DELETE FROM tt_lps_claims WHERE id = @i", null, ("@i", id)))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        public void UpdateClaimStack(long id, int stack)
        {
            lock (_lock)
            {
                using (var c = Cmd("UPDATE tt_lps_claims SET stack = @n WHERE id = @i", null, ("@n", stack), ("@i", id)))
                {
                    c.ExecuteNonQuery();
                }
            }
        }

        private int CountActive(string seller)
        {
            using (var c = Cmd(
                "SELECT COUNT(*) FROM tt_lps_listings WHERE seller = @s AND status = 'active'",
                null, ("@s", seller)))
            {
                return Convert.ToInt32(c.ExecuteScalar());
            }
        }

        private static LpsListing ReadListing(IDataRecord r)
        {
            return new LpsListing
            {
                Id = Convert.ToInt64(r[0]),
                Seller = Convert.ToString(r[1]),
                ItemType = Convert.ToInt32(r[2]),
                Stack = Convert.ToInt32(r[3]),
                Prefix = Convert.ToInt32(r[4]),
                Price = Convert.ToInt64(r[5])
            };
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
