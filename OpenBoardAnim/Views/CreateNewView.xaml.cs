using HandyControl.Controls;
using System.Windows;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for CreateNewView.xaml
    /// </summary>
    public partial class CreateNewView : GlowWindow
    {
        public CreateNewView()
        {
            InitializeComponent();
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}