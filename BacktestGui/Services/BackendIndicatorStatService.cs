using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using BacktestGui.Configurations;
using CustomShared;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Grpc.Net.Compression;
using GrpcShared;
using IndicatorProto;
using IndicatorServiceShared;
using IndicatorsGrpcShared;
using Microsoft.Extensions.Logging;
using NodaTime;
using IndicatorParameterDefinition = IndicatorServiceShared.IndicatorParameterDefinition;
using IndicatorWithParams = IndicatorServiceShared.IndicatorWithParams;
using ProfitFactorMatrixRow = IndicatorServiceShared.ProfitFactorMatrixRow;
using SamplingFreq = TradeServicesSharedDotNet.Models1.SamplingFreq;

namespace BacktestGui.Services;

public class BackendIndicatorStatGrpcClientWrapper
{
    private readonly IndicatorStatService.IndicatorStatServiceClient _client;

    private readonly uint? _retryCount;

    public BackendIndicatorStatGrpcClientWrapper(
        IndicatorGrpcConfig grpcConfig)
    {
        _retryCount = grpcConfig.RetryCount;

        var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Trace);
        });

        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };


        var channel = GrpcChannel.ForAddress(
            grpcConfig.ConnectionString,
            new GrpcChannelOptions
            {
                LoggerFactory = loggerFactory,
                CompressionProviders = new List<ICompressionProvider>
                {
                    new BrotliCompressionProvider(CompressionLevel.Fastest),
                    new GzipCompressionProvider(CompressionLevel.Optimal)
                },
                HttpHandler = httpClientHandler
            });

        _client = new IndicatorStatService.IndicatorStatServiceClient(channel);
    }

    private Dictionary<string, IndicatorParameterDefinitionsHolder> UnwrapIndicatorParameterDefinitionMap(
        MapField<string, IndicatorParameterDefinitions> indicatorParamDefinitionMap)
    {
        var indicatorParameterDefinitionMap =
            new Dictionary<string, IndicatorParameterDefinitionsHolder>();

        foreach (var (indicator, inIndicatorParameterDefinitions) in
                 indicatorParamDefinitionMap)
        {
            var indicatorParameterDefinitions = new Dictionary<string, IndicatorParameterDefinition>();

            foreach (var (paramName, indicatorParameterDefinition) in inIndicatorParameterDefinitions
                         .ParameterDefinitionMap)
            {
                var indicatorParameterDefinitionConverted =
                    IndicatorsConverters.Convert(indicatorParameterDefinition);

                indicatorParameterDefinitions[paramName] = indicatorParameterDefinitionConverted;
            }

            indicatorParameterDefinitionMap[indicator]
                = new IndicatorParameterDefinitionsHolder(indicatorParameterDefinitions);
        }

        return indicatorParameterDefinitionMap;
    }

    private async Task<T> RunWithRetries<T>(
        Func<Task<T>> requestFunc,
        string requestFuncName)
    {
        return await ExceptionFuncs.RunWithRetries(
            requestFunc,
            requestFuncName,
            _retryCount,
            TimeSpan.FromMilliseconds(200));
    }

    public async Task<IndicatorServiceDefinedParams> GetAllSelectableParamsAsync()
    {
        var selectableParamsResponse = await RunWithRetries(
            async () => await _client.GetAllSelectableParamsAsync(new Empty()),
            nameof(IndicatorStatService.IndicatorStatServiceClient.GetAllSelectableParamsAsync));

        var selectableIndicatorParams = new IndicatorServiceDefinedParams();
        selectableIndicatorParams.SymbolUniverses.AddRange(
            selectableParamsResponse.SymbolUniverses);

        selectableIndicatorParams.IndicatorParameterDefinitionMap =
            UnwrapIndicatorParameterDefinitionMap(
                selectableParamsResponse.IndicatorParameterDefinitions);

        selectableIndicatorParams.IndicatorWrapperParameterDefinitionMap =
            UnwrapIndicatorParameterDefinitionMap(
                selectableParamsResponse.IndicatorWrappersParameterDefinitions);

        var indicatorSavedParamSets =
            new DefaultableDictionary<string, List<IndicatorParams2>>(
                () => new());

        foreach (var indicatorSavedParamSet in selectableParamsResponse.IndicatorSavedParamSets)
        {
            var dictionary = indicatorSavedParamSet.SelectableParams.ToDictionary(
                kv => kv.Key,
                kv => GrpcCommonConverters.Convert(kv.Value));

            indicatorSavedParamSets[indicatorSavedParamSet.Indicator].Add(
                new(
                    dictionary));
        }

        selectableIndicatorParams.IndicatorSavedParamSets = indicatorSavedParamSets;

        return selectableIndicatorParams;
    }

    public async Task<CalcProfitFactorRocResponse>
        CalcProfitFactorRoc(
            CalcProfitFactorRocRequest calcProfitFactorRocRequest,
            CancellationToken cancellationToken)
    {
        return await RunWithRetries(
            async () => await _client.CalcProfitFactorRocAsync(
                calcProfitFactorRocRequest,
                cancellationToken: cancellationToken),
            nameof(IndicatorStatService.IndicatorStatServiceClient.CalcProfitFactorRocAsync));
    }

    public async Task<IndicatorParamFields> GetIndicatorParamFieldsAsync(
        GetIndicatorParamFieldsRequest getIndicatorParamFieldsRequest)
    {
        var getIndicatorParamFieldsResponse =
            await RunWithRetries(
                async () => await _client.GetIndicatorParamFieldsAsync(getIndicatorParamFieldsRequest),
                nameof(IndicatorStatService.IndicatorStatServiceClient.GetIndicatorParamFieldsAsync));

        var indicatorToIndicatorParamValuesDict
            = new Dictionary<string, (IndicatorParameterDefinitionsHolder IndicatorParameterDefinitions,
                List<IndicatorParams2> outIndicatorMultiParamSets)>();

        foreach (var (indicator, currIndicatorData) in getIndicatorParamFieldsResponse.PerIndicatorData)
        {
            var outIndicatorParameterDefinitions =
                currIndicatorData.IndicatorParameterDefinitions.ParameterDefinitionMap
                    .ToDictionary(
                        kv => kv.Key,
                        kv => IndicatorsConverters.Convert(kv.Value));

            var outIndicatorMultiParamSets =
                IndicatorsConverters.Convert(currIndicatorData.IndicatorMultiParamSets);

            var indicatorParameterDefinitionsHolder =
                new IndicatorParameterDefinitionsHolder(outIndicatorParameterDefinitions);

            indicatorToIndicatorParamValuesDict[indicator] =
                (indicatorParameterDefinitionsHolder, outIndicatorMultiParamSets);
        }

        var indicatorParamFieldsResponse = new IndicatorParamFields
        {
            IndicatorParamSetsMap = indicatorToIndicatorParamValuesDict
        };

        return indicatorParamFieldsResponse;
    }

    public async Task<IDictionary<string, List<IndicatorParameterDefinition>>>
        GetIndicatorParameterDefinitions(ICollection<string> indicators)
    {
        var getIndicatorParameterDefinitionRequest = new GetIndicatorParameterDefinitionRequest();
        getIndicatorParameterDefinitionRequest.Indicators.AddRange(indicators);

        var getIndicatorParameterDefinitionResponse =
            await RunWithRetries(
                async () => await _client.GetIndicatorParameterDefinitionsAsync(getIndicatorParameterDefinitionRequest),
                nameof(IndicatorStatService.IndicatorStatServiceClient.GetIndicatorParameterDefinitionsAsync));

        var parameterDefinitionsSetPerIndicator = new DefaultableDictionary<string, List<IndicatorParameterDefinition>>(
            () => new());

        foreach (var (indicator, indicatorParameterDefinitionSet) in getIndicatorParameterDefinitionResponse
                     .IndicatorParameterDefinitionMap)
        {
            foreach (var indicatorParameterDefinition in indicatorParameterDefinitionSet.ParameterDefinitionSets)
            {
                parameterDefinitionsSetPerIndicator[indicator]
                    .Add(IndicatorsConverters.Convert(indicatorParameterDefinition));
            }
        }

        return parameterDefinitionsSetPerIndicator;
    }

    public async Task<IndicatorSummaryStatsResponse> CalcSummaryStats(CalcSummaryStatsRequest calcSummaryStatsRequest)
    {
        var calcSummaryStatsResponse =
            await RunWithRetries(
                async () => await _client.CalcSummaryStatsAsync(calcSummaryStatsRequest),
                nameof(IndicatorStatService.IndicatorStatServiceClient.CalcSummaryStatsAsync));

        var indicatorSummaryStatsMap =
            calcSummaryStatsResponse.IndicatorSummaryStatsTuples
                .ToDictionary(
                    kv => IndicatorsConverters.Convert(kv.IndicatorWithParams),
                    kv => IndicatorsConverters.Convert(kv.IndicatorSummaryStats));

        return new IndicatorSummaryStatsResponse(
            indicatorSummaryStatsMap);
    }
}

