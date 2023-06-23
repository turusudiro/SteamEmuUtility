using System;
using Playnite;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK.Models;

namespace GreenBerg.Models.Steam
{
    public static class Steam
    {
        private static Guid steamPluginId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        public static bool IsGameSteamGame(Game game)
        {
            return game.PluginId == steamPluginId;
        }
    }
}
