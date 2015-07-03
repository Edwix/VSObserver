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

namespace VSObserver.Dialog
{
    /// <summary>
    /// Logique d'interaction pour ColoringRulesDialog.xaml
    /// </summary>
    public partial class ColoringRulesDialog : Window
    {
        public ColoringRulesDialog()
        {
            InitializeComponent();
            this.DataContext = new ColoringRulesManager();
        }

        public void setColoringRulesManager(ColoringRulesManager manager)
        {
            if (manager != null)
            {
                this.DataContext = manager;
            }
        }
    }
}
