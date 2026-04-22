namespace Sedziowanie.ViewModels
{
    public class NiedyspozycjaCreateViewModel
    {
        public bool IsAllDay { get; set; } = true;

        public DateTime? StartDate { get; set; }   // YYYY-MM-DD
        public DateTime? EndDate { get; set; }     // YYYY-MM-DD

        public DateTime? StartDateTime { get; set; }  // YYYY-MM-DDTHH:mm
        public DateTime? EndDateTime { get; set; }    // YYYY-MM-DDTHH:mm
    }
}
