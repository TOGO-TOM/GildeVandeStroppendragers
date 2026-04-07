using AdminMembers.Models;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace AdminMembers.Services
{
    public class ExportService
    {
        public byte[] ExportToExcel(List<Member> members, List<string> selectedFields)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Members");

            // Add headers
            int col = 1;
            var fieldMap = GetFieldMap();
            foreach (var field in selectedFields)
            {
                if (fieldMap.ContainsKey(field))
                {
                    worksheet.Cell(1, col).Value = fieldMap[field];
                    worksheet.Cell(1, col).Style.Font.Bold = true;
                    worksheet.Cell(1, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                    col++;
                }
            }

            // Add data
            int row = 2;
            foreach (var member in members)
            {
                col = 1;
                foreach (var field in selectedFields)
                {
                    var value = GetFieldValue(member, field);
                    worksheet.Cell(row, col).Value = value;
                    col++;
                }
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportToPdf(List<Member> members, List<string> selectedFields, byte[]? logoData = null, string? companyName = null)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter.GetInstance(document, stream);
            document.Open();

            // Add logo if provided
            if (logoData != null && logoData.Length > 0)
            {
                try
                {
                    var logo = iTextSharp.text.Image.GetInstance(logoData);
                    logo.ScaleToFit(120f, 60f);
                    logo.Alignment = Element.ALIGN_CENTER;
                    logo.SpacingAfter = 10;
                    document.Add(logo);
                }
                catch
                {
                    // If logo fails to load, just skip it
                }
            }

            // Add company name or default title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var title = new Paragraph(companyName ?? "Members Export", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            // Add export date
            var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(128, 128, 128));
            var date = new Paragraph($"Exported on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", dateFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(date);

            // Create table
            var table = new PdfPTable(selectedFields.Count)
            {
                WidthPercentage = 100,
                SpacingBefore = 10
            };

            // Add headers
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(255, 255, 255));
            var fieldMap = GetFieldMap();
            foreach (var field in selectedFields)
            {
                if (fieldMap.ContainsKey(field))
                {
                    var cell = new PdfPCell(new Phrase(fieldMap[field], headerFont))
                    {
                        BackgroundColor = new BaseColor(70, 130, 180),
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    };
                    table.AddCell(cell);
                }
            }

            // Add data rows
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            foreach (var member in members)
            {
                foreach (var field in selectedFields)
                {
                    var value = GetFieldValue(member, field);
                    var cell = new PdfPCell(new Phrase(value, cellFont))
                    {
                        Padding = 5,
                        HorizontalAlignment = Element.ALIGN_LEFT
                    };
                    table.AddCell(cell);
                }
            }

            document.Add(table);

            // Add footer
            var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128));
            var footer = new Paragraph($"Total Members: {members.Count}", footerFont)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingBefore = 20
            };
            document.Add(footer);

            document.Close();
            return stream.ToArray();
        }

        public byte[] ExportToCsv(List<Member> members, List<string> selectedFields)
        {
            var csv = new StringBuilder();

            // Add UTF-8 BOM for special character support (ë, é, etc.)
            csv.Append('\ufeff');

            var fieldMap = GetFieldMap();

            // Add headers
            var headers = selectedFields.Select(f => fieldMap.ContainsKey(f) ? fieldMap[f] : f);
            csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // Add data
            foreach (var member in members)
            {
                var values = selectedFields.Select(field => 
                {
                    var value = GetFieldValue(member, field);
                    // Escape quotes and wrap in quotes
                    return $"\"{value.Replace("\"", "\"\"")}\"";
                });
                csv.AppendLine(string.Join(",", values));
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private Dictionary<string, string> GetFieldMap()
        {
            return new Dictionary<string, string>
            {
                { "Id", "ID" },
                { "MemberNumber", "Member Number" },
                { "FirstName", "First Name" },
                { "LastName", "Last Name" },
                { "Gender", "Gender" },
                { "Role", "Role" },
                { "BirthDate", "Birth Date" },
                { "Email", "Email" },
                { "PhoneNumber", "Phone Number" },
                { "IsAlive", "Status" },
                { "SeniorityDate", "Seniority Date" },
                { "Street", "Street" },
                { "HouseNumber", "House Number" },
                { "City", "City" },
                { "PostalCode", "Postal Code" },
                { "Country", "Country" },
                { "CreatedAt", "Created Date" },
                { "UpdatedAt", "Updated Date" }
            };
        }

        private string GetFieldValue(Member member, string field)
        {
            var value = field switch
            {
                "Id" => member.Id.ToString(),
                "MemberNumber" => member.MemberNumber.ToString(),
                "FirstName" => member.FirstName,
                "LastName" => member.LastName,
                "Gender" => member.Gender,
                "Role" => member.Role,
                "BirthDate" => member.BirthDate?.ToString("yyyy-MM-dd"),
                "Email" => member.Email,
                "PhoneNumber" => member.PhoneNumber,
                "IsAlive" => member.IsAlive ? "Alive" : "Deceased",
                "SeniorityDate" => member.SeniorityDate?.ToString("yyyy-MM-dd"),
                "Street" => member.Address?.Street,
                "HouseNumber" => member.Address?.HouseNumber,
                "City" => member.Address?.City,
                "PostalCode" => member.Address?.PostalCode,
                "Country" => member.Address?.Country,
                "CreatedAt" => member.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                "UpdatedAt" => member.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                _ => null
            };
            return value ?? string.Empty;
        }
    }
}
