// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable InconsistentNaming

using System.Runtime.InteropServices;

namespace Shuffler.Core;

public class AudioManager
{
    private readonly ISimpleAudioVolume? _simpleAudioVolume;

    public AudioManager(int processId)
    {
        _simpleAudioVolume = GetVolumeControl(processId);
    }

    public void SetApplicationMute(bool mute)
    {
        if (_simpleAudioVolume != null)
        {
            var guid = Guid.NewGuid();
            _simpleAudioVolume.SetMute(mute, ref guid);
        }
    }
    
    public bool GetApplicationMute()
    {
        if (_simpleAudioVolume != null)
        {
            _simpleAudioVolume.GetMute(out var mute);
            return mute;
        }

        return false;
    }

    private ISimpleAudioVolume? GetVolumeControl(int processId)
    {
        var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
        deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);

        var iidIAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
        device.Activate(ref iidIAudioSessionManager2, 0, IntPtr.Zero, out var sessionManager);

        var mgr = (IAudioSessionManager2)sessionManager;
        mgr.GetSessionEnumerator(out var sessionEnumerator);
        sessionEnumerator.GetCount(out var count);

        for (var i = 0; i < count; i++)
        {
            sessionEnumerator.GetSession(i, out IAudioSessionControl2 session);
            session.GetProcessId(out uint sessionProcessId);

            if (sessionProcessId == processId)
                return session as ISimpleAudioVolume;
        }

        return null;
    }
}

#region COM Interfaces
[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class MMDeviceEnumerator
{
}

[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    int NotImpl1();
    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
}

[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    [PreserveSig]
    int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
}

[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionManager2
{
    int NotImpl1();
    int NotImpl2();
    [PreserveSig]
    int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
}

[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionEnumerator
{
    [PreserveSig]
    int GetCount(out int SessionCount);
    [PreserveSig]
    int GetSession(int SessionCount, out IAudioSessionControl2 Session);
}

[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISimpleAudioVolume
{
    [PreserveSig]
    int SetMasterVolume(float fLevel, ref Guid EventContext);
    [PreserveSig]
    int GetMasterVolume(out float pfLevel);
    [PreserveSig]
    int SetMute(bool bMute, ref Guid EventContext);
    [PreserveSig]
    int GetMute(out bool pbMute);
}

[Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl2
{
    // IAudioSessionControl
    [PreserveSig]
    int NotImpl0();
    [PreserveSig]
    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
    [PreserveSig]
    int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
    [PreserveSig]
    int GetGroupingParam(out Guid pRetVal);
    [PreserveSig]
    int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
    [PreserveSig]
    int NotImpl1();
    [PreserveSig]
    int NotImpl2();

    // IAudioSessionControl2
    [PreserveSig]
    int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int GetProcessId(out uint pRetVal);
    [PreserveSig]
    int IsSystemSoundsSession();
    [PreserveSig]
    int SetDuckingPreference(bool optOut);
}

internal enum EDataFlow
{
    eRender,
    eCapture,
    eAll,
    EDataFlow_enum_count
}

internal enum ERole
{
    eConsole,
    eMultimedia,
    eCommunications,
    ERole_enum_count
}
#endregion
