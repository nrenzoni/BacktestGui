using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Selection;
using BacktestGui.Services;
using BacktestGui.ViewModels.Helpers;
using CustomShared;
using IndicatorServiceShared;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using BacktestGui.Configurations;
using DynamicData.Binding;

namespace BacktestGui.ViewModels.Indicators;

public class IndicatorSelectorViewModelFactory
{
    private readonly BackendIndicatorStatService _backendIndicatorStatService;
    private readonly Globals _globals;

    public IndicatorSelectorViewModelFactory(BackendIndicatorStatService backendIndicatorStatService,
        Globals globals)
    {
        _backendIndicatorStatService = backendIndicatorStatService;
        _globals = globals;
    }

    public IndicatorSelectorViewModel Create(bool singleSelectedIndicator)
    {
        return new IndicatorSelectorViewModel(
            _backendIndicatorStatService,
            _globals,
            singleSelectedIndicator);
    }
}

public class IndicatorSelectorViewModel : ViewModelBase
{
    internal IndicatorSelectorViewModel(
        BackendIndicatorStatService backendIndicatorStatService,
        Globals globals,
        bool singleSelectedIndicatorMode)
    {
        SingleSelectedIndicatorMode = singleSelectedIndicatorMode;

        _indicatorWithParamsList = backendIndicatorStatService
            .IndicatorWithParamsList
            .ToProperty(this, nameof(IndicatorWithParams));

        var selectableIndicatorMetaDataConnectableObservable =
            backendIndicatorStatService
                .SelectableIndicatorMetaDataConnectableObservable
                .Publish();

        _indicatorParameterDefinitionMap =
            selectableIndicatorMetaDataConnectableObservable
                .Select(x => x.IndicatorParameterDefinitionMap)
                .ToProperty(this, nameof(IndicatorParameterDefinitionMap));

        _indicatorWrapperParameterDefinitionMap =
            selectableIndicatorMetaDataConnectableObservable
                .Select(x => x.IndicatorWrapperParameterDefinitionMap)
                .ToProperty(this, nameof(IndicatorWrapperParameterDefinitionMap));

        selectableIndicatorMetaDataConnectableObservable.Connect();

        _indicatorsWithSelectableParamValues = this.WhenAnyValue(
                x => x.IndicatorParameterDefinitionMap)
            .Throttle(globals.ThrottleTime)
            .WhereNotNull()
            .Select(x =>
                BuildDynamicIndicatorWithParams(x))
            .ToProperty(this, nameof(IndicatorsWithSelectableParamValues));

        _indicatorWrappersWithSelectableParamValues = this.WhenAnyValue(
                x => x.IndicatorWrapperParameterDefinitionMap)
            .Throttle(globals.ThrottleTime)
            .WhereNotNull()
            .Select(x =>
                BuildDynamicIndicatorWithParams(x))
            .ToProperty(this, nameof(IndicatorWrappersWithSelectableParamValues));

        _selectedDynamicIndicatorsWithFullParams =
            this.WhenPropertyChanged(x => x.SelectedDynamicIndicatorsWithPartialParams)
                .Skip(1)
                .Select(x => FilterIndicatorsByHavingRequiredParams(
                    x.Value,
                    IndicatorParameterDefinitionMap))
                .ToProperty(this, nameof(SelectedDynamicIndicatorsWithFullParams));

        _selectedIndicatorWrappersWithFullParams =
            this.WhenPropertyChanged(x => x.SelectedDynamicIndicatorWrappersWithPartialParams)
                .Skip(1)
                .Select(x => FilterIndicatorsByHavingRequiredParams(
                    x.Value,
                    IndicatorWrapperParameterDefinitionMap))
                .ToProperty(this, nameof(SelectedIndicatorWrappersWithFullParams));

        _filteredSelectedIndicatorsWithParams =
            this
                .WhenAnyValue(
                    x => x.ExistingIndicatorsOnly,
                    x => x.SelectedDynamicIndicatorsWithFullParams,
                    x => x.SelectedIndicatorWrappersWithFullParams)
                .CombineLatest(this.WhenPropertyChanged(x => x.SelectedAlreadySavedIndicatorCombinations),
                    (v1, v2) =>
                        (v1.Item1, v2.Value, v1.Item2, v1.Item3))
                .Skip(1)
                .Throttle(globals.ThrottleTime)
                .Select(x =>
                    CalculateIndicatorsWithParametersDependingOnSwitch(
                        x.Item1,
                        x.Item2,
                        x.Item3,
                        x.Item4))
                .ToProperty(this, nameof(FilteredSelectedIndicatorsWithParams));

        this.WhenPropertyChanged(x => x.FilteredSelectedIndicatorsWithParams)
            .Skip(1)
            .Select(x => x?.Value?.FirstOrDefault())
            .BindTo(this, x => x.FilteredSelectedIndicatorWithParams);

        _selectedIndicatorWithSelectableParamValues
            = this.WhenAnyValue(
                    x => x.SelectedIndicator)
                .WhereNotNull()
                .Select(x => (x, UnrollParametersForIndicator(
                    x,
                    IndicatorParameterDefinitionMap,
                    nameof(IndicatorParameterDefinitionMap))))
                .ToProperty(
                    this,
                    nameof(SelectedIndicatorWithSelectableParamValues));

        _selectedIndicatorWrappersWithSelectableParamValues
            = this.WhenAnyValue(
                    x => x.SelectedIndicatorWrapper)
                .WhereNotNull()
                .Select(x => (x, UnrollParametersForIndicator(
                    x,
                    IndicatorWrapperParameterDefinitionMap,
                    nameof(IndicatorWrapperParameterDefinitionMap))))
                .ToProperty(
                    this,
                    nameof(SelectedIndicatorWrappersWithSelectableParamValues));

        if (SingleSelectedIndicatorMode)
        {
            this.WhenAnyValue(x => x.SelectedIndicatorWithSelectableParamValues)
                .Skip(1)
                .Subscribe(x => _singleParamsPerSelectedDynamicIndicatorCache = new());

            this.WhenAnyValue(x => x.SelectedIndicatorWrappersWithSelectableParamValues)
                .Skip(1)
                .Subscribe(x => _singleParamsPerSelectedDynamicIndicatorWrapperCache =
                    new());

            SingleParamsPerSelectedDynamicIndicatorObservable =
                this.WhenAnyValue(
                        x => x.LastSelectedParamEvent)
                    .Skip(1)
                    .Throttle(globals.ThrottleTime)
                    .Where(x => x?.IsIndicatorWrapper is false)
                    .Select(x
                        => UpdateSingleParamValuesForSelectedDynamicIndicator(
                            x,
                            _singleParamsPerSelectedDynamicIndicatorCache,
                            SelectedDynamicIndicatorsWithPartialParams))
                    .Publish()
                    .RefCount();

            SingleParamsPerSelectedIndicatorWrapperObservable =
                this.WhenAnyValue(
                        x => x.LastSelectedParamEvent)
                    .Skip(1)
                    .Throttle(globals.ThrottleTime)
                    .Where(x => x?.IsIndicatorWrapper is true)
                    .Select(x => UpdateSingleParamValuesForSelectedDynamicIndicator(
                        x,
                        _singleParamsPerSelectedDynamicIndicatorWrapperCache,
                        SelectedDynamicIndicatorWrappersWithPartialParams))
                    .Publish()
                    .RefCount();
        }
        else
            SingleParamsPerSelectedDynamicIndicatorObservable = Observable.Empty<Dictionary<string, decimal>>();

        ExistingIndicatorsSelection = new SelectionModel<IndicatorWithParams>
        {
            SingleSelect = false
        };
        ExistingIndicatorsSelection.SelectionChanged += ExistingIndicatorsSelectionChanged;
    }