public class BackendIndicatorStatService
{
    private readonly BackendIndicatorStatGrpcClientWrapper _backendIndicatorStatGrpcClientWrapper;

    public IObservable<IndicatorServiceDefinedParams> SelectableIndicatorMetaDataConnectableObservable { get; }

    public IObservable<List<IndicatorWithParams>> IndicatorWithParamsList { get; }

    public BackendIndicatorStatService(
        BackendIndicatorStatGrpcClientWrapper backendIndicatorStatGrpcClientWrapper)
    {
        _backendIndicatorStatGrpcClientWrapper = backendIndicatorStatGrpcClientWrapper;

        SelectableIndicatorMetaDataConnectableObservable =
            GetSelectableIndicatorMetaDataAsync()
                .ToObservable()
                .Replay(1)
                // .AutoConnect();
                .RefCount();
        // Observable.FromAsync(GetSelectableIndicatorMetaDataAsync);

        IndicatorWithParamsList =
            SelectableIndicatorMetaDataConnectableObservable
                .Select(x =>
                {
                    var indicatorSavedParamSets = x.IndicatorSavedParamSets;

                    var indicatorWithParamsList = new List<IndicatorWithParams>();

                    foreach (var (indicator, savedParams) in indicatorSavedParamSets)
                    {
                        foreach (var indicatorParams2 in savedParams)
                        {
                            indicatorWithParamsList.Add(new(
                                indicator,
                                indicatorParams2.Values,
                                null));
                        }
                    }

                    return indicatorWithParamsList;
                })
                .Replay(1)
                .RefCount();
    }

