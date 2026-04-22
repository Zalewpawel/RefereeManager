using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sedziowanie.Models;

namespace Sedziowanie.Data
{
    public class DBObsadyContext : IdentityDbContext<ApplicationUser>
    {
        public DBObsadyContext(DbContextOptions options) : base(options) { }

        public DbSet<Mecz> Mecze { get; set; }
        public DbSet<Sedzia> Sedziowie { get; set; }
        public DbSet<Niedyspozycja> Niedyspozycje { get; set; }
        public DbSet<Rozgrywki> Rozgrywki { get; set; }
        public DbSet<Komisja> Komisje { get; set; }
        public DbSet<KomisjaCzlonek> KomisjaCzlonkowie { get; set; }
        public DbSet<SukcesWydzialu> SukcesyWydzialu { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityUserLogin<string>>()
                .HasKey(l => new { l.LoginProvider, l.ProviderKey });

            modelBuilder.Entity<IdentityUserRole<string>>()
                .HasKey(r => new { r.UserId, r.RoleId });

            modelBuilder.Entity<IdentityUserToken<string>>()
                .HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.SedziaI)
                .WithMany(s => s.MeczeJakoSedzia1)
                .HasForeignKey(m => m.SedziaIId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.SedziaII)
                .WithMany(s => s.MeczeJakoSedzia2)
                .HasForeignKey(m => m.SedziaIIId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.SedziaSekretarz)
                .WithMany(s => s.MeczeJakoSedziaSekretarz)
                .HasForeignKey(m => m.SedziaSekretarzId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.SedziaLiniowyI)
                .WithMany(s => s.MeczeJakoSedziaLiniowyI)
                .HasForeignKey(m => m.SedziaLiniowyIId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.SedziaLiniowyII)
                .WithMany(s => s.MeczeJakoSedziaLiniowyII)
                .HasForeignKey(m => m.SedziaLiniowyIIId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.SedziaGlowny)
                .WithMany(s => s.MeczeJakoSedziaGlowny)
                .HasForeignKey(m => m.SedziaGlownyId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Niedyspozycja>()
               .HasOne(n => n.Sedzia)
               .WithMany(s => s.Niedyspozycje)
               .HasForeignKey(n => n.SedziaId);

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.Rozgrywki)
                .WithMany(r => r.Mecze)
                .HasForeignKey(m => m.RozgrywkiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
               .HasOne(u => u.Sedzia)
               .WithOne()
               .HasForeignKey<ApplicationUser>(u => u.SedziaId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KomisjaCzlonek>()
                .HasOne(kc => kc.Komisja)
                .WithMany(k => k.Czlonkowie)
                .HasForeignKey(kc => kc.KomisjaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KomisjaCzlonek>()
                .HasOne(kc => kc.Sedzia)
                .WithMany()
                .HasForeignKey(kc => kc.SedziaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sedzia>()
                .Property(s => s.CzyUrlop)
                .HasDefaultValue(false);

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.GospodarzKlub)
                .WithMany()
                .HasForeignKey(m => m.GospodarzKlubId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mecz>()
                .HasOne(m => m.GoscKlub)
                .WithMany()
                .HasForeignKey(m => m.GoscKlubId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
