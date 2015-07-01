using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VAdapter
{
    public class InterfaceManager
    {
        public Stream OpenDevice(string guid)
        {
            const string UsermodeDeviceSpace = "\\\\.\\Global\\";

            IntPtr ptr = CreateFile(UsermodeDeviceSpace + guid + ".tap", FileAccess.ReadWrite,
                FileShare.ReadWrite, 0, FileMode.Open, FILE_ATTRIBUTE_SYSTEM | FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            int len;
            IntPtr pstatus = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(pstatus, 1);
            DeviceIoControl(ptr, TAP_CONTROL_CODE(6, METHOD_BUFFERED) /* TAP_IOCTL_SET_MEDIA_STATUS */, pstatus, 4,
                    pstatus, 4, out len, IntPtr.Zero);
         //   DeviceIoControl(ptr, TAP_CONTROL_CODE(10, METHOD_BUFFERED) /* TAP_IOCTL_CONFIG_TUN */, ptun, 12,
          //      ptun, 12, out len, IntPtr.Zero);
            return new FileStream(ptr, FileAccess.ReadWrite, true, 10000, true);
            
        }


        //
        // Pick up the first tuntap device and return its node GUID
        //
        public IEnumerable<string> GetDevices()
        {
            const string AdapterKey = "SYSTEM\\CurrentControlSet\\Control\\Class\\{4D36E972-E325-11CE-BFC1-08002BE10318}";
            RegistryKey regAdapters = Registry.LocalMachine.OpenSubKey(AdapterKey, true);
            string[] keyNames = regAdapters.GetSubKeyNames();
            foreach (string x in keyNames)
            {
                if (x != "Properties")
                {
                    RegistryKey regAdapter = regAdapters.OpenSubKey(x);
                    object id = regAdapter.GetValue("ComponentId");
                    if (id != null && id.ToString().StartsWith("tap")) {
                        yield return regAdapter.GetValue("NetCfgInstanceId").ToString();
                    
                    }
                }
            }
        }
        //
        // Returns the device name from the Control panel based on GUID
        //
        public string FriendlyName(string guid)
        {
            const string ConnectionKey = "SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}";
            if (guid != "")
            {
                RegistryKey regConnection = Registry.LocalMachine.OpenSubKey(ConnectionKey + "\\" + guid + "\\Connection", true);
                object id = regConnection.GetValue("Name");
                if (id != null) return id.ToString();
            }
            return "";
        }

        private static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            return ((DeviceType << 16) | (Access << 14) | (Function << 2) | Method);
        }

        static uint TAP_CONTROL_CODE(uint request, uint method)
        {
            return CTL_CODE(FILE_DEVICE_UNKNOWN, request, method, FILE_ANY_ACCESS);
        }
        private const uint METHOD_BUFFERED = 0;
        private const uint FILE_ANY_ACCESS = 0;
        private const uint FILE_DEVICE_UNKNOWN = 0x00000022;

        static FileStream Tap;
        static int BytesRead;

        [DllImport("Kernel32.dll", /* ExactSpelling = true, */ SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateFile(
            string filename,
            [MarshalAs(UnmanagedType.U4)]FileAccess fileaccess,
            [MarshalAs(UnmanagedType.U4)]FileShare fileshare,
            int securityattributes,
            [MarshalAs(UnmanagedType.U4)]FileMode creationdisposition,
            int flags,
            IntPtr template);
        const int FILE_ATTRIBUTE_SYSTEM = 0x4;
        const int FILE_FLAG_OVERLAPPED = 0x40000000;

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
            IntPtr lpInBuffer, uint nInBufferSize,
            IntPtr lpOutBuffer, uint nOutBufferSize,
            out int lpBytesReturned, IntPtr lpOverlapped);

    }
    
}
