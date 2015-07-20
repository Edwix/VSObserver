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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.Windows.Input;
using VSObserver.Dialog;
using System.Configuration;
using Forms = System.Windows.Forms;
using System.Collections;

namespace VSObserver.Models
{
    class VariableObserver : ViewModelBase, IFileChange, IEqualityComparer<DataObserver>
    {
        //private DataTable variableTable;
        private ObservableCollection<DataObserver> _listOfDataObserver;
        private ObservableCollection<DataObserver> _variableList;
        private string _searchText;
        private string ipAddr;
        private string pathDataBase;
        private int _varNb;

        VariableController vc;

        private const string TRACE_FILE_DEFAULT = "Trace-VSO";

        private const string NAME_ALL_FORCING = "AllForced";

        private const string QUERY_MAPPING = "SELECT V1.Name AS Path, V2.Name AS Mapping FROM Variable AS V1, Variable AS V2 WHERE  V2.variableId = V1.parentId";
        private const string QUERY_ALL_VAR = "SELECT Name AS Path FROM Variable";

        private const string VARIABLE_LIST = "VariableList";
        private const string SEARCH_TEXT = "SearchText";
        private const string SELECTED_VARIABLE = "SelectedVariable";
        private const string VAR_NUMBER_FOUND = "VarNumberFound";
        private const string WRITING_TYPE = "WritingType";
        private const string SEARCH_REGEX = "SearchRegex";
        private const string FILE_NAME_LOCKED_LIST = "FileNameLockedList";
        private const string LIST_FILE_LOCKED_VAR = "ListOfFileLockedVar";
        private const string INFORMATION_MSG = "InformationMessage";
        private const string SEARCH_ENABLED = "SearchTextEnabled";

        public const string NODE_LIST = "List";
        public const string NODE_ITEM = "Item";
        public const string NODE_VARIABLE = "Variable";
        public const string NODE_RULE_SET = "RuleSet";
        public const string NODE_SIMPLE_RULE = "SimpleRule";
        public const string NODE_COMMENT = "Comment";
        public const string ATTR_VALUE = "value";
        public const string ATTR_COLOR = "color";
        public const string ATTR_OPERATOR = "operator";

        public const string OPERATOR_EQUAL = "eq";
        public const string OPERATOR_GREATER_THAN = "gt";
        public const string OPERATOR_LOWER_THAN = "lt";
        public const string OPERATOR_GREATER_EQUAL = "ge";
        public const string OPERATOR_LOWER_EQUAL = "le";
        
        public const string F_VAL = "F";
        public const string W_VAL = "W";
        private const string PATH = "Path";
        private const string MAPPING = "Mapping";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
        private const string TIMESTAMP = "Timestamp";
        private const string IS_RECORDING = "IsRecording";
        private const string REGEX_SEARCH = @"^[0-9a-zA-Z_/\-:\*\?]+$";
        private const string REGEX_REMPLACE = @"[^0-9a-zA-Z_/\-:\*\?]";
        private const string REGEX_REMPLACE_FILE = @"[^0-9a-zA-Z_/\-]";

        private bool connectionOK;
        private Regex reg_var;
        //private DataApplication dataApp;
        private DataObserver _selVar;
        private string _writTyp;
        private int show_number;
        private bool search_regex;
        private bool _isRecording;
        //String builder qui va enregistrer toutes les informations
        private StringBuilder strRecording;

        private string _fileNameLockedVar;
        private ObservableCollection<string> _listOfFileLockedVar;

        //Dictionnaire qui contient l'ensemble des règles de couleur
        //En fonction du chemin + nom de la variable (clé unique)
        private Dictionary<string, ColoringRulesManager> colorRulesWithPath;

        //Managère qui va gérer l'ensemble des règles de couleur
        private ColoringRulesManager managerColorRules;

        //dictionnaire qui va contenire le chemin + nom (clé unique) et le mapping associé
        private Dictionary<string, string> dic;

        private List<DataObserver> _selectedItems;

        private string _infoMsg;

        ///Commands
        private ICommand cmdCopyVar;
        private ICommand cmdSavCurLckedList;
        private ICommand cmdCopyMap;
        private ICommand cmdEditRule;
        private ICommand cmdPutValue;

        public VariableObserver(string ipAddr, string pathDataBase, int show_number)
        {
            this.ipAddr = ipAddr;
            this.pathDataBase = pathDataBase;
            reg_var = new Regex(REGEX_SEARCH);
            _variableList = new ObservableCollection<DataObserver>();
            vc = Vs.getVariableController();
            //this.dataApp = dataApp;
            _writTyp = F_VAL;
            this.show_number = show_number;
            search_regex = false;
            colorRulesWithPath = new Dictionary<string, ColoringRulesManager>();
            _listOfFileLockedVar = new ObservableCollection<string>();
            _isRecording = false;
            _selectedItems = new List<DataObserver>();
        }

        public string InformationMessage
        {
            get { return _infoMsg; }
            set { _infoMsg = value; OnPropertyChanged(INFORMATION_MSG); }
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

        public string WritingType
        {
            get { return _writTyp; }
            set { _writTyp = value; OnPropertyChanged(WRITING_TYPE); }
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

                }
                else
                {
                    VariableList = getLockedVariables();
                }
            }
        }

