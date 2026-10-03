import AppKit
import SwiftUI
import WebKit
import UniformTypeIdentifiers

@main
struct OpenMDMacApp: App {
    @StateObject private var document = MarkdownDocument()

    var body: some Scene {
        WindowGroup {
            EditorView().environmentObject(document)
                .frame(minWidth: 850, minHeight: 560)
        }
        .commands { AppCommands(document: document) }
    }
}

@MainActor
final class MarkdownDocument: ObservableObject {
    @Published var text = """
    # Welcome to OpenMD

    Write Markdown on the left and see a live preview on the right.

    ## Make it yours

    - Open and save ordinary `.md` files
    - Use the toolbar for common formatting
    - Switch between light and dark themes
    """
    @Published var fileURL: URL?
    @Published var isDark = UserDefaults.standard.bool(forKey: "OpenMD.darkMode")
    @Published var showingImporter = false
    @Published var showingExporter = false
    @Published var showingHtmlExporter = false
    @Published var error: String?

    var title: String { (fileURL?.lastPathComponent ?? "Untitled") + " — OpenMD" }
    var status: String {
        let words = text.split { $0.isWhitespace || $0.isNewline }.count
        return "\(words.formatted()) words  •  \(text.count.formatted()) characters"
    }

    func open(_ result: Result<URL, Error>) {
        do {
            let url = try result.get()
            let permitted = url.startAccessingSecurityScopedResource()
            defer { if permitted { url.stopAccessingSecurityScopedResource() } }
            text = try String(contentsOf: url, encoding: .utf8)
            fileURL = url
        } catch { self.error = "Could not open the file. \(error.localizedDescription)" }
    }

    func exportMarkdown(_ result: Result<URL, Error>) {
        do { try text.write(to: result.get(), atomically: true, encoding: .utf8) }
        catch { self.error = "Could not save the file. \(error.localizedDescription)" }
    }

    func exportHTML(_ result: Result<URL, Error>) {
        do { try PreviewHTML.document(markdown: text, dark: false).write(to: result.get(), atomically: true, encoding: .utf8) }
        catch { self.error = "Could not export HTML. \(error.localizedDescription)" }
    }

    func newDocument() { text = ""; fileURL = nil }
    func toggleTheme() { isDark.toggle(); UserDefaults.standard.set(isDark, forKey: "OpenMD.darkMode") }
}

struct EditorView: View {
    @EnvironmentObject private var document: MarkdownDocument

    var body: some View {
        VStack(spacing: 0) {
            ToolBar()
            HSplitView {
                VStack(spacing: 0) {
                    SectionLabel(title: "MARKDOWN")
                    TextEditor(text: $document.text)
                        .font(.system(.body, design: .monospaced))
                        .padding(12)
                        .background(document.isDark ? Color(red: 0.06, green: 0.09, blue: 0.16) : Color(red: 0.98, green: 0.98, blue: 0.99))
                }.frame(minWidth: 300)
                VStack(spacing: 0) {
                    SectionLabel(title: "LIVE PREVIEW")
                    MarkdownPreview(markdown: document.text, dark: document.isDark)
                }.frame(minWidth: 300)
            }
            Text(document.status).font(.caption).foregroundStyle(.secondary)
                .frame(maxWidth: .infinity, alignment: .leading).padding(.horizontal, 12).padding(.vertical, 6)
        }
        .preferredColorScheme(document.isDark ? .dark : .light)
        .fileImporter(isPresented: $document.showingImporter, allowedContentTypes: [.plainText, .text, .init(filenameExtension: "md")!], onCompletion: document.open)
        .fileExporter(isPresented: $document.showingExporter, document: TextFile(contents: document.text), contentType: .init(filenameExtension: "md")!, defaultFilename: document.fileURL?.deletingPathExtension().lastPathComponent ?? "Untitled") { document.exportMarkdown($0) }
        .fileExporter(isPresented: $document.showingHtmlExporter, document: TextFile(contents: PreviewHTML.document(markdown: document.text, dark: false)), contentType: .html, defaultFilename: "document") { document.exportHTML($0) }
        .alert("OpenMD", isPresented: .constant(document.error != nil), actions: { Button("OK") { document.error = nil } }, message: { Text(document.error ?? "") })
    }
}

struct SectionLabel: View {
    let title: String
    var body: some View { Text(title).font(.caption.weight(.semibold)).foregroundStyle(.secondary).frame(maxWidth: .infinity, alignment: .leading).padding(.horizontal, 14).padding(.vertical, 10).background(.bar) }
}

