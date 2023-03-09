using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels.Shared;

public class MultiFieldSelectorViewModelFactory
{
    public MultiFieldSelectorViewModel Create(
        bool singleSelect,
        bool initSelectAll,
        List<string> selectableFields)
    {
        return new MultiFieldSelectorViewModel(
            singleSelect,
            initSelectAll,
            selectableFields);
    }
}

public class MultiFieldSelectorViewModel : ViewModelBase
{
    public MultiFieldSelectorViewModel(
        bool singleSelect,
        bool initSelectAll,
        List<string> selectableFields)
    {
        SingleSelect = singleSelect;
        InitSelectAll = initSelectAll;
        SelectableFields = selectableFields;
    }

    public bool SingleSelect { get; }

    public bool InitSelectAll { get; }

    public List<string> SelectableFields { get; }

    public HashSet<string> SelectedFields { get; } = new();

    [Reactive] public SelectedFieldChangedEvent? SelectedFieldChangedEvent { get; set; }

    public void HandleSelection(SelectedFieldChangedEvent selectedFieldChangedEvent)
    {
        if (!selectedFieldChangedEvent.IsSelected
            && !SelectedFields.Contains(selectedFieldChangedEvent.Field))
            return;

        this.RaisePropertyChanging(nameof(SelectedFields));

        if (selectedFieldChangedEvent.IsSelected)
        {
            SelectedFields.Add(selectedFieldChangedEvent.Field);
            SelectedFields.Add(selectedFieldChangedEvent.Field);
            SelectedFieldChangedEvent = selectedFieldChangedEvent;
        }
        else
            SelectedFields.Remove(selectedFieldChangedEvent.Field);

        this.RaisePropertyChanged(nameof(SelectedFields));
    }
}

public record SelectedFieldChangedEvent(string Field, bool IsSelected);