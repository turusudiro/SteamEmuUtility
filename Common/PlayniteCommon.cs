using System;
using System.Collections.Generic;
using Playnite.SDK.Models;
using SteamCommon;

namespace PlayniteCommon
{
    public class PlayniteUtilities
    {
        public static bool AddedFeature(Game game, GameFeature Feature)
        {
            if (game.Features == null && SteamUtilities.IsGameSteamGame(game))
            {
                game.FeatureIds = new List<Guid> { Feature.Id };
                return true;
            }
            if (!game.FeatureIds.Contains(Feature.Id) && SteamUtilities.IsGameSteamGame(game))
            {
                game.FeatureIds.Add(Feature.Id);
                return true;
            }
            return false;
        }
        public static int AddFeatures(IEnumerable<Game> games, GameFeature Feature)
        {
            var count = 0;
            foreach (var game in games)
            {
                if (AddedFeature(game, Feature))
                {
                    count++;
                }
            }
            return count;
        }
        public static bool RemovedFeature(Game game, GameFeature Feature)
        {
            if (game.FeatureIds.Contains(Feature.Id) && SteamUtilities.IsGameSteamGame(game))
            {
                game.FeatureIds.Remove(Feature.Id);
                return true;
            }
            return false;
        }
        public static int RemoveFeatures(IEnumerable<Game> games, GameFeature Feature)
        {
            var count = 0;
            foreach (var game in games)
            {
                if (RemovedFeature(game, Feature))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
