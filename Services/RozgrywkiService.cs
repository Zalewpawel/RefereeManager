using Microsoft.EntityFrameworkCore;
using Sedziowanie.Data;
using Sedziowanie.Models;
using Sedziowanie.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Sedziowanie.Services
{
    public class RozgrywkiService : IRozgrywkiService
    {
        private readonly DBObsadyContext _context;

        public RozgrywkiService(DBObsadyContext context)
        {
            _context = context;
        }

        public void AddRozgrywki(Rozgrywki rozgrywki)
        {
            _context.Rozgrywki.Add(rozgrywki);
            _context.SaveChanges();
        }

        public List<Rozgrywki> GetAllRozgrywki()
        {
            var preferredOrder = new List<string>
            {
                "I Liga Kobiet",
                "PLS 1.Liga Mężczyzn",
                "II Liga Kobiet",
                "II Liga Mężczyzn",
                "III Liga Kobiet",
                "III Liga Mężczyzn",

                "MW Juniorek",
                "MW Juniorów",
                "MW Kadetek",
                "MW Kadetów",
                "MW Młodziczek",
                "MW Młodzików",

                "Minisiatkówka",

                "MP Juniorek",
                "MP Juniorów",
                "MP Kadetek",
                "MP Kadetów",
                "MP Młodziczek",
                "MP Młodzików",

                "PALM - Liga Międzyuczelniana",
                "Turniej Towarzyski",
                "Siatkówka plażowa"
            };

            var list = _context.Rozgrywki
                .AsNoTracking()
                .OrderBy(r => r.Nazwa)
                .ToList();

            list = list
                .OrderBy(r =>
                {
                    var idx = preferredOrder.FindIndex(x => x == r.Nazwa);
                    return idx < 0 ? int.MaxValue : idx;
                })
                .ThenBy(r => r.Nazwa)
                .ToList();

            return list;
        }

        public IEnumerable<Mecz> GetMeczeForRozgrywki(int id)
        {
            var all = _context.Mecze
                .Include(m => m.SedziaI)
                .Include(m => m.SedziaII)
                .Include(m => m.SedziaSekretarz)
                .Include(m => m.SedziaLiniowyI)
                .Include(m => m.SedziaLiniowyII)
                .Include(m => m.SedziaGlowny)
                .Where(m => m.RozgrywkiId == id)
                .AsNoTracking()
                .ToList();

            var result = new List<Mecz>();

            foreach (var dayGroup in all.GroupBy(m => m.Data.Date).OrderBy(g => g.Key))
            {
                // tournament groups (non-empty Turniej)
                var tournGroups = dayGroup
                    .Where(m => !string.IsNullOrEmpty(m.Turniej))
                    .GroupBy(m => m.Turniej)
                    .Select(g => new
                    {
                        Key = g.Key,
                        Matches = g.OrderBy(m => m.Data).ToList(),
                        Start = g.Min(m => m.Data)
                    })
                    .ToList();

                // single matches (no Turniej) as individual items
                var singles = dayGroup
                    .Where(m => string.IsNullOrEmpty(m.Turniej))
                    .Select(m => new { Match = m, Time = m.Data })
                    .ToList();

                // build items: each single is an item; each tourn group is an item with Start time
                var items = new List<(DateTime Time, bool IsGroup, object Payload)>();

                foreach (var s in singles)
                    items.Add((s.Time, false, s.Match));

                foreach (var tg in tournGroups)
                    items.Add((tg.Start, true, tg));

                // order items by representative time
                foreach (var item in items.OrderBy(it => it.Time))
                {
                    if (!item.IsGroup)
                    {
                        result.Add((Mecz)item.Payload);
                    }
                    else
                    {
                        var tg = (dynamic)item.Payload;
                        foreach (Mecz mm in tg.Matches)
                            result.Add(mm);
                    }
                }
            }

            return result;
        }

        public string? GetRozgrywkiName(int id) =>
            _context.Rozgrywki.Where(r => r.Id == id).Select(r => r.Nazwa).FirstOrDefault();


        public string GetRozgrywkaName(int rozgrywkiId)
        {
            return _context.Rozgrywki
                .Where(r => r.Id == rozgrywkiId)
                .Select(r => r.Nazwa)
                .FirstOrDefault();
        }

        public Rozgrywki? GetById(int id)
        {
            return _context.Rozgrywki
                      .AsNoTracking()  
                      .FirstOrDefault(r => r.Id == id);
        }

        public void Update(Rozgrywki model)
        {
            var entity = _context.Rozgrywki.FirstOrDefault(r => r.Id == model.Id);
            if (entity == null) return;

            entity.Nazwa = model.Nazwa?.Trim();

            _context.SaveChanges();
        }

        public bool Delete(int id)
        {
            var entity = _context.Rozgrywki.FirstOrDefault(r => r.Id == id);
            if (entity == null) return true;

            var powiazaneMecze = _context.Mecze.Any(m => m.RozgrywkiId == id);
            if (powiazaneMecze)
            {
                return false;
            }

            _context.Rozgrywki.Remove(entity);
            _context.SaveChanges();
            return true;
        }


    }
}
