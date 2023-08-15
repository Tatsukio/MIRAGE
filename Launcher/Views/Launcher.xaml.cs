using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MIRAGE_Launcher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void DragMove(object sender, MouseButtonEventArgs e)
        {
            DragMove();
            if (e.ClickCount == 2) WindowState = WindowState.Minimized;
        }
    }
}
