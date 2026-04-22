using Sedziowanie.Models;
using Sedziowanie.Services;
using Sedziowanie.Tests.TestHelpers;
using Xunit;

namespace Sedziowanie.Tests.Services;

public class SedziaServiceTests
{
    private static Sedzia MakeSedzia(int id, string imie = "Jan", string nazwisko = "Kowalski", string klasa = "K1") =>
        new Sedzia { Id = id, Imie = imie, Nazwisko = nazwisko, Klasa = klasa, Email = $"s{id}@test.pl", Telefon = "000000000", Miasto = "Miasto" };

    private static Rozgrywki MakeRozgrywki(int id, string nazwa) =>
        new Rozgrywki { Id = id, Nazwa = nazwa };

    // ── GetAllSedziowie ──────────────────────────────────────────────────────

    [Fact]
    public void GetAllSedziowie_ReturnsAllSedziowie_OrderedByNazwiskoImie()
    {
        using var ctx = DbContextFactory.Create(nameof(GetAllSedziowie_ReturnsAllSedziowie_OrderedByNazwiskoImie));
        ctx.Sedziowie.AddRange(
            MakeSedzia(1, "Piotr", "Zalewski"),
            MakeSedzia(2, "Adam",  "Adamski"),
            MakeSedzia(3, "Zofia", "Adamski"));
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        var result = svc.GetAllSedziowie();

        Assert.Equal(3, result.Count);
        Assert.Equal("Adamski", result[0].Nazwisko);
        Assert.Equal("Adam",    result[0].Imie);
        Assert.Equal("Adamski", result[1].Nazwisko);
        Assert.Equal("Zofia",   result[1].Imie);
        Assert.Equal("Zalewski",result[2].Nazwisko);
    }

    // ── GetSedziaById ────────────────────────────────────────────────────────