    private Dictionary<string, decimal> _singleParamsPerSelectedDynamicIndicatorCache = new();
    private Dictionary<string, decimal> _singleParamsPerSelectedDynamicIndicatorWrapperCache = new();

    // only update when single param mode set
    public IObservable<Dictionary<string, decimal>> SingleParamsPerSelectedDynamicIndicatorObservable { get; }

    public IObservable<Dictionary<string, decimal>> SingleParamsPerSelectedIndicatorWrapperObservable { get; }

    private Dictionary<string, decimal> UpdateSingleParamValuesForSelectedDynamicIndicator(
        ChangedParamEvent changedParamEvent,
        Dictionary<string, decimal> singleParamsPerSelectedDynamicIndicatorCache,
        IDictionary<string, DefaultableDictionary<string, HashSet<decimal>>>
            selectedDynamicIndicatorsWithPartialParamsMap)
    {
        var paramValsMap =
            selectedDynamicIndicatorsWithPartialParamsMap[changedParamEvent.Indicator];

        foreach (var (paramName, paramVals) in paramValsMap)
        {
            if (paramName == changedParamEvent.ParameterName)
            {
                if (!changedParamEvent.IsSelected)
                    continue;

                singleParamsPerSelectedDynamicIndicatorCache[paramName] = changedParamEvent.ParamValue;

                continue;
            }

            singleParamsPerSelectedDynamicIndicatorCache[paramName] = paramVals.FirstOrDefault();
        }

        return singleParamsPerSelectedDynamicIndicatorCache;
    }

