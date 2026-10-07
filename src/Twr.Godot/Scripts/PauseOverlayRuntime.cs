using Godot;

namespace Twr.Godot;

public partial class PauseOverlayRuntime : CanvasLayer
{
    public Action? ResumeRequested { get; set; }
    public Action? RestartRequested { get; set; }
    public Action? QuitRequested { get; set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;
        AddChild(new ColorRect
        {
            Color = new Color(0,0,0,0.72f),
            AnchorRight = 1,
            AnchorBottom = 1
        });

        AddChild(LabelAt(455,145,780,54,34,"PAUSED"));
        AddChild(LabelAt(455,210,790,34,16,"MASTER VOLUME"));

        var volume = new HSlider
        {
            OffsetLeft = 455, OffsetTop = 250, OffsetRight = 820, OffsetBottom = 286,
            MinValue = 0, MaxValue = 1, Step = 0.01, Value = CurrentMasterVolume()
        };
        volume.ValueChanged += value =>
        {
            var bus=AudioServer.GetBusIndex("Master");
            AudioServer.SetBusVolumeDb(bus,Mathf.LinearToDb(Mathf.Clamp((float)value,0.0001f,1f)));
            AudioServer.SetBusMute(bus,value<=0.0001);
        };
        AddChild(volume);

        AddChild(ButtonAt(455,325,820,378,"RESUME",() => ResumeRequested?.Invoke()));
        AddChild(ButtonAt(455,395,820,448,"RESTART MAP",() => RestartRequested?.Invoke()));
        AddChild(ButtonAt(455,465,820,518,"QUIT TO MENU",() => QuitRequested?.Invoke()));
        AddChild(LabelAt(455,545,820,32,14,"P / ESC: RESUME"));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo &&
            (key.Keycode is Key.P or Key.Escape))
            ResumeRequested?.Invoke();
    }

    private static double CurrentMasterVolume()
    {
        var bus=AudioServer.GetBusIndex("Master");
        if(AudioServer.IsBusMute(bus))return 0;
        return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(bus));
    }

    private static Label LabelAt(float l,float t,float r,float h,int size,string text)
    {
        var label=new Label { OffsetLeft=l,OffsetTop=t,OffsetRight=r,OffsetBottom=t+h,Text=text };
        label.AddThemeFontSizeOverride("font_size",size);
        return label;
    }

    private static Button ButtonAt(float l,float t,float r,float b,string text,Action pressed)
    {
        var button=new Button { OffsetLeft=l,OffsetTop=t,OffsetRight=r,OffsetBottom=b,Text=text };
        button.Pressed+=pressed;
        return button;
    }
}
