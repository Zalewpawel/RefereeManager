using Sedziowanie.Models;
using System.Linq;

namespace Sedziowanie.Services.Extensions
{
    public static class SedziaSortingExtensions
    {
        public static IQueryable<Sedzia> OrderByNazwiskoImie(this IQueryable<Sedzia> query)
        {
            return query
                .OrderBy(s => s.Nazwisko)
                .ThenBy(s => s.Imie);
        }
    }
}
