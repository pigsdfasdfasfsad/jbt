using Godot;
using Twr.Domain.Model;
using Twr.Domain.Services;

namespace Twr.Godot;

public partial class GameplayHud : CanvasLayer
{
    public LocalSessionNode? Runtime {get;set;}
    private Label _top = null!;
    private Label _health = null!;
    private Label _ammo = null!;
    private Label _credits = null!;
    private Label _banner = null!;
    private Label _objective = null!;
    private Label _utility = null!;
    private ColorRect _damageFlash = null!;
    private ColorRect _lowHealth = null!;
    private Control _armorRow = null!;
    private Label _armorLabel = null!;
    private readonly ColorRect[] _armorBars = new ColorRect[4];
    private double _damageFlashSeconds;

    public override void _Ready()
    {
        _top = MakeLabel(24, 20, 760, 42, 24);
        _health = MakeLabel(24, 650, 360, 38, 22);
        _ammo = MakeLabel(860, 650, 390, 38, 22);
        _credits = MakeLabel(24, 610, 500, 34, 18);
        _banner = MakeLabel(250, 90, 780, 70, 32);
        _banner.HorizontalAlignment = HorizontalAlignment.Center;
        _objective = MakeLabel(250, 150, 780, 42, 20);
        _objective.HorizontalAlignment = HorizontalAlignment.Center;
        _utility = MakeLabel(250, 195, 780, 36, 17);
        _utility.HorizontalAlignment = HorizontalAlignment.Center;
        _armorLabel=MakeLabel(24,570,92,20,14);
        _armorLabel.Text="ARMOR";
        _armorRow=new Control
        {
            OffsetLeft=118,OffsetTop=575,OffsetRight=422,OffsetBottom=585
        };
        AddChild(_armorRow);
        for(var i=0;i<4;i++)
        {
            var back=new ColorRect
            {
                OffsetLeft=i*76,OffsetTop=0,OffsetRight=i*76+72,OffsetBottom=8,
                Color=new Color(0.094f,0.094f,0.102f,0.72f),
                MouseFilter=Control.MouseFilterEnum.Ignore
            };
            _armorRow.AddChild(back);
            var fill=new ColorRect
            {
                OffsetLeft=0,OffsetTop=0,OffsetRight=72,OffsetBottom=8,
                Color=new Color(70f/255f,140f/255f,230f/255f),
                MouseFilter=Control.MouseFilterEnum.Ignore
            };
            back.AddChild(fill);
            _armorBars[i]=fill;
        }

        // APPROXIMATED base presentation: original post-processing stack is not
        // recovered. Perk reduction percentages applied to these effects are exact.
        _lowHealth = new ColorRect
        {
            Color = new Color(0.45f,0.0f,0.0f,0.0f),
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_lowHealth);

        _damageFlash = new ColorRect
        {
            Color = new Color(0.72f,0.03f,0.02f,0.0f),
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_damageFlash);

        var crosshair = MakeLabel(620, 342, 40, 40, 26);
        crosshair.Text = "+";
        crosshair.HorizontalAlignment = HorizontalAlignment.Center;
    }

    private Label MakeLabel(float left, float top, float width, float height, int size)
    {
        var label = new Label
        {
            OffsetLeft = left,
            OffsetTop = top,
            OffsetRight = left + width,
            OffsetBottom = top + height
        };
        label.AddThemeFontSizeOverride("font_size", size);
        AddChild(label);
        return label;
    }

    public void UpdateState(PlayerState player, MatchState match, string stage, double seconds, string weaponName, string? throwableName=null)
    {
        _top.Text = $"REGULAR  |  {match.MapName.ToUpperInvariant()}  |  WAVE {match.Wave}/15  |  {stage}  {FormatTime(seconds)}";
        _health.Text = $"HEALTH {Math.Ceiling(player.Health):0}/{player.MaxHealth:0}" +
            (player.GasMaskActive ? "  MASK" : "") +
            (player.EnergyDrinkSeconds>0 ? $"  DRINK {Math.Ceiling(player.EnergyDrinkSeconds):0}s" : "");

        if(throwableName is not null)
        {
            _ammo.Text=$"{throwableName.ToUpperInvariant()}  x{player.Inventory.GetValueOrDefault(throwableName)}";
        }
        else
        {
            var spec = RuntimeWeaponCatalog.Get(weaponName);
            if (spec.IsMelee)
            {
                _ammo.Text = $"{weaponName.ToUpperInvariant()}   MELEE";
            }
            else
            {
                var loaded = player.Ammo.GetValueOrDefault(weaponName);
                var reserve = player.ReserveAmmo.GetValueOrDefault(weaponName);
                _ammo.Text = $"{weaponName.ToUpperInvariant()}   {loaded} / {reserve}";
            }
        }

        _credits.Text = $"LEVEL {player.Level}  XP {player.Xp:N0}/{ProgressionRules.RequiredForNextLevel(player.Level):N0}  CREDITS ${player.Credits:N0}  [1/2/3] WEAPONS [5/6/7/G] GRENADES [F] HAMMER";
        UpdateArmor(player);

        var healthRatio=player.MaxHealth<=0 ? 1f : player.Health/player.MaxHealth;
        var baseLowAlpha=healthRatio<0.35f ? Math.Clamp((0.35f-healthRatio)/0.35f*0.45f,0f,0.45f) : 0f;
        if(Runtime?.HasPerk("Hardened Sight")==true) baseLowAlpha*=0.5f;
        _lowHealth.Color=new Color(0.45f,0f,0f,baseLowAlpha);
    }

    public override void _Process(double delta)
    {
        if(_damageFlashSeconds<=0)
        {
            _damageFlash.Color=new Color(0.72f,0.03f,0.02f,0f);
            return;
        }

        _damageFlashSeconds=Math.Max(0,_damageFlashSeconds-delta);
        var alpha=0.42f*(float)Math.Clamp(_damageFlashSeconds/0.22,0,1);
        if(Runtime?.HasPerk("Bruiser")==true)alpha*=0.35f;
        _damageFlash.Color=new Color(0.72f,0.03f,0.02f,alpha);
    }

    public void FlashDamage()
    {
        // APPROXIMATED base flash duration. Bruiser's 65% reduction is VERIFIED.
        _damageFlashSeconds=0.22;
    }

    private void UpdateArmor(PlayerState player)
    {
        var visible=player.ArmorDurability>0;
        _armorRow.Visible=visible;
        _armorLabel.Visible=visible;
        if(!visible)return;

        var juggernaut=player.ArmorKind=="Juggernaut";
        _armorLabel.Text=juggernaut ? "JUG" : "ARMOR";
        var max=juggernaut ? 80f : 40f;
        var per=max/4f;
        var color=juggernaut
            ? new Color(230f/255f,140f/255f,40f/255f)
            : new Color(70f/255f,140f/255f,230f/255f);
        for(var i=0;i<4;i++)
        {
            var fraction=Math.Clamp((player.ArmorDurability-i*per)/per,0f,1f);
            _armorBars[i].OffsetRight=72f*fraction;
            _armorBars[i].Color=color;
        }
    }
    public void SetBanner(string text) => _banner.Text = text;
    public void SetObjective(string text) => _objective.Text = text;
    public void SetUtility(string text) => _utility.Text = text;

    private static string FormatTime(double seconds)
    {
        var total = Math.Max(0, (int)Math.Ceiling(seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}
