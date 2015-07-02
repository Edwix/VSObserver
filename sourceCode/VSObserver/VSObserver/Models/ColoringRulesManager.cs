using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace VSObserver.Models
{
    class ColoringRulesManager : ViewModelBase
    {
        private const string LIST_COLORINGRULES = "ListOfColoringRules";

        private ObservableCollection<ColoringRules> _listOfColoringRules;

        public ColoringRulesManager ()
        {

        }

        public ObservableCollection<ColoringRules> ListOfColoringRules
        {
            get { return _listOfColoringRules; }
            set { _listOfColoringRules = value; OnPropertyChanged(LIST_COLORINGRULES); }
        }
    }
}
