using AdminMembers.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text.Json;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using ParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using Document = iTextSharp.text.Document;
using Font = iTextSharp.text.Font;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;

namespace AdminMembers.Services
{
    public class BoardReportExportService
    {
        private const string DarkGrayHex = "2D2D2D";
        private const string MedGrayHex = "5A5A5A";
        private const string AccentHex = "1B3A5C"; // dark navy accent
        private const string AccentLightHex = "E8EEF4"; // very light blue for agenda bars

        private record AgendaEntry(string Title, string Notes);

        private static List<AgendaEntry> ParseAgendaItems(string? agendaJson)
        {
            if (string.IsNullOrWhiteSpace(agendaJson)) return [];
            try
            {
                var items = JsonSerializer.Deserialize<List<JsonElement>>(agendaJson);
                if (items == null) return [];
                return items.Select(el =>
                {
                    var title = el.TryGetProperty("Title", out var t) ? t.GetString() ?? "" : "";
                    var notes = el.TryGetProperty("Notes", out var n) ? n.GetString() ?? "" : "";
                    return new AgendaEntry(title, notes);
                }).Where(a => !string.IsNullOrWhiteSpace(a.Title)).ToList();
            }
            catch
            {
                return agendaJson.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => new AgendaEntry(l.Trim(), "")).ToList();
            }
        }

