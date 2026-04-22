namespace Sedziowanie.ViewModels
{
    public class SedziaStatsDto
    {
        public int SedziaId { get; set; }
        public string Imie { get; set; }
        public string Nazwisko { get; set; }

        public int MeczeCount { get; set; }

        public int SiCount { get; set; }
        public int SiiCount { get; set; }
        public int SsCount { get; set; }
        public int SgCount { get; set; }
        public int LCount { get; set; }

        public int SzczebelCount { get; set; }
        public int Liga3Count { get; set; }
        public int JCount { get; set; }
        public int KCount { get; set; }
        public int MlCount { get; set; }
        public int MmpCount { get; set; }
        public int MiniCount { get; set; }
        public int TowCount { get; set; }
        public int PlazaCount { get; set; }
        public int PalmCount { get; set; }

        public string DuetSedzia { get; set; } = "-";
        public int DuetCount { get; set; }
    }
}
