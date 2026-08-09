#if BASTION_DEVTOOLS
using System.Threading.Tasks;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Wave;
using Bastion.Game.Presentation;
using Godot;

namespace Bastion.Game.DevTools;

/// <summary>
/// Walks a scripted wave through every phase and writes a screenshot of each, then quits.
/// </summary>
/// <remarks>
/// <para>
/// <b>A development tool, compiled out unless the build opts in</b> with
/// <c>-p:BastionDevTools=true</c> (see Directory.Build.props). A node that drives the game by itself
/// has no business existing in a player build, so it does not - it is absent from the binary rather
/// than merely dormant. Even in a dev build it stays inert until asked for on the command line.
/// </para>
/// <code>
/// dotnet build "21 Bastion.sln" -p:BastionDevTools=true
/// godot --path . -- --capture
/// godot --path . -- --capture --capture-out C:/somewhere/else
/// </code>
/// <para>
/// It exists because the UI cannot be reviewed by reading it. The layout is anchored regions, wrapped
/// flow containers, and hand-drawn geometry that only resolves at a real viewport size; the first run
/// of this harness found clipped panels, a junction slot straddling two lanes, captions landing
/// mid-row, and - via <see cref="CheckClickPath"/> - a hit test reading the OS cursor instead of the
/// event position, which had made the whole board unclickable. None of that is visible in source.
/// </para>
/// <para>
/// The click check leads deliberately. Screenshots are driven through <see cref="WaveController"/>
/// because that is reliable and repeatable, but driving the controller directly proves nothing about
/// whether a player could have caused the same thing. One synthesised click, asserted, covers that.
/// </para>
/// </remarks>
public partial class CaptureRun : Node
{
    private const string EnableFlag = "--capture";
    private const string OutputFlag = "--capture-out";
    private const string DefaultOutputDirectory = "res://.captures";

    private WaveController _controller = null!;
    private BattlefieldView _battlefield = null!;
    private string _outputDirectory = DefaultOutputDirectory;
    private int _written;
    private int _failures;

    /// <summary>
    /// Attaches the capture run if <c>--capture</c> was passed after <c>--</c>, otherwise does nothing.
    /// </summary>
    /// <remarks>
    /// Reads the user args - everything after <c>--</c> - so the flags cannot collide with Godot's own.
    /// Called from the composition root, which is the only place that has the wired-up nodes to hand.
    /// </remarks>
    public static void AttachIfRequested(Node parent, WaveController controller, BattlefieldView battlefield)
    {
        string[] args = OS.GetCmdlineUserArgs();

        if (System.Array.IndexOf(args, EnableFlag) < 0)
        {
            return;
        }

        var run = new CaptureRun
        {
            Name = "CaptureRun",
            _controller = controller,
            _battlefield = battlefield,
            _outputDirectory = ArgumentAfter(args, OutputFlag) ?? DefaultOutputDirectory,
        };

        parent.AddChild(run);
    }

    public override async void _Ready()
    {
        try
        {
            if (!PrepareOutputDirectory())
            {
                GetTree().Quit(1);
                return;
            }

            await Frames(30);

            await CheckClickPath();
            await CaptureScriptedWave();

            GD.Print($"[capture] wrote {_written} screenshot(s) to {_outputDirectory}");
            GD.Print(_failures == 0 ? "[capture] OK" : $"[capture] {_failures} FAILURE(S)");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[capture] threw: {ex}");
            _failures++;
        }

        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    /// <summary>
    /// Places one card by synthesising a real click, and asserts the board changed.
    /// </summary>
    /// <remarks>
    /// The event goes through <c>Input.ParseInputEvent</c> so it travels the path a player's click
    /// does - viewport, past the Control layer, into the battlefield's unhandled-input handler. A
    /// failure here means the board is unclickable however good the screenshots look.
    /// </remarks>
    private async Task CheckClickPath()
    {
        SocketRef target = SocketRef.InLane(0, 0);
        Vector2 at = _battlefield.ScreenPositionOf(target);

        Godot.Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        await Frames(2);

        foreach (bool pressed in new[] { true, false })
        {
            Godot.Input.ParseInputEvent(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = pressed,
                Position = at,
                GlobalPosition = at,
            });

            await Frames(2);
        }

        await Frames(4);

        if (_controller.Board.Towers.Any(t => t.Socket == target))
        {
            GD.Print("[capture] pass — a click on the board places a tower");
            return;
        }

        GD.PrintErr($"[capture] FAIL — clicking {target} at {at} placed nothing. The board is not accepting clicks.");
        _failures++;
    }

    /// <summary>
    /// The scripted wave. Deterministic: the composition root's fixed seed means the same cards, the
    /// same Dealer hand, and therefore comparable screenshots between runs.
    /// </summary>
    private async Task CaptureScriptedWave()
    {
        await Shot("01-placement");

        Place(SocketRef.InLane(1, 0), Family.Spade);
        await Shot("02-draw-decision");

        _controller.Hit();
        await Settle();
        await Shot("03-placement-after-hit");

        Place(SocketRef.InLane(0, 2), Family.Club);
        await Shot("04-draw-decision-again");

        _controller.Stand();
        await Settle();
        await Shot("05-adjustment-window");

        _controller.Lock();
        await Settle();
        await Shot("06-combat-start");

        Press("Play");
        await Frames(150);
        await Shot("07-combat-midway");

        Press("Skip to end");
        await Settle();
        await Shot("08-combat-complete");
    }

    private void Place(SocketRef socket, Family family)
    {
        if (_controller.Session.Phase == WavePhase.AwaitingPlacement)
        {
            _controller.Place(family, socket);
        }
    }

    private async Task Settle() => await Frames(6);

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private async Task Shot(string name)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        Image image = GetViewport().GetTexture().GetImage();
        Error error = image.SavePng($"{_outputDirectory}/{name}.png");

        if (error != Error.Ok)
        {
            GD.PrintErr($"[capture] could not write {name}.png: {error}");
            _failures++;
            return;
        }

        _written++;
    }

    /// <summary>Clicks the first visible button carrying this label, wherever it is in the tree.</summary>
    private void Press(string label)
    {
        if (FindButton(GetTree().Root, label) is { } button)
        {
            button.EmitSignal(BaseButton.SignalName.Pressed);
            return;
        }

        GD.PrintErr($"[capture] FAIL — no visible button labelled '{label}'");
        _failures++;
    }

    private static Button? FindButton(Node node, string label)
    {
        if (node is Button { Visible: true } button && button.Text == label)
        {
            return button;
        }

        foreach (Node child in node.GetChildren())
        {
            if (FindButton(child, label) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private bool PrepareOutputDirectory()
    {
        Error error = DirAccess.MakeDirRecursiveAbsolute(_outputDirectory);

        if (error is Error.Ok or Error.AlreadyExists)
        {
            return true;
        }

        GD.PrintErr($"[capture] could not create '{_outputDirectory}': {error}");
        return false;
    }

    private static string? ArgumentAfter(string[] args, string flag)
    {
        int index = System.Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
#endif
