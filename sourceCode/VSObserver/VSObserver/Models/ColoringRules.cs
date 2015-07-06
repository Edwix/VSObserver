using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSObserver.Models
{
    public class ColoringRules : ViewModelBase, ICloneable
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

        public object Clone()
        {
            ColoringRules colorRule = (ColoringRules)MemberwiseClone();
            return colorRule;
        }
    }
}
