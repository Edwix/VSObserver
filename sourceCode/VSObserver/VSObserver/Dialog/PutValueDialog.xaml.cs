using System;
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
using System.Windows.Shapes;

namespace VSObserver.Dialog
{
    /// <summary>
    /// Logique d'interaction pour PutValueDialog.xaml
    /// </summary>
    public partial class PutValueDialog : Window
    {
        private string value;
        private bool hasCanceled;

        public PutValueDialog()
        {
            InitializeComponent();

            //Affichage en premier plan
            this.Topmost = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            hasCanceled = true;
            this.Close();
        }

        private void btnValid_Click(object sender, RoutedEventArgs e)
        {
            value = tb_value.Text;
            hasCanceled = false;
            this.Close();
        }

        public string getValue()
        {
            return value;
        }

        public bool hasBeenCanceled()
        {
            return hasCanceled;
        }
    }
}
