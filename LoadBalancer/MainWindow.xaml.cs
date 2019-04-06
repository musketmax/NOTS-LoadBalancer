using System.Windows;
using System.Windows.Threading;
using LoadBalancerClassLibrary.Models;
using LoadBalancerClassLibrary.ViewModels;

namespace LoadBalancer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LoadBalancerView VM;
        public MainWindow()
        {
            InitializeComponent();
            VM = new LoadBalancerView();
            DataContext = VM;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            VM.StartStop();
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            VM.ClearLog();
        }
    }
}