        public bool SearchTextEnabled
        {
            get { return !_isRecording; }
        }

        public bool SearchRegex
        {
            get { return search_regex; }
            set { search_regex = value; OnPropertyChanged(SEARCH_REGEX); }
        }

        /// <summary>
        /// To activate or deactivate the recording of variables
        /// </summary>
        public bool IsRecording
        {
            get { return _isRecording; }
            set 
            {
                _isRecording = value;

                if (_isRecording)
                {
                    strRecording = new StringBuilder();

                    //==========================================================================================================
                    //Header of trace
                    //==========================================================================================================
                        strRecording.Append("Date:" + DateTime.Now.ToString("yyyy-MM-dd") + "\n");
                        strRecording.Append("Case: ;Action location: \n");
                        strRecording.Append("Vehicle number: ;Train number: \n");
                        strRecording.Append("Set serial number: ;Set name: \n");
                        strRecording.Append("Comments: \n");
                        strRecording.Append("\nSampling period: " + ConfigurationManager.AppSettings["RefreshRate"] + ";\n");
                        strRecording.Append("\nArming date: ;Arming time: ;Arming cause: ;Function return: 0\n");
                        strRecording.Append("Triggering date: " + DateTime.Now.ToString("yyyy-MM-dd") + ";Triggering time: " + DateTime.Now.ToString("HH:mm:ss") + ";Triggering cause: Maintenance;Function return: 0\n");
                    //==========================================================================================================

                        strRecording.Append("Timestamp;");

                    foreach (DataObserver dobs in _variableList)
                    {
                        if (dobs != _variableList.Last())
                        {
                            strRecording.Append(dobs.PathName + " ((-));TS;");
                        }
                        else
                        {
                            strRecording.Append(dobs.PathName + " ((-));TS");
                        }
                    }
                }
                else
                {
                    if(strRecording != null)
                        saveVariables(TRACE_FILE_DEFAULT, strRecording.ToString());
                }

                OnPropertyChanged(IS_RECORDING);
                OnPropertyChanged(SEARCH_ENABLED);
            }
        }

        public string FileNameLockedList
        {
            get { return _fileNameLockedVar; }
            set { _fileNameLockedVar = value; OnPropertyChanged(FILE_NAME_LOCKED_LIST); }
        }

        public ObservableCollection<string> ListOfFileLockedVar
        {
            get { return _listOfFileLockedVar; }
            set { _listOfFileLockedVar = value; OnPropertyChanged(LIST_FILE_LOCKED_VAR);  }
        }

