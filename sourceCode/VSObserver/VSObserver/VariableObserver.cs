using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using U_TEST;
using VS;
using System.Windows;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Data.Common;
using System.Timers;
using System.ComponentModel;

namespace VSObserver
{
    class VariableObserver : ViewModelBase
    {
        private DataTable variableTable;
        private ObservableCollection<DataObserver> _variableList;
        private string _searchText;
        private string ipAddr;
        private string pathDataBase;
        private int _varNb;

        VariableController vc;

        private const string QUERY_MAPPING = "SELECT V1.Name AS Path, V2.Name AS Mapping FROM Variable AS V1, Variable AS V2 WHERE  V2.variableId = V1.parentId";

        private const string VARIABLE_LIST = "VariableList";
        private const string SEARCH_TEXT = "SearchText";
        private const string SELECTED_VARIABLE = "SelectedVariable";
        private const string VAR_NUMBER_FOUND = "VarNumberFound";

        private const string PATH = "Path";
        private const string MAPPING = "Mapping";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
        private const string TIMESTAMP = "Timestamp";
        private const string REGEX_SEARCH = @"^[0-9a-zA-Z_/\-:]+$";
        private const string REGEX_REMPLACE = @"[^0-9a-zA-Z_/\-:]";
        private const int SHOW_NUMBER = 40;
        private const int INTERVAL_SEARCH = 500;
        private bool connectionOK;
        private Regex reg_var;
        private DataApplication dataApp;
        private DataObserver _selVar;

        private Timer timerWaitSearch;

        public VariableObserver(DataApplication dataApp, string ipAddr, string pathDataBase)
        {
            this.ipAddr = ipAddr;
            this.pathDataBase = pathDataBase;
            reg_var = new Regex(REGEX_SEARCH);
            _variableList = new ObservableCollection<DataObserver>();
            vc = Vs.getVariableController();
            this.dataApp = dataApp;
            timerWaitSearch = new Timer(INTERVAL_SEARCH);
            timerWaitSearch.Elapsed += new ElapsedEventHandler(timerWaitSearch_Elapsed);
        }

        public int VarNumberFound
        {
            get { return _varNb; }
            set { _varNb = value; OnPropertyChanged(VAR_NUMBER_FOUND); }    
        }

        public ObservableCollection<DataObserver> VariableList
        {
            get { return _variableList; }
            set { _variableList = value; OnPropertyChanged(VARIABLE_LIST);}
        }

