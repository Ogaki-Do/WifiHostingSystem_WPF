using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;
using Windows.Networking;
using System.Windows.Threading;
using Microsoft.Win32;
using ZXing.QrCode;
using ZXing;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using Windows.Devices.WiFi;
using System.Net;
namespace WifiHostingSystem_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public class Host
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public string Mac { get; set; }
        public Host(string name, string ip, string mac)
        {
            Name = name;
            IP = ip;
            Mac = mac;
        }
    }


    public partial class App : Application
    {
        //ネットワーク管理用インスタンス
        public ConnectionProfile connectionProfile;
        public NetworkOperatorTetheringManager tetheringManager;

        public bool NetworkLoader()
        {
            connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            if (connectionProfile == null)
            {
                //MessageBox.Show("owwps1");
                return false;
            }
            tetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);
            return true;
        }



        //ホットスポットの動作状態確認機
        public bool? CheckHotspot(bool nullable)
        {

                switch (tetheringManager.TetheringOperationalState)
                {
                    case TetheringOperationalState.On:
                        return true;
                    case TetheringOperationalState.Off:
                        return false;
                    default:
                        return null;
                }
            
        }
        public bool CheckHotspot()
        {
            return tetheringManager.TetheringOperationalState == TetheringOperationalState.On;
        }

        // HotSpotを開始する
        public  bool? SetHotSpotRuning(bool mode)
        {

            
            tetheringManager.StartTetheringAsync();
            tetheringManager.StopTetheringAsync();


            


            return null;


        }










        public  List<Host> MakeHostlist()
        {
            return new List <Host>{
                new Host("hoge1host", "192.168.1.1","11:11:11:11:11:11"),
                new Host("hoge2host", "192.168.1.2","22:22:22:22:22:22"),
                new Host("hoge3host", "192.168.1.3","33:33:33:33:33:33"),
                new Host("hoge4host", "192.168.1.4","44:44:44:44:44:44")
            };
            
        }
    }
}

