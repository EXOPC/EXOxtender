//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Management;
//using System.Runtime.InteropServices;

//namespace EXOxtenderLibrary
//{
//    public class Wifi
//    {
//        public  int GetSignalStrength()
//        {
//            Int32 returnStrength = 0;
//            ManagementObjectSearcher searcher = null;
//            try
//            {

//                searcher = new ManagementObjectSearcher("root\\WMI", "select Ndis80211ReceivedSignalStrength from MSNdis_80211_ReceivedSignalStrength where active=true");
//                ManagementObjectCollection adapterObjects = searcher.Get();

//                foreach (ManagementObject mo in adapterObjects)
//                {
//                    returnStrength = Convert.ToInt32(mo["Ndis80211ReceivedSignalStrength"].ToString());
//                    break;
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }
//            return returnStrength;
//        }





//        //[DllImport("Wlanapi.dll", SetLastError = true)]
//        //public static extern uint WlanGetAvailableNetworkList(IntPtr hClientHandle, ref Guid pInterfaceGuid, uint dwFlags, IntPtr pReserved, ref IntPtr ppAvailableNetworkList);

//        //private const uint WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_ADHOC_PROFILES = 0x00000001;
//        //private const uint WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_MANUAL_HIDDEN_PROFILES = 0x00000002;

//        //static void getNetworkList()
//        //{
//        //    //IntPtr ppAvailableNetworkList = new IntPtr();
//        //    //Guid pInterfaceGuid = ((WLAN_INTERFACE_INFO)wlanInterfaceInfoList.InterfaceInfo[0]).InterfaceGuid;
//        //    //WlanGetAvailableNetworkList(ClientHandle, ref pInterfaceGuid, WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_MANUAL_HIDDEN_PROFILES, new IntPtr(), ref  ppAvailableNetworkList);
//        //    //WLAN_AVAILABLE_NETWORK_LIST wlanAvailableNetworkList = new WLAN_AVAILABLE_NETWORK_LIST(ppAvailableNetworkList);
//        //    //WlanFreeMemory(ppAvailableNetworkList);
//        //    //for (int j = 0; j < wlanAvailableNetworkList.dwNumberOfItems; j++)
//        //    //{
//        //    //    Interop.WLAN_AVAILABLE_NETWORK network = list.wlanAvailableNetwork[j];
//        //    //    Console.WriteLine("Available Network: ");
//        //    //    Console.WriteLine("SSID: " + network.dot11Ssid.ucSSID);
//        //    //    Console.WriteLine("Encrypted: " + network.bSecurityEnabled);
//        //    //    Console.WriteLine("Signal Strength: " + network.wlanSignalQuality);
//        //    //    Console.WriteLine("Default Authentication: " +
//        //    //        network.dot11DefaultAuthAlgorithm.ToString());
//        //    //    Console.WriteLine("Default Cipher: " + network.dot11DefaultCipherAlgorithm.ToString());
//        //    //    Console.WriteLine();
//        //    //}
//        //}


//    }
//}
