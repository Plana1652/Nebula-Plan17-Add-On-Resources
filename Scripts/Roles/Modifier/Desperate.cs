using Plana.Core;
using Plana.Roles.Crewmate;
using UnityEngine.UIElements;

namespace Plana.Roles.Modifier;

public class Desperate : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private Desperate() : base("desperate", NebulaTeams.ImpostorTeam.Color, [NoActiveMeeting,SuicideTime],true,()=>PatchManager.DesperateAssignToImp)
    {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder;
        if (configurationHolder == null)
        {
            return;
        }
        configurationHolder.ScheduleAddRelated(() => new IConfigurationHolder[] { GeneralConfigurations.AssignmentOptions });
    }
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    static IntegerConfiguration NoActiveMeeting = NebulaAPI.Configurations.Configuration("options.role.desperate.meeting", (1, 15), 2);
    static FloatConfiguration SuicideTime = NebulaAPI.Configurations.Configuration("options.role.desperate.suicideTime", (0f,120f,2.5f),60f,FloatConfigurationDecorator.Second);
    static public Desperate MyRole = new Desperate();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        int ActiveMeeting;
        float time;
        int killnum;
        TextMeshPro tmp;
        [OnlyMyPlayer]
        [Local]
        private void OnKillPlayer(PlayerKillPlayerEvent ev)
        {
            time = 0f;
            killnum++;
            if (killnum>=2)
            {
                MyPlayer.RemoveModifier(Desperate.MyRole);
            }
        }
            void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                killnum=0;
                ActiveMeeting = 0;
                GameObject TextHolder = UnityHelper.CreateObject("SuicideTime", DestroyableSingleton<HudManager>.Instance.KillButton.transform.parent, UnityEngine.Vector3.zero, null);
                this.BindGameObject(TextHolder.gameObject);
                TextMeshPro tmPro = null;
                Size size;
                new NoSGUIText(GUIAlignment.Bottom, new TextAttribute(NebulaGUIWidgetEngine.API.GetAttribute(AttributeParams.StandardBaredBoldLeftNonFlexible))
                {
                    Alignment = Virial.Text.TextAlignment.Top,
                    FontSize = new FontSize(1.6f, true),
                    Size = new Size(3f, 1f)
                }, new RawTextComponent(""))
                {
                    PostBuilder = delegate (TextMeshPro t)
                    {
                        tmPro = t;
                        tmPro.sortingOrder = 0;
                    }
                }.Instantiate(new Anchor(new global::Virial.Compat.Vector2(0f, 0f), new global::Virial.Compat.Vector3(-0.5f, -0.5f, 0f)), new Size(20f, 20f), out size).transform.SetParent(TextHolder.transform, false);
                GameOperatorManager instance = GameOperatorManager.Instance;
                if (instance != null)
                {
                    instance.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                    {
                        if (tmPro)
                        {
                            if (!MeetingHud.Instance&&!ExileController.Instance&&MyPlayer.TryGetModifier<Desperate.Instance>(out var m))
                            {
                                tmPro.gameObject.SetActive(true);
                                time += Time.deltaTime;
                                tmPro.text = Language.Translate("role.desperate.suicideTimeText") +":"+ Mathf.FloorToInt(SuicideTime-time);
                                if (time>=SuicideTime)
                                {
                                    MyPlayer.Suicide(PatchManager.DesperateDead, null, KillParameter.NormalKill);
                                }
                                tmPro.transform.localPosition = new UnityEngine.Vector3(-0.07f, -2.34f, 0f);
                            }
                            else
                            {
                                tmPro.gameObject.SetActive(false);
                            }
                        }
                    }, this, 100);
                    tmp = tmPro;
                }
            }
        }
        void RuntimeAssignable.OnInactivated()
        {
            if (tmp)
            {
                UnityEngine.Object.Destroy(tmp);
            }
        }
        string RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort)
        {
            return Language.Translate("role.desperate.prefix").Color(MyRole.RoleColor.ToUnityColor()) + lastRoleName;
        }
        void OnMeetingStart(MeetingPreStartEvent ev)
        {
            if (AmOwner)
            {
                time = 0f;
                ActiveMeeting++;
                if (ActiveMeeting >= NoActiveMeeting)
                {
                    MyPlayer.RemoveModifier(Desperate.MyRole);
                }
            }
        }
        void OnTaskStart(TaskPhaseStartEvent ev)
        {
            if (AmOwner)
            {
                time = 0f;
            }
        }
    }
}
