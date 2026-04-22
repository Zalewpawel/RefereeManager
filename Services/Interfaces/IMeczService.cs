using Sedziowanie.Models;
using Sedziowanie.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sedziowanie.Services.Interfaces
{
    public interface IMeczService
    {
        IEnumerable<Sedzia> GetSedziowieByDate(DateTime data);
        IEnumerable<Rozgrywki> GetRozgrywki();
        List<Mecz> GetAllMecze();
        Mecz GetMeczById(int id);
        Task AddMecz(string numerMeczu, DateTime data, int rozgrywkiId, string gospodarz, string gosc,
                              int? sedziaIId, int? sedziaIIId, int? sedziaSekretarzId, int? sedziaLiniowyIId, int? sedziaLiniowyIIId, int? sedziaGlownyId,
                              string? turniej = null, string? adres = null, string? dodatkoweInformacje = null,
                              string? gospodarzKlubId = null, string? goscKlubId = null);
        Task UpdateMecz(Mecz mecz);
        void DeleteMecz(int id);
        IEnumerable<SedziaOptionDto> GetSedziowieByDateWithStatus(DateTime data);
        Task SetUdostepnijGodzineAsync(int meczId, bool value);
        List<Mecz> GetMeczeForUserId(string userId);
    }
}
