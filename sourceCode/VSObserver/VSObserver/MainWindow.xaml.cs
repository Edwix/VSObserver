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
using System.Threading;
using System.ComponentModel;
using Forms = System.Windows.Forms;
using Draw = System.Drawing;
using System.Data;
using System.Configuration;

namespace VSObserver
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private const int INTERVAL = 250;

        private int refreshRate;
        private string ipAddresseRTCServer;
        private string sqlLiteDataBase;

        private string oldClipBoardText;
        private DispatcherTimer clipBoardTimer;
        private VariableObserver vo;
        private Forms.NotifyIcon notifyIcon;
        private DataApplication dataApp;
        private BackgroundWorker refreshWorker;
        private int totalNumberOfVariables = 0;

        public MainWindow()
        {
            loadConfiguration();
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
                clipBoardTimer.Interval = new TimeSpan(0, 0, 0, 0, refreshRate);

            //Affichage en premier de l'application
                this.Topmost = true;

            //Affichage de la fenêtre en bas à droite
                var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                this.Left = desktopWorkingArea.Right - this.Width;
                this.Top = desktopWorkingArea.Bottom - this.Height;

            clipBoardTimer.Start();
            Clipboard.Clear();


            dataApp = new DataApplication();
            img_refresh.DataContext = dataApp;
            tbl_message.DataContext = dataApp;
            
            dataApp.LoadDone = true;

            vo = new VariableObserver(dataApp, ipAddresseRTCServer, sqlLiteDataBase);
            totalNumberOfVariables = vo.loadVariableList();

            //Datacontext pour lier le nombre de variable trouvés
            tbl_varNumber.DataContext = vo;
            changeVariableIndication();

            tb_variableName.DataContext = vo;
            dg_variableList.DataContext = vo;

            //Création de la tâche de fond qui va rafraichir la liste des varaibles
            refreshWorker = new BackgroundWorker();

            //Association des évènement aux méthodes à appliquer
            refreshWorker.DoWork += refreshWorker_DoWork;
            refreshWorker.RunWorkerCompleted += refreshWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// Charge les données rentrées dans le fichier de configuration
        /// En même temps cette fonction vérifie que les paramètres rentrés soient correctes
        /// Sinon on prend une valeur par défaut
        /// </summary>
        public void loadConfiguration()
        {
            try
            {
                refreshRate = Convert.ToInt32(ConfigurationManager.AppSettings["RefreshRate"]);
            }
            catch
            {
                refreshRate = 250;
            }

            try
            {
                byte R = Convert.ToByte(ConfigurationManager.AppSettings["RedValue"]);
                byte G = Convert.ToByte(ConfigurationManager.AppSettings["GreenValue"]);
                byte B = Convert.ToByte(ConfigurationManager.AppSettings["BlueValue"]);
                this.Resources.Add("colorAnim_valueChanged", Color.FromArgb(255, R, G, B));
            }
            catch
            {
                this.Resources.Add("colorAnim_valueChanged", Colors.LightBlue);
            }

            try
            {
                int sec = Convert.ToInt32(ConfigurationManager.AppSettings["DurationColor"]);
                this.Resources.Add("durationColor", new Duration(new TimeSpan(0,0,sec)));
            }
            catch
            {
                this.Resources.Add("durationColor", new Duration(new TimeSpan(0, 0, 2)));
            }

            ipAddresseRTCServer = ConfigurationManager.AppSettings["IpAddrRTC"];
            sqlLiteDataBase = ConfigurationManager.AppSettings["PathDataBase"];
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

                ///Raffraîchissement des valeurs des variables à chaque interval du timer
                vo.refreshValues();
                

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

        private void refresh_ClickDown(object sender, MouseButtonEventArgs e)
        {
            //On dit que le chargement des variable n'est pas fini
            //Le bouton refresh va commencer à trouner
            dataApp.LoadDone = false;

            if (!refreshWorker.IsBusy)
            {
                //On arrête le timer
                clipBoardTimer.Stop();

                //On desactive la saisie de variable
                tb_variableName.IsEnabled = false;

                //On lance la tâche asynchrone refreshWorker_DoWork
                refreshWorker.RunWorkerAsync();

            }
        }

        /// <summary>
        /// Evènement qui réalise une opération longue
        /// En l'occurence on charge toutes la liste des variables
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            totalNumberOfVariables = vo.loadVariableList();
        }

        /// <summary>
        /// Evènement qui se déclenche lorsque la tâche asynchrone (BackgoundWorker) à terminer
        /// En l'occurance on met à True LoadDone pour indiquer que l'on peut arrêter la rotation
        /// de l'icône de rafarichissement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataApp.LoadDone = true;
            changeVariableIndication();
            tb_variableName.IsEnabled = true;

            //On redémarre le timer
            clipBoardTimer.Start();
        }

        /// <summary>
        /// Met à jour le texte qui affiche le nombre de variable trouvé sur le 
        /// nombre de variable total
        /// </summary>
        private void changeVariableIndication()
        {
            run_varTotal.Text = totalNumberOfVariables.ToString();
        }

        private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            vo.stopRefreshOnSelectedElement(true);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vo.stopRefreshOnSelectedElement(false);
                vo.forceSelectedVariable();
            }
        }
    }
}