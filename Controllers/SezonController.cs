using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sedziowanie.Data;
using Sedziowanie.Models;
using Sedziowanie.Services.Interfaces;
using System.IO.Compression;

namespace Sedziowanie.Controllers;

[Authorize(Roles = "Admin")]
public class SezonController : Controller
{
    private readonly DBObsadyContext _context;
    private readonly ISedziaService _sedziaService;

    public SezonController(DBObsadyContext context, ISedziaService sedziaService)
    {
        _context = context;
        _sedziaService = sedziaService;
    }

    [HttpGet]
    public IActionResult ZarzadzanieSezonem()
    {
        var rozgrywki = _context.Rozgrywki.AsNoTracking().OrderBy(r => r.Nazwa).ToList();
        ViewBag.Rozgrywki = rozgrywki;
        ViewBag.MeczeCount = _context.Mecze.Count();
        ViewBag.NiedyspozycjeCount = _context.Niedyspozycje.Count();
        return View();
    }

    // ── Export: All matches ───────────────────────────────────────────────────

    [HttpGet]
    public IActionResult ExportWszystkieMecze()
    {
        var mecze = LoadMeczeWithIncludes().ToList();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Wszystkie mecze");
        WriteMeczeSheet(ws, mecze);
        return ExcelFile(wb, $"Mecze_Wszystkie_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    // ── Export: Single category ───────────────────────────────────────────────

    [HttpGet]
    public IActionResult ExportKategoria(int id)
    {
        var rozgrywki = _context.Rozgrywki.Find(id);
        if (rozgrywki == null) return NotFound();

        var mecze = LoadMeczeWithIncludes().Where(m => m.RozgrywkiId == id).ToList();
        using var wb = new XLWorkbook();
        var sheetName = SafeSheetName(rozgrywki.Nazwa ?? "Kategoria");
        var ws = wb.Worksheets.Add(sheetName);
        WriteMeczeSheet(ws, mecze);
        var fileName = $"Mecze_{SafeFileName(rozgrywki.Nazwa ?? "Kategoria")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return ExcelFile(wb, fileName);
    }

    // ── Export: Full season (multi-sheet) ────────────────────────────────────

    [HttpGet]
    public IActionResult ExportPelnySezon()
    {
        var allMecze = LoadMeczeWithIncludes().ToList();
        var rozgrywkiList = _context.Rozgrywki.AsNoTracking().OrderBy(r => r.Nazwa).ToList();
        var stats = _sedziaService.GetSedziaStatistics();

        using var wb = new XLWorkbook();

        // Sheet 1: all matches
        var wsAll = wb.Worksheets.Add("Wszystkie mecze");
        WriteMeczeSheet(wsAll, allMecze);

        // One sheet per category
        foreach (var r in rozgrywkiList)
        {
            var filtered = allMecze.Where(m => m.RozgrywkiId == r.Id).ToList();
            if (!filtered.Any()) continue;
            var ws = wb.Worksheets.Add(SafeSheetName(r.Nazwa ?? r.Id.ToString()));
            WriteMeczeSheet(ws, filtered);
        }

        // Last sheet: referee statistics
        var wsStats = wb.Worksheets.Add("Statystyki sędziów");
        WriteStatystykiSheet(wsStats, stats);

        return ExcelFile(wb, $"PelnySezon_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    // ── Zakończ sezon ─────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ZakonczSezon(bool confirmed)
    {
        if (!confirmed)
        {
            TempData["Error"] = "Nie potwierdzono zakończenia sezonu.";
            return RedirectToAction(nameof(ZarzadzanieSezonem));
        }

        var allMecze = LoadMeczeWithIncludes().ToList();
        var rozgrywkiList = _context.Rozgrywki.AsNoTracking().OrderBy(r => r.Nazwa).ToList();
        var stats = _sedziaService.GetSedziaStatistics();

        // Build ZIP in memory with two Excel files
        using var zipStream = new MemoryStream();
        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // File 1: full season (all sheets)
            var fullSezonBytes = BuildPelnySezonExcel(allMecze, rozgrywkiList, stats);
            var entry1 = zip.CreateEntry($"PelnySezon_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            using (var es = entry1.Open())
                await es.WriteAsync(fullSezonBytes);

            // File 2: all matches only
            var allMeczeBytes = BuildMeczeExcel(allMecze);
            var entry2 = zip.CreateEntry($"Mecze_Wszystkie_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            using (var es = entry2.Open())
                await es.WriteAsync(allMeczeBytes);
        }

        // Delete data
        _context.Niedyspozycje.RemoveRange(_context.Niedyspozycje);
        _context.Mecze.RemoveRange(_context.Mecze);
        await _context.SaveChangesAsync();

        zipStream.Position = 0;
        return File(zipStream.ToArray(), "application/zip", $"KopiaZapasowaSezonu_{DateTime.Now:yyyyMMdd_HHmm}.zip");
    }

    // ── Import matches from Excel ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportMecze(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Nie wybrano pliku.";
            return RedirectToAction(nameof(ZarzadzanieSezonem));
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            TempData["Error"] = "Obsługiwany format to .xlsx.";
            return RedirectToAction(nameof(ZarzadzanieSezonem));
        }

        var allSedziowie = _context.Sedziowie.AsNoTracking().ToList();
        var allRozgrywki = _context.Rozgrywki.ToList();

        int imported = 0, skipped = 0;

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var wb = new XLWorkbook(stream);

        // Import from first sheet named "Wszystkie mecze" or first sheet
        var ws = wb.Worksheets.FirstOrDefault(s => s.Name == "Wszystkie mecze")
                  ?? wb.Worksheets.First();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var numerMeczu = ws.Cell(row, 1).GetString().Trim();
                var dataStr = ws.Cell(row, 2).GetString().Trim();
                var godzinaStr = ws.Cell(row, 3).GetString().Trim();
                var udostepnijStr = ws.Cell(row, 4).GetString().Trim();
                var rozgrywkiNazwa = ws.Cell(row, 5).GetString().Trim();
                var gospodarz = ws.Cell(row, 6).GetString().Trim();
                var gosc = ws.Cell(row, 7).GetString().Trim();
                var turniej = ws.Cell(row, 8).GetString().Trim();
                var adres = ws.Cell(row, 9).GetString().Trim();
                var si = ws.Cell(row, 10).GetString().Trim();
                var sii = ws.Cell(row, 11).GetString().Trim();
                var ss = ws.Cell(row, 12).GetString().Trim();
                var sl1 = ws.Cell(row, 13).GetString().Trim();
                var sl2 = ws.Cell(row, 14).GetString().Trim();
                var sg = ws.Cell(row, 15).GetString().Trim();
                var dodatkowe = ws.Cell(row, 16).GetString().Trim();

                if (string.IsNullOrEmpty(dataStr) && string.IsNullOrEmpty(gospodarz))
                {
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParseExact(dataStr, "dd.MM.yyyy", null,
                        System.Globalization.DateTimeStyles.None, out var data))
                {
                    if (!DateTime.TryParse(dataStr, out data))
                    {
                        skipped++;
                        continue;
                    }
                }

                // Combine date + time
                if (!string.IsNullOrEmpty(godzinaStr) && TimeSpan.TryParse(godzinaStr, out var t))
                    data = data.Date + t;

                bool udostepnij = udostepnijStr.Equals("TAK", StringComparison.OrdinalIgnoreCase);

                // Rozgrywki: find or create
                var roz = allRozgrywki.FirstOrDefault(r =>
                    r.Nazwa?.Equals(rozgrywkiNazwa, StringComparison.OrdinalIgnoreCase) == true);
                if (roz == null && !string.IsNullOrEmpty(rozgrywkiNazwa))
                {
                    roz = new Rozgrywki { Nazwa = rozgrywkiNazwa };
                    _context.Rozgrywki.Add(roz);
                    await _context.SaveChangesAsync();
                    allRozgrywki.Add(roz);
                }

                if (roz == null) { skipped++; continue; }

                var mecz = new Mecz
                {
                    NumerMeczu = numerMeczu,
                    Data = data,
                    UdostepnijGodzine = udostepnij,
                    RozgrywkiId = roz.Id,
                    Gospodarz = gospodarz,
                    Gosc = gosc,
                    Turniej = string.IsNullOrEmpty(turniej) ? null : turniej,
                    Adres = string.IsNullOrEmpty(adres) ? null : adres,
                    DodatkoweInformacje = string.IsNullOrEmpty(dodatkowe) ? null : dodatkowe,
                    SedziaIId = FindSedzia(allSedziowie, si)?.Id,
                    SedziaIIId = FindSedzia(allSedziowie, sii)?.Id,
                    SedziaSekretarzId = FindSedzia(allSedziowie, ss)?.Id,
                    SedziaLiniowyIId = FindSedzia(allSedziowie, sl1)?.Id,
                    SedziaLiniowyIIId = FindSedzia(allSedziowie, sl2)?.Id,
                    SedziaGlownyId = FindSedzia(allSedziowie, sg)?.Id,
                };

                _context.Mecze.Add(mecz);
                imported++;
            }
            catch
            {
                skipped++;
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Zaimportowano {imported} meczów. Pominięto {skipped} wierszy.";
        return RedirectToAction(nameof(ZarzadzanieSezonem));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IQueryable<Mecz> LoadMeczeWithIncludes()
    {
        return _context.Mecze
            .AsNoTracking()
            .Include(m => m.Rozgrywki)
            .Include(m => m.SedziaI)
            .Include(m => m.SedziaII)
            .Include(m => m.SedziaSekretarz)
            .Include(m => m.SedziaLiniowyI)
            .Include(m => m.SedziaLiniowyII)
            .Include(m => m.SedziaGlowny)
            .OrderBy(m => m.Data);
    }

    private static void WriteMeczeSheet(IXLWorksheet ws, List<Mecz> mecze)
    {
        var headers = new[]
        {
            "Numer meczu", "Data", "Godzina", "Udostępnij godzinę",
            "Rozgrywki", "Gospodarz", "Gość", "Turniej", "Adres",
            "Sędzia I", "Sędzia II", "Sędzia Sekretarz",
            "Sędzia Liniowy I", "Sędzia Liniowy II", "Sędzia Główny",
            "Dodatkowe informacje"
        };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var m in mecze)
        {
            ws.Cell(row, 1).Value = m.NumerMeczu ?? "";
            ws.Cell(row, 2).Value = m.Data.ToString("dd.MM.yyyy");
            ws.Cell(row, 3).Value = m.UdostepnijGodzine ? m.Data.ToString("HH:mm") : "";
            ws.Cell(row, 4).Value = m.UdostepnijGodzine ? "TAK" : "NIE";
            ws.Cell(row, 5).Value = m.Rozgrywki?.Nazwa ?? "";
            ws.Cell(row, 6).Value = m.Gospodarz ?? "";
            ws.Cell(row, 7).Value = m.Gosc ?? "";
            ws.Cell(row, 8).Value = m.Turniej ?? "";
            ws.Cell(row, 9).Value = m.Adres ?? "";
            ws.Cell(row, 10).Value = SedziaName(m.SedziaI);
            ws.Cell(row, 11).Value = SedziaName(m.SedziaII);
            ws.Cell(row, 12).Value = SedziaName(m.SedziaSekretarz);
            ws.Cell(row, 13).Value = SedziaName(m.SedziaLiniowyI);
            ws.Cell(row, 14).Value = SedziaName(m.SedziaLiniowyII);
            ws.Cell(row, 15).Value = SedziaName(m.SedziaGlowny);
            ws.Cell(row, 16).Value = m.DodatkoweInformacje ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        // Style header row
        ws.Row(1).Style.Font.Bold = true;
    }

    private static void WriteStatystykiSheet(IXLWorksheet ws, List<Sedziowanie.ViewModels.SedziaStatsDto> stats)
    {
        var headers = new[]
        {
            "Miejsce", "Nazwisko", "Imię", "Łącznie", "SI", "SII", "SS", "SG", "L",
            "SC", "3", "J", "K", "MŁ", "MMP", "Mini", "Tow", "Plaża", "PALM", "Duet", "Duet Mecze"
        };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2, lp = 1;
        foreach (var s in stats)
        {
            ws.Cell(row, 1).Value = lp;
            ws.Cell(row, 2).Value = s.Nazwisko;
            ws.Cell(row, 3).Value = s.Imie;
            ws.Cell(row, 4).Value = s.MeczeCount;
            ws.Cell(row, 5).Value = s.SiCount;
            ws.Cell(row, 6).Value = s.SiiCount;
            ws.Cell(row, 7).Value = s.SsCount;
            ws.Cell(row, 8).Value = s.SgCount;
            ws.Cell(row, 9).Value = s.LCount;
            ws.Cell(row, 10).Value = s.SzczebelCount;
            ws.Cell(row, 11).Value = s.Liga3Count;
            ws.Cell(row, 12).Value = s.JCount;
            ws.Cell(row, 13).Value = s.KCount;
            ws.Cell(row, 14).Value = s.MlCount;
            ws.Cell(row, 15).Value = s.MmpCount;
            ws.Cell(row, 16).Value = s.MiniCount;
            ws.Cell(row, 17).Value = s.TowCount;
            ws.Cell(row, 18).Value = s.PlazaCount;
            ws.Cell(row, 19).Value = s.PalmCount;
            ws.Cell(row, 20).Value = s.DuetSedzia;
            ws.Cell(row, 21).Value = s.DuetCount;
            row++;
            lp++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
        ws.Row(1).Style.Font.Bold = true;
    }

    private static string SedziaName(Sedzia? s) =>
        s == null ? "" : $"{s.Imie} {s.Nazwisko}";

    private static Sedzia? FindSedzia(List<Sedzia> sedziowie, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return sedziowie.FirstOrDefault(s =>
            $"{s.Imie} {s.Nazwisko}".Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string SafeSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        foreach (var c in invalid) name = name.Replace(c, ' ');
        return name.Length > 31 ? name[..31] : name;
    }

    private static string SafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }

    private static IActionResult ExcelFile(XLWorkbook wb, string fileName)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new FileContentResult(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        { FileDownloadName = fileName };
    }

    private byte[] BuildPelnySezonExcel(
        List<Mecz> allMecze,
        List<Rozgrywki> rozgrywkiList,
        List<Sedziowanie.ViewModels.SedziaStatsDto> stats)
    {
        using var wb = new XLWorkbook();
        var wsAll = wb.Worksheets.Add("Wszystkie mecze");
        WriteMeczeSheet(wsAll, allMecze);

        foreach (var r in rozgrywkiList)
        {
            var filtered = allMecze.Where(m => m.RozgrywkiId == r.Id).ToList();
            if (!filtered.Any()) continue;
            var ws = wb.Worksheets.Add(SafeSheetName(r.Nazwa ?? r.Id.ToString()));
            WriteMeczeSheet(ws, filtered);
        }

        var wsStats = wb.Worksheets.Add("Statystyki sędziów");
        WriteStatystykiSheet(wsStats, stats);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private byte[] BuildMeczeExcel(List<Mecz> mecze)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Wszystkie mecze");
        WriteMeczeSheet(ws, mecze);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
