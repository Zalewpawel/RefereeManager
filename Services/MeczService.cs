using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Sedziowanie.Data;
using Sedziowanie.Models;
using Sedziowanie.Services.Interfaces;
using Sedziowanie.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public class MeczService : IMeczService
{
    private readonly DBObsadyContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<MeczService> _logger;

    public MeczService(DBObsadyContext context, IEmailService emailService, ILogger<MeczService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task AddMecz(string numerMeczu, DateTime data, int rozgrywkiId, string gospodarz, string gosc,
                              int? sedziaIId, int? sedziaIIId, int? sedziaSekretarzId, int? sedziaLiniowyIId, int? sedziaLiniowyIIId, int? sedziaGlownyId,
                              string? turniej = null, string? adres = null, string? dodatkoweInformacje = null,
                              string? gospodarzKlubId = null, string? goscKlubId = null)
    {
        var mecz = new Mecz
        {
            NumerMeczu = numerMeczu,
            Data = data,
            Gospodarz = gospodarz,
            Gosc = gosc,
            RozgrywkiId = rozgrywkiId,
            SedziaIId = sedziaIId,
            SedziaIIId = sedziaIIId,
            SedziaSekretarzId = sedziaSekretarzId,
            SedziaLiniowyIId = sedziaLiniowyIId,
            SedziaLiniowyIIId = sedziaLiniowyIIId,
            SedziaGlownyId = sedziaGlownyId,
            Turniej = turniej,
            Adres = adres,
            DodatkoweInformacje = dodatkoweInformacje,
            GospodarzKlubId = gospodarzKlubId,
            GoscKlubId = goscKlubId
        };

        _context.Mecze.Add(mecz);
        await _context.SaveChangesAsync();

        var sedziowieIds = new[] { sedziaIId, sedziaIIId, sedziaSekretarzId, sedziaLiniowyIId, sedziaLiniowyIIId, sedziaGlownyId }
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToList();

        var sedziowieEmails = await _context.Sedziowie
            .Where(s => sedziowieIds.Contains(s.Id))
            .Select(s => s.Email)
            .Where(email => !string.IsNullOrEmpty(email))
            .ToListAsync();

        var shouldSendNotifications = mecz.Data > DateTime.Now;

        if (shouldSendNotifications && sedziowieEmails.Any())
        {
            try
            {
                await SendMatchEmail(mecz, sedziowieEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania e-maila z informacją o meczu.");
            }
        }
    }

    private async Task SendMatchEmail(Mecz mecz, List<string> emails)
    {
        var rozgrywkiNazwa = await _context.Rozgrywki
            .Where(r => r.Id == mecz.RozgrywkiId)
            .Select(r => r.Nazwa)
            .FirstOrDefaultAsync();

        var sedzia1 = await _context.Sedziowie.FindAsync(mecz.SedziaIId);
        var sedzia2 = await _context.Sedziowie.FindAsync(mecz.SedziaIIId);
        var sekretarz = await _context.Sedziowie.FindAsync(mecz.SedziaSekretarzId);
        var liniowy1 = await _context.Sedziowie.FindAsync(mecz.SedziaLiniowyIId);
        var liniowy2 = await _context.Sedziowie.FindAsync(mecz.SedziaLiniowyIIId);
        var glowny = await _context.Sedziowie.FindAsync(mecz.SedziaGlownyId);

        string subject = $"Informacja o meczu {mecz.Gospodarz} - {mecz.Gosc}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<p>Dzień dobry,</p>");
        sb.AppendLine("<p>Informujemy, że został Ci przydzielony nowy mecz lub zostały w nim wprowadzone zmiany. Prosimy o zapoznanie się.</p>");
        sb.AppendLine("<br>");

        // Date: always show date; show time only if UdostepnijGodzine is true
        var dateOnly = mecz.Data.ToString("dd.MM.yyyy");
        var dateWithTime = mecz.Data.ToString("dd.MM.yyyy HH:mm");
        if (mecz.UdostepnijGodzine)
            sb.AppendLine($"<p><b>DATA:</b> {dateWithTime}</p>");
        else
            sb.AppendLine($"<p><b>DATA:</b> {dateOnly}</p>");

        sb.AppendLine($"<p><b>MECZ:</b> {mecz.Gospodarz} - {mecz.Gosc}</p>");
        sb.AppendLine($"<p><b>NUMER MECZU:</b> {mecz.NumerMeczu}</p>");
        sb.AppendLine($"<p><b>ROZGRYWKI:</b> {rozgrywkiNazwa ?? "brak danych"}</p>");
        if (!string.IsNullOrEmpty(mecz.Turniej)) sb.AppendLine($"<p><b>TURNEJ:</b> {mecz.Turniej}</p>");
        if (!string.IsNullOrEmpty(mecz.Adres)) sb.AppendLine($"<p><b>ADRES:</b> {mecz.Adres}</p>");
        if (!string.IsNullOrEmpty(mecz.DodatkoweInformacje)) sb.AppendLine($"<p><b>DODATKOWE INFORMACJE:</b> {mecz.DodatkoweInformacje}</p>");
        sb.AppendLine("<br>");

        void AppendIfNotNull(string label, Sedzia? s)
        {
            if (s != null)
                sb.AppendLine($"<p>{label}: {s.Imie} {s.Nazwisko}</p>");
        }

        sb.AppendLine("<p><b>Obsada sędziowska:</b></p>");
        AppendIfNotNull("Sędzia I", sedzia1);
        AppendIfNotNull("Sędzia II", sedzia2);
        AppendIfNotNull("Sędzia Sekretarz", sekretarz);
        AppendIfNotNull("Sędzia Liniowy I", liniowy1);
        AppendIfNotNull("Sędzia Liniowy II", liniowy2);
        AppendIfNotNull("Sędzia Główny", glowny);

        string body = sb.ToString();

        _logger.LogInformation("Przygotowano mail: subj='{Subject}', recipients={Count}", subject, emails.Count);

        foreach (var email in emails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
            _logger.LogInformation("E-mail wysłany do: {Email}", email);
        }
    }

    private async Task SendRemovalEmailAsync(Mecz mecz, Sedzia removed, string roleDescription)
    {
        if (removed == null || string.IsNullOrEmpty(removed.Email)) return;

        var rozgrywkiNazwa = await _context.Rozgrywki
            .Where(r => r.Id == mecz.RozgrywkiId)
            .Select(r => r.Nazwa)
            .FirstOrDefaultAsync();

        string subject = $"Zmiana obsady meczu {mecz.Gospodarz} - {mecz.Gosc}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<p>Dzień dobry,</p>");
        sb.AppendLine("<p>Informujemy, że usunięto Twoją obsadę.</p>");
        if (!string.IsNullOrEmpty(roleDescription))
            sb.AppendLine($"<p>Poprzednia rola: <b>{roleDescription}</b></p>");
        sb.AppendLine("<br>");

        // Date: always show date; show time only if UdostepnijGodzine is true
        var dateOnly = mecz.Data.ToString("dd.MM.yyyy");
        var dateWithTime = mecz.Data.ToString("dd.MM.yyyy HH:mm");
        if (mecz.UdostepnijGodzine)
            sb.AppendLine($"<p><b>DATA:</b> {dateWithTime}</p>");
        else
            sb.AppendLine($"<p><b>DATA:</b> {dateOnly}</p>");

        sb.AppendLine($"<p><b>MECZ:</b> {mecz.Gospodarz} - {mecz.Gosc}</p>");
        sb.AppendLine($"<p><b>NUMER MECZU:</b> {mecz.NumerMeczu}</p>");
        sb.AppendLine($"<p><b>ROZGRYWKI:</b> {rozgrywkiNazwa ?? "brak danych"}</p>");
        if (!string.IsNullOrEmpty(mecz.Turniej)) sb.AppendLine($"<p><b>TURNEJ:</b> {mecz.Turniej}</p>");
        if (!string.IsNullOrEmpty(mecz.Adres)) sb.AppendLine($"<p><b>ADRES:</b> {mecz.Adres}</p>");
        sb.AppendLine("<br>");

        string body = sb.ToString();

        await _emailService.SendEmailAsync(removed.Email, subject, body, isHtml: true);
        _logger.LogInformation("E-mail (usunięcie z obsady) wysłany do: {Email}", removed.Email);
    }

    public IEnumerable<Rozgrywki> GetRozgrywki()
    {
       
        return _context.Rozgrywki
                       .AsNoTracking()
                       .OrderBy(r => r.Nazwa)
                       .ToList();
    }

    public IEnumerable<SedziaOptionDto> GetSedziowieByDateWithStatus(DateTime data)
    {
        var dayStart = data.Date;
        var dayEnd = dayStart.AddDays(1);

        var query = _context.Sedziowie
            .AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.Imie,
                s.Nazwisko,
                s.Klasa,
                s.Miasto,
                s.CzyUrlop,
                MaNiedyspozycje = _context.Niedyspozycje
                    .Any(n => n.SedziaId == s.Id && n.Poczatek <= data && data <= n.Koniec),

                MaMecz = _context.Mecze.Any(m =>
                    m.Data >= dayStart && m.Data < dayEnd &&
                    (
                        m.SedziaIId == s.Id ||
                        m.SedziaIIId == s.Id ||
                        m.SedziaSekretarzId == s.Id ||
                        m.SedziaLiniowyIId == s.Id ||
                        m.SedziaLiniowyIIId == s.Id ||
                        m.SedziaGlownyId == s.Id
                    ))
            })
            .Select(x => new SedziaOptionDto
            {
                Id = x.Id,
                Imie = x.Imie,
                Nazwisko = x.Nazwisko,
                Klasa = x.Klasa,
                Miasto = x.Miasto,
                Status = x.CzyUrlop ? "URLOP" : (x.MaNiedyspozycje ? "NIEDOSTEPNY" : "DOSTEPNY"),
                MaMeczTegoDnia = x.MaMecz
            })
            .OrderBy(s => s.Status == "DOSTEPNY" ? 0 : (s.Status == "NIEDOSTEPNY" ? 1 : 2))
            .ThenBy(s => s.Nazwisko)
            .ThenBy(s => s.Imie);

        return query.ToList();
    }


    public List<Mecz> GetAllMecze()
    {
        var all = _context.Mecze
             .Include(m => m.Rozgrywki)
                .Include(m => m.SedziaI)
                .Include(m => m.SedziaII)
                .Include(m => m.SedziaSekretarz)
                .Include(m => m.SedziaLiniowyI)
                .Include(m => m.SedziaLiniowyII)
                .Include(m => m.SedziaGlowny)
            .AsNoTracking()
            .ToList();

        var ordered = all
            .GroupBy(m => m.Data.Date)
            .OrderBy(g => g.Key)
            .SelectMany(dayGroup =>
            {
                var tournamentItems = dayGroup
                    .Where(m => !string.IsNullOrWhiteSpace(m.Turniej))
                    .GroupBy(m => m.Turniej)
                    .Select(g => (Time: g.Min(m => m.Data), Matches: (IEnumerable<Mecz>)g.OrderBy(m => m.Data)));

                var singleItems = dayGroup
                    .Where(m => string.IsNullOrWhiteSpace(m.Turniej))
                    .Select(m => (Time: m.Data, Matches: (IEnumerable<Mecz>)new List<Mecz> { m }));

                return tournamentItems
                    .Concat(singleItems)
                    .OrderBy(x => x.Time)
                    .SelectMany(x => x.Matches);
            })
            .ToList();

        return ordered;
    }

        public async Task SetUdostepnijGodzineAsync(int meczId, bool value)
        {
            var mecz = await _context.Mecze.FindAsync(meczId);
            if (mecz == null) throw new ArgumentException($"Nie znaleziono meczu {meczId}");
            mecz.UdostepnijGodzine = value;
            await _context.SaveChangesAsync();
        }

        public Mecz GetMeczById(int id)
            {
                return _context.Mecze
                        .Include(m => m.Rozgrywki)
                        .Include(m => m.SedziaI)
                        .Include(m => m.SedziaII)
                        .Include(m => m.SedziaSekretarz)
                        .Include(m => m.SedziaLiniowyI)
                        .Include(m => m.SedziaLiniowyII)
                        .Include(m => m.SedziaGlowny)   
                    .FirstOrDefault(m => m.Id == id);
            }


        public async Task UpdateMecz(Mecz mecz)
        {
            // pobierz poprzednią wersję, żeby wiedzieć kto był wcześniej w obsadzie
            var previous = await _context.Mecze.AsNoTracking().FirstOrDefaultAsync(m => m.Id == mecz.Id);
            if (previous == null) throw new ArgumentException($"Nie znaleziono meczu {mecz.Id}");

            _context.Mecze.Update(mecz);
            await _context.SaveChangesAsync();

            var newIds = new[] { mecz.SedziaIId, mecz.SedziaIIId, mecz.SedziaSekretarzId, mecz.SedziaLiniowyIId, mecz.SedziaLiniowyIIId, mecz.SedziaGlownyId }
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            var oldIds = new[] { previous.SedziaIId, previous.SedziaIIId, previous.SedziaSekretarzId, previous.SedziaLiniowyIId, previous.SedziaLiniowyIIId, previous.SedziaGlownyId }
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            var allIds = oldIds.Union(newIds).ToList();

            var sedziowie = await _context.Sedziowie
                .Where(s => allIds.Contains(s.Id))
                .ToListAsync();

            var sedziowieEmails = sedziowie
                .Select(s => s.Email)
                .Where(email => !string.IsNullOrEmpty(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var shouldSendNotifications = mecz.Data > DateTime.Now;

            // Wyślij powiadomienia do aktualnych (tak jak wcześniej)
            if (shouldSendNotifications && newIds.Any())
            {
                var emailsNew = sedziowie
                    .Where(s => newIds.Contains(s.Id))
                    .Select(s => s.Email)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (emailsNew.Any())
                {
                    try
                    {
                        await SendMatchEmail(mecz, emailsNew);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Błąd podczas wysyłania e-maila po edycji meczu.");
                    }
                }
            }

            // Wyślij osobne maile do tych, którzy byli wcześniej, a teraz nie są
            if (shouldSendNotifications)
            {
                var removedIds = oldIds.Except(newIds).ToList();
                foreach (var rid in removedIds)
                {
                    var removed = sedziowie.FirstOrDefault(s => s.Id == rid);
                    if (removed == null) continue;

                    // określ poprzednią rolę
                    var roles = new List<string>();
                    if (previous.SedziaIId == rid) roles.Add("Sędzia I");
                    if (previous.SedziaIIId == rid) roles.Add("Sędzia II");
                    if (previous.SedziaSekretarzId == rid) roles.Add("Sędzia Sekretarz");
                    if (previous.SedziaLiniowyIId == rid) roles.Add("Sędzia Liniowy I");
                    if (previous.SedziaLiniowyIIId == rid) roles.Add("Sędzia Liniowy II");
                    if (previous.SedziaGlownyId == rid) roles.Add("Sędzia Główny");

                    var roleDesc = string.Join(", ", roles);
                    try
                    {
                        await SendRemovalEmailAsync(mecz, removed, roleDesc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Błąd podczas wysyłania e-maila o usunięciu z obsady.");
                    }
                }
            }
        }


    public void DeleteMecz(int id)
        {
            var mecz = _context.Mecze.FirstOrDefault(m => m.Id == id);
            if (mecz != null)
            {
                _context.Mecze.Remove(mecz);
                _context.SaveChanges();
            }
        }

    IEnumerable<Sedzia> IMeczService.GetSedziowieByDate(DateTime data)
    {
        throw new NotImplementedException();
    }

    public List<Mecz> GetMeczeForUserId(string userId)
    {
        return _context.Mecze
            .Include(m => m.Rozgrywki)
            .Include(m => m.SedziaI)
            .Include(m => m.SedziaII)
            .Include(m => m.SedziaSekretarz)
            .Include(m => m.SedziaLiniowyI)
            .Include(m => m.SedziaLiniowyII)
            .Include(m => m.SedziaGlowny)
            .AsNoTracking()
            .Where(m => m.GospodarzKlubId == userId || m.GoscKlubId == userId)
            .OrderBy(m => m.Data)
            .ToList();
    }
}

