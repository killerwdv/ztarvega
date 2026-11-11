# ZtarVega

A lightweight frontend / launcher for your own NES game collection on Windows, written in C# (Windows Forms).

> ZtarVega is an open-source game manager and frontend. It does not include,
> distribute or encourage the download of ROMs or other copyrighted material,
> and it does not include or recommend any source of ROMs or images.

NES is a trademark of Nintendo. This project is not affiliated with or endorsed by Nintendo.

## Features

- **Complete catalog.** Import a DAT file (the game lists published by ROM preservation projects) and see every known game, with its regional versions linked together. You do not have to own a game to read its information.
- **ROM scanner.** Point it to your ROMs folder and it marks the games you have. ROMs are recognized by their CRC32 checksum, so renamed files and files inside ZIP archives work too.
- **Metadata.** Fill in the year, developer and genre from metadata files.
- **Images.** Box art, cartridge, logo and up to five screenshots per game. Import them from a folder or choose them by hand. Hacks and translations inherit the images of their base game and can have their own.
- **Search and filters.** By title, region, genre, favorites, recently played and availability. The filters are remembered.
- **Any emulator.** You choose the emulator and its command-line arguments.
- **Dark and light theme**, and a **language system**: English and Spanish are included, and anyone can add a language with a text file.
- **Lightweight and portable.** A virtual game list that stays fast with thousands of entries, images loaded on demand with a fixed memory budget, and no installer: everything lives in one folder.
- **Image downloader.** Download the images of your games from a website that you choose: you write the web address once, using placeholders for the name of the game, and ZtarVega does the rest. It does not include or recommend any source.

## Requirements

- Windows 10 or 11 (tested). Windows 7 SP1 or later should work, but it has not been verified yet.
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) (already included in current versions of Windows 10 and 11).
- An emulator of your choice, such as FCEUX or Mesen.

## Third-party software

ZtarVega includes [System.Data.SQLite](https://system.data.sqlite.org/) and the SQLite library to store the catalog. Both are in the public domain.

## Getting started

1. Download the latest ZIP from the Releases page. If Windows warns you about the file, right-click the ZIP, choose **Properties** and tick **Unblock** *before* extracting it.
2. Extract it anywhere and run `ZtarVega.exe`.
3. The **Getting started** window opens by itself and guides you through the rest in the right order. You can open it again from **Help > Getting started** (F1). These are its steps:
   1. **Tools > Settings**: choose your emulator, its arguments (use `{rom}` where the ROM path goes) and the folder that contains your ROM files.
   2. **Tools > Import Catalog from DAT**: build your catalog with a DAT file that you downloaded (for example, the headerless NES DAT of the No-Intro project; check its terms of use).
   3. **Tools > Scan ROMs**: detect which games you have.
   4. Optional: **Tools > Import Metadata**, **Tools > Import Images from Folder** or **Tools > Download Images** to complete the information of the games.

You must provide your own game files, DAT files and images: ZtarVega does not include any of them.

## Where the data is stored

Everything is kept next to `ZtarVega.exe`:

| File or folder | Content |
|---|---|
| `ztarvega.db` | The catalog (a SQLite database) |
| `config.json` | Your settings |
| `assets/` | Your images |
| `languages/` | The language files |
| `logs/` | Technical logs |

ROMs taken out of ZIP archives to start a game are stored temporarily in `%TEMP%\ZtarVega` and removed when they have not been used for a week.

To move ZtarVega to another computer, copy its folder.

## Images

Images are named after the CRC32 checksum of the game's ROM, which identifies the game on every installation:

| Folder | File name |
|---|---|
| `assets/boxart` and `assets/cartridges` | `{CRC32}.jpg` |
| `assets/logos` | `{CRC32}.png` |
| `assets/screenshots` | `{CRC32}_1.png` ... `{CRC32}_5.png` |

Games without a checksum use `id-{Id}` instead. You do not need to name files by hand: use **Game > Set Images** or **Tools > Import Images from Folder**.

## Downloading images from a website

Use **Tools > Download Images** and write the address of the images, with these placeholders where the game goes:

| Placeholder | Replaced by |
|---|---|
| `{title}` | The name of the game as it appears in the catalog |
| `{filetitle}` | The same name, with the characters that file names cannot have replaced by `_` (listed below) |
| `{crc32}` | The CRC32 checksum of the game's ROM |

The characters replaced by `_` in `{filetitle}` are: `&` `*` `/` `:` `<` `>` `?` `\` `|` `"` and the backtick (`` ` ``).

For example: `https://example.com/images/{filetitle}.png`. Use the **Test with a game** button to check the address before downloading.

ZtarVega does not include or recommend any image source. Images are usually protected by copyright: use a source you are allowed to use and respect its terms of use. Requests are made one by one, with a short pause, and the download can be cancelled at any time.

## Adding a language

1. Start ZtarVega once. It writes `languages/_template_en.txt`, a file with every text of the program and instructions.
2. Copy it as `languages/xx.lang`, where `xx` is the language code (for example `fr.lang`).
3. Translate the text after each `=`, and fill in `language.name` and `language.author`. Texts you leave empty are shown in English.
4. Save the file as UTF-8, restart ZtarVega and choose the language in **Tools > Settings**.

## Troubleshooting

- **Something went wrong.** Technical details are saved in `logs/ztarvega.log`.
- **The program starts slowly on an old computer.** Start it with `ZtarVega.exe --profile`: it writes `logs/startup-timings.txt` with how long each step of the startup takes.
- **An error about SQLite at startup.** Install the Microsoft Visual C++ Redistributable 2015-2022 (x86 or x64, according to your Windows).
- **Windows says it protected your PC (SmartScreen).** ZtarVega is not digitally signed. Choose **More info > Run anyway**, or right-click the downloaded ZIP, open **Properties** and tick **Unblock** before extracting it. You can check the file against the SHA-256 shown in the release notes.

## Building from source

1. Install Visual Studio with the ".NET desktop development" workload.
2. Open the solution, restore the NuGet packages and build.

Debug builds include a generator of fictional sample games and images, so the interface can be tested without any real data. To create the distribution package, build the Release configuration and run `tools/Package-Release.ps1`.

## Support

ZtarVega cannot help you find ROMs, DAT files or images, and requests for them will be closed. Questions about using the program, bug reports and translations are welcome. To share a translation, send your `.lang` file in an issue or a pull request.

## License

Released under the MIT License. See [LICENSE](LICENSE.txt).