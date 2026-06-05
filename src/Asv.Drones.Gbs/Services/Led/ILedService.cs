namespace Asv.Drones.Gbs.Led;

public interface ILedService
{
    IDisposable LedAnimation(string record, TimeSpan? tickDuration = null);
}
