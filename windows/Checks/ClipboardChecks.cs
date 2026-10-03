using System.Text;
using System.Text.RegularExpressions;
using OpenMD;

internal static class ClipboardChecks
{
    internal static void Run(Action<bool, string> check)
    {
        const string md = "# Café 😀\n\n**Bold sample** and *italic sample*.\n\n- First\n- Second\n\n| Name | Value |\n| --- | --- |\n| alpha | 42 |\n\n[Example](https://example.com)\n";
        var data = MarkdownClipboard.Create(md);
        data.TryGetData<string>(DataFormats.Html, out var htmlPayload);
        var payload = htmlPayload!;
        var bytes = Encoding.UTF8.GetBytes(payload);
        int Offset(string name) => int.Parse(Regex.Match(payload, name + @":(\d+)").Groups[1].Value);
        check(Encoding.UTF8.GetString(bytes, Offset("StartFragment"), Offset("EndFragment") - Offset("StartFragment")) ==
            MarkdownClipboard.StyledFragment(md), "CF_HTML UTF-8 byte offsets handle accents and emoji");
        check(Offset("EndHTML") == bytes.Length && Encoding.UTF8.GetString(bytes, Offset("StartHTML"), 6) == "<html>",
            "CF_HTML document boundaries are valid");
        check(data.GetDataPresent(DataFormats.Html) && data.GetDataPresent(DataFormats.UnicodeText) &&
            data.GetDataPresent(MarkdownClipboard.MarkdownFormat), "Formatted copy includes rich HTML, text fallback and Markdown");
        check(!ReadText(data).Contains("**Bold"), "Plain fallback contains rendered text");
        check(MarkdownClipboard.ReadMarkdown(data, true) == md, "OpenMD copy/paste preserves exact Markdown source");
        var rawHtml = MarkdownClipboard.Create(md, true);
        check(ReadText(rawHtml).StartsWith("<!doctype html>"), "Copy HTML exposes source to code editors");
        var foreign = new DataObject();
        foreign.SetData(DataFormats.Html, payload);
        var converted = MarkdownClipboard.ReadMarkdown(foreign, true)!;
        check(converted.Contains("# Café") && converted.Contains("**Bold sample**") && converted.Contains("italic sample") &&
            converted.Contains("https://example.com") && converted.Contains("alpha"), "HTML paste converts headings, emphasis, tables and links to Markdown");
        check(MarkdownClipboard.HtmlToMarkdown("<p><span style='font-weight:700;font-style:italic'>Office</span></p>").Contains("***Office***") ||
            MarkdownClipboard.HtmlToMarkdown("<p><span style='font-weight:700;font-style:italic'>Office</span></p>").Contains("**_Office_**"),
            "Styled Office spans preserve emphasis");
        check(!MarkdownClipboard.HtmlToMarkdown("<style>x{}</style><script>alert(1)</script><p>safe</p>").Contains("alert"), "HTML import drops scripts and stylesheet content");
        var plain = new DataObject();
        plain.SetData(DataFormats.UnicodeText, "## Source\n\n**literal**");
        check(MarkdownClipboard.ReadMarkdown(plain, false) == "## Source\n\n**literal**", "Paste MD keeps plain Markdown syntax");

        var saved = new DataObject();
        var previous = Clipboard.GetDataObject();
        if (previous is not null)
            foreach (var format in previous.GetFormats(false))
                try { if (previous.GetData(format, false) is object value) saved.SetData(format, false, value); } catch { }
        try
        {
            Clipboard.SetDataObject(data, true, 5, 100);
            var actual = Clipboard.GetDataObject()!;
            check(actual.GetDataPresent(DataFormats.Html) && MarkdownClipboard.ReadMarkdown(actual, false) == md,
                "Windows clipboard round trip retains HTML and Markdown formats");
            if (Environment.GetEnvironmentVariable("OPENMD_CHECK_WORD") == "1") CheckWord(check);
        }
        finally { Clipboard.SetDataObject(saved, true, 5, 100); }
    }

    private static string ReadText(DataObject data)
    {
        data.TryGetData<string>(DataFormats.UnicodeText, out var text);
        return text!;
    }

    private static void CheckWord(Action<bool, string> check)
    {
        dynamic? word = null;
        dynamic? document = null;
        try
        {
            var type = Type.GetTypeFromProgID("Word.Application");
            if (type is null) { check(false, "Word is installed"); return; }
            word = Activator.CreateInstance(type)!;
            word.Visible = false;
            word.DisplayAlerts = 0;
            document = word.Documents.Add();
            document.Content.PasteSpecial(DataType: 10); // wdPasteHTML
            string text = document.Content.Text;
            check(text.Contains("Café") && text.Contains("Bold sample") && !text.Contains("**"), "Word pastes rendered text rather than Markdown syntax");
            check((int)document.Tables.Count == 1 && (string)document.Tables[1].Cell(2, 2).Range.Text == "42\r\a", "Word preserves table structure and cells");
            dynamic bold = document.Content.Duplicate;
            bold.Find.Execute(FindText: "Bold sample");
            check((int)bold.Font.Bold == -1, "Word preserves bold formatting");
            dynamic italic = document.Content.Duplicate;
            italic.Find.Execute(FindText: "italic sample");
            check((int)italic.Font.Italic == -1, "Word preserves italic formatting");
            check((float)document.Paragraphs[1].Range.Font.Size > 11, "Word preserves heading size");
            check((int)document.Hyperlinks.Count >= 1, "Word preserves hyperlinks");
        }
        catch (Exception ex) { check(false, "Word paste: " + ex.Message); }
        finally
        {
            if (document is not null) document.Close(SaveChanges: 0);
            if (word is not null) word.Quit(SaveChanges: 0);
        }
    }
}


