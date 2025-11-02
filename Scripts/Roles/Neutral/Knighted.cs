
using Nebula.VoiceChat;
using Plana.Roles.Crewmate;
using Plana.Roles.Neutral;

namespace Nebula.Roles.Crewmate;

[NebulaRPCHolder]
public class Knighted : DefinedRoleTemplate, DefinedRole, HasCitation
{
    public static Team MyTeam = new Team("teams.knighted", new Virial.Color(255, 178, 70), TeamRevealType.OnlyMe);
private Knighted(): base("knighted",new(255,178,70), RoleCategory.NeutralRole,MyTeam, [NumOfKILLOption, KILLCooldownOption,CanKillTarget,TargetCanKillKnighted])
{
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    bool DefinedRole.IsKiller => true;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static IntegerConfiguration NumOfKILLOption = NebulaAPI.Configurations.Configuration("options.role.knighted.op1", new ValueTuple<int, int>(1, 15), 1, null, null, null);
    public static FloatConfiguration KILLCooldownOption = NebulaAPI.Configurations.Configuration("options.role.knighted.op2", new ValueTuple<float,float,float>(10f,45f,2.5f),25f, FloatConfigurationDecorator.Second, null, null);
    public static BoolConfiguration CanKillTarget = NebulaAPI.Configurations.Configuration("options.role.knighted.op3", false);
    public static BoolConfiguration TargetCanKillKnighted = NebulaAPI.Configurations.Configuration("options.role.knighted.op4", false);
    static public Knighted MyRole = new Knighted();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        ModAbilityButton killButton, selectbutton;
        public Instance(GamePlayer player) : base(player)
        {
        }
        GamePlayer secondSetColorPlayer;
        [Local]
        private void DecorateKnightedTargetColor(PlayerDecorateNameEvent ev)
        {
            try
            {
                if (AmOwner)
                {
                    if (ev.Player != null && selectPlayer != null && selectPlayer.PlayerId == ev.Player.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                        if (ev.Player.Modifiers.Any((RuntimeModifier m) => m is Lover.Instance))
                        {
                            Lover.Instance lover = ev.Player.Modifiers.FirstOrDefault((RuntimeModifier mod) => mod is Lover.Instance) as Lover.Instance;
                            secondSetColorPlayer = lover.MyLover.Get();
                        }
                    }
                    if (ev.Player != null && ev.Player.PlayerId == MyPlayer.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                    }
                    if (secondSetColorPlayer != null && ev.Player.PlayerId == secondSetColorPlayer.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        static private Virial.Media.Image selectImage = NebulaAPI.AddonAsset.GetResource("Select.png").AsImage(115f);
        [OnlyMyPlayer]
        void OnCheckExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (selectPlayer == null)
            {
                return;
            }
            if (ev.WinnersMask.Test(selectPlayer) && !MyPlayer.TryGetModifier<SkinnerDog.Instance>(out var m))
            {
                ev.SetWin(true);
                ev.ExtraWinMask.Add(PatchManager.knightedExtra);
            }
        }
        private void OnGameEnd(GameEndEvent ev)
        {
            if (selectPlayer == null)
            {
                return;
            }
            if (ev.EndState.Winners.Test(selectPlayer))
            {
                if (MyPlayer.AmOwner)
                {
                    if (selectPlayer.IsImpostor)
                    {
                        new StaticAchievementToken("knighted.common2");
                    }
                    if (selectPlayer.TryGetModifier<Lover.Instance>(out var lover))
                    {
                        new StaticAchievementToken("knighted.common3");
                    }
                    if (selectPlayerEarlyDead&&!MyPlayer.IsDead)
                    {
                        new StaticAchievementToken("knighted.common4");
                    }
                    if (killSelectPlayer)
                    {
                        new StaticAchievementToken("knighted.another1");
                    }
                    if (selectPlayer.Role.Role.Category == RoleCategory.NeutralRole)
                    {
                        new StaticAchievementToken("knighted.challenge1");
                    }
                }
            }
        }
        int meetingnum;
        bool selectPlayerEarlyDead;
        bool killSelectPlayer;
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            meetingnum++;
            if (meetingnum<=2&&selectPlayer!=null&&selectPlayer.IsDead)
            {
                selectPlayerEarlyDead = true;
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                selectPlayer = null;
                if (NebulaGameManager.Instance!=null)
                {
                    var p = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.TryGetModifier<KnightedTarget.Instance>(out var m));
                    if (p!=null)
                    {
                        selectPlayer = p;
                    }
                }
                int leftKill = NumOfKILLOption;
                SpriteRenderer lockSprite = null;
                ObjectTracker<GamePlayer> killTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(base.MyPlayer), null, false, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, false, true, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Kill, null);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && selectPlayer != null &&(CanKillTarget||killTracker.CurrentTarget != selectPlayer) && this.MyPlayer.CanMove && lockSprite == null;
                killButton.Visibility = (button) => !base.MyPlayer.IsDead && leftKill > 0;
                killButton.ShowUsesIcon(2, leftKill.ToString());
                try
                {
                    if (selectPlayer == null)
                    {
                        lockSprite = (killButton as ModAbilityButtonImpl).VanillaButton.AddLockedOverlay();
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log("SetLockError : " + e.ToString());
                }
                this.killButton.OnClick = delegate (ModAbilityButton button)
                {
                    if (selectPlayer != null && killTracker.CurrentTarget.PlayerId == selectPlayer.PlayerId)
                    {
                        killSelectPlayer = true;
                    }
                    leftKill--;
                    MyPlayer.MurderPlayer(killTracker.CurrentTarget, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill, (result) =>
                    {
                        if (result == KillResult.Kill)
                        {
                            killButton.UpdateUsesIcon(leftKill.ToString());
                        }
                        else
                        {
                            leftKill++;
                        }
                    });
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KILLCooldownOption).SetAsKillCoolTimer().Start(null);
                killButton.StartCoolDown();
                killButton.SetLabel("kill");
                selectbutton = NebulaAPI.Modules.AbilityButton(this,false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability, null);
                selectbutton.Availability = (button) => killTracker.CurrentTarget != null && this.MyPlayer.CanMove;
                selectbutton.Visibility = (button) => !base.MyPlayer.IsDead && selectPlayer==null;
                (selectbutton as Virial.Components.ModAbilityButton).SetImage(selectImage);
                selectbutton.OnClick = delegate (ModAbilityButton button)
                {
                    new StaticAchievementToken("knighted.common1");
                    SetPlayer.Invoke(killTracker.CurrentTarget);
                    selectPlayer.AddModifier(KnightedTarget.MyRole, null);
                    if (lockSprite)
                    {
                        UnityEngine.Object.Destroy(lockSprite);
                        lockSprite = null;
                    }
                };
                selectbutton.SetLabel("knightedSelect");
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
        public static GamePlayer selectPlayer;
        static RemoteProcess<GamePlayer> SetPlayer = new RemoteProcess<Virial.Game.Player>("KnightedSetPlayer", delegate (GamePlayer
            message, bool _)
        {
            selectPlayer = message;
        });
        private void OnGameStarted(GameStartEvent ev)
        {
            selectPlayer = null;
        }
    }
}
public class KnightedTarget : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>,HasCitation
{
    private KnightedTarget() : base("knightedtarget",  new(255, 178, 70),null,true,()=>false)
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public KnightedTarget MyRole = new KnightedTarget();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (base.AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor.ToUnityColor(), 0f, 0.25f, 0.4f, 0f);
            }
        }
        [OnlyMyPlayer]
        private void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (!Knighted.TargetCanKillKnighted&&ev.Target.Role is Knighted.Instance)
            {
                ev.SetAsCannotKillBasically();
            }
        }

        [Local]
        private void DecorateKnightedTargetColor(PlayerDecorateNameEvent ev)
        {
            if (AmOwner)
            {
                if (ev.Player != null && ev.Player.Role is Knighted.Instance)
                {
                    ev.Color = new Virial.Color?(MyRole.RoleColor);
                }
                if (ev.Player != null && ev.Player.PlayerId == MyPlayer.PlayerId)
                {
                    ev.Color = new Virial.Color?(MyRole.RoleColor);
                }
            }
        }
        [OnlyMyPlayer]
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " ¡".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}