    private Dictionary<string, IDictionary<string, HashSet<decimal>>>? FilterIndicatorsByHavingRequiredParams(
        DefaultableDictionary<string, DefaultableDictionary<string, HashSet<decimal>>>? indicatorWithPartialParams,
        Dictionary<string, IndicatorParameterDefinitionsHolder>? parameterDefinitionsHolders)
    {
        if (indicatorWithPartialParams is null)
            return null;

        Dictionary<string, IDictionary<string, HashSet<decimal>>>? indicatorsWithParams = new();

        foreach (var (indicator, paramVals)
                 in indicatorWithPartialParams)
        {
            var requiredParamNames = parameterDefinitionsHolders[indicator].ParameterToDefinitionMap.Keys;

            var setParamValueNames =
                paramVals
                    .Where(kv => kv.Value.Any())
                    .Select(kv => kv.Key)
                    .ToHashSet();

            if (requiredParamNames.Except(setParamValueNames).Any())
                continue;

            indicatorsWithParams[indicator] = paramVals;
        }

        return indicatorsWithParams;
    }

    public bool SingleSelectedIndicatorMode { get; }

    private readonly ObservableAsPropertyHelper<Dictionary<string, IndicatorParameterDefinitionsHolder>>?
        _indicatorParameterDefinitionMap;

    public Dictionary<string, IndicatorParameterDefinitionsHolder>? IndicatorParameterDefinitionMap
        => _indicatorParameterDefinitionMap?.Value;

    private readonly ObservableAsPropertyHelper<Dictionary<string, IndicatorParameterDefinitionsHolder>>?
        _indicatorWrapperParameterDefinitionMap;

    public Dictionary<string, IndicatorParameterDefinitionsHolder>? IndicatorWrapperParameterDefinitionMap
        => _indicatorWrapperParameterDefinitionMap?.Value;

    private readonly ObservableAsPropertyHelper<Dictionary<string, Dictionary<string, List<decimal>?>>>
        _indicatorsWithSelectableParamValues;

    public Dictionary<string, Dictionary<string, List<decimal>?>> IndicatorsWithSelectableParamValues =>
        _indicatorsWithSelectableParamValues.Value;

    private readonly ObservableAsPropertyHelper<Dictionary<string, Dictionary<string, List<decimal>?>>>
        _indicatorWrappersWithSelectableParamValues;

    public Dictionary<string, Dictionary<string, List<decimal>?>> IndicatorWrappersWithSelectableParamValues =>
        _indicatorWrappersWithSelectableParamValues.Value;

    [Reactive] public string? SelectedIndicator { get; set; }

    [Reactive] public string? SelectedIndicatorWrapper { get; set; }

    private readonly ObservableAsPropertyHelper<(string x, Dictionary<string, List<decimal>?>)>
        _selectedIndicatorWithSelectableParamValues;

    public (string x, Dictionary<string, List<decimal>?>) SelectedIndicatorWithSelectableParamValues
        => _selectedIndicatorWithSelectableParamValues.Value;

