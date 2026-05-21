using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.Linq;

[BepInPlugin(
    "com.stereotypicaldweeb.hitstopmusicsync",
    "Hitstop Music Sync",
    "1.0.0"
)]
public class hitstopmusicsync : BaseUnityPlugin
{
    private Harmony harmony;

    private void Awake()
    {
        harmony = new Harmony("com.stereotypicaldweeb.hitstopmusicsync");
        harmony.PatchAll();
    }
}


public static class BrowserMuter
{
    private static float previousVolume = -1f;
    private static bool isMuted = false;

    // CHANGE THIS if you use a different browser: "msedge", "firefox", "opera"
    private static readonly string TargetBrowser = "firefox"; 

    public static void Toggle()
    {
        // 1. Find all running instances of your browser
        var browserProcesses = Process.GetProcessesByName(TargetBrowser);
        if (browserProcesses.Length == 0) { 
            var logSource = Logger.CreateLogSource("HitstopMusicSync");
            logSource.LogError($"{TargetBrowser} not found! Is it open?");
            return;
        }

        // Create a list of all process IDs for the browser
        var browserIds = browserProcesses.Select(p => p.Id).ToList();

        // 2. Access the Windows Audio Mixer
        var enumerator = new MMDeviceEnumerator();
        var sessions = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                                 .AudioSessionManager.Sessions;

        // 3. Loop through Windows audio sessions
        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            
            // Check if this audio session belongs to ANY of our browser processes
            if (browserIds.Contains((int)session.GetProcessID))
            {
                if (!isMuted)
                {
                    previousVolume = session.SimpleAudioVolume.Volume;
                    session.SimpleAudioVolume.Volume = 0f;
                    isMuted = true;
                }
                else
                {
                    // Restore the browser volume
                    session.SimpleAudioVolume.Volume = previousVolume;
                    isMuted = false;
                }
                // We don't break; here because browsers often run multiple audio sessions
            }
        }
    }
}
public static class MusicState
{
    public static bool PausedByMod = false;
}

[HarmonyPatch(typeof(TimeController), "TrueStop")]
public static class HitstopStartPatch
{
    [HarmonyPrefix]
    private static void Prefix(float length)
    {

        if (!MusicState.PausedByMod)
        {
            SpotifyMuter.Toggle();
            MusicState.PausedByMod = true;
        }
    }
}


[HarmonyPatch(typeof(TimeController), "ContinueTime")]
public static class HitstopEndPatch
{
    [HarmonyPostfix]
    private static void Postfix(float length, bool trueStop)
    {
        if (MusicState.PausedByMod)
        {
            SpotifyMuter.Toggle();
            MusicState.PausedByMod = false;
        }
    }
}
