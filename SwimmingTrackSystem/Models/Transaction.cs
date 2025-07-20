namespace SwimmingTrackSystem.Models;

public partial class Transaction
{
    public int Id { get; set; }

    public DateTime CreateDate { get; set; }

    public string TypeTransaction { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? ErrorMessage { get; set; }
    
    public string? ProductName { get; set; }
    
    public DateTime? TrackDate { get; set; }

    public int? Status { get; set; }

    public DateTime? ExpireDate { get; set; }
    
    public DateTime? DeletedAt { get; set; }

    public string StatusPay => ErrorMessage is null ? "Оплачен" : "Не оплачен";
    
    public string StatusTrack => Status switch
    {
        1 => "Зашел",
        2 => "Ушел",
        _ => string.Empty
    };
}
