using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Selection;
using ReactiveUI;

namespace BacktestGui.ViewModels.Helpers;

public static class SelectionModelHelpers
{
    public static void SelectionChangedHandler2<TEvent, TSender>
    (
        this TSender reactiveObject,
        SelectionModelSelectionChangedEventArgs<TEvent> e,
        List<TEvent> selectionList,
        string propertyName)
        where TSender : IReactiveObject
    {
        if (!e.SelectedItems.Any() && !e.DeselectedItems.Any())
            return;

        reactiveObject.RaisePropertyChanging(propertyName);

        if (e.DeselectedItems.Any())
        {
            foreach (var eDeselectedItem in e.DeselectedItems)
            {
                selectionList.Remove(eDeselectedItem);
            }
        }

        if (e.SelectedItems.Any())
            selectionList.AddRange(e.SelectedItems);

        reactiveObject.RaisePropertyChanged(propertyName);
    }
}