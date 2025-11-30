using System;
using System.Linq;
using UnityEngine;
public class MicLoopProbe : MonoBehaviour
{
    string dev;
    AudioClip clip;
    int readHeadFrames;
    float[] inter, mono;
    public bool loopToSpeakers = true; // hear yourself to confirm OS->Unity path

    void Start()
    {
        foreach (var d in Microphone.devices) Debug.Log("[MicDevice] " + d);
        dev = Microphone.devices.FirstOrDefault(d => d.Contains("Airpod")) 
              ?? Microphone.devices.FirstOrDefault();
        if (string.IsNullOrEmpty(dev)) { Debug.LogError("No mic devices."); return; }

        clip = Microphone.Start(dev, true, 2, 48000);
        StartCoroutine(WaitAndInit());
    }

    System.Collections.IEnumerator WaitAndInit()
    {
        float t0 = Time.realtimeSinceStartup;
        while (Microphone.GetPosition(dev) <= 0 && Time.realtimeSinceStartup - t0 < 2f) yield return null;

        Debug.Log($"[Rates] micClip.freq(actual)={clip.frequency} outputRate={AudioSettings.outputSampleRate} ch={clip.channels}");
        int frames = Mathf.Max(256, clip.frequency/20); // ~50ms
        inter = new float[frames * clip.channels];
        mono  = new float[frames];

        if (loopToSpeakers)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = clip; src.loop = true; src.volume = 1f; 
            while (Microphone.GetPosition(dev) <= 0) yield return null;
            src.Play(); // you should hear live monitoring in build
        }
    }

    void Update()
    {
        if (clip == null || inter == null) return;

        int ch = clip.channels;
        int clipFrames = clip.samples;                    // per-channel frames
        int writePos = Microphone.GetPosition(dev);       // per-channel frames
        if (writePos < 0) return;

        int avail = (clipFrames + writePos - readHeadFrames) % clipFrames;
        int frames = mono.Length;

        if (avail >= frames)
        {
            int end = readHeadFrames + frames;
            if (end <= clipFrames) clip.GetData(inter, readHeadFrames);
            else {
                int tail = clipFrames - readHeadFrames, head = frames - tail;
                var t = new float[tail*ch]; var h = new float[head*ch];
                clip.GetData(t, readHeadFrames); clip.GetData(h, 0);
                Buffer.BlockCopy(t, 0, inter, 0, t.Length*sizeof(float));
                Buffer.BlockCopy(h, 0, inter, t.Length*sizeof(float), h.Length*sizeof(float));
            }

            if (ch == 1) Buffer.BlockCopy(inter, 0, mono, 0, mono.Length*sizeof(float));
            else { for (int f=0,i=0; f<mono.Length; f++, i+=ch) { double s=0; for (int c=0;c<ch;c++) s+=inter[i+c]; mono[f]=(float)(s/ch); } }

            double acc=0; float peak=0;
            for (int i=0;i<mono.Length;i++){ float v=mono[i]; acc+=v*v; if (Mathf.Abs(v)>peak) peak=Mathf.Abs(v);}
            float rms=(float)Math.Sqrt(acc/mono.Length);
            Debug.Log($"[Probe] pos={writePos} rms={rms:F4} peak={peak:F3}");

            readHeadFrames = (readHeadFrames + frames) % clipFrames;
        }
    }
}
