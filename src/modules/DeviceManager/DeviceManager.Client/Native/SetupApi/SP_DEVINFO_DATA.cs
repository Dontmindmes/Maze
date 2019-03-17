﻿using System;
using System.Runtime.InteropServices;

namespace DeviceManager.Client.Native.SetupApi
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public int Size;
        public Guid ClassGuid;
        public int DevInst;
        public IntPtr Reserved;
    }
}