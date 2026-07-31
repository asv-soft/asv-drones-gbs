using Asv.Avalonia;
using Asv.Common;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class SetCoordsNameDialogViewModel : GbsDialogViewModelBase
{
    public const string DialogId = "set-coords-name";

    private readonly PropertyTextBoxViewModel _name;
    private readonly ReactiveProperty<string?> _nameValue;
    private readonly SerialDisposable _sub;

    public SetCoordsNameDialogViewModel()
        : base(DialogId)
    {
        _sub = new SerialDisposable().DisposeItWith(Disposable);

        FieldsEditor = new PropertyEditorViewModel("fields")
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);

        _nameValue = new ReactiveProperty<string?>().DisposeItWith(Disposable);
        _name = new PropertyTextBoxReactive("name", _nameValue)
        {
            Header = RS.SetCoordsNameDialogView_Name_Header,
            ShortHeader = RS.SetCoordsNameDialogView_Name_ShortHeader,
            Icon = MaterialIconKind.Rename,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);
        _name
            .Text.EnableValidationRoutable(
                value =>
                    string.IsNullOrWhiteSpace(value)
                        ? ValidationResult.FailAsNullOrWhiteSpace
                        : ValidationResult.Success,
                this,
                true
            )
            .DisposeItWith(Disposable);

        FieldsEditor.ItemsSource.Add(_name);
    }

    public override void ApplyDialog(ContentDialog dialog)
    {
        base.ApplyDialog(dialog);

        _sub.Disposable = IsValid
            .ObserveOnUIThreadDispatcher()
            .Subscribe(x => dialog.IsPrimaryButtonEnabled = x);
    }

    public string Name => _nameValue.CurrentValue ?? string.Empty;

    public PropertyEditorViewModel FieldsEditor { get; }

    public override IEnumerable<IViewModel> GetChildren()
    {
        yield return FieldsEditor;
    }
}