    private readonly
        ObservableAsPropertyHelper<(string x, Dictionary<string, List<decimal>?>)>
        _selectedIndicatorWrappersWithSelectableParamValues;

    public (string x, Dictionary<string, List<decimal>?>)
        SelectedIndicatorWrappersWithSelectableParamValues
        => _selectedIndicatorWrappersWithSelectableParamValues.Value;

    private readonly ObservableAsPropertyHelper<List<IndicatorWithParams>> _indicatorWithParamsList;

    public List<IndicatorWithParams> IndicatorWithParams
        => _indicatorWithParamsList.Value;

    [Reactive] public bool ExistingIndicatorsOnly { get; set; }

    public List<IndicatorWithParams> SelectedAlreadySavedIndicatorCombinations { get; } = new();

    public SelectionModel<IndicatorWithParams> ExistingIndicatorsSelection { get; }

    private DefaultableDictionary<string, DefaultableDictionary<string, HashSet<decimal>>>
        SelectedDynamicIndicatorsWithPartialParams { get; } = new(() => new(() => new()));

    private DefaultableDictionary<string, DefaultableDictionary<string, HashSet<decimal>>>
        SelectedDynamicIndicatorWrappersWithPartialParams { get; } = new(() => new(() => new()));

    private readonly ObservableAsPropertyHelper<Dictionary<string, IDictionary<string, HashSet<decimal>>>?>
        _selectedDynamicIndicatorsWithFullParams;

    private Dictionary<string, IDictionary<string, HashSet<decimal>>>?
        SelectedDynamicIndicatorsWithFullParams => _selectedDynamicIndicatorsWithFullParams.Value;

    private readonly ObservableAsPropertyHelper<Dictionary<string, IDictionary<string, HashSet<decimal>>>?>
        _selectedIndicatorWrappersWithFullParams;

    private Dictionary<string, IDictionary<string, HashSet<decimal>>>?
        SelectedIndicatorWrappersWithFullParams => _selectedIndicatorWrappersWithFullParams.Value;

    private readonly ObservableAsPropertyHelper<List<IndicatorWithParams>?> _filteredSelectedIndicatorsWithParams;

    public List<IndicatorWithParams>? FilteredSelectedIndicatorsWithParams
        => _filteredSelectedIndicatorsWithParams.Value;

    [Reactive] public IndicatorWithParams? FilteredSelectedIndicatorWithParams { get; set; }

    [Reactive] private ChangedParamEvent? LastSelectedParamEvent { get; set; }

    private List<Dictionary<string, decimal>> UnwrapIndicator(
        IDictionary<string, HashSet<decimal>> indicatorParams)
    {
        var cartesianProductOfParams
            = EnumerableUtils.CartesianProduct(indicatorParams.Values);

        var paramNames = indicatorParams.Keys.ToArray();

        var unwrappedParams = new List<Dictionary<string, decimal>>();

        foreach (var pairOfParamValues in cartesianProductOfParams)
        {
            var newParamsPair = new Dictionary<string, decimal>();

            foreach (var (paramValue, i) in pairOfParamValues.WithIndex())
            {
                var paramName = paramNames[i];
                newParamsPair[paramName] = paramValue;
            }

            unwrappedParams.Add(newParamsPair);
        }

        return unwrappedParams;
    }

    private IDictionary<string, List<Dictionary<string, decimal>>> UnwrapIndicators(
        Dictionary<string, IDictionary<string, HashSet<decimal>>> indicatorWrappersWithParams)
    {
        var indicatorsToParameterizedSets =
            new DefaultableDictionary<string, List<Dictionary<string, decimal>>>(() => new());

        foreach (var (indicator, paramToValues)
                 in indicatorWrappersWithParams)
        {
            var unwrappedIndicatorDicts =
                UnwrapIndicator(paramToValues);

            indicatorsToParameterizedSets[indicator]
                .AddRange(unwrappedIndicatorDicts);
        }

        return indicatorsToParameterizedSets;
    }

