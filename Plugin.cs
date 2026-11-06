using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TerraTopia
{
    [ApiVersion(2, 1)]
    public class TerraTopiaPlugin : TerrariaPlugin
    {
        public override string Name => "TerraTopia";
        public override string Author => "TerraTopia";
        public override string Description => "TerraTopia SMP S5 - Hello World";
        public override Version Version => new Version(0, 1, 0);

        public TerraTopiaPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            TShock.Log.ConsoleInfo("[TerraTopia] Hello World! Plugin berhasil dimuat.");
        }
    }
}
