using Sedziowanie.Models;
using Sedziowanie.Services;
using Sedziowanie.Tests.TestHelpers;
using Xunit;

namespace Sedziowanie.Tests.Services;

public class RozgrywkiServiceTests
{
    private static Rozgrywki Make(int id, string nazwa) => new Rozgrywki { Id = id, Nazwa = nazwa };

    // ── AddRozgrywki ──────────────────────────────────────────────────────────

    [Fact]
    public void AddRozgrywki_PersistsRecord()
    {
        using var ctx = DbContextFactory.Create(nameof(AddRozgrywki_PersistsRecord));
        var svc = new RozgrywkiService(ctx);

        svc.AddRozgrywki(Make(0, "Nowe Rozgrywki"));

        Assert.Single(ctx.Rozgrywki);
        Assert.Equal("Nowe Rozgrywki", ctx.Rozgrywki.First().Nazwa);
    }

    // ── GetAllRozgrywki ordering ──────────────────────────────────────────────

    [Fact]
    public void GetAllRozgrywki_PreferredNamesFirst()
    {
        using var ctx = DbContextFactory.Create(nameof(GetAllRozgrywki_PreferredNamesFirst));
        ctx.Rozgrywki.AddRange(
            Make(1, "Turniej Towarzyski"),
            Make(2, "I Liga Kobiet"),
            Make(3, "III Liga Mężczyzn"),
            Make(4, "MW Juniorek"));
        ctx.SaveChanges();

        var svc = new RozgrywkiService(ctx);
        var result = svc.GetAllRozgrywki();

        Assert.Equal("I Liga Kobiet",    result[0].Nazwa);
        Assert.Equal("III Liga Mężczyzn",result[1].Nazwa);
        Assert.Equal("MW Juniorek",      result[2].Nazwa);
        Assert.Equal("Turniej Towarzyski", result[3].Nazwa);
    }

    [Fact]
    public void GetAllRozgrywki_UnknownNamesAppendedAlphabetically()
    {
        using var ctx = DbContextFactory.Create(nameof(GetAllRozgrywki_UnknownNamesAppendedAlphabetically));
        ctx.Rozgrywki.AddRange(
            Make(1, "Zzz Nieznana Liga"),
            Make(2, "Aaa Nieznana Liga"),
            Make(3, "I Liga Kobiet"));
        ctx.SaveChanges();

        var svc = new RozgrywkiService(ctx);
        var result = svc.GetAllRozgrywki();

        Assert.Equal("I Liga Kobiet",    result[0].Nazwa);
        Assert.Equal("Aaa Nieznana Liga", result[1].Nazwa);
        Assert.Equal("Zzz Nieznana Liga", result[2].Nazwa);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public void GetById_ExistingId_ReturnsRozgrywki()
    {
        using var ctx = DbContextFactory.Create(nameof(GetById_ExistingId_ReturnsRozgrywki));
        ctx.Rozgrywki.Add(Make(1, "I Liga Kobiet"));
        ctx.SaveChanges();

        var svc = new RozgrywkiService(ctx);
        var r = svc.GetById(1);

        Assert.NotNull(r);
        Assert.Equal("I Liga Kobiet", r!.Nazwa);
    }

    [Fact]
    public void GetById_NonExistingId_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create(nameof(GetById_NonExistingId_ReturnsNull));
        var svc = new RozgrywkiService(ctx);
        Assert.Null(svc.GetById(999));
    }

    // ── GetRozgrywkiName / GetRozgrywkaName ───────────────────────────────────

    [Fact]
    public void GetRozgrywkiName_ExistingId_ReturnsName()
    {
        using var ctx = DbContextFactory.Create(nameof(GetRozgrywkiName_ExistingId_ReturnsName));
        ctx.Rozgrywki.Add(Make(1, "PLS 1.Liga Mężczyzn"));
        ctx.SaveChanges();

        var svc = new RozgrywkiService(ctx);
        Assert.Equal("PLS 1.Liga Mężczyzn", svc.GetRozgrywkiName(1));
        Assert.Equal("PLS 1.Liga Mężczyzn", svc.GetRozgrywkaName(1));
    }

    [Fact]
    public void GetRozgrywkiName_NonExistingId_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create(nameof(GetRozgrywkiName_NonExistingId_ReturnsNull));
        var svc = new RozgrywkiService(ctx);
        Assert.Null(svc.GetRozgrywkiName(999));
        Assert.Null(svc.GetRozgrywkaName(999));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ExistingId_ChangesNameAndTrims()
    {
        using var ctx = DbContextFactory.Create(nameof(Update_ExistingId_ChangesNameAndTrims));
        ctx.Rozgrywki.Add(Make(1, "Stara Nazwa"));
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var svc = new RozgrywkiService(ctx);
        svc.Update(new Rozgrywki { Id = 1, Nazwa = "  Nowa Nazwa  " });

        Assert.Equal("Nowa Nazwa", ctx.Rozgrywki.Find(1)!.Nazwa);
    }

