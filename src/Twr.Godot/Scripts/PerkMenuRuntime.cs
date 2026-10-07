using Godot;

namespace Twr.Godot;

public partial class PerkMenuRuntime : CanvasLayer
{
    public LocalSessionNode Runtime {get;set;}=null!;
    public Action? CloseRequested {get;set;}

    public override void _Ready() => Rebuild();

    private void Rebuild()
    {
        foreach(var child in GetChildren())child.QueueFree();

        AddChild(new ColorRect
        {
            Color=new Color(0.025f,0.027f,0.03f),
            AnchorRight=1,
            AnchorBottom=1
        });

        AddChild(LabelAt(48,28,900,55,34,"PERKS"));
        AddChild(LabelAt(50,78,940,56,15,
            $"EQUIPPED {Runtime.Profile?.EquippedPerks.Count ?? 0}/3  |  " +
            "Regular offline uses the three default perk slots."));

        var back=ButtonAt(1010,35,1200,82,"BACK",()=>CloseRequested?.Invoke());
        AddChild(back);

        var scroll=new ScrollContainer
        {
            OffsetLeft=50,OffsetTop=145,OffsetRight=1210,OffsetBottom=690
        };
        AddChild(scroll);

        var list=new VBoxContainer{CustomMinimumSize=new Vector2(1100,0)};
        list.AddThemeConstantOverride("separation",5);
        scroll.AddChild(list);

        var profile=Runtime.Profile;
        var player=Runtime.Player;
        foreach(var perk in RuntimePerkCatalog.All())
        {
            var equipped=profile?.EquippedPerks.Contains(perk.Name)==true;
            var unlocked=(player?.Level ?? 1)>=perk.Level;
            var full=(profile?.EquippedPerks.Count ?? 0)>=3 && !equipped;
            var button=new Button
            {
                CustomMinimumSize=new Vector2(1080,54),
                Alignment=HorizontalAlignment.Left,
                Disabled=!unlocked || full,
                Text=(equipped?"[EQUIPPED] ":unlocked?"[AVAILABLE] ":"[LOCKED] ") +
                     $"{perk.Category.ToUpperInvariant()} | {perk.Name} | LVL {perk.Level} | {perk.Description}"
            };
            var selected=perk;
            button.Pressed+=()=>
            {
                Runtime.SetPerk(selected.Name,!Runtime.HasPerk(selected.Name));
                Rebuild();
            };
            list.AddChild(button);
        }
    }

    private static Label LabelAt(float l,float t,float r,float h,int size,string text)
    {
        var label=new Label{OffsetLeft=l,OffsetTop=t,OffsetRight=r,OffsetBottom=t+h,Text=text};
        label.AddThemeFontSizeOverride("font_size",size);
        return label;
    }

    private static Button ButtonAt(float l,float t,float r,float b,string text,Action action)
    {
        var button=new Button{OffsetLeft=l,OffsetTop=t,OffsetRight=r,OffsetBottom=b,Text=text};
        button.Pressed+=action;
        return button;
    }
}
