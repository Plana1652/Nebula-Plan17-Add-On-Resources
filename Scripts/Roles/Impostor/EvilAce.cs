using Plana.Core;
using Plana.Roles.Crewmate;

namespace Plana.Roles.Impostor;

public class EvilAce : DefinedRoleTemplate, DefinedRole, HasCitation
{
    private EvilAce() : base("evilAce", NebulaTeams.ImpostorTeam.Color,RoleCategory.ImpostorRole,NebulaTeams.ImpostorTeam, [KillCoolDown,MurderSubCoolDown,MinCoolDown])
    {
    }
    static private FloatConfiguration KillCoolDown = NebulaAPI.Configurations.Configuration("options.role.evilAce.killcooldown", (0f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration MurderSubCoolDown = NebulaAPI.Configurations.Configuration("options.role.evilAce.murdersub", (0f, 60f, 2.5f), 2.5f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration MinCoolDown = NebulaAPI.Configurations.Configuration("options.role.evilAce.mincd", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public EvilAce MyRole = new EvilAce();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        ModAbilityButton? killButton;
        List<byte> killplayers = new List<byte>();
        bool RuntimeRole.HasVanillaKillButton => false;
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (killplayers!=null&&killplayers.Count>=5&&ev.EndState.Winners.Test(MyPlayer))
            {
                new StaticAchievementToken("evilAce.challenge1");
            }
        }
        [Local]
        void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (killplayers==null)
            {
                killplayers = new List<byte>();
            }
            if (killplayers.Contains(ev.Player.PlayerId))
            {
                ev.Append(ev.Player.Role.DisplayColoredName);
            }
        }
        [Local,OnlyMyPlayer]
        void OnKill(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead.PlayerState==PlayerState.Guessed&&!killplayers.Contains(ev.Dead.PlayerId))
            {
                killplayers.Add(ev.Dead!.PlayerId);
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                killplayers = new List<byte>();
                var myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer,ObjectTrackers.LocalKillablePredicate ,Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null,KillCoolDown, "kill", null, (ModAbilityButton _) => myTracker.CurrentTarget != null, (ModAbilityButton _) => base.MyPlayer.AllowToShowKillButtonByAbilities, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.OnClick = (button) =>
                {
                    var player=myTracker.CurrentTarget;
                    MyPlayer.MurderPlayer(player!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill,result=> { 
                        if (result == KillResult.Kill)
                        {
                            if (killplayers != null)
                            {
                                killplayers.Add(player!.PlayerId);
                                if (killplayers.Count>=5)
                                {
                                    new StaticAchievementToken("evilAce.challenge2");
                                }
                            }
                            button.CoolDownTimer = NebulaAPI.Modules.Timer(this, Mathf.Max(MinCoolDown, KillCoolDown - MurderSubCoolDown * killplayers!.Count)).SetAsKillCoolTimer().Start();
                            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                        }
                        });
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                GameOperatorManager.Instance.Subscribe<PlayerMurderedEvent>(ev =>
                {
                    if (ev.Murderer.AmOwner && ev.Dead.PlayerState == PlayerState.Guessed && killplayers.Count >= 3)
                    {
                        new StaticAchievementToken("evilAce.common1");
                    }
                }, this);
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
    }
}