    public async Task<IndicatorServiceDefinedParams> GetSelectableIndicatorMetaDataAsync()
    {
        return await _backendIndicatorStatGrpcClientWrapper.GetAllSelectableParamsAsync();
    }

    public async Task<List<ProfitFactorMatrixRow>?> CalcProfitFactorRoc(
        string symbolUniverse,
        IndicatorWithParams indicatorWithParams,
        SamplingFreq samplingFreq,
        bool logReturns,
        LocalDate startDate,
        LocalDate endDate,
        CancellationToken cancellationToken = default)
    {
        var calcProfitFactorRocRequest =
            new CalcProfitFactorRocRequest();
        calcProfitFactorRocRequest.SymbolUniverse = symbolUniverse;
        calcProfitFactorRocRequest.IndicatorWithParams = IndicatorsConverters.Convert(indicatorWithParams);
        calcProfitFactorRocRequest.SamplingFreq = IndicatorsConverters.Convert(samplingFreq);
        calcProfitFactorRocRequest.LogReturns = logReturns;
        calcProfitFactorRocRequest.StartDate = GrpcCommonConverters.Convert(startDate);
        calcProfitFactorRocRequest.EndDate = GrpcCommonConverters.Convert(endDate);

        CalcProfitFactorRocResponse calcProfitFactorRoc;

        try
        {
            calcProfitFactorRoc =
                await _backendIndicatorStatGrpcClientWrapper.CalcProfitFactorRoc(
                    calcProfitFactorRocRequest,
                    cancellationToken);
        }
        catch (Exception e)
        {
            return null;
        }

        var profitFactorMatrixRows
            = new List<IndicatorServiceShared.ProfitFactorMatrixRow>();

        foreach (var profitFactorMatrixRowRes in calcProfitFactorRoc.ProfitFactorMatrixRow)
        {
            var profitFactorMatrixRow = new IndicatorServiceShared.ProfitFactorMatrixRow(
                GrpcCommonConverters.Convert(profitFactorMatrixRowRes.Thresh),
                GrpcCommonConverters.Convert(profitFactorMatrixRowRes.FracGtrOrEq),
                GrpcCommonConverters.Convert(profitFactorMatrixRowRes.FracGtrOrEqLongPf),
                GrpcCommonConverters.Convert(profitFactorMatrixRowRes.FracGtrOrEqShortPf),
                GrpcCommonConverters.Convert(profitFactorMatrixRowRes.FracLess),
                GrpcCommonConverters.Convert(profitFactorMatrixRowRes.FracLessShortPf),
                GrpcCommonConverters.Convert(profitFactorMatrixRowRes.FracLessLongPf)
            );

            profitFactorMatrixRows.Add(profitFactorMatrixRow);
        }

        return profitFactorMatrixRows;
    }

    public async Task<IndicatorParamFields> GetIndicatorParamFieldsAsync(ICollection<string> indicators)
    {
        var getIndicatorParamFieldsRequest = new GetIndicatorParamFieldsRequest();
        getIndicatorParamFieldsRequest.Indicators.AddRange(indicators);

        var indicatorParamFields =
            await _backendIndicatorStatGrpcClientWrapper.GetIndicatorParamFieldsAsync(
                getIndicatorParamFieldsRequest);

        return indicatorParamFields;
    }

    public async Task<IndicatorSummaryStatsResponse> CalcSummaryStats(
        string symbolUniverse,
        IList<IndicatorWithParams> indicatorsWithParams,
        LocalDate startDate,
        LocalDate endDate,
        SamplingFreq samplingFreq)
    {
        var calcSummaryStatsRequest = new CalcSummaryStatsRequest();
        calcSummaryStatsRequest.SymbolUniverse = symbolUniverse;
        calcSummaryStatsRequest.IndicatorWithParams.AddRange(
            indicatorsWithParams.Select(IndicatorsConverters.Convert));
        calcSummaryStatsRequest.StartDate = GrpcCommonConverters.Convert(startDate);
        calcSummaryStatsRequest.EndDate = GrpcCommonConverters.Convert(endDate);
        calcSummaryStatsRequest.SamplingFreq = IndicatorsConverters.Convert(samplingFreq);

        return await
            _backendIndicatorStatGrpcClientWrapper
                .CalcSummaryStats(calcSummaryStatsRequest);
    }
}