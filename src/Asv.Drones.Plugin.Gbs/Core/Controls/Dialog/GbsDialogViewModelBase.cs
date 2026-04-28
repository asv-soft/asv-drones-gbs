using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public abstract class GbsDialogViewModelBase(string id) : DialogViewModelBase(BaseGbsDialogId + id)
{
    private const string BaseGbsDialogId = $"{BaseId}.gbs.";
}
