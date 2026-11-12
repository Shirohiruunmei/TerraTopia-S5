using TShockAPI;

namespace TerraTopia.Home
{
    public class HomeCommands
    {
        private readonly HomeService _home;

        public HomeCommands(HomeService home)
        {
            _home = home;
        }

        public void SetHome(CommandArgs args)
        {
            var p = args.Player;
            string account = p.Account?.Name;
            if (account == null)
            {
                p.SendErrorMessage("Kamu harus login dulu.");
                return;
            }

            int x = p.TileX;
            int y = p.TileY;

            string ownedRegion = null;
            var names = TShock.Regions.InAreaRegionName(x, y);
            if (names != null)
            {
                foreach (var name in names)
                {
                    var region = TShock.Regions.GetRegionByName(name);
                    if (region != null && region.Owner == account)
                    {
                        ownedRegion = name;
                        break;
                    }
                }
            }

            if (ownedRegion == null)
            {
                p.SendErrorMessage("Berdiri di dalam region rumahmu sendiri dulu, lalu ketik /sethome.");
                return;
            }

            _home.Set(account, ownedRegion, x, y);
            p.TPlayer.SpawnX = x;
            p.TPlayer.SpawnY = y;

            p.SendSuccessMessage($"Rumah diset di region {ownedRegion}. Pakai /tph untuk kembali ke rumah.");
        }

        public void TpH(CommandArgs args)
        {
            var p = args.Player;
            string account = p.Account?.Name;
            if (account == null)
            {
                p.SendErrorMessage("Kamu harus login dulu.");
                return;
            }

            if (!_home.Get(account, out int x, out int y))
            {
                p.SendErrorMessage("Kamu belum punya rumah. Berdiri di region rumahmu lalu ketik /sethome.");
                return;
            }

            p.Teleport(x * 16f, y * 16f);
            p.SendSuccessMessage("Kamu dipindahkan ke rumahmu.");
        }
    }
}
