#if DEBUG
using System;
using System.Collections.Generic;
using ZtarVega.Models;

namespace ZtarVega.Data
{
    /// <summary>Inserts fictional sample games so the UI can be tested. Debug builds only.</summary>
    internal static class DevelopmentSeeder
    {
        private const int SampleGameCount = 3000;

        private static readonly string[] FirstWords = { "Super", "Mega", "Turbo", "Hyper", "Ultra", "Neo" };
        private static readonly string[] SecondWords = { "Dragon", "Ninja", "Space", "Robot", "Castle", "Racer", "Quest" };

        /// <summary>Adds sample games, but only when the database is empty.</summary>
        public static void SeedIfEmpty(IGameRepository repository)
        {
            if (repository.GetAll().Count > 0)
            {
                return;
            }

            var random = new Random(12345);
            var romTypes = (RomType[])Enum.GetValues(typeof(RomType));
            var regions = (GameRegion[])Enum.GetValues(typeof(GameRegion));
            var games = new List<Game>(SampleGameCount);

            for (int number = 1; number <= SampleGameCount; number++)
            {
                games.Add(new Game
                {
                    Title = FirstWords[random.Next(FirstWords.Length)] + " " +
                            SecondWords[random.Next(SecondWords.Length)] + " " + number.ToString("0000"),
                    Year = 1985 + random.Next(11),
                    RomType = romTypes[random.Next(romTypes.Length)],
                    Region = regions[random.Next(regions.Length)],
                    RomFilename = "sample-" + number.ToString("0000") + ".nes",
                    IsAvailable = random.Next(2) == 0,
                    IsFavorite = random.Next(20) == 0
                });
            }

            repository.AddRange(games);
        }
    }
}
#endif