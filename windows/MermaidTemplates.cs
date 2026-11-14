namespace OpenMD;

public static class MermaidTemplates
{
    public static IReadOnlyList<(string Name, string Source)> All { get; } = Array.AsReadOnly(new[]
    {
        ("Flowchart", "flowchart TD\n    A[Start] --> B{Decision}\n    B -->|Yes| C[Continue]\n    B -->|No| D[Stop]"),
        ("Sequence diagram", "sequenceDiagram\n    participant User\n    participant App\n    User->>App: Request\n    App-->>User: Response"),
        ("Class diagram", "classDiagram\n    class Animal {\n        +String name\n        +speak()\n    }\n    Animal <|-- Dog"),
        ("State diagram", "stateDiagram-v2\n    [*] --> Idle\n    Idle --> Working: Start\n    Working --> Idle: Finish\n    Idle --> [*]"),
        ("Entity relationship diagram", "erDiagram\n    CUSTOMER ||--o{ ORDER : places\n    CUSTOMER {\n        int id PK\n        string name\n    }\n    ORDER {\n        int id PK\n        int customer_id FK\n    }"),
        ("Gantt chart", "gantt\n    title Project plan\n    dateFormat YYYY-MM-DD\n    section Work\n    Planning :a1, 2026-01-01, 3d\n    Building :a2, after a1, 5d"),
        ("Pie chart", "pie title Example breakdown\n    \"Category A\" : 60\n    \"Category B\" : 40"),
        ("Mind map", "mindmap\n  root((Main idea))\n    Topic A\n      Detail\n    Topic B")
    });
}
