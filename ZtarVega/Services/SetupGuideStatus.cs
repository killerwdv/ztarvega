using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>
    /// Which steps of the "Getting started" guide are done and which can be done now. The state is worked out
    /// from the real data (files, catalog, images), never from a flag the user ticks, so it cannot get out of date.
    /// </summary>
    internal sealed class SetupGuideStatus
    {
        // The steps that have to be done for the application to be usable. The others are optional.
        private static readonly SetupStep[] RequiredSteps = { SetupStep.Settings, SetupStep.Catalog, SetupStep.Scan };

        private readonly HashSet<SetupStep> _doneSteps;

        public SetupGuideStatus(IEnumerable<SetupStep> doneSteps)
        {
            _doneSteps = new HashSet<SetupStep>(doneSteps ?? throw new ArgumentNullException(nameof(doneSteps)));
        }

        /// <summary>True when every required step is done.</summary>
        public bool RequiredStepsDone
        {
            get { return RequiredSteps.All(IsDone); }
        }

        /// <summary>The first required step that is not done, or null when all of them are.</summary>
        public SetupStep? NextRequiredStep
        {
            get
            {
                foreach (SetupStep step in RequiredSteps)
                {
                    if (!IsDone(step))
                    {
                        return step;
                    }
                }

                return null;
            }
        }

        /// <summary>Works out the state of the steps from the real data of the application.</summary>
        public static SetupGuideStatus Create(AppSettings settings, GameCatalog catalog, AssetLocator assetLocator)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (assetLocator == null)
            {
                throw new ArgumentNullException(nameof(assetLocator));
            }

            var doneSteps = new List<SetupStep>();

            bool isEmulatorReady = !string.IsNullOrWhiteSpace(settings.EmulatorPath) && File.Exists(settings.EmulatorPath);
            bool isRomsFolderReady = !string.IsNullOrWhiteSpace(settings.RomsFolder) && Directory.Exists(settings.RomsFolder);

            if (isEmulatorReady && isRomsFolderReady)
            {
                doneSteps.Add(SetupStep.Settings);
            }

            if (catalog.GameCount > 0)
            {
                doneSteps.Add(SetupStep.Catalog);
            }

            if (catalog.AvailableCount > 0)
            {
                doneSteps.Add(SetupStep.Scan);
            }

            if (catalog.HasMetadata)
            {
                doneSteps.Add(SetupStep.Metadata);
            }

            if (assetLocator.HasAnyImages())
            {
                doneSteps.Add(SetupStep.Images);
            }

            return new SetupGuideStatus(doneSteps);
        }

        public bool IsDone(SetupStep step)
        {
            return _doneSteps.Contains(step);
        }

        /// <summary>A step can be done once the steps it depends on are done.</summary>
        public bool IsEnabled(SetupStep step)
        {
            return FirstMissingPrerequisite(step) == null;
        }

        /// <summary>The first step that has to be done before the given one, or null when there is none left.</summary>
        public SetupStep? FirstMissingPrerequisite(SetupStep step)
        {
            foreach (SetupStep prerequisite in GetPrerequisites(step))
            {
                if (!IsDone(prerequisite))
                {
                    return prerequisite;
                }
            }

            return null;
        }

        private static IEnumerable<SetupStep> GetPrerequisites(SetupStep step)
        {
            switch (step)
            {
                case SetupStep.Catalog:
                case SetupStep.Metadata:
                    return new[] { SetupStep.Settings, SetupStep.Catalog }.Where(prerequisite => prerequisite != step);

                case SetupStep.Scan:
                    return new[] { SetupStep.Settings, SetupStep.Catalog };

                case SetupStep.Images:
                    return new[] { SetupStep.Settings, SetupStep.Catalog, SetupStep.Scan };

                default:
                    return new SetupStep[0];
            }
        }
    }
}