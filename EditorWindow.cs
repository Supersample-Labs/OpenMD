using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OpenMD;

public sealed class EditorWindow : Form
{
    private readonly RichTextBox editor = new()
    {
        Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
        Font = new Font("Cascadia Mono", 11), AcceptsTab = true,
        DetectUrls = false, WordWrap = true, HideSelection = false,
        BackColor = Color.FromArgb(249, 250, 252), ForeColor = Color.FromArgb(30, 41, 59)
    };
    private readonly WebView2 preview = new() { Dock = DockStyle.Fill };
    private readonly ToolStripStatusLabel status = new();
    private readonly System.Windows.Forms.Timer debounce = new() { Interval = 250 };
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().DisableHtml().Build();
    private string? filePath;
    private bool dirty;
    private bool loading;
    private bool previewReady;
    private bool darkMode;
    private readonly ToolStripMenuItem lightThemeItem = new("Light");
    private readonly ToolStripMenuItem darkThemeItem = new("Dark");

    public EditorWindow(string? initialPath)
    {
        Text = "OpenMD";
        Size = new Size(1250, 820);
        MinimumSize = new Size(850, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        using (var iconStream = typeof(EditorWindow).Assembly.GetManifestResourceStream("OpenMD.Icon"))
        {
            if (iconStream is not null)
            {
                using var sourceIcon = new Icon(iconStream);
                Icon = (Icon)sourceIcon.Clone();
            }
        }

        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("&File");
        AddMenu(file, "&New", Keys.Control | Keys.N, () => { if (ConfirmDiscard()) SetDocument("", null); });
        AddMenu(file, "&Open…", Keys.Control | Keys.O, OpenFile);
        AddMenu(file, "&Save", Keys.Control | Keys.S, () => Save(false));
        AddMenu(file, "Save &As…", Keys.Control | Keys.Shift | Keys.S, () => Save(true));
        file.DropDownItems.Add(new ToolStripSeparator());
        AddMenu(file, "Export &HTML…", Keys.None, ExportHtml);
        AddMenu(file, "E&xit", Keys.None, Close);
        menu.Items.Add(file);
        var edit = new ToolStripMenuItem("&Edit");
        AddMenu(edit, "&Undo", Keys.Control | Keys.Z, () => editor.Undo());
        AddMenu(edit, "&Redo", Keys.Control | Keys.Y, () => editor.Redo());
        AddMenu(edit, "&Bold", Keys.Control | Keys.B, () => Wrap("**", "**", "bold text"));
        AddMenu(edit, "&Italic", Keys.Control | Keys.I, () => Wrap("*", "*", "italic text"));
        menu.Items.Add(edit);
        var view = new ToolStripMenuItem("&View");
        var theme = new ToolStripMenuItem("&Theme");
        lightThemeItem.Click += async (_, _) => await SetTheme(false);
        darkThemeItem.Click += async (_, _) => await SetTheme(true);
        theme.DropDownItems.AddRange([lightThemeItem, darkThemeItem]);
        view.DropDownItems.Add(theme);
        menu.Items.Add(view);
        MainMenuStrip = menu;

        var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(8), BackColor = Color.White };
        AddButton(toolbar, "New", () => { if (ConfirmDiscard()) SetDocument("", null); });
        AddButton(toolbar, "Open", OpenFile);
        AddButton(toolbar, "Save", () => Save(false));
        toolbar.Items.Add(new ToolStripSeparator());
        var headings = new ToolStripDropDownButton("Heading");
        for (var level = 1; level <= 6; level++)
        {
            var chosen = level;
            headings.DropDownItems.Add("Heading " + level, null, (_, _) => ApplyLines("heading", chosen));
        }
        headings.DropDownItems.Add("Normal text", null, (_, _) => ApplyLines("heading", 0));
        toolbar.Items.Add(headings);
        AddButton(toolbar, "Bold", () => Wrap("**", "**", "bold text"));
        AddButton(toolbar, "Italic", () => Wrap("*", "*", "italic text"));
        AddButton(toolbar, "Strike", () => Wrap("~~", "~~", "text"));
        AddButton(toolbar, "Code", () => Wrap("`", "`", "code"));
        toolbar.Items.Add(new ToolStripSeparator());
        AddButton(toolbar, "Bullets", () => ApplyLines("bullet"));
        AddButton(toolbar, "Numbered", () => ApplyLines("number"));
        AddButton(toolbar, "Checklist", () => ApplyLines("task"));
        AddButton(toolbar, "Quote", () => ApplyLines("quote"));
        AddButton(toolbar, "Link", () => Wrap("[", "](https://example.com)", "link text"));
        AddButton(toolbar, "Image", () => Wrap("![", "](https://example.com/image.png)", "image description"));
        AddButton(toolbar, "Code block", () => Block("```\n", "\n```", "code"));
        AddButton(toolbar, "Table", () => Block("", "", "| Column 1 | Column 2 |\n| --- | --- |\n| Value | Value |"));
        AddButton(toolbar, "Rule", () => Block("", "", "---"));

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterWidth = 6 };
        split.Panel1.Controls.Add(editor);
        split.Panel1.Controls.Add(new Label { Text = "MARKDOWN", Dock = DockStyle.Top, Height = 38, Padding = new Padding(12, 10, 0, 0), ForeColor = Color.DimGray });
        split.Panel2.Controls.Add(preview);
        split.Panel2.Controls.Add(new Label { Text = "LIVE PREVIEW", Dock = DockStyle.Top, Height = 38, Padding = new Padding(12, 10, 0, 0), ForeColor = Color.DimGray });
        var footer = new StatusStrip();
        footer.Items.Add(status);
        Controls.Add(split);
        Controls.Add(toolbar);
        Controls.Add(menu);
        Controls.Add(footer);
        Shown += async (_, _) =>
        {
            split.SplitterDistance = split.Width / 2;
            await InitializePreview();
            editor.Focus();
        };
        editor.TextChanged += (_, _) =>
        {
            if (!loading) dirty = true;
            UpdateStatus();
            debounce.Stop();
            debounce.Start();
        };
        editor.SelectionChanged += (_, _) => UpdateStatus();
        debounce.Tick += async (_, _) => { debounce.Stop(); await RenderPreview(); };
        FormClosing += (_, e) => e.Cancel = !ConfirmDiscard();
        FormClosed += (_, _) => debounce.Dispose();

