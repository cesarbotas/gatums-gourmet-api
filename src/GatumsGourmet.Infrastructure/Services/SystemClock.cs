using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
