using System;

namespace JeremyAnsel.DirectX.SdkCamera
{
    [Flags]
    public enum SdkCameraKeyStates
    {
        IsDownMask = 0x01,
        WasDownMask = 0x80,
    }
}
