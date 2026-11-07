using System;
using System.Collections.Generic;
using Terraria;
using TShockAPI;
using TerraTopia.Economy;

namespace TerraTopia.Lps
{
    public class LpsCommands
    {
        private const int InventorySlots = 50;
        private const int MaxChunk = 999;

        private readonly LpsService _lps;
        private readonly EconomyService _economy;
        private readonly EconomyConfig _econ;

        public LpsCommands(LpsService lps, EconomyService economy, EconomyConfig econ)
        {
            _lps = lps;
            _economy = economy;
            _econ = econ;
        }

        public void Lps(CommandArgs args)
        {
            var p = args.Player;
            var list = _lps.ListActive(20);
            if (list.Count == 0)
            {
                p.SendInfoMessage("Belum ada listing aktif. Pegang item lalu ketik /lpss <harga>.");
                return;
            }

            p.SendInfoMessage("Player Shop (/lpsb <nomor> untuk beli):");
            foreach (var l in list)
            {
                p.SendInfoMessage($"[{l.Id}] {ItemName(l.ItemType)} x{l.Stack}{PrefixText(l.Prefix)} - {l.Price} {_econ.CurrencyName} (penjual: {l.Seller})");
            }
        }

        public void Lpss(CommandArgs args)
        {
            var p = args.Player;
            string account = RequireAccount(p);
            if (account == null)
            {
                return;
            }

            if (args.Parameters.Count < 1 || !long.TryParse(args.Parameters[0], out long price) || price <= 0)
            {
                p.SendErrorMessage("Pakai: /lpss <harga>. Pegang item yang mau dijual di tangan.");
                return;
            }

            int sel = p.TPlayer.selectedItem;
            Item held = p.TPlayer.inventory[sel];
            if (held == null || held.type == 0 || held.stack <= 0)
            {
                p.SendErrorMessage("Pegang item yang mau dijual di tangan.");
                return;
            }

            int type = held.type;
            int stack = held.stack;
            int prefix = held.prefix;

            // 1) Catat listing dulu. Kalau gagal di sini, item masih ada di tangan.
            long id = _lps.Create(account, type, stack, prefix, price, out string error);
            if (id < 0)
            {
                p.SendErrorMessage(error);
                return;
            }

            // 2) Baru ambil item dari inventory. Kalau gagal, listing dibatalkan lagi.
            try
            {
                p.TPlayer.inventory[sel] = new Item();
                NetMessage.SendData(5, p.Index, -1, null, p.Index, sel);
            }
            catch (Exception ex)
            {
                _lps.SetStatus(id, "cancelled");
                TShock.Log.Error("[TerraTopia] LPSS gagal ambil item: " + ex.Message);
                p.SendErrorMessage("Gagal menitipkan item, listing dibatalkan.");
                return;
            }

            p.SendSuccessMessage($"Listing [{id}] dibuat: {ItemName(type)} x{stack}{PrefixText(prefix)} seharga {price} {_econ.CurrencyName}.");
        }

        public void Lpsb(CommandArgs args)
        {
            var p = args.Player;
            string account = RequireAccount(p);
            if (account == null)
            {
                return;
            }

            if (args.Parameters.Count < 1 || !long.TryParse(args.Parameters[0], out long id))
            {
                p.SendErrorMessage("Pakai: /lpsb <nomor>. Lihat nomornya di /lps.");
                return;
            }

            var l = _lps.GetActive(id);
            if (l == null)
            {
                p.SendErrorMessage("Listing tidak ditemukan atau sudah tidak aktif.");
                return;
            }

            if (l.Seller == account)
            {
                p.SendErrorMessage("Kamu tidak bisa membeli listing sendiri.");
                return;
            }

            // 1) Tandai terjual. Kalau ternyata sudah dibeli orang lain, berhenti.
            if (!_lps.SetStatus(id, "sold"))
            {
                p.SendErrorMessage("Listing sudah tidak tersedia.");
                return;
            }

            // 2) Pindahkan koin dari pembeli ke penjual.
            if (!_economy.Transfer(account, l.Seller, l.Price, out string error))
            {
                _lps.Revert(id);
                p.SendErrorMessage(error);
                return;
            }

            // 3) Beri item ke pembeli. Sisanya masuk klaim, tidak dibuang.
            int left = DeliverChunks(p, l.ItemType, l.Stack, l.Prefix);
            if (left > 0)
            {
                _lps.AddClaim(account, l.ItemType, left, l.Prefix);
                p.SendInfoMessage($"Inventory penuh. {left} item masuk klaim, ambil dengan /lpsk.");
            }

            p.SendSuccessMessage($"Kamu membeli {ItemName(l.ItemType)} x{l.Stack} seharga {l.Price} {_econ.CurrencyName}.");
            ForwardToSeller(l, account);
        }

