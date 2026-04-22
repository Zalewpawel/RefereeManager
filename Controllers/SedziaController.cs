using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sedziowanie.Models;
using Sedziowanie.Services.Interfaces;
using Sedziowanie.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sedziowanie.Controllers
{
    public class SedziaController : Controller
    {
        private readonly ISedziaService _sedziaService;

        public SedziaController(ISedziaService sedziaService)
        {
            _sedziaService = sedziaService;
        }

        [HttpGet]
        public IActionResult ShowAll()
        {
            var sedziowie = _sedziaService.GetAllSedziowie();
            return View(sedziowie);
        }
        [HttpGet]
        public IActionResult ShowBezDanych()
        {
            var sedziowie = _sedziaService.GetAllSedziowie();
            return View(sedziowie);
        }

        [HttpGet]
        public IActionResult ShowSedzia()
        {
            var sedziowie = _sedziaService.GetAllSedziowie();
            return View(sedziowie);
        }

        [HttpGet]
        public IActionResult Add()
        {
            var model = new Sedzia
            {
                CzyUrlop = false
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Add(Sedzia sedzia)
        {
            if (!ModelState.IsValid)
            {
                return View(sedzia);
            }

            _sedziaService.AddSedzia(sedzia);
            return RedirectToAction("ShowAll");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var sedzia = _sedziaService.GetSedziaById(id);
            if (sedzia == null)
            {
                return NotFound();
            }
            return View(sedzia);
        }

        [HttpPost]
        public IActionResult Edit(Sedzia sedzia)
        {
            if (!ModelState.IsValid)
            {
                return View(sedzia);
            }

            _sedziaService.UpdateSedzia(sedzia);
            return RedirectToAction("ShowAll");
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var sedzia = _sedziaService.GetSedziaById(id);
            if (sedzia == null)
            {
                return NotFound();
            }
            return View(sedzia);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            _sedziaService.DeleteSedzia(id);
            return RedirectToAction("ShowAll");
        }

        [HttpGet]
        public IActionResult MeczeSedziego(int sedziaId)
        {
            var mecze = _sedziaService.GetMeczeForSedzia(sedziaId);
            ViewBag.Sedzia = _sedziaService.GetSedziaName(sedziaId);
            return View(mecze);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Informacje(int id)
        {
            var sedzia = _sedziaService.GetSedziaById(id);
            if (sedzia == null) return NotFound();

            var mecze = _sedziaService.GetMeczeForSedzia(id);
            ViewBag.MeczeCount = mecze?.Count() ?? 0;
            ViewBag.Stats = _sedziaService.GetSedziaStatistics().FirstOrDefault(s => s.SedziaId == id);

            return View(sedzia);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Statystyki(string? sortBy = "MeczeCount", bool desc = true)
        {
            var stats = SortStats(_sedziaService.GetSedziaStatistics(), sortBy, desc);

            ViewBag.SortBy = sortBy;
            ViewBag.Desc = desc;
            return View(stats);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult ExportStatystykiExcel(string? sortBy = "MeczeCount", bool desc = true)
        {
            var stats = SortStats(_sedziaService.GetSedziaStatistics(), sortBy, desc);

            var sb = new StringBuilder();
            sb.AppendLine("Miejsce;Nazwisko;Imię;Łącznie;SI;SII;SS;SG;L;SC;3;J;K;MŁ;MMP;Mini;Tow;Plaża;PALM;Duet;Duet Mecze");

            var lp = 1;
            foreach (var s in stats)
            {
                sb.AppendLine(string.Join(";",
                    lp,
                    CsvEscape(s.Nazwisko),
                    CsvEscape(s.Imie),
                    s.MeczeCount,
                    s.SiCount,
                    s.SiiCount,
                    s.SsCount,
                    s.SgCount,
                    s.LCount,
                    s.SzczebelCount,
                    s.Liga3Count,
                    s.JCount,
                    s.KCount,
                    s.MlCount,
                    s.MmpCount,
                    s.MiniCount,
                    s.TowCount,
                    s.PlazaCount,
                    s.PalmCount,
                    CsvEscape(s.DuetSedzia),
                    s.DuetCount));

                lp++;
            }

            var fileName = $"StatystykiSedziow_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            using var ms = new MemoryStream();
            using (var writer = new StreamWriter(ms, new UTF8Encoding(true), leaveOpen: true))
            {
                writer.Write(sb.ToString());
                writer.Flush();
            }

            return File(ms.ToArray(), "text/csv; charset=utf-8", fileName);
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var escaped = value.Replace("\"", "\"\"");
            if (escaped.Contains(';') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
            {
                return $"\"{escaped}\"";
            }

            return escaped;
        }

        private static List<SedziaStatsDto> SortStats(List<SedziaStatsDto> stats, string? sortBy, bool desc)
        {
            return sortBy switch
            {
                "Nazwisko" => desc ? stats.OrderByDescending(x => x.Nazwisko).ThenByDescending(x => x.Imie).ToList()
                                    : stats.OrderBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "Imie" => desc ? stats.OrderByDescending(x => x.Imie).ThenByDescending(x => x.Nazwisko).ToList()
                                : stats.OrderBy(x => x.Imie).ThenBy(x => x.Nazwisko).ToList(),
                "MeczeCount" => desc ? stats.OrderByDescending(x => x.MeczeCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                      : stats.OrderBy(x => x.MeczeCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "SiCount" => desc ? stats.OrderByDescending(x => x.SiCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                   : stats.OrderBy(x => x.SiCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "SiiCount" => desc ? stats.OrderByDescending(x => x.SiiCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                    : stats.OrderBy(x => x.SiiCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "SsCount" => desc ? stats.OrderByDescending(x => x.SsCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                   : stats.OrderBy(x => x.SsCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "SgCount" => desc ? stats.OrderByDescending(x => x.SgCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                   : stats.OrderBy(x => x.SgCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "LCount" => desc ? stats.OrderByDescending(x => x.LCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                  : stats.OrderBy(x => x.LCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "SzczebelCount" => desc ? stats.OrderByDescending(x => x.SzczebelCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                         : stats.OrderBy(x => x.SzczebelCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "Liga3Count" => desc ? stats.OrderByDescending(x => x.Liga3Count).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                      : stats.OrderBy(x => x.Liga3Count).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "JCount" => desc ? stats.OrderByDescending(x => x.JCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                  : stats.OrderBy(x => x.JCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "KCount" => desc ? stats.OrderByDescending(x => x.KCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                  : stats.OrderBy(x => x.KCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "MlCount" => desc ? stats.OrderByDescending(x => x.MlCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                   : stats.OrderBy(x => x.MlCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "MmpCount" => desc ? stats.OrderByDescending(x => x.MmpCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                    : stats.OrderBy(x => x.MmpCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "MiniCount" => desc ? stats.OrderByDescending(x => x.MiniCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                     : stats.OrderBy(x => x.MiniCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "TowCount" => desc ? stats.OrderByDescending(x => x.TowCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                    : stats.OrderBy(x => x.TowCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "PlazaCount" => desc ? stats.OrderByDescending(x => x.PlazaCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                     : stats.OrderBy(x => x.PlazaCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "PalmCount" => desc ? stats.OrderByDescending(x => x.PalmCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                     : stats.OrderBy(x => x.PalmCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                "DuetCount" => desc ? stats.OrderByDescending(x => x.DuetCount).ThenBy(x => x.DuetSedzia).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
                                     : stats.OrderBy(x => x.DuetCount).ThenBy(x => x.DuetSedzia).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList(),
                _ => stats.OrderByDescending(x => x.MeczeCount).ThenBy(x => x.Nazwisko).ThenBy(x => x.Imie).ToList()
            };
        }
    }
}
