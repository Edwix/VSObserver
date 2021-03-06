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
        ColoringRulesManager colorManager;

        public ColoringRulesDialog(string rulePath)
        {
            InitializeComponent();

            //Affichage en premier plan
            this.Topmost = true;

            this.rulePath = rulePath;
            colorManager = new ColoringRulesManager();
            colorManager.setRulePath(rulePath);

            this.DataContext = colorManager;
        }

        public void setColoringRulesManager(ColoringRulesManager manager)
        {
            if (manager != null)
            {
                manager.setRulePath(rulePath);
                manager.setOldRegex(manager.RuleRegex);
                manager.setRuleExist(true);                
                this.DataContext = manager;
                colorManager = manager;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            closeWindow();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            closeWindow();
        }

        private void closeWindow()
        {
            this.Close();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Button btnDel = sender as Button;

            if (btnDel != null)
            {
                //The tag has a ColoringRules object, so we return it to delete it
                //Le champ "Tag" du bouton contient un objet "ColoringRules", donc on le retourne pour qu'il soit supprimé
                colorManager.removeColoringRule((ColoringRules)btnDel.Tag);
            }
        }
    }
}
