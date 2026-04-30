using System;
using System.Threading;
using UnityEngine;

public static class SingleInstanceMutexGuard
{
    private static Mutex singleInstanceMutex;
    private static bool hasMutex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureSingleInstance()
    {
#if UNITY_EDITOR
        return;
#else
        if (singleInstanceMutex != null)
        {
            return;
        }

        string appId = Application.identifier;
        if (string.IsNullOrWhiteSpace(appId))
        {
            appId = Application.companyName + "." + Application.productName;
        }

        string mutexName = ("SoundSelf.SingleInstance." + appId).Replace(" ", "_");

        try
        {
            singleInstanceMutex = new Mutex(initiallyOwned: false, name: mutexName);
            hasMutex = singleInstanceMutex.WaitOne(0, false);

            if (!hasMutex)
            {
                Debug.LogWarning("Duplicate game launch detected. Closing this instance.");
                Application.Quit();
                return;
            }

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }
        catch (Exception ex)
        {
            Debug.LogError("Single instance mutex setup failed: " + ex.Message);
        }
#endif
    }

    private static void OnProcessExit(object sender, EventArgs e)
    {
        ReleaseMutex();
    }

    private static void OnDomainUnload(object sender, EventArgs e)
    {
        ReleaseMutex();
    }

    private static void ReleaseMutex()
    {
        if (singleInstanceMutex == null)
        {
            return;
        }

        if (hasMutex)
        {
            try
            {
                singleInstanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex is already released.
            }
        }

        singleInstanceMutex.Dispose();
        singleInstanceMutex = null;
        hasMutex = false;
    }
}
