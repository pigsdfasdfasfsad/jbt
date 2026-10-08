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
    private ColorRect _healthBar = null!;
    private WeaponDialHud _weaponDial = null!;
    private double _damageFlashSeconds;

    public override void _Ready()
    {
        // Panels are anchored to the viewport. Original proportions are
        // approximated from reference stills, rather than fixed 1280x720 text.
        var topShade = new ColorRect
        {
            Name = "WaveHeaderBackdrop",
            AnchorRight = 1,
            OffsetBottom = 56,
            Color = new Color(.025f,.034f,.043f,.61f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(topShade);

        var leftShade = new ColorRect
        {
            Name = "SurvivalStatusBackdrop",
            AnchorTop = 1,
            AnchorBottom = 1,
            OffsetLeft = 15,
            OffsetRight = 425,
            OffsetTop = -146,
            OffsetBottom = -15,
            Color = new Color(.025f,.035f,.039f,.70f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(leftShade);

        _top = MakeLabel(20, 10, 1240, 40, 18);
        _top.AnchorLeft = 0;
        _top.AnchorRight = 1;
        _top.OffsetLeft = 18;
        _top.OffsetRight = -18;
        _top.HorizontalAlignment = HorizontalAlignment.Center;
        _top.AddThemeColorOverride("font_color",
            new Color(.90f,.91f,.86f));

        _health = MakeLabel(29, 0, 380, 35, 24);
        _health.AnchorTop = 1;
        _health.AnchorBottom = 1;
        _health.OffsetTop = -83;
        _health.OffsetBottom = -51;
        _health.AddThemeColorOverride("font_color",
            new Color(.89f,.93f,.88f));

        _ammo = MakeLabel(860,650,350,40,22);
        _ammo.Visible = false;
        _weaponDial = new WeaponDialHud();
        AddChild(_weaponDial);

        _credits = MakeLabel(30, 0, 390, 30, 15);
        _credits.AnchorTop = 1;
        _credits.AnchorBottom = 1;
        _credits.OffsetTop = -48;
        _credits.OffsetBottom = -19;
        _credits.AddThemeColorOverride("font_color",
            new Color(.73f,.75f,.70f));

        _banner = MakeLabel(0,72,750,64,30);
        _banner.AnchorLeft = .5f;
        _banner.AnchorRight = .5f;
        _banner.OffsetLeft = -380;
        _banner.OffsetRight = 380;
        _banner.HorizontalAlignment = HorizontalAlignment.Center;
        _banner.AddThemeColorOverride("font_color",
            new Color(.94f,.90f,.77f));

        _objective = MakeLabel(0,139,740,37,19);
        _objective.AnchorLeft = .5f;
        _objective.AnchorRight = .5f;
        _objective.OffsetLeft = -370;
        _objective.OffsetRight = 370;
        _objective.HorizontalAlignment = HorizontalAlignment.Center;
        _objective.AddThemeColorOverride("font_color",
            new Color(.86f,.88f,.84f));

        _utility = MakeLabel(0,183,680,29,16);
        _utility.AnchorLeft = .5f;
        _utility.AnchorRight = .5f;
        _utility.OffsetLeft = -340;
        _utility.OffsetRight = 340;
        _utility.HorizontalAlignment = HorizontalAlignment.Center;
        _utility.AddThemeColorOverride("font_color",
            new Color(.76f,.80f,.78f));

        _armorLabel = MakeLabel(30,0,94,22,14);
        _armorLabel.AnchorTop = 1;
        _armorLabel.AnchorBottom = 1;
        _armorLabel.OffsetTop = -114;
        _armorLabel.OffsetBottom = -92;
        _armorLabel.Text = "ARMOR";
        _armorLabel.AddThemeColorOverride("font_color",
            new Color(.68f,.75f,.85f));
        _armorRow = new Control
        {
            AnchorTop = 1,
            AnchorBottom = 1,
            OffsetLeft = 120,
            OffsetTop = -108,
            OffsetRight = 415,
            OffsetBottom = -98,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_armorRow);
        for(var i=0;i<4;i++)
        {
            var back = new ColorRect
            {
                OffsetLeft=i*73,OffsetTop=0,OffsetRight=i*73+68,OffsetBottom=10,
                Color=new Color(.09f,.10f,.12f,.81f),
                MouseFilter=Control.MouseFilterEnum.Ignore
            };
            _armorRow.AddChild(back);
            var fill = new ColorRect
            {
                OffsetLeft=0,OffsetTop=0,OffsetRight=68,OffsetBottom=10,
                Color=new Color(70f/255f,140f/255f,230f/255f),
                MouseFilter=Control.MouseFilterEnum.Ignore
            };
            back.AddChild(fill);
            _armorBars[i] = fill;
        }
        var healthTrack = new ColorRect
        {
            Name = "HealthTrack",
            AnchorTop = 1,
            AnchorBottom = 1,
            OffsetLeft = 30,
            OffsetRight = 413,
            OffsetTop = -18,
            OffsetBottom = -12,
            Color = new Color(.10f,.13f,.12f,.95f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(healthTrack);
        _healthBar = new ColorRect
        {
            Name = "HealthFill",
            OffsetRight = 383,
            OffsetBottom = 6,
            Color = new Color(.35f,.65f,.35f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        healthTrack.AddChild(_healthBar);

        // APPROXIMATED original injury overlays; perk effects are inherited.
        _lowHealth = new ColorRect
        {
            Color = new Color(.45f,0.0f,0.0f,0.0f),
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_lowHealth);
        _damageFlash = new ColorRect
        {
            Color = new Color(.72f,.03f,.02f,0.0f),
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_damageFlash);

        var crosshair = MakeLabel(0,0,34,34,24);
        crosshair.AnchorLeft = .5f;
        crosshair.AnchorRight = .5f;
        crosshair.AnchorTop = .5f;
        crosshair.AnchorBottom = .5f;
        crosshair.OffsetLeft = -17;
        crosshair.OffsetRight = 17;
        crosshair.OffsetTop = -17;
        crosshair.OffsetBottom = 17;
        crosshair.Text = "+";
        crosshair.HorizontalAlignment = HorizontalAlignment.Center;
        crosshair.AddThemeColorOverride("font_color",
            new Color(.84f,.87f,.84f,.85f));
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
        _top.Text = $"REGULAR     {match.MapName.ToUpperInvariant()}     WAVE {match.Wave}/15     {stage.ToUpperInvariant()} {FormatTime(seconds)}";
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

        _credits.Text = $"LVL {player.Level}    XP {player.Xp:N0}/{ProgressionRules.RequiredForNextLevel(player.Level):N0}    ${player.Credits:N0}";
        UpdateArmor(player);
        var hpFraction = player.MaxHealth > 0
            ? Math.Clamp(player.Health / player.MaxHealth, 0f, 1f)
            : 0f;
        _healthBar.OffsetRight = 383f * hpFraction;
        _healthBar.Color = hpFraction > .35f
            ? new Color(.33f,.65f,.35f) : new Color(.80f,.22f,.18f);
        if (throwableName is not null)
            _weaponDial.Display(throwableName,
                player.Inventory.GetValueOrDefault(throwableName), 0, 1,
                "THROWABLE", false, true);
        else
        {
            var definition = RuntimeWeaponCatalog.Get(weaponName);
            _weaponDial.Display(weaponName,
                player.Ammo.GetValueOrDefault(weaponName),
                player.ReserveAmmo.GetValueOrDefault(weaponName),
                definition.Magazine,
                definition.IsMelee ? "MELEE" :
                    definition.IsShotgun ? "SHOTGUN" :
                    definition.IsLauncher ? "LAUNCHER" :
                    definition.Slot == "Secondary" ? "SIDEARM" : "PRIMARY",
                definition.IsMelee, false);
        }

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
            _armorBars[i].OffsetRight=68f*fraction;
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
