using Autofac;
using DalaMock.Host.Hosting;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVWeather.Lumina;
using KamiToolKit;
using Lumina;
using Lumina.Excel;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Tourist.Addons;
using Tourist.Services;
using Tourist.Windows;

namespace Tourist;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class Plugin : HostedPlugin
{
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        IDataManager dataManager,
        ICommandManager commandManager,
        IClientState clientState,
        IGameGui gameGui,
        IFramework framework,
        IGameInteropProvider gameInteropProvider)
        : base(
            pluginInterface,
            pluginLog,
            dataManager,
            commandManager,
            clientState,
            gameGui,
            framework,
            gameInteropProvider)
    {
        CreateHost();
        Start();
    }

    private List<Type> HostedServices { get; } =
    [
        typeof(CommandService),
        typeof(ConfigurationLoaderService),
        typeof(InstallerWindowService),
        typeof(MarkerService),
        typeof(NativeUiService),
        typeof(VfxService),
        typeof(VistaUnlockedListenerService),
        typeof(WindowService),
    ];

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        foreach (var hostedService in HostedServices)
        {
            containerBuilder.RegisterType(hostedService).AsSelf().AsImplementedInterfaces().SingleInstance();
        }

        containerBuilder.Register(c => c.Resolve<IDataManager>().GameData).SingleInstance();
        containerBuilder.Register(c => new FFXIVWeatherLuminaService(c.Resolve<GameData>()));

        containerBuilder.RegisterType<MainWindow>().As<Window>().AsSelf().SingleInstance();

        // containerBuilder.RegisterType<NativeController>().AsSelf().SingleInstance();
        // containerBuilder
        //     .RegisterType<AddonTourist>()
        //     .WithProperty("InternalName", "Tourist")
        //     .WithProperty("Title", "Tourist")
        //     .WithProperty("Size", new Vector2(350.0f, 450.0f))
        //     .AsSelf()
        //     .SingleInstance();

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

    public override void ConfigureServices(IServiceCollection serviceCollection)
    { }
}