    public List<IndicatorWithParams>? CalculateIndicatorsWithParametersDependingOnSwitch(
        bool existingIndicatorsOnly,
        List<IndicatorWithParams>? selectedAlreadySavedIndicatorCombinations,
        Dictionary<string, IDictionary<string, HashSet<decimal>>>?
            selectedDynamicIndicatorsWithParams,
        Dictionary<string, IDictionary<string, HashSet<decimal>>>? selectedIndicatorWrappersWithParams)
    {
        List<IndicatorWithParams> baseIndicators;
        if (existingIndicatorsOnly)
        {
            baseIndicators = new List<IndicatorWithParams>(
                selectedAlreadySavedIndicatorCombinations);
        }
        else
        {
            if (selectedDynamicIndicatorsWithParams is null || !selectedDynamicIndicatorsWithParams.Any())
                return null;

            var unrolledIndicators =
                UnwrapIndicators(
                    selectedDynamicIndicatorsWithParams);

            baseIndicators = new();

            foreach (var (indicator, indicatorParamSets)
                     in unrolledIndicators)
            {
                foreach (var (indicatorParamSet, i)
                         in indicatorParamSets.WithIndex())
                {
                    baseIndicators.Add(
                        new(
                            indicator,
                            indicatorParamSet,
                            null));
                }
            }
        }

        var indicatorWithParamsWithWrappers =
            new List<IndicatorWithParams>();

        // unwrapped list per indicator wrapper
        var unwrappedIndicatorWrappers =
            selectedIndicatorWrappersWithParams is not null
                ? UnwrapIndicators(selectedIndicatorWrappersWithParams)
                : null;

        var indicatorWrappers =
            unwrappedIndicatorWrappers?.Keys.ToArray();

        var indicatorWrapperParams =
            unwrappedIndicatorWrappers?.Values?.CartesianProduct()
                .Select(x => x.ToList())
                .ToList();

        IEnumerable<IndicatorWithParams> GetUnrolledIndicatorWrappers()
        {
            if (indicatorWrapperParams is null)
                yield break;

            // list of wrappers
            foreach (var indicatorWrappersParam in indicatorWrapperParams)
            {
                IndicatorWithParams? lastIndicator = null!;

                foreach (var (currParams, i) in indicatorWrappersParam.WithIndex())
                {
                    var indicatorName = indicatorWrappers[i];

                    lastIndicator = new IndicatorWithParams(
                        indicatorName,
                        currParams,
                        lastIndicator);
                }

                yield return lastIndicator;
            }
        }

        foreach (var indicatorWithParam in baseIndicators)
        {
            var unrolledIndicatorWrappers =
                GetUnrolledIndicatorWrappers();

            foreach (var indicatorWrapper in unrolledIndicatorWrappers)
            {
                indicatorWithParamsWithWrappers.Add(
                    indicatorWrapper with { WrappedIndicator = indicatorWithParam });
            }

            if (!indicatorWithParamsWithWrappers.Any())
                indicatorWithParamsWithWrappers.Add(indicatorWithParam);
        }

        return indicatorWithParamsWithWrappers;
    }

    private void ExistingIndicatorsSelectionChanged(
        object? sender,
        SelectionModelSelectionChangedEventArgs<IndicatorWithParams> e)
    {
        this.SelectionChangedHandler2(
            e,
            SelectedAlreadySavedIndicatorCombinations,
            nameof(SelectedAlreadySavedIndicatorCombinations));
    }

    private readonly Dictionary<string, Dictionary<string, List<decimal>?>> _cachedUnrolledParamValues = new();

    private Dictionary<string, List<decimal>?> UnrollParametersForIndicator(
        string indicator,
        Dictionary<string, IndicatorParameterDefinitionsHolder>? indicatorParameterDefinitionsHolders,
        string indicatorParameterDefinitionMapName)
    {
        if (_cachedUnrolledParamValues.ContainsKey(indicator))
            return _cachedUnrolledParamValues[indicator];

        if (indicatorParameterDefinitionsHolders is null
            || !indicatorParameterDefinitionsHolders.ContainsKey(indicator))
        {
            throw new Exception(
                $"{indicatorParameterDefinitionMapName} should not be null " +
                $"and should contain key {indicator}.");
        }

        var indicatorParameterDefinitionsHolder
            = indicatorParameterDefinitionsHolders[indicator];

        var unrolledParamRanges
            = UnrollParamRanges(indicatorParameterDefinitionsHolder);

        _cachedUnrolledParamValues[indicator] = unrolledParamRanges;

        return unrolledParamRanges;
    }

