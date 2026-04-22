using Microsoft.AspNetCore.Mvc.Rendering;
using Sedziowanie.Models;
using System;
using System.Collections.Generic;

namespace Sedziowanie.Services.Interfaces
{
    public interface INiedyspozycjaService
    {
        
        IEnumerable<Niedyspozycja> GetAllNiedyspozycje();
        
        List<SelectListItem> GetSedziowieList();
        Sedzia GetSedziaByUserId(string userId);
        bool DeleteNiedyspozycja(int id);
        void AddNiedyspozycja(int sedziaId, DateTime poczatek, DateTime koniec);
        IEnumerable<Niedyspozycja> GetForSedzia(int sedziaId);
    }
}
