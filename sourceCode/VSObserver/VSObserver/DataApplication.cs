using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSObserver
{
    class DataApplication : ViewModelBase
    {
        private const string LOAD_DONE = "LoadDone";

        private bool _ldone;

        public bool LoadDone
        {
            get { return _ldone; }
            set { _ldone = value; OnPropertyChanged(LOAD_DONE); }
        }
    }
}