        /// <summary>
        /// On met les éléments sélectionnés dans la liste approprié pour faire un traitement
        /// </summary>
        /// <param name="selectedItems"></param>
        public void setSelectedItems(IList selectedItems)
        {
            _selectedItems.Clear();

            foreach (var dObsSelected in selectedItems)
            {
                DataObserver dObs = dObsSelected as DataObserver;

                if (dObs != null)
                {
                    _selectedItems.Add(dObs);
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
            dic = new Dictionary<string, string>();

            //Création de la connexion SQLite
            string dataSource = @"Data Source=" + pathDataBase;
            SQLiteConnection sqliteConn = new SQLiteConnection(dataSource);

            vc = Vs.getVariableController();
            IControl control = IControl.create();
            _listOfDataObserver = new ObservableCollection<DataObserver>();

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

                InformationMessage = null;
            }
            catch (Exception)
            {
                InformationMessage = "Error on SQLite data base !";
            }

            try
            {
                control.connect(this.ipAddr, 9090);
                connectionOK = true;
                InformationMessage = null;
            }
            catch (Exception)
            {
                //Connexion impossible
                connectionOK = false;
                InformationMessage = "Connection to RTC server isn't possible !";
            }

            if (connectionOK)
            {
                try
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
                                _listOfDataObserver.Add(createDataObserver(listeUT.get(i), "", VS_Type.INVALID, 0, "", false));
                            }
                            else
                            {
                                _listOfDataObserver.Add(createDataObserver(listeUT.get(i), "", VS_Type.INVALID, 0, dic[listeUT.get(i)].ToString(), false));
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    InformationMessage = "Impossible to get the list of variables !\n" + e.ToString() ;
                }
            }
            else
            {
                try
                {
                    SQLiteCommand cmdGetAll;
                    sqliteConn.Open();
                    cmdGetAll = new SQLiteCommand(QUERY_ALL_VAR, sqliteConn);

                    ///Lecture des données contenu dans la base de données SQLite
                    ///On met la clé et la valeur dans le dictionnaire
                    SQLiteDataReader reader = cmdGetAll.ExecuteReader();

                    while (reader.Read())
                    {
                        if (!dic.ContainsKey(reader[PATH].ToString()))
                            dic.Add(reader[PATH].ToString(), "");

                        ///Si la clé primaire existe déjà dans le dictionnaire alors on rajoute le mapping
                        ///Si elle n'existe pas on met un mapping vide
                        if (!dic.ContainsKey(reader[PATH].ToString()))
                        {
                            _listOfDataObserver.Add(createDataObserver(reader[PATH].ToString(), "", VS_Type.INVALID, 0, "", false));
                        }
                        else
                        {
                            _listOfDataObserver.Add(createDataObserver(reader[PATH].ToString(), "", VS_Type.INVALID, 0, dic[reader[PATH].ToString()].ToString(), false));
                        }
                    }

                    sqliteConn.Close();

                    InformationMessage = null;

                    //On met qu'on a eu une connexion puisqu'on a réussi à importé les variables du SQLite
                    connectionOK = true;
                }
                catch (Exception e)
                {
                    InformationMessage = "Error on SQLite data base !\n" + e.ToString() ;
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

                InformationMessage = null;
            }
            catch (Exception)
            {
                InformationMessage = "Error on SQLite data base !";
            }

            return _listOfDataObserver.Count;
        }

        /// <summary>
        /// Méthode qui cherche les correspondance avec la variable entrée dans la textbox
        /// elle modifie le tableau de bord avec toutes les variables trouvées
        /// </summary>
        public void searchVariables()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            ///On recherche le nom de la variable à travers la liste des variables
            ///Cela nous retourne plusieurs en fonction de nom entrée
            ObservableCollection<DataObserver> variableResult = getLockedVariables();
            int variableNumber = 0;
            string rawVariableName = _searchText;

            if (rawVariableName != null)
            {
                string variableName;

                //If we search with regex then we don't replace the raw variable
                if(!search_regex)
                {
                    //On remplace le nom de variable en entrée par une variable enlevé de tout caractère spéciaux
                    //Par exemple si on a Live%bit la fonction retourne Livebit
                    variableName = Regex.Replace(rawVariableName, REGEX_REMPLACE, "");
                }
                else
                {
                    variableName = rawVariableName;
                }

                if (!reg_var.IsMatch(rawVariableName) && !search_regex)
                {
                    InformationMessage = "The variable name has been remplaced by " + variableName + ".";
                }
                else
                {
                    InformationMessage = null;
                }

                IEnumerable<DataObserver> searchResult = null;
                var source = _listOfDataObserver;

                if (search_regex)
                {
                    bool isValidRegex = IsValidRegex(variableName);

                    if(isValidRegex)
                    {
                        InformationMessage = null;

                        ///RegexOptions.IgnoreCase allows to ignore the case when we make a search
                        searchResult = from matchI in source
                                       where Regex.IsMatch(matchI.PathName, variableName, RegexOptions.IgnoreCase) || Regex.IsMatch(matchI.Mapping, variableName, RegexOptions.IgnoreCase)
                                       select matchI;
                    }
                    else
                    {
                        InformationMessage = "The regular expression is incorrect !";
                    }
                }
                else if (reg_var.IsMatch(variableName))
                {
                    //We elaborate the regex string for a normal search
                    //We remplace * by .* means any more characthers
                    //We remplace ? by . means any characthers
                    string regexVarName = "^.*" + variableName.Replace("*", ".*").Replace('?', '.') + ".*$";

                    searchResult = source.Where(x => (Regex.IsMatch(x.PathName, regexVarName, RegexOptions.IgnoreCase) || Regex.IsMatch(x.Mapping, regexVarName, RegexOptions.IgnoreCase)));
                }


                if (searchResult != null)
                {
                    ObservableCollection<DataObserver> newListDObs = new ObservableCollection<DataObserver>(searchResult);
                    Stopwatch swResh = new Stopwatch();
                    swResh.Start();

                    variableNumber = newListDObs.Count;
                    swResh.Stop();
                    Console.WriteLine("COUNT " + swResh.Elapsed.ToString());

                    int compt = 0;

                    vc = Vs.getVariableController();

                    foreach (DataObserver dObs in newListDObs)
                    {
                        if (compt == show_number)
                        {
                            break;
                        }

                        if (!variableResult.Contains(dObs, this))
                            variableResult.Add(dObs);               

                        compt++;
                    }

                    VariableList = variableResult;
                }                
            }

            VarNumberFound = variableNumber;

            sw.Stop();
            Console.WriteLine("Search => " + sw.Elapsed.ToString());
        }