    [Fact]
    public void GetSedziaById_ExistingId_ReturnsSedzia()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaById_ExistingId_ReturnsSedzia));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        var result = svc.GetSedziaById(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void GetSedziaById_NonExistingId_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaById_NonExistingId_ReturnsNull));
        var svc = new SedziaService(ctx);
        Assert.Null(svc.GetSedziaById(999));
    }

    // ── AddSedzia ────────────────────────────────────────────────────────────

    [Fact]
    public void AddSedzia_PersistsSedzia()
    {
        using var ctx = DbContextFactory.Create(nameof(AddSedzia_PersistsSedzia));
        var svc = new SedziaService(ctx);

        svc.AddSedzia(MakeSedzia(0, "Anna", "Nowak"));

        Assert.Equal(1, ctx.Sedziowie.Count());
        Assert.Equal("Nowak", ctx.Sedziowie.First().Nazwisko);
    }

    // ── UpdateSedzia ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateSedzia_ModifiesExistingSedzia()
    {
        using var ctx = DbContextFactory.Create(nameof(UpdateSedzia_ModifiesExistingSedzia));
        var s = MakeSedzia(1, "Jan", "Kowalski");
        ctx.Sedziowie.Add(s);
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var svc = new SedziaService(ctx);
        s.Nazwisko = "Zmieniony";
        svc.UpdateSedzia(s);

        Assert.Equal("Zmieniony", ctx.Sedziowie.Find(1)!.Nazwisko);
    }

    // ── DeleteSedzia ─────────────────────────────────────────────────────────

    [Fact]
    public void DeleteSedzia_ExistingId_RemovesSedzia()
    {
        using var ctx = DbContextFactory.Create(nameof(DeleteSedzia_ExistingId_RemovesSedzia));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        svc.DeleteSedzia(1);

        Assert.Empty(ctx.Sedziowie);
    }

    [Fact]
    public void DeleteSedzia_NonExistingId_DoesNotThrow()
    {
        using var ctx = DbContextFactory.Create(nameof(DeleteSedzia_NonExistingId_DoesNotThrow));
        var svc = new SedziaService(ctx);

        var ex = Record.Exception(() => svc.DeleteSedzia(999));
        Assert.Null(ex);
    }

    // ── GetSedziaName ─────────────────────────────────────────────────────────

    [Fact]
    public void GetSedziaName_ExistingId_ReturnsFullName()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaName_ExistingId_ReturnsFullName));
        ctx.Sedziowie.Add(MakeSedzia(1, "Jan", "Kowalski"));
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        Assert.Equal("Jan Kowalski", svc.GetSedziaName(1));
    }

    [Fact]
    public void GetSedziaName_NonExistingId_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaName_NonExistingId_ReturnsNull));
        var svc = new SedziaService(ctx);
        Assert.Null(svc.GetSedziaName(999));
    }

    // ── GetMeczeForSedzia ─────────────────────────────────────────────────────

    [Fact]
    public void GetMeczeForSedzia_AsAllSixRoles_ReturnsMatch()
    {
        using var ctx = DbContextFactory.Create(nameof(GetMeczeForSedzia_AsAllSixRoles_ReturnsMatch));
        var r = MakeRozgrywki(1, "I Liga Kobiet");
        ctx.Rozgrywki.Add(r);
        var s1 = MakeSedzia(1); var s2 = MakeSedzia(2); var s3 = MakeSedzia(3);
        var s4 = MakeSedzia(4); var s5 = MakeSedzia(5); var s6 = MakeSedzia(6);
        ctx.Sedziowie.AddRange(s1, s2, s3, s4, s5, s6);
        ctx.SaveChanges();

        ctx.Mecze.AddRange(
            new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1 },
            new Mecz { Id = 2, NumerMeczu = "M2", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIIId = 2 },
            new Mecz { Id = 3, NumerMeczu = "M3", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaSekretarzId = 3 },
            new Mecz { Id = 4, NumerMeczu = "M4", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaLiniowyIId = 4 },
            new Mecz { Id = 5, NumerMeczu = "M5", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaLiniowyIIId = 5 },
            new Mecz { Id = 6, NumerMeczu = "M6", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaGlownyId = 6 }
        );
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        for (int i = 1; i <= 6; i++)
        {
            var mecze = svc.GetMeczeForSedzia(i).ToList();
            Assert.Single(mecze);
        }
    }

    [Fact]
    public void GetMeczeForSedzia_SedziaNotInAnyMatch_ReturnsEmpty()
    {
        using var ctx = DbContextFactory.Create(nameof(GetMeczeForSedzia_SedziaNotInAnyMatch_ReturnsEmpty));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        Assert.Empty(svc.GetMeczeForSedzia(1));
    }

    // ── GetSedziaStatistics ───────────────────────────────────────────────────

    [Fact]
    public void GetSedziaStatistics_CountsLeagueCategories()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaStatistics_CountsLeagueCategories));
        var categories = new[]
        {
            "I Liga Kobiet",           // szczebel
            "III Liga Kobiet",         // liga3
            "MW Juniorek",             // J
            "MW Kadetek",              // K
            "MW Młodziczek",           // Ml
            "MP Juniorek",             // Mmp
            "Minisiatkówka",           // Mini
            "Turniej Towarzyski",      // Tow
            "Siatkówka plażowa",       // Plaża
            "PALM - Liga Międzyuczelniana" // Palm
        };

        for (int i = 0; i < categories.Length; i++)
            ctx.Rozgrywki.Add(new Rozgrywki { Id = i + 1, Nazwa = categories[i] });

        ctx.Sedziowie.Add(MakeSedzia(1, "Jan", "Kowalski"));
        ctx.SaveChanges();

        for (int i = 0; i < categories.Length; i++)
        {
            ctx.Mecze.Add(new Mecz
            {
                Id = i + 1,
                NumerMeczu = $"M{i + 1}",
                Data = DateTime.Today,
                RozgrywkiId = i + 1,
                Gospodarz = "A",
                Gosc = "B",
                SedziaIId = 1
            });
        }
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        var stats = svc.GetSedziaStatistics();

        Assert.Single(stats);
        var s = stats[0];
        Assert.Equal(10, s.MeczeCount);
        Assert.Equal(1, s.SzczebelCount);
        Assert.Equal(1, s.Liga3Count);
        Assert.Equal(1, s.JCount);
        Assert.Equal(1, s.KCount);
        Assert.Equal(1, s.MlCount);
        Assert.Equal(1, s.MmpCount);
        Assert.Equal(1, s.MiniCount);
        Assert.Equal(1, s.TowCount);
        Assert.Equal(1, s.PlazaCount);
        Assert.Equal(1, s.PalmCount);
    }

    [Fact]
    public void GetSedziaStatistics_ExcludesTowAndMiniAndPlazaFromSiSiiSsCounts()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaStatistics_ExcludesTowAndMiniAndPlazaFromSiSiiSsCounts));
        ctx.Rozgrywki.AddRange(
            new Rozgrywki { Id = 1, Nazwa = "I Liga Kobiet" },
            new Rozgrywki { Id = 2, Nazwa = "Turniej Towarzyski" });
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.SaveChanges();

        ctx.Mecze.AddRange(
            new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1 },
            new Mecz { Id = 2, NumerMeczu = "M2", Data = DateTime.Today, RozgrywkiId = 2, Gospodarz = "A", Gosc = "B", SedziaIId = 1 }
        );
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        var stats = svc.GetSedziaStatistics();

        var s = stats[0];
        Assert.Equal(2, s.MeczeCount);
        Assert.Equal(1, s.SiCount);  // Turniej Towarzyski excluded from SI count
    }

    [Fact]
    public void GetSedziaStatistics_DuetTracking_FindsMostFrequentPartner()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaStatistics_DuetTracking_FindsMostFrequentPartner));
        ctx.Rozgrywki.Add(new Rozgrywki { Id = 1, Nazwa = "I Liga Kobiet" });
        ctx.Sedziowie.AddRange(
            MakeSedzia(1, "Jan",  "Alpha"),
            MakeSedzia(2, "Piotr","Beta"),
            MakeSedzia(3, "Marek","Gamma"));
        ctx.SaveChanges();

        // s1 plays 2 matches with s2, 1 match with s3
        ctx.Mecze.AddRange(
            new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1, SedziaIIId = 2 },
            new Mecz { Id = 2, NumerMeczu = "M2", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1, SedziaIIId = 2 },
            new Mecz { Id = 3, NumerMeczu = "M3", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1, SedziaIIId = 3 }
        );
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        var stats = svc.GetSedziaStatistics();

        var s1Stats = stats.Single(x => x.SedziaId == 1);
        Assert.Equal(2, s1Stats.DuetCount);
        Assert.Contains("Beta", s1Stats.DuetSedzia);
    }

    [Fact]
    public void GetSedziaStatistics_NoMatches_DuetCountIsZero()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaStatistics_NoMatches_DuetCountIsZero));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        var stats = svc.GetSedziaStatistics();

        Assert.Single(stats);
        Assert.Equal(0, stats[0].DuetCount);
    }

    [Fact]
    public void GetSedziaStatistics_OrderedByMeczeCountDescending()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziaStatistics_OrderedByMeczeCountDescending));
        ctx.Rozgrywki.Add(new Rozgrywki { Id = 1, Nazwa = "I Liga Kobiet" });
        ctx.Sedziowie.AddRange(MakeSedzia(1, "Jan", "Kowalski"), MakeSedzia(2, "Anna", "Nowak"));
        ctx.SaveChanges();

        ctx.Mecze.AddRange(
            new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 2 },
            new Mecz { Id = 2, NumerMeczu = "M2", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 2 },
            new Mecz { Id = 3, NumerMeczu = "M3", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1 }
        );
        ctx.SaveChanges();

        var svc = new SedziaService(ctx);
        var stats = svc.GetSedziaStatistics();

        Assert.Equal(2, stats[0].SedziaId); // Nowak has 2 matches
        Assert.Equal(1, stats[1].SedziaId); // Kowalski has 1 match
    }
}
