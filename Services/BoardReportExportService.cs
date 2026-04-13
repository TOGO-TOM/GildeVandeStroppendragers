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
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using Document = iTextSharp.text.Document;
using Font = iTextSharp.text.Font;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;

namespace AdminMembers.Services
{
    public class BoardReportExportService
    {
        // B&W color scheme
        private const string BlackHex = "000000";
        private const string DarkGrayHex = "333333";
        private const string MedGrayHex = "666666";
        private const string LightGrayHex = "F0F0F0";
        private const string WhiteHex = "FFFFFF";
        private const string AgendaBarHex = "D6E4F0"; // light blue bar for agenda items

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
                // Fallback: old newline-separated format
                return agendaJson.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => new AgendaEntry(l.Trim(), "")).ToList();
            }
        }

        // ── Word (.docx) export ─────────────────────────────────────────
        public byte[] ExportToWord(BoardReport report, byte[]? logoData = null)
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Page margins
                var sectionProps = new SectionProperties(
                    new PageMargin { Top = 1134, Bottom = 1134, Left = 1134u, Right = 1134u });
                body.AppendChild(sectionProps);

                // Logo in top-right
                if (logoData?.Length > 0)
                {
                    try
                    {
                        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
                        using (var ms = new MemoryStream(logoData))
                            imagePart.FeedData(ms);

                        var imageId = mainPart.GetIdOfPart(imagePart);
                        var drawing = CreateWordImage(imageId, 1800000, 900000); // ~1.8cm x 0.9cm
                        var imgRun = new Run(drawing);
                        var imgPara = new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Right }));
                        imgPara.Append(imgRun);
                        body.InsertBefore(imgPara, body.Descendants<SectionProperties>().FirstOrDefault());
                    }
                    catch { /* skip bad logo */ }
                }

                // Title
                AddWordParagraph(body, report.Title, 24, true, BlackHex, JustificationValues.Center);
                AddWordParagraph(body, "Bestuursverslag", 12, false, MedGrayHex, JustificationValues.Center);
                AddWordSpacer(body);

                // Meeting details table
                var detailsTable = CreateWordTable(body);
                AddWordDetailRow(detailsTable, "Datum", report.MeetingDate.ToString("dddd d MMMM yyyy"));
                AddWordDetailRow(detailsTable, "Locatie", report.Location);
                AddWordDetailRow(detailsTable, "Status", report.Status);
                AddWordDetailRow(detailsTable, "Opgesteld door", report.CreatedByUsername);
                AddWordSpacer(body);

                // Attendees
                if (report.Attendees?.Any() == true)
                {
                    AddWordParagraph(body, "Aanwezigen", 14, true, BlackHex);

                    var attendeeTable = CreateWordTable(body);
                    AddWordHeaderRow(attendeeTable, "Naam", "Status");

                    foreach (var a in report.Attendees.OrderBy(x => x.Member?.FirstName))
                    {
                        var name = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : $"Lid #{a.MemberId}";
                        AddWordDataRow(attendeeTable, name, a.IsPresent ? "Aanwezig" : "Afwezig");
                    }
                    AddWordSpacer(body);
                }

                // Agenda items — light blue bars with notes
                var agenda = ParseAgendaItems(report.AgendaItems);
                if (agenda.Count > 0)
                {
                    AddWordParagraph(body, "Agendapunten", 14, true, BlackHex);

                    // Summary list of all items
                    int idx = 1;
                    foreach (var item in agenda)
                    {
                        AddWordAgendaBar(body, $"{idx}. {item.Title}");
                        idx++;
                    }
                    AddWordSpacer(body);

                    // Detailed items with notes
                    bool hasAnyNotes = agenda.Any(a => !string.IsNullOrWhiteSpace(a.Notes));
                    if (hasAnyNotes)
                    {
                        AddWordParagraph(body, "Verslag per agendapunt", 14, true, BlackHex);
                        idx = 1;
                        foreach (var item in agenda)
                        {
                            AddWordAgendaBar(body, $"{idx}. {item.Title}");
                            if (!string.IsNullOrWhiteSpace(item.Notes))
                            {
                                foreach (var line in item.Notes.Split('\n'))
                                    AddWordParagraph(body, line, 10, false, DarkGrayHex);
                            }
                            else
                            {
                                AddWordParagraph(body, "—", 10, false, MedGrayHex);
                            }
                            idx++;
                        }
                        AddWordSpacer(body);
                    }
                }

                // Notes
                if (!string.IsNullOrWhiteSpace(report.Notes))
                {
                    AddWordParagraph(body, "Opmerkingen", 14, true, BlackHex);
                    foreach (var line in report.Notes.Split('\n'))
                        AddWordParagraph(body, line, 10, false, MedGrayHex);
                }

                // Footer
                AddWordSpacer(body);
                AddWordParagraph(body, $"Geëxporteerd op {DateTime.Now:dd/MM/yyyy HH:mm}", 8, false, "999999", JustificationValues.Center);

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        // ── PDF export ──────────────────────────────────────────────────
        public byte[] ExportToPdf(BoardReport report, byte[]? logoData = null)
        {
            using var stream = new MemoryStream();
            var document = new Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);
            var writer = PdfWriter.GetInstance(document, stream);
            document.Open();

            // Colors
            var black = BaseColor.Black;
            var darkGray = new BaseColor(51, 51, 51);
            var medGray = new BaseColor(102, 102, 102);
            var lightGray = new BaseColor(240, 240, 240);
            var lightBlue = new BaseColor(214, 228, 240); // #D6E4F0

            // Fonts
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22, black);
            var subFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, medGray);
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, black);
            var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, darkGray);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, darkGray);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(153, 153, 153));
            var agendaTitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, black);
            var notesFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, darkGray);

            // ── Header row: title left, logo right ──
            if (logoData?.Length > 0)
            {
                try
                {
                    var headerTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 8 };
                    headerTable.SetWidths(new float[] { 75, 25 });

                    // Left cell: empty (title goes below)
                    headerTable.AddCell(new PdfPCell { Border = iTextSharp.text.Rectangle.NO_BORDER });

                    // Right cell: logo
                    var logo = iTextSharp.text.Image.GetInstance(logoData);
                    logo.ScaleToFit(120f, 65f);
                    var logoCell = new PdfPCell(logo)
                    {
                        Border = iTextSharp.text.Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        PaddingBottom = 8
                    };
                    headerTable.AddCell(logoCell);
                    document.Add(headerTable);
                }
                catch { /* skip bad logo */ }
            }

            // Title
            document.Add(new iTextSharp.text.Paragraph(report.Title, titleFont)
                { Alignment = Element.ALIGN_CENTER, SpacingAfter = 2 });
            document.Add(new iTextSharp.text.Paragraph("Bestuursverslag", subFont)
                { Alignment = Element.ALIGN_CENTER, SpacingAfter = 12 });

            // Decorative line
            var lineTable = new PdfPTable(1) { WidthPercentage = 40, SpacingAfter = 18, HorizontalAlignment = Element.ALIGN_CENTER };
            lineTable.AddCell(new PdfPCell { BackgroundColor = darkGray, FixedHeight = 1f, Border = iTextSharp.text.Rectangle.NO_BORDER });
            document.Add(lineTable);

            // ── Meeting details (bordered box) ──
            var detailTable = new PdfPTable(2) { WidthPercentage = 60, SpacingAfter = 18, HorizontalAlignment = Element.ALIGN_LEFT };
            detailTable.SetWidths(new float[] { 35, 65 });

            AddPdfDetailRow(detailTable, "Datum:", report.MeetingDate.ToString("dddd d MMMM yyyy"), boldFont, bodyFont);
            AddPdfDetailRow(detailTable, "Locatie:", report.Location, boldFont, bodyFont);
            AddPdfDetailRow(detailTable, "Status:", report.Status, boldFont, bodyFont);
            AddPdfDetailRow(detailTable, "Opgesteld door:", report.CreatedByUsername, boldFont, bodyFont);
            document.Add(detailTable);

            // ── Attendees ──
            if (report.Attendees?.Any() == true)
            {
                document.Add(new iTextSharp.text.Paragraph("Aanwezigen", sectionFont) { SpacingBefore = 6, SpacingAfter = 8 });

                var attTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 14 };
                attTable.SetWidths(new float[] { 70, 30 });

                var thFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.White);
                attTable.AddCell(new PdfPCell(new Phrase("Naam", thFont)) { BackgroundColor = darkGray, Padding = 6, BorderColor = darkGray });
                attTable.AddCell(new PdfPCell(new Phrase("Status", thFont)) { BackgroundColor = darkGray, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, BorderColor = darkGray });

                bool alt = false;
                foreach (var a in report.Attendees.OrderBy(x => x.Member?.FirstName))
                {
                    var bg = alt ? lightGray : BaseColor.White;
                    var name = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : $"Lid #{a.MemberId}";
                    var statusLabel = a.IsPresent ? "Aanwezig" : "Afwezig";

                    attTable.AddCell(new PdfPCell(new Phrase(name, bodyFont)) { BackgroundColor = bg, Padding = 5, BorderColor = lightGray });
                    attTable.AddCell(new PdfPCell(new Phrase(statusLabel, boldFont))
                        { BackgroundColor = bg, Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER, BorderColor = lightGray });

                    alt = !alt;
                }
                document.Add(attTable);
            }

            // ── Agenda items — light blue bars with notes underneath ──
            var agenda = ParseAgendaItems(report.AgendaItems);
            if (agenda.Count > 0)
            {
                document.Add(new iTextSharp.text.Paragraph("Agendapunten", sectionFont) { SpacingBefore = 8, SpacingAfter = 8 });

                // First: summary overview of all agenda points
                var summaryTable = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 14 };
                for (int i = 0; i < agenda.Count; i++)
                {
                    var summaryCell = new PdfPCell(new Phrase($"  {i + 1}. {agenda[i].Title}", agendaTitleFont))
                    {
                        BackgroundColor = lightBlue,
                        Border = iTextSharp.text.Rectangle.NO_BORDER,
                        Padding = 7,
                        PaddingLeft = 12
                    };
                    summaryTable.AddCell(summaryCell);
                    // Tiny spacer between items
                    var spacerCell = new PdfPCell { Border = iTextSharp.text.Rectangle.NO_BORDER, FixedHeight = 3 };
                    summaryTable.AddCell(spacerCell);
                }
                document.Add(summaryTable);

                // Then: detailed agenda items with notes
                bool hasAnyNotes = agenda.Any(a => !string.IsNullOrWhiteSpace(a.Notes));
                if (hasAnyNotes)
                {
                    document.Add(new iTextSharp.text.Paragraph("Verslag per agendapunt", sectionFont) { SpacingBefore = 6, SpacingAfter = 8 });

                    for (int i = 0; i < agenda.Count; i++)
                    {
                        var item = agenda[i];

                        // Agenda item title bar
                        var barTable = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 4, SpacingAfter = 2 };
                        var barCell = new PdfPCell(new Phrase($"  {i + 1}. {item.Title}", agendaTitleFont))
                        {
                            BackgroundColor = lightBlue,
                            Border = iTextSharp.text.Rectangle.NO_BORDER,
                            Padding = 7,
                            PaddingLeft = 12
                        };
                        barTable.AddCell(barCell);
                        document.Add(barTable);

                        // Notes text underneath
                        if (!string.IsNullOrWhiteSpace(item.Notes))
                        {
                            foreach (var line in item.Notes.Split('\n'))
                            {
                                document.Add(new iTextSharp.text.Paragraph($"     {line}", notesFont) { SpacingAfter = 2, IndentationLeft = 16 });
                            }
                            document.Add(new iTextSharp.text.Paragraph(" ") { SpacingAfter = 4 });
                        }
                        else
                        {
                            document.Add(new iTextSharp.text.Paragraph("     —", FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, medGray)) { SpacingAfter = 4, IndentationLeft = 16 });
                        }
                    }
                }
            }

            // ── General Notes ──
            if (!string.IsNullOrWhiteSpace(report.Notes))
            {
                document.Add(new iTextSharp.text.Paragraph("Opmerkingen", sectionFont) { SpacingBefore = 14, SpacingAfter = 6 });
                foreach (var line in report.Notes.Split('\n'))
                    document.Add(new iTextSharp.text.Paragraph(line, FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, medGray)) { SpacingAfter = 2, IndentationLeft = 8 });
            }

            // ── Footer ──
            document.Add(new iTextSharp.text.Paragraph(" ") { SpacingAfter = 20 });
            var footerLine = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 6 };
            footerLine.AddCell(new PdfPCell { BackgroundColor = lightGray, FixedHeight = 1, Border = iTextSharp.text.Rectangle.NO_BORDER });
            document.Add(footerLine);
            document.Add(new iTextSharp.text.Paragraph($"Geëxporteerd op {DateTime.Now:dd/MM/yyyy HH:mm}", smallFont)
                { Alignment = Element.ALIGN_CENTER });

            document.Close();
            return stream.ToArray();
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
            para.Append(new ParagraphProperties(new Justification { Val = justify }));
            para.Append(run);
            body.InsertBefore(para, body.Descendants<SectionProperties>().FirstOrDefault());
        }

        private static void AddWordAgendaBar(Body body, string text)
        {
            var run = new Run();
            var rp = new RunProperties();
            rp.Append(new FontSize { Val = "22" });
            rp.Append(new Color { Val = BlackHex });
            rp.Append(new Bold());
            run.Append(rp);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            var para = new Paragraph();
            var paraProps = new ParagraphProperties();
            paraProps.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = AgendaBarHex });
            paraProps.Append(new SpacingBetweenLines { Before = "120", After = "60" });
            para.Append(paraProps);
            para.Append(run);
            body.InsertBefore(para, body.Descendants<SectionProperties>().FirstOrDefault());
        }

        private static void AddWordSpacer(Body body)
        {
            var para = new Paragraph(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));
            body.InsertBefore(para, body.Descendants<SectionProperties>().FirstOrDefault());
        }

        private static Table CreateWordTable(Body body)
        {
            var table = new Table();
            var tblProps = new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 4 }));
            table.Append(tblProps);
            body.InsertBefore(table, body.Descendants<SectionProperties>().FirstOrDefault());
            return table;
        }

        private static void AddWordDetailRow(Table table, string label, string value)
        {
            var row = new TableRow();

            var labelCell = new TableCell();
            var labelRun = new Run(new RunProperties(new Bold(), new FontSize { Val = "20" }, new Color { Val = BlackHex }), new Text(label));
            labelCell.Append(new Paragraph(labelRun));
            labelCell.Append(new TableCellProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = LightGrayHex }));

            var valueCell = new TableCell();
            var valueRun = new Run(new RunProperties(new FontSize { Val = "20" }, new Color { Val = DarkGrayHex }), new Text(value));
            valueCell.Append(new Paragraph(valueRun));

            row.Append(labelCell, valueCell);
            table.Append(row);
        }

        private static void AddWordHeaderRow(Table table, params string[] headers)
        {
            var row = new TableRow();
            foreach (var h in headers)
            {
                var cell = new TableCell();
                var run = new Run(new RunProperties(new Bold(), new FontSize { Val = "18" }, new Color { Val = WhiteHex }), new Text(h));
                cell.Append(new Paragraph(run));
                cell.Append(new TableCellProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = BlackHex }));
                row.Append(cell);
            }
            table.Append(row);
        }

        private static void AddWordDataRow(Table table, params string[] values)
        {
            var row = new TableRow();
            foreach (var v in values)
            {
                var cell = new TableCell();
                var run = new Run(new RunProperties(new FontSize { Val = "20" }, new Color { Val = DarkGrayHex }), new Text(v));
                cell.Append(new Paragraph(run));
                row.Append(cell);
            }
            table.Append(row);
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

        // ── PDF helpers ─────────────────────────────────────────────────
        private static void AddPdfDetailRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            table.AddCell(new PdfPCell(new Phrase(label, labelFont))
                { Border = iTextSharp.text.Rectangle.NO_BORDER, Padding = 4, BackgroundColor = new BaseColor(240, 240, 240) });
            table.AddCell(new PdfPCell(new Phrase(value, valueFont))
                { Border = iTextSharp.text.Rectangle.NO_BORDER, Padding = 4 });
        }
    }
}
