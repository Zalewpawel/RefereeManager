using Microsoft.Extensions.Logging;
using Moq;
using Sedziowanie.Models;
using Sedziowanie.Services.Interfaces;
using Sedziowanie.Tests.TestHelpers;
using Xunit;

namespace Sedziowanie.Tests.Services;

public class MeczServiceTests
{
    private static Sedzia MakeSedzia(int id, string email = "") =>
        new Sedzia { Id = id, Imie = "Jan", Nazwisko = "Kowalski", Klasa = "K1", Email = email, Telefon = "000000000", Miasto = "Miasto" };

    private static Rozgrywki MakeRozgrywki(int id, string nazwa = "I Liga Kobiet") =>
        new Rozgrywki { Id = id, Nazwa = nazwa };

    private MeczService CreateService(string dbName,
        IEmailService? emailSvc = null,
        ILogger<MeczService>? logger = null)
    {
        var ctx = DbContextFactory.Create(dbName);
        emailSvc ??= Mock.Of<IEmailService>();
        logger ??= Mock.Of<ILogger<MeczService>>();
        return new MeczService(ctx, emailSvc, logger);
    }

    private (MeczService svc, Sedziowanie.Data.DBObsadyContext ctx) CreateServiceWithCtx(
        string dbName,
        IEmailService? emailSvc = null,
        ILogger<MeczService>? logger = null)
    {
        var ctx = DbContextFactory.Create(dbName);
        emailSvc ??= Mock.Of<IEmailService>();
        logger ??= Mock.Of<ILogger<MeczService>>();
        return (new MeczService(ctx, emailSvc, logger), ctx);
    }

    // ── GetAllMecze ───────────────────────────────────────────────────────────

