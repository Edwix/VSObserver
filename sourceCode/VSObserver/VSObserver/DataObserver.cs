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
        private const string PATH_NAME = "PathName";
        private const string PATH = "Path";
        private const string VARIABLE = "Variable";
        private const string TYPE = "Type";
        private const string VALUE = "Value";
        private const string TIMESTAMP = "Timestamp";
        private const string HAS_CHANGED = "ValueHasChanged";
        private const string IS_LOCKED = "IsLocked";
        private const string IS_FORCED = "IsForced";
        private const string IS_CHANGING = "IsChanging";
        private const string MAPPING = "Mapping";

        private string _pathName;
        private string _path;
        private string _var;
        private string _ts;
        private string _val;
        private string _map;
        private bool _hasChanged;
        private bool _loocked;
        private bool _isForced;
        private bool _isChanging;

        public DataObserver()
        {
            _loocked = false;
            _isForced = false;
        }

        public string PathName
        {
            get { return _pathName; }
            set { _pathName = value; OnPropertyChanged(PATH_NAME); }
        }
        
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

        public string Mapping
        {
            get { return _map; }
            set { _map = value; OnPropertyChanged(MAPPING); }
        }

        public string Timestamp
        {
            get { return _ts; }
            set { _ts = value; OnPropertyChanged(TIMESTAMP); }
        }

        public bool ValueHasChanged
        {
            get { return _hasChanged; }
            set { _hasChanged = value; OnPropertyChanged(HAS_CHANGED); }
        }

        public bool IsChanging
        {
            get { return _isChanging; }
            set { _isChanging = value; OnPropertyChanged(IS_CHANGING); }
        }
        

        public bool IsLocked
        {
            get { return _loocked; }
            set { _loocked = value; OnPropertyChanged(IS_LOCKED); }
        }

        public bool IsForced
        {
            get { return _isForced; }
            set { _isForced = value; OnPropertyChanged(IS_FORCED); }
        }
    }
}
