using System;
using System.Reactive.Concurrency;
using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using BacktestCSharpShared;
using BacktestGui.Configurations;
using BacktestGui.Services;
using BacktestGui.Utils;
using BacktestGui.ViewModels;
using BacktestGui.ViewModels.Indicators;
using BacktestGui.ViewModels.Misc;
using BacktestGui.ViewModels.Shared;
using BacktestGui.ViewModels.TabViewModels;
using BacktestGui.Views;
using BacktestGui.Views.Helpers;
using CustomShared;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using ReactiveUI;
using Splat;
using Splat.Autofac;
using StockDataCore.Db;

namespace BacktestGui
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            DotEnvLoader.Load();
            MongoShared.Setup();

            RegisterAllDependencies();
        }

        private static IConfiguration LoadMicrosoftConfiguration()
        {
            var appSettingsFile = PathUtils.FindPath("appsettings.json");

            return new ConfigurationBuilder()
                .AddJsonFile(appSettingsFile)
                .Build();
        }

        private static void LoadConfigurationsHelper<T>(
            ContainerBuilder containerBuilder,
            IConfiguration configuration,
            string sectionName) where T : class, new()
        {
            var config = new T();
            configuration.GetSection(sectionName).Bind(config);
            containerBuilder.RegisterInstance(config)
                .As<T>()
                .SingleInstance();
        }

        private static void LoadConfigurations(
            ContainerBuilder containerBuilder)
        {
            var configuration = LoadMicrosoftConfiguration();

            LoadConfigurationsHelper<IndicatorGrpcConfig>(
                containerBuilder,
                configuration,
                "IndicatorGrpc");

            LoadConfigurationsHelper<IndicatorGrpcClientConfiguration>(
                containerBuilder,
                configuration,
                "IndicatorGrpcClient");
        }

        public static Globals BuildGlobals()
        {
            return new Globals(TimeSpan.FromMilliseconds(200));
        }

        public static void RegisterAllDependencies()
        {
            var builder = new ContainerBuilder();

            builder.Register(ctx => MongoCommon.GetMongoClient());
            builder.Register(ctx =>
                    new MongoBacktestsRepo(
                        ctx.Resolve<MongoClient>()))
                .SingleInstance();
            builder.RegisterType<BacktestService>()
                .SingleInstance();
            builder.RegisterType<BacktestsConnectable>()
                .SingleInstance();
            builder.RegisterType<Converters>()
                .SingleInstance();
            builder.Register(ctx => BuildGlobals())
                .SingleInstance();
            builder.Register(ctx =>
                    new YearNonWeekendClosedDayChecker(ConfigVariables.Instance.MarketDayClosedListDir))
                .As<IYearNonWeekendClosedDayChecker>()
                .SingleInstance();
            builder.RegisterType<MarketDayChecker>()
                .SingleInstance();
            builder.RegisterType<ScottPlotHelper>()
                .SingleInstance();
            builder.RegisterType<ScottPlotHelperFactory>()
                .SingleInstance();

            builder.RegisterType<BacktestSelectorViewModel>()
                .As<IBacktestSelectorViewModel>()
                .SingleInstance();
            builder.RegisterType<BacktestGroupSelectorViewModel>()
                .As<IBacktestGroupSelectorViewModel>()
                .SingleInstance();
            builder.RegisterType<MultiBacktestParametersComparisonBarChartViewModel>()
                .SingleInstance();
            builder.RegisterType<TradesViewModel>()
                .SingleInstance();
            builder.RegisterType<IndividualBacktestStatViewerTabViewModel>()
                .SingleInstance();
            builder.RegisterType<MainWindowViewModel>()
                .SingleInstance();
            builder.RegisterType<IndividualBacktestStatsViewModel>()
                .SingleInstance();
            builder.RegisterType<TradesPnlChartViewModel>()
                .SingleInstance();
            builder.RegisterType<AllBacktestsDataViewModel>()
                .SingleInstance();
            builder.RegisterType<MaeToMfeChartViewModel>()
                .SingleInstance();
            builder.RegisterType<IndicatorResearchTabViewModel>()
                .SingleInstance();
            builder.RegisterType<MultiIndicatorTableViewModel>()
                .SingleInstance();
            builder.RegisterType<ProfitFactorTableViewModel>()
                .SingleInstance();
            builder.RegisterType<BackendIndicatorStatService>()
                .SingleInstance();
            builder.RegisterType<BackendIndicatorStatGrpcClientWrapper>()
                .SingleInstance();
            builder.RegisterType<TradesPlotPerTimeScatterPlotViewModel>()
                .SingleInstance();

            // factories
            builder.RegisterType<IndicatorSelectorViewModelFactory>()
                .SingleInstance();
            builder.RegisterType<MultiFieldSelectorViewModelFactory>()
                .SingleInstance();

            // non single instance view models
            builder.RegisterType<SymbolUniverseSelectorViewModel>();
            builder.RegisterType<DateRangeSelectorViewModel>();
            builder.RegisterType<SamplingFreqSelectorViewModel>();

            LoadConfigurations(builder);

            var autofacResolver = SplatAutofacExtensions.UseAutofacDependencyResolver(builder);
            // Locator.SetLocator(autofacResolver);
            builder.RegisterInstance(autofacResolver);

            autofacResolver.InitializeSplat();
            autofacResolver.InitializeReactiveUI();
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            Locator.CurrentMutable.RegisterConstant<IActivationForViewFetcher>(new AvaloniaActivationForViewFetcher());
            Locator.CurrentMutable.RegisterConstant<IPropertyBindingHook>(new AutoDataTemplateBindingHook());

            autofacResolver.SetLifetimeScope(
                builder.Build());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Bootstrapper.GetService<MainWindowViewModel>()
                };

                // RxApp.TaskpoolScheduler.Schedule(Tests.InitBacktests);
                RxApp.TaskpoolScheduler.Schedule(LoadBacktests);
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void LoadBacktests()
        {
            var backtestManager = Bootstrapper.GetService<BacktestService>();

            Bootstrapper.GetService<BacktestsConnectable>().SetNewBacktests(
                backtestManager.GetBacktestEntries());
        }
    }
}