# macOS build

This repository now includes a macOS `.app` packaging script and GitHub Actions workflow.

## Easiest: GitHub Actions

1. Push this repository to GitHub.
2. Open **Actions** → **Build macOS app** → **Run workflow**.
3. Download the artifact for your Mac:
   - `MIDIProgramSplitter-osx-arm64` for Apple Silicon (M1/M2/M3/M4/etc.)
   - `MIDIProgramSplitter-osx-x64` for Intel Mac
4. Unzip it and move `MIDIProgramSplitter.app` to Applications.

The app is self-contained, so the target Mac does not need .NET installed.

## Build locally on a Mac

Install .NET 7 SDK, then run:

```bash
./scripts/build-mac-app.sh osx-arm64
```

or for Intel:

```bash
./scripts/build-mac-app.sh osx-x64
```

The finished zip is written under `artifacts/`.

## Gatekeeper note

This build is ad-hoc signed, not Apple Developer ID notarized. If macOS says the app cannot be opened after downloading the ZIP, Control-click the app and choose **Open**. If quarantine still blocks it, run:

```bash
xattr -dr com.apple.quarantine /Applications/MIDIProgramSplitter.app
```
