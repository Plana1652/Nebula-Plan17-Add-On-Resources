using Il2CppInterop.Runtime.InteropTypes;
using Nebula.Roles;
using Nebula.Roles.Assignment;
using Plana.Roles.Crewmate;
using Plana.Roles.Modifier;

namespace Plana.Core;

public static class APICompat
{
    static public UnityEngine.Color ToUnityColor(this Virial.Color color) => new UnityEngine.Color(color.R, color.G, color.B, color.A);
    static public Virial.Color ToNebulaColor(this UnityEngine.Color color) => new Virial.Color(color.r, color.g, color.b, color.a);
    static public UnityEngine.Vector2 ToUnityVector(this Virial.Compat.Vector2 v) => new UnityEngine.Vector2(v.x, v.y);
    static public UnityEngine.Vector3 ToUnityVector(this Virial.Compat.Vector3 v) => new UnityEngine.Vector3(v.x, v.y,v.z);
    static public Virial.Compat.Vector2 ToNebulaVector(this UnityEngine.Vector2 v) => new Virial.Compat.Vector2(v.x, v.y);
    static public Virial.Compat.Vector3 ToNebulaVector(this UnityEngine.Vector3 v) => new Virial.Compat.Vector3(v.x, v.y,v.z);
    static public GamePlayer ToNebulaPlayer(this PlayerControl player) => GamePlayer.GetPlayer(player.PlayerId);
    static public PlayerControl ToAUPlayer(this GamePlayer player) => player.VanillaPlayer;
    public static IEnumerable<(byte playerId, List<DefinedModifier> role)> GetPlayersModifier(this IRoleTable table)
    {
        var grouped = (table as RoleTable).modifiers
    .GroupBy(x => x.Item3)
    .ToDictionary(g => g.Key, g => g.Select(x => x.Item1).ToList());

        return PlayerControl.AllPlayerControls.GetFastEnumerator().Select(p=>p.PlayerId)
            .Select(playerId => ValueTuple.Create(
                playerId,
                grouped.ContainsKey(playerId) ? grouped[playerId] : new List<DefinedModifier>()
            ))
            .ToList();
    }
    public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
    {
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }
    public static void AddValueV2(this Dictionary<byte, int> self, byte target, int num)
    {
        if (self.TryGetValue(target, out var last))
            self[target] = last + num;
        else
            self[target] = num;
    }
    public static KeyValuePair<byte, int> MaxPairV2(this Dictionary<byte, int> self, out bool tie)
    {
        tie = true;
        KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(PlayerVoteArea.SkippedVote, 0);
        foreach (KeyValuePair<byte, int> keyValuePair in self)
        {
            if (keyValuePair.Value > result.Value)
            {
                result = keyValuePair;
                tie = false;
            }
            else if (keyValuePair.Value == result.Value)
            {
                tie = true;
            }
        }
        return result;
    }
    /*static public AllocationParameters MoriartizedParamaters(this DefinedRole role)
    {
        if (PatchManager.MoriartizedRoleDic.TryGetValue(role.Id, out var p))
        {
            return p;
        }
        return null;

    }
    static public AllocationParameters YandereRoleParamaters(this DefinedRole role)
    {
        if (PatchManager.YandereRoleDic.TryGetValue(role.Id, out var p))
        {
            return p;
        }
        return null;
    }*/
    static public bool IsModMadmate(this GamePlayer player)
    {
        if (player.IsMadmate)
        {
            return true;
        }
        if (player.TryGetModifier<Infected.Instance>(out var i))
        {
            return true;
        }
        if (player.TryGetModifier<InsomniacsModifier.Instance>(out var m)||player.TryGetModifier<MadmatePLUS.Instance>(out var mp))
        {
            return true;
        }
        return false;
    }
    public static UnityEngine.Color RGBMultiplied(this UnityEngine.Color color,float multiplier)
    {
        return new UnityEngine.Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
    }
    public static UnityEngine.Color RGBMultiplied(this UnityEngine.Color color, UnityEngine.Color multiplier)
    {
        return new UnityEngine.Color(color.r * multiplier.r, color.g * multiplier.g, color.b * multiplier.b, color.a);
    }
    static public UnityEngine.Color AlphaMultiplied(this UnityEngine.Color color,float multiplier)
    {
        return new UnityEngine.Color(color.r,color.g,color.b,color.a * multiplier);
    }
    static public Virial.Color AlphaMultiplied(this Virial.Color color, float multiplier)
    {
        return new Virial.Color(color.R, color.G, color.B, color.A * multiplier);
    }
    static public FieldInfo GetPrivateFieldInfo(this object instance,string fieldname)
    {
        return instance.GetType().GetField(fieldname, BindingFlags.Instance | BindingFlags.NonPublic);
    }
    static public T GetPrivateField<T>(this object instance, string fieldname)
    {
        return (T)(object)instance.GetPrivateFieldInfo(fieldname).GetValue(instance);
    }
    static public void SetPrivateField(this object instance, string fieldname,object value)
    {
        instance.GetPrivateFieldInfo(fieldname).SetValue(instance,value);
    }
    static public MethodInfo GetPrivateMethodInfo(this object instance, string method)
    {
        if (instance is Type)
        {
            return (instance as Type)!.GetPrivateMethodInfoType(method);
        }
        return instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
    }
    static public MethodInfo GetPrivateMethodInfoType(this Type type, string method)
    {
        return type.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
    }
    static public MethodInfo GetPrivateStaticMethodInfo(this object instance, string method)
    {
        return instance.GetType().GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
    }
    static public MethodInfo GetPrivateStaticMethodInfoType(this Type type, string method)
    {
        return type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
    }
    static public T CallPrivateMethod<T>(this object instance, string method,params object[] param)
    {
        return (T)(object)instance.GetPrivateMethodInfo(method).Invoke(instance,param);
    }
    static public T CallPrivateStaticMethod<T>(this object instance, string method,params object[] param)
    {
        return (T)(object)instance.GetPrivateStaticMethodInfo(method).Invoke(instance, param);
    }
    static public Type GetPrivateChildType(this Type t,string name)
    {
        return t.GetNestedType(name, BindingFlags.NonPublic);
    }
}