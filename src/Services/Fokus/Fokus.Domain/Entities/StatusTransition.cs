namespace Fokus.Domain.Entities;

public class StatusTransition
{
    public int Id { get; set; }
    public required string TicketKey { get; set; }
    public required string FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public DateTime Timestamp { get; set; }
    public string? AuthorId { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
