using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OpenBoardAnim.Util;

namespace OpenBoardAnim.Windows.Other
{
    public partial class Startup : Window
    {
        public Startup()
        {
            InitializeComponent();
        }

        #region Events

        private void Startup_OnLoaded(object sender, RoutedEventArgs e)
        {

        }
        

        private void Buttons_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Editor_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var editor = new Editor();
            GenericShowDialog(editor);
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //var options = new Options { Owner = this };
            //options.ShowDialog();
        }

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            var test = new TestField();
            test.ShowDialog();
        }

        #endregion

        #region Methods

        private void GenericShowDialog(Window window)
        {
            Hide();

            window.Owner = this;
            Application.Current.MainWindow = window;

            window.ShowDialog();

            Close();
        }

        #endregion
    }
}
