# AGENTS.md — ScreenWriter

Guide for AI coding agents (Claude Code, Copilot, Cursor, Codex, …) working in this repo.
`CLAUDE.md` imports this file, so this is the single source of truth: edit here.

## What this is
**Screen Writer**: a Windows desktop app (C# · WPF · .NET 10) that puts a transparent drawing
overlay on top of the screen, so you can annotate during screen sharing. The UI is bilingual
(Arabic RTL and English) and switches language instantly.
It ships **only through the Microsoft Store** as an MSIX package. There are no GitHub Releases.

The owner writes in **Arabic**. Reply to them in Arabic unless they write in English.

## Commands
```bash
dotnet build ScreenWriter/ScreenWriter.csproj                                   # build
dotnet run --project ScreenWriter/ScreenWriter.csproj                           # run (dev)
dotnet publish ScreenWriter/ScreenWriter.csproj -p:PublishProfile=FolderProfile # → publish/ (Release, win-x64, self-contained)
```
- There are no automated tests. Verify a change by building it, and by running the app for UI work.
- The app allows only one instance (a global mutex). If the Store copy is already running, `dotnet run`
  shows "already running" and exits. Ask the user to quit the running copy from the tray first.

## Layout
```
ScreenWriter/                 WPF project (App.xaml.cs wires everything together)
  Windows/                    Overlay (InkCanvas), Toolbar, About, UpdateDialog, Splash
  Services/                   Hotkey, Tray, Localization, Settings, Update
  Resources/Strings.{ar,en}.xaml   all UI strings
  Assets/                     icon.ico + toolbar PNG icons (each listed as <Resource> in the csproj)
landing/                      static website (GitHub Pages), ar/ + en/ + root redirect
installer/ScreenWriter.iss    legacy Inno Setup script (not used now; see below)
msix/                         local MSIX packaging workspace, git-ignored
```

## Conventions
- **Strings:** never hard-code UI text. Add every key to **both** `Strings.ar.xaml` and `Strings.en.xaml`.
  In XAML, use `{DynamicResource Str_X}`. In code, use `LocalizationService.Instance.Get("Str_X")`.
  Escape `&` as `&amp;`.
- **RTL:** windows set `FlowDirection` from `LocalizationService.Instance.FlowDirection` and update it on
  `LanguageChanged`. Check new layouts in Arabic as well, because Arabic text is longer.
- **Style:** the windows use a dark theme (`#111827` background, `#06D6A0` teal accent, rounded borders).
  Reuse the existing styles in `Window.Resources` instead of copying templates inline.
- **Popups are Topmost.** If an action opens an external window (the Store, a browser), close the popup,
  or it may cover that window.
- **Settings:** stored in `%APPDATA%\ScreenWriter\settings.txt` and `language.txt`. Don't write to them in tools or tests.
- **Code comments:** short comments in Arabic or English are both fine.
- **New icons:** add the file under `Assets/Icons/` and add a `<Resource Include=…>` line to the csproj.

## Microsoft Store
- **Store ID:** `9NF25DZQXLCT`.
- **Listing:** https://apps.microsoft.com/detail/9nf25dzqxlct
- **Opening the Store from the app:** `UpdateService.OpenStoreListing()` opens the product page and
  `UpdateService.OpenStoreReview()` opens the review form.
- **Package identity:** these values must match exactly, or the Store rejects the package:
  - `Name="NasserAlmadi.ScreenWriteApp"`
  - `Publisher="CN=9260B530-9E02-440E-8E99-B74323B1CE93"`
  - `DisplayName`: `ScreenWrite App`
  - `PublisherDisplayName`: `Nasser Almadi`
- **Package Family Name:** `NasserAlmadi.ScreenWriteApp_fryfff081qvec`.

## Release steps (new Store version)
1. **Bump the version** to X.Y.Z in `ScreenWriter/ScreenWriter.csproj` (`Version`, `FileVersion` and
   `AssemblyVersion`). The About window reads the assembly version.
2. **Update the changelog** in `README.md`. It appears twice: once in the Arabic section and once in the
   English section.
3. **Publish:** `dotnet publish … -p:PublishProfile=FolderProfile`.
4. **Copy into the package folder:** copy `publish/*` over `msix/PackageLayout/`. Keep `AppxManifest.xml`
   and `Assets/` (the Store logos). Delete files that are no longer in `publish/`, for example an old
   `mscordaccore_amd64_amd64_<ver>.dll` left over after a .NET runtime update.
5. **Set the package version:** `Version="X.Y.Z.0"` in `msix/PackageLayout/AppxManifest.xml`. Check the
   identity values listed above.
6. **Build the package:** `msix/SDK/makeappx.exe pack /d msix/PackageLayout /p msix/ScreenWriter_X.Y.Z.0_x64.msix /o`
   (in Git Bash, prefix the command with `MSYS_NO_PATHCONV=1`). The package does not need signing,
   because the Store signs it.
7. **Upload:** the owner uploads the package to Partner Center by hand. Give them "What's new" text in
   both Arabic and English.
8. **Commit and tag** `vX.Y.Z` on `main` (lightweight tag), then push, but only when the owner asks.

The `msix/` folder exists only on the owner's machine. It holds the SDK tools, `PackageLayout/` and
`store-screenshots/`.

## Website (landing/)
- **Hosting:** GitHub Pages at https://nasmn77.github.io/ScreenWriter/. A push to `main` that changes
  `landing/**` deploys the site through `.github/workflows/pages.yml`.
- **Two pages:** `landing/ar/index.html` and `landing/en/index.html` are separate full pages. Apply every
  change to both.
- **Static only:** there is no backend.
- **Old address:** the old host `docs.naseralmadi.cloud/apps/` is no longer used.

## Legacy / gotchas
- **No installer anymore:** the GitHub Releases and tags were deleted on purpose (Nov 2026). Distribution
  is Store-only. `installer/` and the GitHub-release check in `UpdateService.CheckAsync` remain from the
  old installer build. In the Store build, `IsPackagedApp` is true and that check is skipped.
- **Unpackaged runs:** a dev run (`dotnet run`) is not packaged, so "Check for updates" queries GitHub,
  finds no release and shows "couldn't check". This is expected.
- **Store version first:** don't bring back GitHub Releases or an installer download link unless the
  owner asks.

## Git
- **Branch:** commit directly on `main`, which is the owner's workflow.
- **Commit messages:** in English, with a short summary line. Release commits look like
  `vX.Y.Z — <summary>`.
- **When:** commit or push only when the owner asks.
- **Never commit** `bin/`, `obj/`, `publish/` or `msix/`. They are already in `.gitignore`.
