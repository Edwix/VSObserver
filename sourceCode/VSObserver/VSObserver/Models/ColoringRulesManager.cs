using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VSObserver.Models
{
    public class ColoringRulesManager : ViewModelBase, IEqualityComparer<ColoringRules>
    {
        private const string LIST_COLORINGRULES = "ListOfColoringRules";
        private const string RULE_REGEX = "RuleRegex";
        private const string REGEX_ERROR = "RegexError";
        private const string RULE_COMMENT = "RuleComment";

        private ObservableCollection<ColoringRules> _listOfColoringRules;

        private string _regex;
        private string _comment;
        private string _regexErr;

        private string rulePath;
        private bool ruleExist;

        private ICommand cmdAddColRul;
        private ICommand cmdSaveRul;

        public ColoringRulesManager ()
        {
            _listOfColoringRules = new ObservableCollection<ColoringRules>();
            ruleExist = false;
        }

        public ObservableCollection<ColoringRules> ListOfColoringRules
        {
            get { return _listOfColoringRules; }
            set { _listOfColoringRules = value; OnPropertyChanged(LIST_COLORINGRULES); }
        }

        public string RuleRegex
        {
            get { return _regex; }
            set { _regex = value; OnPropertyChanged(REGEX_ERROR); OnPropertyChanged(RULE_REGEX); }
        }

        public string RuleComment
        {
            get { return _comment; }
            set { _comment = value; OnPropertyChanged(RULE_COMMENT); }
        }

        public string RegexError
        {
            get 
            {
                bool isValid = IsValidRegex2(_regex);

                if (!isValid)
                    _regexErr = "The regular expression isn't correct !";
                else
                    _regexErr = null;

                return _regexErr; 
            }
        }

        public void setRulePath(string rulePath)
        {
            this.rulePath = rulePath;
        }

        public void setRuleExist(bool ruleExist)
        {
            this.ruleExist = ruleExist;
        }

        public ICommand AddColoringRule
        {
            get
            {
                if (this.cmdAddColRul == null)
                    this.cmdAddColRul = new RelayCommand(() => addColoringRule(), () => true);

                return cmdAddColRul;
            }
        }

        public void addColoringRule()
        {
            if (_listOfColoringRules != null)
            {
                _listOfColoringRules.Add(new ColoringRules());
            }
        }

        private static bool IsValidRegex(string pattern)
        {
            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        public ICommand SaveRules
        {
            get
            {
                if (this.cmdSaveRul == null)
                    this.cmdSaveRul = new RelayCommand(() => saveColoringRule(), () => canSaveRules());

                return cmdSaveRul;
            }
        }

        public void saveColoringRule()
        {
            if (!String.IsNullOrEmpty(rulePath))
            {
                if (ruleExist)
                {
                    XDocument xmlFile = XDocument.Load(rulePath);

                    var query = from items in xmlFile.Elements(VariableObserver.NODE_ITEM)
                                where items.Element(VariableObserver.NODE_VARIABLE).Value == RuleRegex
                                select items;

                    foreach (XElement item in query)
                    {
                        foreach (XElement simpleRule in item.Elements(VariableObserver.NODE_SIMPLE_RULE))
                        {

                        }
                    }

                    xmlFile.Save(rulePath);
                }
                else
                {

                }
            }
        }

        public bool canSaveRules()
        {
            bool isOk = false;

            if (!String.IsNullOrEmpty(RuleRegex))
            {
                if (String.IsNullOrEmpty(RegexError) && ListOfColoringRules.Count > 0)
                {
                    if (!String.IsNullOrEmpty(ListOfColoringRules.First().Value) &&
                        !String.IsNullOrEmpty(ListOfColoringRules.First().Color))
                    {
                        isOk = true;
                    }
                }
            }            

            return isOk;
        }

        private static bool IsValidRegex2(string pattern)
        {
            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Permet de comparer deux valeur dans le Coloring Rule
        /// Ainsi on peut savoir si la valeur existe déjà
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(ColoringRules x, ColoringRules y)
        {
            if (x == null || y == null)
                return false;

            return x.Value.Equals(y.Value);
        }

        public int GetHashCode(ColoringRules obj)
        {
            //throw new NotImplementedException();
            if (obj == null)
                return 0;

            return obj.GetHashCode();
        }
    }
}
