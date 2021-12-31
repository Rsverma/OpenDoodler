﻿using System;
using System.Windows;
using System.Windows.Input;

namespace OpenBoardAnim.Windows.Other
{
    /// <summary>
    /// Interaction logic for GoTo.xaml
    /// </summary>
    public partial class GoTo : Window
    {
        #region Properties

        public int Maximum { get; set; }

        public int Selected { get; set; }

        #endregion

        public GoTo(int maximum)
        {
            InitializeComponent();

            Maximum = maximum;
        }

        #region Events

        private void GoTo_OnLoaded(object sender, RoutedEventArgs e)
        {
            GoToLabel.Content = String.Format(GoToLabel.Content.ToString(), Maximum);
            NumberTextBox.Maximum = Maximum;

            NumberTextBox.SelectAll();
        }

        private void Cancel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = (int)NumberTextBox.Value;
            DialogResult = true;
        }

        private void NumberTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
                OkButton_Click(null, null);
        }

        #endregion
    }
}
