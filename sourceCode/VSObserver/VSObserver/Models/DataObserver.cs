using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VS;

namespace VSObserver.Models
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
        private const string VALUEF = "ValueF";
        private const string TIMESTAMP = "Timestamp";
        private const string HAS_CHANGED = "ValueHasChanged";
        private const string IS_LOCKED = "IsLocked";
        private const string IS_FORCED = "IsForced";
        private const string IS_CHANGING = "IsChanging";
        private const string MAPPING = "Mapping";
        private const string COLOR = "Color";
        private const string COMMENT_COLOR = "CommentColor";
        private const string WHEN_UPDATED = "WhenUpdated";

        private string _pathName;
        private string _path;
        private string _var;
        private long _ts;
        private string _val;
        private string _valF;
        private string _map;
        private string _color;
        private string _commentColor;
        private string _wUpdated;
        private bool _hasChanged;
        private bool _loocked;
        private bool _isForced;
        private bool _isChanging;
        private VS_Type _type;

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

        public string ValueF
        {
            get { return _valF; }
            set { _valF = value; OnPropertyChanged(VALUEF); }
        }

        public string Mapping
        {
            get { return _map; }
            set { _map = value; OnPropertyChanged(MAPPING); }
        }

        public string WhenUpdated
        {
            get { return _wUpdated; }
            set { _wUpdated = value; OnPropertyChanged(WHEN_UPDATED); }
        }

        public long Timestamp
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

        public VS_Type Type
        {
            get { return _type; }
            set { _type = value; OnPropertyChanged(TYPE); }
        }
        

        public bool IsLocked
        {
            get { return _loocked; }
            set 
            { _loocked = value; OnPropertyChanged(IS_LOCKED); }
        }

        public bool IsForced
        {
            get { return _isForced; }
            set { _isForced = value; OnPropertyChanged(IS_FORCED); }
        }

        public string Color
        {
            get { return _color; }
            set { _color = value; OnPropertyChanged(COLOR); }
        }

        public string CommentColor
        {
            get { return _commentColor; }
            set { _commentColor = value; OnPropertyChanged(COMMENT_COLOR); }
        }
    }
}