        public DataObserver SelectedVariable
        {
            get { return _selVar; }
            set { _selVar = value; OnPropertyChanged(SELECTED_VARIABLE); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set 
            { 
                _searchText = value; 
                OnPropertyChanged(SEARCH_TEXT);

                if (_searchText != "" && _searchText.Length >= 3)
                {
                    //timerWaitSearch.Stop();
                    //timerWaitSearch.Start();

                    int nb = 0;
                    VariableList = searchVariables(value, out nb);
                    VarNumberFound = nb;
                    //  vvariableCollectionViewSource.Source = vo.readValue(tb_variableName.Text, out variableNumber);
                    //changeVariableIndication();
                }
                else
                {
                    dataApp.InformationMessage = "The variable should have more than 2 characters";
                    //variableCollectionViewSource.Source = new List<DataObserver>();
                    VariableList = getLockedVariables();
                }
            }
        }

        /// <summary>
        /// Charge toutes les variables de la configuration et returne le nombre total
        /// </summary>
        /// <returns></returns>
        public int loadVariableList()
        {
            //On crée un dictionnaire qui va contenire le chemin + nom (clé unique) et le mapping associé
            Dictionary<string, string> dic = new Dictionary<string, string>();

            //Création de la connexion SQLite
            string dataSource = @"Data Source=" + pathDataBase;
            SQLiteConnection sqliteConn = new SQLiteConnection(dataSource);

            vc = Vs.getVariableController();
            IControl control = IControl.create();
            variableTable = new DataTable();
            variableTable.Columns.Add(PATH, typeof(string));
            variableTable.Columns.Add(MAPPING, typeof(string));

            variableTable.PrimaryKey = new DataColumn[] { variableTable.Columns[PATH] };

            try
            {
                SQLiteCommand cmd;
                sqliteConn.Open();
                cmd = new SQLiteCommand(QUERY_MAPPING, sqliteConn);

                
                StringBuilder sb = new StringBuilder();

                ///Lecture des données contenu dans la base de données SQLite
                ///On met la clé et la valeur dans le dictionnaire
                SQLiteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    if (!dic.ContainsKey(reader[PATH].ToString()))
                        dic.Add(reader[PATH].ToString(), reader[MAPPING].ToString());
                }

                sqliteConn.Close();

                dataApp.InformationMessage = null;
            }
            catch (Exception)
            {
                dataApp.InformationMessage = "Error on SQLite data base !";
            }

            try
            {
                control.connect(this.ipAddr, 9090);
                connectionOK = true;
                dataApp.InformationMessage = null;
            }
            catch (Exception)
            {
                //Connexion impossible
                connectionOK = false;
                dataApp.InformationMessage = "Connection to RTC server isn't possible !";
            }

            if (connectionOK)
            {
                ///Récupération de toutes les variables U-test
                NameList listeUT = control.getVariableList();

                if (listeUT.size() > 0)
                {                    
                    for (int i = 0; i < listeUT.size(); i++)
                    {
                        ///Si la clé primaire existe déjà dans le dictionnaire alors on rajoute le mapping
                        ///Si elle n'existe pas on met un mapping vide
                        if (!dic.ContainsKey(listeUT.get(i)))
                        {
                            variableTable.Rows.Add(listeUT.get(i), "");
                        }
                        else
                        {
                            variableTable.Rows.Add(listeUT.get(i), dic[listeUT.get(i)].ToString());
                        }
                    }
                }
            }

            try
            {
                SQLiteCommand cmd;
                sqliteConn.Open();
                cmd = new SQLiteCommand(QUERY_MAPPING, sqliteConn);
                
                SQLiteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    //On vérifie si la clé n'existe pas
                   if(!dic.ContainsKey(reader[PATH].ToString()))
                        dic.Add(reader[PATH].ToString(), reader[MAPPING].ToString());
                }

                sqliteConn.Close();

                dataApp.InformationMessage = null;
            }
            catch (Exception)
            {
                dataApp.InformationMessage = "Error on SQLite data base !";
            }

            return variableTable.Rows.Count;
        }

        /// <summary>
        /// Méthode qui cherche les correspondance avec la variable entrée dans la textbox
        /// elle retourne un tableau avec toutes les variable trouvés
        /// </summary>
        /// <param name="rawVariableName"></param>
        /// <param name="variableNumber"></param>
        /// <returns></returns>
        public ObservableCollection<DataObserver> searchVariables(string rawVariableName, out int variableNumber)
        {
            ///On recherche le nom de la variable à travers la liste des variables
            ///Cela nous retourne plusieurs en fonction de nom entrée
            ObservableCollection<DataObserver> lockVars = getLockedVariables();
            ObservableCollection<DataObserver> variableResult = getLockedVariables();
            variableNumber = 0;

            //On remplace le nom de variable en entrée par une variable enlevé de tout caractère spéciaux
            //Par exemple si on a Live%bit la fonction retourne Livebit
            string variableName = Regex.Replace(rawVariableName, REGEX_REMPLACE, "");

            if (!reg_var.IsMatch(rawVariableName))
            {
                dataApp.InformationMessage = "The variable name has been remplaced by " + variableName + ".";
            }
            else
            {
                dataApp.InformationMessage = null;
            }

            ///Si la regex ne match pas alors on cherche les variable
            ///La regex interdit tous les caractères spéciaux
            if(reg_var.IsMatch(variableName))
            {
                DataRow[] searchResult =  variableTable.Select("Path LIKE '%" + variableName + "%' OR Mapping LIKE '%" + variableName + "%'");
                variableNumber = searchResult.Count();

                ///On vérifie si on a bien une connexion à U-test
                if (connectionOK)
                {
                    int compt = 0;
                    
                    foreach (DataRow row in searchResult)
                    {
                        string completeVariable = (string)row[PATH];
                        string mapping = (string)row[MAPPING];

                        //La lecture de variable retourne un DataObserver avec toutes les informations
                        DataObserver dobs = readValue(completeVariable, mapping);

                        //Si c'est différent que null ça veut dire qu'on à réussit à trouver un observer
                        //Et si le tableau des variables blocké ne contient pas l'élément on l'ajoute
                        //Cela permet d'éviter des doublons
                        if (dobs != null && !containsDatatObserver(lockVars, dobs))
                        {
                            variableResult.Add(dobs);
                            compt++;
                        }

                        //Si on a atteint le nombre d'affichage max on arrête la boucle
                        if (compt == SHOW_NUMBER)
                        {
                            break;
                        }
                    }
                }
            }

            return variableResult;
        }

