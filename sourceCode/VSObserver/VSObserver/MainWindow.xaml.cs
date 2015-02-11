﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace VSObserver
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int SECOND_INTERVAL = 1;

        private string oldClipBoardText;
        private DispatcherTimer clipBoardTimer;

        public MainWindow()
        {
            InitializeComponent();

            oldClipBoardText = "";
            clipBoardTimer = new DispatcherTimer();
            clipBoardTimer.Tick += new EventHandler(clipBoardTimer_Tick);
            clipBoardTimer.Interval = new TimeSpan(0, 0, SECOND_INTERVAL);

            this.Hide();

            //Affichage en premier de l'application
            this.Topmost = true;

            //Affichage de la fenêtre en bas à droite
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;

            clipBoardTimer.Start();
            Clipboard.Clear();
        }

        public string getTextFromClipBoard()
        {
            string clipBoardText = "";

            if (Clipboard.ContainsText())
            {
                string tempText = Clipboard.GetText(System.Windows.TextDataFormat.Text);

                if (tempText != oldClipBoardText)
                {
                    clipBoardText = tempText;
                    oldClipBoardText = tempText;
                }
            }

            return clipBoardText;
        }

        private void clipBoardTimer_Tick(object sender, EventArgs e)
        {
            string clipBoardText = getTextFromClipBoard();

            if (clipBoardText != "")
            {
                tb_variableName.Text = clipBoardText;
                this.Show();
            }
        }

        private void btn_hideDialog_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}