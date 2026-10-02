# OpenMD

A simple Windows Markdown editor and viewer built with .NET 10 LTS and Windows Forms.

## Download

Download the Windows ZIP from [GitHub Releases](https://github.com/Supersampled-Labs/OpenMD/releases), extract all files to a folder, and run OpenMD.exe. The .NET runtime is included. Microsoft Edge WebView2 Runtime is still required.

## Run

Install the .NET 10 SDK and Microsoft Edge WebView2 Runtime, then run:

```powershell
dotnet run --project OpenMD.csproj
```

Or open OpenMD.csproj in Visual Studio with .NET desktop development support.

## Use

- Type on the left; the preview updates on the right.
- Choose View → Theme → Light or Dark. The editor, menus, toolbar, title bar and live preview follow the selected theme. Your choice is remembered between launches.
- The executable and window use the custom OpenMD application icon.
- Select text and click Bold, Italic, Strike, or Code. With no selection, a placeholder is inserted and selected.
- Choose Heading → Heading 1–6 to format the current line or selected lines. Existing heading markers are replaced. Normal text removes them.
- Use Bullets, Numbered, Checklist, Quote, Link, Image, Code block, Table, and Rule.
- Edit the URL placeholders inserted by Link and Image. HTTPS images render in the preview; local image files are not supported.
- Ctrl+N: new; Ctrl+O: open; Ctrl+S: save; Ctrl+Shift+S: save as.
- Ctrl+B / Ctrl+I: bold / italic; Ctrl+Z / Ctrl+Y: undo / redo.
- File → Export HTML saves a standalone styled HTML document.
- Unsaved changes prompt before opening, creating a new document, or closing.
- Pass a Markdown file path on the command line to open it.

Raw HTML is disabled in Markdown. Preview links are displayed without navigating away from the document.

## Build a portable Windows executable

```powershell
dotnet publish OpenMD.csproj -c Release -r win-x64 --self-contained true -o publish
```

Run publish/OpenMD.exe. Keep all published files together. The self-contained build includes .NET; the WebView2 Runtime must still be installed.

## Formatting checks

```powershell
dotnet run --project Checks/OpenMD.Checks.csproj
```



