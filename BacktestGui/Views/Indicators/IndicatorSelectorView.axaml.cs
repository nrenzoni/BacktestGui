using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Console = System.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using BacktestGui.Configurations;
using BacktestGui.Utils;
using BacktestGui.ViewModels.Indicators;
using CustomShared;
using ReactiveUI;

namespace BacktestGui.Views.Indicators;

delegate void SingleSelectionHandler(
    string parameter,
    decimal selectableValue,
    Button toggleButton);

delegate void UpdateOnSelection(
    string indicator,
    string parameterName,
    decimal paramValue,
    bool isSelected);

public partial class IndicatorSelectorView : UserControl
{
    private IndicatorSelectorViewModel IndicatorSelectorViewModel =>
        (IndicatorSelectorViewModel)DataContext;

    private Grid AllIndicatorSelectionGrid => this.Get<Grid>("AllIndicatorSelectionGrid1");

    private Grid AllIndicatorLabelsGrid => this.Get<Grid>("AllIndicatorLabelsGrid1");

    private Grid IndicatorWrappersSelectionGrid => this.Get<Grid>("IndicatorWrappersSelectionGrid1");

    private Grid IndicatorWrappersLabelsGrid => this.Get<Grid>("IndicatorWrappersLabelsGrid1");

    readonly List<IDisposable> _toDisposeOnNextIndicatorSelection = new();


    protected override void OnDataContextChanged(EventArgs e)
    {
        var globals = Bootstrapper.GetService<Globals>();

        if (DataContext is null)
            return;

        IndicatorSelectorViewModel
            .WhenAnyValue(x => x.SelectedIndicatorWithSelectableParamValues)
            .Where(x => x.NoNullMembers())
            .Throttle(globals.ThrottleTime)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => CreateAllDynamicSelectors(
                x.Item1,
                x.Item2,
                AllIndicatorLabelsGrid,
                AllIndicatorSelectionGrid,
                SingleSelectionIndicatorHandler,
                IndicatorSelectorViewModel.UpdateSelectionDynamicIndicators));

