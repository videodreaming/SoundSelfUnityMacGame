using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IDchecker : MonoBehaviour
{

    void Start()
        {
            // We first enumerate all Devices from the System shareset to have all available devices on Windows.
            uint sharesetIdSystem = AkSoundEngine.GetIDFromString("System");
            uint deviceCount = 0;
            // Change the name for the name of the Device as written in the Authoring Audio Preference list.
            uint deviceId = AkSoundEngine.GetDeviceIDFromName("Headphones (WH-1000XM5 Stereo)");

            // We create the Second Audio Device Listener GameObject and find the System_01 ShareSetID.
            AkSoundEngine.RegisterGameObj(gameObject, "System2Listener");
            uint sharesetIdSystem2 = AkSoundEngine.GetIDFromString("System_01");
            // Creation of the Output Settings for the second Audio Device. Which will be another device on the machine different from the main Default Device (e.g a Focusrite).
            AkOutputSettings outputSettings2 = new AkOutputSettings();
            outputSettings2.audioDeviceShareset = sharesetIdSystem2;
            // In my case, on my machine, I output to my monitor speaker in HDMI which is different from the default audio device (Realtek audio).
            /*if (devices.Count() == 0)
            {
                print("No Devices found");
                return;
            }*/
            outputSettings2.idDevice = deviceId;
            // We call the AddOutput with the newly created OutputSetting2 for the System_01 and for the system2Listener.
            ulong outDeviceId = 0;
            ulong[] ListenerIds = { AkSoundEngine.GetAkGameObjectID(gameObject) };
            AkSoundEngine.AddOutput(outputSettings2, out outDeviceId, ListenerIds, 1);
            // We Set the listener of Game_Object_System2 to be listened by system2Listener. Set will clear all Emitter-Listener already there, 
            // so the default listener will not be associated anymore.
            AkSoundEngine.RegisterGameObj(gameObject, "System2Go");
            AkSoundEngine.SetListeners(AkSoundEngine.GetAkGameObjectID(gameObject), ListenerIds, 1);

            AkSoundEngine.PostEvent("Play_Sound2", gameObject);
        }
}
