using System;
using System.Collections.Generic;
using System.Reflection;
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
        private readonly Dictionary<int, int> _marking = new Dictionary<int, int>();
        private readonly Dictionary<int, int[]> _pending = new Dictionary<int, int[]>();
        private readonly List<KeyValuePair<EventInfo, Delegate>> _subs = new List<KeyValuePair<EventInfo, Delegate>>();

        public HouseCommands(HomeService home)
        {
            _home = home;
        }

        public void RegisterTileHook()
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
                        if (!ev.Name.Contains("TileEdit"))
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
                        TShock.Log.ConsoleInfo($"[House] Terhubung ke {t.FullName}.{ev.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] House gagal mencari event pukul block: " + ex.Message);
            }

            if (_subs.Count == 0)
            {
                TShock.Log.ConsoleInfo("[House] Event pukul block tidak ditemukan. /house set tidak akan berfungsi.");
            }
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
                    StartMark(args);
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

        private void StartMark(CommandArgs args)
        {
            var p = args.Player;

            if (args.Parameters.Count < 2 || (args.Parameters[1] != "1" && args.Parameters[1] != "2"))
            {
                p.SendErrorMessage("Pakai: /house set 1 atau /house set 2");
                return;
            }

            int corner = args.Parameters[1] == "1" ? 1 : 2;
            _marking[p.Index] = corner;

            string sudut = corner == 1 ? "kiri atas" : "kanan bawah";
            p.SendInfoMessage($"Pukul block di sudut {sudut} rumahmu dengan pickaxe. Block tidak akan hancur, hanya ditandai.");
        }

        private void OnTileEdit(object e)
        {
            try
            {
                if (e == null)
                {
                    return;
                }

                var player = GetValue(e, "Player") as TSPlayer;
                if (player == null || !_marking.TryGetValue(player.Index, out int corner))
                {
                    return;
                }

                string action = GetValue(e, "Action")?.ToString() ?? "";
                if (!action.Contains("Kill"))
                {
                    return;
                }

                object xo = GetValue(e, "X");
                object yo = GetValue(e, "Y");
                if (xo == null || yo == null)
                {
                    TShock.Log.ConsoleInfo("[House] Field X/Y tidak ditemukan di event TileEdit.");
                    return;
                }

                int x = Convert.ToInt32(xo);
                int y = Convert.ToInt32(yo);

                if (!_pending.TryGetValue(player.Index, out int[] pts))
                {
                    pts = new[] { -1, -1, -1, -1 };
                }

                int offset = corner == 1 ? 0 : 2;
                pts[offset] = x;
                pts[offset + 1] = y;
                _pending[player.Index] = pts;

                SetHandled(e);
                _marking.Remove(player.Index);

                player.SendSuccessMessage($"Titik {corner} ditandai di ({x}, {y}).");
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] House gagal menandai block: " + ex.Message);
            }
        }

        private void Define(TSPlayer p, string account)
        {
            if (!_pending.TryGetValue(p.Index, out int[] pts) || pts[0] < 0 || pts[2] < 0)
            {
                p.SendErrorMessage("Tandai dulu dua sudut: /house set 1 lalu pukul block, dan /house set 2 lalu pukul block.");
                return;
            }

            string name = HouseName(account);
            if (TShock.Regions.GetRegionByName(name) != null)
            {
                p.SendErrorMessage("Kamu sudah punya rumah. Hapus dulu dengan /house delete.");
                return;
            }

            int x1 = Math.Min(pts[0], pts[2]);
            int y1 = Math.Min(pts[1], pts[3]);
            int width = Math.Abs(pts[0] - pts[2]) + 1;
            int height = Math.Abs(pts[1] - pts[3]) + 1;

            if (width > MaxWidth || height > MaxHeight)
            {
                p.SendErrorMessage($"Rumah terlalu besar ({width}x{height}). Maksimal {MaxWidth}x{MaxHeight}.");
                return;
            }

            bool ok = TShock.Regions.AddRegion(x1, y1, width, height, name, account, Main.worldID.ToString());
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
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnTileEdit2), BindingFlags.NonPublic | BindingFlags.Instance));
                }
                if (count == 1)
                {
                    return Delegate.CreateDelegate(delegateType, this, GetType().GetMethod(nameof(OnTileEdit1), BindingFlags.NonPublic | BindingFlags.Instance));
                }
            }
            catch { }
            return null;
        }

        private void OnTileEdit2(object sender, object e) => OnTileEdit(e);

        private void OnTileEdit1(object e) => OnTileEdit(e);

        private static object GetValue(object obj, string prop)
        {
            return obj.GetType()
                .GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?.GetValue(obj);
        }

        private static void SetHandled(object obj)
        {
            obj.GetType()
                .GetProperty("Handled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?.SetValue(obj, true);
        }
    }
}
