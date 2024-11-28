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
            NetworkInformation.NetworkStatusChanged += WaitingForNetworkConnection;
            NetworkCheker();

            QRWrrter = new ZXing.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    CharacterSet = "UTF-8",
                    Height = 200,
                    Width = 200
                }
            };
        }


       
        //ネットワーク初期接続関数(接続からプロファイルとれるまでラグがあるので10秒待つ)
        public void WaitingForNetworkConnection(object o ) 
        {
            Task.Delay(10000).Wait();
            Dispatcher.InvokeAsync(() => NetworkCheker() );
        }

        //ネットワークに接続状態判定＆マネージャ握って初期操作するマン
        private void NetworkCheker(){
            connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            if (connectionProfile == null)
            {
                //MessageBox.Show("owwps1");
                return;
            }
            //初期設定
            tetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);
            //待機イベント削除
            NetworkInformation.NetworkStatusChanged -= WaitingForNetworkConnection;
            //ブロッカー削除
            Window_Grid.Children.Remove(NeworkWaiting);
            //初期化処理
            //ホットスポット起動と各種イベントの登録
            if (CheckHotspot(tetheringManager) == false) Dispatcher.InvokeAsync(() => StartHotSpot());

            //イベント登録
            NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
            NetworkInformation.NetworkStatusChanged += MHS_Restarter;
            //ステータス更新
            OnNetworkStatusChanged(new Object());

        }
      
        //VD強制再起動装置
        private void RebootVD(object sender, RoutedEventArgs e)
        {
            //PID検索
            string processName = "VirtualDesktop.Streamer";
            var process = Process.GetProcessesByName(processName).FirstOrDefault();
            if (process == null) return;
            else{
                try
                {
                    //再起動用ファイルパス取得
                    string VDpass = process.MainModule.FileName;
                    process.Kill();//kill
                    //VD再起動
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

        //モバイルホットスポット操作用チェックボックス
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

            //QRコード生成
            if (string.IsNullOrEmpty(QRContent))
            {
                //QRCode.Source = new ImageSource(new Uri(`./pic/HeadMono.png", UriKind.Absolute));
                QRCode.Source = null;
                return;
            }
            //QRコード画像変換
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

        //試作　モバイルホットスポット再起動マシーン
        private async void MHS_Restarter(object sender){
            //ネットにつながってなくてモバイルホットスポットが自動で止まった時再起動する。
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

        // デバッグテキスト生成機
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

        //ホットスポットの動作判定機
        private bool CheckHotspot(NetworkOperatorTetheringManager TM)
        {
            return TM.TetheringOperationalState == TetheringOperationalState.On;
        }

        //簡易チェックボックス操作機
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

        //ネットワーク待機状態の手動更新ボタン
        private void ReLoadNetworkStatus_Click(object sender, RoutedEventArgs e)
        {
            WaitingForNetworkConnection(new object());
        }
    }

}