using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sedziowanie.Data;
using Sedziowanie.Models;
using Sedziowanie.Services.Extensions;
using Sedziowanie.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sedziowanie.Services
{
    public class NiedyspozycjaService : INiedyspozycjaService
    {
        private readonly DBObsadyContext _context;

        public NiedyspozycjaService(DBObsadyContext context)
        {
            _context = context;
        }


        public IEnumerable<Niedyspozycja> GetAllNiedyspozycje()
        {
            return _context.Niedyspozycje
                .Include(n => n.Sedzia)
                .AsNoTracking()
                .OrderByDescending(n => n.Poczatek)
                .ThenByDescending(n => n.Koniec)
                .ThenBy(n => n.Sedzia.Nazwisko)
                .ThenBy(n => n.Sedzia.Imie)
                .ToList();
        }

        public IEnumerable<Niedyspozycja> GetForSedzia(int sedziaId)
        {
            return _context.Niedyspozycje
                .Include(n => n.Sedzia)
                .AsNoTracking()
                .Where(n => n.SedziaId == sedziaId)
                .OrderByDescending(n => n.Poczatek)
                .ThenByDescending(n => n.Koniec)
                .ToList();
        }


        public List<SelectListItem> GetSedziowieList()
        {
            return _context.Sedziowie
                .AsNoTracking()
                .OrderByNazwiskoImie()
                .Select(s => new SelectListItem
                {
                    Text = s.Nazwisko + " " + s.Imie,
                    Value = s.Id.ToString()
                })
                .ToList();
        }

        
       
        public Sedzia GetSedziaByUserId(string userId)
        {
            var user = _context.Users.Include(u => u.Sedzia).FirstOrDefault(u => u.Id == userId);
            return user?.Sedzia;
        }



        public void AddNiedyspozycja(int sedziaId, DateTime poczatek, DateTime koniec)
        {
            if (poczatek >= koniec)
            {
                throw new ArgumentException("Data początku musi być wcześniejsza niż data końca.");
            }

            var sedzia = _context.Sedziowie.FirstOrDefault(s => s.Id == sedziaId);
            if (sedzia == null)
            {
                throw new ArgumentException("Wybrany sędzia nie istnieje.");
            }

            var niedyspozycja = new Niedyspozycja
            {
                SedziaId = sedzia.Id,
                Poczatek = poczatek,
                Koniec = koniec,
                DataDodania = DateTime.UtcNow
            };

            _context.Niedyspozycje.Add(niedyspozycja);
            _context.SaveChanges();
        }

        public bool DeleteNiedyspozycja(int id)
        {
            var entity = _context.Niedyspozycje.Find(id);
            if (entity == null) return false;

            _context.Niedyspozycje.Remove(entity);
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }


    }
}
