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
        private const string PATH = "Path";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
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

        public string readValue(string variableName, out int variableNumber)
        {
            ///On recherche le nom de la variable à travers la liste des variables
            ///Cela nous retourne plusieurs en fonction de nom entrée
            DataRow[] searchResult =  variableTable.Select("Path LIKE '%" + variableName + "%'");
            variableNumber = searchResult.Count();

            StringBuilder value = new StringBuilder();

            ///On vérifie si on a bien une connexion à U-test
            if (connectionOK)
            {
                int compt = 0;

                foreach (DataRow row in searchResult)
                {
                    string completeVariable = (string)row[PATH];
                    VariableController vc = Vs.getVariableController();
                    int importOk = vc.importVariable(completeVariable, VS_Type.INTEGER);

                    if (importOk != 0)
                    {
                        IntegerReader intr = vc.createIntegerReader(completeVariable);
                        int valVar;
                        long timeStamp;
                        if (intr != null)
                        {
                            intr.setBlocking(1 * 200);
                            VariableState t = intr.waitForConnection();

                            if (t == VariableState.Ok)
                            {
                                intr.get(out valVar, out timeStamp);

                                value.Append(completeVariable + " | " + valVar.ToString() + "\n");
                            }
                            else
                            {
                                value.Append("ERR3\n");
                            }

                            compt++;
                        }
                        else
                        {
                            value.Append("ERR2\n");
                        }
                    }
                    else
                    {
                        value.Append("ERR1\n");
                    }

                    //Si on a atteint le nombre d'affichage max on arrête la boucle
                    if (compt == SHOW_NUMBER)
                    {
                        break;
                    }
                }
            }

            return value.ToString();
        }
    }
}
