﻿using System;
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

namespace VSObserver
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

        private const string NODE_ITEM = "Item";
        private const string NODE_VARIABLE = "Variable";
        private const string NODE_RULE_SET = "RuleSet";
        private const string NODE_SIMPLE_RULE = "SimpleRule";
        private const string NODE_COMMENT = "Comment";
        private const string ATTR_VALUE = "value";
        private const string ATTR_COLOR = "color";

        
        public const string F_VAL = "F";
        public const string W_VAL = "W";
        private const string PATH = "Path";
        private const string MAPPING = "Mapping";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
        private const string TIMESTAMP = "Timestamp";
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

        private string _fileNameLockedVar;
        private ObservableCollection<string> _listOfFileLockedVar;

        private Dictionary<string, FileRules> colorRules;

        private string _infoMsg;

        ///Commands
        private ICommand cmdCopyVar;
        private ICommand cmdSavCurLckedList;

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
            colorRules = new Dictionary<string, FileRules>();
            _listOfFileLockedVar = new ObservableCollection<string>();
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
                    //timerWaitSearch.Interval = new TimeSpan(0, 0, 0, INTERVAL_SEARCH);
                    //timerWaitSearch.Start();

                    //searchVariables();
                    /*int nb = 0;
                    VariableList = searchVariables(value, out nb);
                    VarNumberFound = nb;*/
                    //  vvariableCollectionViewSource.Source = vo.readValue(tb_variableName.Text, out variableNumber);
                    //changeVariableIndication();
                }
                else
                {
                    //dataApp.InformationMessage = "The variable should have more than 2 characters";
                    //variableCollectionViewSource.Source = new List<DataObserver>();
                    VariableList = getLockedVariables();
                }
            }
        }

        public bool SearchRegex
        {
            get { return search_regex; }
            set { search_regex = value; OnPropertyChanged(SEARCH_REGEX); }
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

            /*if (connectionOK)
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
            else
            {*/
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
            //}

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
            ObservableCollection<DataObserver> lockVars = getLockedVariables();
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
                    //variableNumber = searchResult.Count();
                    //variableNumber = listDR.Count;
                    //variableNumber = new ObservableCollection<DataObserver>(searchResult).Count;
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

                        //int typeVS = -10;
                        //vc.getType(dObs.PathName, out typeVS);
                        //dObs.Type = (VS_Type)typeVS;
                        //Console.WriteLine("TYPE SEARYCH => " + dObs.Type);
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
            vc = Vs.getVariableController();
            vc.getType(completeVariable, out typeVS);
            Console.WriteLine("readValue : " + completeVariable + " TYPE " + typeVS + " VC " + importOk);

            //Récupération du status d'une variable
           InjectionVariableStatus status = new InjectionVariableStatus();
           vc.getInjectionStatus(completeVariable, status);

           bool variableForced = false;
           if (status.state == InjectionStates.InjectionStates_IsSet)
           {
               variableForced = true;
           }
           else
           {
               variableForced = false;
           }

           //Console.WriteLine("readValue : " + completeVariable + " TYPE " + typeVS + " ==> STATUS : " + status.state.ToString() + " = " + variableForced);

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

                                dataObserver = createDataObserver(completeVariable, valVarInt.ToString(), VS_Type.INTEGER, timeStamp, mapping, variableForced);
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

                                dataObserver = createDataObserver(completeVariable, valVarDbl.ToString("0.00000"), VS_Type.DOUBLE, timeStamp, mapping, variableForced);
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

                                dataObserver = createDataObserver(completeVariable, tableToString(valVarVecInt), VS_Type.VECTOR_INTEGER, timeStamp, mapping, variableForced);
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
                        //Console.WriteLine("READVALUE DEFAULT : " + completeVariable + " TYPE " + typeVS + " VC " + importOk);
                        dataObserver = createDataObserver(completeVariable, "Undefined", VS_Type.VECTOR_DOUBLE, 0L, mapping, variableForced);
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
        /// 
        /// </summary>
        /// <param name="completeVariable"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        private string readValue2(string completeVariable, out string dateTime)
        {
            string value = "";
            int importOk = vc.importVariable(completeVariable);
            int typeVS = -1;
            long timeStamp = 0;
            //vc = Vs.getVariableController();
            vc.getType(completeVariable, out typeVS);
            Console.WriteLine("readValue : " + completeVariable + " TYPE " + typeVS + " VC " + importOk);


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
                                value = valVarInt.ToString();
                            }
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
                        value = "Undefined";
                        break;
                }
            }

            dateTime = createDateTime(timeStamp);
            return value;
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

                dObs.Timestamp = createDateTime(timeStamp);
            }

            return dObs;
        }

        public string createDateTime(long timeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            long ts = (timeStamp / 1000) + (2 * 360 * 10000);
            dtDateTime = dtDateTime.AddMilliseconds(ts);
            return dtDateTime.ToString();
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
                Timestamp = createDateTime(timeStamp)
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ObservableCollection<DataObserver> oldVariableTable = VariableList;

            if (oldVariableTable != null)
            {
                foreach (DataObserver rowObserver in oldVariableTable)
                {
                    string oldValue = rowObserver.Value;
                    //string newDateTime = "";
                    //string newValue = readValue2(rowObserver.PathName, out newDateTime);
                    //DataObserverFF dObs = readValue(rowObserver.PathName, rowObserver.Mapping);
                    DataObserver dObs = readValue3(rowObserver);

                    //We put the type because the old type is Invalid (the first loadding)
                    //rowObserver.Type = dObs.Type;


                    InjectionVariableStatus status = new InjectionVariableStatus();
                    vc.getInjectionStatus(rowObserver.PathName, status);


                    if (colorRules.ContainsKey(rowObserver.PathName))
                    {
                        if (colorRules[rowObserver.PathName].ColorRules.ContainsKey(rowObserver.Value))
                        {
                            rowObserver.Color = colorRules[rowObserver.PathName].ColorRules[rowObserver.Value];
                        }
                        else
                        {
                            rowObserver.Color = "";
                        }

                        rowObserver.CommentColor = colorRules[rowObserver.PathName].Comment;
                    }

                    if (status.state == InjectionStates.InjectionStates_IsSet)
                    {
                        rowObserver.IsForced = true;
                    }
                    else
                    {
                        rowObserver.IsForced = false;
                    }

                    /*if (!rowObserver.Value.Equals(dObs.Value) && !rowObserver.IsChanging)
                    {
                        rowObserver.Value = dObs.Value;
                        rowObserver.ValueHasChanged = true;
                    }
                    else
                    {
                        rowObserver.ValueHasChanged = false;
                    }

                    if (rowObserver.Timestamp != newDateTime)
                    {
                        rowObserver.Timestamp = newDateTime;
                    }*/
                }
            }

            sw.Stop();
            //Console.WriteLine("REFRESH TIME => " + sw.Elapsed.ToString());
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

        public void loadXMLRule(string path)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (File.Exists(path))
            {
                Console.WriteLine("LOAD XML RULE ==> " + path);
                string rulePath = @"Resources/Rule.xsd";
                colorRules.Clear();

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
                                    /*if (VariableList.Where(x => (Regex.IsMatch(x.PathName, varName, RegexOptions.IgnoreCase) || Regex.IsMatch(x.Mapping, varName, RegexOptions.IgnoreCase))).Count() > 0)
                                    {
                                        foreach (DataObserver dobs in VariableList.Where(x => (Regex.IsMatch(x.PathName, varName, RegexOptions.IgnoreCase) || Regex.IsMatch(x.Mapping, varName, RegexOptions.IgnoreCase))))
                                        {
                                            dobs.Color = 
                                        }
                                    }*/

                                    FileRules rules = new FileRules();

                                    //Loading of all rule set elements
                                    Dictionary<string, string> ruleSets = new Dictionary<string, string>();

                                    foreach (var ruleSet in item.RuleSet)
                                    {
                                        if (!ruleSets.ContainsKey(ruleSet.Attribute(ATTR_VALUE).Value))
                                        {
                                            ruleSets.Add(ruleSet.Attribute(ATTR_VALUE).Value, ruleSet.Attribute(ATTR_COLOR).Value);
                                        }
                                    }

                                    //Adding the colors in function of value on the rule object
                                    rules.ColorRules = ruleSets;
                                    //Adding comment in the rules
                                    rules.Comment = item.Comment;

                                    //Searching all variables in all variables list
                                    var source = _listOfDataObserver.AsEnumerable();
                                    var searchResult = source.Where(x => (Regex.IsMatch(x.PathName, varName, RegexOptions.IgnoreCase) || Regex.IsMatch(x.Mapping, varName, RegexOptions.IgnoreCase)));
                                    
                                    ///Creating the dictionnary with elements 
                                    foreach (DataObserver result in searchResult)
                                    {
                                        string varPathAndName = result.PathName;

                                        if (!colorRules.ContainsKey(varPathAndName))
                                        {
                                            colorRules.Add(varPathAndName, rules);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            sw.Stop();
            Console.WriteLine("END LOAD FILE : " + sw.ElapsedMilliseconds);
        }

        public void saveVariableLocked(string name)
        {
            try
            {
                string lockedListSaved = @"Resources/" + name + ".csv";

                if (!File.Exists(lockedListSaved))
                {
                    File.Create(lockedListSaved).Dispose();
                }

                FileInfo fi = new FileInfo(lockedListSaved);
                TextWriter tw = new StreamWriter(fi.Open(FileMode.Truncate));

                StringBuilder builder = new StringBuilder();

                foreach(DataObserver dobs in getLockedVariables())
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

                tw.Write(builder.ToString());
                tw.Flush();
                tw.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error : \n" + e.ToString());
            }
        }

        public void loadLockedVariables(string name)
        {
            try
            {
                string lockedListSaved = @"Resources/"+ name + ".csv";

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

                    foreach (string pathName in listOfVariables)
                    {
                        DataObserver dobs = readValue(pathName, "");
                        dobs.IsLocked = true;
                        listDobs.Add(dobs);
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
                    if (!Path.GetFileNameWithoutExtension(filePath).Equals(MainWindow.LOCKED_LIST_FILE))
                    {
                        _listOfFileLockedVar.Add(Path.GetFileNameWithoutExtension(filePath));
                    }
                }
            }

            OnPropertyChanged(LIST_FILE_LOCKED_VAR);
        }

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
                    //On remplace le nom de fichier entré pour enlevé tous mes caractères spéciaux
                    string fileName = Regex.Replace(_fileNameLockedVar, REGEX_REMPLACE_FILE, "");

                    //Sauvegarde des variable bloqués
                    saveVariableLocked(fileName);

                    //On récupère la liste pour l'afficher
                    getListLockedVarSaved();
                }
            }
        #endregion

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
