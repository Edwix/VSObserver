using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using U_TEST;
using VS;
using System.Windows;

namespace VSObserver
{
    class VariableObserver
    {
        private DataTable variableTable;
        private DataTable variableResult;

        private const string PATH = "Path";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
        private const string TIMESTAMP = "Timestamp";
        private const int SHOW_NUMBER = 40;
        private bool connectionOK;

        public VariableObserver()
        {
            variableTable = new DataTable();
            variableTable.Columns.Add(PATH, typeof(string));
            variableTable.Columns.Add(VARIABLE, typeof(string));
            variableTable.Columns.Add(TYPE, typeof(string));
            variableTable.Columns.Add(VALUE, typeof(string));

            

            loadVariableList();
        }

        public void loadVariableList()
        {
            IControl control = IControl.create();

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

        public DataTable readValue(string variableName, out int variableNumber)
        {
            ///On recherche le nom de la variable à travers la liste des variables
            ///Cela nous retourne plusieurs en fonction de nom entrée
            DataRow[] searchResult =  variableTable.Select("Path LIKE '%" + variableName.Replace("'", "") + "%'");
            variableNumber = searchResult.Count();


            variableResult = new DataTable();
            variableResult.Columns.Add(PATH, typeof(string));
            variableResult.Columns.Add(VARIABLE, typeof(string));
            variableResult.Columns.Add(VALUE, typeof(string));
            variableResult.Columns.Add(TIMESTAMP, typeof(string));
            variableResult.Clear();

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

                                            variableResult.Rows.Add(completeVariable, System.IO.Path.GetFileName(completeVariable), 
                                                valVarInt.ToString(), DateTime.FromFileTimeUtc(timeStamp).ToString());
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

                                            variableResult.Rows.Add(completeVariable, System.IO.Path.GetFileName(completeVariable), 
                                                 valVarDbl.ToString("0.00000"), DateTime.FromFileTimeUtc(timeStamp).ToString());

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

                                            variableResult.Rows.Add(completeVariable, System.IO.Path.GetFileName(completeVariable),
                                                 tableToString(valVarVecInt), DateTime.FromFileTimeUtc(timeStamp).ToString());

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
                                variableResult.Rows.Add(completeVariable, System.IO.Path.GetFileName(completeVariable), "Undefined", "");
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

            return variableResult;
        }

        /// <summary>
        /// Rafraichit les valeurs en fonction de l'ancien Datatable
        /// </summary>
        /// <param name="dataTable"></param>
        public void refreshValues(string variableName, DataTable oldVariableTable)
        {
            int nb;
            DataTable newVariableTable = readValue(variableName, out nb);

            foreach (DataRow newRow in newVariableTable.Rows)
            {
                foreach(DataRow oldRow in oldVariableTable.Rows)
                {
                    if (newRow[PATH] == oldRow[PATH])
                    {
                        if (newRow[VALUE].ToString() != oldRow[VALUE].ToString())
                        {
                            oldRow[VALUE] = newRow[VALUE];
                        }

                        if (newRow[TIMESTAMP].ToString() != oldRow[TIMESTAMP].ToString())
                        {
                            oldRow[TIMESTAMP] = newRow[TIMESTAMP];
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
