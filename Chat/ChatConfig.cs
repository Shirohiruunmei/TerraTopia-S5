using System.IO;
using System.Text.Json;
using TShockAPI;

namespace TerraTopia.Chat
{
    public class ChatConfig
    {
        public bool Enabled { get; set; } = true;
        public string Format { get; set; } = "[{rank}] {name}: {text}";
        public byte R { get; set; } = 255;
        public byte G { get; set; } = 255;
        public byte B { get; set; } = 255;

        public static ChatConfig Load()
        {
            string dir = Path.Combine(TShock.SavePath, "TerraTopia");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "chat.json");

            if (!File.Exists(path))
            {
                var def = new ChatConfig();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(def, opts));
                return def;
            }

            return JsonSerializer.Deserialize<ChatConfig>(File.ReadAllText(path)) ?? new ChatConfig();
        }
    }
}
