using System.IO.Compression;
using System.Security;
using System.Text;

namespace RostelecomAppeals.Shared;

public sealed class ExportService
{
    public async Task ExportXlsxAsync(string path, IEnumerable<AppealDto> appeals, CancellationToken ct = default)
    {
        var rows = appeals.ToList();
        if (File.Exists(path)) File.Delete(path);
        await using var fs = File.Create(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        Add(zip, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
</Types>
""");
        Add(zip, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""");
        Add(zip, "xl/_rels/workbook.xml.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
""");
        Add(zip, "xl/workbook.xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Обращения" sheetId="1" r:id="rId1"/></sheets></workbook>
""");
        Add(zip, "xl/styles.xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs></styleSheet>
""");

        var headers = new[] { "Номер", "ФИО/организация", "Телефон", "Адрес", "Описание", "Тип", "Статус", "Приоритет", "Дата", "Специалист" };
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cols>");
        for (int i = 1; i <= headers.Length; i++) sb.Append($"<col min=\"{i}\" max=\"{i}\" width=\"22\" customWidth=\"1\"/>");
        sb.Append("</cols><sheetData>");
        sb.Append(Row(1, headers, true));
        var rowIndex = 2;
        foreach (var a in rows)
        {
            sb.Append(Row(rowIndex++, new[]
            {
                a.PublicNumber ?? "", a.ApplicantName, a.ContactPhone, a.ConnectionAddress, a.Description,
                a.TypeName, a.StatusName, a.PriorityName, a.RegisteredAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"), a.SpecialistName
            }, false));
        }
        sb.Append("</sheetData></worksheet>");
        Add(zip, "xl/worksheets/sheet1.xml", sb.ToString());
        await Task.CompletedTask;
    }

    public async Task ExportDocxAsync(string path, IEnumerable<AppealDto> appeals, CancellationToken ct = default)
    {
        var rows = appeals.ToList();
        if (File.Exists(path)) File.Delete(path);
        await using var fs = File.Create(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        Add(zip, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>
""");
        Add(zip, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>
""");
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
        sb.Append(Paragraph("Реестр обращений граждан и клиентов Ростелекома", true));
        sb.Append(Paragraph($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm}", false));
        sb.Append("<w:tbl><w:tblPr><w:tblBorders><w:top w:val=\"single\" w:sz=\"4\"/><w:left w:val=\"single\" w:sz=\"4\"/><w:bottom w:val=\"single\" w:sz=\"4\"/><w:right w:val=\"single\" w:sz=\"4\"/><w:insideH w:val=\"single\" w:sz=\"4\"/><w:insideV w:val=\"single\" w:sz=\"4\"/></w:tblBorders></w:tblPr>");
        sb.Append(DocxRow(new[] { "Номер", "Заявитель", "Телефон", "Тип", "Статус", "Приоритет", "Дата" }, true));
        foreach (var a in rows)
        {
            sb.Append(DocxRow(new[] { a.PublicNumber ?? "", a.ApplicantName, a.ContactPhone, a.TypeName, a.StatusName, a.PriorityName, a.RegisteredAt.ToLocalTime().ToString("dd.MM.yyyy") }, false));
        }
        sb.Append("</w:tbl><w:sectPr/></w:body></w:document>");
        Add(zip, "word/document.xml", sb.ToString());
        await Task.CompletedTask;
    }

    private static string Row(int index, IReadOnlyList<string> values, bool header)
    {
        var sb = new StringBuilder($"<row r=\"{index}\">");
        for (var i = 0; i < values.Count; i++)
        {
            var cellRef = ColumnName(i + 1) + index;
            var style = header ? " s=\"1\"" : "";
            sb.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"{style}><is><t>{Esc(values[i])}</t></is></c>");
        }
        sb.Append("</row>");
        return sb.ToString();
    }

    private static string DocxRow(IEnumerable<string> values, bool header)
    {
        var sb = new StringBuilder("<w:tr>");
        foreach (var v in values)
            sb.Append("<w:tc><w:tcPr><w:tcW w:w=\"2400\" w:type=\"dxa\"/></w:tcPr><w:p><w:r>" + (header ? "<w:rPr><w:b/></w:rPr>" : "") + $"<w:t>{Esc(v)}</w:t></w:r></w:p></w:tc>");
        sb.Append("</w:tr>");
        return sb.ToString();
    }

    private static string Paragraph(string text, bool bold)
    {
        return "<w:p><w:r>" + (bold ? "<w:rPr><w:b/><w:sz w:val=\"32\"/></w:rPr>" : "") + $"<w:t>{Esc(text)}</w:t></w:r></w:p>";
    }

    private static string Esc(string? value) => SecurityElement.Escape(value ?? "") ?? "";

    private static string ColumnName(int index)
    {
        var name = "";
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }
        return name;
    }

    private static void Add(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
