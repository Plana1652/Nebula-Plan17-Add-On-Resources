using Nebula.Roles.Complex;
using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Game;
using Vector2 = UnityEngine.Vector2;

namespace Plana.Core;


public static class TrapperSystem
{
    private static SpriteLoader?[] buttonSprites = [
        NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.AccelTrapButton.png",115f),
        NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.DecelTrapButton.png",115f),
        NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.CommTrapButton.png",115f),
        NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.KillTrapButton.png",115f),
        null
    ];

    private const int AccelTrapId = 0;
    private const int DecelTrapId = 1;
    private const int CommTrapId = 2;
    private const int KillTrapId = 3;
    public static void OnActivated(IUsurpableAbility myRole, bool isEvil, (int id,int cost)[] buttonVariation, List<Trapper.Trap> localTraps,List<Trapper.Trap>? specialTraps, int leftCost)
    {
        Vector2? pos = null;
        int buttonIndex = 0;
        var placeButton = NebulaAPI.Modules.EffectButton(myRole, myRole.MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "trapper.place",
            Trapper.PlaceCoolDownOption, Trapper.PlaceDurationOption, "place", buttonSprites[buttonVariation[0].id]!, 
            _ => leftCost >= buttonVariation[buttonIndex].cost, _ => leftCost > 0).SetAsUsurpableButton(myRole);
        placeButton.BindSubKey(Virial.Compat.VirtualKeyInput.AidAction, "trapper.switch", true);
        int iconVariation = isEvil ? 0 : 3;
        placeButton.ShowUsesIcon(iconVariation, leftCost.ToString());
        placeButton.OnEffectStart = (button) =>
        {
            float duration = Trapper.PlaceDurationOption;
            NebulaAsset.PlaySE(duration < 3f ? NebulaAudioClip.Trapper2s : NebulaAudioClip.Trapper3s);

            pos = (Vector2)myRole.MyPlayer.TruePosition + new Vector2(0f, 0.085f);
            myRole.MyPlayer.GainSpeedAttribute(0f, duration, false, 10);
        };
        placeButton.OnEffectEnd = (button) => 
        {
            NebulaGameManager.Instance?.RpcDoGameAction(myRole.MyPlayer, myRole.MyPlayer.Position, myRole.MyPlayer.IsImpostor ? GameActionTypes.EvilTrapPlacementAction : GameActionTypes.NiceTrapPlacementAction);

            placeButton.StartCoolDown();
            var lTrap = Trapper.Trap.GenerateTrap(buttonVariation[buttonIndex].id, pos!.Value);
            if (!PatchManager.PlaceTrapNoMeetingStart)
            {
                localTraps.Add(lTrap);
            }
            else
            {
                var Trap=NebulaSyncObject.RpcInstantiate(Trapper.Trap.MyGlobalTag, [lTrap.TypeId, lTrap.Position.x, lTrap.Position.y])?.SyncObject as Trapper.Trap;
                Trap?.SetAsOwner();
                NebulaSyncObject.LocalDestroy(lTrap.ObjectId);
                if (Trap?.TypeId is CommTrapId or KillTrapId)
                {
                    specialTraps?.Add(Trap!);
                }
            }
            leftCost -= buttonVariation[buttonIndex].cost;
            button.UpdateUsesIcon(leftCost.ToString());

            if (Trapper.KillTrapSoundDistanceOption > 0f)
            {
                if (buttonVariation[buttonIndex].id == KillTrapId) NebulaAsset.RpcPlaySE.Invoke((NebulaAudioClip.TrapperKillTrap, PlayerControl.LocalPlayer.transform.position, Trapper.KillTrapSoundDistanceOption * 0.6f, Trapper.KillTrapSoundDistanceOption));
            }

            if (isEvil && buttonVariation[buttonIndex].id == DecelTrapId)
                new StaticAchievementToken("evilTrapper.common1");
            if (!isEvil && buttonVariation[buttonIndex].id == AccelTrapId)
                new StaticAchievementToken("niceTrapper.common1");

        };
        placeButton.OnSubAction = (button) =>
        {
            if (button.IsInEffect) return;
            buttonIndex = (buttonIndex + 1) % buttonVariation.Length;
            placeButton.SetImage(buttonSprites[buttonVariation[buttonIndex].id]!);
        };
    }

    public static void OnMeetingStart(List<Trapper.Trap> localTraps, List<Trapper.Trap>? specialTraps)
    {
        foreach (var lTrap in localTraps)
        {
            var gTrap = NebulaSyncObject.RpcInstantiate(Trapper.Trap.MyGlobalTag, [lTrap.TypeId, lTrap.Position.x, lTrap.Position.y])?.SyncObject as Trapper.Trap;
            gTrap?.SetAsOwner();
            NebulaSyncObject.LocalDestroy(lTrap.ObjectId);
            if (gTrap?.TypeId is KillTrapId or CommTrapId) specialTraps?.Add(gTrap!);
        }
        localTraps.Clear();
    }

    public static void OnInactivated(List<Trapper.Trap> localTraps, List<Trapper.Trap>? specialTraps)
    {
        foreach (var lTrap in localTraps) NebulaSyncObject.LocalDestroy(lTrap.ObjectId);
        foreach (var sTrap in specialTraps ?? []) NebulaSyncObject.LocalDestroy(sTrap.ObjectId);
        localTraps.Clear();
        specialTraps?.Clear();
    }
}
public class NebulaResourceTextureLoader : ITextureLoader, IManageableAsset
{
    public static SpriteLoader FromResource(string address, float pixels)
    {
        return new SpriteLoader(new NebulaResourceTextureLoader(address), pixels);
    }
    public NebulaResourceTextureLoader(string address)
    {
        this.address = address;
    }
    public static Texture2D LoadTextureFromResources(string path)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        Stream stream = typeof(NebulaManager).Assembly.GetManifestResourceStream(path);
        if (stream == null)
        {
            return null;
        }
        byte[] byteTexture = new byte[stream.Length];
        stream.Read(byteTexture, 0, (int)stream.Length);
        GraphicsHelper.LoadImage(texture, byteTexture, true);
        return texture;
    }
    public Texture2D GetTexture()
    {
        if (!this.texture)
        {
            this.texture = LoadTextureFromResources(this.address);
        }
        return this.texture;
    }

    public System.Collections.IEnumerator LoadAsset()
    {
        if (!this.texture)
        {
            this.texture = GetTexture();
            yield break;
        }
        UnityEngine.Object.Destroy(texture);
        yield break;
    }

    public void UnloadAsset()
    {
        if (this.texture)
        {
            UnityEngine.Object.Destroy(this.texture);
        }
    }

    public void MarkAsUnloadAsset()
    {
    }
    private string address;
    private Texture2D texture;
}