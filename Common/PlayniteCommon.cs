using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteCommon
{
    public class PlayniteUtilities
    {
        public static bool HasFeature(Game game, string featureName)
        {
            if (game.Features != null && game.Features.Any(feature => feature.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }
        private static bool HasFeature(Game game, GameFeature feature)
        {
            if (game.Features != null && game.FeatureIds.Contains(feature.Id))
            {
                return true;
            }
            return false;
        }
        public static int AddFeatures(IEnumerable<Game> games, IEnumerable<GameFeature> features)
        {
            int count = 0;
            foreach (Game game in games)
            {
                bool allFeaturesAdded = true;
                foreach (GameFeature feature in features)
                {
                    if (HasFeature(game, feature))
                    {
                        continue;
                    }
                    if (!AddedFeature(game, feature))
                    {
                        allFeaturesAdded = false;
                    }
                }
                if (allFeaturesAdded)
                {
                    count++;
                }
            }
            return count;
        }
        public static int AddFeatures(IEnumerable<Game> games, GameFeature feature)
        {
            var count = 0;
            foreach (var game in games)
            {
                if (AddedFeature(game, feature))
                {
                    count++;
                }
            }
            return count;
        }
        private static bool AddedFeature(Game game, GameFeature feature)
        {
            if (game.Features == null)
            {
                game.FeatureIds = new List<Guid> { feature.Id };
                return true;
            }
            if (!game.FeatureIds.Contains(feature.Id))
            {
                game.FeatureIds.Add(feature.Id);
                return true;
            }
            return false;
        }
        public static int RemoveFeatures(IEnumerable<Game> games, IEnumerable<GameFeature> features)
        {
            int count = 0;
            foreach (Game game in games)
            {
                foreach (GameFeature feature in features)
                {
                    RemovedFeature(game, feature);
                }
                count++;
            }
            return count;
        }
        public static int RemoveFeatures(IEnumerable<Game> games, GameFeature feature)
        {
            var count = 0;
            foreach (var game in games)
            {
                if (RemovedFeature(game, feature))
                {
                    count++;
                }
            }
            return count;
        }
        private static bool RemovedFeature(Game game, GameFeature feature)
        {
            if (game.Features != null && game.FeatureIds.Contains(feature.Id))
            {
                game.FeatureIds.Remove(feature.Id);
                return true;
            }
            return false;
        }
    }
}