struct ToolBar: View {
    @EnvironmentObject private var document: MarkdownDocument
    private func insert(_ before: String, _ after: String = "", placeholder: String) { document.text += before + placeholder + after }
    var body: some View {
        HStack(spacing: 6) {
            Button("New") { document.newDocument() }; Button("Open") { document.showingImporter = true }; Button("Save") { document.showingExporter = true }
            Divider().frame(height: 18)
            Menu("Heading") { ForEach(1...6, id: \.self) { level in Button("Heading \(level)") { insert(String(repeating: "#", count: level) + " ", placeholder: "Heading") } } }
            Button("Bold") { insert("**", "**", placeholder: "bold text") }.keyboardShortcut("b", modifiers: .command)
            Button("Italic") { insert("*", "*", placeholder: "italic text") }.keyboardShortcut("i", modifiers: .command)
            Button("Code") { insert("`", "`", placeholder: "code") }
            Menu("Insert") {
                Button("Bullets") { insert("- ", placeholder: "item") }; Button("Numbered") { insert("1. ", placeholder: "item") }
                Button("Checklist") { insert("- [ ] ", placeholder: "task") }; Button("Quote") { insert("> ", placeholder: "quote") }
                Button("Link") { insert("[", "](https://example.com)", placeholder: "link text") }; Button("Image") { insert("![", "](https://example.com/image.png)", placeholder: "description") }
                Button("Code block") { insert("\n```\n", "\n```\n", placeholder: "code") }; Button("Table") { insert("\n", "\n", placeholder: "| Column 1 | Column 2 |\n| --- | --- |\n| Value | Value |") }
            }
            Spacer()
            Button(document.isDark ? "Light" : "Dark") { document.toggleTheme() }
        }.buttonStyle(.bordered).controlSize(.small).padding(8).background(.bar)
    }
}

struct AppCommands: Commands {
    @ObservedObject var document: MarkdownDocument
    var body: some Commands {
        CommandGroup(replacing: .newItem) {
            Button("New") { document.newDocument() }.keyboardShortcut("n")
            Button("Open…") { document.showingImporter = true }.keyboardShortcut("o")
            Button("Save As…") { document.showingExporter = true }.keyboardShortcut("s")
            Button("Export HTML…") { document.showingHtmlExporter = true }
        }
        CommandMenu("View") { Button(document.isDark ? "Use Light Theme" : "Use Dark Theme") { document.toggleTheme() } }
    }
}

struct TextFile: FileDocument {
    static var readableContentTypes: [UTType] { [.plainText] }
    var contents: String
    init(contents: String) { self.contents = contents }
    init(configuration: ReadConfiguration) throws { contents = String(data: configuration.file.regularFileContents ?? Data(), encoding: .utf8) ?? "" }
    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper { FileWrapper(regularFileWithContents: Data(contents.utf8)) }
}

struct MarkdownPreview: NSViewRepresentable {
    let markdown: String; let dark: Bool
    func makeNSView(context: Context) -> WKWebView { let view = WKWebView(); view.setValue(false, forKey: "drawsBackground"); return view }
    func updateNSView(_ webView: WKWebView, context: Context) { webView.loadHTMLString(PreviewHTML.document(markdown: markdown, dark: dark), baseURL: nil) }
}

enum PreviewHTML {
    static func document(markdown: String, dark: Bool) -> String {
        let encoded = markdown.data(using: .utf8)!.base64EncodedString()
        let page = #"""
        <!doctype html><html><head><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; img-src https: data:"><style>:root{color-scheme:__SCHEME__;--bg:__BG__;--fg:__FG__;--code:__CODE__;--border:__BORDER__}body{margin:0;padding:30px;background:var(--bg);color:var(--fg);font:16px/1.7 -apple-system,sans-serif}h1,h2,h3{line-height:1.3}h2{border-bottom:1px solid var(--border);padding-bottom:.3em}code,pre{background:var(--code);font-family:ui-monospace,monospace;border-radius:5px}code{padding:2px 5px}pre{padding:14px;overflow:auto}blockquote{border-left:4px solid #818cf8;padding-left:16px;color:#64748b}table{border-collapse:collapse}td,th{border:1px solid var(--border);padding:7px 10px}img{max-width:100%}</style></head><body><main id="content"></main><script>const s=decodeURIComponent(escape(atob('__MARKDOWN__')));let e=s.replace(/&/g,'&amp;').replace(/</g,'&lt;');e=e.replace(/^### (.*)$/gm,'<h3>$1</h3>').replace(/^## (.*)$/gm,'<h2>$1</h2>').replace(/^# (.*)$/gm,'<h1>$1</h1>').replace(/\*\*(.*?)\*\*/g,'<strong>$1</strong>').replace(/\*(.*?)\*/g,'<em>$1</em>').replace(/`([^`]+)`/g,'<code>$1</code>').replace(/!\[([^]]*)\]\((https:[^)]+)\)/g,'<img alt="$1" src="$2">').replace(/\[([^]]+)\]\((https:[^)]+)\)/g,'<a href="$2">$1</a>').replace(/^&gt; (.*)$/gm,'<blockquote>$1</blockquote>').replace(/^- \[ \] (.*)$/gm,'☐ $1').replace(/^- \[x\] (.*)$/gmi,'☑ $1').replace(/^- (.*)$/gm,'• $1').replace(/\n/g,'<br>');document.getElementById('content').innerHTML=e;</script></body></html>
        """#
        return page.replacingOccurrences(of: "__MARKDOWN__", with: encoded)
            .replacingOccurrences(of: "__SCHEME__", with: dark ? "dark" : "light")
            .replacingOccurrences(of: "__BG__", with: dark ? "#0f172a" : "#fff")
            .replacingOccurrences(of: "__FG__", with: dark ? "#e2e8f0" : "#243247")
            .replacingOccurrences(of: "__CODE__", with: dark ? "#1e293b" : "#f1f5f9")
            .replacingOccurrences(of: "__BORDER__", with: dark ? "#334155" : "#e2e8f0")
    }
}
