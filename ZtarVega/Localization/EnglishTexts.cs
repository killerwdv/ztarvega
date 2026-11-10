using System;
using System.Collections.Generic;

namespace ZtarVega.Localization
{
    /// <summary>
    /// The built-in English texts. They are the reference for every translation, and the ones shown when a
    /// translation lacks a text.
    /// </summary>
    internal static class EnglishTexts
    {
        private static readonly List<KeyValuePair<string, string>> EntryList = new List<KeyValuePair<string, string>>();
        private static readonly Dictionary<string, string> Lookup = new Dictionary<string, string>(StringComparer.Ordinal);

        static EnglishTexts()
        {
            // language
            Add("language.name", "English");
            Add("language.author", string.Empty);

            // app
            Add("app.alreadyRunning", "ZtarVega is already running.");
            Add("app.unexpectedError", "ZtarVega ran into an unexpected problem and has to close.\n\nThe technical details were saved in:\n{0}");

            // common
            Add("common.ok", "OK");
            Add("common.cancel", "Cancel");
            Add("common.browse", "Browse...");
            Add("common.choose", "Choose...");
            Add("common.clear", "Clear");
            Add("common.remove", "Remove");
            Add("common.select", "Select");

            // menu
            Add("menu.game", "&Game");
            Add("menu.game.add", "&Add Game...");
            Add("menu.game.edit", "&Edit Game...");
            Add("menu.game.setImages", "Set &Images...");
            Add("menu.game.delete", "&Delete Game...");
            Add("menu.game.toggleFavorite", "Toggle &Favorite");
            Add("menu.tools", "&Tools");
            Add("menu.tools.importDat", "&Import Catalog from DAT...");
            Add("menu.tools.importMetadata", "Import &Metadata...");
            Add("menu.tools.importImages", "Import I&mages from Folder...");
            Add("menu.tools.scanRoms", "Scan &ROMs...");
            Add("menu.tools.settings", "&Settings...");
            Add("menu.help", "&Help");
            Add("menu.help.about", "&About ZtarVega");

            // list
            Add("list.search.placeholder", "Search games...");
            Add("list.filter.all", "All games");
            Add("list.filter.available", "Available only");
            Add("list.filter.favorites", "Favorites");
            Add("list.filter.recent", "Recently played");
            Add("list.region.all", "All regions");
            Add("list.genre.all", "All genres");
            Add("list.hideAlternates", "Hide alternate versions");
            Add("list.hideAlternates.tip", "Hides the regional versions and revisions of a game that is already listed.\nThe games whose ROM you have are always shown.");
            Add("list.count", "{0:N0} game(s)");
            Add("list.column.title", "Title");
            Add("list.column.year", "Year");

            // region
            Add("region.world", "World");
            Add("region.usa", "USA");
            Add("region.japan", "Japan");
            Add("region.europe", "Europe");
            Add("region.other", "Other");

            // romtype
            Add("romtype.original", "Original");
            Add("romtype.hack", "Hack");
            Add("romtype.bootleg", "Bootleg");
            Add("romtype.translation", "Translation");
            Add("romtype.homebrew", "Homebrew");
            Add("romtype.prototype", "Prototype");
            Add("romtype.unlicensed", "Unlicensed");

            // details
            Add("details.field.year", "Year");
            Add("details.field.region", "Region");
            Add("details.field.language", "Language");
            Add("details.field.romType", "ROM type");
            Add("details.field.developer", "Developer");
            Add("details.field.players", "Players");
            Add("details.field.tags", "Tags");
            Add("details.noLogo", "No logo");
            Add("details.noScreenshots", "No screenshots available");
            Add("details.noGame", "No game selected");

            // artwork
            Add("artwork.noBoxArt", "No box art");
            Add("artwork.noCartridge", "No cartridge image");
            Add("artwork.play", "PLAY");
            Add("artwork.favorite.add", "Add to favorites");
            Add("artwork.favorite.remove", "Remove from favorites");

            // settings
            Add("settings.title", "Settings");
            Add("settings.emulator", "Emulator");
            Add("settings.arguments", "Arguments");
            Add("settings.argumentsHint", "Use {0} where the ROM path must go. Extra options (such as full screen) are described in your emulator's documentation.");
            Add("settings.romsFolder", "ROMs folder");
            Add("settings.theme", "Theme");
            Add("settings.theme.dark", "Dark");
            Add("settings.theme.light", "Light");
            Add("settings.language", "Language");
            Add("settings.restartHint", "Restart ZtarVega to apply the theme and the language completely.");
            Add("settings.error.emulatorMissing", "The emulator file does not exist.");
            Add("settings.error.argumentsPlaceholder", "The arguments must contain {0}.");
            Add("settings.error.romsFolderMissing", "The ROMs folder does not exist.");
            Add("settings.browseEmulator.title", "Select the emulator");
            Add("settings.browseEmulator.filter", "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
            Add("settings.browseRomsFolder.description", "Select the folder that contains your ROM files.");

            // edit
            Add("edit.title.add", "Add game");
            Add("edit.title.edit", "Edit game");
            Add("edit.field.title", "Title");
            Add("edit.field.baseGame", "Base game");
            Add("edit.field.romFilename", "ROM file name");
            Add("edit.baseGame.none", "(none)");
            Add("edit.error.title", "The title cannot be empty.");
            Add("edit.error.year", "The year must be a number between {0} and {1}, or empty.");
            Add("edit.error.players", "The number of players must be between 1 and {0}, or empty.");
            Add("edit.error.romFilename", "The ROM file name must be a file name only, without a folder path.");

            // images
            Add("images.title", "Images - {0}");
            Add("images.slot.boxArt", "Box art");
            Add("images.slot.cartridge", "Cartridge");
            Add("images.slot.logo", "Logo");
            Add("images.slot.screenshot", "Screenshot {0}");
            Add("images.noImage", "No image");
            Add("images.status.new", "New image (saved when you press OK)");
            Add("images.status.current", "Current image");
            Add("images.status.inherited", "Inherited from {0}");
            Add("images.status.inheritedAfterSaving", "Inherited from {0} (after saving)");
            Add("images.status.willBeRemoved", "Will be removed");
            Add("images.chooseTitle", "Select an image for: {0}");
            Add("images.chooseFilter", "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*");
            Add("images.error.invalidImage", "The selected file is not a valid image.");
            Add("images.error.saveFailed", "The images could not be saved.\n\n{0}");

            // picker
            Add("picker.title", "Choose the base game");
            Add("picker.limited", "Showing the first {0} results. Type more to narrow the search.");
            Add("picker.count", "{0} result(s)");

            // metadataOptions
            Add("metadataOptions.title", "Import metadata");
            Add("metadataOptions.intro", "{0} file(s) selected. What should happen with the games that already have data?");
            Add("metadataOptions.fillEmpty", "Fill only the empty fields (recommended)");
            Add("metadataOptions.fillEmptyHint", "The values you entered or imported before are kept.");
            Add("metadataOptions.replace", "Replace the values found in the files");
            Add("metadataOptions.replaceHint", "Overwrites the year, developer and tags of every game the files know about, including values you entered by hand. The tags are replaced by the genre.");

            // imageFolder
            Add("imageFolder.title", "Import images from a folder");
            Add("imageFolder.folder", "Folder");
            Add("imageFolder.imageType", "Image type");
            Add("imageFolder.intro", "Each image is matched to a game by its file name, and a report lists the files that could not be matched. Tip: use \"{0}\" for in-game screenshots and \"{1}\" for title screens.");
            Add("imageFolder.fillEmpty", "Fill only the games that have no image of this type (recommended)");
            Add("imageFolder.fillEmptyHint", "The images you chose before are kept.");
            Add("imageFolder.replace", "Replace the images of the games found in the folder");
            Add("imageFolder.replaceHint", "Overwrites the existing image of this type, including the ones you chose by hand.");
            Add("imageFolder.includeModified", "Include images of hacks and translations");
            Add("imageFolder.includeModifiedHint", "They are assigned to the hacks and translations in your catalog that have the same title. If you have none, they are listed in the report.");
            Add("imageFolder.error.folderMissing", "The folder does not exist.");
            Add("imageFolder.browseDescription", "Select the folder that contains the images.");

            // progress
            Add("progress.preparing", "Preparing...");
            Add("progress.cancelling", "Cancelling...");
            Add("progress.scan.title", "Scanning ROMs");
            Add("progress.scan.format", "Checking {0} of {1}: {2}");
            Add("progress.images.title", "Importing images");
            Add("progress.images.format", "Importing {0} of {1}: {2}");

            // main
            Add("main.restart.question", "The new theme or language is applied completely after restarting ZtarVega.\n\nDo you want to restart now?");
            Add("main.restart.failed", "ZtarVega could not be restarted. Please open it again manually.");
            Add("main.askSettings", "{0}\n\nDo you want to open the settings now?");
            Add("main.settingsSaveWarning", "The settings could not be saved and will only apply until the application is closed.\n\n{0}");
            Add("main.favorite.saveFailed", "The favorite could not be saved.\n\n{0}");
            Add("main.game.addFailed", "The game could not be added.");
            Add("main.game.saveFailed", "The changes could not be saved.");
            Add("main.game.deleteFailed", "The game could not be deleted.");
            Add("main.game.deleteConfirm", "Delete \"{0}\" from the catalog?\n\nYour ROM file and images are not deleted.");

            // launch
            Add("launch.error.emulatorNotConfigured", "The emulator is not configured, or its file no longer exists.");
            Add("launch.error.romsFolderNotConfigured", "The ROMs folder is not configured, or it no longer exists.");
            Add("launch.error.noRomFilename", "This game has no ROM file name registered.");
            Add("launch.error.romFileNotFound", "The file \"{0}\" was not found in the ROMs folder.");
            Add("launch.error.startFailed", "The emulator could not be started: {0}");
            Add("launch.error.romNotInArchive", "The ROM was not found inside \"{0}\". Scan your ROMs again.");
            Add("launch.error.zipUnreadable", "The ZIP file could not be read: {0}");
            Add("launch.error.extractFailed", "The ROM could not be extracted: {0}");

            // scan
            Add("scan.cancelled", "The scan was cancelled. No changes were made.");
            Add("scan.error", "The scan could not be completed. No changes were made.\n\n{0}");
            Add("scan.error.folderMissing", "The ROMs folder does not exist: {0}");
            Add("scan.summary.finished", "Scan finished.");
            Add("scan.summary.filesFound", "ROM files found: {0}");
            Add("scan.summary.available", "Games available: {0}");
            Add("scan.summary.newlyAvailable", "Newly available: {0}");
            Add("scan.summary.noLongerAvailable", "No longer available: {0}");
            Add("scan.summary.unmatched", "Files that match no game: {0}");
            Add("scan.summary.duplicates", "Duplicate files ignored: {0}");
            Add("scan.summary.unreadable", "Unreadable files: {0}");
            Add("scan.addUnmatched.question", "Do you want to add the {0} unmatched file(s) to the catalog?");
            Add("scan.import.failed", "The games could not be added. No changes were made.\n\n{0}");
            Add("scan.import.skipped", "{0} file(s) skipped because they duplicate an existing game.");
            Add("scan.import.linked", "{0} linked to a base game automatically (you can change it with Game > Edit Game).");

            // import
            Add("import.gamesAdded", "{0} game(s) added to the catalog.");
            Add("import.alreadyInCatalog", "{0} already in the catalog.");
            Add("import.ignored", "{0} entries ignored (BIOS files, add-ons and other non-game entries).");
            Add("import.linked", "{0} version(s) linked to the main version of their game (they share its images and data).");
            Add("import.scanHint", "Set your ROMs folder in Tools > Settings and use Tools > Scan ROMs to see which games you have.");
            Add("import.scanQuestion", "Do you want to scan your ROMs folder now to see which games you have?");

            // dat
            Add("dat.dialogTitle", "Select a DAT file");
            Add("dat.filter", "DAT files (*.dat;*.xml)|*.dat;*.xml|All files (*.*)|*.*");
            Add("dat.error", "The catalog could not be imported. No changes were made.\n\n{0}");
            Add("dat.error.notDatFile", "The file is not a DAT file.");
            Add("dat.error.notValid", "The file is not a valid DAT file.");
            Add("dat.error.noGames", "No games with a checksum were found in the DAT file.");

            // metadata
            Add("metadata.dialogTitle", "Select the metadata files (select all of them at once)");
            Add("metadata.filter", "Metadata files (*.dat)|*.dat|All files (*.*)|*.*");
            Add("metadata.error", "The metadata could not be imported. No changes were made.\n\n{0}");
            Add("metadata.error.endedUnexpectedly", "The file ended unexpectedly.");
            Add("metadata.error.notValid", "The file is not a valid metadata file.");
            Add("metadata.error.noGames", "No games were found in the file \"{0}\".");
            Add("metadata.summary.entries", "Entries read from the files: {0}");
            Add("metadata.summary.matched", "Games with matching data: {0} of {1}");
            Add("metadata.summary.inherited", "Games completed from their base game: {0}");
            Add("metadata.summary.updated", "Games updated: {0}");
            Add("metadata.summary.replaced", "The existing values were replaced with the ones found in the files.");
            Add("metadata.summary.filled", "Only the empty fields were filled; the data you entered yourself was kept.");

            // imageImport
            Add("imageImport.cancelled", "The import was cancelled. The images saved so far were kept.");
            Add("imageImport.finished", "The import finished.");
            Add("imageImport.type", "Image type: {0}");
            Add("imageImport.filesFound", "Files found: {0}");
            Add("imageImport.saved", "Images saved: {0}");
            Add("imageImport.alreadyHad", "Games that already had an image (kept): {0}");
            Add("imageImport.unmatched", "Not matched to any game: {0}");
            Add("imageImport.ambiguous", "Ambiguous (several games possible): {0}");
            Add("imageImport.skippedModified", "Skipped (hacks and translations): {0}");
            Add("imageImport.duplicates", "Duplicates skipped: {0}");
            Add("imageImport.failed", "Failed: {0}");
            Add("imageImport.reportQuestion", "A detailed report was saved. Do you want to open it?");
            Add("imageImport.reportOpenFailed", "The report could not be opened. It is saved at:\n{0}");
            Add("imageImport.error", "The images could not be imported.\n\n{0}");
            Add("imageImport.error.invalidImage", "The file \"{0}\" is not a valid image.");
            Add("imageImport.error.folderNotFound", "The folder does not exist: {0}");

            // about
            Add("about.title", "About {0}");
            Add("about.description", "An open-source, lightweight frontend for your own game collection.\n\nZtarVega does not include, distribute or encourage the download of ROMs or other copyrighted material, and it does not include or recommend any source of ROMs, game lists or images.\n\nNES is a trademark of Nintendo. ZtarVega is not affiliated with or endorsed by Nintendo.");

            // zip
            Add("zip.error.entryTooLarge", "A file inside the archive is larger than a ROM can be.");

            // download
            Add("menu.tools.downloadImages", "Do&wnload Images...");
            Add("download.title", "Download images");
            Add("download.intro", "ZtarVega downloads the images from a website that you choose. It does not include or recommend any source: use one that you are allowed to use and respect its terms of use.");
            Add("download.imageType", "Image type");
            Add("download.address", "Address");
            Add("download.addressHint", "Write the web address of the images and use these words where the name of the game goes: {0} (the name of the game as it appears in the catalog), {1} (the same name with the characters that file names cannot have replaced by _) and {2} (the checksum of the game). Example: {3}");
            Add("download.scope.available", "Only the games I have (recommended)");
            Add("download.scope.all", "All the games of the catalog (it can take a long time)");
            Add("download.scope.selected", "Only the selected game");
            Add("download.mode.fillEmpty", "Download only the images that the games do not have yet (recommended)");
            Add("download.mode.replace", "Replace the images that the games already have");
            Add("download.test", "Test with a game");
            Add("download.test.testing", "Testing...");
            Add("download.test.noGame", "There is no game to test with.");
            Add("download.test.ok", "The image was found ({0} KB).");
            Add("download.test.notFound", "Not found: the website answered that this image does not exist.");
            Add("download.test.notAnImage", "The website answered, but the file is not an image.");
            Add("download.test.tooLarge", "The file is too large to be an image.");
            Add("download.test.networkError", "The connection failed: {0}");
            Add("download.test.invalidUrl", "The address is not valid.");
            Add("download.test.noChecksum", "This game has no checksum, and the address needs one.");
            Add("download.test.address", "Address tried:");
            Add("download.start", "Download");
            Add("download.error.templateEmpty", "Write the address of the images.");
            Add("download.error.templateScheme", "The address must start with http:// or https://.");
            Add("download.error.templatePlaceholder", "The address must contain {0}, {1} or {2}, so that every game has its own address.");
            Add("download.noGames", "There are no games in that selection. Scan your ROMs first or choose another option.");
            Add("download.confirmMany", "Up to {0} images will be requested from the website, one by one. It can take a long time and you can cancel at any moment. Do you want to continue?");
            Add("download.summary.cancelled", "The download was cancelled. The images saved so far were kept.");
            Add("download.summary.finished", "The download finished.");
            Add("download.summary.aborted", "The download stopped because the connection failed several times in a row. Check your internet connection and the address.");
            Add("download.summary.type", "Image type: {0}");
            Add("download.summary.games", "Games in the selection: {0}");
            Add("download.summary.saved", "Images downloaded: {0}");
            Add("download.summary.alreadyHad", "Games that already had the image (kept): {0}");
            Add("download.summary.notFound", "Not found on the website: {0}");
            Add("download.summary.notAnImage", "Answers that were not images: {0}");
            Add("download.summary.noChecksum", "Games without a checksum (skipped): {0}");
            Add("download.summary.failed", "Failed: {0}");
            Add("download.summary.exampleNotFound", "Example of an address that was not found:\n{0}");
            Add("download.summary.reportQuestion", "A detailed report was saved. Do you want to open it?");
            Add("download.error", "The images could not be downloaded.\n\n{0}");
            Add("progress.download.title", "Downloading images");
            Add("progress.download.format", "Downloading {0} of {1}: {2}");

            // guide
            Add("menu.help.guide", "&Getting started...");
            Add("guide.title", "Getting started");
            Add("guide.intro", "Follow these steps in order. A step turns on when the steps before it are done, and the next one you have to do is marked with an arrow. You can close this window at any time and open it again from Help > Getting started.");
            Add("guide.step.settings.title", "1. Choose your emulator and your ROMs folder");
            Add("guide.step.settings.text", "ZtarVega does not run games by itself: it starts the emulator that you choose. Choose the emulator program (for example FCEUX or Mesen) and the folder where your ROM files are.");
            Add("guide.step.settings.button", "Open the settings");
            Add("guide.step.catalog.title", "2. Load the catalog of games");
            Add("guide.step.catalog.text", "The catalog is the list of all the games that exist, so you can see them even if you do not have them. It comes from a DAT file: a game list published by ROM preservation projects (for example, the headerless NES DAT of the No-Intro project). ZtarVega does not include it: download it yourself and check its terms of use.");
            Add("guide.step.catalog.button", "Choose the DAT file...");
            Add("guide.step.scan.title", "3. Find out which games you have");
            Add("guide.step.scan.text", "ZtarVega looks inside your ROMs folder and marks the games you have. It recognizes them by their content, so it does not matter how the files are named, and it also reads ZIP files.");
            Add("guide.step.scan.button", "Scan my ROMs");
            Add("guide.step.metadata.title", "4. Complete the information of the games (optional)");
            Add("guide.step.metadata.text", "Adds the year, the developer and the genre to the games, using metadata files that you download yourself (select all of them at once).");
            Add("guide.step.metadata.button", "Choose the metadata files...");
            Add("guide.step.images.title", "5. Get the images of the games (optional)");
            Add("guide.step.images.text", "Box art, cartridges, logos and screenshots. You can import them from a folder that you already have, or download them from a website that you choose.");
            Add("guide.step.images.importButton", "Import from a folder...");
            Add("guide.step.images.downloadButton", "Download from a website...");
            Add("guide.requires", "First do step {0}.");
            Add("guide.ready", "Everything is ready: select a game in the list and press PLAY. You can do the optional steps whenever you want.");
            Add("guide.showAtStartup", "Show this window when ZtarVega starts while there are steps left");
            Add("guide.close", "Close");
        }

        /// <summary>Every text in the order it was declared, which is the order of the template file.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Entries
        {
            get { return EntryList; }
        }

        /// <summary>Returns the English text of a key, or null when the key does not exist.</summary>
        public static string Find(string key)
        {
            string text;
            return Lookup.TryGetValue(key, out text) ? text : null;
        }

        private static void Add(string key, string text)
        {
            EntryList.Add(new KeyValuePair<string, string>(key, text));
            Lookup.Add(key, text);
        }
    }
}