    [Fact]
    public void GetAllMecze_ReturnsAllMecze()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(GetAllMecze_ReturnsAllMecze));
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.SaveChanges();
        ctx.Mecze.AddRange(
            new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B" },
            new Mecz { Id = 2, NumerMeczu = "M2", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "C", Gosc = "D" }
        );
        ctx.SaveChanges();

        Assert.Equal(2, svc.GetAllMecze().Count);
    }

    // ── GetMeczById ───────────────────────────────────────────────────────────

    [Fact]
    public void GetMeczById_ExistingId_ReturnsMecz()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(GetMeczById_ExistingId_ReturnsMecz));
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.SaveChanges();
        ctx.Mecze.Add(new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B" });
        ctx.SaveChanges();

        var result = svc.GetMeczById(1);
        Assert.NotNull(result);
        Assert.Equal("M1", result.NumerMeczu);
    }

    [Fact]
    public void GetMeczById_NonExistingId_ReturnsNull()
    {
        var svc = CreateService(nameof(GetMeczById_NonExistingId_ReturnsNull));
        Assert.Null(svc.GetMeczById(999));
    }

    // ── DeleteMecz ────────────────────────────────────────────────────────────

    [Fact]
    public void DeleteMecz_ExistingId_RemovesMecz()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(DeleteMecz_ExistingId_RemovesMecz));
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.SaveChanges();
        ctx.Mecze.Add(new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B" });
        ctx.SaveChanges();

        svc.DeleteMecz(1);

        Assert.Empty(ctx.Mecze);
    }

    [Fact]
    public void DeleteMecz_NonExistingId_DoesNotThrow()
    {
        var svc = CreateService(nameof(DeleteMecz_NonExistingId_DoesNotThrow));
        var ex = Record.Exception(() => svc.DeleteMecz(999));
        Assert.Null(ex);
    }

    // ── AddMecz ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddMecz_PastDate_DoesNotSendEmail()
    {
        var emailMock = new Mock<IEmailService>();
        var (svc, ctx) = CreateServiceWithCtx(nameof(AddMecz_PastDate_DoesNotSendEmail),
            emailMock.Object);
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.Sedziowie.Add(MakeSedzia(1, "test@test.pl"));
        ctx.SaveChanges();

        await svc.AddMecz("M1", DateTime.Now.AddDays(-1), 1, "A", "B",
            sedziaIId: 1, null, null, null, null, null);

        emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        Assert.Single(ctx.Mecze);
    }

    [Fact]
    public async Task AddMecz_FutureDate_SendsEmail()
    {
        var emailMock = new Mock<IEmailService>();
        var (svc, ctx) = CreateServiceWithCtx(nameof(AddMecz_FutureDate_SendsEmail),
            emailMock.Object);
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.Sedziowie.Add(MakeSedzia(1, "sedzia@test.pl"));
        ctx.SaveChanges();

        await svc.AddMecz("M1", DateTime.Now.AddDays(7), 1, "A", "B",
            sedziaIId: 1, null, null, null, null, null);

        emailMock.Verify(e => e.SendEmailAsync("sedzia@test.pl", It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task AddMecz_NoSedzia_PersistsMeczWithoutEmail()
    {
        var emailMock = new Mock<IEmailService>();
        var (svc, ctx) = CreateServiceWithCtx(nameof(AddMecz_NoSedzia_PersistsMeczWithoutEmail),
            emailMock.Object);
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.SaveChanges();

        await svc.AddMecz("M1", DateTime.Now.AddDays(1), 1, "A", "B",
            null, null, null, null, null, null);

        emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        Assert.Single(ctx.Mecze);
    }

    [Fact]
    public async Task AddMecz_SedziaWithEmptyEmail_DoesNotSendEmail()
    {
        var emailMock = new Mock<IEmailService>();
        var (svc, ctx) = CreateServiceWithCtx(nameof(AddMecz_SedziaWithEmptyEmail_DoesNotSendEmail),
            emailMock.Object);
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.Sedziowie.Add(MakeSedzia(1, ""));  // empty email
        ctx.SaveChanges();

        await svc.AddMecz("M1", DateTime.Now.AddDays(7), 1, "A", "B",
            sedziaIId: 1, null, null, null, null, null);

        emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // ── SetUdostepnijGodzineAsync ─────────────────────────────────────────────

    [Fact]
    public async Task SetUdostepnijGodzineAsync_ExistingMecz_SetsValue()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(SetUdostepnijGodzineAsync_ExistingMecz_SetsValue));
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.SaveChanges();
        ctx.Mecze.Add(new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", UdostepnijGodzine = false });
        ctx.SaveChanges();

        await svc.SetUdostepnijGodzineAsync(1, true);

        Assert.True(ctx.Mecze.Find(1)!.UdostepnijGodzine);
    }

    [Fact]
    public async Task SetUdostepnijGodzineAsync_NonExistingMecz_ThrowsArgumentException()
    {
        var svc = CreateService(nameof(SetUdostepnijGodzineAsync_NonExistingMecz_ThrowsArgumentException));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.SetUdostepnijGodzineAsync(999, true));
    }

    // ── GetSedziowieByDateWithStatus ──────────────────────────────────────────

    [Fact]
    public void GetSedziowieByDateWithStatus_ReturnsCorrectStatuses()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(GetSedziowieByDateWithStatus_ReturnsCorrectStatuses));
        ctx.Sedziowie.AddRange(
            new Sedzia { Id = 1, Imie = "A", Nazwisko = "Dostepny", Klasa = "K1", CzyUrlop = false, Email = "a@test.pl", Telefon = "000", Miasto = "M" },
            new Sedzia { Id = 2, Imie = "B", Nazwisko = "Urlop", Klasa = "K1", CzyUrlop = true, Email = "b@test.pl", Telefon = "000", Miasto = "M" },
            new Sedzia { Id = 3, Imie = "C", Nazwisko = "Niedysp", Klasa = "K1", CzyUrlop = false, Email = "c@test.pl", Telefon = "000", Miasto = "M" }
        );
        var matchDay = new DateTime(2025, 5, 20, 10, 0, 0);
        ctx.Niedyspozycje.Add(new Niedyspozycja
        {
            Id = 1,
            SedziaId = 3,
            Poczatek = matchDay.AddDays(-1),
            Koniec = matchDay.AddDays(1),
            DataDodania = DateTime.UtcNow
        });
        ctx.SaveChanges();

        var result = svc.GetSedziowieByDateWithStatus(matchDay).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("DOSTEPNY",    result.Single(x => x.Id == 1).Status);
        Assert.Equal("URLOP",       result.Single(x => x.Id == 2).Status);
        Assert.Equal("NIEDOSTEPNY", result.Single(x => x.Id == 3).Status);
    }

    [Fact]
    public void GetSedziowieByDateWithStatus_MaMeczTegoDnia_True()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(GetSedziowieByDateWithStatus_MaMeczTegoDnia_True));
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.Sedziowie.Add(new Sedzia { Id = 1, Imie = "A", Nazwisko = "Kowalski", Klasa = "K1", CzyUrlop = false, Email = "a@test.pl", Telefon = "000", Miasto = "M" });
        ctx.SaveChanges();

        var matchDay = new DateTime(2025, 5, 20, 10, 0, 0);
        ctx.Mecze.Add(new Mecz { Id = 1, NumerMeczu = "M1", Data = matchDay, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1 });
        ctx.SaveChanges();

        var result = svc.GetSedziowieByDateWithStatus(matchDay).ToList();

        Assert.True(result.Single(x => x.Id == 1).MaMeczTegoDnia);
    }

    [Fact]
    public void GetSedziowieByDateWithStatus_OrderedDostepniFirst()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(GetSedziowieByDateWithStatus_OrderedDostepniFirst));
        ctx.Sedziowie.AddRange(
            new Sedzia { Id = 1, Imie = "A", Nazwisko = "Urlop", Klasa = "K1", CzyUrlop = true, Email = "a@test.pl", Telefon = "000", Miasto = "M" },
            new Sedzia { Id = 2, Imie = "B", Nazwisko = "Dostepny", Klasa = "K1", CzyUrlop = false, Email = "b@test.pl", Telefon = "000", Miasto = "M" }
        );
        ctx.SaveChanges();

        var result = svc.GetSedziowieByDateWithStatus(DateTime.Today).ToList();

        Assert.Equal("DOSTEPNY", result[0].Status);
        Assert.Equal("URLOP", result[1].Status);
    }

    // ── UpdateMecz ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMecz_NonExistingId_ThrowsArgumentException()
    {
        var svc = CreateService(nameof(UpdateMecz_NonExistingId_ThrowsArgumentException));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.UpdateMecz(new Mecz { Id = 999, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B" }));
    }

    [Fact]
    public async Task UpdateMecz_FutureMecz_SendsEmailToNewSedzia()
    {
        var emailMock = new Mock<IEmailService>();
        var (svc, ctx) = CreateServiceWithCtx(nameof(UpdateMecz_FutureMecz_SendsEmailToNewSedzia),
            emailMock.Object);
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.Sedziowie.Add(MakeSedzia(1, "new@test.pl"));
        ctx.SaveChanges();

        var futureDate = DateTime.Now.AddDays(10);
        ctx.Mecze.Add(new Mecz { Id = 1, NumerMeczu = "M1", Data = futureDate, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B" });
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var updated = new Mecz { Id = 1, NumerMeczu = "M1", Data = futureDate, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1 };
        await svc.UpdateMecz(updated);

        emailMock.Verify(e => e.SendEmailAsync("new@test.pl", It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task UpdateMecz_RemovedSedzia_SendsRemovalEmail()
    {
        var emailMock = new Mock<IEmailService>();
        var (svc, ctx) = CreateServiceWithCtx(nameof(UpdateMecz_RemovedSedzia_SendsRemovalEmail),
            emailMock.Object);
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.Sedziowie.AddRange(
            MakeSedzia(1, "old@test.pl"),
            MakeSedzia(2, "new@test.pl"));
        ctx.SaveChanges();

        var futureDate = DateTime.Now.AddDays(10);
        ctx.Mecze.Add(new Mecz { Id = 1, NumerMeczu = "M1", Data = futureDate, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1 });
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        // Replace sedzia 1 with sedzia 2
        var updated = new Mecz { Id = 1, NumerMeczu = "M1", Data = futureDate, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 2 };
        await svc.UpdateMecz(updated);

        // old@test.pl should get removal email, new@test.pl should get assignment email
        emailMock.Verify(e => e.SendEmailAsync("old@test.pl", It.Is<string>(s => s.Contains("Zmiana")), It.IsAny<string>(), true), Times.Once);
        emailMock.Verify(e => e.SendEmailAsync("new@test.pl", It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task UpdateMecz_PastDate_NoEmailsSent()
    {
        var emailMock = new Mock<IEmailService>();
        var (svc, ctx) = CreateServiceWithCtx(nameof(UpdateMecz_PastDate_NoEmailsSent),
            emailMock.Object);
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.Sedziowie.Add(MakeSedzia(1, "test@test.pl"));
        ctx.SaveChanges();

        var pastDate = DateTime.Now.AddDays(-5);
        ctx.Mecze.Add(new Mecz { Id = 1, NumerMeczu = "M1", Data = pastDate, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B" });
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        await svc.UpdateMecz(new Mecz { Id = 1, NumerMeczu = "M1", Data = pastDate, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", SedziaIId = 1 });

        emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // ── GetMeczeForUserId ─────────────────────────────────────────────────────

    [Fact]
    public void GetMeczeForUserId_ReturnsMeczeForClub()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(GetMeczeForUserId_ReturnsMeczeForClub));
        ctx.Rozgrywki.Add(MakeRozgrywki(1));
        ctx.SaveChanges();
        ctx.Mecze.AddRange(
            new Mecz { Id = 1, NumerMeczu = "M1", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "A", Gosc = "B", GospodarzKlubId = "user1" },
            new Mecz { Id = 2, NumerMeczu = "M2", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "C", Gosc = "D", GoscKlubId = "user1" },
            new Mecz { Id = 3, NumerMeczu = "M3", Data = DateTime.Today, RozgrywkiId = 1, Gospodarz = "E", Gosc = "F" }
        );
        ctx.SaveChanges();

        var result = svc.GetMeczeForUserId("user1");

        Assert.Equal(2, result.Count);
    }

    // ── GetRozgrywki ──────────────────────────────────────────────────────────

    [Fact]
    public void GetRozgrywki_ReturnsAllRozgrywki()
    {
        var (svc, ctx) = CreateServiceWithCtx(nameof(GetRozgrywki_ReturnsAllRozgrywki));
        ctx.Rozgrywki.AddRange(MakeRozgrywki(1), MakeRozgrywki(2, "II Liga Kobiet"));
        ctx.SaveChanges();

        Assert.Equal(2, svc.GetRozgrywki().Count());
    }
}
