using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Models;
using SteamEmuUtility.Common;

namespace PlayniteCommon
{
    public class PlayniteCommon
    {
        public static bool HasFeature(IPlayniteAPI PlayniteApi, Game game, GameFeature Feature)
        {
            if (game.FeatureIds.Contains(Feature.Id))
            {
                return true;
            }
            return false;
        }
        public static bool AddedFeature(IPlayniteAPI PlayniteApi, Game game, GameFeature Feature)
        {
            if (game.Features == null && SteamCommon.IsGameSteamGame(game))
            {
                game.FeatureIds = new List<Guid> { Feature.Id };
                return true;
            }
            if (!game.FeatureIds.Contains(Feature.Id) && SteamCommon.IsGameSteamGame(game))
            {
                game.FeatureIds.Add(Feature.Id);
                return true;
            }
            return false;
        }
        public static int AddFeatures(IPlayniteAPI PlayniteApi, IEnumerable<Game> games, string Feature)
        {
            var feature = PlayniteApi.Database.Features.Add(Feature);
            return AddFeature(PlayniteApi, games, feature);
        }
        public static int AddFeature(IPlayniteAPI PlayniteApi, IEnumerable<Game> games, GameFeature Feature)
        {
            var count = 0;
            foreach (var game in games)
            {
                if (AddedFeature(PlayniteApi, game, Feature))
                {
                    count++;
                }
            }
            return count;
        }
        public static bool RemovedFeature(IPlayniteAPI PlayniteApi, Game game, GameFeature Feature)
        {
            if (game.FeatureIds.Contains(Feature.Id) && SteamCommon.IsGameSteamGame(game))
            {
                game.FeatureIds.Remove(Feature.Id);
                return true;
            }
            return false;
        }
        public static int RemoveFeatures(IPlayniteAPI PlayniteApi, IEnumerable<Game> games, string Feature)
        {
            var feature = PlayniteApi.Database.Features.Add(Feature);
            return RemoveFeature(PlayniteApi, games, feature);
        }
        public static int RemoveFeature(IPlayniteAPI PlayniteApi, IEnumerable<Game> games, GameFeature Feature)
        {
            var count = 0;
            foreach (var game in games)
            {
                if (RemovedFeature(PlayniteApi, game, Feature))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