        public void Lpsc(CommandArgs args)
        {
            var p = args.Player;
            string account = RequireAccount(p);
            if (account == null)
            {
                return;
            }

            if (args.Parameters.Count < 1 || !long.TryParse(args.Parameters[0], out long id))
            {
                p.SendErrorMessage("Pakai: /lpsc <nomor>.");
                return;
            }

            var l = _lps.GetActive(id);
            if (l == null || l.Seller != account)
            {
                p.SendErrorMessage("Listing tidak ditemukan, bukan milikmu, atau sudah tidak aktif.");
                return;
            }

            if (!_lps.SetStatus(id, "cancelled"))
            {
                p.SendErrorMessage("Listing sudah tidak aktif.");
                return;
            }

            int left = DeliverChunks(p, l.ItemType, l.Stack, l.Prefix);
            if (left > 0)
            {
                _lps.AddClaim(account, l.ItemType, left, l.Prefix);
                p.SendInfoMessage($"Inventory penuh. {left} item masuk klaim, ambil dengan /lpsk.");
            }

            p.SendSuccessMessage($"Listing [{id}] dibatalkan. {ItemName(l.ItemType)} x{l.Stack} dikembalikan.");
        }

        public void Lpsk(CommandArgs args)
        {
            var p = args.Player;
            string account = RequireAccount(p);
            if (account == null)
            {
                return;
            }

            var claims = _lps.GetClaims(account);
            if (claims.Count == 0)
            {
                p.SendInfoMessage("Tidak ada item di klaim.");
                return;
            }

            int taken = 0;
            foreach (var c in claims)
            {
                int left = DeliverChunks(p, c.ItemType, c.Stack, c.Prefix);
                if (left == 0)
                {
                    _lps.DeleteClaim(c.Id);
                }
                else
                {
                    _lps.UpdateClaimStack(c.Id, left);
                }
                taken++;
            }

            p.SendSuccessMessage($"Klaim diproses: {taken} entri. Yang belum muat tetap di klaim.");
        }

        // Memberi item ke pemain. Mengembalikan jumlah yang tidak muat.
        private int DeliverChunks(TSPlayer p, int type, int stack, int prefix)
        {
            int left = stack;
            while (left > 0 && FindEmptySlot(p) >= 0)
            {
                int chunk = Math.Min(left, MaxChunk);
                p.GiveItem(type, chunk, prefix);
                left -= chunk;
            }
            return left;
        }

        private void ForwardToSeller(LpsListing l, string buyer)
        {
            foreach (var t in TShock.Players)
            {
                if (t != null && t.Account != null && t.Account.Name == l.Seller)
                {
                    t.SendSuccessMessage($"{buyer} membeli {ItemName(l.ItemType)} x{l.Stack} dari listing [{l.Id}]. +{l.Price} {_econ.CurrencyName}.");
                }
            }
        }

        private static int FindEmptySlot(TSPlayer p)
        {
            var inv = p.TPlayer.inventory;
            int max = Math.Min(InventorySlots, inv.Length);
            for (int i = 0; i < max; i++)
            {
                if (inv[i] == null || inv[i].type == 0 || inv[i].stack <= 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string RequireAccount(TSPlayer p)
        {
            string account = p.Account?.Name;
            if (account == null)
            {
                p.SendErrorMessage("Kamu harus login dulu.");
            }
            return account;
        }

        private static string ItemName(int type)
        {
            try
            {
                return TShock.Utils.GetItemById(type).Name;
            }
            catch
            {
                return "Item#" + type;
            }
        }

        private static string PrefixText(int prefix)
        {
            return prefix > 0 ? $" [prefix {prefix}]" : "";
        }
    }
}
