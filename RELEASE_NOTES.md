OpenMD 1.3.1 centers Mermaid diagrams by default and refreshes the application icon on both Windows and macOS.

- Diagrams are horizontally centered in the live preview and exported HTML/PDF. Diagram labels and surrounding document text keep their own alignment.
- New black app icon with the existing white M, downward arrow, and folded document corner. Includes Windows executable/window icons and the macOS app/Dock icon.
- All eight Mermaid starter templates and offline diagram rendering remain available.

Downloads:
- **Windows x64:** extract the entire ZIP and run OpenMD.exe. .NET is included; Microsoft Edge WebView2 Runtime is required.
- **macOS Apple Silicon:** extract OpenMD.app and move it to Applications. Requires macOS 14 or later. The app is signed for local use (ad hoc), not Developer ID signed or notarized.

Validation: both application builds and the Windows check project compiled successfully. Rendering checks verified flowchart and sequence diagram centering in both platform templates, in light/dark themes at wide and narrow viewport sizes. The macOS app signature was verified. Windows UI checks were not run on the development Mac.

Both platform sources are included in the source archives below.
