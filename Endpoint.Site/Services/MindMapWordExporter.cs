using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Endpoint.Site.Models.MindMap;

namespace Endpoint.Site.Services;

public static class MindMapWordExporter
{
    private const string PersianFont = "B Lotus";
    private const int ListNumberingId = 1;
    private const int MaxListLevel = 9;

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

        var nodeOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < payload.Nodes.Count; i++)
        {
            var id = payload.Nodes[i].Id;
            if (string.IsNullOrWhiteSpace(id) || nodeOrder.ContainsKey(id))
            {
                continue;
            }

            nodeOrder[id] = i;
        }

        var links = (payload.Links ?? Enumerable.Empty<MindMapLinkPayload>())
            .Where(l => !string.IsNullOrWhiteSpace(l.From) && !string.IsNullOrWhiteSpace(l.To))
            .Where(l => nodeById.ContainsKey(l.From!) && nodeById.ContainsKey(l.To!))
            .ToList();

        var parentByChild = new Dictionary<string, string>(StringComparer.Ordinal);
        var childrenByParent = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            if (!parentByChild.ContainsKey(link.To!))
            {
                parentByChild[link.To!] = link.From!;
            }

            if (!childrenByParent.TryGetValue(link.From!, out var children))
            {
                children = new List<string>();
                childrenByParent[link.From!] = children;
            }

            if (!children.Contains(link.To!, StringComparer.Ordinal))
            {
                children.Add(link.To!);
            }
        }

        var depth = ComputeDepths(nodeById.Keys, links, parentByChild);
        var roots = nodeById.Keys
            .Where(id => !parentByChild.ContainsKey(id))
            .OrderBy(id => nodeOrder.GetValueOrDefault(id, int.MaxValue))
            .ThenBy(id => nodeById[id].Text, StringComparer.Ordinal)
            .ToList();

        var tempPath = Path.Combine(Path.GetTempPath(), "mindmap-export-" + Guid.NewGuid().ToString("N") + ".docx");
        try
        {
            using (var doc = WordprocessingDocument.Create(tempPath, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                EnsurePersianStylesAndSettings(mainPart);
                EnsureMultilevelNumbering(mainPart);

                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                body.AppendChild(TitleParagraph("خروجی مایندمپ"));

                var visited = new HashSet<string>(StringComparer.Ordinal);
                foreach (var rootId in roots)
                {
                    AppendNodeSubtree(body, rootId, 0, nodeById, childrenByParent, depth, nodeOrder, visited);
                }

                foreach (var node in nodeById.Values
                             .Where(n => !visited.Contains(n.Id))
                             .OrderBy(n => nodeOrder.GetValueOrDefault(n.Id, int.MaxValue))
                             .ThenBy(n => n.Text, StringComparer.Ordinal))
                {
                    AppendNodeSubtree(body, node.Id, depth.GetValueOrDefault(node.Id, 0), nodeById, childrenByParent, depth, nodeOrder, visited);
                }

                body.AppendChild(new SectionProperties(
                    new BiDi(),
                    new PageMargin
                    {
                        Top = 1440,
                        Right = 1440,
                        Bottom = 1440,
                        Left = 1440
                    }));

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

    private static Dictionary<string, int> ComputeDepths(
        IEnumerable<string> nodeIds,
        IReadOnlyList<MindMapLinkPayload> links,
        IReadOnlyDictionary<string, string> parentByChild)
    {
        var depth = nodeIds.ToDictionary(id => id, id => parentByChild.ContainsKey(id) ? -1 : 0, StringComparer.Ordinal);

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var link in links)
            {
                if (!depth.TryGetValue(link.From!, out var parentDepth) || parentDepth < 0)
                {
                    continue;
                }

                var nextDepth = parentDepth + 1;
                if (depth[link.To!] < nextDepth)
                {
                    depth[link.To!] = nextDepth;
                    changed = true;
                }
            }
        }

        var maxKnown = depth.Values.Where(d => d >= 0).DefaultIfEmpty(0).Max();
        foreach (var id in depth.Keys.Where(id => depth[id] < 0))
        {
            depth[id] = maxKnown + 1;
        }

        return depth;
    }

    private static void AppendNodeSubtree(
        Body body,
        string nodeId,
        int listLevel,
        IReadOnlyDictionary<string, MindMapNodePayload> nodeById,
        IReadOnlyDictionary<string, List<string>> childrenByParent,
        IReadOnlyDictionary<string, int> depth,
        IReadOnlyDictionary<string, int> nodeOrder,
        ISet<string> visited)
    {
        if (!nodeById.TryGetValue(nodeId, out var node) || !visited.Add(nodeId))
        {
            return;
        }

        var nodeDepth = depth.GetValueOrDefault(nodeId, listLevel);
        body.AppendChild(MultiLevelListParagraph(node.Text ?? "", listLevel, nodeDepth));

        if (!childrenByParent.TryGetValue(nodeId, out var children))
        {
            return;
        }

        foreach (var childId in children
                     .OrderBy(id => nodeOrder.GetValueOrDefault(id, int.MaxValue))
                     .ThenBy(id => nodeById.TryGetValue(id, out var child) ? child.Text : null, StringComparer.Ordinal))
        {
            AppendNodeSubtree(body, childId, listLevel + 1, nodeById, childrenByParent, depth, nodeOrder, visited);
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
                        new Justification { Val = JustificationValues.Start }))),
            new Style(
                new StyleName { Val = "Normal" },
                new PrimaryStyle(),
                new StyleParagraphProperties(
                    new BiDi(),
                    new Justification { Val = JustificationValues.Start }),
                new StyleRunProperties(
                    new RunFonts
                    {
                        Ascii = PersianFont,
                        HighAnsi = PersianFont,
                        ComplexScript = PersianFont
                    },
                    new Languages { Val = "fa-IR", EastAsia = "fa-IR", Bidi = "fa-IR" },
                    new RightToLeftText()))
            {
                Type = StyleValues.Paragraph,
                StyleId = "Normal",
                Default = true
            });

        var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
        settingsPart.Settings = new Settings(
            new ThemeFontLanguages
            {
                Val = "fa-IR",
                EastAsia = "fa-IR",
                Bidi = "fa-IR"
            });
    }

    private static void EnsureMultilevelNumbering(MainDocumentPart mainPart)
    {
        var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
        var abstractNum = new AbstractNum { AbstractNumberId = 1 };
        abstractNum.AppendChild(new MultiLevelType { Val = MultiLevelValues.Multilevel });

        var bullets = new[] { "\u2022", "\u25E6", "\u25AA", "\u2013", "\u2022", "\u25E6", "\u25AA", "\u2013", "\u2022" };
        for (var level = 0; level < MaxListLevel; level++)
        {
            var listLevel = new Level
            {
                LevelIndex = level,
                StartNumberingValue = new StartNumberingValue { Val = 1 },
                NumberingFormat = new NumberingFormat { Val = NumberFormatValues.Bullet },
                LevelText = new LevelText { Val = bullets[level] },
                LevelJustification = new LevelJustification { Val = LevelJustificationValues.Left }
            };

            var indentTwips = 360 * (level + 1);
            listLevel.AppendChild(new ParagraphProperties(
                new BiDi(),
                new Justification { Val = JustificationValues.Start },
                new Indentation
                {
                    Start = indentTwips.ToString(),
                    Hanging = "360"
                }));

            listLevel.AppendChild(new RunProperties(
                new RunFonts
                {
                    Ascii = PersianFont,
                    HighAnsi = PersianFont,
                    ComplexScript = PersianFont
                },
                new Languages { Val = "fa-IR", EastAsia = "fa-IR", Bidi = "fa-IR" },
                new RightToLeftText()));

            abstractNum.AppendChild(listLevel);
        }

        numberingPart.Numbering = new Numbering(
            abstractNum,
            new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = ListNumberingId });
    }

    private static int FontHalfPointsForDepth(int depth)
    {
        return depth switch
        {
            0 => 48,
            1 => 40,
            2 => 36,
            3 => 32,
            4 => 28,
            _ => 24
        };
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

    private static ParagraphProperties BaseParaProps(string? spacingBefore = null, string? spacingAfter = null)
    {
        var p = new ParagraphProperties(
            new BiDi(),
            new Justification { Val = JustificationValues.Start },
            new ParagraphStyleId { Val = "Normal" });

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
            BaseParaProps("0", "360"),
            new Run(RunProps(36, true), SafeText(text)));
    }

    private static Paragraph MultiLevelListParagraph(string text, int listLevel, int depth)
    {
        var level = Math.Clamp(listLevel, 0, MaxListLevel - 1);
        var pProps = new ParagraphProperties(
            new ParagraphStyleId { Val = "Normal" },
            new BiDi(),
            new NumberingProperties(
                new NumberingLevelReference { Val = level },
                new NumberingId { Val = ListNumberingId }),
            new Justification { Val = JustificationValues.Start },
            new SpacingBetweenLines
            {
                Before = "40",
                After = "40"
            });

        return new Paragraph(
            pProps,
            new Run(RunProps(FontHalfPointsForDepth(depth), depth == 0), SafeText(string.IsNullOrWhiteSpace(text) ? "—" : text)));
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
