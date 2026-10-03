# OpenMD

Platform-specific applications are kept separate: [`windows/`](windows/) contains the Windows Forms project and tests, while [`macOS/`](macOS/) contains the native SwiftUI app. Shared branding assets remain in [`Assets/`](Assets/). Generated build output is ignored by Git.

A cross-platform Markdown editor for Windows and macOS with live preview, formatting tools, and light/dark themes.

## macOS

The repository also includes a native macOS version built with SwiftUI and WKWebView. It has the same split Markdown editor, live preview, file import/save, HTML export, toolbar formatting, and saved light/dark preference. It requires macOS 14 or later.

Build a signed-for-local-use app bundle on a Mac with Xcode Command Line Tools:

```zsh
chmod +x macOS/scripts/build-macos-app.sh
macOS/scripts/build-macos-app.sh
open macOS/dist/OpenMD.app
```

The macOS-specific Swift package, source, script, intermediate build products, and resulting application all live in `macOS/`. The resulting application is `macOS/dist/OpenMD.app`. Distribute it only after signing with your Apple Developer certificate and notarizing it.

## Download

Download the Windows ZIP from [GitHub Releases](https://github.com/Supersampled-Labs/OpenMD/releases), extract all files to a folder, and run OpenMD.exe. The .NET runtime is included. Microsoft Edge WebView2 Runtime is still required.

## Run

Install the .NET 10 SDK and Microsoft Edge WebView2 Runtime, then run:

```powershell
dotnet run --project windows/OpenMD.csproj
```

Or open windows/OpenMD.csproj in Visual Studio with .NET desktop development support.

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
dotnet publish windows/OpenMD.csproj -c Release -r win-x64 --self-contained true -o windows/publish
```

Run windows/publish/OpenMD.exe. Keep all published files together. The self-contained build includes .NET; the WebView2 Runtime must still be installed.

## Formatting checks

```powershell
dotnet run --project windows/Checks/OpenMD.Checks.csproj
```




## Clipboard copy and paste

The clipboard toolbar is at the top of the window; the same commands are under Clipboard.

| Button | Action |
| --- | --- |
| Copy Formatted | Copy rendered HTML for Word, OneNote, Outlook, and other rich-text editors. Paste with Ctrl+V and select **Keep Source Formatting** if the target asks. |
| Copy HTML | Copy a standalone HTML document as text for HTML/code editors; rich HTML is also available on the clipboard. |
| Copy MD | Copy the original Markdown syntax as plain text. |
| Paste MD | Insert clipboard Markdown/plain text without interpreting it as rich text. |
| Paste Formatted → MD | Convert clipboard HTML to Markdown, preserving supported headings, emphasis, links, lists, and tables. If no HTML is available, paste plain text. |

Copy commands use the selected source, or the whole document when nothing is selected. Paste commands insert at the cursor or replace the selection, and support Undo.

Shortcuts: Ctrl+Shift+C copies formatted content; Ctrl+Shift+V pastes Markdown; Ctrl+Alt+V pastes formatted content as Markdown. Normal Ctrl+C/Ctrl+V continue to work in the source editor.

The formatted clipboard uses an Office-friendly light palette even when the app is in dark mode. Word HTML-paste tests verify heading size, bold, italic, tables and links. OneNote receives the same HTML format; its exact result depends on its paste settings. Markdown cannot represent every Office feature: font colors, page layouts and complex merged tables may not survive conversion. Remote images are referenced by URL rather than embedded. When rich clipboard content has no HTML, plain text is used.

Content copied from OpenMD also carries its original Markdown alongside the rendered clipboard formats so pasting back into OpenMD preserves the exact source.

To include a real Word paste test (requires installed Microsoft Word):

```powershell
$env:OPENMD_CHECK_WORD = "1"
dotnet run --project windows/Checks/OpenMD.Checks.csproj
```
