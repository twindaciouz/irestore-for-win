﻿// @iSn0wra1n http://twitter.com/iSn0wra1n
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

class Program
{
    #region stuff
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct AMDeviceNotificationCallbackInfo
    {
        public IntPtr dev
        {
            get
            {
                return dev_ptr;
            }
        }
        private IntPtr dev_ptr;
        public int msg;
    }


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void DeviceNotificationCallback(ref AMDeviceNotificationCallbackInfo callback_info);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void DeviceRestoreNotificationCallback(IntPtr callback_info);

    [DllImport("iTunesMobileDevice.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static int AMDeviceNotificationSubscribe(DeviceNotificationCallback callback, uint unused1, uint unused2, uint unused3, out IntPtr am_device_notification_ptr);

    [DllImport("iTunesMobileDevice.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static int AMDeviceEnterRecovery(IntPtr device);

    [DllImport("iTunesMobileDevice.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static int AMRestoreRegisterForDeviceNotifications(
        DeviceRestoreNotificationCallback dfu_connect,
        DeviceRestoreNotificationCallback recovery_connect,
        DeviceRestoreNotificationCallback dfu_disconnect,
        DeviceRestoreNotificationCallback recovery_disconnect,
        uint unknown0,
        IntPtr user_info);
    #endregion
    #region initz
    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);

    public static bool Is64Bit()
    {
        if (IntPtr.Size == 8 || (IntPtr.Size == 4 && Is32BitProcessOn64BitProcessor()))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static bool Is32BitProcessOn64BitProcessor()
    {
        bool retVal;
        IsWow64Process(Process.GetCurrentProcess().Handle, out retVal);
        return retVal;
    }

    private static int AttachiTunes()
    {
        FileInfo iTunesMobileDeviceFile = null;
        DirectoryInfo ApplicationSupportDirectory = null;
        if (Is64Bit() == true)
        {
            string dir1 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Apple Inc.\Apple Mobile Device Support", "InstallDir", "iTunesMobileDevice.dll").ToString() + "iTunesMobileDevice.dll";
            iTunesMobileDeviceFile = new FileInfo(dir1);
            string dir2 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Apple Inc.\Apple Application Support", "InstallDir", Environment.CurrentDirectory).ToString();
            ApplicationSupportDirectory = new DirectoryInfo(dir2);
        }
        else
        {
            iTunesMobileDeviceFile = new FileInfo(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Apple Inc.\Apple Mobile Device Support\Shared", "iTunesMobileDeviceDLL", "iTunesMobileDevice.dll").ToString());
            ApplicationSupportDirectory = new DirectoryInfo(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Apple Inc.\Apple Application Support", "InstallDir", Environment.CurrentDirectory).ToString());
        }

        string directoryName = iTunesMobileDeviceFile.DirectoryName;
        if (!iTunesMobileDeviceFile.Exists)
        {
            Console.Error.WriteLine("Could not find iTunesMobileDevice file");
            return -1;
        }
        Environment.SetEnvironmentVariable("Path", string.Join(";", new string[] { Environment.GetEnvironmentVariable("Path"), directoryName, ApplicationSupportDirectory.FullName }));
        return 0;
    }
    #endregion
    
    static void dfuConnect(IntPtr usbDev)
    {
        Console.WriteLine("Device connected in DFU Mode");
    }

    static void dfuDisconnect(IntPtr usbDev)
    {
        Console.WriteLine("Device exited DFU Mode");
    }

    static void recoveryConnect(IntPtr usbDev)
    {
        Console.WriteLine("Device connected in Recovery Mode");
    }

    static void recoveryDisconnect(IntPtr usbDev)
    {
        Console.WriteLine("Device exited Recovery Mode");
    }

    static void usbMuxMode(ref AMDeviceNotificationCallbackInfo callback_info)
    {
        IntPtr devHandle = callback_info.dev;
        if (callback_info.msg == 1)
        {
            Console.WriteLine("Device connected in Usb Multiplexing mode");
            DoMyCode(devHandle);
        }
        else if (callback_info.msg == 2)
            Console.WriteLine("Device disconnected when in Usb Multiplexing mode");
        else
            Console.WriteLine("Device in unknown usbmux mode");
        
    }

    static void DoMyCode(IntPtr dev)
    {
        Console.WriteLine("Device entering recovery mode");
        int ret = AMDeviceEnterRecovery(dev);
        if (ret != 0)
        {
            Console.WriteLine("Device could not enter recovery mode!");
            Environment.Exit(ret);
        }        
    }
    public static int Main(string[] args)
    {
        if (AttachiTunes() == -1)
        {
            Console.Error.WriteLine("Failed to attach iTunes!");
            return -1;
        }
        IntPtr am_device_notification;

        AMDeviceNotificationSubscribe(usbMuxMode, 0, 0, 0, out am_device_notification);
        AMRestoreRegisterForDeviceNotifications(dfuConnect, recoveryConnect, dfuDisconnect, recoveryDisconnect, 0, IntPtr.Zero);
        Thread.Sleep(-1);
        return 0;
    }

}


