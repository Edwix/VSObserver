using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using U_TEST;
using VS;
using System.Windows;
using System.Text.RegularExpressions;

namespace VSObserver
{
    class VariableObserver
    {
        private DataTable variableTable;
        private List<DataObserver> variableResult;

        private const string PATH = "Path";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
        private const string TIMESTAMP = "Timestamp";
        private const string REGEX_SEARCH = @"[^0-9a-zA-Z_/\-:]+";
        private const int SHOW_NUMBER = 40;
        private bool connectionOK;
        private Regex reg_var;

        public VariableObserver()
        {
            reg_var = new Regex(REGEX_SEARCH);

            loadVariableList();
        }

        public void loadVariableList()
        {
            IControl control = IControl.create();
            variableTable = new DataTable();
            variableTable.Columns.Add(PATH, typeof(string));
            variableTable.Columns.Add(VARIABLE, typeof(string));
            variableTable.Columns.Add(TYPE, typeof(string));
            variableTable.Columns.Add(VALUE, typeof(string));

            try
            {
                control.connect("10.23.154.180", 9090);
                connectionOK = true;
            }
            catch (Exception)
            {
                //Connexion impossible
                connectionOK = false;
            }

            if (connectionOK)
            {
                NameList listeUT = control.getVariableList();

                if (listeUT.size() > 0)
                {
                    for (int i = 0; i < listeUT.size(); i++)
                    {
                        variableTable.Rows.Add(listeUT.get(i), "", "", "");
                    }
                }
            }
        }

        public List<DataObserver> readValue(string variableName, out int variableNumber)
        {
            ///On recherche le nom de la variable à travers la liste des variables
            ///Cela nous retourne plusieurs en fonction de nom entrée
            variableResult = new List<DataObserver>();
            variableNumber = 0;

            ///Si la regex ne match pas alors on cherche les variable
            ///La regex interdit tous les caractères normal
            ///Donc en faisant l'inverse de cette Regex, on ignore tous les caractères spéciaux
            if(!reg_var.Match(variableName).Success)
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

                                                variableResult.Add(new DataObserver{
                                                        Path = completeVariable,
                                                        Variable = System.IO.Path.GetFileName(completeVariable),
                                                        Value = valVarInt.ToString(),
                                                        Timestamp = timeStamp.ToString()
                                                });
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

                                                variableResult.Add(new DataObserver
                                                {
                                                    Path = completeVariable,
                                                    Variable = System.IO.Path.GetFileName(completeVariable),
                                                    Value = valVarDbl.ToString("0.00000"),
                                                    Timestamp = DateTime.FromFileTimeUtc(timeStamp).ToString()
                                                });

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

                                                variableResult.Add(new DataObserver
                                                {
                                                    Path = completeVariable,
                                                    Variable = System.IO.Path.GetFileName(completeVariable),
                                                    Value = tableToString(valVarVecInt),
                                                    Timestamp = DateTime.FromFileTimeUtc(timeStamp).ToString()
                                                });

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
                                    //Console.WriteLine(completeVariable + " ==> Type : " + typeVS);
                                    variableResult.Add(new DataObserver
                                    {
                                        Path = completeVariable,
                                        Variable = System.IO.Path.GetFileName(completeVariable),
                                        Value = "Undefined",
                                        Timestamp = ""
                                    });
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

        /// <summary>
        /// Rafraichit les valeurs en fonction de l'ancien Datatable
        /// </summary>
        /// <param name="dataTable"></param>
        public void refreshValues(string variableName, List<DataObserver> oldVariableTable)
        {
            int nb;
            List<DataObserver> newVariableTable = readValue(variableName, out nb);

            foreach (DataObserver newRow in newVariableTable)
            {
                foreach(DataObserver oldRow in oldVariableTable)
                {
                    if (newRow.Path == oldRow.Path)
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