    private Dictionary<string, Dictionary<string, List<decimal>?>> BuildDynamicIndicatorWithParams(
        Dictionary<string, IndicatorParameterDefinitionsHolder> indicatorParameterDefinitions)
    {
        var unrolledParamsPerIndicator =
            new Dictionary<string, Dictionary<string, List<decimal>?>>();

        foreach (var (indicator, parameterDefinitionsHolder) in indicatorParameterDefinitions)
        {
            var unrolledParamRanges = UnrollParamRanges(parameterDefinitionsHolder);

            unrolledParamsPerIndicator[indicator] = unrolledParamRanges;
        }

        return unrolledParamsPerIndicator;
    }

    // return dictionary: key: param name. values: allowed numbers
    private static Dictionary<string, List<decimal>?> UnrollParamRanges(
        IndicatorParameterDefinitionsHolder parameterDefinitionsHolder)
    {
        var allowedParamValues = new Dictionary<string, List<decimal>?>();

        foreach (var (paramName, indicatorParameterDefinition) in parameterDefinitionsHolder.ParameterToDefinitionMap)
        {
            if (!indicatorParameterDefinition.MinValue.HasValue ||
                !indicatorParameterDefinition.MaxValue.HasValue
                || !indicatorParameterDefinition.StepSize.HasValue)
            {
                allowedParamValues[paramName] = null;
                continue;
            }

            var unrolledRange = MathUtils.UnrollRange(
                    indicatorParameterDefinition.MinValue.Value,
                    indicatorParameterDefinition.MaxValue.Value,
                    indicatorParameterDefinition.StepSize.Value,
                    indicatorParameterDefinition.MinValueInclusive,
                    indicatorParameterDefinition.MaxValueInclusive)
                .ToList();

            allowedParamValues[paramName] = unrolledRange;
        }

        return allowedParamValues;
    }

    public record ChangedParamEvent(
        string Indicator,
        string ParameterName,
        decimal ParamValue,
        bool IsSelected,
        bool IsIndicatorWrapper);

    public void UpdateSelectionDynamicIndicators(
        string indicator,
        string parameterName,
        decimal paramValue,
        bool isSelected)
    {
        UpdateSelectionDynamicHelper(
            indicator,
            parameterName,
            paramValue,
            isSelected,
            SelectedDynamicIndicatorsWithPartialParams,
            nameof(SelectedDynamicIndicatorsWithPartialParams),
            false);
    }

    public void UpdateSelectionDynamicIndicatorWrappers(
        string indicator,
        string parameterName,
        decimal paramValue,
        bool isSelected)
    {
        UpdateSelectionDynamicHelper(
            indicator,
            parameterName,
            paramValue,
            isSelected,
            SelectedDynamicIndicatorWrappersWithPartialParams,
            nameof(SelectedDynamicIndicatorWrappersWithPartialParams),
            true);
    }

    private void UpdateSelectionDynamicHelper(
        string indicator,
        string parameterName,
        decimal paramValue,
        bool isSelected,
        DefaultableDictionary<string, DefaultableDictionary<string, HashSet<decimal>>> selectedMap,
        string selectedMapName,
        bool isIndicatorWrapper)
    {
        void PropertyChange(Action a)
        {
            this.RaisePropertyChanging(selectedMapName);
            a();
            this.RaisePropertyChanged(selectedMapName);
        }

        if (isSelected)
        {
            PropertyChange(() =>
                selectedMap[indicator][parameterName].Add(paramValue));

            LastSelectedParamEvent = new ChangedParamEvent(
                indicator,
                parameterName,
                paramValue,
                isSelected,
                isIndicatorWrapper);
        }
        else
        {
            if (!selectedMap.ContainsKey(indicator))
                return;
            if (!selectedMap[indicator].ContainsKey(parameterName))
                return;

            PropertyChange(() =>
                selectedMap[indicator][parameterName].Remove(paramValue));
        }
    }
}