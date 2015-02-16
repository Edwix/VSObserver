using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSObserver
{
    /// <summary>
    /// Classe qui permet de récupérer les données brutes
    /// </summary>
    class DataObserver : ViewModelBase
    {
        private const string PATH = "Path";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
        private const string TIMESTAMP = "Timestamp";
        private const string HAS_CHANGED = "ValueHasChanged";

        private string _path;
        private string _var;
        private string _ts;
        private string _val;
        private bool _hasChanged;

        public string Path
        { 
            get { return _path; } 
            set { _path = value; OnPropertyChanged(PATH); } 
        }

        public string Variable
        {
            get { return _var; }
            set { _var = value; OnPropertyChanged(VARIABLE); }
        }

        public string Value
        {
            get { return _val; }
            set { _val = value; OnPropertyChanged(VALUE); }
        }

        public string Timestamp
        {
            get { return _ts; }
            set { _ts = value; OnPropertyChanged(Timestamp); }
        }

        public bool ValueHasChanged
        {
            get { return _hasChanged; }
            set { _hasChanged = value; OnPropertyChanged(HAS_CHANGED); }
        }
    }
}
