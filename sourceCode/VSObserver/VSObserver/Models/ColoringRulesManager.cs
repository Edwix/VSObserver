using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml;
using System.Configuration;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Threading;
using System.Threading;

namespace VSObserver.Models
{
    public class ColoringRulesManager : ViewModelBase, IEqualityComparer<ColoringRules>, ICloneable
    {
        private const string TAG_COLOR_RULE = "ColRule";

        private const string LIST_COLORINGRULES = "ListOfColoringRules";
        private const string LIST_COLOURS = "ListOfColours";
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

        private ObservableCollection<ComboBoxItem> _listOfColours;

        public ColoringRulesManager ()
        {
            _listOfColoringRules = new ObservableCollection<ColoringRules>();
            ruleExist = false;

            _listOfColours = new ObservableCollection<ComboBoxItem>();
            
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key.Contains(TAG_COLOR_RULE))
                {
                    ///We invoke the creation of combox item, because they are own thread.
                    ///When we change the XML a thread is created. This thread use this contructor.
                    ///So it cannot create the comboboxitems from this thread
                    ///===================================================================================
                    ///On utilise la méthode invoke de l'application courante, afin de généré les comboboxitem
                    ///Car lorsqu'on change la couleur dans le fichier XML ce constructeur est appellé d'un autre thread
                    ///donc on ne peut pas créer les comboboxitem puisqu'il on leurs propre thread.
                    ///Ainsi le invoke permet d'invoquer les thread des composants (comboboxitem)
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (ThreadStart)delegate
                    {
                        ComboBoxItem cbi = new ComboBoxItem();
                        cbi.Content = "";

                        try
                        {
                            cbi.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(ConfigurationManager.AppSettings[key]);
                        }
                        catch
                        {
                            cbi.Background = Brushes.Transparent;
                        }

                        _listOfColours.Add(cbi);
                    });
                }
            }
        }

        public ObservableCollection<ComboBoxItem> ListOfColours
        {
            get { return _listOfColours; }
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
                ColoringRules colRul = new ColoringRules();
                _listOfColoringRules.Add(colRul);
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

                        if (RuleComment != null)
                            item.Element(VariableObserver.NODE_COMMENT).Value = RuleComment;

                        XElement ruleSet = item.Element(VariableObserver.NODE_RULE_SET);
                        ruleSet.RemoveNodes();
                        
                        foreach(ColoringRules colorRule in _listOfColoringRules)
                        {
                            XElement node = new XElement(VariableObserver.NODE_SIMPLE_RULE);

                            if (!String.IsNullOrEmpty(colorRule.Value))
                                node.SetAttributeValue(VariableObserver.ATTR_VALUE, colorRule.Value);
                            else
                                node.SetAttributeValue(VariableObserver.ATTR_VALUE, "0");

                            if (!String.IsNullOrEmpty(colorRule.Color))
                                node.SetAttributeValue(VariableObserver.ATTR_COLOR, colorRule.Color);
                            else
                                node.SetAttributeValue(VariableObserver.ATTR_COLOR, "");

                            if(!String.IsNullOrEmpty(colorRule.Operator))
                                node.SetAttributeValue(VariableObserver.ATTR_OPERATOR, colorRule.Operator);
                            else
                                node.SetAttributeValue(VariableObserver.ATTR_OPERATOR, VariableObserver.OPERATOR_EQUAL);

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
                    
                    if(RuleComment != null)
                        comNode.Value = RuleComment;

                    XElement ruleSetNode = new XElement(VariableObserver.NODE_RULE_SET);

                    foreach (ColoringRules colorRule in _listOfColoringRules)
                    {
                        XElement node = new XElement(VariableObserver.NODE_SIMPLE_RULE);

                        if (!String.IsNullOrEmpty(colorRule.Value))
                            node.SetAttributeValue(VariableObserver.ATTR_VALUE, colorRule.Value);
                        else
                            node.SetAttributeValue(VariableObserver.ATTR_VALUE, "0");

                        if (!String.IsNullOrEmpty(colorRule.Color))
                            node.SetAttributeValue(VariableObserver.ATTR_COLOR, colorRule.Color);
                        else
                            node.SetAttributeValue(VariableObserver.ATTR_COLOR, "");

                        if (!String.IsNullOrEmpty(colorRule.Operator))
                            node.SetAttributeValue(VariableObserver.ATTR_OPERATOR, colorRule.Operator);
                        else
                            node.SetAttributeValue(VariableObserver.ATTR_OPERATOR, VariableObserver.OPERATOR_EQUAL);

                        ruleSetNode.Add(node);
                    }

                    itemNode.Add(varNode);

                    if (RuleComment != null)
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

        /// <summary>
        /// Remove a coloring rule present in the list
        /// Supprime un object ColoringRule présent dans la liste
        /// </summary>
        /// <param name="colorRule"></param>
        public void removeColoringRule(ColoringRules colorRule)
        {
            if(ListOfColoringRules != null && colorRule != null)
                ListOfColoringRules.Remove(colorRule);
        }
    }
}
