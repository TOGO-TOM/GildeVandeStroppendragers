using AdminMembers.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iTextSharp.text;
using iTextSharp.text.pdf;
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
        private const string AccentHex = "4F46E5";
        private const string LightGrayHex = "F1F5F9";
        private const string WhiteHex = "FFFFFF";
        private const string TextDarkHex = "1E293B";

        // ── Word (.docx) export ─────────────────────────────────────────
        public byte[] ExportToWord(BoardReport report)
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

                // Title
                AddWordParagraph(body, report.Title, 28, true, AccentHex, JustificationValues.Center);

                // Subtitle: "Bestuursverslag"
                AddWordParagraph(body, "Bestuursverslag", 14, false, "64748B", JustificationValues.Center);
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
                    AddWordParagraph(body, "Aanwezigen", 16, true, AccentHex);

                    var attendeeTable = CreateWordTable(body);
                    AddWordHeaderRow(attendeeTable, "Naam", "Status");

                    foreach (var a in report.Attendees.OrderBy(x => x.Member?.FirstName))
                    {
                        var name = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : $"Lid #{a.MemberId}";
                        AddWordDataRow(attendeeTable, name, a.IsPresent ? "Aanwezig" : "Afwezig");
                    }
                    AddWordSpacer(body);
                }

                // Agenda
                if (!string.IsNullOrWhiteSpace(report.AgendaItems))
                {
                    AddWordParagraph(body, "Agendapunten", 16, true, AccentHex);
                    var items = report.AgendaItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    int idx = 1;
                    foreach (var item in items)
                    {
                        AddWordParagraph(body, $"{idx}. {item.Trim()}", 11, false, TextDarkHex);
                        idx++;
                    }
                    AddWordSpacer(body);
                }

                // Content
                if (!string.IsNullOrWhiteSpace(report.Content))
                {
                    AddWordParagraph(body, "Verslag", 16, true, AccentHex);
                    foreach (var line in report.Content.Split('\n'))
                    {
                        AddWordParagraph(body, line, 11, false, TextDarkHex);
                    }
                    AddWordSpacer(body);
                }

                // Notes
                if (!string.IsNullOrWhiteSpace(report.Notes))
                {
                    AddWordParagraph(body, "Opmerkingen", 16, true, AccentHex);
                    foreach (var line in report.Notes.Split('\n'))
                    {
                        AddWordParagraph(body, line, 11, false, "64748B");
                    }
                }

                // Footer
                AddWordSpacer(body);
                AddWordParagraph(body, $"Geëxporteerd op {DateTime.Now:dd/MM/yyyy HH:mm}", 9, false, "94A3B8", JustificationValues.Center);

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        // ── PDF export ──────────────────────────────────────────────────
        public byte[] ExportToPdf(BoardReport report)
        {
            using var stream = new MemoryStream();
            var document = new Document(iTextSharp.text.PageSize.A4, 40, 40, 50, 40);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            var accent = new BaseColor(79, 70, 229);
            var textDark = new BaseColor(30, 41, 59);
            var textMuted = new BaseColor(100, 116, 139);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22, accent);
            var subFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, textMuted);
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, accent);
            var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, textDark);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, textDark);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(148, 163, 184));

            // Title
            document.Add(new iTextSharp.text.Paragraph(report.Title, headerFont)
                { Alignment = Element.ALIGN_CENTER, SpacingAfter = 4 });
            document.Add(new iTextSharp.text.Paragraph("Bestuursverslag", subFont)
                { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });

            // Accent line
            var lineTable = new PdfPTable(1) { WidthPercentage = 40, SpacingAfter = 16, HorizontalAlignment = Element.ALIGN_CENTER };
            lineTable.AddCell(new PdfPCell { BackgroundColor = accent, FixedHeight = 3, Border = iTextSharp.text.Rectangle.NO_BORDER });
            document.Add(lineTable);

            // Meeting details
            var detailTable = new PdfPTable(2) { WidthPercentage = 70, SpacingAfter = 16, HorizontalAlignment = Element.ALIGN_CENTER };
            detailTable.SetWidths(new float[] { 30, 70 });

            AddPdfDetailRow(detailTable, "Datum:", report.MeetingDate.ToString("dddd d MMMM yyyy"), boldFont, bodyFont);
            AddPdfDetailRow(detailTable, "Locatie:", report.Location, boldFont, bodyFont);
            AddPdfDetailRow(detailTable, "Status:", report.Status, boldFont, bodyFont);
            AddPdfDetailRow(detailTable, "Opgesteld door:", report.CreatedByUsername, boldFont, bodyFont);
            document.Add(detailTable);

            // Attendees
            if (report.Attendees?.Any() == true)
            {
                document.Add(new iTextSharp.text.Paragraph("Aanwezigen", sectionFont) { SpacingBefore = 12, SpacingAfter = 8 });

                var attTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 12 };
                attTable.SetWidths(new float[] { 70, 30 });

                var thFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.White);
                attTable.AddCell(new PdfPCell(new Phrase("Naam", thFont)) { BackgroundColor = accent, Padding = 6 });
                attTable.AddCell(new PdfPCell(new Phrase("Status", thFont)) { BackgroundColor = accent, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });

                bool alt = false;
                foreach (var a in report.Attendees.OrderBy(x => x.Member?.FirstName))
                {
                    var bg = alt ? new BaseColor(241, 245, 249) : BaseColor.White;
                    var name = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : $"Lid #{a.MemberId}";
                    var statusLabel = a.IsPresent ? "Aanwezig" : "Afwezig";
                    var statusColor = a.IsPresent ? new BaseColor(220, 252, 231) : new BaseColor(254, 226, 226);

                    attTable.AddCell(new PdfPCell(new Phrase(name, bodyFont)) { BackgroundColor = bg, Padding = 5 });
                    attTable.AddCell(new PdfPCell(new Phrase(statusLabel, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, textDark)))
                        { BackgroundColor = statusColor, Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });

                    alt = !alt;
                }
                document.Add(attTable);
            }

            // Agenda
            if (!string.IsNullOrWhiteSpace(report.AgendaItems))
            {
                document.Add(new iTextSharp.text.Paragraph("Agendapunten", sectionFont) { SpacingBefore = 12, SpacingAfter = 6 });
                var items = report.AgendaItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                int idx = 1;
                foreach (var item in items)
                {
                    document.Add(new iTextSharp.text.Paragraph($"  {idx}. {item.Trim()}", bodyFont) { SpacingAfter = 2 });
                    idx++;
                }
            }

            // Content
            if (!string.IsNullOrWhiteSpace(report.Content))
            {
                document.Add(new iTextSharp.text.Paragraph("Verslag", sectionFont) { SpacingBefore = 14, SpacingAfter = 6 });
                foreach (var line in report.Content.Split('\n'))
                {
                    document.Add(new iTextSharp.text.Paragraph(line, bodyFont) { SpacingAfter = 3 });
                }
            }

            // Notes
            if (!string.IsNullOrWhiteSpace(report.Notes))
            {
                document.Add(new iTextSharp.text.Paragraph("Opmerkingen", sectionFont) { SpacingBefore = 14, SpacingAfter = 6 });
                foreach (var line in report.Notes.Split('\n'))
                {
                    document.Add(new iTextSharp.text.Paragraph(line, FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 10, textMuted)) { SpacingAfter = 2 });
                }
            }

            // Footer
            document.Add(new iTextSharp.text.Paragraph($"Geëxporteerd op {DateTime.Now:dd/MM/yyyy HH:mm}", smallFont)
                { SpacingBefore = 24, Alignment = Element.ALIGN_CENTER });

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
                    new TopBorder { Val = BorderValues.Single, Color = "E2E8F0", Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Color = "E2E8F0", Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Color = "E2E8F0", Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Color = "E2E8F0", Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Color = "E2E8F0", Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Color = "E2E8F0", Size = 4 }));
            table.Append(tblProps);
            body.InsertBefore(table, body.Descendants<SectionProperties>().FirstOrDefault());
            return table;
        }

        private static void AddWordDetailRow(Table table, string label, string value)
        {
            var row = new TableRow();

            var labelCell = new TableCell();
            var labelRun = new Run(new RunProperties(new Bold(), new FontSize { Val = "20" }, new Color { Val = AccentHex }), new Text(label));
            labelCell.Append(new Paragraph(labelRun));
            labelCell.Append(new TableCellProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = LightGrayHex }));

            var valueCell = new TableCell();
            var valueRun = new Run(new RunProperties(new FontSize { Val = "20" }, new Color { Val = TextDarkHex }), new Text(value));
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
                cell.Append(new TableCellProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = AccentHex }));
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
                var run = new Run(new RunProperties(new FontSize { Val = "20" }, new Color { Val = TextDarkHex }), new Text(v));
                cell.Append(new Paragraph(run));
                row.Append(cell);
            }
            table.Append(row);
        }

        // ── PDF helpers ─────────────────────────────────────────────────
        private static void AddPdfDetailRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            table.AddCell(new PdfPCell(new Phrase(label, labelFont))
                { Border = iTextSharp.text.Rectangle.NO_BORDER, Padding = 4, BackgroundColor = new BaseColor(241, 245, 249) });
            table.AddCell(new PdfPCell(new Phrase(value, valueFont))
                { Border = iTextSharp.text.Rectangle.NO_BORDER, Padding = 4 });
        }
    }
}
