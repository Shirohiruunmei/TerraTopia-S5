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

            p.SendSuccessMessage($"Rumah diset di region {ownedRegion}. Saat mati, kamu akan respawn di sini.");
        }
    }
}
