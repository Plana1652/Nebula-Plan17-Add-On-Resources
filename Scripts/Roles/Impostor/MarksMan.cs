using Plana.Core;
using Plana.Roles.Crewmate;
using Virial.Helpers;

namespace Plana.Roles.Impostor;

public class MarksMan : DefinedSingleAbilityRoleTemplate<MarksMan.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{
    private MarksMan() : base("marksMan", NebulaTeams.ImpostorTeam.Color,RoleCategory.ImpostorRole,NebulaTeams.ImpostorTeam, [KillCooldown,MaxBullet,UseButtetResetCooldown,LoadButtetCooldown])
    {
    }
    static FloatConfiguration KillCooldown = NebulaAPI.Configurations.Configuration("options.role.marksman.killcooldown", (0f, 60f, 2.5f), 27.5f, FloatConfigurationDecorator.Second);
    static IntegerConfiguration MaxBullet = NebulaAPI.Configurations.Configuration("options.role.marksman.maxbullet", new IntegerSelection([1,2,3,4,5,6,7,8,9,10,15,20,99]),3);
    static FloatConfiguration UseButtetResetCooldown = NebulaAPI.Configurations.Configuration("options.role.marksman.resetcooldown", (0f, 60f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    static FloatConfiguration LoadButtetCooldown = NebulaAPI.Configurations.Configuration("options.role.marksman.loadbulletcooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    Citation? HasCitation.Citation { get { return Citations.SuperNewRoles; } }

    static public MarksMan MyRole = new MarksMan();
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.KillersSide;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new(player, arguments.GetAsBool(0),arguments.Get(1,0));
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        public static Virial.Media.Image unloadBulletImage=NebulaAPI.AddonAsset.GetResource("marksUnloadBullet.png")!.AsImage(115f)!, loadBulletImage = NebulaAPI.AddonAsset.GetResource("marksLoadBullet.png.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(),bullet];
        public Ability(GamePlayer player,bool isUsurped,int bullets) : base(player,isUsurped)
        {
            if (AmOwner)
            {
                bullet = bullets;
                var myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.LocalKillablePredicate, Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null, KillCooldown, "kill", null, (ModAbilityButton _) => myTracker.CurrentTarget != null, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.OnClick = (button) =>
                {
                    var player = myTracker.CurrentTarget;
                    MyPlayer.MurderPlayer(player!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.ShowUsesIcon(0, $"{bullet}/{((int)MaxBullet)}");
                unloadbulletButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, false, false, VirtualKeyInput.SecondaryAbility, null, 0f, "marksman.unloadbullet",unloadBulletImage, (ModAbilityButton _) => bullet<MaxBullet&& !killButton.IsInCooldown, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor).SetAsUsurpableButton(this);
                unloadbulletButton.OnClick = (button) =>
                {
                    bullet++;
                    bullet = Mathf.Clamp(bullet, 0, MaxBullet);
                    killButton.UpdateUsesIcon($"{bullet}/{((int)MaxBullet)}");
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                loadbulletButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, false, false, VirtualKeyInput.Ability, null, LoadButtetCooldown, "marksman.loadbullet",loadBulletImage, (ModAbilityButton _) => bullet > 0 && killButton.IsInCooldown, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor).SetAsUsurpableButton(this);
                loadbulletButton.OnClick = (button) =>
                {
                    bullet--;
                    bullet = Mathf.Clamp(bullet, 0, MaxBullet);
                    killButton.UpdateUsesIcon($"{bullet}/{((int)MaxBullet)}");
                    killButton.CoolDownTimer.Start(UseButtetResetCooldown);
                };
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
        ModAbilityButton? killButton, unloadbulletButton, loadbulletButton;
        bool IPlayerAbility.HideKillButton => true;
        int bullet;
    }
}
