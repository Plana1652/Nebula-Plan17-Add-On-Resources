using System;
using System.IO;
using BepInEx;
using Nebula;
using UnityEngine;

namespace Plana.Core;

public class PDebug
{
    public static void Log(object message)
    {
        string m = $"{DateTime.Now:[yyyy/MM/dd HH:mm:ss]} " + message.ToString() + Environment.NewLine;
        if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/LOG"))
        {
            File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/LOG/log.txt", m);
        }
        File.AppendAllText(new DirectoryInfo(Application.dataPath).Parent+ "/MoreRolesLog.txt", m);
    }
}
