using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tourist.Util;

namespace Tourist.Services;

// Mostly copied from https://git.anna.lgbt/anna/OrangeGuidanceTomestone/src/branch/main/client/Vfx.cs
public unsafe class VfxService : IHostedService
{
    private static readonly byte[] Pool = "Client.System.Scheduler.Instance.VfxObject\0"u8.ToArray();

    private readonly IFramework _framework;
    private readonly IPluginLog _pluginLog;
    private readonly Stopwatch _queueTimer = Stopwatch.StartNew();

    public VfxService(IFramework framework, IPluginLog pluginLog, IGameInteropProvider interopProvider)
    {
        _framework = framework;
        _pluginLog = pluginLog;

        interopProvider.InitializeFromAttributes(this);
    }

    private SemaphoreSlim Mutex { get; } = new(1, 1);
    private Dictionary<ushort, nint> Spawned { get; } = [];
    private Queue<IQueueAction> Queue { get; } = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _framework.Update += HandleQueues;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _framework.Update -= HandleQueues;
        RemoveAllSync();

        return Task.CompletedTask;
    }

    private void HandleQueues(IFramework framework)
    {
        _queueTimer.Restart();

        while (_queueTimer.Elapsed < TimeSpan.FromMilliseconds(1))
        {
            if (!Queue.TryDequeue(out var action))
            {
                return;
            }

            switch (action)
            {
                case AddQueueAction add:
                    {
                        using var guard = Mutex.With();
                        _pluginLog.Debug($"adding vfx for {add.Id}");
                        if (Spawned.Remove(add.Id, out var existing))
                        {
                            _pluginLog.Warning($"vfx for {add.Id} already exists, queueing remove");
                            Queue.Enqueue(new RemoveRawQueueAction(existing));
                        }

                        var vfx = SpawnStatic(add.Path, add.Position, add.Rotation);
                        Spawned[add.Id] = (nint)vfx;
                        break;
                    }

                case RemoveQueueAction remove:
                    {
                        using var guard = Mutex.With();
                        _pluginLog.Debug($"removing vfx for {remove.Id}");
                        if (!Spawned.Remove(remove.Id, out var ptr))
                        {
                            break;
                        }

                        RemoveStatic((VfxStruct*)ptr);
                        break;
                    }

                case RemoveRawQueueAction remove:
                    {
                        _pluginLog.Debug($"removing raw vfx at {remove.Pointer:X}");
                        RemoveStatic((VfxStruct*)remove.Pointer);
                        break;
                    }
            }
        }
    }

    private void RemoveAllSync()
    {
        using var guard = Mutex.With();

        foreach (var spawned in Spawned.Values.ToArray())
        {
            RemoveStatic((VfxStruct*)spawned);
        }

        Spawned.Clear();
    }

    internal void QueueSpawn(ushort id, string path, Vector3 pos, Quaternion rotation)
    {
        using var guard = Mutex.With();
        Queue.Enqueue(new AddQueueAction(id, path, pos, rotation));
    }

    internal void QueueRemove(ushort id)
    {
        using var guard = Mutex.With();
        Queue.Enqueue(new RemoveQueueAction(id));
    }

    internal void QueueRemoveAll()
    {
        using var guard = Mutex.With();

        foreach (var id in Spawned.Keys)
        {
            Queue.Enqueue(new RemoveQueueAction(id));
        }
    }

    private VfxStruct* SpawnStatic(string path, Vector3 position, Quaternion rotation)
    {
        VfxStruct* vfx;
        fixed (byte* p = Encoding.UTF8.GetBytes(path).NullTerminate())
        {
            fixed (byte* pool = Pool)
            {
                vfx = _staticVfxCreate(p, pool);
            }
        }

        if (vfx == null)
        {
            return null;
        }

        vfx->Position = new Vector3(position.X, position.Z + 0.5f, position.Y);
        vfx->Rotation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);

        vfx->Flags |= 2;
        vfx->Unknown0 = 0;
        vfx->UnknownV3 = new Vector3(0f, 0f, 0f);

        _staticVfxRun(vfx, 1.0f, -1);

        return vfx;
    }

    private void RemoveStatic(VfxStruct* vfx)
    {
        _staticVfxRemove(vfx);
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct VfxStruct
    {
        [FieldOffset(0x38)] public nint Flags;
        [FieldOffset(0x50)] public Vector3 Position;
        [FieldOffset(0x60)] public Quaternion Rotation;
        [FieldOffset(0x70)] public Vector3 Scale;
        [FieldOffset(0x280)] public Vector3 UnknownV3;
        [FieldOffset(0x28C)] public int Unknown0;
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    [Signature("E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08")]
    private readonly delegate* unmanaged<byte*, byte*, VfxStruct*> _staticVfxCreate;

    [Signature("E8 ?? ?? ?? ?? 8B 4B 7C 85 C9")]
    private readonly delegate* unmanaged<VfxStruct*, float, int, ulong> _staticVfxRun;

    [Signature("40 53 48 83 EC 20 48 8B D9 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 28 33 D2 E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9")]
    private readonly delegate* unmanaged<VfxStruct*, nint> _staticVfxRemove;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}

internal interface IQueueAction;

internal sealed record AddQueueAction(
    ushort Id,
    string Path,
    Vector3 Position,
    Quaternion Rotation) : IQueueAction;

internal sealed record RemoveQueueAction(ushort Id) : IQueueAction;

internal sealed record RemoveRawQueueAction(nint Pointer) : IQueueAction;
