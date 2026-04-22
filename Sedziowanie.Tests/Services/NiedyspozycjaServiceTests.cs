using Sedziowanie.Models;
using Sedziowanie.Services;
using Sedziowanie.Tests.TestHelpers;
using Xunit;

namespace Sedziowanie.Tests.Services;

public class NiedyspozycjaServiceTests
{
    private static Sedzia MakeSedzia(int id) =>
        new Sedzia { Id = id, Imie = "Jan", Nazwisko = "Kowalski", Klasa = "K1", Email = $"s{id}@test.pl", Telefon = "000000000", Miasto = "Miasto" };

    // ── AddNiedyspozycja validation ───────────────────────────────────────────

    [Fact]
    public void AddNiedyspozycja_PoczatekGeKoniec_ThrowsArgumentException()
    {
        using var ctx = DbContextFactory.Create(nameof(AddNiedyspozycja_PoczatekGeKoniec_ThrowsArgumentException));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.SaveChanges();

        var svc = new NiedyspozycjaService(ctx);
        var ex = Assert.Throws<ArgumentException>(() =>
            svc.AddNiedyspozycja(1, DateTime.Today, DateTime.Today));

        Assert.Contains("wcześniejsza", ex.Message);
    }

    [Fact]
    public void AddNiedyspozycja_PoczatekAfterKoniec_ThrowsArgumentException()
    {
        using var ctx = DbContextFactory.Create(nameof(AddNiedyspozycja_PoczatekAfterKoniec_ThrowsArgumentException));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.SaveChanges();

        var svc = new NiedyspozycjaService(ctx);
        Assert.Throws<ArgumentException>(() =>
            svc.AddNiedyspozycja(1, DateTime.Today.AddDays(1), DateTime.Today));
    }

    [Fact]
    public void AddNiedyspozycja_SedziaNotFound_ThrowsArgumentException()
    {
        using var ctx = DbContextFactory.Create(nameof(AddNiedyspozycja_SedziaNotFound_ThrowsArgumentException));
        var svc = new NiedyspozycjaService(ctx);

        var ex = Assert.Throws<ArgumentException>(() =>
            svc.AddNiedyspozycja(999, DateTime.Today, DateTime.Today.AddDays(1)));

        Assert.Contains("nie istnieje", ex.Message);
    }

    [Fact]
    public void AddNiedyspozycja_ValidInput_PersistsRecord()
    {
        using var ctx = DbContextFactory.Create(nameof(AddNiedyspozycja_ValidInput_PersistsRecord));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.SaveChanges();

        var svc = new NiedyspozycjaService(ctx);
        var start = new DateTime(2025, 6, 1);
        var end = new DateTime(2025, 6, 10);
        svc.AddNiedyspozycja(1, start, end);

        var n = ctx.Niedyspozycje.Single();
        Assert.Equal(1, n.SedziaId);
        Assert.Equal(start, n.Poczatek);
        Assert.Equal(end, n.Koniec);
    }

    // ── DeleteNiedyspozycja ───────────────────────────────────────────────────

    [Fact]
    public void DeleteNiedyspozycja_NotFound_ReturnsFalse()
    {
        using var ctx = DbContextFactory.Create(nameof(DeleteNiedyspozycja_NotFound_ReturnsFalse));
        var svc = new NiedyspozycjaService(ctx);
        Assert.False(svc.DeleteNiedyspozycja(999));
    }

    [Fact]
    public void DeleteNiedyspozycja_Existing_ReturnsTrueAndRemoves()
    {
        using var ctx = DbContextFactory.Create(nameof(DeleteNiedyspozycja_Existing_ReturnsTrueAndRemoves));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.Niedyspozycje.Add(new Niedyspozycja
        {
            Id = 1,
            SedziaId = 1,
            Poczatek = DateTime.Today,
            Koniec = DateTime.Today.AddDays(3),
            DataDodania = DateTime.UtcNow
        });
        ctx.SaveChanges();

        var svc = new NiedyspozycjaService(ctx);
        var result = svc.DeleteNiedyspozycja(1);

        Assert.True(result);
        Assert.Empty(ctx.Niedyspozycje);
    }

