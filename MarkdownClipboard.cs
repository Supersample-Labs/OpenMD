using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Markdig;

namespace OpenMD;

public static class MarkdownClipboard
{
    public const string MarkdownFormat = "text/markdown";
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().DisableHtml().Build();

    public static string StyledFragment(string markdown)
    {
        var html = Markdown.ToHtml(markdown, Pipeline);
        // Office pastes don't reliably keep a separate stylesheet. Inline styles travel with the fragment.
        html = Regex.Replace(html, @"<input\b[^>]*>", m => m.Value.Contains("checked", StringComparison.OrdinalIgnoreCase) ? "☑ " : "☐ ");
        var document = new HtmlParser().ParseDocument(html);
        foreach (var element in document.Body!.QuerySelectorAll("*"))
        {
            var style = element.LocalName switch
            {
                "h1" => "font-size:24pt;font-weight:bold;margin:18pt 0 8pt;color:#111827;",
                "h2" => "font-size:20pt;font-weight:bold;margin:16pt 0 8pt;color:#111827;",
                "h3" => "font-size:16pt;font-weight:bold;margin:14pt 0 6pt;color:#111827;",
                "h4" or "h5" or "h6" => "font-size:13pt;font-weight:bold;margin:12pt 0 6pt;color:#111827;",
                "p" => "margin:0 0 10pt;",
                "strong" or "b" => "font-weight:bold;",
                "em" or "i" => "font-style:italic;",
                "del" or "s" => "text-decoration:line-through;",
                "pre" => "font-family:Consolas,monospace;white-space:pre-wrap;background-color:#f1f5f9;padding:10pt;",
                "code" => "font-family:Consolas,monospace;background-color:#f1f5f9;",
                "blockquote" => "border-left:3pt solid #818cf8;padding-left:12pt;margin-left:0;color:#475569;",
                "table" => "border-collapse:collapse;margin:10pt 0;",
                "td" => "border:1pt solid #cbd5e1;padding:6pt;",
                "th" => "border:1pt solid #cbd5e1;padding:6pt;background-color:#f1f5f9;font-weight:bold;",
                "a" => "color:#4f46e5;text-decoration:underline;",
                "img" => "max-width:100%;",
                "ul" or "ol" => "margin:0 0 10pt;padding-left:24pt;",
                _ => ""
            };
            if (style.Length > 0) element.SetAttribute("style", style);
        }
        return "<div style=\"font-family:Calibri,Arial,sans-serif;font-size:11pt;line-height:1.5;color:#111827;\">" + document.Body.InnerHtml + "</div>";
    }

    public static string HtmlDocument(string fragment) =>
        "<!doctype html><html><head><meta charset=\"utf-8\"></head><body>" + fragment + "</body></html>";

    public static string HtmlPayload(string fragment)
    {
        const string headerTemplate = "Version:1.0\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
        const string prefix = "<html><head><meta charset=\"utf-8\"></head><body><!--StartFragment-->";
        const string suffix = "<!--EndFragment--></body></html>";
        var startHtml = Encoding.UTF8.GetByteCount(string.Format(CultureInfo.InvariantCulture, headerTemplate, 0, 0, 0, 0));
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(prefix);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(suffix);
        return string.Format(CultureInfo.InvariantCulture, headerTemplate, startHtml, endHtml, startFragment, endFragment) +
            prefix + fragment + suffix;
    }

    public static string ExtractHtml(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var start = Regex.Match(payload, @"(?m)^StartFragment:\s*(\d+)");
        var end = Regex.Match(payload, @"(?m)^EndFragment:\s*(\d+)");
        if (int.TryParse(start.Groups[1].Value, out var first) && int.TryParse(end.Groups[1].Value, out var last) &&
            first >= 0 && last >= first && last <= bytes.Length)
            return Encoding.UTF8.GetString(bytes, first, last - first);
        const string startMarker = "<!--StartFragment-->";
        var marker = payload.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        var endMarker = payload.IndexOf("<!--EndFragment-->", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0 && endMarker >= marker + startMarker.Length)
            return payload[(marker + startMarker.Length)..endMarker];
        return payload;
    }

    public static DataObject Create(string markdown, bool htmlSource = false)
    {
        var fragment = StyledFragment(markdown);
        var data = new DataObject();
        data.SetData(DataFormats.Html, false, HtmlPayload(fragment));
        data.SetData(DataFormats.UnicodeText, false,
            htmlSource ? HtmlDocument(fragment) : Markdown.ToPlainText(markdown, Pipeline));
        data.SetData(MarkdownFormat, false, markdown);
        return data;
    }

    public static string HtmlToMarkdown(string payload)
    {
        var document = new HtmlParser().ParseDocument(ExtractHtml(payload));
        foreach (var element in document.QuerySelectorAll("script,style,meta,link")) element.Remove();
        // Word and OneNote often represent inline formatting with styled spans.
        foreach (var element in document.Body!.QuerySelectorAll("span"))
        {
            var style = element.GetAttribute("style") ?? "";
            if (Regex.IsMatch(style, @"font-weight\s*:\s*(?:bold|[6-9]00)", RegexOptions.IgnoreCase))
                element.InnerHtml = "<strong>" + element.InnerHtml + "</strong>";
            if (Regex.IsMatch(style, @"font-style\s*:\s*italic", RegexOptions.IgnoreCase))
                element.InnerHtml = "<em>" + element.InnerHtml + "</em>";
            if (Regex.IsMatch(style, @"text-decoration[^:]*\s*:[^;]*line-through", RegexOptions.IgnoreCase))
                element.InnerHtml = "<del>" + element.InnerHtml + "</del>";
        }
        return new ReverseMarkdown.Converter(new ReverseMarkdown.Config
        {
            GithubFlavored = true,
            Tags = { Unknown = ReverseMarkdown.Config.UnknownTagsOption.Bypass },
            Formatting = { RemoveComments = true }
        }).Convert(document.Body.InnerHtml);
    }

    public static string? ReadMarkdown(IDataObject data, bool convertHtml)
    {
        if (data.GetDataPresent(MarkdownFormat, false) && data.GetData(MarkdownFormat, false) is string markdown)
            return markdown;
        if (convertHtml && data.GetDataPresent(DataFormats.Html))
        {
            var value = data.GetData(DataFormats.Html);
            if (value is string html) return HtmlToMarkdown(html);
            if (value is MemoryStream stream) return HtmlToMarkdown(Encoding.UTF8.GetString(stream.ToArray()).TrimEnd('\0'));
        }
        return data.GetData(DataFormats.UnicodeText) as string ?? data.GetData(DataFormats.Text) as string;
    }
}

