using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Endpoint.Site.Models.MindMap;

namespace Endpoint.Site.Services;

public static class MindMapWordExporter
{
    private const string PersianFont = "B Lotus";

    public static byte[] BuildDocxBytes(MindMapExportPayload payload)
    {
        var nodeById = payload.Nodes
            .Where(n => !string.IsNullOrWhiteSpace(n.Id))
            .GroupBy(n => n.Id!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        if (nodeById.Count == 0)
        {
            throw new InvalidOperationException("هیچ نودی برای خروجی وجود ندارد.");
        }

        var incoming = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var link in payload.Links ?? Enumerable.Empty<MindMapLinkPayload>())
        {
            if (string.IsNullOrWhiteSpace(link.To) || !nodeById.ContainsKey(link.To))
            {
                continue;
            }

            incoming[link.To] = incoming.GetValueOrDefault(link.To, 0) + 1;
        }

        var depth = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var id in nodeById.Keys)
        {
            depth[id] = incoming.GetValueOrDefault(id, 0) == 0 ? 0 : -1;
        }

        var links = (payload.Links ?? Enumerable.Empty<MindMapLinkPayload>())
            .Where(l => !string.IsNullOrWhiteSpace(l.From) && !string.IsNullOrWhiteSpace(l.To))
            .Where(l => nodeById.ContainsKey(l.From!) && nodeById.ContainsKey(l.To!))
            .ToList();

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var link in links)
            {
                if (!depth.TryGetValue(link.From!, out var df) || df < 0)
                {
                    continue;
                }

                var nd = df + 1;
                if (depth[link.To!] < nd)
                {
                    depth[link.To!] = nd;
                    changed = true;
                }
            }
        }

        var maxKnown = depth.Values.Where(d => d >= 0).DefaultIfEmpty(0).Max();
        foreach (var id in nodeById.Keys.Where(id => depth[id] < 0))
        {
            depth[id] = maxKnown + 1;
        }

        var byDepth = nodeById.Values
            .GroupBy(n => depth[n.Id])
            .OrderBy(g => g.Key)
            .ToList();

        var tempPath = Path.Combine(Path.GetTempPath(), "mindmap-export-" + Guid.NewGuid().ToString("N") + ".docx");
        try
        {
            using (var doc = WordprocessingDocument.Create(tempPath, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                EnsurePersianStylesAndSettings(mainPart);

                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                body.AppendChild(TitleParagraph("خروجی مایندمپ (لایه‌به‌لایه)"));

                foreach (var grp in byDepth)
                {
                    body.AppendChild(LayerHeadingParagraph($"لایه {grp.Key}"));
                    foreach (var node in grp.OrderBy(n => n.Text, StringComparer.Ordinal))
                    {
                        body.AppendChild(NodeTitleParagraph(node.Text ?? ""));
                    }
                }

                mainPart.Document.Save();
            }

            return File.ReadAllBytes(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// پیش‌فرض سند: RTL، تراز راست، فونت B Lotus، زبان فارسی؛ تنظیمات تم برای متن دوسویه.
    /// </summary>
    private static void EnsurePersianStylesAndSettings(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunProperties(
                        new RunFonts
                        {
                            Ascii = PersianFont,
                            HighAnsi = PersianFont,
                            ComplexScript = PersianFont
                        },
                        new Languages { Val = "fa-IR", EastAsia = "fa-IR", Bidi = "fa-IR" },
                        new RightToLeftText())),
                new ParagraphPropertiesDefault(
                    new ParagraphProperties(
                        new BiDi(),
                        new Justification { Val = JustificationValues.Right }))));

        var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
        settingsPart.Settings = new Settings(
            new ThemeFontLanguages
            {
                Val = "fa-IR",
                EastAsia = "fa-IR",
                Bidi = "fa-IR"
            });
    }

    private static RunProperties RunProps(int halfPoints, bool bold)
    {
        var rp = new RunProperties(
            new RunFonts
            {
                Ascii = PersianFont,
                HighAnsi = PersianFont,
                ComplexScript = PersianFont
            },
            new Languages { Val = "fa-IR", EastAsia = "fa-IR", Bidi = "fa-IR" },
            new RightToLeftText(),
            new FontSize { Val = halfPoints.ToString() },
            new FontSizeComplexScript { Val = halfPoints.ToString() });
        if (bold)
        {
            rp.AppendChild(new Bold());
            rp.AppendChild(new BoldComplexScript());
        }

        return rp;
    }

    private static ParagraphProperties BaseParaProps(int? indentRightTwips = null, string? spacingBefore = null, string? spacingAfter = null)
    {
        var p = new ParagraphProperties(
            new BiDi(),
            new Justification { Val = JustificationValues.Right });
        if (indentRightTwips.HasValue)
        {
            p.AppendChild(new Indentation { Right = indentRightTwips.Value.ToString() });
        }

        if (spacingBefore != null || spacingAfter != null)
        {
            p.AppendChild(new SpacingBetweenLines
            {
                Before = spacingBefore ?? "0",
                After = spacingAfter ?? "0"
            });
        }

        return p;
    }

    private static Paragraph TitleParagraph(string text)
    {
        return new Paragraph(
            BaseParaProps(null, "0", "360"),
            new Run(RunProps(36, true), SafeText(text)));
    }

    private static Paragraph LayerHeadingParagraph(string text)
    {
        return new Paragraph(
            BaseParaProps(null, "280", "160"),
            new Run(RunProps(30, true), SafeText(text)));
    }

    private static Paragraph NodeTitleParagraph(string text)
    {
        return new Paragraph(
            BaseParaProps(360, "80", "80"),
            new Run(RunProps(26, false), SafeText(string.IsNullOrWhiteSpace(text) ? "—" : text)));
    }

    private static Paragraph BulletParagraph(string text)
    {
        return new Paragraph(
            BaseParaProps(720, "40", "40"),
            new Run(RunProps(22, false), SafeText("• " + text)));
    }

    private static Text SafeText(string value)
    {
        var cleaned = new string(value
            .Select(ch => char.IsControl(ch) && ch != '\t' ? ' ' : ch)
            .ToArray());
        if (cleaned.Length > 8000)
        {
            cleaned = cleaned[..8000] + "…";
        }

        return new Text(cleaned) { Space = SpaceProcessingModeValues.Preserve };
    }
}
