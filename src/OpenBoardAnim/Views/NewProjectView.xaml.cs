using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for NewProjectView.xaml
    /// </summary>
    public partial class NewProjectView : UserControl
    {
        public NewProjectView()
        {
            InitializeComponent();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not NewProjectPromptModel model) return;
                model.CreateProject?.Invoke(model.Project);
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
