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


public static class SpotifyMuter
{
    private static float previousVolume = -1f;
    private static bool isMuted = false;

    public static void Toggle()
    {
        var spotifyProcess = Process.GetProcessesByName("Spotify").FirstOrDefault();
        if (spotifyProcess == null) { 
            var logSource = Logger.CreateLogSource("HitstopMusicSync");
            logSource.LogError("Spotify not found! Is it open?");
            return;
        }

        var enumerator = new MMDeviceEnumerator();
        var sessions = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                                 .AudioSessionManager.Sessions;

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            if (session.GetProcessID == spotifyProcess.Id)
            {
                if (!isMuted)
                {
                    previousVolume = session.SimpleAudioVolume.Volume;
                    session.SimpleAudioVolume.Volume = 0f;
                    isMuted = true;
                }
                else
                {
                    session.SimpleAudioVolume.Volume = previousVolume;
                    isMuted = false;
                }
                break;
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
