using System;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using Microsoft.Win32;

// references:
// https://www.nextofwindows.com/the-best-way-to-uniquely-identify-a-windows-machine
// https://www.howtogeek.com/211664/is-it-safe-for-everyone-to-be-able-to-see-my-windows-product-id/
// https://ntsblog.homedev.com.au/index.php/2012/06/26/read-computer-sid-64-bit-machine/

namespace MachineIdTester
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ////**********  UUID **********////

            // A UUID is a unique identifier generated and supplied by the motherboard vendor.
            //
            // Example: 96149BFB-1914-483A-2C03-F3669756E3DF
            //
            // Ways to obtain manually:
            // from console: wmic csproduct get UUID
            // from PowerShell: (Get-CimInstance -Class Win32_ComputerSystemProduct).UUID
            //
            // Note: in the event a motherboard vendor does not provide one, the default value
            // is FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF
            string uuid = GetUUID();
            Console.WriteLine("wmic csproduct get UUID:");
            Console.WriteLine($"{uuid}\n");

            ////********** Hard Drive Serial Number **********////

            // Example:  ACE4_2E81_7028_5BB6.
            //
            // Ways to obtain manually:
            // from console: wmic DISKDRIVE get SerialNumber
            // from PowerShell: (Get-CimInstance -Class Win32_DiskDrive).SerialNumber
            string serialNumber = GetSerialNumber();
            Console.WriteLine("wmic DISKDRIVE get SerialNumber:");
            Console.WriteLine($"{serialNumber}\n");

            ////********** Registry MachineGuid ********** ////

            // Example: D8BF5749-9C2C-4CBC-A688-C999D619DD6B
            //
            // Way to obtain manually:
            // via Registry Editor: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography
            string machineGuid = GetMachineGuid();
            Console.WriteLine("MachineGuid (from registry):");
            Console.WriteLine($"{machineGuid}\n");

            ////********** Windows Product ID ********** ////

            // A Windows Product ID is a system specific alphanumeric code which is derived/calculated
            // from the product key used to activate Windows (during installation) and the hardware
            // configuration of the machine.
            //
            // Example: 00330-62994-59108-BBOEM
            //
            // Ways to obtain manually:
            // via Control Panel: Control Panel -> System and Security -> System
            // via Registry Editor: SOFTWARE\Microsoft\Windows NT\CurrentVersion
            string productId = GetWindowsProductId();
            Console.WriteLine("Windows ProductId (from registry):");
            Console.WriteLine($"{productId}\n");

            ////********** MAC Address(es) ********** ////

            // A Media Access Control address(MAC address) is a unique identifier assigned to most
            // network adapters or network interface cards (NICs). A computer can easily have multiple
            // MAC address from multiple network adapters. For example, almost all laptops come with one
            // for Ethernet, one for Wi-Fi network and one for Bluetooth.
            //
            // Example: F26E0BD68352 (dashes and/or colons removed)
            //
            // Way to obtain manually:
            // from console: ipconfig /all (look for Physical Address of each adaptor listed)
            string address = GetFirstMACAddress();
            Console.WriteLine("first MAC address obtained via System.Net.NetworkInterface.GetAllNetworkInterfaces():");
            Console.WriteLine($"{address}\n");
            string addresses = GetAllMACAddresses();
            Console.WriteLine("all MAC addresses obtained via System.Net.NetworkInterface.GetAllNetworkInterfaces():");
            Console.WriteLine($"{addresses}\n");
            string addresses2 = GetManagementPropertyValue("Win32_NetworkAdapterConfiguration", "MacAddress");
            Console.WriteLine("all MAC addresses obtained via System.Management:");
            Console.WriteLine($"{addresses2}\n");

            ////********** Processor ID ********** ////

            // Example: BFEBFBFF000712EA
            //
            // Way to obtain manually:
            // from command line: wmic cpu get ProcessorId
            //
            // Note: the Processor ID for will be the same for all systems running as virtual machines on
            // the same hypervisor.
            Console.WriteLine("processor id obtained via System.Management:");
            string value = GetManagementPropertyValue("Win32_Processor", "ProcessorId");
            Console.WriteLine($"{value}\n");

            ////********** Other Identifying Info? ********** ////

            Console.WriteLine("Win32_Processor Name: {0}", GetManagementPropertyValue("Win32_Processor", "Name"));
            Console.WriteLine("Win32_Processor Manufacturer: {0}", GetManagementPropertyValue("Win32_Processor", "Manufacturer"));
            Console.WriteLine("Win32_Processor MaxClockSpeed: {0}\n", GetManagementPropertyValue("Win32_Processor", "MaxClockSpeed"));
            Console.WriteLine("Win32_Bios Manufacturer: {0}", GetManagementPropertyValue("Win32_BIOS", "Manufacturer"));
            Console.WriteLine("Win32_Bios SMBIOSBIOSVersion: {0}", GetManagementPropertyValue("Win32_BIOS", "SMBIOSBIOSVersion"));
            Console.WriteLine("Win32_Bios IdentificationCode: {0}", GetManagementPropertyValue("Win32_BIOS", "IdentificationCode"));
            Console.WriteLine("Win32_Bios SerialNumber: {0}", GetManagementPropertyValue("Win32_BIOS", "SerialNumber"));
            Console.WriteLine("Win32_Bios ReleaseDate: {0}", GetManagementPropertyValue("Win32_BIOS", "ReleaseDate"));
            Console.WriteLine("Win32_Bios Version: {0}\n", GetManagementPropertyValue("Win32_BIOS", "Version"));
            Console.WriteLine("Win32_DiskDrive Model: {0}", GetManagementPropertyValue("Win32_DiskDrive", "Model"));
            Console.WriteLine("Win32_DiskDrive Manufacturer: {0}", GetManagementPropertyValue("Win32_DiskDrive", "Manufacturer"));
            Console.WriteLine("Win32_DiskDrive Signature: {0}", GetManagementPropertyValue("Win32_DiskDrive", "Signature"));
            Console.WriteLine("Win32_DiskDrive TotalHeads: {0}\n", GetManagementPropertyValue("Win32_DiskDrive", "TotalHeads"));
            Console.WriteLine("Win32_BaseBoard Model: {0}", GetManagementPropertyValue("Win32_BaseBoard", "Model"));
            Console.WriteLine("Win32_BaseBoard Manufacturer: {0}", GetManagementPropertyValue("Win32_BaseBoard", "Manufacturer"));
            Console.WriteLine("Win32_BaseBoard Signature: {0}", GetManagementPropertyValue("Win32_BaseBoard", "Signature"));
            Console.WriteLine("Win32_BaseBoard TotalHeads: {0}\n", GetManagementPropertyValue("Win32_BaseBoard", "TotalHeads"));
            Console.WriteLine("Win32_VideoController DriveVersion: {0}", GetManagementPropertyValue("Win32_VideoController", "DriverVersion"));
            Console.WriteLine("Win32_VideoController Name: {0}", GetManagementPropertyValue("Win32_VideoController", "Name"));

            Console.ReadLine();
        }

        public static string GetUUID()
        {
            var procStartInfo = new ProcessStartInfo("cmd", "/c " + "wmic csproduct get UUID")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = new Process() { StartInfo = procStartInfo };
            proc.Start();

            return proc.StandardOutput.ReadToEnd().Replace("UUID", string.Empty).Trim().ToUpper();
        }

        public static string GetSerialNumber()
        {
            var procStartInfo = new ProcessStartInfo("cmd", "/c " + "wmic DISKDRIVE get SerialNumber")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = new Process() { StartInfo = procStartInfo };
            proc.Start();

            return proc.StandardOutput.ReadToEnd().Replace("SerialNumber", string.Empty).Trim().ToUpper();
        }

        public static string GetMachineGuid() =>
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")?.GetValue("MachineGuid")?.ToString().ToUpper();

        public static string GetWindowsProductId() =>
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")?.GetValue("ProductId")?.ToString();

        public static string GetFirstMACAddress()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            string address = string.Empty;
            foreach (NetworkInterface adapter in interfaces)
            {
                if (address == string.Empty)
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    address = adapter.GetPhysicalAddress().ToString();
                }
            }

            return address;
        }

        public static string GetAllMACAddresses()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            string addresses = string.Empty;
            foreach (NetworkInterface adapter in interfaces)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                addresses += adapter.GetPhysicalAddress().ToString();
            }

            return addresses;
        }

        private static string GetManagementPropertyValue(string managementClass, string managementPropertyName)
        {
            string result = string.Empty;

            ManagementClass mc = new ManagementClass(managementClass);
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                try
                {
                    result += mo[managementPropertyName].ToString().Replace(":", string.Empty);
                }
                catch (Exception ex)
                {
                }
            }

            /*
            this is an alternative way to do the above:
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + managementClass);
            foreach (ManagementObject mo in searcher.Get())
            {
                result += mo[managementProperty];
            }
            */

            return result;
        }
    }
}
