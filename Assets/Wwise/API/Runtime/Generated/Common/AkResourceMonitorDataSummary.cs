#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (https://www.swig.org).
// Version 4.2.1
//
// Do not make changes to this file unless you know what you are doing - modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------


public class AkResourceMonitorDataSummary : global::System.IDisposable {
  private global::System.IntPtr swigCPtr;
  protected bool swigCMemOwn;

  internal AkResourceMonitorDataSummary(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = cPtr;
  }

  internal static global::System.IntPtr getCPtr(AkResourceMonitorDataSummary obj) {
    return (obj == null) ? global::System.IntPtr.Zero : obj.swigCPtr;
  }

  internal virtual void setCPtr(global::System.IntPtr cPtr) {
    Dispose();
    swigCPtr = cPtr;
  }

  ~AkResourceMonitorDataSummary() {
    Dispose(false);
  }

  public void Dispose() {
    Dispose(true);
    global::System.GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          AkSoundEnginePINVOKE.CSharp_delete_AkResourceMonitorDataSummary(swigCPtr);
        }
        swigCPtr = global::System.IntPtr.Zero;
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public float totalCPU { set { AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_totalCPU_set(swigCPtr, value); }  get { return AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_totalCPU_get(swigCPtr); } 
  }

  public float pluginCPU { set { AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_pluginCPU_set(swigCPtr, value); }  get { return AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_pluginCPU_get(swigCPtr); } 
  }

  public uint physicalVoices { set { AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_physicalVoices_set(swigCPtr, value); }  get { return AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_physicalVoices_get(swigCPtr); } 
  }

  public uint virtualVoices { set { AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_virtualVoices_set(swigCPtr, value); }  get { return AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_virtualVoices_get(swigCPtr); } 
  }

  public uint totalVoices { set { AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_totalVoices_set(swigCPtr, value); }  get { return AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_totalVoices_get(swigCPtr); } 
  }

  public uint nbActiveEvents { set { AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_nbActiveEvents_set(swigCPtr, value); }  get { return AkSoundEnginePINVOKE.CSharp_AkResourceMonitorDataSummary_nbActiveEvents_get(swigCPtr); } 
  }

  public AkResourceMonitorDataSummary() : this(AkSoundEnginePINVOKE.CSharp_new_AkResourceMonitorDataSummary(), true) {
  }

}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.