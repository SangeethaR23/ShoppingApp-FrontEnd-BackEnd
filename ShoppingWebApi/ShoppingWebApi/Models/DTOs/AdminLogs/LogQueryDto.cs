namespace ShoppingWebApi.Models.DTOs.AdminLogs
{
    public class LogQueryDto
    {
        public string? Level { get; set; }            // Info, Warn, Error
        public string? Search { get; set; }           // keyword search
        public DateTime? From { get; set; }           // start date
        public DateTime? To { get; set; }             // end date
        public string? Source { get; set; }           // e.g., ProductService, OrderService

        public int Page { get; set; } = 1;            // pagination
        public int Size { get; set; } = 20;
        public string? SortDir { get; set; } = "desc"; // asc or desc
        public string? SortBy { get; set; } = "date"; // date, level,

    }
}
