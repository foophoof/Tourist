using Autofac;
using Autofac.Extensions.DependencyInjection;
using DalaMock.Host.Factories;
using DalaMock.Host.LoggingProviders;
using DalaMock.Shared.Classes;
using DalaMock.Shared.Interfaces;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVWeather.Lumina;
using Lumina;
using Lumina.Excel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tourist.Services;
using Tourist.Windows;

namespace Tourist;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;
    private readonly IPluginLog _pluginLog;
    private readonly NativeUiService _nativeUiService;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        IDataManager dataManager,
        ICommandManager commandManager,
        IClientState clientState,
        IGameGui gameGui,
        IFramework framework,
        IGameInteropProvider gameInteropProvider)
    {
        _nativeUiService = new NativeUiService(pluginInterface, framework, pluginLog);
        _nativeUiService.Startup();

        _pluginLog = pluginLog;

        // _host = CreateHost();
        // Start();

        var hostBuilder = new HostBuilder()
            .UseContentRoot(pluginInterface.ConfigDirectory.FullName)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureLogging(lb =>
            {
                lb.ClearProviders();
                lb.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DalamudLoggingProvider>(b =>
                    new DalamudLoggingProvider(b.GetRequiredService<IPluginLog>())));
                lb.SetMinimumLevel(LogLevel.Trace);
            })
            .ConfigureContainer<ContainerBuilder>(collection =>
            {
                collection.RegisterInstance(pluginInterface).As<IDalamudPluginInterface>().AsSelf();
                collection.RegisterType<DalamudWindowSystem>().As<IWindowSystem>();
                collection.RegisterType<WindowSystemFactory>().As<IWindowSystemFactory>().AsSelf().SingleInstance();

                collection.RegisterInstance(pluginLog).AsSelf().AsImplementedInterfaces();
                collection.RegisterInstance(dataManager).AsSelf().AsImplementedInterfaces();
                collection.RegisterInstance(commandManager).AsSelf().AsImplementedInterfaces();
                collection.RegisterInstance(clientState).AsSelf().AsImplementedInterfaces();
                collection.RegisterInstance(gameGui).AsSelf().AsImplementedInterfaces();
                collection.RegisterInstance(framework).AsSelf().AsImplementedInterfaces();
                collection.RegisterInstance(gameInteropProvider).AsSelf().AsImplementedInterfaces();

                collection.Register<IUiBuilder>(c =>
                {
                    var pluginInterface = c.Resolve<IDalamudPluginInterface>();
                    return pluginInterface.UiBuilder;
                });
            });
        hostBuilder.ConfigureContainer<ContainerBuilder>(ConfigureContainer);
        _host = hostBuilder.Build();

        Start();
    }

    public bool IsStarted { get; private set; }

    public void Start()
    {
        if (_host == null)
        {
            _pluginLog.Error("You attempted to start the plugin before the container has been built.");
            return;
        }

        var startTask = _host.StartAsync();
        if (startTask.IsFaulted)
        {
            _pluginLog.Error(startTask.Exception, "Plugin startup faulted.");
            throw startTask.Exception;
        }

        IsStarted = true;
    }

    public void Dispose()
    {
        _pluginLog.Verbose("Plugin.Dispose, getting NativeUiService");
        // var nativeUiService = _host.Services.GetService<NativeUiService>();

        _pluginLog.Verbose("Plugin.Dispose, stopping host");
        if (IsStarted)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            IsStarted = false;
        }
        _host.Dispose();

        _pluginLog.Verbose("Plugin.Dispose, cleaning up NativeUiService");
        _nativeUiService.Cleanup();

        _pluginLog.Verbose("Plugin.Dispose, done");
    }

    private List<Type> HostedServices { get; } =
    [
        typeof(CommandService),
        typeof(ConfigurationLoaderService),
        typeof(InstallerWindowService),
        typeof(MarkerService),
        typeof(VfxService),
        typeof(VistaUnlockedListenerService),
        typeof(WindowService),
    ];

    private void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        foreach (var hostedService in HostedServices)
        {
            containerBuilder.RegisterType(hostedService).AsSelf().AsImplementedInterfaces().SingleInstance();
        }

        containerBuilder.Register(c => c.Resolve<IDataManager>().GameData).SingleInstance();
        containerBuilder.Register(c => new FFXIVWeatherLuminaService(c.Resolve<GameData>()));

        containerBuilder.RegisterType<MainWindow>().As<Window>().AsSelf().SingleInstance();

        // Sheets
        containerBuilder.RegisterGeneric((context, parameters) =>
            {
                var gameData = context.Resolve<GameData>();
                var method = typeof(GameData).GetMethod(nameof(GameData.GetExcelSheet))
                    ?.MakeGenericMethod(parameters);
                var sheet = method!.Invoke(gameData, [null, null])!;
                return sheet;
            })
            .As(typeof(ExcelSheet<>));

        containerBuilder.Register(s =>
        {
            var configurationLoaderService = s.Resolve<ConfigurationLoaderService>();
            return configurationLoaderService.GetPluginConfig();
        }).SingleInstance();
    }
}
