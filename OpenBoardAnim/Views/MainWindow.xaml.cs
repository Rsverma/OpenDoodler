using HandyControl.Controls;
using OpenBoardAnim.ViewModels;
using System.ComponentModel;

namespace OpenBoardAnim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm && !vm.ConfirmExit())
                e.Cancel = true;
        }
    }
}