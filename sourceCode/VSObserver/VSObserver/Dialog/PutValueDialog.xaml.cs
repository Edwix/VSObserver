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
        public PutValueDialog()
        {
            InitializeComponent();

            //Affichage en premier plan
            this.Topmost = true;
        }
    }
}
