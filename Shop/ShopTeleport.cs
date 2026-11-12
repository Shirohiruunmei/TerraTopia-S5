using TShockAPI;

namespace TerraTopia.Shop
{
    public class ShopTeleport
    {
        private readonly ShopConfig _cfg;

        public ShopTeleport(ShopConfig cfg)
        {
            _cfg = cfg;
        }

        public void TpShop(CommandArgs args)
        {
            var p = args.Player;

            var region = TShock.Regions.GetRegionByName(_cfg.RegionName);
            if (region == null)
            {
                p.SendErrorMessage($"Region {_cfg.RegionName} belum dibuat. Hubungi admin.");
                return;
            }

            var area = region.Area;
            int x = area.X + area.Width / 2;
            int y = area.Y + area.Height / 2;

            p.Teleport(x * 16f, y * 16f);
            p.SendSuccessMessage("Kamu dipindahkan ke shop.");
        }
    }
}