        SetDocument("# Welcome to OpenMD\n\nSelect text and choose a format from the toolbar. Choose a **heading level** to format the current line or selected lines.\n\n## Make it yours\n\n- Write Markdown on the left\n- See the result on the right\n- Open and save ordinary .md files\n\n### A little formatting\n\nUse **bold**, *italic*, ~~strikethrough~~, and `inline code`.\n\n> A clear space for your ideas.\n\n- [ ] Write something great\n- [x] Open OpenMD\n\n| Feature | Ready |\n| --- | --- |\n| Live preview | Yes |\n| Formatting toolbar | Yes |\n", null);
        if (!string.IsNullOrWhiteSpace(initialPath)) ReadFile(initialPath);
        _ = SetTheme(ThemeSettings.Load(), false);
    }

    private static void AddMenu(ToolStripMenuItem parent, string text, Keys keys, Action action)
    {
        parent.DropDownItems.Add(new ToolStripMenuItem(text, null, (_, _) => action()) { ShortcutKeys = keys });
    }

    private static void AddButton(ToolStrip toolbar, string text, Action action)
    {
        toolbar.Items.Add(new ToolStripButton(text, null, (_, _) => action()) { ToolTipText = text + " — format selected text or insert at cursor" });
    }

    private async Task InitializePreview()
    {
        try
        {
            var environment = await CoreWebView2Environment.CreateAsync(null,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenMD", "WebView2"));
            await preview.EnsureCoreWebView2Async(environment);
            preview.CoreWebView2.Settings.AreDevToolsEnabled = false;
            preview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            preview.CoreWebView2.NavigationStarting += (_, e) =>
            {
                
                if (!previewReady && e.Uri.StartsWith("data:text/html", StringComparison.Ordinal)) return;
                if (e.Uri != "about:blank" && !e.Uri.StartsWith("about:blank#", StringComparison.Ordinal)) e.Cancel = true;
            };
            preview.CoreWebView2.NewWindowRequested += (_, e) => e.Handled = true;
            var ready = new TaskCompletionSource();
            preview.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                if (e.IsSuccess) ready.TrySetResult();
                else ready.TrySetException(new IOException("Preview page could not load: " + e.WebErrorStatus));
            };
            preview.NavigateToString(HtmlShell("<p>Loading preview…</p>"));
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(60));
            previewReady = true;
            await RenderPreview();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            MessageBox.Show(this, "The editor is ready, but the preview could not start. Install Microsoft Edge WebView2 Runtime and reopen OpenMD.\n\n" + ex.Message,
                "OpenMD preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async Task RenderPreview()
    {
        if (!previewReady || IsDisposed) return;
        try
        {
            var html = Markdown.ToHtml(editor.Text, pipeline);
            await preview.ExecuteScriptAsync("document.documentElement.dataset.theme = " + JsonSerializer.Serialize(darkMode ? "dark" : "light") + "; document.getElementById('content').innerHTML = " + JsonSerializer.Serialize(html) + ";");
        }
        catch (Exception ex) { status.Text = "Preview error: " + ex.Message; }
    }

    private static string HtmlShell(string body) => """
        <!doctype html><html><head><meta charset="utf-8">
        <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; img-src https: data:; script-src 'none'">
        <style>
        :root{color-scheme:light;--page:#fff;--text:#243247;--heading:#142238;--border:#e2e8f0;--code:#f1f5f9;--muted:#64748b;--quote:#f8fafc;--link:#4f46e5}
        :root[data-theme="dark"]{color-scheme:dark;--page:#0f172a;--text:#e2e8f0;--heading:#f8fafc;--border:#334155;--code:#1e293b;--muted:#94a3b8;--quote:#182235;--link:#a5b4fc}
        body{font:16px/1.7 'Segoe UI',sans-serif;color:var(--text);background:var(--page);margin:0;padding:32px}
        #content{max-width:850px;margin:auto;overflow-wrap:anywhere}
        h1,h2,h3,h4,h5,h6{line-height:1.3;color:var(--heading);margin:1.4em 0 .5em}
        h1{font-size:2.2em}h2{border-bottom:1px solid var(--border);padding-bottom:.3em}
        pre,code{font-family:Consolas,monospace;background:var(--code);border-radius:5px}
        code{padding:2px 5px}pre{padding:16px;overflow:auto}pre code{padding:0}
        blockquote{border-left:4px solid #818cf8;margin:20px 0;padding:1px 20px;color:var(--muted);background:var(--quote)}
        table{border-collapse:collapse;width:100%;margin:20px 0}td,th{border:1px solid var(--border);padding:8px 12px;text-align:left}th{background:var(--code)}
        a{color:var(--link)}img{max-width:100%;height:auto}hr{border:0;border-top:1px solid var(--border);margin:28px 0}
        </style></head><body><main id="content">
        """ + body + "</main></body></html>";


    private async Task SetTheme(bool dark, bool persist = true)
    {
        darkMode = dark;
        var palette = dark ? ThemePalette.Dark : ThemePalette.Light;
        var renderer = new ToolStripProfessionalRenderer(new ThemeColorTable(palette));
        var selectionStart = editor.SelectionStart;
        var selectionLength = editor.SelectionLength;
        var wasLoading = loading;
        loading = true;
        try
        {
            using var undo = UndoSuspension.Begin(editor);
            ApplyControlTheme(this, palette, renderer);
            editor.BackColor = palette.Editor;
            editor.ForeColor = palette.Text;
            editor.Select(selectionStart, selectionLength);
        }
        finally { loading = wasLoading; }
        UpdateStatus();
        preview.DefaultBackgroundColor = palette.Editor;
        lightThemeItem.Checked = !dark;
        darkThemeItem.Checked = dark;
        if (IsHandleCreated) WindowTheme.Apply(Handle, dark);
        if (persist)
        {
            try { ThemeSettings.Save(dark); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { status.Text = "Theme changed, but your preference could not be saved: " + ex.Message; }
        }
        await RenderPreview();
    }

    private static void ApplyControlTheme(Control control, ThemePalette palette, ToolStripRenderer renderer)
    {
        if (control is WebView2) return;
        control.BackColor = palette.Surface;
        control.ForeColor = control is Label ? palette.Muted : palette.Text;
        if (control is ToolStrip strip)
        {
            strip.Renderer = renderer;
            ApplyItemTheme(strip.Items, palette, renderer);
            if (strip is not MenuStrip && strip is not StatusStrip)
            {
                strip.OverflowButton.DropDown.BackColor = palette.Surface;
                strip.OverflowButton.DropDown.ForeColor = palette.Text;
                strip.OverflowButton.DropDown.Renderer = renderer;
            }
        }
        foreach (Control child in control.Controls) ApplyControlTheme(child, palette, renderer);
    }

    private static void ApplyItemTheme(ToolStripItemCollection items, ThemePalette palette, ToolStripRenderer renderer)
    {
        foreach (ToolStripItem item in items)
        {
            item.BackColor = palette.Surface;
            item.ForeColor = palette.Text;
            if (item is ToolStripDropDownItem dropdown && dropdown.HasDropDownItems)
            {
                dropdown.DropDown.BackColor = palette.Surface;
                dropdown.DropDown.ForeColor = palette.Text;
                dropdown.DropDown.Renderer = renderer;
                ApplyItemTheme(dropdown.DropDownItems, palette, renderer);
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTheme.Apply(Handle, darkMode);
    }

    private void Apply(EditResult result)
    {
        // Replace only the changed range to preserve RichTextBox's native undo history.
        var original = editor.Text;
        var prefix = 0;
        while (prefix < original.Length && prefix < result.Text.Length && original[prefix] == result.Text[prefix]) prefix++;
        var suffix = 0;
        while (suffix < original.Length - prefix && suffix < result.Text.Length - prefix &&
            original[original.Length - suffix - 1] == result.Text[result.Text.Length - suffix - 1]) suffix++;
        editor.Select(prefix, original.Length - prefix - suffix);
        editor.SelectedText = result.Text.Substring(prefix, result.Text.Length - prefix - suffix);
        editor.Select(result.Start, result.Length);
        editor.Focus();
    }

    private void Wrap(string before, string after, string placeholder) =>
        Apply(Formatting.Wrap(editor.Text, editor.SelectionStart, editor.SelectionLength, before, after, placeholder));

    private void ApplyLines(string kind, int level = 0) =>
        Apply(Formatting.Lines(editor.Text, editor.SelectionStart, editor.SelectionLength, kind, level));

    private void Block(string before, string after, string placeholder)
    {
        var start = editor.SelectionStart;
        var end = start + editor.SelectionLength;
        var lead = start > 0 && editor.Text[start - 1] != '\n' ? "\n\n" : "\n";
        var tail = end < editor.TextLength && editor.Text[end] != '\n' ? "\n\n" : "\n";
        Wrap(lead + before, after + tail, placeholder);
    }

    private void SetDocument(string text, string? path)
    {
        loading = true;
        editor.Text = text;
        editor.ClearUndo();
        loading = false;
        filePath = path;
        dirty = false;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        Text = (dirty ? "* " : "") + (filePath is null ? "Untitled" : Path.GetFileName(filePath)) + " — OpenMD";
        var line = editor.GetLineFromCharIndex(editor.SelectionStart);
        var column = editor.SelectionStart - editor.GetFirstCharIndexFromLine(line);
        status.Text = $"{Regex.Matches(editor.Text, @"\S+").Count:N0} words   •   {editor.TextLength:N0} characters   •   Ln {line + 1}, Col {column + 1}   •   " + (dirty ? "Unsaved changes" : "Saved");
    }

    private bool ConfirmDiscard()
    {
        if (!dirty) return true;
        var answer = MessageBox.Show(this, "Save your changes before continuing?", "OpenMD",
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return answer == DialogResult.No || answer == DialogResult.Yes && Save(false);
    }

    private void OpenFile()
    {
        if (!ConfirmDiscard()) return;
        using var dialog = new OpenFileDialog { Filter = "Markdown files|*.md;*.markdown;*.txt|All files|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK) ReadFile(dialog.FileName);
    }

    private void ReadFile(string path)
    {
        try { SetDocument(File.ReadAllText(path), Path.GetFullPath(path)); }
        catch (Exception ex) { ShowError("Could not open the file.", ex); }
    }

    private bool Save(bool saveAs)
    {
        var destination = filePath;
        if (saveAs || destination is null)
        {
            using var dialog = new SaveFileDialog { Filter = "Markdown files|*.md|Text files|*.txt|All files|*.*",
                DefaultExt = "md", FileName = destination is null ? "Untitled.md" : Path.GetFileName(destination) };
            if (dialog.ShowDialog(this) != DialogResult.OK) return false;
            destination = dialog.FileName;
        }
        try
        {
            File.WriteAllText(destination, editor.Text, new UTF8Encoding(false));
            filePath = destination;
            dirty = false;
            UpdateStatus();
            return true;
        }
        catch (Exception ex) { ShowError("Could not save the file.", ex); return false; }
    }

    private void ExportHtml()
    {
        using var dialog = new SaveFileDialog { Filter = "HTML files|*.html", DefaultExt = "html", FileName = "document.html" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(dialog.FileName, HtmlShell(Markdown.ToHtml(editor.Text, pipeline)), new UTF8Encoding(false)); }
        catch (Exception ex) { ShowError("Could not export HTML.", ex); }
    }

    private void ShowError(string message, Exception ex) =>
        MessageBox.Show(this, message + "\n\n" + ex.Message, "OpenMD", MessageBoxButtons.OK, MessageBoxIcon.Error);
}








