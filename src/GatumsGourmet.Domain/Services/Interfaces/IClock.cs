namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}
