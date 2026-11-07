using TerrariaApi.Server;
using TShockAPI;
using TerraTopia.Rank;

namespace TerraTopia.Chat
{
    public class ChatPrefix
    {
        private readonly ChatConfig _cfg;
        private readonly RankService _rank;
        private readonly RankConfig _rankCfg;

        public ChatPrefix(ChatConfig cfg, RankService rank, RankConfig rankCfg)
        {
            _cfg = cfg;
            _rank = rank;
            _rankCfg = rankCfg;
        }

        public void OnChat(ServerChatEventArgs e)
        {
            if (!_cfg.Enabled || e.Handled)
            {
                return;
            }

            string text = e.Text;
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("/") || text.StartsWith("."))
            {
                return;
            }

            var p = TShock.Players[e.Who];
            if (p == null || p.Account == null || p.mute)
            {
                return;
            }

            int level = _rank.Get(p.Account.Name);
            string rankName = _rankCfg.GetRankName(level);

            string msg = _cfg.Format
                .Replace("{rank}", rankName)
                .Replace("{name}", p.Name)
                .Replace("{text}", text);

            e.Handled = true;
            TSPlayer.All.SendMessage(msg, _cfg.R, _cfg.G, _cfg.B);
            TShock.Log.ConsoleInfo($"[Chat] {p.Name}: {text}");
        }
    }
}
