using AdminMembers.Models;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace AdminMembers.Services
{
    public class StockExportService
    {
        // ?? Excel ?????????????????????????????????????????????????????????
        public byte[] ExportToExcel(List<StockItem> items, string? companyName)
        {
            using var workbook = new XLWorkbook();

            // Sheet 1: Stock overview
            var ws = workbook.Worksheets.Add("Stock");

            var headerColor = XLColor.FromHtml("#4f46e5");
            string[] headers = { "Name", "Category", "Description", "Unit", "Current Stock", "Minimum Stock", "Status" };

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = headerColor;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var item in items.OrderBy(i => i.Category).ThenBy(i => i.Name))
            {
                ws.Cell(row, 1).Value = item.Name;
                ws.Cell(row, 2).Value = item.Category ?? "";
                ws.Cell(row, 3).Value = item.Description ?? "";
                ws.Cell(row, 4).Value = item.Unit;
                ws.Cell(row, 5).Value = (double)item.CurrentStock;
                ws.Cell(row, 6).Value = item.MinimumStock.HasValue ? (double)item.MinimumStock.Value : 0;
                ws.Cell(row, 7).Value = item.Status.ToString();

                // Colour-code status
                var statusCell = ws.Cell(row, 7);
                statusCell.Style.Fill.BackgroundColor = item.Status switch
                {
                    StockStatus.Out => XLColor.FromHtml("#fee2e2"),
                    StockStatus.Low => XLColor.FromHtml("#ffedd5"),
                    _               => XLColor.FromHtml("#dcfce7")
                };

                // Highlight low / out rows
                if (item.Status != StockStatus.Ok)
                {
                    ws.Cell(row, 1).Style.Font.Bold = true;
                }

                row++;
            }

            ws.Columns().AdjustToContents();

            // Sheet 2: Categories summary
            var wsCat = workbook.Worksheets.Add("Per Categorie");
            string[] catHeaders = { "Category", "Items", "Total Stock" };
            for (int c = 0; c < catHeaders.Length; c++)
            {
                var cell = wsCat.Cell(1, c + 1);
                cell.Value = catHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = headerColor;
                cell.Style.Font.FontColor = XLColor.White;
            }

            int catRow = 2;
            foreach (var grp in items.GroupBy(i => i.Category ?? "Overig").OrderBy(g => g.Key))
            {
                wsCat.Cell(catRow, 1).Value = grp.Key;
                wsCat.Cell(catRow, 2).Value = grp.Count();
                wsCat.Cell(catRow, 3).Value = (double)grp.Sum(i => i.CurrentStock);
                catRow++;
            }
            wsCat.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ?? CSV ???????????????????????????????????????????????????????????
        public byte[] ExportToCsv(List<StockItem> items)
        {
            var csv = new StringBuilder();
            csv.Append('\ufeff'); // UTF-8 BOM

            csv.AppendLine("\"Name\",\"Category\",\"Description\",\"Unit\",\"Current Stock\",\"Minimum Stock\",\"Status\"");

            foreach (var item in items.OrderBy(i => i.Category).ThenBy(i => i.Name))
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    $"\"{Esc(item.Name)}\"",
                    $"\"{Esc(item.Category ?? "")}\"",
                    $"\"{Esc(item.Description ?? "")}\"",
                    $"\"{Esc(item.Unit)}\"",
                    item.CurrentStock.ToString("G29"),
                    item.MinimumStock?.ToString("G29") ?? "",
                    $"\"{item.Status}\""
                }));
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        // ?? PDF ???????????????????????????????????????????????????????????
        public byte[] ExportToPdf(List<StockItem> items, byte[]? logoData, string? companyName)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 30, 30, 40, 30);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            var accentColor = new BaseColor(79, 70, 229);  // indigo-600

            // Logo
            if (logoData?.Length > 0)
            {
                try
                {
                    var logo = iTextSharp.text.Image.GetInstance(logoData);
                    logo.ScaleToFit(100f, 50f);
                    logo.Alignment = Element.ALIGN_CENTER;
                    logo.SpacingAfter = 8;
                    document.Add(logo);
                }
                catch { /* skip bad logo */ }
            }

            // Title
            document.Add(new Paragraph(companyName ?? "Stock Overzicht",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, accentColor))
            { Alignment = Element.ALIGN_CENTER, SpacingAfter = 4 });

            document.Add(new Paragraph($"Ge\u00ebxporteerd op: {DateTime.Now:dd/MM/yyyy HH:mm}",
                FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(130, 130, 130)))
            { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });

            // Summary row
            var total   = items.Count;
            var lowCnt  = items.Count(i => i.Status == StockStatus.Low);
            var outCnt  = items.Count(i => i.Status == StockStatus.Out);

            var summaryTable = new PdfPTable(3) { WidthPercentage = 60, SpacingAfter = 20, HorizontalAlignment = Element.ALIGN_CENTER };
            AddSummaryCell(summaryTable, total.ToString(),  "Totaal",       new BaseColor(79, 70, 229));
            AddSummaryCell(summaryTable, lowCnt.ToString(), "Laag",         new BaseColor(194, 65, 12));
            AddSummaryCell(summaryTable, outCnt.ToString(), "Uitverkocht",  new BaseColor(185, 28, 28));
            document.Add(summaryTable);

            // Per category
            foreach (var grp in items.GroupBy(i => i.Category ?? "Overig").OrderBy(g => g.Key))
            {
                // Category header
                document.Add(new Paragraph(grp.Key,
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, accentColor))
                { SpacingBefore = 10, SpacingAfter = 4 });

                var table = new PdfPTable(5) { WidthPercentage = 100, SpacingAfter = 6 };
                table.SetWidths(new float[] { 30, 12, 12, 14, 10 });

                // Header row
                var hFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(255, 255, 255));
                foreach (var h in new[] { "Naam", "Hoeveelheid", "Minimum", "Eenheid", "Status" })
                    table.AddCell(new PdfPCell(new Phrase(h, hFont))
                    { BackgroundColor = accentColor, Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });

                // Data rows
                var dFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                bool alt = false;
                foreach (var item in grp.OrderBy(i => i.Name))
                {
                    var bg = alt ? new BaseColor(248, 248, 252) : new BaseColor(255, 255, 255);
                    var statusBg = item.Status switch
                    {
                        StockStatus.Out => new BaseColor(254, 226, 226),
                        StockStatus.Low => new BaseColor(255, 237, 213),
                        _               => new BaseColor(220, 252, 231)
                    };
                    var statusLabel = item.Status switch
                    {
                        StockStatus.Out => "Uitverkocht",
                        StockStatus.Low => "Laag",
                        _               => "OK"
                    };

                    AddDataCell(table, item.Name,                          dFont, bg);
                    AddDataCell(table, item.CurrentStock.ToString("G29"),  dFont, bg, Element.ALIGN_RIGHT);
                    AddDataCell(table, item.MinimumStock?.ToString("G29") ?? "-", dFont, bg, Element.ALIGN_RIGHT);
                    AddDataCell(table, item.Unit,                          dFont, bg, Element.ALIGN_CENTER);
                    table.AddCell(new PdfPCell(new Phrase(statusLabel, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)))
                    { BackgroundColor = statusBg, Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });

                    alt = !alt;
                }

                document.Add(table);
            }

            document.Close();
            return stream.ToArray();
        }

        // ?? PDF with movements ????????????????????????????????????????????
        public byte[] ExportToPdfWithMovements(List<StockItem> items, byte[]? logoData, string? companyName)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 30, 30, 40, 30);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            var accentColor = new BaseColor(79, 70, 229);
            var mutedColor  = new BaseColor(130, 130, 130);

            // Logo
            if (logoData?.Length > 0)
            {
                try
                {
                    var logo = iTextSharp.text.Image.GetInstance(logoData);
                    logo.ScaleToFit(100f, 50f);
                    logo.Alignment = Element.ALIGN_CENTER;
                    logo.SpacingAfter = 8;
                    document.Add(logo);
                }
                catch { /* skip bad logo */ }
            }

            document.Add(new Paragraph(companyName ?? "Stock Overzicht",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, accentColor))
            { Alignment = Element.ALIGN_CENTER, SpacingAfter = 4 });

            document.Add(new Paragraph($"Ge\u00ebxporteerd op: {DateTime.Now:dd/MM/yyyy HH:mm} \u2014 inclusief bewegingsgeschiedenis",
                FontFactory.GetFont(FontFactory.HELVETICA, 9, mutedColor))
            { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });

            // Summary
            var summaryTable = new PdfPTable(3) { WidthPercentage = 60, SpacingAfter = 20, HorizontalAlignment = Element.ALIGN_CENTER };
            AddSummaryCell(summaryTable, items.Count.ToString(),                             "Totaal",       new BaseColor(79, 70, 229));
            AddSummaryCell(summaryTable, items.Count(i => i.Status == StockStatus.Low).ToString(),  "Laag",  new BaseColor(194, 65, 12));
            AddSummaryCell(summaryTable, items.Count(i => i.Status == StockStatus.Out).ToString(),  "Uitverkocht", new BaseColor(185, 28, 28));
            document.Add(summaryTable);

            var hFont  = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(255, 255, 255));
            var dFont  = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            var mvFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
            var mvHFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, new BaseColor(255, 255, 255));
            var mvHeaderColor = new BaseColor(107, 99, 200);

            foreach (var grp in items.GroupBy(i => i.Category ?? "Overig").OrderBy(g => g.Key))
            {
                document.Add(new Paragraph(grp.Key,
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, accentColor))
                { SpacingBefore = 12, SpacingAfter = 4 });

                foreach (var item in grp.OrderBy(i => i.Name))
                {
                    var statusBg = item.Status switch
                    {
                        StockStatus.Out => new BaseColor(254, 226, 226),
                        StockStatus.Low => new BaseColor(255, 237, 213),
                        _               => new BaseColor(220, 252, 231)
                    };
                    var statusLabel = item.Status switch
                    {
                        StockStatus.Out => "Uitverkocht",
                        StockStatus.Low => "Laag",
                        _               => "OK"
                    };

                    // Item header row
                    var itemTable = new PdfPTable(5) { WidthPercentage = 100, SpacingAfter = 2 };
                    itemTable.SetWidths(new float[] { 30, 12, 12, 14, 10 });
                    foreach (var h in new[] { "Naam", "Hoeveelheid", "Minimum", "Eenheid", "Status" })
                        itemTable.AddCell(new PdfPCell(new Phrase(h, hFont))
                        { BackgroundColor = accentColor, Padding = 4, HorizontalAlignment = Element.ALIGN_CENTER });

                    var rowBg = new BaseColor(255, 255, 255);
                    AddDataCell(itemTable, item.Name, dFont, rowBg);
                    AddDataCell(itemTable, item.CurrentStock.ToString("G29"), dFont, rowBg, Element.ALIGN_RIGHT);
                    AddDataCell(itemTable, item.MinimumStock?.ToString("G29") ?? "-", dFont, rowBg, Element.ALIGN_RIGHT);
                    AddDataCell(itemTable, item.Unit, dFont, rowBg, Element.ALIGN_CENTER);
                    itemTable.AddCell(new PdfPCell(new Phrase(statusLabel, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)))
                    { BackgroundColor = statusBg, Padding = 4, HorizontalAlignment = Element.ALIGN_CENTER });
                    document.Add(itemTable);

                    // Movement history
                    if (item.Movements?.Any() == true)
                    {
                        var mvTable = new PdfPTable(4) { WidthPercentage = 96, SpacingAfter = 8, HorizontalAlignment = Element.ALIGN_RIGHT };
                        mvTable.SetWidths(new float[] { 22, 10, 40, 18 });
                        foreach (var h in new[] { "Datum", "Type", "Notitie", "Hoeveelheid" })
                            mvTable.AddCell(new PdfPCell(new Phrase(h, mvHFont))
                            { BackgroundColor = mvHeaderColor, Padding = 3, HorizontalAlignment = Element.ALIGN_CENTER });

                        bool alt = false;
                        foreach (var mv in item.Movements)
                        {
                            var bg = alt ? new BaseColor(245, 244, 255) : new BaseColor(255, 255, 255);
                            var date = (mv.MovementDate ?? mv.CreatedAt).ToString("dd/MM/yyyy HH:mm");
                            var typeLabel = mv.Type switch
                            {
                                AdminMembers.Models.StockMovementType.In         => "+ In",
                                AdminMembers.Models.StockMovementType.Out        => "- Uit",
                                AdminMembers.Models.StockMovementType.Correction => "= Correctie",
                                _                                                => mv.Type.ToString()
                            };
                            AddDataCell(mvTable, date, mvFont, bg);
                            AddDataCell(mvTable, typeLabel, mvFont, bg, Element.ALIGN_CENTER);
                            AddDataCell(mvTable, mv.Note ?? "", mvFont, bg);
                            AddDataCell(mvTable, mv.Quantity.ToString("G29"), mvFont, bg, Element.ALIGN_RIGHT);
                            alt = !alt;
                        }
                        document.Add(mvTable);
                    }
                    else
                    {
                        document.Add(new Paragraph("  Geen bewegingen geregistreerd.",
                            FontFactory.GetFont(FontFactory.HELVETICA, 8, mutedColor))
                        { SpacingAfter = 8 });
                    }
                }
            }

            document.Close();
            return stream.ToArray();
        }

        // ?? Helpers ???????????????????????????????????????????????????????
        private static string Esc(string s) => s.Replace("\"", "\"\"");

        private static void AddSummaryCell(PdfPTable t, string value, string label, BaseColor color)
        {
            var chunk = new Chunk(value, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, color));
            var lbl   = new Chunk($"\n{label}", FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(100, 100, 100)));
            var p     = new Paragraph { chunk, lbl };
            p.Alignment = Element.ALIGN_CENTER;
            t.AddCell(new PdfPCell(p) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 8 });
        }

        private static void AddDataCell(PdfPTable t, string text, iTextSharp.text.Font font,
            BaseColor bg, int align = Element.ALIGN_LEFT)
        {
            t.AddCell(new PdfPCell(new Phrase(text, font))
            { BackgroundColor = bg, Padding = 5, HorizontalAlignment = align });
        }
    }
}
