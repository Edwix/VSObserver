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
using VSObserver.Models;
using System.Configuration;

namespace VSObserver.Dialog
{
    /// <summary>
    /// Logique d'interaction pour ColoringRulesDialog.xaml
    /// </summary>
    public partial class ColoringRulesDialog : Window
    {
        string rulePath;

        public ColoringRulesDialog()
        {
            InitializeComponent();
            ColoringRulesManager colorManager = new ColoringRulesManager();
            rulePath = ConfigurationManager.AppSettings["PathRuleFile"];
            colorManager.setRulePath(rulePath);

            this.DataContext = colorManager;
        }

        public void setColoringRulesManager(ColoringRulesManager manager)
        {
            if (manager != null)
            {
                manager.setRulePath(rulePath);
                manager.setRuleExist(true);
                this.DataContext = manager;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
