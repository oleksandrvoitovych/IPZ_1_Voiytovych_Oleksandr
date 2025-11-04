namespace IPZ_Tickets_Client.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Date { get; set; }    // для простоти - string
        public string Time { get; set; }
        public int Seat { get; set; }
        public decimal Price { get; set; }
    }
}
