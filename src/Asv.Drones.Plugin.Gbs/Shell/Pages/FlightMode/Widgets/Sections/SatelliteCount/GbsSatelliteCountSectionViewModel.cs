using Asv.Avalonia;
using Asv.Common;
using Asv.Mavlink;
using Avalonia.Controls;
using R3;

namespace Asv.Drones.Plugin.Gbs;

#pragma warning disable SA1313
public sealed record GbsSatelliteCountSectionArgs(IAsvGbsExClient Gbs);
#pragma warning restore SA1313

public sealed class GbsSatelliteCountSectionViewModel : ViewModel, IGbsSatelliteCountSection
{
    public const string SectionId = "gbs-satellite-count-section";

    public GbsSatelliteCountSectionViewModel()
        : base(SectionId)
    {
        DesignTime.ThrowIfNotDesignMode();

        BeidouSats = new GridLength(4, GridUnitType.Star);
        GalSats = new GridLength(7, GridUnitType.Star);
        GlonassSats = new GridLength(5, GridUnitType.Star);
        GpsSats = new GridLength(9, GridUnitType.Star);
        ImesSats = new GridLength(2, GridUnitType.Star);
        QzssSats = new GridLength(3, GridUnitType.Star);
        SbasSats = new GridLength(1, GridUnitType.Star);
    }

    public GbsSatelliteCountSectionViewModel(GbsSatelliteCountSectionArgs args)
        : base(SectionId)
    {
        var gbs = args.Gbs;
        gbs.BeidouSatellites.ObserveOnUIThreadDispatcher()
            .Subscribe(count => BeidouSats = new GridLength(count, GridUnitType.Star))
            .DisposeItWith(Disposable);
        gbs.GalSatellites.ObserveOnUIThreadDispatcher()
            .Subscribe(count => GalSats = new GridLength(count, GridUnitType.Star))
            .DisposeItWith(Disposable);
        gbs.GlonassSatellites.ObserveOnUIThreadDispatcher()
            .Subscribe(count => GlonassSats = new GridLength(count, GridUnitType.Star))
            .DisposeItWith(Disposable);
        gbs.GpsSatellites.ObserveOnUIThreadDispatcher()
            .Subscribe(count => GpsSats = new GridLength(count, GridUnitType.Star))
            .DisposeItWith(Disposable);
        gbs.ImesSatellites.ObserveOnUIThreadDispatcher()
            .Subscribe(count => ImesSats = new GridLength(count, GridUnitType.Star))
            .DisposeItWith(Disposable);
        gbs.QzssSatellites.ObserveOnUIThreadDispatcher()
            .Subscribe(count => QzssSats = new GridLength(count, GridUnitType.Star))
            .DisposeItWith(Disposable);
        gbs.SbasSatellites.ObserveOnUIThreadDispatcher()
            .Subscribe(count => SbasSats = new GridLength(count, GridUnitType.Star))
            .DisposeItWith(Disposable);
    }

    public int Order => 20;

    public GridLength BeidouSats
    {
        get;
        set => SetField(ref field, value);
    }

    public GridLength GalSats
    {
        get;
        set => SetField(ref field, value);
    }

    public GridLength GlonassSats
    {
        get;
        set => SetField(ref field, value);
    }

    public GridLength GpsSats
    {
        get;
        set => SetField(ref field, value);
    }

    public GridLength ImesSats
    {
        get;
        set => SetField(ref field, value);
    }

    public GridLength QzssSats
    {
        get;
        set => SetField(ref field, value);
    }

    public GridLength SbasSats
    {
        get;
        set => SetField(ref field, value);
    }
}
