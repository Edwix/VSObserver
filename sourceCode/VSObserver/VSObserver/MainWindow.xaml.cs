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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Draw = System.Drawing;
using System.Data;

namespace VSObserver
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int INTERVAL = 250;

        private string oldClipBoardText;
        private DispatcherTimer clipBoardTimer;
        private VariableObserver vo;
        private Forms.NotifyIcon notifyIcon;
        private CollectionViewSource variableCollectionViewSource;

        public MainWindow()
        {
            InitializeComponent();

            //Icone de notification
                notifyIcon = new Forms.NotifyIcon();
                this.notifyIcon.Text = "VSObserver";
                notifyIcon.Icon = Properties.Resources.VSObserver_Icon;
                this.notifyIcon.Visible = true;
            

            //Création du timer pour récupérer la valeur du presse papier
            //Initialisation de l'ancienne valeur du presse papier
                oldClipBoardText = "";
                clipBoardTimer = new DispatcherTimer();
                clipBoardTimer.Tick += new EventHandler(clipBoardTimer_Tick);
                clipBoardTimer.Interval = new TimeSpan(0, 0, 0, 0, INTERVAL);

            this.Hide();

            //Affichage en premier de l'application
                this.Topmost = true;

            //Affichage de la fenêtre en bas à droite
                var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                this.Left = desktopWorkingArea.Right - this.Width;
                this.Top = desktopWorkingArea.Bottom - this.Height;

            clipBoardTimer.Start();
            Clipboard.Clear();

            variableCollectionViewSource = (CollectionViewSource)(FindResource("VariableCollectionViewSource"));

            vo = new VariableObserver();
        }

        /// <summary>
        /// Récupère le dernier texte du presse-papier
        /// Si le texte est différent de l'ancienne copie
        /// Sinon ça retourne une chaine vide
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// A chaque intervale cette méthode est déclenché
        /// Elle permet de mettre le texte récupérer du presse-papier dans une textbox et d'afficher le dialogue
        /// Par contre si le texte du presse-papier retourne une chaine vide, alors on n'affiche pas le dialogue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clipBoardTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                string clipBoardText = getTextFromClipBoard();

                if (clipBoardText != "")
                {
                    ///Suppression des espace blancs au debut et à la fin
                    tb_variableName.Text = clipBoardText.TrimStart().TrimEnd();
                    this.Show();
                }

                ///Raffraîchissement des variables toutes les secondes (si on a une variable)
                ///On affiche les variable aussi si la variable copier à une longeur supérieur à 3
                if (tb_variableName.Text != "" && tb_variableName.Text.Length >= 3)
                {
                    
                    //VariableList.ItemsSource = vo.readValue(tb_variableName.Text, out variableNumber).DefaultView;
                    //int nb;
                    //variableCollectionViewSource.Source = vo.readValue(tb_variableName.Text, out nb);
                    vo.refreshValues(tb_variableName.Text, (List<DataObserver>)variableCollectionViewSource.Source);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error :\n" + ex.ToString());
                this.Close();
            }
        }

        /// <summary>
        /// Cache la fenêtre du programme lorsqu'on clique sur le bouton hide
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_hideDialog_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void tb_variableName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tb_variableName.Text != "" && tb_variableName.Text.Length >= 3)
            {
                int variableNumber = 0;
                variableCollectionViewSource.Source = vo.readValue(tb_variableName.Text, out variableNumber);
                tbl_varNumber.Text = "Variables number : " + variableNumber.ToString();
            }
        }
    }
}