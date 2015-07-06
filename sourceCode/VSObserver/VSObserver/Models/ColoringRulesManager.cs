using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml;

namespace VSObserver.Models
{
    public class ColoringRulesManager : ViewModelBase, IEqualityComparer<ColoringRules>, ICloneable
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
        private string oldRegex;
        private bool ruleExist;

        private ICommand cmdAddColRul;
        private ICommand cmdSaveRul;

        //Permet de revenir lorsqu'on fait un cancel
        private ColoringRulesManager savedInstance;

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

        public void setOldRegex(string old)
        {
            this.oldRegex = old;
        }

        public void setRulePath(string rulePath)
        {
            this.rulePath = rulePath;
        }

        public void setRuleExist(bool ruleExist)
        {
            this.ruleExist = ruleExist;
            savedInstance = (ColoringRulesManager)this.Clone();
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
                XDocument xmlFile = XDocument.Load(rulePath);
                
                if (ruleExist)
                {                    

                    var query = from items in xmlFile.Descendants(VariableObserver.NODE_ITEM)
                                where items.Element(VariableObserver.NODE_VARIABLE).Value == oldRegex
                                select items;

                    foreach (XElement item in query)
                    {
                        item.Element(VariableObserver.NODE_VARIABLE).Value = RuleRegex;
                        item.Element(VariableObserver.NODE_COMMENT).Value = RuleComment;
                        XElement ruleSet = item.Element(VariableObserver.NODE_RULE_SET);
                        ruleSet.RemoveNodes();
                        
                        foreach(ColoringRules colorRule in _listOfColoringRules)
                        {
                            XElement node = new XElement(VariableObserver.NODE_SIMPLE_RULE);
                            node.SetAttributeValue(VariableObserver.ATTR_VALUE, colorRule.Value);
                            node.SetAttributeValue(VariableObserver.ATTR_COLOR, colorRule.Color);
                            ruleSet.Add(node);
                        }
                    }                    
                }
                else
                {
                    XElement listNode = xmlFile.Element(VariableObserver.NODE_LIST);
                    XElement itemNode = new XElement(VariableObserver.NODE_ITEM);
                    XElement varNode = new XElement(VariableObserver.NODE_VARIABLE);
                    varNode.Value = RuleRegex;

                    XElement comNode = new XElement(VariableObserver.NODE_COMMENT);
                    comNode.Value = RuleComment;

                    XElement ruleSetNode = new XElement(VariableObserver.NODE_RULE_SET);

                    foreach (ColoringRules colorRule in _listOfColoringRules)
                    {
                        XElement node = new XElement(VariableObserver.NODE_SIMPLE_RULE);
                        node.SetAttributeValue(VariableObserver.ATTR_VALUE, colorRule.Value);
                        node.SetAttributeValue(VariableObserver.ATTR_COLOR, colorRule.Color);
                        ruleSetNode.Add(node);
                    }

                    itemNode.Add(varNode);
                    itemNode.Add(comNode);
                    itemNode.Add(ruleSetNode);

                    //Ajout de l'item dans la liste
                    listNode.Add(itemNode);
                }

                xmlFile.Save(rulePath);
            }
        }

        public ColoringRulesManager getSavedInstance()
        {
            if (savedInstance != null)
                return savedInstance;
            else
                return new ColoringRulesManager();
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

        /// <summary>
        /// Pour avoir un clone de l'objet et pour ne pas influencer sur l'existant
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            ColoringRulesManager colorRuleManager = (ColoringRulesManager)MemberwiseClone();

            ObservableCollection<ColoringRules> newListColRul = new ObservableCollection<ColoringRules>();
            //On copie toutes les règles de couleur existante, ainsi quand on change l'objet ça n'infulence pas sur les autres
            foreach (ColoringRules colorRule in _listOfColoringRules)
            {
                newListColRul.Add((ColoringRules)colorRule.Clone());
            }

            colorRuleManager.ListOfColoringRules = newListColRul;
            return colorRuleManager;
        }
    }
}