    [Fact]
    public void Update_NonExistingId_DoesNotThrow()
    {
        using var ctx = DbContextFactory.Create(nameof(Update_NonExistingId_DoesNotThrow));
        var svc = new RozgrywkiService(ctx);
        var ex = Record.Exception(() => svc.Update(new Rozgrywki { Id = 999, Nazwa = "X" }));
        Assert.Null(ex);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_NonExistingId_ReturnsTrue()
    {
        using var ctx = DbContextFactory.Create(nameof(Delete_NonExistingId_ReturnsTrue));
        var svc = new RozgrywkiService(ctx);
        Assert.True(svc.Delete(999));
    }

    [Fact]
    public void Delete_WithPowiazaneMecze_ReturnsFalse()
    {
        using var ctx = DbContextFactory.Create(nameof(Delete_WithPowiazaneMecze_ReturnsFalse));
        ctx.Rozgrywki.Add(Make(1, "I Liga Kobiet"));
        ctx.SaveChanges();
        ctx.Mecze.Add(new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B" });
        ctx.SaveChanges();

        var svc = new RozgrywkiService(ctx);
        Assert.False(svc.Delete(1));
        Assert.Single(ctx.Rozgrywki);
    }

    [Fact]
    public void Delete_NoMecze_ReturnsTrueAndRemoves()
    {
        using var ctx = DbContextFactory.Create(nameof(Delete_NoMecze_ReturnsTrueAndRemoves));
        ctx.Rozgrywki.Add(Make(1, "I Liga Kobiet"));
        ctx.SaveChanges();

        var svc = new RozgrywkiService(ctx);
        Assert.True(svc.Delete(1));
        Assert.Empty(ctx.Rozgrywki);
    }

    // ── GetMeczeForRozgrywki ──────────────────────────────────────────────────

    [Fact]
    public void GetMeczeForRozgrywki_ReturnsMeczeForGivenId()
    {
        using var ctx = DbContextFactory.Create(nameof(GetMeczeForRozgrywki_ReturnsMeczeForGivenId));
        ctx.Rozgrywki.AddRange(Make(1, "I Liga Kobiet"), Make(2, "II Liga Kobiet"));
        ctx.SaveChanges();
        ctx.Mecze.AddRange(
            new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B" },
            new Mecz { Id = 2, NumerMeczu = "M2", Data = DateTime.Today, RozgrywkiId = 2, Gospodarz = "C", Gosc = "D" }
        );
        ctx.SaveChanges();

        var svc = new RozgrywkiService(ctx);
        var result = svc.GetMeczeForRozgrywki(1).ToList();

        Assert.Single(result);
        Assert.Equal("M1", result[0].NumerMeczu);
    }

    [Fact]
    public void GetMeczeForRozgrywki_TournamentGroupsOrderedByTime()
    {
        using var ctx = DbContextFactory.Create(nameof(GetMeczeForRozgrywki_TournamentGroupsOrderedByTime));
        ctx.Rozgrywki.Add(Make(1, "I Liga Kobiet"));
        ctx.SaveChanges();

        var day = new DateTime(2025, 5, 10);
        ctx.Mecze.AddRange(
            new Mecz { Id = 1, NumerMeczu = "M1", Data = day.AddHours(14), RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", Turniej = "Turniej A" },
            new Mecz { Id = 2, NumerMeczu = "M2", Data = day.AddHours(10), RozgrywkiId = 1, Gospodarz = "C", Gosc = "D", Turniej = "Turniej A" },
            new Mecz { Id = 3, NumerMeczu = "M3", Data = day.AddHours(12), RozgrywkiId = 1, Gospodarz = "E", Gosc = "F" }
        );
        ctx.SaveChanges();

        var svc = new RozgrywkiService(ctx);
        var result = svc.GetMeczeForRozgrywki(1).ToList();

        // Turniej A starts at 10:00, single match at 12:00
        // So Turniej A (M2 then M1) come first, then M3
        Assert.Equal(3, result.Count);
        Assert.Equal("M2", result[0].NumerMeczu);
        Assert.Equal("M1", result[1].NumerMeczu);
        Assert.Equal("M3", result[2].NumerMeczu);
    }
}
