using System;

namespace JeremyAnsel.DirectX.SdkCamera
{
    [Flags]
    public enum SdkCameraMouseKeys
    {
        LeftButton = 0x01,
        MiddleButton = 0x02,
        RightButton = 0x04,
        Wheel = 0x08,
    }
}
