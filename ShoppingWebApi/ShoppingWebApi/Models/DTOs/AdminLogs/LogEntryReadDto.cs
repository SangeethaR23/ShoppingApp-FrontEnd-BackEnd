namespace ShoppingWebApi.Models.DTOs.AdminLogs
{
    public class LogEntryReadDto
    {
        public int Id { get; set; }
        public string Level { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Exception { get; set; }
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public int? EventId { get; set; }
        public string? CorrelationId { get; set; }
        public string? RequestPath { get; set; }
        public DateTime CreatedUtc { get; set; }

    }
}