        private static string FormatAttendeeList(IEnumerable<BoardReportAttendee> attendees, bool present)
        {
            var names = attendees
                .Where(a => a.IsPresent == present)
                .OrderBy(a => a.Member?.FirstName)
                .Select(a => a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : $"Lid #{a.MemberId}")
                .ToList();
            if (names.Count == 0) return "";
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.Take(names.Count - 1)) + " en " + names.Last();
        }

        private static string DetectImageContentType(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "image/jpeg";
            if (data.Length >= 4 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
                return "image/gif";
            if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
                return "image/bmp";
            return "image/png";
        }

        // ── Word (.docx) export ─────────────────────────────────────────
        public byte[] ExportToWord(BoardReport report, byte[]? logoData = null, string? logoContentType = null)
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new Body());

                var sectionProps = new SectionProperties(
                    new PageMargin { Top = 850, Bottom = 850, Left = 1100u, Right = 1100u });
                body.AppendChild(sectionProps);

                // ── Logo top-right ──
                if (logoData?.Length > 0)
                {
                    try
                    {
                        var ct = logoContentType ?? DetectImageContentType(logoData);
                        var imagePartType = ct switch
                        {
                            "image/jpeg" => ImagePartType.Jpeg,
                            "image/gif" => ImagePartType.Gif,
                            "image/bmp" => ImagePartType.Bmp,
                            _ => ImagePartType.Png
                        };
                        var imagePart = mainPart.AddImagePart(imagePartType);
                        using (var ms = new MemoryStream(logoData))
                            imagePart.FeedData(ms);

                        var imageId = mainPart.GetIdOfPart(imagePart);
                        var drawing = CreateWordImage(imageId, 2400000, 1200000);
                        var imgRun = new Run(drawing);
                        var imgPara = new Paragraph(
                            new ParagraphProperties(
                                new Justification { Val = JustificationValues.Right },
                                new SpacingBetweenLines { After = "100" }));
                        imgPara.Append(imgRun);
                        body.InsertBefore(imgPara, body.Descendants<SectionProperties>().FirstOrDefault());
                    }
                    catch { /* Logo rendering failed — continue without logo */ }
                }

                // ── Title ──
                AddWordParagraph(body, report.Title, 22, true, AccentHex, JustificationValues.Left);
                // Thin accent line
                var linePara = new Paragraph();
                var lineProps = new ParagraphProperties(
                    new ParagraphBorders(
                        new BottomBorder { Val = BorderValues.Single, Color = AccentHex, Size = 8, Space = 1 }),
                    new SpacingBetweenLines { After = "200" });
                linePara.Append(lineProps);
                body.InsertBefore(linePara, body.Descendants<SectionProperties>().FirstOrDefault());

                // ── Meeting info block ──
                AddWordLabelValue(body, "Datum", report.MeetingDate.ToString("dddd d MMMM yyyy"));
                if (!string.IsNullOrWhiteSpace(report.MeetingTime))
                    AddWordLabelValue(body, "Aanvang", report.MeetingTime + " uur");
                AddWordLabelValue(body, "Locatie", report.Location);
                AddWordLabelValue(body, "Opgesteld door", report.CreatedByUsername);

                // ── Attendees inline ──
                if (report.Attendees?.Any() == true)
                {
                    var presentList = FormatAttendeeList(report.Attendees, true);
                    var absentList = FormatAttendeeList(report.Attendees, false);

                    if (!string.IsNullOrEmpty(presentList))
                        AddWordLabelValue(body, "Aanwezig", presentList);
                    if (!string.IsNullOrEmpty(absentList))
                        AddWordLabelValue(body, "Verontschuldigd", absentList);
                }
                AddWordSpacer(body);

                // ── Agenda overview ──
                var agenda = ParseAgendaItems(report.AgendaItems);
                if (agenda.Count > 0)
                {
                    AddWordSectionHeader(body, "AGENDA");

                    for (int i = 0; i < agenda.Count; i++)
                    {
                        AddWordAgendaBar(body, $"{i + 1}.  {agenda[i].Title}");
                    }
                    AddWordSpacer(body);

                    // ── Detailed agenda items with notes ──
                    AddWordSectionHeader(body, "VERSLAG");

                    for (int i = 0; i < agenda.Count; i++)
                    {
                        AddWordAgendaBar(body, $"{i + 1}.  {agenda[i].Title}");
                        if (!string.IsNullOrWhiteSpace(agenda[i].Notes))
                        {
                            foreach (var line in agenda[i].Notes.Split('\n'))
                                AddWordParagraph(body, line, 10, false, MedGrayHex);
                        }
                        else
                        {
                            AddWordParagraph(body, "Geen notities.", 9, false, "999999");
                        }
                    }
                    AddWordSpacer(body);
                }

                // ── General notes ──
                if (!string.IsNullOrWhiteSpace(report.Notes))
                {
                    AddWordSectionHeader(body, "OPMERKINGEN");
                    foreach (var line in report.Notes.Split('\n'))
                        AddWordParagraph(body, line, 10, false, MedGrayHex);
                    AddWordSpacer(body);
                }

                mainPart.Document.Save();
            }
            return stream.ToArray();
        }

        // ── PDF export ──────────────────────────────────────────────────
        public byte[] ExportToPdf(BoardReport report, byte[]? logoData = null, string? logoContentType = null)
        {
            using var stream = new MemoryStream();
            var document = new Document(iTextSharp.text.PageSize.A4, 50, 50, 45, 40);
            var writer = PdfWriter.GetInstance(document, stream);
            document.Open();

            // Colors
            var accent = new BaseColor(27, 58, 92);       // #1B3A5C navy
            var accentLight = new BaseColor(232, 238, 244); // #E8EEF4
            var dark = new BaseColor(45, 45, 45);
            var medium = new BaseColor(90, 90, 90);

            // Fonts
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, accent);
            var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, dark);
            var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, medium);
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, accent);
            var agendaTitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, dark);
            var notesFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, medium);
            var emptyFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, new BaseColor(170, 170, 170));

            // ── Logo top-right ──
            if (logoData?.Length > 0)
            {
                try
                {
                    var headerTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 4 };
                    headerTable.SetWidths(new float[] { 70, 30 });
                    headerTable.AddCell(new PdfPCell { Border = iTextSharp.text.Rectangle.NO_BORDER, MinimumHeight = 10 });

                    var logo = iTextSharp.text.Image.GetInstance(logoData);
                    logo.ScaleToFit(180f, 90f);
                    var logoCell = new PdfPCell(logo)
                    {
                        Border = iTextSharp.text.Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        VerticalAlignment = Element.ALIGN_TOP,
                        PaddingBottom = 4
                    };
                    headerTable.AddCell(logoCell);
                    document.Add(headerTable);
                }
                catch { /* Logo rendering failed — continue without logo */ }
            }

            // ── Title + accent line ──
            document.Add(new iTextSharp.text.Paragraph(report.Title, titleFont)
                { SpacingAfter = 3 });

            var accentLine = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 14 };
            accentLine.AddCell(new PdfPCell
            {
                BackgroundColor = accent,
                FixedHeight = 2.5f,
                Border = iTextSharp.text.Rectangle.NO_BORDER
            });
            document.Add(accentLine);

            // ── Meeting details (clean label: value lines) ──
            var infoTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 4 };
            infoTable.SetWidths(new float[] { 22, 78 });

            AddPdfInfoRow(infoTable, "Datum", report.MeetingDate.ToString("dddd d MMMM yyyy"), labelFont, valueFont);
            if (!string.IsNullOrWhiteSpace(report.MeetingTime))
                AddPdfInfoRow(infoTable, "Aanvang", report.MeetingTime + " uur", labelFont, valueFont);
            AddPdfInfoRow(infoTable, "Locatie", report.Location, labelFont, valueFont);
            AddPdfInfoRow(infoTable, "Opgesteld door", report.CreatedByUsername, labelFont, valueFont);

            // ── Attendees inline ──
            if (report.Attendees?.Any() == true)
            {
                var presentList = FormatAttendeeList(report.Attendees, true);
                var absentList = FormatAttendeeList(report.Attendees, false);

                if (!string.IsNullOrEmpty(presentList))
                    AddPdfInfoRow(infoTable, "Aanwezig", presentList, labelFont, valueFont);
                if (!string.IsNullOrEmpty(absentList))
                    AddPdfInfoRow(infoTable, "Verontschuldigd", absentList, labelFont, valueFont);
            }
            document.Add(infoTable);
            document.Add(new iTextSharp.text.Paragraph(" ") { SpacingAfter = 6 });

            // ── Agenda overview ──
            var agenda = ParseAgendaItems(report.AgendaItems);
            if (agenda.Count > 0)
            {
                AddPdfSectionHeader(document, "AGENDA", sectionFont, accent);

                var summaryTable = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 12 };
                for (int i = 0; i < agenda.Count; i++)
                {
                    var cell = new PdfPCell(new Phrase($"  {i + 1}.   {agenda[i].Title}", agendaTitleFont))
                    {
                        BackgroundColor = accentLight,
                        Border = iTextSharp.text.Rectangle.NO_BORDER,
                        Padding = 7,
                        PaddingLeft = 14
                    };
                    summaryTable.AddCell(cell);
                    summaryTable.AddCell(new PdfPCell { Border = iTextSharp.text.Rectangle.NO_BORDER, FixedHeight = 2 });
                }
                document.Add(summaryTable);

                // ── Detailed agenda + notes ──
                AddPdfSectionHeader(document, "VERSLAG", sectionFont, accent);

                for (int i = 0; i < agenda.Count; i++)
                {
                    var item = agenda[i];

                    // Title bar
                    var barTable = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 3, SpacingAfter = 2 };
                    barTable.AddCell(new PdfPCell(new Phrase($"  {i + 1}.   {item.Title}", agendaTitleFont))
                    {
                        BackgroundColor = accentLight,
                        Border = iTextSharp.text.Rectangle.NO_BORDER,
                        Padding = 7,
                        PaddingLeft = 14
                    });
                    document.Add(barTable);

                    // Notes text
                    if (!string.IsNullOrWhiteSpace(item.Notes))
                    {
                        foreach (var line in item.Notes.Split('\n'))
                        {
                            document.Add(new iTextSharp.text.Paragraph(line, notesFont)
                                { SpacingAfter = 1, IndentationLeft = 18 });
                        }
                        document.Add(new iTextSharp.text.Paragraph(" ") { SpacingAfter = 4 });
                    }
                    else
                    {
                        document.Add(new iTextSharp.text.Paragraph("Geen notities.", emptyFont)
                            { SpacingAfter = 6, IndentationLeft = 18 });
                    }
                }
            }

            // ── General notes ──
            if (!string.IsNullOrWhiteSpace(report.Notes))
            {
                AddPdfSectionHeader(document, "OPMERKINGEN", sectionFont, accent);
                foreach (var line in report.Notes.Split('\n'))
                    document.Add(new iTextSharp.text.Paragraph(line, notesFont) { SpacingAfter = 2, IndentationLeft = 4 });
            }

            document.Close();
            return stream.ToArray();
        }

        // ── PDF helpers ─────────────────────────────────────────────────
        private static void AddPdfInfoRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            table.AddCell(new PdfPCell(new Phrase(label + ":", labelFont))
            {
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                PaddingTop = 3,
                PaddingBottom = 3,
                PaddingLeft = 2
            });
            table.AddCell(new PdfPCell(new Phrase(value, valueFont))
            {
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                PaddingTop = 3,
                PaddingBottom = 3
            });
        }

        private static void AddPdfSectionHeader(Document document, string text, Font font, BaseColor accentColor)
        {
            document.Add(new iTextSharp.text.Paragraph(text, font) { SpacingBefore = 10, SpacingAfter = 2 });
            var line = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 8 };
            line.AddCell(new PdfPCell { BackgroundColor = accentColor, FixedHeight = 1f, Border = iTextSharp.text.Rectangle.NO_BORDER });
            document.Add(line);
        }

        // ── Word helpers ────────────────────────────────────────────────
        private static void AddWordParagraph(Body body, string text, int fontSize, bool bold, string colorHex)
            => AddWordParagraph(body, text, fontSize, bold, colorHex, JustificationValues.Left);

        private static void AddWordParagraph(Body body, string text, int fontSize, bool bold, string colorHex, JustificationValues justify)
        {
            var run = new Run();
            var rp = new RunProperties();
            rp.Append(new FontSize { Val = (fontSize * 2).ToString() });
            rp.Append(new Color { Val = colorHex });
            if (bold) rp.Append(new Bold());
            run.Append(rp);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            var para = new Paragraph();
            para.Append(new ParagraphProperties(
                new Justification { Val = justify },
                new SpacingBetweenLines { After = "40" }));
            para.Append(run);
            body.InsertBefore(para, body.Descendants<SectionProperties>().FirstOrDefault());
        }

        private static void AddWordLabelValue(Body body, string label, string value)
        {
            var para = new Paragraph();
            para.Append(new ParagraphProperties(new SpacingBetweenLines { After = "20" }));

            var labelRun = new Run(
                new RunProperties(new Bold(), new FontSize { Val = "18" }, new Color { Val = DarkGrayHex }),
                new Text(label + ":  ") { Space = SpaceProcessingModeValues.Preserve });
            var valueRun = new Run(
                new RunProperties(new FontSize { Val = "18" }, new Color { Val = MedGrayHex }),
                new Text(value) { Space = SpaceProcessingModeValues.Preserve });

            para.Append(labelRun);
            para.Append(valueRun);
            body.InsertBefore(para, body.Descendants<SectionProperties>().FirstOrDefault());
        }

        private static void AddWordSectionHeader(Body body, string text)
        {
            var run = new Run(
                new RunProperties(new Bold(), new FontSize { Val = "22" }, new Color { Val = AccentHex }),
                new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            var para = new Paragraph();
            para.Append(new ParagraphProperties(
                new SpacingBetweenLines { Before = "240", After = "60" },
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Color = AccentHex, Size = 4, Space = 2 })));
            para.Append(run);
            body.InsertBefore(para, body.Descendants<SectionProperties>().FirstOrDefault());
        }

        private static void AddWordAgendaBar(Body body, string text)
        {
            var run = new Run();
            var rp = new RunProperties();
            rp.Append(new FontSize { Val = "20" });
            rp.Append(new Color { Val = DarkGrayHex });
            rp.Append(new Bold());
            run.Append(rp);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            var para = new Paragraph();
            var paraProps = new ParagraphProperties();
            paraProps.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = AccentLightHex });
            paraProps.Append(new SpacingBetweenLines { Before = "80", After = "40" });
            para.Append(paraProps);
            para.Append(run);
            body.InsertBefore(para, body.Descendants<SectionProperties>().FirstOrDefault());
        }

        private static void AddWordSpacer(Body body)
        {
            var para = new Paragraph(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));
            para.Append(new ParagraphProperties(new SpacingBetweenLines { After = "80" }));
            body.InsertBefore(para, body.Descendants<SectionProperties>().FirstOrDefault());
        }

        private static DocumentFormat.OpenXml.Wordprocessing.Drawing CreateWordImage(string relationshipId, long cx, long cy)
        {
            var element = new DocumentFormat.OpenXml.Wordprocessing.Drawing(
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = cx, Cy = cy },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1U, Name = "Logo" },
                    new DocumentFormat.OpenXml.Drawing.Graphic(
                        new DocumentFormat.OpenXml.Drawing.GraphicData(
                            new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties { Id = 0U, Name = "logo.png" },
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                                new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                    new DocumentFormat.OpenXml.Drawing.Blip { Embed = relationshipId },
                                    new DocumentFormat.OpenXml.Drawing.Stretch(new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                                new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                    new DocumentFormat.OpenXml.Drawing.Transform2D(
                                        new DocumentFormat.OpenXml.Drawing.Offset { X = 0, Y = 0 },
                                        new DocumentFormat.OpenXml.Drawing.Extents { Cx = cx, Cy = cy }),
                                    new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                        new DocumentFormat.OpenXml.Drawing.AdjustValueList()) { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }))
                        ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                ) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

            return element;
        }
    }
}
