using System.Collections.Generic;
using Terraria;
using TShockAPI;
using TerraTopia.Home;

namespace TerraTopia.House
{
    public class HouseCommands
    {
        private const int MaxWidth = 200;
        private const int MaxHeight = 50;

        private readonly HomeService _home;
        private readonly Dictionary<int, int[]> _pending = new Dictionary<int, int[]>();

        public HouseCommands(HomeService home)
        {
            _home = home;
        }

        public void House(CommandArgs args)
        {
            var p = args.Player;
            string account = p.Account?.Name;
            if (account == null)
            {
                p.SendErrorMessage("Kamu harus login dulu.");
                return;
            }

            if (args.Parameters.Count == 0)
            {
                ShowUsage(p);
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "set":
                    Set(args);
                    break;
                case "define":
                    Define(p, account);
                    break;
                case "info":
                    Info(p);
                    break;
                case "delete":
                    Delete(p, account);
                    break;
                default:
                    ShowUsage(p);
                    break;
            }
        }

        private void ShowUsage(TSPlayer p)
        {
            p.SendInfoMessage("Pakai: /house set 1, /house set 2, /house define, /house info, /house delete");
            p.SendInfoMessage($"Batas rumah: {MaxWidth} tile ke samping, {MaxHeight} tile ke atas. Hanya 1 rumah per pemain.");
        }

        private void Set(CommandArgs args)
        {
            var p = args.Player;

            if (args.Parameters.Count < 2 || (args.Parameters[1] != "1" && args.Parameters[1] != "2"))
            {
                p.SendErrorMessage("Pakai: /house set 1 atau /house set 2");
                return;
            }

            if (!_pending.TryGetValue(p.Index, out int[] pts))
            {
                pts = new[] { -1, -1, -1, -1 };
            }

            int offset = args.Parameters[1] == "1" ? 0 : 2;
            pts[offset] = p.TileX;
            pts[offset + 1] = p.TileY;
            _pending[p.Index] = pts;

            p.SendSuccessMessage($"Titik {args.Parameters[1]} diset di ({p.TileX}, {p.TileY}).");
        }

        private void Define(TSPlayer p, string account)
        {
            if (!_pending.TryGetValue(p.Index, out int[] pts) || pts[0] < 0 || pts[2] < 0)
            {
                p.SendErrorMessage("Set titik 1 dan titik 2 dulu dengan /house set 1 dan /house set 2.");
                return;
            }

            string name = HouseName(account);
            if (TShock.Regions.GetRegionByName(name) != null)
            {
                p.SendErrorMessage("Kamu sudah punya rumah. Hapus dulu dengan /house delete.");
                return;
            }

            int x1 = System.Math.Min(pts[0], pts[2]);
            int y1 = System.Math.Min(pts[1], pts[3]);
            int width = System.Math.Abs(pts[0] - pts[2]) + 1;
            int height = System.Math.Abs(pts[1] - pts[3]) + 1;

            if (width > MaxWidth || height > MaxHeight)
            {
                p.SendErrorMessage($"Rumah terlalu besar ({width}x{height}). Maksimal {MaxWidth}x{MaxHeight}.");
                return;
            }

            bool ok = TShock.Regions.AddRegion(x1, y1, width, height, name, account, Main.worldID);
            if (!ok)
            {
                p.SendErrorMessage("Gagal membuat region rumah. Coba lagi atau hubungi admin.");
                return;
            }

            _home.Set(account, name, x1 + width / 2, y1 + height / 2);
            _pending.Remove(p.Index);

            p.SendSuccessMessage($"Rumah dibuat ({width}x{height}) dan jadi milikmu. Pakai /tph untuk kembali ke rumah.");
        }

        private void Info(TSPlayer p)
        {
            var names = TShock.Regions.InAreaRegionName(p.TileX, p.TileY);
            bool found = false;

            if (names != null)
            {
                foreach (var name in names)
                {
                    var region = TShock.Regions.GetRegionByName(name);
                    if (region != null)
                    {
                        found = true;
                        p.SendInfoMessage($"Region {name} milik {region.Owner}.");
                    }
                }
            }

            if (!found)
            {
                p.SendInfoMessage("Tidak ada region di tempat ini.");
            }
        }

        private void Delete(TSPlayer p, string account)
        {
            string name = HouseName(account);
            if (TShock.Regions.GetRegionByName(name) == null)
            {
                p.SendErrorMessage("Kamu belum punya rumah.");
                return;
            }

            TShock.Regions.DeleteRegion(name);
            _home.Delete(account);
            p.SendSuccessMessage("Rumahmu sudah dihapus. Kamu bisa membuat rumah baru.");
        }

        private static string HouseName(string account)
        {
            return "house_" + account.ToLower();
        }
    }
}