        /// <summary>
        /// Lecture d'une variable VS. Cette méthode retourne un DataObserver avec tous les 
        /// paramètres de la variable VS (nom et chemin, nom, valeur timestamp...)
        /// </summary>
        /// <param name="completeVariable"></param>
        /// <returns></returns>
        private DataObserver readValue(string completeVariable, string mapping)
        {
            DataObserver dataObserver = null;
            int importOk = vc.importVariable(completeVariable);
            int typeVS = -1;
            long timeStamp;
            vc.getType(completeVariable, out typeVS);

            //Récupération du status d'une variable
            InjectionVariableStatus status = new InjectionVariableStatus();
            vc.getInjectionStatus(completeVariable, status);

            Console.WriteLine(completeVariable +  " TYPE " + typeVS + " ==> STATUS : " + status.state.ToString());

            if (importOk != 0)
            {
                switch (typeVS)
                {
                    ///=================================================================================================
                    /// Si le type est égal à 1 alors c'est un entier
                    ///=================================================================================================
                    case 1:
                        IntegerReader intr = vc.createIntegerReader(completeVariable);
                        int valVarInt;

                        if (intr != null)
                        {
                            intr.setBlocking(1 * 200);
                            VariableState t = intr.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                intr.get(out valVarInt, out timeStamp);

                                dataObserver = createDataObserver(completeVariable, valVarInt.ToString(), timeStamp, mapping);
                            }
                            else
                            {
                                //value.Append("ERR3\n");
                            }
                        }
                        else
                        {
                            //value.Append("ERR2\n");
                        }
                        break;
                    ///=================================================================================================
                    ///=================================================================================================
                    /// Si le type est égal à 2 alors c'est un double
                    ///=================================================================================================
                    case 2:
                        DoubleReader dblr = vc.createDoubleReader(completeVariable);
                        double valVarDbl;

                        if (dblr != null)
                        {
                            dblr.setBlocking(1 * 200);
                            VariableState t = dblr.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                dblr.get(out valVarDbl, out timeStamp);

                                dataObserver = createDataObserver(completeVariable, valVarDbl.ToString("0.00000"), timeStamp, mapping);
                            }
                            else
                            {
                                //value.Append("ERR3\n");
                            }
                        }
                        else
                        {
                            //value.Append("ERR2\n");
                        }
                        break;
                    ///=================================================================================================
                    case 3:
                        break;
                    ///=================================================================================================
                    /// Si le type est égal à 4 alors c'est un Vector Integer (Tableau d'entier)
                    ///=================================================================================================
                    case 4:
                        VectorIntegerReader vecIntReader = vc.createVectorIntegerReader(completeVariable);
                        IntegerVector valVarVecInt = new IntegerVector();

                        if (vecIntReader != null)
                        {
                            vecIntReader.setBlocking(1 * 200);
                            VariableState t = vecIntReader.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                vecIntReader.get(valVarVecInt, out timeStamp);

                                dataObserver = createDataObserver(completeVariable, tableToString(valVarVecInt), timeStamp, mapping);
                            }
                            else
                            {
                                //value.Append("ERR3\n");
                            }
                        }
                        else
                        {
                            //value.Append("ERR2\n");
                        }
                        break;
                    ///=================================================================================================
                    default:
                        dataObserver = createDataObserver(completeVariable, "Undefined", 0L, mapping);
                        break;
                }
            }
            else
            {
                //value.Append("ERR1\n");
            }

            return dataObserver;
        }

        /// <summary>
        /// Fonction qui permet de voir si une liste de DataObserver contient un élément DataObserver
        /// </summary>
        /// <param name="listOfDobs"></param>
        /// <param name="dObs"></param>
        /// <returns></returns>
        private bool containsDatatObserver(ObservableCollection<DataObserver> listOfDobs, DataObserver dObs)
        {
            bool containObserver = false;

            foreach (DataObserver observer in listOfDobs)
            {
                ///Si on les deux DataObserver on le même nom, ça veut dire que le tableau le contient
                if (observer.PathName == dObs.PathName)
                {
                    containObserver = true;
                    //On s'arrête car pas besoin d'utiliser de l'espace mémoire en plus
                    //Puisque on à trouvé notre correspondance
                    break;
                }
            }

            return containObserver;
        }

        /// <summary>
        /// Méthode qui permet de créer un DataObser à partir des données de VS
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        private DataObserver createDataObserver(string path, string value, long timeStamp, string mapping)
        {
            DataObserver dObs = new DataObserver {
                PathName = path,
                Path = System.IO.Path.GetDirectoryName(path).Replace("\\", "/"),
                Variable = System.IO.Path.GetFileName(path),
                Value = value,
                Mapping = mapping,
                Timestamp = DateTime.FromFileTimeUtc(timeStamp).ToString()
            };

            return dObs;
        }

        /// <summary>
        /// Méthode qui récupère la liste des variables bloqués
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<DataObserver> getLockedVariables()
        {
            ObservableCollection<DataObserver> lockedVars = new ObservableCollection<DataObserver>();

            foreach (DataObserver dObs in VariableList)
            {
                if (dObs.IsLocked)
                {
                    lockedVars.Add(dObs);
                }
            }

            return lockedVars;
        }

        /// <summary>
        /// Rafraichit les valeurs. On ne fait que lire les valeurs existante dans la liste de variable
        /// </summary>
        public void refreshValues()
        {
            ObservableCollection<DataObserver> oldVariableTable = VariableList;

            foreach(DataObserver rowObserver in oldVariableTable)
            {
                string oldValue = rowObserver.Value;
                DataObserver dObs = readValue(rowObserver.PathName, rowObserver.Mapping);

                if (dObs != null)
                {
                    if (rowObserver.Value != dObs.Value && !rowObserver.IsChanging)
                    {
                        rowObserver.Value = dObs.Value;
                        rowObserver.ValueHasChanged = true;
                    }
                    else
                    {
                        rowObserver.ValueHasChanged = false;
                    }

                    if (rowObserver.Timestamp != dObs.Timestamp)
                    {
                        rowObserver.Timestamp = dObs.Timestamp;
                    }
                }
            }
        }

        public void forceValue(DataObserver variable)
        {
            /*MapStrStr mapStr = new MapStrStr();
            mapStr.*/
            vc.configureInjection(variable.PathName, "Force", null);
        }

        private string tableToString(IntegerVector vector)
        {
            StringBuilder sb = new StringBuilder();
            sb.Clear();
            sb.Append("[");

            int i = 0;
            foreach (int value in vector)
            {
                if (i != vector.Count)
                {
                    sb.Append(value + ",");
                }
                else
                {
                    sb.Append(value);
                }

                i++;
            }

            sb.Append("]");
            return sb.ToString();
        }

        public void stopRefreshOnSelectedElement(bool stop)
        {
            SelectedVariable.IsChanging = stop;
        }

        public void forceSelectedVariable()
        {
            MapStrStr mSI = new MapStrStr();
            mSI["value"] = SelectedVariable.Value;
            vc.configureInjection(SelectedVariable.PathName, "FixedValue", mSI);
            vc.waitForInjection(SelectedVariable.PathName, 1);
            
            /*POUR l'écriture
             * IntegerWriter iw = vc.createIntegerWriter(SelectedVariable.PathName);
            iw.set(Convert.ToInt32(SelectedVariable.Value));*/
        }

        public void timerWaitSearch_Elapsed(object sender, ElapsedEventArgs e)
        {
            int varNb = 0;
            VariableList = searchVariables(SearchText, out varNb);
            VarNumberFound = varNb;
        }
    }
}
