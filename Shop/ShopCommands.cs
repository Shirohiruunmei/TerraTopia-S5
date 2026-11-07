using System;
using System.Linq;
using TShockAPI;
using TerraTopia.Economy;

namespace TerraTopia.Shop
{
    public class ShopCommands
    {
        private readonly EconomyService _economy;
        private readonly EconomyConfig _econ;
        private readonly ShopConfig _shop;

        public ShopCommands(EconomyService economy, EconomyConfig econ, ShopConfig shop)
        {
            _economy = economy;
            _econ = econ;
            _shop = shop;
        }

        public void Sb(CommandArgs args)
        {
            var p = args.Player;
            string account = p.Account?.Name;
            if (account == null)
            {
                p.SendErrorMessage("Kamu harus login dulu.");
                return;
            }

            if (!InShopRegion(p))
            {
                p.SendErrorMessage($"Shop hanya bisa dipakai di region {_shop.RegionName}.");
                return;
            }

            if (args.Parameters.Count == 0)
            {
                ShowList(p);
                return;
            }

            if (args.Parameters.Count < 2
                || !int.TryParse(args.Parameters[0], out int id)
                || !int.TryParse(args.Parameters[1], out int qty)
                || qty <= 0)
            {
                p.SendErrorMessage("Pakai: /sb <id> <jumlah>. Ketik /sb untuk melihat daftar.");
                return;
            }

            var item = _shop.Items.FirstOrDefault(i => i.Id == id);
            if (item == null)
            {
                p.SendErrorMessage("ID item tidak ditemukan.");
                return;
            }

            if (qty > item.MaxStack)
            {
                p.SendErrorMessage($"Maksimal {item.MaxStack} per pembelian.");
                return;
            }

            long total = item.Price * qty;
            if (!_economy.Adjust(account, -total, out string error))
            {
                p.SendErrorMessage(error);
                return;
            }

            try
            {
                p.GiveItem(item.ItemType, qty);
            }
            catch (Exception ex)
            {
                _economy.Adjust(account, total, out _);
                TShock.Log.Error("[TerraTopia] GiveItem gagal: " + ex.Message);
                p.SendErrorMessage("Gagal memberikan item, koin dikembalikan.");
                return;
            }

            p.SendSuccessMessage($"Kamu membeli {qty} {item.Name} seharga {total} {_econ.CurrencyName}.");
        }

        private bool InShopRegion(TSPlayer p)
        {
            var names = TShock.Regions.InAreaRegionName(p.TileX, p.TileY);
            return names != null && names.Contains(_shop.RegionName);
        }

        private void ShowList(TSPlayer p)
        {
            p.SendInfoMessage($"Shop {_shop.RegionName} (pakai /sb <id> <jumlah>):");
            foreach (var i in _shop.Items)
            {
                p.SendInfoMessage($"[{i.Id}] {i.Name} - {i.Price} {_econ.CurrencyName}/biji");
            }
        }
    }
}
