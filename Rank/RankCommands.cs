using TShockAPI;
using TerraTopia.Economy;

namespace TerraTopia.Rank
{
    public class RankCommands
    {
        private readonly RankService _rank;
        private readonly RankConfig _cfg;
        private readonly EconomyService _economy;
        private readonly EconomyConfig _econ;

        public RankCommands(RankService rank, RankConfig cfg, EconomyService economy, EconomyConfig econ)
        {
            _rank = rank;
            _cfg = cfg;
            _economy = economy;
            _econ = econ;
        }

        public void ShowRank(CommandArgs args)
        {
            var p = args.Player;
            string account = p.Account?.Name;
            if (account == null)
            {
                p.SendErrorMessage("Kamu harus login dulu.");
                return;
            }
            if (!_cfg.Enabled)
            {
                p.SendErrorMessage("Sistem rank sedang nonaktif.");
                return;
            }

            int level = _rank.Get(account);
            p.SendInfoMessage($"Rank kamu: {_cfg.GetRankName(level)} (Level {level}/{_cfg.MaxLevel})");
            p.SendInfoMessage($"Bonus hadiah mob: +{level * _cfg.PercentPerLevel}%");

            if (level >= _cfg.MaxLevel)
            {
                p.SendInfoMessage("Kamu sudah di level maksimal.");
            }
            else
            {
                p.SendInfoMessage($"Biaya rankup berikutnya: {_cfg.GetRankupCost(level)} {_econ.CurrencyName}. Ketik /rankup.");
            }
        }

        public void DoRankUp(CommandArgs args)
        {
            var p = args.Player;
            string account = p.Account?.Name;
            if (account == null)
            {
                p.SendErrorMessage("Kamu harus login dulu.");
                return;
            }
            if (!_cfg.Enabled)
            {
                p.SendErrorMessage("Sistem rank sedang nonaktif.");
                return;
            }

            if (_rank.RankUp(account, _cfg, _economy, out string msg))
            {
                p.SendSuccessMessage(msg);
            }
            else
            {
                p.SendErrorMessage(msg);
            }
        }
    }
}
