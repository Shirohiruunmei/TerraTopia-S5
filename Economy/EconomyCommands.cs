using System.Linq;
using TShockAPI;

namespace TerraTopia.Economy
{
    public class EconomyCommands
    {
        private readonly EconomyService _economy;
        private readonly EconomyConfig _config;

        public EconomyCommands(EconomyService economy, EconomyConfig config)
        {
            _economy = economy;
            _config = config;
        }

        private string CurrencyName => _config.CurrencyName;

        public void Bal(CommandArgs args)
        {
            string account = args.Player.Account?.Name;
            if (account == null)
            {
                args.Player.SendErrorMessage("Kamu harus login dulu.");
                return;
            }
            args.Player.SendInfoMessage($"Saldo kamu: {_economy.Get(account)} {CurrencyName}");
        }

        public void Pay(CommandArgs args)
        {
            string from = args.Player.Account?.Name;
            if (from == null)
            {
                args.Player.SendErrorMessage("Kamu harus login dulu.");
                return;
            }
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("Pakai: /pay <nama> <jumlah>");
                return;
            }

            var target = TShock.UserAccounts.GetUserAccountByName(args.Parameters[0]);
            if (target == null)
            {
                args.Player.SendErrorMessage("Akun tujuan tidak ditemukan.");
                return;
            }
            if (!long.TryParse(args.Parameters[1], out long amount))
            {
                args.Player.SendErrorMessage("Jumlah harus berupa angka.");
                return;
            }

            if (!_economy.Transfer(from, target.Name, amount, out string error))
            {
                args.Player.SendErrorMessage(error);
                return;
            }

            args.Player.SendSuccessMessage($"Kamu mengirim {amount} {CurrencyName} ke {target.Name}.");
            foreach (var p in TShock.Players)
            {
                if (p != null && p.Account != null && p.Account.Name == target.Name)
                {
                    p.SendSuccessMessage($"Kamu menerima {amount} {CurrencyName} dari {from}.");
                }
            }
        }

        public void Baltop(CommandArgs args)
        {
            var top = _economy.Top(10);
            if (top.Count == 0)
            {
                args.Player.SendInfoMessage("Belum ada saldo.");
                return;
            }
            args.Player.SendInfoMessage($"Top {CurrencyName}:");
            for (int i = 0; i < top.Count; i++)
            {
                args.Player.SendInfoMessage($"#{i + 1} {top[i].Key}: {top[i].Value} {CurrencyName}");
            }
        }

        public void GiveBal(CommandArgs args) => AdminChange(args, "give");
        public void TakeBal(CommandArgs args) => AdminChange(args, "take");
        public void SetBal(CommandArgs args) => AdminChange(args, "set");

        private void AdminChange(CommandArgs args, string mode)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage($"Pakai: /{mode}bal <nama> <jumlah>");
                return;
            }

            var target = TShock.UserAccounts.GetUserAccountByName(args.Parameters[0]);
            if (target == null)
            {
                args.Player.SendErrorMessage("Akun tidak ditemukan.");
                return;
            }
            if (!long.TryParse(args.Parameters[1], out long amount) || amount < 0)
            {
                args.Player.SendErrorMessage("Jumlah harus angka 0 atau lebih.");
                return;
            }

            bool ok;
            string error = null;
            if (mode == "give")
            {
                ok = _economy.Adjust(target.Name, amount, out error);
            }
            else if (mode == "take")
            {
                ok = _economy.Adjust(target.Name, -amount, out error);
            }
            else
            {
                ok = _economy.SetBalance(target.Name, amount, out error);
            }

            if (!ok)
            {
                args.Player.SendErrorMessage(error);
                return;
            }
            args.Player.SendSuccessMessage(
                $"Saldo {target.Name} sekarang: {_economy.Get(target.Name)} {CurrencyName}");
        }
    }
}
