using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using System.Numerics;

namespace Tourist.Addons;

public class AddonTourist(NativeController nativeController) : NativeAddon
{
    private VistaNode? _vistaNode;

    private const float FramePadding = 8.0f;

    protected override unsafe void OnSetup(AtkUnitBase* addon)
    {
        _vistaNode = new VistaNode(nativeController)
        {
            Position = new Vector2(FramePadding, FramePadding + addon->WindowHeaderCollisionNode->Height),
            IsVisible = true,
            Number = "001",
            Title = "Test",
        };
        nativeController.AttachToAddon(_vistaNode, this);
    }

    protected override unsafe void OnHide(AtkUnitBase* addon)
    {
        if (_vistaNode is null) return;

        _vistaNode.Number = "000";
        _vistaNode.Title = "None";
    }
}

public class VistaNode : ResNode
{
    private readonly TextNode _numberText;
    private readonly TextNode _titleText;

    public VistaNode(NativeController nativeController)
    {
        _numberText = new TextNode
        {
            IsVisible = true,
            AlignmentType = AlignmentType.TopLeft,
            FontSize = 20,
        };
        nativeController.AttachToNode(_numberText, this, NodePosition.AsLastChild);

        _titleText = new TextNode
        {
            IsVisible = true,
            AlignmentType = AlignmentType.TopLeft,
            FontSize = 20,
            Position = new Vector2(10, 50),
        };
        nativeController.AttachToNode(_titleText, this, NodePosition.AsLastChild);
    }

    public string Number
    {
        get => _numberText.Text.ToString();
        set => _numberText.Text = value;
    }

    public string Title
    {
        get => _titleText.Text.ToString();
        set => _titleText.Text = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _numberText.Dispose();
        _titleText.Dispose();

        base.Dispose(disposing);
    }
}
