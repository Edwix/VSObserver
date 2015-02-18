using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSObserver
{
    class DataApplication : ViewModelBase
    {
        private const string LOAD_DONE = "LoadDone";
        private const string INFORMATION_MSG = "InformationMessage";

        private bool _ldone;
        private string _msg;

        public bool LoadDone
        {
            get { return _ldone; }
            set { _ldone = value; OnPropertyChanged(LOAD_DONE); }
        }

        public string InformationMessage
        {
            get { return _msg; }
            set { _msg = value; OnPropertyChanged(INFORMATION_MSG); }
        }
    }
}
