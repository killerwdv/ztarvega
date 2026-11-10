using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ZtarVega.Models
{
    /// <summary>User settings stored in config.json.</summary>
    [DataContract]
    public sealed class AppSettings
    {
        /// <summary>Token replaced by the quoted ROM path inside the emulator arguments.</summary>
        public const string RomPlaceholder = "{rom}";

        public const string DefaultEmulatorArguments = RomPlaceholder;

        public const string DarkTheme = "dark";
        public const string LightTheme = "light";

        [DataMember(Name = "emulatorPath")]
        public string EmulatorPath { get; set; }

        [DataMember(Name = "emulatorArguments")]
        public string EmulatorArguments { get; set; }

        [DataMember(Name = "romsFolder")]
        public string RomsFolder { get; set; }

        /// <summary>The visual theme: <see cref="DarkTheme"/> or <see cref="LightTheme"/>.</summary>
        [DataMember(Name = "theme")]
        public string ThemeName { get; set; }

        /// <summary>The code of the language ("en", "es"...). Empty means automatic: the language of Windows.</summary>
        [DataMember(Name = "language")]
        public string LanguageCode { get; set; }

        /// <summary>The filter chosen in the game list: the name of a <see cref="GameFilterMode"/>. Null when never saved.</summary>
        [DataMember(Name = "filterMode")]
        public string FilterMode { get; set; }

        /// <summary>The region filter: the name of a <see cref="GameRegion"/>, or null for all regions.</summary>
        [DataMember(Name = "filterRegion")]
        public string FilterRegion { get; set; }

        /// <summary>The genre filter, or null for all genres.</summary>
        [DataMember(Name = "filterGenre")]
        public string FilterGenre { get; set; }

        [DataMember(Name = "hideAlternateVersions")]
        public bool HideAlternateVersions { get; set; }

        /// <summary>The web addresses (with placeholders) the user chose to download each kind of image from.</summary>
        [DataMember(Name = "imageSources")]
        public List<ImageSourceSetting> ImageSources { get; set; }

        /// <summary>True when the user turned off the "Getting started" window that opens when the application starts.</summary>
        [DataMember(Name = "hideSetupGuide")]
        public bool HideSetupGuide { get; set; }

        /// <summary>Left edge of the main window when it is in its normal (not maximized) state. Null when never saved.</summary>
        [DataMember(Name = "windowLeft")]
        public int? WindowLeft { get; set; }

        [DataMember(Name = "windowTop")]
        public int? WindowTop { get; set; }

        [DataMember(Name = "windowWidth")]
        public int? WindowWidth { get; set; }

        [DataMember(Name = "windowHeight")]
        public int? WindowHeight { get; set; }

        [DataMember(Name = "isWindowMaximized")]
        public bool IsWindowMaximized { get; set; }

        /// <summary>The JSON deserializer skips constructors, so missing values are completed here.</summary>
        public void EnsureDefaults()
        {
            EmulatorPath = EmulatorPath ?? string.Empty;
            RomsFolder = RomsFolder ?? string.Empty;

            if (string.IsNullOrWhiteSpace(EmulatorArguments))
            {
                EmulatorArguments = DefaultEmulatorArguments;
            }

            // Dark is the default; anything that is not explicitly "light" counts as dark.
            ThemeName = string.Equals(ThemeName, LightTheme, StringComparison.OrdinalIgnoreCase) ? LightTheme : DarkTheme;
            LanguageCode = string.IsNullOrWhiteSpace(LanguageCode) ? null : LanguageCode.Trim();
            ImageSources = ImageSources ?? new List<ImageSourceSetting>();
        }

        public AppSettings Clone()
        {
            return new AppSettings
            {
                EmulatorPath = EmulatorPath,
                EmulatorArguments = EmulatorArguments,
                RomsFolder = RomsFolder,
                ThemeName = ThemeName,
                LanguageCode = LanguageCode,
                FilterMode = FilterMode,
                FilterRegion = FilterRegion,
                FilterGenre = FilterGenre,
                HideAlternateVersions = HideAlternateVersions,
                WindowLeft = WindowLeft,
                WindowTop = WindowTop,
                WindowWidth = WindowWidth,
                WindowHeight = WindowHeight,
                IsWindowMaximized = IsWindowMaximized,
                HideSetupGuide = HideSetupGuide,
                ImageSources = ImageSources?
                    .Select(source => new ImageSourceSetting { TargetKey = source.TargetKey, UrlTemplate = source.UrlTemplate })
                    .ToList(),
            };
        }

        /// <summary>Returns the address template saved for an image type, or an empty text.</summary>
        public string GetImageSource(string targetKey)
        {
            ImageSourceSetting source = ImageSources?.FirstOrDefault(
                item => string.Equals(item.TargetKey, targetKey, StringComparison.OrdinalIgnoreCase));

            return source == null ? string.Empty : source.UrlTemplate ?? string.Empty;
        }

        /// <summary>Saves the address template of an image type.</summary>
        public void SetImageSource(string targetKey, string urlTemplate)
        {
            if (ImageSources == null)
            {
                ImageSources = new List<ImageSourceSetting>();
            }

            ImageSourceSetting source = ImageSources.FirstOrDefault(
                item => string.Equals(item.TargetKey, targetKey, StringComparison.OrdinalIgnoreCase));

            if (source == null)
            {
                ImageSources.Add(new ImageSourceSetting { TargetKey = targetKey, UrlTemplate = urlTemplate });
            }
            else
            {
                source.UrlTemplate = urlTemplate;
            }
        }
    }
}