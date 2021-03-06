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
using System.Threading;
using System.ComponentModel;
using Forms = System.Windows.Forms;
using Draw = System.Drawing;
using System.Data;
using System.Configuration;
using SysTime = System.Timers;
using VSObserver.Models;
using System.Collections;

namespace VSObserver
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string APP_NAME = "VSObserver";

        private const int INTERVAL = 250;
        private const int INTERVAL_SEARCH = 500;
        public const string LOCKED_LIST_FILE = "LockedList";

        private int refreshRate;
        private string ipAddresseRTCServer;
        private string sqlLiteDataBase;
        //Chemin du fichier avec les règles
        private string rulePath;

        private string oldClipBoardText;
        private DispatcherTimer clipBoardTimer;
        private VariableObserver vo;
        private DataApplication dataApp;
        private BackgroundWorker refreshWorker;
        private int totalNumberOfVariables = 0;
        private int number_variable;

        private DispatcherTimer timerWaitSearch;

        public MainWindow()
        {
            log.Info("Loading configuration");
            loadConfiguration();

            log.Info("initialize components");
            InitializeComponent();

            log.Info("Starting GIT synchronization");
            //On lance la synchronisation des fichiers dans un gist
                GitSync gitsync = new GitSync();
                gitsync.pushContent();

            log.Debug("initialize clipboard timer");
            //Création du timer pour récupérer la valeur du presse papier
            //Initialisation de l'ancienne valeur du presse papier
                oldClipBoardText = "";
                clipBoardTimer = new DispatcherTimer();
                clipBoardTimer.Tick += new EventHandler(clipBoardTimer_Tick);
                clipBoardTimer.Interval = new TimeSpan(0, 0, 0, 0, refreshRate);

                timerWaitSearch = new DispatcherTimer();
                timerWaitSearch.Tick += new EventHandler(timerWaitSearch_Elapsed);
                timerWaitSearch.Interval = new TimeSpan(0, 0, 0, 0, INTERVAL_SEARCH);

            //Affichage en premier de l'application
                this.Topmost = true;

            //Affichage de la fenêtre en bas à droite
                var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                this.Left = desktopWorkingArea.Right - this.Width;
                this.Top = desktopWorkingArea.Bottom - this.Height;
            
            Clipboard.Clear();
            clipBoardTimer.Start();

            log.Debug("initialize DataApplication()");
            dataApp = new DataApplication();
            img_refresh.DataContext = dataApp;
           
            
            dataApp.LoadDone = true;

            log.Debug("initialize VariableObserver()");
            vo = new VariableObserver(ipAddresseRTCServer, sqlLiteDataBase, number_variable);
            totalNumberOfVariables = vo.getNumberOfVariables();

            log.Debug("initialize changeVariableIndication()");
            changeVariableIndication();

            ///================================================================================================================================
            ///DATACONTEXT:
            ///On met le data context avec le variable observer
            ///================================================================================================================================
                tb_variableName.DataContext = vo;
                dg_variableList.DataContext = vo;
                btn_typeW.DataContext = vo;
                cb_RegexSearch.DataContext = vo;
                img_lockedList.DataContext = vo;
                btnRecord.DataContext = vo;
                pop_listLockedFiles.DataContext = vo;
            ///================================================================================================================================

            log.Debug("initialize variable supervisor");
            //Création de la tâche de fond qui va rafraichir la liste des varaibles
            refreshWorker = new BackgroundWorker();

            //Association des évènement aux méthodes à appliquer
            refreshWorker.DoWork += refreshWorker_DoWork;
            refreshWorker.RunWorkerCompleted += refreshWorker_RunWorkerCompleted;

            log.Debug("initialize GIT file watcher");
            //Création de l'objet qui va regarder le fichier de colorisation
            FileWatcher fileWatch = new FileWatcher(gitsync.getRepositoryPath() + "\\" + rulePath);
            fileWatch.setFileChangeListener(vo);

            log.Debug("initialize saved variable list");
            //Chargmement des listes de variables sauvegardé
            vo.loadVariables(LOCKED_LIST_FILE, true);

            log.Debug("initialize getListLockedVarSaved()");
            //Chargement de tous les fichiers sauvegardés qui contiennent les variable bloqués
            vo.getListLockedVarSaved();

            log.Debug("End of main thread");
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

            try
            {
                number_variable = Convert.ToInt32(ConfigurationManager.AppSettings["NumberVisibleVariable"]);
            }
            catch
            {
                number_variable = 40;
            }

            ipAddresseRTCServer = ConfigurationManager.AppSettings["IpAddrRTC"];
            sqlLiteDataBase = ConfigurationManager.AppSettings["PathDataBase"];
            rulePath = ConfigurationManager.AppSettings["PathRuleFile"];
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
                //Si on a cohcé la cas e vérifier le presse-papier, alors on le vérifie sinon on ne le fait pas
                if (cb_VerifyClipBoard.IsChecked ==  true)
                {
                    string clipBoardText = getTextFromClipBoard();

                    if (clipBoardText != "")
                    {
                        ///Suppression des espace blancs au debut et à la fin
                        tb_variableName.Text = clipBoardText.TrimStart().TrimEnd();

                        //Lorsqu'on fait un copier / coller on reaffiche la fenêtre
                        this.WindowState = System.Windows.WindowState.Normal;
                    }
                }

                if (vo != null)
                {
                    ///Raffraîchissement des valeurs des variables à chaque interval du timer
                    vo.refreshValues();
                }
                

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error :\n" + ex.ToString());
                this.Close();
            }
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
            vo.loadVariableList();
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
            totalNumberOfVariables = vo.getNumberOfVariables();

            //On redémarre le timer
            clipBoardTimer.Start();
        }

        /// <summary>
        /// Met à jour le texte qui affiche le nombre de variable trouvé sur le 
        /// nombre de variable total
        /// </summary>
        private void changeVariableIndication()
        {
            //run_varTotal.Text = totalNumberOfVariables.ToString();
            tb_variableName.TotalVariable = totalNumberOfVariables;
        }

        private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            //On arrête le reffraîchissement
            vo.stopRefreshOnSelectedElement(true);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vo.stopRefreshOnSelectedElement(false);
                vo.makeActionOnValue();
            }
        }

        public void timerWaitSearch_Elapsed(object sender, EventArgs e)
        {
            //On arrête le timer lorsqu'on à fait une recherche
            timerWaitSearch.Stop();
            vo.searchVariables();
        }

        private void tb_variableName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //On remet à zéro le timer tant qu'on écrit un texte
            timerWaitSearch.Interval = new TimeSpan(0, 0, 0, 0, INTERVAL_SEARCH);
            timerWaitSearch.Start();
        }

        private void force_ball_icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Border forceBallBorder = (Border)sender;


            if (forceBallBorder != null)
            {
                //Le tag contient le nom de la variable plus chemin
                vo.cleanupInjectionVariable(forceBallBorder.Tag.ToString());
            }
        }

        private void btn_typeW_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (vo.WritingType != null)
            {
                if (vo.WritingType.Equals(VariableObserver.F_VAL))
                {
                    vo.WritingType = VariableObserver.W_VAL;
                }
                else
                {
                    vo.WritingType = VariableObserver.F_VAL;
                }
            }
        }

        private void lockBtn_Click(object sender, RoutedEventArgs e)
        {
            vo.saveVariableLocked(LOCKED_LIST_FILE);
        }

        private void fileLockedList_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if(menuItem != null)
            {
                Console.WriteLine("MENU ITEM CLICK : " + menuItem.Header.ToString());
                //On indique dans le titre quel fichier est chargé
                this.Title = APP_NAME + " - " + menuItem.Header.ToString();
                vo.loadVariables(menuItem.Header.ToString(), true);
            }

            closePopupLockedList();
        }

        private void defaultLockedList_Click(object sender, RoutedEventArgs e)
        {
            this.Title = APP_NAME;
            vo.loadVariables(LOCKED_LIST_FILE, true);
            closePopupLockedList();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            vo.stopRefreshOnSelectedElement(false);
        }

        private void sortByName_Click(object sender, RoutedEventArgs e)
        {
            ICollectionView cvTasks = CollectionViewSource.GetDefaultView(dg_variableList.ItemsSource);
            int cellIndex = dg_variableList.CurrentCell.Column.DisplayIndex;
            string selectedColumnHeader = (string)dg_variableList.SelectedCells[cellIndex].Column.Header;
            
            if (cvTasks != null)
            {
                cvTasks.SortDescriptions.Clear();
                cvTasks.SortDescriptions.Add(new SortDescription(selectedColumnHeader, ListSortDirection.Ascending));
            }
        }

        private void showAllForcedVars_Click(object sender, RoutedEventArgs e)
        {
            this.Title = APP_NAME;
            vo.showAllForcing();
            closePopupLockedList();
        }

        /// <summary>
        /// Evènement déclenché lorsqu'on clique sur le bouton d'enregistrement des traces
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (btnRecord.IsChecked == true)
            {
                pop_infoTrace.IsOpen = true;
                pop_infoTrace.StaysOpen = true;
            }
            else
            {
                pop_infoTrace.IsOpen = false;
                pop_infoTrace.StaysOpen = false;
            }
        }

        private void img_lockedList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (pop_listLockedFiles.IsOpen == true)
            {
                pop_listLockedFiles.IsOpen = false;
            }
            else
            {
                pop_listLockedFiles.IsOpen = true;
            }
        }

        private void closePopupLockedList()
        {
            pop_listLockedFiles.IsOpen = false;
        }

        private void img_parameters_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (pop_parameters.IsOpen == true)
            {
                pop_parameters.IsOpen = false;
            }
            else
            {
                pop_parameters.IsOpen = true;
            }
        }

        /// <summary>
        /// Ce produit lorsqu'il y a une sélection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dg_variableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IList list = dg_variableList.SelectedItems;

            vo.setSelectedItems(dg_variableList.SelectedItems);
        }
    }
}