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

namespace VSObserver
{
    /// <summary>
    /// Suivez les étapes 1a ou 1b puis 2 pour utiliser ce contrôle personnalisé dans un fichier XAML.
    ///
    /// Étape 1a) Utilisation de ce contrôle personnalisé dans un fichier XAML qui existe dans le projet actif.
    /// Ajoutez cet attribut XmlNamespace à l'élément racine du fichier de balisage où il doit 
    /// être utilisé :
    ///
    ///     xmlns:MyNamespace="clr-namespace:VSObserver"
    ///
    ///
    /// Étape 1b) Utilisation de ce contrôle personnalisé dans un fichier XAML qui existe dans un autre projet.
    /// Ajoutez cet attribut XmlNamespace à l'élément racine du fichier de balisage où il doit 
    /// être utilisé :
    ///
    ///     xmlns:MyNamespace="clr-namespace:VSObserver;assembly=VSObserver"
    ///
    /// Vous devrez également ajouter une référence du projet contenant le fichier XAML
    /// à ce projet et régénérer pour éviter des erreurs de compilation :
    ///
    ///     Cliquez avec le bouton droit sur le projet cible dans l'Explorateur de solutions, puis sur
    ///     "Ajouter une référence"->"Projets"->[Recherchez et sélectionnez ce projet]
    ///
    ///
    /// Étape 2)
    /// Utilisez à présent votre contrôle dans le fichier XAML.
    ///
    ///     <MyNamespace:VSOTextBox/>
    ///
    /// </summary>
    public class VSOTextBox : TextBox
    {
        static VSOTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VSOTextBox), new FrameworkPropertyMetadata(typeof(VSOTextBox)));
        }

        /// <summary>
        /// Registers a dependency property as backing store for the Content property
        /// </summary>
        public static readonly DependencyProperty NbElementProperty =
            DependencyProperty.Register("NbElement", typeof(int), typeof(VSOTextBox), new PropertyMetadata(0));

        /// <summary>
        /// Nombre d'éléments trouvés
        /// </summary>
        public int NbElement
        {
            get { return (int)GetValue(NbElementProperty); }
            set { SetValue(NbElementProperty, value); }
        }


        // <summary>
        /// Registers a dependency property as backing store for the Content property
        /// </summary>
        public static readonly DependencyProperty TotalVariableProperty =
            DependencyProperty.Register("TotalVariable", typeof(int), typeof(VSOTextBox), new PropertyMetadata(0));

        /// <summary>
        /// Nombre maximum de variable
        /// </summary>
        public int TotalVariable
        {
            get { return (int)GetValue(TotalVariableProperty); }
            set { SetValue(TotalVariableProperty, value); }
        }

        // <summary>
        /// Registers a dependency property as backing store for the Content property
        /// </summary>
        public static readonly DependencyProperty ShowNumberResultProperty =
            DependencyProperty.Register("ShowNumberResult", typeof(bool), typeof(VSOTextBox), new PropertyMetadata(true));

        /// <summary>
        /// Nombre maximum de variable
        /// </summary>
        public bool ShowNumberResult
        {
            get { return (bool)GetValue(ShowNumberResultProperty); }
            set { SetValue(ShowNumberResultProperty, value); }
        }

        // <summary>
        /// Registers a dependency property as backing store for the Content property
        /// </summary>
        public static readonly DependencyProperty SearchErrorProperty =
            DependencyProperty.Register("SearchError", typeof(string), typeof(VSOTextBox), new PropertyMetadata(""));

        /// <summary>
        /// Error text when there is on the search
        /// </summary>
        public string SearchError
        {
            get { return (string)GetValue(SearchErrorProperty); }
            set 
            { 
                SetValue(SearchErrorProperty, value);
                if (value == "" || value == null)
                {
                    HasError = false;
                }
                else
                {
                    HasError = true;
                }
            }
        }

        public bool HasError
        {
            get;
            set;
        }
    }
}
