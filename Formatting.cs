using System.Text.RegularExpressions;

namespace OpenMD;

public readonly record struct EditResult(string Text, int Start, int Length);

public static class Formatting
{
    public static EditResult Wrap(string text, int start, int length, string before, string after, string placeholder)
    {
        var selected = length == 0 ? placeholder : text.Substring(start, length);
        return new(text.Remove(start, length).Insert(start, before + selected + after),
            start + before.Length, selected.Length);
    }

    public static EditResult Lines(string text, int start, int length, string kind, int level = 0)
    {
        var first = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;
        var lastSelected = length > 0 ? start + length - 1 : start;
        var end = text.IndexOf('\n', Math.Min(lastSelected, text.Length));
        if (end < 0) end = text.Length;
        var lines = text[first..end].Replace("\r", "").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (kind == "heading")
            {
                line = Regex.Replace(line, @"^ {0,3}#{1,6}(?:\s+|$)", "");
                lines[i] = level == 0 ? line : new string('#', level) + " " + line;
            }
            else
            {
                line = Regex.Replace(line, @"^\s*(?:[-+*]\s+(?:\[[ xX]\]\s+)?|\d+[.)]\s+|>\s?)", "");
                lines[i] = kind switch
                {
                    "bullet" => "- " + line,
                    "number" => (i + 1) + ". " + line,
                    "task" => "- [ ] " + line,
                    "quote" => "> " + line,
                    _ => line
                };
            }
        }
        var replacement = string.Join("\n", lines);
        return new(text.Remove(first, end - first).Insert(first, replacement), first, replacement.Length);
    }
}
