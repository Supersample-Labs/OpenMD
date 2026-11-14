import SwiftUI
import AppKit

struct MermaidTemplate {
    let name: String
    let source: String
    static let all: [MermaidTemplate] = [
        .init(name: "Flowchart", source: "flowchart TD\n    A[Start] --> B{Decision}\n    B -->|Yes| C[Continue]\n    B -->|No| D[Stop]"),
        .init(name: "Sequence diagram", source: "sequenceDiagram\n    participant User\n    participant App\n    User->>App: Request\n    App-->>User: Response"),
        .init(name: "Class diagram", source: "classDiagram\n    class Animal {\n        +String name\n        +speak()\n    }\n    Animal <|-- Dog"),
        .init(name: "State diagram", source: "stateDiagram-v2\n    [*] --> Idle\n    Idle --> Working: Start\n    Working --> Idle: Finish\n    Idle --> [*]"),
        .init(name: "Entity relationship diagram", source: "erDiagram\n    CUSTOMER ||--o{ ORDER : places\n    CUSTOMER {\n        int id PK\n        string name\n    }\n    ORDER {\n        int id PK\n        int customer_id FK\n    }"),
        .init(name: "Gantt chart", source: "gantt\n    title Project plan\n    dateFormat YYYY-MM-DD\n    section Work\n    Planning :a1, 2026-01-01, 3d\n    Building :a2, after a1, 5d"),
        .init(name: "Pie chart", source: "pie title Example breakdown\n    \"Category A\" : 60\n    \"Category B\" : 40"),
        .init(name: "Mind map", source: "mindmap\n  root((Main idea))\n    Topic A\n      Detail\n    Topic B"),
    ]
}

struct MermaidOptions: View {
    @ObservedObject var document: MarkdownDocument
    var body: some View {
        ForEach(MermaidTemplate.all, id: \.name) { template in
            Button(template.name) { document.insertMermaid(template.source) }
        }
    }
}

struct SourceEditor: NSViewRepresentable {
    @ObservedObject var document: MarkdownDocument
    func makeCoordinator() -> Coordinator { Coordinator(document) }
    func makeNSView(context: Context) -> NSScrollView {
        let scroll = NSTextView.scrollableTextView()
        let editor = scroll.documentView as! NSTextView
        editor.isRichText = false
        editor.allowsUndo = true
        editor.isAutomaticQuoteSubstitutionEnabled = false
        editor.isAutomaticDashSubstitutionEnabled = false
        editor.font = .monospacedSystemFont(ofSize: NSFont.systemFontSize, weight: .regular)
        editor.textContainerInset = NSSize(width: 8, height: 8)
        editor.delegate = context.coordinator
        editor.string = document.text
        document.sourceEditor = editor
        return scroll
    }
    func updateNSView(_ scroll: NSScrollView, context: Context) {
        guard let editor = scroll.documentView as? NSTextView else { return }
        if editor.string != document.text {
            editor.string = document.text
            editor.undoManager?.removeAllActions()
        }
        editor.backgroundColor = document.isDark ? NSColor(calibratedRed: 0.06, green: 0.09, blue: 0.16, alpha: 1) : NSColor.textBackgroundColor
        editor.textColor = document.isDark ? .white : .black
        editor.insertionPointColor = editor.textColor ?? .textColor
    }
    @MainActor final class Coordinator: NSObject, NSTextViewDelegate {
        let document: MarkdownDocument
        init(_ document: MarkdownDocument) { self.document = document }
        func textDidChange(_ notification: Notification) {
            guard let editor = notification.object as? NSTextView else { return }
            document.text = editor.string
        }
    }
}
