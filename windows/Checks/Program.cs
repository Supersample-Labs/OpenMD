using System.Reflection;
using Microsoft.Web.WebView2.WinForms;
using OpenMD;

internal static class Program
{
    static int failures;
    static void Check(bool condition, string name)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
        if (!condition) failures++;
    }
    [STAThread]
    static int Main()
    {
        Check(Formatting.Lines("hello", 2, 0, "heading", 2).Text == "## hello", "Heading on current line");
        Check(Formatting.Lines("### hello", 5, 0, "heading", 1).Text == "# hello", "Replace existing heading");
        Check(Formatting.Lines("## hello", 0, 0, "heading", 0).Text == "hello", "Remove heading");
        Check(Formatting.Lines("one\ntwo\nthree", 0, 4, "heading", 2).Text == "## one\ntwo\nthree", "Selection ending at newline excludes next line");
        Check(Formatting.Lines("one\ntwo", 0, 7, "number").Text == "1. one\n2. two", "Number selected lines");
        Check(Formatting.Lines("- [x] done", 0, 0, "bullet").Text == "- done", "Convert checklist to bullet");
        Check(Formatting.Lines("", 0, 0, "heading", 6).Text == "###### ", "Heading on empty document");
        var wrapped = Formatting.Wrap("hello world", 6, 5, "**", "**", "text");
        Check(wrapped.Text == "hello **world**" && wrapped.Start == 8 && wrapped.Length == 5, "Wrap selection and preserve selected content");
        Check(Formatting.Wrap("", 0, 0, "*", "*", "text").Text == "*text*", "Insert placeholder");
        ApplicationConfiguration.Initialize();
        using var form = new EditorWindow(null);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var editor = (RichTextBox)typeof(EditorWindow).GetField("editor", flags)!.GetValue(form)!;
        var preview = (WebView2)typeof(EditorWindow).GetField("preview", flags)!.GetValue(form)!;
        var readyField = typeof(EditorWindow).GetField("previewReady", flags)!;
        var setDocument = typeof(EditorWindow).GetMethod("SetDocument", flags)!;
        using var timer = new System.Windows.Forms.Timer { Interval = 500 };
        var attempts = 0;
        var checking = false;
        timer.Tick += async (_, _) =>
        {
            if (checking) return;
            if (!(bool)readyField.GetValue(form)!)
            {
                if (++attempts < 180) return;
                Check(false, "Preview initialization within 90 seconds");
            }
            else
            {
                checking = true;
                try
                {
                    setDocument.Invoke(form, new object?[] { "hello", null });
                    editor.Select(0, 5);
                    typeof(EditorWindow).GetMethod("Wrap", flags)!.Invoke(form, new object[] { "**", "**", "text" });
                    Check(editor.Text == "**hello**", "Toolbar formatting changes editor");
                    editor.Undo();
                    Check(editor.Text == "hello", "Toolbar formatting supports Undo");
                    setDocument.Invoke(form, new object?[] { "# Verified\n\n**bold**\n\n| A | B |\n| --- | --- |\n| 1 | 2 |", null });
                    await (Task)typeof(EditorWindow).GetMethod("RenderPreview", flags)!.Invoke(form, null)!;
                    var html = System.Text.Json.JsonSerializer.Deserialize<string>(await preview.ExecuteScriptAsync("document.getElementById('content').innerHTML"))!;
                    Check(html.Contains("<h1") && html.Contains("Verified") && html.Contains("<strong>bold</strong>") && html.Contains("<table>"),
                        "Live WebView2 preview renders headings, bold and tables");
                    var setTheme = typeof(EditorWindow).GetMethod("SetTheme", flags)!;
                    var darkField = typeof(EditorWindow).GetField("darkMode", flags)!;
                    var originalTheme = (bool)darkField.GetValue(form)!;
                    var originalText = editor.Text;
                    editor.Select(2, 4);
                    var originalDirty = typeof(EditorWindow).GetField("dirty", flags)!.GetValue(form);
                    await (Task)setTheme.Invoke(form, new object[] { true, false })!;
                    var darkColors = System.Text.Json.JsonSerializer.Deserialize<string[]>(
                        await preview.ExecuteScriptAsync("[getComputedStyle(document.body).backgroundColor, getComputedStyle(document.body).color].map(String)"))!;
                    Check(editor.BackColor == System.Drawing.Color.FromArgb(15, 23, 42) &&
                        darkColors[0] == "rgb(15, 23, 42)" && darkColors[1] == "rgb(226, 232, 240)", "Dark mode colors editor and preview");
                    Check(editor.Text == originalText && editor.SelectionStart == 2 && editor.SelectionLength == 4 &&
                        Equals(originalDirty, typeof(EditorWindow).GetField("dirty", flags)!.GetValue(form)), "Theme switch preserves text, selection and unsaved state");
                    Check(((ToolStripMenuItem)typeof(EditorWindow).GetField("darkThemeItem", flags)!.GetValue(form)!).Checked,
                        "Dark theme menu reflects selected mode");
                    await (Task)setTheme.Invoke(form, new object[] { false, false })!;
                    var lightColor = System.Text.Json.JsonSerializer.Deserialize<string>(
                        await preview.ExecuteScriptAsync("getComputedStyle(document.body).backgroundColor"));
                    Check(editor.BackColor == System.Drawing.Color.FromArgb(249, 250, 252) && lightColor == "rgb(255, 255, 255)",
                        "Light mode colors editor and preview");
                    await (Task)setTheme.Invoke(form, new object[] { originalTheme, false })!;
                    setDocument.Invoke(form, new object?[] { "hello", null });
                    editor.Select(0, 5);
                    typeof(EditorWindow).GetMethod("Wrap", flags)!.Invoke(form, new object[] { "**", "**", "text" });
                    await (Task)setTheme.Invoke(form, new object[] { !originalTheme, false })!;
                    editor.Undo();
                    Check(editor.Text == "hello", "Theme switch preserves formatting undo history");
                    await (Task)setTheme.Invoke(form, new object[] { originalTheme, false })!;
                    var settingsType = typeof(EditorWindow).Assembly.GetType("OpenMD.ThemeSettings")!;
                    var settingsPath = Path.Combine(AppContext.BaseDirectory, "theme-check.json");
                    try
                    {
                        settingsType.GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { true, settingsPath });
                        Check((bool)settingsType.GetMethod("Load", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { settingsPath })!,
                            "Theme preference persists");
                        File.WriteAllText(settingsPath, "{ invalid");
                        Check(!(bool)settingsType.GetMethod("Load", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { settingsPath })!,
                            "Invalid settings safely fall back to light");
                    }
                    finally { if (File.Exists(settingsPath)) File.Delete(settingsPath); }

                    var oldClipboard = Clipboard.GetDataObject();
                    var restoreClipboard = new DataObject();
                    if (oldClipboard is not null)
                        foreach (var format in oldClipboard.GetFormats(false))
                            try { if (oldClipboard.GetData(format, false) is object value) restoreClipboard.SetData(format, false, value); } catch { }
                    try
                    {
                        setDocument.Invoke(form, new object?[] { "# Heading\n\n**selected**", null });
                        editor.Select(11, 12);
                        typeof(EditorWindow).GetMethod("CopyMarkdown", flags)!.Invoke(form, null);
                        Check(Clipboard.GetText() == "**selected**", "Copy MD button uses selected source");
                        editor.Select(0, 0);
                        typeof(EditorWindow).GetMethod("CopyFormatted", flags)!.Invoke(form, new object[] { false });
                        Check(Clipboard.ContainsText(TextDataFormat.Html), "Copy Formatted button puts rich HTML on Windows clipboard");
                        setDocument.Invoke(form, new object?[] { "start end", null });
                        editor.Select(6, 3);
                        Clipboard.SetText("**pasted**");
                        typeof(EditorWindow).GetMethod("PasteMarkdown", flags)!.Invoke(form, new object[] { false });
                        Check(editor.Text == "start **pasted**", "Paste MD replaces selected text at cursor");
                        editor.Undo();
                        Check(editor.Text == "start end", "Paste MD supports Undo");
                        var officeData = new DataObject();
                        officeData.SetData(DataFormats.Html, MarkdownClipboard.HtmlPayload("<h2>Imported</h2><p><strong>Rich</strong></p>"));
                        Clipboard.SetDataObject(officeData, true);
                        editor.Select(editor.TextLength, 0);
                        typeof(EditorWindow).GetMethod("PasteMarkdown", flags)!.Invoke(form, new object[] { true });
                        Check(editor.Text.Contains("## Imported") && editor.Text.Contains("**Rich**"), "Paste Formatted button converts HTML to Markdown");
                    }
                    finally { Clipboard.SetDataObject(restoreClipboard, true); }
                    ClipboardChecks.Run(Check);

                    Check(form.Icon is not null && form.Icon.Width >= 16, "Application window has an icon");

                    Check(form.Width >= 850 && editor.Width > 200 && preview.Width > 200, "Both editor and preview are laid out");
                }
                catch (Exception ex) { Check(false, ex.ToString()); }
            }
            timer.Stop();
            setDocument.Invoke(form, new object?[] { "", null });
            form.Close();
        };
        form.Shown += (_, _) => timer.Start();
        Application.Run(form);
        Console.WriteLine(failures == 0 ? "All OpenMD checks passed." : $"{failures} checks failed.");
        return failures == 0 ? 0 : 1;
    }
}






