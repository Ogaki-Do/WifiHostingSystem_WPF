using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Networking.NetworkOperators;

namespace WifiHostingSystem_WPF.PageData
{
    /// <summary>
    /// HotSpotStats.xaml の相互作用ロジック
    /// </summary>
    /// 
    
    public partial class HotSpotStats : UserControl
    {

        private NetworkOperatorTetheringManager tetheringManager;
        public HotSpotStats()
        {
            InitializeComponent();
            this.Loaded += ThisLoaded;
            
            
        }

        private void ThisLoaded (object sender, RoutedEventArgs e)
        {
            var appins = Application.Current as App;
            HostList.ItemsSource = appins.MakeHostlist();
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
