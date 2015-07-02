using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSObserver.Models
{
    class ColoringRules : ViewModelBase
    {
        private const string VALUE = "Value";
        private const string COLOR = "Color";

        private string _val;
        private string _col;

        public string Value
        {
            get { return _val; }
            set { _val = value; OnPropertyChanged(VALUE); }
        }

        public string Color
        {
            get { return _col; }
            set { _col = value; OnPropertyChanged(COLOR); }
        }
    }
}