        IndicatorSelectorViewModel
            .WhenAnyValue(x => x.SelectedIndicatorWrappersWithSelectableParamValues)
            .Where(x => x.NoNullMembers())
            .Throttle(globals.ThrottleTime)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => CreateAllDynamicSelectors(
                x.Item1,
                x.Item2,
                IndicatorWrappersLabelsGrid,
                IndicatorWrappersSelectionGrid,
                SingleSelectionIndicatorWrapperHandler,
                IndicatorSelectorViewModel.UpdateSelectionDynamicIndicatorWrappers));
    }

    public IndicatorSelectorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void CreateAllDynamicSelectors(
        string indicator,
        Dictionary<string, List<decimal>?> allParamRanges,
        Grid labelsGrid,
        Grid selectionGrid,
        SingleSelectionHandler singleSelectionHandler,
        UpdateOnSelection updateOnSelection)
    {
        labelsGrid.Children.Clear();
        selectionGrid.Children.Clear();

        // runs twice on init
        // for (var i = 0; i < _toDisposeOnNextIndicatorSelection.Count; ++i)
        // {
        //     _toDisposeOnNextIndicatorSelection[0].Dispose();
        //     _toDisposeOnNextIndicatorSelection.RemoveAt(0);
        // }

        var nParams = allParamRanges.Keys.Count;
        selectionGrid.ColumnDefinitions.Clear();
        labelsGrid.ColumnDefinitions.Clear();

        var columnWidth = 100;
        foreach (var _ in Enumerable.Range(0, nParams))
        {
            // can not have same ColumnDefinition in same grid more than once
            var columnDefinition = new ColumnDefinition
            {
                Width = GridLength.Auto
            };

            labelsGrid.ColumnDefinitions.Add(columnDefinition);
            selectionGrid.ColumnDefinitions.Add(columnDefinition);
        }

        foreach (var ((paramName, paramValues), column) in allParamRanges.WithIndex())
        {
            var paramColumnLabelText = new TextBlock
            {
                Text = paramName,
                Width = columnWidth,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            labelsGrid.Children.Add(paramColumnLabelText);
            Grid.SetColumn(paramColumnLabelText, column);

            if (paramValues is null)
                continue;

            var selectorsForParam =
                CreateCombinationSingleParamSelectors(
                    indicator,
                    paramName,
                    paramValues,
                    singleSelectionHandler,
                    updateOnSelection
                );

            var columnPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = columnWidth
            };
            columnPanel.Children.AddRange(selectorsForParam);

            var scrollViewerPerColumn = new ScrollViewer
            {
                Content = columnPanel
            };

            selectionGrid.Children.Add(scrollViewerPerColumn);
            Grid.SetColumn(scrollViewerPerColumn, column);
        }
    }

    private List<Control> CreateCombinationSingleParamSelectors(
        string indicator,
        string parameter,
        IList<decimal> selectableValues,
        SingleSelectionHandler singleSelectionHandler,
        UpdateOnSelection updateOnSelection)
    {
        var marginThickness = new Thickness(2.5);

        var panelsPerColumn = new List<Control>();

        foreach (var selectableValue in selectableValues)
        {
            var paramLabelTextBlock = new TextBlock
            {
                Text = $"{selectableValue}",
                // Margin = marginThickness,
                // VerticalAlignment = VerticalAlignment.Center
            };

            var toggleButton = new ToggleButton
            {
                Margin = marginThickness,
                VerticalAlignment = VerticalAlignment.Center,
                Content = paramLabelTextBlock,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            if (IndicatorSelectorViewModel.SingleSelectedIndicatorMode)
                singleSelectionHandler(
                parameter,
                selectableValue,
                toggleButton);

            var disposableCheckboxSub = toggleButton
                .GetObservable(ToggleButton.IsCheckedProperty)
                .Subscribe(selected =>
                {
                    if (!selected.HasValue)
                        return;

                    updateOnSelection(
                        indicator,
                        parameter,
                        selectableValue,
                        selected.Value);
                });

            _toDisposeOnNextIndicatorSelection.Add(disposableCheckboxSub);

            var layoutTransformControl = new LayoutTransformControl
            {
                Child = toggleButton,
                LayoutTransform = new ScaleTransform(0.7, 0.7)
            };

            panelsPerColumn.Add(layoutTransformControl);
        }

        return panelsPerColumn;
    }

    private void SingleSelectionIndicatorHandler(
        string parameter,
        decimal selectableValue,
        Button toggleButton)
    {

        var isParamValueSelectedObservable =
            IndicatorSelectorViewModel.SingleParamsPerSelectedDynamicIndicatorObservable
                .Select(x =>
                {
                    var success = x.TryGetValue(parameter, out var outVar);
                    return new BindingValue<bool?>(success && outVar == selectableValue);
                });

        var disposableBinding = toggleButton.Bind(
            ToggleButton.IsCheckedProperty,
            isParamValueSelectedObservable);

        _toDisposeOnNextIndicatorSelection.Add(disposableBinding);
    }

    private void SingleSelectionIndicatorWrapperHandler(
        string parameter,
        decimal selectableValue,
        Button toggleButton)
    {
        var isParamValueSelectedObservable =
            IndicatorSelectorViewModel.SingleParamsPerSelectedIndicatorWrapperObservable
                .Select(x =>
                {
                    var success = x.TryGetValue(parameter, out var outVar);
                    return new BindingValue<bool?>(success && outVar == selectableValue);
                });

        var disposableBinding = toggleButton.Bind(
            ToggleButton.IsCheckedProperty,
            isParamValueSelectedObservable);

        _toDisposeOnNextIndicatorSelection.Add(disposableBinding);
    }
}