using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using BacktestGui.ViewModels.Shared;
using ReactiveUI;

namespace BacktestGui.Views.Shared;

public partial class MultiFieldSelectorView : UserControl
{
    private MultiFieldSelectorViewModel? MultiFieldSelectorViewModel
        => (MultiFieldSelectorViewModel?)DataContext;

    private WrapPanel RootWrapPanel => this.Get<WrapPanel>("RootWrapPanel1");

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (MultiFieldSelectorViewModel is null)
            return;

        LoadSelectors(MultiFieldSelectorViewModel.SelectableFields);
    }

    public MultiFieldSelectorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadSelectors(List<string> selectableFields)
    {
        RootWrapPanel.Children.Clear();

        foreach (var selectableField in selectableFields)
        {
            var textLabel = new TextBlock
            {
                Text = selectableField,
                Margin = new(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var button = new ToggleButton()
            {
                Margin = new(5),
                Content = textLabel
            };
            button.GetObservable(ToggleButton.IsCheckedProperty)
                .Skip(1)
                .Subscribe(x =>
                {
                    if (x is null)
                        return;
                    MultiFieldSelectorViewModel?.HandleSelection(
                        new(selectableField, x.Value));
                });

            if (MultiFieldSelectorViewModel?.SingleSelect is true)
            {
                var isFieldSelectedObservable = MultiFieldSelectorViewModel.WhenAnyValue(
                        x => x.SelectedFieldChangedEvent)
                    .Select(x => new BindingValue<bool?>(x?.Field == selectableField));

                button.Bind(
                    ToggleButton.IsCheckedProperty,
                    isFieldSelectedObservable);
            }

            if (MultiFieldSelectorViewModel?.InitSelectAll is true)
                button.IsChecked = true;
            
            RootWrapPanel.Children.Add(button);
        }
    }
}