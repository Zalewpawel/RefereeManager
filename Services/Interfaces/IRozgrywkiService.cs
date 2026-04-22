using Sedziowanie.Models;
using System.Collections.Generic;

namespace Sedziowanie.Services.Interfaces
{
    public interface IRozgrywkiService
    {
        void AddRozgrywki(Rozgrywki rozgrywki);
        List<Rozgrywki> GetAllRozgrywki();
        IEnumerable<Mecz> GetMeczeForRozgrywki(int rozgrywkiId);
        string GetRozgrywkaName(int rozgrywkiId);
        bool Delete(int id);
        void Update(Rozgrywki model);
        Rozgrywki? GetById(int id);

    }
}
