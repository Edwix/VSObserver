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

namespace VSObserver
{
    class VariableObserver : ViewModelBase
    {
        private DataTable variableTable;
        private ObservableCollection<DataObserver> _variableList;
        private string _searchText;

        private const string VARIABLE_LIST = "VariableList";
        private const string SEARCH_TEXT = "SearchText";

        private const string PATH = "Path";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
        private const string TIMESTAMP = "Timestamp";
        private const string REGEX_SEARCH = @"^[0-9a-zA-Z_/\-:]+$";
        private const string REGEX_REMPLACE = @"[^0-9a-zA-Z_/\-:]";
        private const int SHOW_NUMBER = 40;
        private bool connectionOK;
        private Regex reg_var;
        private DataApplication dataApp;

        public VariableObserver(DataApplication dataApp)
        {
            reg_var = new Regex(REGEX_SEARCH);
            _variableList = new ObservableCollection<DataObserver>();

            this.dataApp = dataApp;
        }

        public ObservableCollection<DataObserver> VariableList
        {
            get { return _variableList; }
            set { _variableList = value; OnPropertyChanged(VARIABLE_LIST);}
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
                    int nb = 0;
                    VariableList = readValue(value, out nb);
                    //variableCollectionViewSource.Source = vo.readValue(tb_variableName.Text, out variableNumber);
                    //changeVariableIndication();
                }
                else
                {
                    dataApp.InformationMessage = "The variable should have more than 2 characters";
                    //variableCollectionViewSource.Source = new List<DataObserver>();
                    VariableList = new ObservableCollection<DataObserver>();
                }
            }
        }

        /// <summary>
        /// Charge toutes les variables de la configuration et returne le nombre total
        /// </summary>
        /// <returns></returns>
        public int loadVariableList()
        {
            IControl control = IControl.create();
            variableTable = new DataTable();
            variableTable.Columns.Add(PATH, typeof(string));

            try
            {
                control.connect("10.23.154.180", 9090);
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
                NameList listeUT = control.getVariableList();

                if (listeUT.size() > 0)
                {
                    for (int i = 0; i < listeUT.size(); i++)
                    {
                        variableTable.Rows.Add(listeUT.get(i));
                    }
                }
            }

            return variableTable.Rows.Count;
        }

        public ObservableCollection<DataObserver> readValue(string rawVariableName, out int variableNumber)
        {
            ///On recherche le nom de la variable à travers la liste des variables
            ///Cela nous retourne plusieurs en fonction de nom entrée
            ObservableCollection<DataObserver> variableResult = new ObservableCollection<DataObserver>();
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
                DataRow[] searchResult =  variableTable.Select("Path LIKE '%" + variableName + "%'");
                variableNumber = searchResult.Count();

                ///On vérifie si on a bien une connexion à U-test
                if (connectionOK)
                {
                    int compt = 0;
                    VariableController vc = Vs.getVariableController();

                    foreach (DataRow row in searchResult)
                    {
                        string completeVariable = (string)row[PATH];
                        int importOk = vc.importVariable(completeVariable);
                        int typeVS;
                        long timeStamp;
                        vc.getType(completeVariable, out typeVS);
                        //Console.WriteLine(completeVariable + " ==> Type : " + typeVS);

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

                                                variableResult.Add(createDataObserver(completeVariable, valVarInt.ToString(), timeStamp));                                                
                                            }
                                            else
                                            {
                                                //value.Append("ERR3\n");
                                            }

                                            compt++;
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

                                                variableResult.Add(createDataObserver(completeVariable, valVarDbl.ToString("0.00000"), timeStamp));
                                            }
                                            else
                                            {
                                                //value.Append("ERR3\n");
                                            }

                                            compt++;
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

                                                variableResult.Add(createDataObserver(completeVariable, tableToString(valVarVecInt), timeStamp));
                                            }
                                            else
                                            {
                                                //value.Append("ERR3\n");
                                            }

                                            compt++;
                                        }
                                        else
                                        {
                                            //value.Append("ERR2\n");
                                        }
                                    break;
                                ///=================================================================================================
                                default:
                                    variableResult.Add(createDataObserver(completeVariable, "Undefined", 0L));
                                    break;
                            }
                        }
                        else
                        {
                            //value.Append("ERR1\n");
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

        private DataObserver createDataObserver(string path, string value, long timeStamp)
        {
            DataObserver dObs = new DataObserver {
                PathName = path,
                Path = System.IO.Path.GetDirectoryName(path).Replace("\\", "/"),
                Variable = System.IO.Path.GetFileName(path),
                Value = value,
                Timestamp = DateTime.FromFileTimeUtc(timeStamp).ToString()
            };

            return dObs;
        }

        /// <summary>
        /// Rafraichit les valeurs en fonction de l'ancien Datatable
        /// </summary>
        /// <param name="dataTable"></param>
        public void refreshValues(string variableName)
        {
            int nb;
            ObservableCollection<DataObserver> oldVariableTable = VariableList;
            ObservableCollection<DataObserver> newVariableTable = readValue(variableName, out nb);

            foreach (DataObserver newRow in newVariableTable)
            {
                foreach(DataObserver oldRow in oldVariableTable)
                {
                    if (newRow.PathName == oldRow.PathName)
                    {
                        if (newRow.Value != oldRow.Value)
                        {
                            oldRow.Value = newRow.Value;
                            oldRow.ValueHasChanged = true;
                        }
                        else
                        {
                            oldRow.ValueHasChanged = false;
                        }

                        if (newRow.Timestamp != oldRow.Timestamp)
                        {
                            oldRow.Timestamp = newRow.Timestamp;
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
    }
}