    // ── GetForSedzia ─────────────────────────────────────────────────────────

    [Fact]
    public void GetForSedzia_ReturnsOnlyMatchingSedzia()
    {
        using var ctx = DbContextFactory.Create(nameof(GetForSedzia_ReturnsOnlyMatchingSedzia));
        ctx.Sedziowie.AddRange(MakeSedzia(1), MakeSedzia(2));
        ctx.Niedyspozycje.AddRange(
            new Niedyspozycja { Id = 1, SedziaId = 1, Poczatek = DateTime.Today, Koniec = DateTime.Today.AddDays(2), DataDodania = DateTime.UtcNow },
            new Niedyspozycja { Id = 2, SedziaId = 2, Poczatek = DateTime.Today, Koniec = DateTime.Today.AddDays(2), DataDodania = DateTime.UtcNow }
        );
        ctx.SaveChanges();

        var svc = new NiedyspozycjaService(ctx);
        var result = svc.GetForSedzia(1).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].SedziaId);
    }

    [Fact]
    public void GetForSedzia_ReturnsOrderedDescendingByPoczatek()
    {
        using var ctx = DbContextFactory.Create(nameof(GetForSedzia_ReturnsOrderedDescendingByPoczatek));
        ctx.Sedziowie.Add(MakeSedzia(1));
        ctx.Niedyspozycje.AddRange(
            new Niedyspozycja { Id = 1, SedziaId = 1, Poczatek = new DateTime(2025, 1, 1), Koniec = new DateTime(2025, 1, 5), DataDodania = DateTime.UtcNow },
            new Niedyspozycja { Id = 2, SedziaId = 1, Poczatek = new DateTime(2025, 6, 1), Koniec = new DateTime(2025, 6, 5), DataDodania = DateTime.UtcNow }
        );
        ctx.SaveChanges();

        var svc = new NiedyspozycjaService(ctx);
        var result = svc.GetForSedzia(1).ToList();

        Assert.Equal(new DateTime(2025, 6, 1), result[0].Poczatek);
        Assert.Equal(new DateTime(2025, 1, 1), result[1].Poczatek);
    }

    // ── GetAllNiedyspozycje ───────────────────────────────────────────────────

    [Fact]
    public void GetAllNiedyspozycje_ReturnsAllRecords()
    {
        using var ctx = DbContextFactory.Create(nameof(GetAllNiedyspozycje_ReturnsAllRecords));
        ctx.Sedziowie.AddRange(MakeSedzia(1), MakeSedzia(2));
        ctx.Niedyspozycje.AddRange(
            new Niedyspozycja { Id = 1, SedziaId = 1, Poczatek = DateTime.Today, Koniec = DateTime.Today.AddDays(1), DataDodania = DateTime.UtcNow },
            new Niedyspozycja { Id = 2, SedziaId = 2, Poczatek = DateTime.Today, Koniec = DateTime.Today.AddDays(1), DataDodania = DateTime.UtcNow }
        );
        ctx.SaveChanges();

        var svc = new NiedyspozycjaService(ctx);
        Assert.Equal(2, svc.GetAllNiedyspozycje().Count());
    }

    // ── GetSedziowieList ──────────────────────────────────────────────────────

    [Fact]
    public void GetSedziowieList_ReturnsSelectListItems()
    {
        using var ctx = DbContextFactory.Create(nameof(GetSedziowieList_ReturnsSelectListItems));
        ctx.Sedziowie.AddRange(
            MakeSedzia(1),
            new Sedzia { Id = 2, Imie = "Anna", Nazwisko = "Nowak", Klasa = "K2", Email = "s2@test.pl", Telefon = "000000000", Miasto = "Miasto" });
        ctx.SaveChanges();

        var svc = new NiedyspozycjaService(ctx);
        var list = svc.GetSedziowieList();

        Assert.Equal(2, list.Count);
        Assert.All(list, item => Assert.False(string.IsNullOrEmpty(item.Value)));
    }
}