        /// <summary>
        /// Read value 3 retourne un DataObserver
        /// </summary>
        /// <param name="completeVariable"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private DataObserver readValue3(DataObserver oldDataObs)
        {
            DataObserver dObs = oldDataObs;
            string completeVariable = oldDataObs.PathName;
            int importOk = vc.importVariable(completeVariable);
            int typeVS = -1;
            long oldTimeStamp = oldDataObs.Timestamp;
            long timeStamp = 0;
            string value = "";
            //vc = Vs.getVariableController();
            vc.getType(completeVariable, out typeVS);
            //Console.WriteLine("readValue : " + completeVariable + " TYPE " + typeVS + " VC " + importOk);


            if (importOk != 0  && !oldDataObs.IsChanging)
            {
                switch (typeVS)
                {
                    ///=================================================================================================
                    /// Si le type est égal à 1 alors c'est un entier
                    ///=================================================================================================
                    case 1:
                        dObs.Type = VS_Type.INTEGER;
                        IntegerReader intr = vc.createIntegerReader(completeVariable);
                        int valVarInt;

                        if (intr != null)
                        {
                            intr.setBlocking(1 * 200);
                            VariableState t = intr.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                intr.get(out valVarInt, out timeStamp);
                                value = valVarInt.ToString();
                            }
                        }

                        break;
                    ///=================================================================================================
                    ///=================================================================================================
                    /// Si le type est égal à 2 alors c'est un double
                    ///=================================================================================================
                    case 2:
                        dObs.Type = VS_Type.DOUBLE;
                        DoubleReader dblr = vc.createDoubleReader(completeVariable);
                        double valVarDbl;

                        if (dblr != null)
                        {
                            dblr.setBlocking(1 * 200);
                            VariableState t = dblr.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                dblr.get(out valVarDbl, out timeStamp);
                                value = valVarDbl.ToString();
                            }
                        }
                        break;
                    ///=================================================================================================
                    case 3:
                        break;
                    ///=================================================================================================
                    /// Si le type est égal à 4 alors c'est un Vector Integer (Tableau d'entier)
                    ///=================================================================================================
                    case 4:
                        dObs.Type = VS_Type.VECTOR_INTEGER;
                        VectorIntegerReader vecIntReader = vc.createVectorIntegerReader(completeVariable);
                        IntegerVector valVarVecInt = new IntegerVector();

                        if (vecIntReader != null)
                        {
                            vecIntReader.setBlocking(1 * 200);
                            VariableState t = vecIntReader.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                vecIntReader.get(valVarVecInt, out timeStamp);
                                value = tableToString(valVarVecInt);
                            }
                        }
                        break;
                    ///=================================================================================================
                    default:
                        dObs.Type = VS_Type.INVALID;
                        value = "Undefined";
                        break;
                }

                if (!oldDataObs.Value.Equals(value))
                {
                    dObs.Value = value;
                    dObs.ValueHasChanged = true;
                }
                else
                {
                    dObs.ValueHasChanged = false;
                }

                dObs.Timestamp = timeStamp;
                //dObs.WhenUpdated = howManyTime(oldTimeStamp, dObs.Timestamp);
                dObs.WhenUpdated = createDateTime(dObs.Timestamp);
            }

            return dObs;
        }

        public string createDateTime(long timeStamp)
        {
            return getDateTimeWithLong(timeStamp).ToString();
        }

        public DateTime getDateTimeWithLong(long timeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            long ts = (timeStamp / 1000) + (2 * 360 * 10000);
            dtDateTime = dtDateTime.AddMilliseconds(ts);
            return dtDateTime;
        }

        public string howManyTime(long timeStamp1, long timeStamp2)
        {
            DateTime dt1 = getDateTimeWithLong(timeStamp1);
            DateTime dt2 = getDateTimeWithLong(timeStamp2);

            TimeSpan total = dt2.Subtract(dt1);
            return total.TotalSeconds + " seconds ago";
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
        private DataObserver createDataObserver(string path, string value, VS_Type type, long timeStamp, string mapping, bool forced)
        {
            DataObserver dObs = new DataObserver {
                PathName = path,
                Path = System.IO.Path.GetDirectoryName(path).Replace("\\", "/"),
                Variable = System.IO.Path.GetFileName(path),
                Value = value,
                Type = type,
                Mapping = mapping,
                IsForced = forced,
                Timestamp = timeStamp
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

            if (oldVariableTable != null)
            {
                if (_isRecording)
                {
                    //On saute une ligne à chaque raffraichissement
                    strRecording.Append("\n");
                    strRecording.Append(DateTime.Now + ";");
                }

                foreach (DataObserver rowObserver in oldVariableTable)
                {
                    string oldValue = rowObserver.Value;
                    DataObserver dObs = readValue3(rowObserver);

                    InjectionVariableStatus status = new InjectionVariableStatus();
                    vc.getInjectionStatus(rowObserver.PathName, status);

                    #region Assignation des couleurs selon les règles appliquées
                    if (colorRulesWithPath.ContainsKey(rowObserver.PathName))
                    {
                        //Création du coloring rule juste pour voir si la valeur existe bien (contains)
                        ColoringRules colRule = new ColoringRules();
                        colRule.Value = rowObserver.Value;

                        //On récupère le manager des règles de couleurs
                        ColoringRulesManager managColor = colorRulesWithPath[rowObserver.PathName];

                        if (managColor != null)
                        {
                            double value = 0;
                            bool valueConverted = false;
                                
                            //================================================================================================
                            // Conversion de la valeur de la variable en double
                            //================================================================================================
                                try
                                {
                                    value = Convert.ToDouble(rowObserver.Value);
                                    valueConverted = true;
                                }
                                catch
                                {

                                }
                            //================================================================================================

                            //On vérifie si la valeur a bien été convetie
                            if (valueConverted)
                            {
                                //================================================================================================
                                // Recherche et assignation des règles de coleurs
                                //================================================================================================
                                    foreach (ColoringRules coloringRule in managColor.ListOfColoringRules)
                                    {
                                        string colorRule = coloringRule.Color;
                                        string operatorRule = coloringRule.Operator;

                                        double valueRule = 0;
                                        bool valueRuleConverted = false;
                                
                                        try
                                        {
                                            valueRule = Convert.ToDouble(coloringRule.Value);
                                            valueRuleConverted = true;
                                        }
                                        catch
                                        {

                                        }

                                        if (!String.IsNullOrEmpty(operatorRule) && valueRuleConverted)
                                        {
                                            ///On va vérifier l'opérateur.
                                            ///En fonction on vérifie la valeur
                                            ///Si elle correpond alors on lui applique la valeur
                                            ///On arrête l'opération dès qu'on a une correspondance
                                            if(operatorRule.Equals(OPERATOR_GREATER_EQUAL))
                                            {
                                                if (value >= valueRule)
                                                {
                                                    rowObserver.Color = colorRule;
                                                    break;
                                                }
                                            }
                                            else if(operatorRule.Equals(OPERATOR_GREATER_THAN))
                                            {
                                                if (value > valueRule)
                                                {
                                                    rowObserver.Color = colorRule;
                                                    break;
                                                }
                                            }
                                            else if(operatorRule.Equals(OPERATOR_LOWER_EQUAL))
                                            {
                                                if (value <= valueRule)
                                                {
                                                    rowObserver.Color = colorRule;
                                                    break;
                                                }
                                            }
                                            else if(operatorRule.Equals(OPERATOR_LOWER_THAN))
                                            {
                                                if (value < valueRule)
                                                {
                                                    rowObserver.Color = colorRule;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                if (valueRule == value)
                                                {
                                                    rowObserver.Color = colorRule;
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            rowObserver.Color = "";
                                        }
                                    }
                                //================================================================================================
                            }
                            else
                            {
                                rowObserver.Color = "";
                            }

                            //On met le commentaire dans la variable spécifié
                            rowObserver.CommentColor = managColor.RuleComment;
                        }
                    }
                    #endregion

                    if (status.state == InjectionStates.InjectionStates_IsSet)
                    {
                        rowObserver.IsForced = true;
                    }
                    else
                    {
                        rowObserver.IsForced = false;
                    }

                    if (_isRecording)
                    {
                        if (rowObserver != oldVariableTable.Last())
                        {
                            strRecording.Append(dObs.Value + ";" + dObs.WhenUpdated +";");
                        }
                        else
                        {
                            strRecording.Append(dObs.Value + ";" + dObs.WhenUpdated );
                        }
                    }
                }
            }
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

        #region Action sur les variables (Forcing / Writing)
            /// <summary>
            /// On fait soit un forçage ou soit une écriture si le texte commence avec =
            /// </summary>
            public void makeActionOnValue()
            {
                if (SelectedVariable != null && _writTyp != null)
                {
                    if (WritingType.Equals(F_VAL))
                    {
                       forceSelectedVariable();
                    }
                    else
                    {
                        writeSelectedVariable();
                    }
                }
            }

            public void makeActionOnValue(DataObserver dObs, string value)
            {
                if (SelectedVariable != null && _writTyp != null)
                {
                    if (WritingType.Equals(F_VAL))
                    {
                        forceVariable(dObs.PathName, value);
                    }
                    else
                    {
                        writeVariable(dObs.PathName, dObs.Type, value);
                    }
                }
            }

            public void stopRefreshOnSelectedElement(bool stop)
            {
                SelectedVariable.IsChanging = stop;
            }

            private void forceSelectedVariable()
            {
                MapStrStr mSI = new MapStrStr();
                mSI["value"] = SelectedVariable.Value;
                vc.configureInjection(SelectedVariable.PathName, "FixedValue", mSI);
                vc.waitForInjection(SelectedVariable.PathName, 10);
            }
        
            /// <summary>
            /// Force une variable en fonction de chemin et de sa valeur
            /// </summary>
            /// <param name="pathName"></param>
            /// <param name="value"></param>
            private void forceVariable(string pathName, string value)
            {
                MapStrStr mSI = new MapStrStr();
                mSI["value"] = value;
                vc.configureInjection(pathName, "FixedValue", mSI);
                vc.waitForInjection(pathName, 10);
            }

            private void writeSelectedVariable()
            {
                //Pour l'instant on ne peut qu'écrire un entier
                try
                {
                    //On remplace le = par un espace qu'on suprime par la suite
                    string strVal = _selVar.Value;

                    switch (_selVar.Type)
                    {
                        case VS_Type.INTEGER:
                                int valInt = Convert.ToInt32(strVal);
                                IntegerWriter iw = vc.createIntegerWriter(SelectedVariable.PathName);
                                iw.set(valInt);
                            break;
                        case VS_Type.DOUBLE:
                            double valDbl = Convert.ToDouble(strVal.Replace('.', ','));
                            DoubleWriter id = vc.createDoubleWriter(SelectedVariable.PathName);
                            id.set(valDbl);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROOR : " + e.ToString());
                }
            }

            /// <summary>
            /// Ecriture d'une variable en fonction de chemin de son type et de sa valeur
            /// </summary>
            /// <param name="pathName"></param>
            /// <param name="type"></param>
            /// <param name="value"></param>
            private void writeVariable(string pathName, VS_Type type, string value)
            {
                //Pour l'instant on ne peut qu'écrire un entier
                try
                {
                    //On remplace le = par un espace qu'on suprime par la suite
                    string strVal = value;

                    switch (type)
                    {
                        case VS_Type.INTEGER:
                            int valInt = Convert.ToInt32(strVal);
                            IntegerWriter iw = vc.createIntegerWriter(pathName);
                            iw.set(valInt);
                            break;
                        case VS_Type.DOUBLE:
                            double valDbl = Convert.ToDouble(strVal.Replace('.', ','));
                            DoubleWriter id = vc.createDoubleWriter(pathName);
                            id.set(valDbl);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROOR : " + e.ToString());
                }
            }
        #endregion

        public void cleanupInjectionSelectedVariable(string pathName)
        {
            vc.cleanupInjection(pathName);
        }

        private static bool IsValidRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;

            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        #region Load the rules from XML. This action is performed when the XML file has been changed
            public void loadXMLRule(string path)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                if (File.Exists(path))
                {
                    Console.WriteLine("LOAD XML RULE ==> " + path);
                    string rulePath = @"Resources/Rule.xsd";
                    colorRulesWithPath.Clear();

                    if(File.Exists(rulePath))
                    {
                        bool errors = false;
                        XDocument xmlDoc = null;

                        try
                        {
                            xmlDoc = XDocument.Load(path);
                        }
                        catch (Exception)
                        {
                            errors = true;
                        }

                        if (errors != true && xmlDoc != null)
                        {
                            XmlSchemaSet schemas = new XmlSchemaSet();
                            schemas.Add("", rulePath);

                            //We are validating the format from XML file
                            xmlDoc.Validate(schemas, (o, e) =>
                            {
                                Console.WriteLine("Error: {0}", e.Message);
                                errors = true;
                            });

                            Console.WriteLine("doc1 {0}", errors ? "did not validate" : "validated");

                            if (errors != true)
                            {
                                //Parsing the XML file
                                var itemsXML = from items in xmlDoc.Descendants(NODE_ITEM)
                                               select new
                                               {
                                                   VariableName = items.Element(NODE_VARIABLE).Value,
                                                   Comment = (string)items.Element(NODE_COMMENT),
                                                   RuleSet = items.Element(NODE_RULE_SET).Descendants(NODE_SIMPLE_RULE)
                                               };

                                foreach (var item in itemsXML)
                                {
                                    string varName = item.VariableName;

                                    //We check whether the variable is a correct regex
                                    if (IsValidRegex(varName))
                                    {

                                        ObservableCollection<ColoringRules> listColorRules = new ObservableCollection<ColoringRules>();
                                        managerColorRules = new ColoringRulesManager();

                                        foreach (var ruleSet in item.RuleSet)
                                        {
                                            ColoringRules colorRule = new ColoringRules();
                                            colorRule.Value = ruleSet.Attribute(ATTR_VALUE).Value;
                                            colorRule.Color = ruleSet.Attribute(ATTR_COLOR).Value;

                                            string operatorRule = (string)ruleSet.Attribute(ATTR_OPERATOR);

                                            ///Si la variable operatorRule est null, 
                                            ///alors ça veut dire que le champ operatro n'existe pas 
                                            ///donc on le met par défaut à equal
                                            if (operatorRule == null)
                                            {
                                                colorRule.Operator = OPERATOR_EQUAL;
                                            }
                                            else
                                            {
                                                colorRule.Operator = operatorRule;
                                            }

                                            listColorRules.Add(colorRule);
                                        }

                                        //Adding the colors in function of value on the rule object
                                        managerColorRules.ListOfColoringRules = listColorRules;

                                        //Adding comment in the rules
                                        managerColorRules.RuleComment = item.Comment;

                                        //Ajout de l'expression régulière dans le manager
                                        managerColorRules.RuleRegex = varName;

                                        //Searching all variables in all variables list
                                        var source = _listOfDataObserver.AsEnumerable();
                                        var searchResult = source.Where(x => (Regex.IsMatch(x.PathName, varName, RegexOptions.IgnoreCase) || Regex.IsMatch(x.Mapping, varName, RegexOptions.IgnoreCase)));
                                        ObservableCollection<DataObserver> _resSearh = new ObservableCollection<DataObserver>(searchResult);

                                        ///Creating the dictionnary with elements 
                                        foreach (DataObserver result in _resSearh)
                                        {
                                            string varPathAndName = result.PathName;

                                            if (!colorRulesWithPath.ContainsKey(varPathAndName))
                                            {
                                                colorRulesWithPath.Add(varPathAndName, managerColorRules);
                                            }
                                        }
                                   }
                                }
                            }
                        }
                    }
                }

                sw.Stop();
                Console.WriteLine("END LOAD FILE : " + sw.Elapsed.ToString());
            }
        #endregion

        #region Locked Variables managment
        /// <summary>
            /// Sauvegarde toutes les variables bloquées
            /// </summary>
            /// <param name="name"></param>
            public void saveVariableLocked(string name)
            {
                try
                {
                    StringBuilder builder = new StringBuilder();

                    if (_variableList != null)
                    {
                        //Si la liste de variable contient plus d'un élément alors
                        //on rajoute le ; sinon on laisse juste le nom
                        if (_variableList.Count > 0)
                            builder.Append(name + ";");
                        else
                            builder.Append(name);

                        foreach (DataObserver dobs in getLockedVariables())
                        {
                            if (dobs.PathName != getLockedVariables().Last().PathName)
                            {
                                builder.Append(dobs.PathName + ";");
                            }
                            else
                            {
                                builder.Append(dobs.PathName);
                            }
                        }

                        saveVariables(name, builder.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error : \n" + e.ToString());
                }
            }

            /// <summary>
            /// Méthode qui permet de charger toutes les variables même les non bloqués
            /// </summary>
            /// <param name="name"></param>
            public void saveAllVariable(string name)
            {
                try
                {
                    StringBuilder builder = new StringBuilder();

                    if (_variableList != null)
                    {
                        //Si la liste de variable contient plus d'un élément alors
                        //on rajoute le ; sinon on laisse juste le nom
                        if (_variableList.Count > 0)
                            builder.Append(name + ";");
                        else
                            builder.Append(name);

                        foreach (DataObserver dobs in _variableList)
                        {
                            if (dobs.PathName != _variableList.Last().PathName)
                            {
                                builder.Append(dobs.PathName + ";");
                            }
                            else
                            {
                                builder.Append(dobs.PathName);
                            }
                        }

                        saveVariables(name, builder.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error : \n" + e.ToString());
                }
            }

            /// <summary>
            /// Méthode qui permet de déterminer si il y a des variables qui ne sont pas bloqué
            /// </summary>
            /// <returns></returns>
            public bool hasUnlockedVariable()
            {
                bool unlocledVar = false;

                int nbUnlockedVar = _variableList.Where(x => x.IsLocked == false).Count();

                if (nbUnlockedVar > 0)
                    unlocledVar = true;

                return unlocledVar;
            }

            /// <summary>
            /// Sauvegarde des variables dans un fichier CSV
            /// </summary>
            /// <param name="name"></param>
            /// <param name="content"></param>
            public void saveVariables(string name, string content)
            {
                try
                {
                    //On remplace le nom de fichier entré pour enlevé tous mes caractères spéciaux
                    string fileName = Regex.Replace(name, REGEX_REMPLACE_FILE, "");

                    string lockedListSaved = @"Resources/" + fileName + ".csv";

                    if (!File.Exists(lockedListSaved))
                    {
                        File.Create(lockedListSaved).Dispose();
                    }

                    FileInfo fi = new FileInfo(lockedListSaved);
                    TextWriter tw = new StreamWriter(fi.Open(FileMode.Truncate));

                    tw.Write(content);
                    tw.Flush();
                    tw.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error : \n" + e.ToString());
                }
            }

            public void loadVariables(string name, bool isLocked)
            {
                try
                {
                    //On remplace le nom de fichier entré pour enlevé tous mes caractères spéciaux
                    string fileName = Regex.Replace(name, REGEX_REMPLACE_FILE, "");

                    string lockedListSaved = @"Resources/" + fileName + ".csv";

                    if (File.Exists(lockedListSaved))
                    {
                        StreamReader reader = new StreamReader(File.OpenRead(lockedListSaved));
                        StringBuilder readString = new StringBuilder();

                        while (!reader.EndOfStream)
                        {
                            readString.Append(reader.ReadLine());
                        }

                        string[] listOfVariables = readString.ToString().Split(';');
                        ObservableCollection<DataObserver> listDobs = new ObservableCollection<DataObserver>();

                        //Le i permet d'exclure le nom du fichier
                        //et ainsi ne pas le mettre dans la liste des variables
                        int i = 0;
                        foreach (string pathName in listOfVariables)
                        {
                            if (i != 0)
                            {
                                string mapping = "";

                                if (dic.ContainsKey(pathName))
                                    mapping = dic[pathName];

                                DataObserver dobs = createDataObserver(pathName, "", VS_Type.INVALID, 0, mapping, false);
                                dobs.IsLocked = isLocked;
                                listDobs.Add(dobs);
                            }

                            i++;
                        }

                        VariableList = listDobs;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error load : \n" + e.ToString());
                }
            }

            /// <summary>
            /// Get the list of file with locked variables (.csv)
            /// </summary>
            public void getListLockedVarSaved()
            {
                _listOfFileLockedVar.Clear();
                string[] listFilePath = Directory.GetFiles(@"Resources/");

                foreach (string filePath in listFilePath)
                {
                    if (Path.GetExtension(filePath).Equals(".csv"))
                    {
                        //Si c'est different de la liste de sauvegarde par défaut, alors on ajoute les fichiers trouvées
                        if (!Path.GetFileNameWithoutExtension(filePath).Equals(MainWindow.LOCKED_LIST_FILE)
                            && !Path.GetFileNameWithoutExtension(filePath).Equals(NAME_ALL_FORCING)
                            && !Path.GetFileNameWithoutExtension(filePath).Equals(TRACE_FILE_DEFAULT))
                        {
                            StreamReader reader = new StreamReader(File.OpenRead(filePath));
                            StringBuilder readString = new StringBuilder();

                            while (!reader.EndOfStream)
                            {
                                readString.Append(reader.ReadLine());
                            }

                            _listOfFileLockedVar.Add(readString.ToString().Split(';')[0]);
                        }
                    }
                }

                OnPropertyChanged(LIST_FILE_LOCKED_VAR);
            }
        #endregion

        #region Commands
            public ICommand CopyVariable
            {
                get
                {
                    if (this.cmdCopyVar == null)
                        this.cmdCopyVar = new RelayCommand(() => copyVariable(), () => true);

                    return cmdCopyVar;
                }
            }

            private void copyVariable()
            {
                try
                {
                    if(SelectedVariable != null)
                    {
                        string pathNameVar = SelectedVariable.PathName;
                        Clipboard.SetText(pathNameVar);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error copy variable : \n" + e.ToString());
                }
            }

            public ICommand SaveCurrentLockedList
            {
                get
                {
                    if (this.cmdSavCurLckedList == null)
                        this.cmdSavCurLckedList = new RelayCommand(() => saveCurrentLockedList(), () => true);

                    return cmdSavCurLckedList;
                }
            }

            private void saveCurrentLockedList()
            {
                if (_fileNameLockedVar != "")
                {
                    if (hasUnlockedVariable())
                    {
                        Forms.DialogResult result1 = Forms.MessageBox.Show("Do you want to save the unlocked variable ?",
                                                                             "Important Question",
                                                                             Forms.MessageBoxButtons.YesNo);
                        if (result1 == Forms.DialogResult.Yes)
                        {
                            saveAllVariable(_fileNameLockedVar);
                        }
                        else
                        {
                            saveVariableLocked(_fileNameLockedVar);
                        }
                    }
                    else
                    {
                        //Sauvegarde des variable bloqués
                        saveVariableLocked(_fileNameLockedVar);
                    }

                    //On récupère la liste pour l'afficher
                    getListLockedVarSaved();
                }
            }

            public ICommand CopyMapping
            {
                get
                {
                    if (this.cmdCopyMap == null)
                        this.cmdCopyMap= new RelayCommand(() => copyMapping(), () => true);

                    return cmdCopyMap;
                }
            }

            private void copyMapping()
            {
                try
                {
                    if (SelectedVariable != null)
                    {
                        string map = SelectedVariable.Mapping;
                        Clipboard.SetText(map);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error copy variable : \n" + e.ToString());
                }
            }

            public ICommand EditColoringRule
            {
                get
                {
                    if (this.cmdEditRule == null)
                        this.cmdEditRule = new RelayCommand(() => showEditRuleDialog(), () => hasSelectedVariable());

                    return cmdEditRule;
                }
            }

            private void showEditRuleDialog()
            {
                try
                {
                    ColoringRulesDialog colorRuleDial = new ColoringRulesDialog();
                    string pathName = SelectedVariable.PathName;

                    if (colorRulesWithPath.ContainsKey(pathName))
                    {
                        //On passe une copie pour pouvoir faire un cancel
                        colorRuleDial.setColoringRulesManager((ColoringRulesManager)colorRulesWithPath[pathName].Clone());
                    }

                    colorRuleDial.ShowDialog();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error on editing rules : \n" + e.ToString());
                }
            }

            public bool hasSelectedVariable()
            {
                if (SelectedVariable != null)
                    return true;
                else
                    return false;
            }

            public ICommand PutValue
            {
                get
                {
                    if (this.cmdPutValue == null)
                        this.cmdPutValue = new RelayCommand(() => putValue(), () => canPutValue());

                    return cmdPutValue;
                }
            }

            private void putValue()
            {
                PutValueDialog dialog = new PutValueDialog();
                dialog.ShowDialog();

                //Si le dialogue n'a pas été annulé alors on peut forcer / écrire toute les variables
                if (!dialog.hasBeenCanceled())
                {
                    string value = dialog.getValue();

                    foreach (DataObserver dObs in _selectedItems)
                    {
                        makeActionOnValue(dObs, value);
                    }
                }
            }

            private bool canPutValue()
            {
                bool putValueIsOk = false;

                if (_selectedItems != null)
                {
                    //On peut mettre une valeur via la boite de dialogue
                    //uniquement si il y a au moins deux éléments selectionnés
                    if (_selectedItems.Count > 1)
                    {
                        putValueIsOk = true;
                    }
                }

                return putValueIsOk;
            }
        #endregion

        public void showAllForcing()
        {
            if (_listOfDataObserver != null)
            {
                if (_listOfDataObserver.Count > 0)
                {
                    StringBuilder builder = new StringBuilder();

                    ObservableCollection<DataObserver> listDobs = new ObservableCollection<DataObserver>();

                    foreach (DataObserver rowObserver in _listOfDataObserver)
                    {
                        InjectionVariableStatus status = new InjectionVariableStatus();
                        vc.getInjectionStatus(rowObserver.PathName, status);

                        //On regarde si le status correspond au forçage
                        if (status.state == InjectionStates.InjectionStates_IsSet)
                        {
                            rowObserver.IsForced = true;
                            builder.Append(rowObserver.PathName + ";");
                            listDobs.Add(rowObserver);
                        }
                        else
                        {
                            rowObserver.IsForced = false;
                        }
                    }

                    VarNumberFound = listDobs.Count;

                    //Affichage des variables forcés
                    VariableList = listDobs;

                    //Save all variable in a csv file
                    saveVariables(NAME_ALL_FORCING, builder.ToString().TrimEnd(';'));
                }
            }
        }

        /// <summary>
        /// Permet de comparer le chemin de deux DataObserver
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(DataObserver x, DataObserver y)
        {
            if (x == null || y == null)
                return false;

            return x.PathName.Equals(y.PathName);
        }

        public int GetHashCode(DataObserver obj)
        {
            //throw new NotImplementedException();
            if (obj == null)
                return 0;

            return obj.GetHashCode();
        }
    }
}
