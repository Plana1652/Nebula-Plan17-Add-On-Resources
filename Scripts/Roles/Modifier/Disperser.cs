using Nebula.Utilities;
using Plana.Core;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Modifier;

public class Disperser : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, ICodeName, HasRoleFilter, HasAssignmentRoutine, ISpawnable, IAssignToCategorizedRole,HasCitation
{
    private Disperser() : base("disperser", "dps", NebulaTeams.ImpostorTeam.Color, [DisperseOption],false,true,true)
    {
    }
    public static T Random<T>(IEnumerable<T> input)
    {
        IList<T> list = (input as IList<T>) ?? input.ToList<T>();
        if (list.Count != 0)
        {
            return list[global::UnityEngine.Random.Range(0, list.Count)];
        }
        return default(T);
    }
    public static Vector2 SendPlayerToVent(Vent vent)
    {
        Vector2 size = vent.GetComponent<BoxCollider2D>().size;
        Vector3 destination = vent.transform.position;
        destination.y += 0.3636f;
        return new Vector2(destination.x, destination.y);
    }
    static RemoteProcess StartDisperseRpc = new RemoteProcess("StartDisperseRpc", (_) =>
    {
        var player = GamePlayer.LocalPlayer;
        var vanillaplayer = PlayerControl.LocalPlayer;
        if (player == null || vanillaplayer == null)
        {
            return;
        }
        if (player.IsDead || player.IsDisconnected)
        {
            return;
        }
        AmongUsUtil.PlayFlash(MyRole.RoleColor.ToUnityColor());
        if (Minigame.Instance)
        {
            try
            {
                Minigame.Instance.Close();
            }
            catch
            {
            }
        }
        if (vanillaplayer.inVent)
        {
            vanillaplayer.MyPhysics.RpcExitVent(Vent.currentVent.Id);
            vanillaplayer.MyPhysics.ExitAllVents();
        }
        switch (DisperseOption.GetValue())
        {
            case 0:
                HashSet<Vent> vents = UnityEngine.Object.FindObjectsOfType<Vent>().ToHashSet<Vent>();
                vanillaplayer.NetTransform.RpcSnapTo(SendPlayerToVent(Random(vents)));
                break;
            case 1:
                byte mapId = AmongUsUtil.CurrentMapId;
                NebulaPreSpawnLocation[] cand = NebulaPreSpawnLocation.Locations[(int)mapId];
                if (cand.Length == 0)
                {
                    cand = NebulaPreSpawnLocation.Locations[(int)mapId].Where((NebulaPreSpawnLocation l) => l.VanillaIndex != null).ToArray<NebulaPreSpawnLocation>();
                }
                var list = new List<Vector2>();
                cand.Do(p => list.Add(p.Position!.Value));
                vanillaplayer.NetTransform.RpcSnapTo(Random(list));
                break;
        }
        if (vanillaplayer.walkingToVent)
        {
            vanillaplayer.inVent = false;
            Vent.currentVent = null;
            vanillaplayer.moveable = true;
            vanillaplayer.MyPhysics.StopAllCoroutines();
        }
    });
    Citation HasCitation.Citation => PCitations.TownOfUs;
    static ValueConfiguration<int> DisperseOption = NebulaAPI.Configurations.Configuration("options.role.disperser.DisperseType", new string[] { "options.role.disperser.vent", "options.role.disperser.spawnpos"},0);
    static public Disperser MyRole = new Disperser();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        Virial.Media.Image disperse = NebulaAPI.AddonAsset.GetResource("disperse.png")!.AsImage(115f)!;
        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                bool useskill = false;
                var button=NebulaAPI.Modules.AbilityButton(this, base.MyPlayer,false,true, VirtualKeyInput.SidekickAction, null, 0f, "disperse", disperse, (ModAbilityButton _) => MyPlayer.CanMove, (ModAbilityButton _) => !MyPlayer.IsDead&&!useskill, false);
                button.OnClick = button =>
                {
                    useskill = true;
                    StartDisperseRpc.Invoke();
                };
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " Ⅹ".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}
