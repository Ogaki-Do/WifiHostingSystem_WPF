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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ConnectionProfile connectionProfile;
        private NetworkOperatorTetheringManager tetheringManager;
        private ZXing.BarcodeWriter QRWrrter;
        
        public  MainWindow()
        {
            InitializeComponent();

            Panel.SetZIndex(MHScontloler, 0);
            Panel.SetZIndex(NeworkWaiting, 1);



            NetworkInformation.NetworkStatusChanged += WaitingForNetworkConnection;
            NetworkCheker();

            QRWrrter = new ZXing.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    CharacterSet = "UTF-8",
                    Height = 400,
                    Width = 400
                }
            };
        }


       

        public void WaitingForNetworkConnection(object o ) 
        {
            Task.Delay(10000).Wait();
            Dispatcher.InvokeAsync(() => NetworkCheker() );
        }
        private void NetworkCheker(){
            connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            if (connectionProfile == null)
            {
                //MessageBox.Show("owwps1");
                return;
            }

            tetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);
            NetworkInformation.NetworkStatusChanged -= WaitingForNetworkConnection;


            Window_Grid.Children.Remove(NeworkWaiting);
            NeworkWaiting.Focus();

            Init();

        }



        private void Init()
        {
            //ホットスポット起動と各種イベントの登録
            //MessageBox.Show("start init");
            if(CheckHotspot(tetheringManager)==false) Dispatcher.InvokeAsync(() => StartHotSpot());

            //イベント登録
            NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
            NetworkInformation.NetworkStatusChanged += MHS_Restarter;

            OnNetworkStatusChanged(new Object());
        }
       

        private void RebootVD(object sender, RoutedEventArgs e)
        {
            string processName = "VirtualDesktop.Streamer";
            var process = Process.GetProcessesByName(processName).FirstOrDefault();
            if (process == null) return;
            else{
                try
                {
                    string VDpass = process.MainModule.FileName;
                    process.Kill();
                    ProcessStartInfo RestertVD = new ProcessStartInfo
                    {
                        FileName = VDpass,
                        UseShellExecute = false
                    };
                    Process.Start(RestertVD);
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            }
        }

        private void MobileHotspootActive_Checked(object sender, RoutedEventArgs e)
        {
            if (!MobileHotspootActive.IsEnabled) return;
            StartHotSpot();
        }
        private void MobileHotspootActive_Uncheckd(object sender, RoutedEventArgs e)
        {
            if (!MobileHotspootActive.IsEnabled) return;
            StopHotSpot();
        }

        
        // ネットワーク情報更新
        private async void OnNetworkStatusChanged(object sender )
        {
            NetworkOperatorTetheringAccessPointConfiguration Conf = tetheringManager.GetCurrentAccessPointConfiguration();
            string QRContent = string.Format("WIFI:S:{1};T:{0};P:{2};;", "WPA", Conf.Ssid, Conf.Passphrase);

            // ホットスポットの状態を更新
            
            await Dispatcher.InvokeAsync(() => WiifiStats.Text = HotSpotStats(tetheringManager));

            //チェックボックス更新
            await Dispatcher.InvokeAsync(() =>
            {
                switch (tetheringManager.TetheringOperationalState)
                {
                    case TetheringOperationalState.On:
                        set_Checkbox(MobileHotspootActive, true);
                        break;
                    case TetheringOperationalState.Off:
                        set_Checkbox(MobileHotspootActive, false);
                        break;
                    default:
                        set_Checkbox(MobileHotspootActive, null);
                        break;

                }
            });

            if (string.IsNullOrEmpty(QRContent))
            {
                //QRCode.Source = new ImageSource(new Uri(`./pic/HeadMono.png", UriKind.Absolute));
                QRCode.Source = null;
                return;
            }
            using (var bmp = QRWrrter.Write(QRContent))
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Bmp);
                // BarcodeWriter.Writeで得られる画像クラスは
                // そのままではWPFでは表示できないので、
                // WPFで扱えるImageSourceに変換する必要がある
                var source = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                await Dispatcher.InvokeAsync(() => QRCode.Source = source);
            }
            return;
        }

        private async void MHS_Restarter(object sender){
            var cpro = NetworkInformation.GetInternetConnectionProfile();
            if (cpro == null && tetheringManager != null)
            {
                if (tetheringManager.TetheringOperationalState == TetheringOperationalState.InTransition)
                {
                    StartHotSpot();
                    return;
                }
            }

        }

        // テキスト生成機
        private string HotSpotStats(NetworkOperatorTetheringManager TM)
        {
            string HSstats = "Update:" + DateTime.Now.ToString("yy/dd/MM HH:mm.ss.FF")+"\n";
            HSstats += "MobileHotSpotStats : ";
            
            // ホットスポットの状態を取得
            switch (TM.TetheringOperationalState)
            {
                case TetheringOperationalState.On:
                    HSstats += "On";
                    break;
                case TetheringOperationalState.Off:
                    HSstats += "Off";
                    break;
                case TetheringOperationalState.InTransition:
                    HSstats += "InTransition";
                    break;
                default:
                    HSstats += "Unknown";
                    break;
            }

            // アクセスポイントの設定を取得
            NetworkOperatorTetheringAccessPointConfiguration Conf = TM.GetCurrentAccessPointConfiguration();
            HSstats += "\n Band : ";
            switch (((int)Conf.Band))
            {
                case 0:
                    HSstats += "Auto";
                    break;
                case 1:
                    HSstats += "2.4Ghz";
                    break;
                case 2:
                    HSstats += "5GHz";
                    break;
                case 3:
                    HSstats += "6GHz";
                    break;
            }

            HSstats += "\n SSID : " + Conf.Ssid;
            HSstats += "\n Pass : " + Conf.Passphrase;
            HSstats += "\n Client:" + TM.ClientCount + "/" + TM.MaxClientCount;
            HSstats += "\n | Name | IPAdd | MacAdd | \n------------------";

            // テザリングクライアントの情報を取得
            IReadOnlyList<NetworkOperatorTetheringClient> Clist = TM.GetTetheringClients();
            foreach (NetworkOperatorTetheringClient client in Clist)
            {
                HSstats += "\n";
                foreach (HostName Hname in client.HostNames)
                    HSstats += Hname.ToString() + "|";
                HSstats += client.MacAddress;
            }

            return HSstats;
        }

        private bool CheckHotspot(NetworkOperatorTetheringManager TM)
        {
            return TM.TetheringOperationalState == TetheringOperationalState.On;
        }

        private void set_Checkbox(CheckBox cbox , Nullable<bool> input)
        {
            cbox.IsEnabled = false;
            cbox.IsChecked = input;
            cbox.IsEnabled = true;
        }
        
        // HotSpotを開始する
        private async void StartHotSpot()
        {
            // モバイルホットスポットをオンにする
            if (CheckHotspot(tetheringManager)) // Offの場合
                MessageBox.Show("ホットスポットは既に有効です");
            else
            {
                tetheringManager.StartTetheringAsync();
                MessageBox.Show("ホットスポットが有効になりました");
            }
        }

        // Hotspotを終了する
        private async void StopHotSpot()
        {
            // モバイルホットスポットをオフにする
            if (CheckHotspot(tetheringManager)) // Onの場合
            {
                tetheringManager.StopTetheringAsync();
                MessageBox.Show("ホットスポット停止しました");
            }
            else
            {
                MessageBox.Show("ホットスポットは既に無効です");
            }
        }

        private void ReLoadNetworkStatus_Click(object sender, RoutedEventArgs e)
        {
            NetworkCheker();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

}