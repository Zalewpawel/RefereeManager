using Microsoft.EntityFrameworkCore;
using Sedziowanie.Data;
using Sedziowanie.Models;
using Sedziowanie.Services.Extensions;
using Sedziowanie.Services.Interfaces;
using Sedziowanie.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Sedziowanie.Services
{
    public class SedziaService : ISedziaService
    {
        private readonly DBObsadyContext _context;

        public SedziaService(DBObsadyContext context)
        {
            _context = context;
        }

        public List<Sedzia> GetAllSedziowie()
        {
            return _context.Sedziowie
                .AsNoTracking()
                .OrderByNazwiskoImie()
                .ToList();
        }

        public Sedzia GetSedziaById(int id)
        {
            return _context.Sedziowie.FirstOrDefault(s => s.Id == id);
        }

        public void AddSedzia(Sedzia sedzia)
        {
            _context.Sedziowie.Add(sedzia);
            _context.SaveChanges();
        }

        public void UpdateSedzia(Sedzia sedzia)
        {
            _context.Sedziowie.Update(sedzia);
            _context.SaveChanges();
        }

        public void DeleteSedzia(int id)
        {
            var sedzia = _context.Sedziowie.FirstOrDefault(s => s.Id == id);
            if (sedzia != null)
            {
                _context.Sedziowie.Remove(sedzia);
                _context.SaveChanges();
            }
        }

        public IEnumerable<Mecz> GetMeczeForSedzia(int sedziaId)
        {
            return _context.Mecze
                .Include(m => m.Rozgrywki)
                .Include(m => m.SedziaI)
                .Include(m => m.SedziaII)
                .Include(m => m.SedziaSekretarz)
                .Include(m => m.SedziaLiniowyI)
                .Include(m => m.SedziaLiniowyII)
                .Include(m => m.SedziaGlowny)
                .Where(m =>
                    m.SedziaIId == sedziaId ||
                    m.SedziaIIId == sedziaId ||
                    m.SedziaSekretarzId == sedziaId ||
                    m.SedziaLiniowyIId == sedziaId ||
                    m.SedziaLiniowyIIId == sedziaId ||
                    m.SedziaGlownyId == sedziaId)
                .OrderBy(m => m.Data)
                .AsNoTracking()
                .ToList();
        }

        public string GetSedziaName(int sedziaId)
        {
            return _context.Sedziowie
                .Where(s => s.Id == sedziaId)
                .Select(s => s.Imie + " " + s.Nazwisko)
                .FirstOrDefault();
        }

        public List<SedziaStatsDto> GetSedziaStatistics()
        {
            var szczebel = new HashSet<string>
            {
                "I Liga Kobiet",
                "PLS 1. Liga Mężczyzn",
                "PLS 1.Liga Mężczyzn",
                "II Liga Kobiet",
                "II Liga Mężczyzn"
            };

            var liga3 = new HashSet<string>
            {
                "III Liga Kobiet",
                "III Liga Mężczyzn"
            };

            var grupaJ = new HashSet<string>
            {
                "MW Juniorek",
                "MW Juniorów"
            };

            var grupaK = new HashSet<string>
            {
                "MW Kadetek",
                "MW Kadetów"
            };

            var grupaMl = new HashSet<string>
            {
                "MW Młodziczek",
                "MW Młodzików"
            };

            var grupaMmp = new HashSet<string>
            {
                "MP Juniorek",
                "MP Juniorów",
                "MP Kadetek",
                "MP Kadetów",
                "MP Młodziczek",
                "MP Młodzików"
            };

            var sedziowie = _context.Sedziowie
                .AsNoTracking()
                .Select(s => new { s.Id, s.Imie, s.Nazwisko })
                .ToList();

            var mecze = _context.Mecze
                .AsNoTracking()
                .Include(m => m.Rozgrywki)
                .Select(m => new
                {
                    m.SedziaIId,
                    m.SedziaIIId,
                    m.SedziaSekretarzId,
                    m.SedziaLiniowyIId,
                    m.SedziaLiniowyIIId,
                    m.SedziaGlownyId,
                    RozgrywkiNazwa = m.Rozgrywki != null ? m.Rozgrywki.Nazwa : string.Empty
                })
                .ToList();

            var stats = sedziowie
                .Select(s =>
                {
                    var meczeSedziego = mecze.Where(m =>
                        m.SedziaIId == s.Id ||
                        m.SedziaIIId == s.Id ||
                        m.SedziaSekretarzId == s.Id ||
                        m.SedziaLiniowyIId == s.Id ||
                        m.SedziaLiniowyIIId == s.Id ||
                        m.SedziaGlownyId == s.Id).ToList();

                    var meczeSedziegoBezRoli = meczeSedziego
                        .Where(m =>
                            (m.RozgrywkiNazwa ?? string.Empty) != "Turniej Towarzyski" &&
                            (m.RozgrywkiNazwa ?? string.Empty) != "Minisiatkówka" &&
                            (m.RozgrywkiNazwa ?? string.Empty) != "Siatkówka plażowa")
                        .ToList();

                    var duetCounts = new Dictionary<int, int>();
                    foreach (var m in meczeSedziego)
                    {
                        var ids = new[]
                        {
                            m.SedziaIId,
                            m.SedziaIIId,
                            m.SedziaSekretarzId,
                            m.SedziaLiniowyIId,
                            m.SedziaLiniowyIIId,
                            m.SedziaGlownyId
                        }
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .Distinct()
                        .Where(id => id != s.Id);

                        foreach (var id in ids)
                        {
                            if (duetCounts.ContainsKey(id))
                                duetCounts[id]++;
                            else
                                duetCounts[id] = 1;
                        }
                    }

                    var topDuet = duetCounts
                        .OrderByDescending(x => x.Value)
                        .ThenBy(x => sedziowie.First(ss => ss.Id == x.Key).Nazwisko)
                        .ThenBy(x => sedziowie.First(ss => ss.Id == x.Key).Imie)
                        .FirstOrDefault();

                    var duetName = topDuet.Key == 0
                        ? "-"
                        : sedziowie
                            .Where(ss => ss.Id == topDuet.Key)
                            .Select(ss => ss.Nazwisko + " " + ss.Imie)
                            .FirstOrDefault() ?? "-";

                    return new SedziaStatsDto
                    {
                        SedziaId = s.Id,
                        Imie = s.Imie,
                        Nazwisko = s.Nazwisko,
                        MeczeCount = meczeSedziego.Count,
                        SiCount = meczeSedziegoBezRoli.Count(m => m.SedziaIId == s.Id),
                        SiiCount = meczeSedziegoBezRoli.Count(m => m.SedziaIIId == s.Id),
                        SsCount = meczeSedziegoBezRoli.Count(m => m.SedziaSekretarzId == s.Id),
                        SgCount = meczeSedziegoBezRoli.Count(m => m.SedziaGlownyId == s.Id),
                        LCount = meczeSedziegoBezRoli.Sum(m => (m.SedziaLiniowyIId == s.Id ? 1 : 0) + (m.SedziaLiniowyIIId == s.Id ? 1 : 0)),
                        SzczebelCount = meczeSedziego.Count(m => szczebel.Contains(m.RozgrywkiNazwa ?? string.Empty)),
                        Liga3Count = meczeSedziego.Count(m => liga3.Contains(m.RozgrywkiNazwa ?? string.Empty)),
                        JCount = meczeSedziego.Count(m => grupaJ.Contains(m.RozgrywkiNazwa ?? string.Empty)),
                        KCount = meczeSedziego.Count(m => grupaK.Contains(m.RozgrywkiNazwa ?? string.Empty)),
                        MlCount = meczeSedziego.Count(m => grupaMl.Contains(m.RozgrywkiNazwa ?? string.Empty)),
                        MmpCount = meczeSedziego.Count(m => grupaMmp.Contains(m.RozgrywkiNazwa ?? string.Empty)),
                        MiniCount = meczeSedziego.Count(m => (m.RozgrywkiNazwa ?? string.Empty) == "Minisiatkówka"),
                        TowCount = meczeSedziego.Count(m => (m.RozgrywkiNazwa ?? string.Empty) == "Turniej Towarzyski"),
                        PlazaCount = meczeSedziego.Count(m => (m.RozgrywkiNazwa ?? string.Empty) == "Siatkówka plażowa"),
                        PalmCount = meczeSedziego.Count(m => (m.RozgrywkiNazwa ?? string.Empty) == "PALM - Liga Międzyuczelniana"),
                        DuetSedzia = duetName,
                        DuetCount = topDuet.Key == 0 ? 0 : topDuet.Value
                    };
                })
                .OrderByDescending(x => x.MeczeCount)
                .ThenBy(x => x.Nazwisko)
                .ThenBy(x => x.Imie)
                .ToList();

            return stats;
        }
    }
}
