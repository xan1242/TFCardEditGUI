﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using IniParser;
using IniParser.Model;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Configuration;
using System.Collections.Specialized;
using BrightIdeasSoftware;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Guid InstanceID;

        public TFCard[] ImportDB;
        public int ImportedCardsCount;
        public int CurrentlySelectedCard;
        

        int DisplayedCardsCount = 0;
        bool bCurrentlySearching = false;

        string ClipboardURL;
        string CurrentFilename;

        string CurrentCacheDir;
        string CurrentCacheIni;

        char CurrentLang = 'E';
        bool bJapaneseLangDetected = false;
        Font DefaultWestStyle = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        Font DescWestStyle = new Font("Segoe UI Historic", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        Font JapaneseStyle = new Font("MS UI Gothic", 10F, FontStyle.Regular, GraphicsUnit.Point);

        FindBox findBoxDialog;
        FilterBox filterBoxDialog;
        SaveQuestionBox saveQuestionDialog;

        CardSearch cardSearch;
        CardSearchParams replaceParams;


        bool bUnsavedChangesMade = false;

        // stolen from: https://stackoverflow.com/a/3301750
        // written by Dan Tao
        T[] InitializeArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }

        int GetLangIndex()
        {
            switch (CurrentLang)
            {
                case 'J':
                    return 0;
                case 'G':
                    return 2;
                case 'F':
                    return 3;
                case 'I':
                    return 4;
                case 'S':
                    return 5;
                case 'E':
                default:
                    return 1;
            }
        }

        char SetLangIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return 'J';
                case 2:
                    return 'G';
                case 3:
                    return 'F';
                case 4:
                    return 'I';
                case 5:
                    return 'S';
                case 1:
                default:
                    return 'E';
            }
        }

        int SearchCardIndexByID(int CardID)
        {
            for (int i = 0; i < ImportedCardsCount; i++)
            {
                if (ImportDB[i].CardID == CardID)
                    return i;
            }
            return 0;
        }

        void ResetAppState()
        {
            listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            olvColumnCardID,
            olvColumnName,
            olvColumnKind,
            olvColumnLevel,
            olvColumnATK,
            olvColumnDEF,
            olvColumnType,
            olvColumnAttrib,
            olvColumnIcon,
            olvColumnRarity,
            olvColumnPassword,
            olvColumnCardExists,
            olvColumnDescr});

            linkLabel1.Enabled = false;
            linkLabel2.Enabled = false;
            textBox1.Enabled = false;
            propertyGrid1.Enabled = false;
            label1.Visible = false;
            label2.Visible = false;
            label3.Visible = false;
            label4.Visible = false;
            label5.Visible = false;
            label6.Visible = false;


            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel2.Text = "";
            toolStripProgressBar1.Value = 0;
            propertyGrid1.SelectedObject = null;
            textBox1.Text = null;
            bJapaneseLangDetected = false;
            hiraganaCheckBox.Enabled = false;
            hiraganaCheckBox.Checked = false;
            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            comboBox1.Enabled = false;
            comboBox1.SelectedIndex = -1;
            bUnsavedChangesMade = false;
        }

        public void UpdateTexts()
        {
            linkLabel1.Enabled = true;
            linkLabel2.Enabled = true;
            textBox1.Enabled = true;
            propertyGrid1.Enabled = true;
            label1.Visible = true;
            label2.Visible = true;
            label3.Visible = true;
            label4.Visible = true;
            label5.Visible = true;
            label6.Visible = true;
            bJapaneseLangDetected = false;

            // detect Japanese...
            if (textBox1.Text.Contains("$R"))
            {
                bJapaneseLangDetected = true;
                hiraganaCheckBox.Enabled = true;
                /*if (!hiraganaCheckBox.Checked)
                {
                   // string[] HiraganaText = textBox1.Text.Split("$R");

                }*/


            }


            // Text box - style and text

            textBox1.Text = ImportDB[CurrentlySelectedCard].Description.Replace("\n", "\r\n");
            if (ImportDB[CurrentlySelectedCard].Kind == CardKinds.Normal && (CurrentLang != 'J'))
                textBox1.Font = new Font(DescWestStyle, FontStyle.Italic);
            else if (CurrentLang == 'J')
                textBox1.Font = JapaneseStyle;
            else
                textBox1.Font = new Font(DefaultWestStyle, FontStyle.Regular);


            // description box display stuff...
            // name
            if (CurrentLang == 'J')
                label1.Font = JapaneseStyle;
            else
                label1.Font = DefaultWestStyle;


            label1.Text = ImportDB[CurrentlySelectedCard].Name;

            // ATK and DEF
            if ((ImportDB[CurrentlySelectedCard].Kind == CardKinds.Spell) || (ImportDB[CurrentlySelectedCard].Kind == CardKinds.Trap))
                label2.Text = "";
            else
            {
                label2.Text = "ATK/ ";
                if (ImportDB[CurrentlySelectedCard].ATK == 5110)
                    label2.Text += "?";
                else
                    label2.Text += ImportDB[CurrentlySelectedCard].ATK;

                label2.Text += " DEF/ ";
                if (ImportDB[CurrentlySelectedCard].DEF == 5110)
                    label2.Text += "?";
                else
                    label2.Text += ImportDB[CurrentlySelectedCard].DEF;
            }
            // Attribute
            if (ImportDB[CurrentlySelectedCard].Attr == CardAttributes.None)
                label3.Text = "?";
            else
                label3.Text = TypeDescriptor.GetConverter(typeof(CardAttributes)).ConvertToString(ImportDB[CurrentlySelectedCard].Attr);

            // Level / S/T text
            if ((ImportDB[CurrentlySelectedCard].Kind == CardKinds.Spell) || (ImportDB[CurrentlySelectedCard].Kind == CardKinds.Trap))
            {
                label4.Font = new Font(label4.Font, FontStyle.Bold);
                label4.Text = "[";
                if (ImportDB[CurrentlySelectedCard].Kind == CardKinds.Spell)
                    label4.Text += "SPELL CARD";
                else
                    label4.Text += "TRAP CARD";

                if (ImportDB[CurrentlySelectedCard].Icon != CardIcons.None)
                    label4.Text += " (" + TypeDescriptor.GetConverter(typeof(CardIcons)).ConvertToString(ImportDB[CurrentlySelectedCard].Icon) + ")";

                label4.Text += "]";
            }
            else
            {
                label4.Font = new Font(label4.Font, FontStyle.Regular);
                label4.Text = "Level: ";
                if (ImportDB[CurrentlySelectedCard].Level == 0)
                    label4.Text += "?";
                else
                    label4.Text += ImportDB[CurrentlySelectedCard].Level;
            }

            // Monster type
            if (!((ImportDB[CurrentlySelectedCard].Kind == CardKinds.Spell) || (ImportDB[CurrentlySelectedCard].Kind == CardKinds.Trap)))
            {
                label5.Text = "[";

                if (ImportDB[CurrentlySelectedCard].Type == CardTypes.None)
                    label5.Text += "?";
                else
                    label5.Text += TypeDescriptor.GetConverter(typeof(CardTypes)).ConvertToString(ImportDB[CurrentlySelectedCard].Type);

                label5.Text += "/" + TypeDescriptor.GetConverter(typeof(CardKinds)).ConvertToString(ImportDB[CurrentlySelectedCard].Kind);
                label5.Text += "]";
            }
            else
                label5.Text = "";

            // Password
            if (ImportDB[CurrentlySelectedCard].Kind == CardKinds.Token)
                label6.Text = "This card cannot be in a Deck.";
            else
                label6.Text = ImportDB[CurrentlySelectedCard].Password.ToString();

            // update ListView as well...

            listView1.SelectedItems[0].SubItems[(int)CardProps.Name].Text = ImportDB[CurrentlySelectedCard].Name;
            listView1.SelectedItems[0].SubItems[(int)CardProps.Kind].Text = TypeDescriptor.GetConverter(typeof(CardKinds)).ConvertToString(ImportDB[CurrentlySelectedCard].Kind);
            listView1.SelectedItems[0].SubItems[(int)CardProps.Level].Text = ImportDB[CurrentlySelectedCard].Level.ToString();
            listView1.SelectedItems[0].SubItems[(int)CardProps.ATK].Text = ImportDB[CurrentlySelectedCard].ATK.ToString();
            listView1.SelectedItems[0].SubItems[(int)CardProps.DEF].Text = ImportDB[CurrentlySelectedCard].DEF.ToString();
            listView1.SelectedItems[0].SubItems[(int)CardProps.Type].Text = TypeDescriptor.GetConverter(typeof(CardTypes)).ConvertToString(ImportDB[CurrentlySelectedCard].Type);
            listView1.SelectedItems[0].SubItems[(int)CardProps.Attr].Text = TypeDescriptor.GetConverter(typeof(CardAttributes)).ConvertToString(ImportDB[CurrentlySelectedCard].Attr);
            listView1.SelectedItems[0].SubItems[(int)CardProps.Icon].Text = TypeDescriptor.GetConverter(typeof(CardIcons)).ConvertToString(ImportDB[CurrentlySelectedCard].Icon);
            listView1.SelectedItems[0].SubItems[(int)CardProps.Rarity].Text = TypeDescriptor.GetConverter(typeof(CardRarity)).ConvertToString(ImportDB[CurrentlySelectedCard].Rarity);
            listView1.SelectedItems[0].SubItems[(int)CardProps.Password].Text = ImportDB[CurrentlySelectedCard].Password.ToString();
            listView1.SelectedItems[0].SubItems[(int)CardProps.CardExists].Text = ImportDB[CurrentlySelectedCard].CardExistFlag.ToString();
            listView1.SelectedItems[0].SubItems[(int)CardProps.Description].Text = ImportDB[CurrentlySelectedCard].Description;
        }

        void UpdateListView(bool[] UpdateFlags, bool bUpdateAll)
        {
            int ci;
            if (UpdateFlags == null)
                UpdateFlags = new bool[listView1.Items.Count];

            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (UpdateFlags[i] || bUpdateAll)
                {
                    ci = SearchCardIndexByID(Int32.Parse(listView1.Items[i].SubItems[0].Text));
                    listView1.Items[i].SubItems[(int)CardProps.Name].Text = ImportDB[ci].Name;
                    listView1.Items[i].SubItems[(int)CardProps.Kind].Text = TypeDescriptor.GetConverter(typeof(CardKinds)).ConvertToString(ImportDB[ci].Kind);
                    listView1.Items[i].SubItems[(int)CardProps.Level].Text = ImportDB[ci].Level.ToString();
                    listView1.Items[i].SubItems[(int)CardProps.ATK].Text = ImportDB[ci].ATK.ToString();
                    listView1.Items[i].SubItems[(int)CardProps.DEF].Text = ImportDB[ci].DEF.ToString();
                    listView1.Items[i].SubItems[(int)CardProps.Type].Text = TypeDescriptor.GetConverter(typeof(CardTypes)).ConvertToString(ImportDB[ci].Type);
                    listView1.Items[i].SubItems[(int)CardProps.Attr].Text = TypeDescriptor.GetConverter(typeof(CardAttributes)).ConvertToString(ImportDB[ci].Attr);
                    listView1.Items[i].SubItems[(int)CardProps.Icon].Text = TypeDescriptor.GetConverter(typeof(CardIcons)).ConvertToString(ImportDB[ci].Icon);
                    listView1.Items[i].SubItems[(int)CardProps.Rarity].Text = TypeDescriptor.GetConverter(typeof(CardRarity)).ConvertToString(ImportDB[ci].Rarity);
                    listView1.Items[i].SubItems[(int)CardProps.Password].Text = ImportDB[ci].Password.ToString();
                    listView1.Items[i].SubItems[(int)CardProps.CardExists].Text = ImportDB[ci].CardExistFlag.ToString();
                    listView1.Items[i].SubItems[(int)CardProps.Description].Text = ImportDB[ci].Description;
                }
            }
        }

        void AddListViewItem(int listview_index, int carddb_index)
        {
           listView1.Items.Add(ImportDB[carddb_index].CardID.ToString());
           listView1.Items[listview_index].SubItems.Add(ImportDB[carddb_index].Name);
           listView1.Items[listview_index].SubItems.Add(TypeDescriptor.GetConverter(typeof(CardKinds)).ConvertToString(ImportDB[carddb_index].Kind));
           listView1.Items[listview_index].SubItems.Add(ImportDB[carddb_index].Level.ToString());
           listView1.Items[listview_index].SubItems.Add(ImportDB[carddb_index].ATK.ToString());
           listView1.Items[listview_index].SubItems.Add(ImportDB[carddb_index].DEF.ToString());
           listView1.Items[listview_index].SubItems.Add(TypeDescriptor.GetConverter(typeof(CardTypes)).ConvertToString(ImportDB[carddb_index].Type));
           listView1.Items[listview_index].SubItems.Add(TypeDescriptor.GetConverter(typeof(CardAttributes)).ConvertToString(ImportDB[carddb_index].Attr));
           listView1.Items[listview_index].SubItems.Add(TypeDescriptor.GetConverter(typeof(CardIcons)).ConvertToString(ImportDB[carddb_index].Icon));
           listView1.Items[listview_index].SubItems.Add(TypeDescriptor.GetConverter(typeof(CardRarity)).ConvertToString(ImportDB[carddb_index].Rarity));
           listView1.Items[listview_index].SubItems.Add(ImportDB[carddb_index].Password.ToString());
           listView1.Items[listview_index].SubItems.Add(ImportDB[carddb_index].CardExistFlag.ToString());
           listView1.Items[listview_index].SubItems.Add(ImportDB[carddb_index].Description);
        }

        void GenerateListView()
        {
            listView1.SetObjects(ImportDB);
            DisplayedCardsCount = listView1.Items.Count;
        }

        bool FilterCheckName(CardFilterParams filterParams, TFCard card)
        {
            if (filterParams.MatchCase)
            {
                if (filterParams.MatchExact)
                {
                    if (card.Name.Equals(filterParams.Name))
                        return true;
                }
                else if (card.Name.Contains(filterParams.Name))
                    return true;
            }
            if (filterParams.MatchExact)
            {
                if (card.Name.ToUpper().Equals(filterParams.Name.ToUpper()))
                    return true;
            }
            else if (card.Name.ToUpper().Contains(filterParams.Name.ToUpper()))
                return true;

            return false;
        }

        bool FilterCheckKinds(CardFilterParams filterParams, TFCard card)
        {
            if ((card.Kind == CardKinds.Normal) && filterParams.Kind[(int)CardKinds.Normal])
                return true;

            if (((card.Kind == CardKinds.Effect) || (card.Kind == CardKinds.Toon) || (card.Kind == CardKinds.Spirit) || (card.Kind == CardKinds.Union)) && filterParams.Kind[(int)CardKinds.Effect])
                return true;

            if (((card.Kind == CardKinds.Fusion) || (card.Kind == CardKinds.FusionEffect)) && filterParams.Kind[(int)CardKinds.Fusion])
                return true;

            if (((card.Kind == CardKinds.Ritual) || (card.Kind == CardKinds.RitualEffect)) && filterParams.Kind[(int)CardKinds.Ritual])
                return true;

            if ((card.Kind == CardKinds.Token) && filterParams.Kind[(int)CardKinds.Token])
                return true;

            if ((card.Kind == CardKinds.Toon) && filterParams.Kind[(int)CardKinds.Toon])
                return true;

            if ((card.Kind == CardKinds.Spirit) && filterParams.Kind[(int)CardKinds.Spirit])
                return true;

            if ((card.Kind == CardKinds.Union) && filterParams.Kind[(int)CardKinds.Union])
                return true;

            if ((card.Kind == CardKinds.Spell) && filterParams.Kind[(int)CardKinds.Spell])
                return true;

            if ((card.Kind == CardKinds.Trap) && filterParams.Kind[(int)CardKinds.Trap])
                return true;

            return false;
        }

        bool FilterCheckTypes(CardFilterParams filterParams, TFCard card)
        {
            if ((card.Type == CardTypes.Dragon) && filterParams.Type[(int)CardTypes.Dragon])
                return true;

            if ((card.Type == CardTypes.Zombie) && filterParams.Type[(int)CardTypes.Zombie])
                return true;

            if ((card.Type == CardTypes.Fiend) && filterParams.Type[(int)CardTypes.Fiend])
                return true;

            if ((card.Type == CardTypes.Pyro) && filterParams.Type[(int)CardTypes.Pyro])
                return true;

            if ((card.Type == CardTypes.SeaSerpent) && filterParams.Type[(int)CardTypes.SeaSerpent])
                return true;

            if ((card.Type == CardTypes.Rock) && filterParams.Type[(int)CardTypes.Rock])
                return true;

            if ((card.Type == CardTypes.Machine) && filterParams.Type[(int)CardTypes.Machine])
                return true;

            if ((card.Type == CardTypes.Fish) && filterParams.Type[(int)CardTypes.Fish])
                return true;

            if ((card.Type == CardTypes.Dinosaur) && filterParams.Type[(int)CardTypes.Dinosaur])
                return true;

            if ((card.Type == CardTypes.Insect) && filterParams.Type[(int)CardTypes.Insect])
                return true;

            if ((card.Type == CardTypes.Beast) && filterParams.Type[(int)CardTypes.Beast])
                return true;

            if ((card.Type == CardTypes.BeastWarrior) && filterParams.Type[(int)CardTypes.BeastWarrior])
                return true;

            if ((card.Type == CardTypes.Plant) && filterParams.Type[(int)CardTypes.Plant])
                return true;

            if ((card.Type == CardTypes.Aqua) && filterParams.Type[(int)CardTypes.Aqua])
                return true;

            if ((card.Type == CardTypes.Warrior) && filterParams.Type[(int)CardTypes.Warrior])
                return true;

            if ((card.Type == CardTypes.WingedBeast) && filterParams.Type[(int)CardTypes.WingedBeast])
                return true;

            if ((card.Type == CardTypes.Fairy) && filterParams.Type[(int)CardTypes.Fairy])
                return true;

            if ((card.Type == CardTypes.Spellcaster) && filterParams.Type[(int)CardTypes.Spellcaster])
                return true;

            if ((card.Type == CardTypes.Thunder) && filterParams.Type[(int)CardTypes.Thunder])
                return true;

            if ((card.Type == CardTypes.Reptile) && filterParams.Type[(int)CardTypes.Reptile])
                return true;

            if ((card.Type == CardTypes.DivineBeast) && filterParams.Type[(int)CardTypes.DivineBeast])
                return true;

            if ((card.Type == CardTypes.Spell) && filterParams.Type[(int)CardTypes.Spell])
                return true;

            if ((card.Type == CardTypes.Trap) && filterParams.Type[(int)CardTypes.Trap])
                return true;

            if ((card.Type == CardTypes.None) && filterParams.Type[(int)CardTypes.None])
                return true;

            return false;
        }

        bool FilterCheckAttr(CardFilterParams filterParams, TFCard card)
        {
            if ((card.Attr == CardAttributes.LIGHT) && filterParams.Attr[(int)CardAttributes.LIGHT])
                return true;
            if ((card.Attr == CardAttributes.DARK) && filterParams.Attr[(int)CardAttributes.DARK])
                return true;
            if ((card.Attr == CardAttributes.WATER) && filterParams.Attr[(int)CardAttributes.WATER])
                return true;
            if ((card.Attr == CardAttributes.FIRE) && filterParams.Attr[(int)CardAttributes.FIRE])
                return true;
            if ((card.Attr == CardAttributes.EARTH) && filterParams.Attr[(int)CardAttributes.EARTH])
                return true;
            if ((card.Attr == CardAttributes.WIND) && filterParams.Attr[(int)CardAttributes.WIND])
                return true;
            if ((card.Attr == CardAttributes.DIVINE) && filterParams.Attr[(int)CardAttributes.DIVINE])
                return true;
            if ((card.Attr == CardAttributes.SPELL) && filterParams.Attr[(int)CardAttributes.SPELL])
                return true;
            if ((card.Attr == CardAttributes.TRAP) && filterParams.Attr[(int)CardAttributes.TRAP])
                return true;
            if ((card.Attr == CardAttributes.None) && filterParams.Attr[(int)CardAttributes.None])
                return true;

            return false;
        }
        bool FilterCheckIcon(CardFilterParams filterParams, TFCard card)
        {
            if ((card.Icon == CardIcons.Counter) && filterParams.Icon[(int)CardIcons.Counter])
                return true;
            if ((card.Icon == CardIcons.Field) && filterParams.Icon[(int)CardIcons.Field])
                return true;
            if ((card.Icon == CardIcons.Equip) && filterParams.Icon[(int)CardIcons.Equip])
                return true;
            if ((card.Icon == CardIcons.Continuous) && filterParams.Icon[(int)CardIcons.Continuous])
                return true;
            if ((card.Icon == CardIcons.QuickPlay) && filterParams.Icon[(int)CardIcons.QuickPlay])
                return true;
            if ((card.Icon == CardIcons.RitualSpell) && filterParams.Icon[(int)CardIcons.RitualSpell])
                return true;
            if ((card.Icon == CardIcons.None) && filterParams.Icon[(int)CardIcons.None])
                return true;
            return false;
        }

        bool FilterCheckRarity(CardFilterParams filterParams, TFCard card)
        {
            if ((card.Rarity == CardRarity.Common) && filterParams.Rarity[(int)CardRarity.Common])
                return true;
            if ((card.Rarity == CardRarity.Rare) && filterParams.Rarity[(int)CardRarity.Rare])
                return true;
            if ((card.Rarity == CardRarity.SuperRare) && filterParams.Rarity[(int)CardRarity.SuperRare])
                return true;
            if ((card.Rarity == CardRarity.UltraRare) && filterParams.Rarity[(int)CardRarity.UltraRare])
                return true;
            if ((card.Rarity == CardRarity.UltimateRare) && filterParams.Rarity[(int)CardRarity.UltimateRare])
                return true;
            return false;
        }

        void FilterListView(CardFilterParams filterParams)
        {
            ModelFilter defaultFilter = new ModelFilter(delegate (object x) { return true; });
            ModelFilter filterCardIDmin = defaultFilter;
            ModelFilter filterCardIDmax = defaultFilter;
            ModelFilter filterName = defaultFilter;
            ModelFilter filterKind = defaultFilter;
            ModelFilter filterLevelmin = defaultFilter;
            ModelFilter filterLevelmax = defaultFilter;
            ModelFilter filterATKmin = defaultFilter;
            ModelFilter filterATKmax = defaultFilter;
            ModelFilter filterDEFmin = defaultFilter;
            ModelFilter filterDEFmax = defaultFilter;
            ModelFilter filterAttr = defaultFilter;
            ModelFilter filterIcon = defaultFilter;
            ModelFilter filterRarity = defaultFilter;
            ModelFilter filterPassMin = defaultFilter;
            ModelFilter filterPassMax = defaultFilter;
            ModelFilter filterCardExist = filterName = new ModelFilter(delegate (object x) { return ((TFCard)x).CardExistFlag == filterParams.CardExists; }); ;
            ModelFilter filterType = defaultFilter;

            if (!string.IsNullOrEmpty(filterParams.Name))
                filterName = new ModelFilter(delegate (object x) { return FilterCheckName(filterParams, (TFCard)x); });

            if (filterParams.bAreAnyFiltersEnabled_Kind())
                filterKind = new ModelFilter(delegate (object x) { return FilterCheckKinds(filterParams, (TFCard)x); });

            if (filterParams.bAreAnyFiltersEnabled_Type())
                filterType = new ModelFilter(delegate (object x) { return FilterCheckTypes(filterParams, (TFCard)x); });

            if (filterParams.bAreAnyFiltersEnabled_Attr())
                filterAttr = new ModelFilter(delegate (object x) { return FilterCheckAttr(filterParams, (TFCard)x); });

            if (filterParams.bAreAnyFiltersEnabled_Icon())
                filterIcon = new ModelFilter(delegate (object x) { return FilterCheckIcon(filterParams, (TFCard)x); });

            if (filterParams.bAreAnyFiltersEnabled_Rarity())
                filterRarity = new ModelFilter(delegate (object x) { return FilterCheckRarity(filterParams, (TFCard)x); });

            if (filterParams.MaxATK >= 0)
                filterATKmax = new ModelFilter(delegate (object x) { return ((TFCard)x).ATK <= filterParams.MaxATK; });
            if (filterParams.MinATK >= 0)
                filterATKmin = new ModelFilter(delegate (object x) { return ((TFCard)x).ATK >= filterParams.MinATK; });

            if (filterParams.MaxDEF >= 0)
                filterDEFmax = new ModelFilter(delegate (object x) { return ((TFCard)x).DEF <= filterParams.MaxDEF; });
            if (filterParams.MinDEF >= 0)
                filterDEFmin = new ModelFilter(delegate (object x) { return ((TFCard)x).DEF >= filterParams.MinDEF; });

            if (filterParams.MaxLevel >= 0)
                filterLevelmax = new ModelFilter(delegate (object x) { return ((TFCard)x).Level <= filterParams.MaxLevel; });
            if (filterParams.MinLevel >= 0)
                filterLevelmin = new ModelFilter(delegate (object x) { return ((TFCard)x).Level >= filterParams.MinLevel; });

            if (filterParams.MaxPassword >= 0)
                filterPassMax = new ModelFilter(delegate (object x) { return ((TFCard)x).Password <= filterParams.MaxPassword; });
            if (filterParams.MinPassword >= 0)
                filterPassMin = new ModelFilter(delegate (object x) { return ((TFCard)x).Password >= filterParams.MinPassword; });

            if (filterParams.MaxCardID >= 0)
                filterCardIDmax = new ModelFilter(delegate (object x) { return ((TFCard)x).CardID <= filterParams.MaxCardID; });
            if (filterParams.MinCardID >= 0)
                filterCardIDmin = new ModelFilter(delegate (object x) { return ((TFCard)x).CardID >= filterParams.MinCardID; });

            var filterTotal = new CompositeAllFilter(new List<IModelFilter> { filterCardIDmin, filterCardIDmax, filterName, filterKind, filterLevelmin, filterLevelmax, filterATKmin, filterATKmax, filterDEFmin, filterDEFmax, filterAttr, filterIcon, filterRarity, filterPassMin, filterPassMax, filterCardExist, filterType });

            listView1.ModelFilter = filterTotal;
            DisplayedCardsCount = listView1.Items.Count;
        }

        public bool InitiateCardSearch(CardSearchParams searchParams)
        {
            string dispstr;
            bCurrentlySearching = true;
            if (cardSearch.Search(searchParams, listView1, false, false))
            {
                dispstr = searchParams.ResultString;
                listView1.Items[searchParams.SearchResultIndex].Selected = true;
                listView1.Items[searchParams.SearchResultIndex].Focused = true;
                listView1.EnsureVisible(searchParams.SearchResultIndex);

                if (searchParams.SearchContext == CardProps.Description)
                {
                    textBox1.Focus();
                    textBox1.Select(searchParams.SearchResultSubStrIndex, searchParams.SearchString.Length);
                }
                else
                    listView1.Focus();

                if (searchParams.bMatchWhole)
                    dispstr = listView1.Items[searchParams.SearchResultIndex].SubItems[(int)Enum.ToObject(typeof(CardProps), searchParams.SearchContext)].Text;

                toolStripStatusLabel1.Text = "Found: " + "[" + searchParams.SearchContext.ToString() + "] " + dispstr + " at item [" + (searchParams.SearchResultIndex + 1) + "]";
                bCurrentlySearching = false;
                return true;
            }
            else
                toolStripStatusLabel1.Text = "String: \"" + searchParams.SearchString + "\" not found.";
            bCurrentlySearching = false;
            return false;
        }

        public void InitiateReplacing(CardSearchParams searchParams)
        {
            if (searchParams.SearchResultIndex < 0) // if a card is unselected / index has changed, search without replacing....
                InitiateCardSearch(searchParams);
            else // else if we have a search result, replace and continue searching again...
            {
                int ci = SearchCardIndexByID(Int32.Parse(listView1.SelectedItems[0].SubItems[0].Text));
                cardSearch.Replace(searchParams, ImportDB[ci]);
                UpdateTexts();
                InitiateCardSearch(searchParams);
                bUnsavedChangesMade = true;
            }
        }

        public void InitiateReplaceAll(CardSearchParams searchParams)
        {
            int ci;
            int rc = 0;
            bool[] lvUpdateFlags = new bool[listView1.Items.Count];
            

            for (int i = 0; i < listView1.Items.Count; i++)
            {
                cardSearch.bFirstSearch = true;
                cardSearch.CurrentlySelectedItem = i;
                cardSearch.CurrentlySelectedSubItem = -1;
                searchParams.SearchResultSubStrIndex = -1;
                searchParams.SearchResultSubStrIndex_Name = -1;
                searchParams.SearchResultSubStrIndex_Desc = -1;

                if (cardSearch.Search(searchParams, listView1, true, true))
                {
                    if (i < searchParams.SearchResultIndex)
                        i = searchParams.SearchResultIndex; // boost the counter forward and only forward...
                    ci = SearchCardIndexByID(Int32.Parse(listView1.Items[searchParams.SearchResultIndex].SubItems[0].Text));
                    cardSearch.Replace(searchParams, ImportDB[ci]);
                    lvUpdateFlags[i] = true;
                    rc++;
                }
            }
            UpdateTexts();
            UpdateListView(lvUpdateFlags, false);
            toolStripStatusLabel1.Text = "Replaced " + rc + " instances";
            if (rc > 0)
                bUnsavedChangesMade = true;
        }

        void ExtractEHP(string InFilename, string OutFolder)
        {
            var ehpprocess = new Process { StartInfo = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true, FileName = "ehppack.exe", Arguments = "\"" + InFilename + "\" \"" + OutFolder + "\""} };
            ehpprocess.Start();
            ehpprocess.WaitForExit();
        }

        void PackEHP(string InFolder, string OutFilename)
        {
            var ehpprocess = new Process { StartInfo = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true, FileName = "ehppack.exe", Arguments = "-p \"" + InFolder + "\" \"" + OutFilename + "\"" } };
            ehpprocess.Start();
            ehpprocess.WaitForExit();
        }

        void ConvertDBToIni(string InFolder, string OutFilename, string LanguageDesignator)
        {
            var tfcecli_process = new Process { StartInfo = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true, FileName = "TFCardEdit.exe", Arguments = "\"" + InFolder + "\" \"" + OutFilename + "\" " + LanguageDesignator } };
            tfcecli_process.Start();
            tfcecli_process.WaitForExit();
        }

        void ConvertIniToDB(string InFilename, string OutFolder, string LanguageDesignator)
        {
            var tfcecli_process = new Process { StartInfo = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true, FileName = "TFCardEdit.exe", Arguments = "-w \"" + InFilename + "\" \"" + OutFolder + "\" " + LanguageDesignator } };
            tfcecli_process.Start();
            tfcecli_process.WaitForExit();
        }

        char DetectLangFromFilename(string Filename)
        {
            if (Filename.LastIndexOf('_') != -1)
            {
                char LangChar = Filename[Filename.LastIndexOf('_') + 1];
                LangChar = LangChar.ToString().ToUpper()[0];
                return LangChar;
            }
            return 'E';
        }

        void CopyFilesForEHP(string Path)
        {
            if (File.Exists(Path + "\\CARD_SamePict_" + CurrentLang.ToString() + ".bin") && !File.Exists(CurrentCacheDir + "\\CARD_SamePict_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\CARD_SamePict_" + CurrentLang.ToString() + ".bin", CurrentCacheDir + "\\CARD_SamePict_" + CurrentLang.ToString() + ".bin");
            if (File.Exists(Path + "\\CARD_Sort_" + CurrentLang.ToString() + ".bin") && !File.Exists(CurrentCacheDir + "\\CARD_Sort_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\CARD_Sort_" + CurrentLang.ToString() + ".bin", CurrentCacheDir + "\\CARD_Sort_" + CurrentLang.ToString() + ".bin");
            if (File.Exists(Path + "\\CARD_Top_" + CurrentLang.ToString() + ".bin") && !File.Exists(CurrentCacheDir + "\\CARD_Top_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\CARD_Top_" + CurrentLang.ToString() + ".bin", CurrentCacheDir + "\\CARD_Top_" + CurrentLang.ToString() + ".bin");
            if (File.Exists(Path + "\\DLG_Indx_" + CurrentLang.ToString() + ".bin") && !File.Exists(CurrentCacheDir + "\\DLG_Indx_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\DLG_Indx_" + CurrentLang.ToString() + ".bin", CurrentCacheDir + "\\DLG_Indx_" + CurrentLang.ToString() + ".bin");
            if (File.Exists(Path + "\\DLG_Text_" + CurrentLang.ToString() + ".bin") && !File.Exists(CurrentCacheDir + "\\DLG_Text_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\DLG_Text_" + CurrentLang.ToString() + ".bin", CurrentCacheDir + "\\DLG_Text_" + CurrentLang.ToString() + ".bin");

            if (File.Exists(Path + "\\CARD_Genre.bin") && !File.Exists(Path + CurrentCacheDir + "\\CARD_Genre.bin"))
                File.Copy(Path + "\\CARD_Genre.bin", CurrentCacheDir + "\\CARD_Genre.bin");
        }

        void ImportCardDB(string Filename)
        {
            int CardImporterCounter = 0;
            FileIniDataParser ImportIni = new FileIniDataParser();
            IniData ParsedIni = ImportIni.ReadFile(Filename);

            ImportedCardsCount = ParsedIni.Sections.Count;
            //ImportDB = new TFCard[ImportedCardsCount];
            ImportDB = InitializeArray<TFCard>(ImportedCardsCount);

            if (listView1.Items.Count > 0)
            {
                ResetAppState();
            }

            toolStripProgressBar1.Enabled = true;
            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Maximum = ImportedCardsCount;
            toolStripProgressBar1.Value = 0;

            foreach (IniParser.Model.SectionData s in ParsedIni.Sections)
            {
                //Console.WriteLine("Importing: [" + s.SectionName + "] " + s.Keys.GetKeyData("Name").Value);
                //toolStripStatusLabel1.Text = "Importing: [" + s.SectionName + "] " + s.Keys.GetKeyData("Name").Value;

                ImportDB[CardImporterCounter].CardID = Int32.Parse(s.SectionName);
                ImportDB[CardImporterCounter].Name = s.Keys.GetKeyData("Name").Value;
                ImportDB[CardImporterCounter].Description = s.Keys.GetKeyData("Description").Value;
                ImportDB[CardImporterCounter].ATK = Int32.Parse(s.Keys.GetKeyData("ATK").Value);
                ImportDB[CardImporterCounter].DEF = Int32.Parse(s.Keys.GetKeyData("DEF").Value);
                ImportDB[CardImporterCounter].Password = Int32.Parse(s.Keys.GetKeyData("Password").Value);
                ImportDB[CardImporterCounter].CardExistFlag = Convert.ToBoolean(Int32.Parse(s.Keys.GetKeyData("CardExistFlag").Value));
                ImportDB[CardImporterCounter].Kind = (CardKinds)Int32.Parse(s.Keys.GetKeyData("Kind").Value);
                ImportDB[CardImporterCounter].Attr = (CardAttributes)Int32.Parse(s.Keys.GetKeyData("Attr").Value);
                ImportDB[CardImporterCounter].Level = Int32.Parse(s.Keys.GetKeyData("Level").Value);
                ImportDB[CardImporterCounter].Icon = (CardIcons)Int32.Parse(s.Keys.GetKeyData("Icon").Value);
                ImportDB[CardImporterCounter].Type = (CardTypes)Int32.Parse(s.Keys.GetKeyData("Type").Value);
                ImportDB[CardImporterCounter].Rarity = (CardRarity)Int32.Parse(s.Keys.GetKeyData("Rarity").Value);

                // also replace the newline character in card descriptions as we're loading them...
                ImportDB[CardImporterCounter].Description = ImportDB[CardImporterCounter].Description.Replace('^', '\n').Replace('˘', '\r');

                toolStripProgressBar1.Value++;
                CardImporterCounter++;
            }
            toolStripProgressBar1.Visible = false;
            toolStripProgressBar1.Enabled = false;
        }

        void ExportCardDB(string Filename, int ProgressBarAdditional)
        {
            FileIniDataParser ExportIniParser = new FileIniDataParser();
            IniData ExportIni = new IniData();

            toolStripProgressBar1.Maximum = ImportedCardsCount + 1 + ProgressBarAdditional;
            toolStripProgressBar1.Value = 0;
            ExportIni.Configuration.NewLineStr = "\n";

            for (int i = 0; i < ImportedCardsCount; i++)
            {
                ExportIni.Sections.AddSection(ImportDB[i].CardID.ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Name", ImportDB[i].Name);
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Description", ImportDB[i].Description.Replace('\n', '^').Replace('\r', '˘'));
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("ATK", ImportDB[i].ATK.ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("DEF", ImportDB[i].DEF.ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Password", ImportDB[i].Password.ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("CardExistFlag", Convert.ToInt32(ImportDB[i].CardExistFlag).ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Kind", Convert.ToInt32(ImportDB[i].Kind).ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Attr", Convert.ToInt32(ImportDB[i].Attr).ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Level", ImportDB[i].Level.ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Icon", Convert.ToInt32(ImportDB[i].Icon).ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Type", Convert.ToInt32(ImportDB[i].Type).ToString());
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Rarity", Convert.ToInt32(ImportDB[i].Rarity).ToString());
                toolStripProgressBar1.Value++;
            }
            
            if (File.Exists(Filename))
                File.Delete(Filename);

            ExportIniParser.WriteFile(Filename, ExportIni, Encoding.Unicode);
            toolStripProgressBar1.Value++;
        }

        void DoTheSaving(string Filename)
        {
            bool bFileCheckLoop = true;
            if (string.Compare(Path.GetExtension(Filename), ".ehp") == 0)
            {
                ExportCardDB(CurrentCacheIni, 20);
                if (!Directory.Exists(CurrentCacheDir))
                    Directory.CreateDirectory(CurrentCacheDir);
                ConvertIniToDB(CurrentCacheIni, CurrentCacheDir, CurrentLang.ToString());
                toolStripProgressBar1.Value += 10;

                // check if all necessary files are in the work folder, if not, ask the user to find them
                while (bFileCheckLoop)
                {
                    bFileCheckLoop = false;
                    if (!File.Exists(CurrentCacheDir + "\\CARD_SamePict_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: CARD_SamePict_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "CARD_SamePict_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists(CurrentCacheDir + "\\CARD_Sort_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: CARD_Sort_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "CARD_Sort_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists(CurrentCacheDir + "\\CARD_Top_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: CARD_Top_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "CARD_Top_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists(CurrentCacheDir + "\\DLG_Indx_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: DLG_Indx_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "DLG_Indx_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists(CurrentCacheDir + "\\DLG_Text_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: DLG_Text_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "DLG_Text_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists(CurrentCacheDir + "\\CARD_Genre.bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: CARD_Genre.bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "CARD_Genre.bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                }

                PackEHP(CurrentCacheDir, Filename);
                toolStripProgressBar1.Value += 10;
            }
            else if (string.Compare(Path.GetExtension(Filename), ".bin") == 0)
            {
                ExportCardDB(CurrentCacheIni, 10);
                ConvertIniToDB(CurrentCacheIni, Path.GetDirectoryName(Filename), CurrentLang.ToString());
                toolStripProgressBar1.Value += 10;
            }
            else
                ExportCardDB(Filename, 0);
        }

        public Form1()
        {
            InitializeComponent();
            findBoxDialog = new FindBox();
            filterBoxDialog = new FilterBox();
            saveQuestionDialog = new SaveQuestionBox();
            replaceParams = new CardSearchParams();
            replaceParams.bSearchName = true;
            cardSearch = new CardSearch();
            InstanceID = Guid.NewGuid();
            CurrentCacheDir = "cache\\" + InstanceID + "\\workehp";
            CurrentCacheIni = CurrentCacheDir + ".ini";
            JapaneseStyle = new Font("Meiryo", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
            if (JapaneseStyle.Name != "Meiryo")
                JapaneseStyle = new Font("MS UI Gothic", 10F, FontStyle.Regular, GraphicsUnit.Point);
        }

        bool HandleUnsavedQuestion()
        {
            if (bUnsavedChangesMade)
            {
                saveQuestionDialog.Filename = Path.GetFileName(CurrentFilename).ToString();
                DialogResult result = saveQuestionDialog.ShowDialog();
                if (result == DialogResult.Cancel)
                    return false;
                if (result == DialogResult.OK)
                {
                    if (ImportedCardsCount > 0 && !string.IsNullOrEmpty(CurrentFilename))
                    {
                        toolStripStatusLabel1.Text = "Saving to: " + CurrentFilename;

                        toolStripProgressBar1.Enabled = true;
                        toolStripProgressBar1.Visible = true;

                        DoTheSaving(CurrentFilename);

                        toolStripProgressBar1.Visible = false;
                        toolStripProgressBar1.Enabled = false;

                        toolStripStatusLabel1.Text = "Saved to: " + CurrentFilename;
                        bUnsavedChangesMade = false;
                    }
                }
            }
            return true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStrip1.Visible = Properties.Settings.Default.ShowToolbar;
            toolbarToolStripMenuItem.Checked = Properties.Settings.Default.ShowToolbar;

            statusStrip1.Visible = Properties.Settings.Default.ShowStatusBar;
            statusBarToolStripMenuItem.Checked = Properties.Settings.Default.ShowStatusBar;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!HandleUnsavedQuestion())
                return;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                OpenFile(openFileDialog1.FileName);
        }

        private void OpenFile(string FileName)
        {
            //Console.WriteLine("Importing: " + FileName);
            CurrentFilename = FileName;
            toolStripStatusLabel1.Text = "Opening: " + FileName;
            CurrentLang = DetectLangFromFilename(FileName);
            //CurrentCacheDir = "cache\\" + InstanceID + "workehp";

            if (string.Compare(Path.GetExtension(FileName), ".ehp") == 0)
            {
                if (File.Exists(CurrentCacheIni))
                    File.Delete(CurrentCacheIni);
                if (Directory.Exists(CurrentCacheDir))
                {
                    Directory.Delete(CurrentCacheDir, true);
                    Directory.CreateDirectory(CurrentCacheDir);
                }
                else
                    Directory.CreateDirectory(CurrentCacheDir);


                // invoke the toolchain...
                ExtractEHP(FileName, CurrentCacheDir);
                if (!File.Exists(CurrentCacheDir + "\\CARD_Name_" + CurrentLang.ToString() + ".bin"))
                {
                    toolStripStatusLabel1.Text = "Invalid EhFolder! Please use only cardinfo_* ehp files.";
                    if (File.Exists(CurrentCacheIni))
                        File.Delete(CurrentCacheIni);
                    if (Directory.Exists(CurrentCacheDir))
                        Directory.Delete(CurrentCacheDir, true);

                    return;
                }
                ConvertDBToIni(CurrentCacheDir, CurrentCacheIni, CurrentLang.ToString());
                ImportCardDB(CurrentCacheIni);
            }
            else if (string.Compare(Path.GetExtension(FileName), ".bin") == 0) // opened an extracted folder...
            {
                string currentDirectory = Path.GetDirectoryName(FileName);

                //checking one by one since there's only so many files that this tool needs work with, so no need to complicate things
                if (!File.Exists(currentDirectory + "\\CARD_Desc_" + CurrentLang.ToString() + ".bin"))
                {
                    toolStripStatusLabel1.Text = "Missing: " + currentDirectory + "\\CARD_Desc_" + CurrentLang.ToString() + ".bin! Please check if all files are present.";
                    return;
                }
                if (!File.Exists(currentDirectory + "\\CARD_Indx_" + CurrentLang.ToString() + ".bin"))
                {
                    toolStripStatusLabel1.Text = "Missing: " + currentDirectory + "\\CARD_Indx_" + CurrentLang.ToString() + ".bin! Please check if all files are present.";
                    return;
                }
                if (!File.Exists(currentDirectory + "\\CARD_Prop.bin"))
                {
                    toolStripStatusLabel1.Text = "Missing: " + currentDirectory + "\\CARD_Prop.bin! Please check if all files are present.";
                    return;
                }
                if (!File.Exists(currentDirectory + "\\CARD_Pass.bin"))
                {
                    toolStripStatusLabel1.Text = "Missing: " + currentDirectory + "\\CARD_Pass.bin! You will not see card passwords.";
                }
                if (!File.Exists(currentDirectory + "\\CARD_IntID.bin"))
                {
                    toolStripStatusLabel1.Text = "Missing: " + currentDirectory + "\\CARD_IntID.bin! Please check if all files are present.";
                    return;
                }


                if (!Directory.Exists(CurrentCacheDir))
                    Directory.CreateDirectory(CurrentCacheDir);
                ConvertDBToIni(currentDirectory, CurrentCacheIni, CurrentLang.ToString());
                ImportCardDB(CurrentCacheIni);
            }
            else if (string.Compare(Path.GetExtension(FileName), ".ini") == 0)
                ImportCardDB(FileName); // TODO: add error handling...
            else
            {
                toolStripStatusLabel1.Text = "Unknown extension: " + Path.GetExtension(FileName);
                return;
            }
            // Console.WriteLine("Imported " + ImportedCardsCount + " cards");
            GenerateListView();
            toolStripStatusLabel1.Text = "Imported " + ImportedCardsCount + " cards";
            toolStripStatusLabel2.Text = "Total card count: " + ImportedCardsCount + " | Displayed: " + DisplayedCardsCount;
            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            findToolStripMenuItem.Enabled = true;
            findNextToolStripMenuItem.Enabled = true;
            filterToolStripMenuItem.Enabled = true;
            replaceToolStripMenuItem.Enabled = true;

            toolStripButtonSave.Enabled = true;
            toolStripButtonSaveAs.Enabled = true;
            toolStripButtonFind.Enabled = true;
            toolStripButtonFindNext.Enabled = true;
            toolStripButtonFilter.Enabled = true;
            toolStripButtonReplace.Enabled = true;


            // update language combobox
            comboBox1.SelectedIndex = GetLangIndex();
            comboBox1.Enabled = true;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                CurrentlySelectedCard = SearchCardIndexByID(Int32.Parse(listView1.SelectedItems[0].SubItems[0].Text));
                propertyGrid1.SelectedObject = ImportDB[CurrentlySelectedCard];

                if (!bCurrentlySearching)
                {
                    toolStripStatusLabel1.Text = "Selected: [" + ImportDB[CurrentlySelectedCard].CardID + "] " + ImportDB[CurrentlySelectedCard].Name;
                    replaceParams.SearchResultIndex = -1;
                    replaceParams.SearchResultSubStrIndex = -1;
                }

                UpdateTexts();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string URL = "https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=2&cid=" + ImportDB[CurrentlySelectedCard].CardID + "&request_locale=";
            if (CurrentLang == 'J')
                URL += "ja";
            else if (CurrentLang == 'G')
                URL += "de";
            else if (CurrentLang == 'F')
                URL += "fr";
            else if (CurrentLang == 'I')
                URL += "it";
            else if (CurrentLang == 'S')
                URL += "es";
            else
                URL += "en";

            if (e.Button != MouseButtons.Right)
                System.Diagnostics.Process.Start(URL);
        }

        private void linkLabel1_MouseEnter(object sender, EventArgs e)
        {
            if (linkLabel1.Enabled)
            {
                string URL = "https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=2&&cid=" + ImportDB[CurrentlySelectedCard].CardID + "&&request_locale=";
                if (CurrentLang == 'J')
                    URL += "ja";
                else if (CurrentLang == 'G')
                    URL += "de";
                else if (CurrentLang == 'F')
                    URL += "fr";
                else if (CurrentLang == 'I')
                    URL += "it";
                else if (CurrentLang == 'S')
                    URL += "es";
                else
                    URL += "en";
                ClipboardURL = "https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=2&cid=" + ImportDB[CurrentlySelectedCard].CardID + "&request_locale=";
                if (CurrentLang == 'J')
                    ClipboardURL += "ja";
                else if (CurrentLang == 'G')
                    ClipboardURL += "de";
                else if (CurrentLang == 'F')
                    ClipboardURL += "fr";
                else if (CurrentLang == 'I')
                    ClipboardURL += "it";
                else if (CurrentLang == 'S')
                    ClipboardURL += "es";
                else
                    ClipboardURL += "en";

                linkLabel1.ContextMenuStrip = contextMenuStrip1;
                toolStripStatusLabel1.Text = URL;
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string URL = "https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=1&keyword=" + ImportDB[CurrentlySelectedCard].Name.Replace(' ', '+') + "&request_locale=";
            if (CurrentLang == 'J')
                URL += "ja";
            else if (CurrentLang == 'G')
                URL += "de";
            else if (CurrentLang == 'F')
                URL += "fr";
            else if (CurrentLang == 'I')
                URL += "it";
            else if (CurrentLang == 'S')
                URL += "es";
            else
                URL += "en";

            if (e.Button != MouseButtons.Right)
                System.Diagnostics.Process.Start(URL);
        }

        private void linkLabel2_MouseEnter(object sender, EventArgs e)
        {
            if (linkLabel2.Enabled)
            {
                string URL = "https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=1&&keyword=" + ImportDB[CurrentlySelectedCard].Name.Replace(' ', '+') + "&&request_locale=";
                if (CurrentLang == 'J')
                    URL += "ja";
                else if (CurrentLang == 'G')
                    URL += "de";
                else if (CurrentLang == 'F')
                    URL += "fr";
                else if (CurrentLang == 'I')
                    URL += "it";
                else if (CurrentLang == 'S')
                    URL += "es";
                else
                    URL += "en";
                linkLabel2.ContextMenuStrip = contextMenuStrip1;
                toolStripStatusLabel1.Text = URL;
                ClipboardURL = "https://www.db.yugioh-card.com/yugiohdb/card_search.action?ope=1&keyword=" + ImportDB[CurrentlySelectedCard].Name.Replace(' ', '+') + "&request_locale=";
                if (CurrentLang == 'J')
                    ClipboardURL += "ja";
                else if (CurrentLang == 'G')
                    ClipboardURL += "de";
                else if (CurrentLang == 'F')
                    ClipboardURL += "fr";
                else if (CurrentLang == 'I')
                    ClipboardURL += "it";
                else if (CurrentLang == 'S')
                    ClipboardURL += "es";
                else
                    ClipboardURL += "en";
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Enabled && textBox1.Focused)
            {
                toolStripStatusLabel1.Text = "Editing: [" + ImportDB[CurrentlySelectedCard].CardID + "] " + ImportDB[CurrentlySelectedCard].Name;
                ImportDB[CurrentlySelectedCard].Description = textBox1.Text.Replace("\r\n", "\n");
                bUnsavedChangesMade = true;
            }
        }

        private void propertyGrid1_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            if (propertyGrid1.Enabled)
            {
                toolStripStatusLabel1.Text = "Editing: [" + ImportDB[CurrentlySelectedCard].CardID + "] " + ImportDB[CurrentlySelectedCard].Name;
                UpdateTexts();
                bUnsavedChangesMade = true;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new AboutForm();
            if (Application.OpenForms[about.Name] == null)
                about.Show(this);
            else
                Application.OpenForms[about.Name].Activate();
        }

        private void copyLinkToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ClipboardURL);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ImportedCardsCount > 0 && !string.IsNullOrEmpty(CurrentFilename))
            {
                toolStripStatusLabel1.Text = "Saving to: " + CurrentFilename;

                toolStripProgressBar1.Enabled = true;
                toolStripProgressBar1.Visible = true;

                DoTheSaving(CurrentFilename);

                toolStripProgressBar1.Visible = false;
                toolStripProgressBar1.Enabled = false;

                toolStripStatusLabel1.Text = "Saved to: " + CurrentFilename;
                bUnsavedChangesMade = false;
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ImportedCardsCount > 0)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    saveFileDialog1_Ok();
                }
            }
        }

        private void saveFileDialog1_Ok()
        {
            toolStripStatusLabel1.Text = "Saving to: " + saveFileDialog1.FileName;

            toolStripProgressBar1.Enabled = true;
            toolStripProgressBar1.Visible = true;

            DoTheSaving(saveFileDialog1.FileName);

            toolStripProgressBar1.Visible = false;
            toolStripProgressBar1.Enabled = false;

            toolStripStatusLabel1.Text = "Saved to: " + saveFileDialog1.FileName;
            CurrentFilename = saveFileDialog1.FileName;
            bUnsavedChangesMade = false;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DisplayedCardsCount > 0 && comboBox1.Enabled && (listView1.SelectedItems.Count > 0))
            {
                CurrentLang = SetLangIndex(comboBox1.SelectedIndex);
                UpdateTexts();
            }
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (findBoxDialog.ShowDialog(this) == DialogResult.OK)
            {
                InitiateCardSearch(findBoxDialog.searchParams);
            }
        }

        private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((findBoxDialog.searchParams.SearchString == null) || (string.Compare(findBoxDialog.searchParams.SearchString, "") == 0))
            {
                findToolStripMenuItem_Click(sender, e);
                return;
            }
            InitiateCardSearch(findBoxDialog.searchParams);
        }

        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filterBoxDialog.ShowDialog(this) == DialogResult.OK)
            {
                // ... do filtering stuff ...
                FilterListView(filterBoxDialog.cardFilterParams);
                toolStripStatusLabel1.Text = DisplayedCardsCount + " card(s) have met these conditions.";
                toolStripStatusLabel2.Text = "Total card count: " + ImportedCardsCount + " | Displayed: " + DisplayedCardsCount;
                removeFilterToolStripMenuItem.Enabled = true;
                toolStripButtonRemoveFilter.Enabled = true;
            }
        }

        // mouse hovers for toolstrip

        private void openToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Open a Database";
        }

        private void saveToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Save the Database to the same location";
        }

        private void saveAsToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Save the Database to a new location";
        }

        private void exitToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Quit the utility";
        }

        private void findToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Search for card by a given parameter";
        }

        private void findNextToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Continue a previous search";
        }

        private void replaceToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Search a parameter and replace it";
        }

        private void filterToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Filter / narrow down the currently visible entries";
        }

        private void removeFilterToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Remove a previously set filter / narrowed search";
        }

        private void aboutToolStripMenuItem_MouseHover(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "About this utility";
        }

        private void removeFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //GenerateListView();
            listView1.ModelFilter = null;
            filterBoxDialog.ResetAllPages();
            toolStripStatusLabel2.Text = "Total card count: " + ImportedCardsCount + " | Displayed: " + DisplayedCardsCount;
            removeFilterToolStripMenuItem.Enabled = false;
            toolStripButtonRemoveFilter.Enabled = false;
        }

        private void toolbarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip1.Visible = toolbarToolStripMenuItem.Checked;
            Properties.Settings.Default.ShowToolbar = toolStrip1.Visible;
            Properties.Settings.Default.Save();
            //ConfigurationManager.AppSettings.Set("ShowToolbar", toolStrip1.Visible.ToString());
        }

        private void statusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip1.Visible = statusBarToolStripMenuItem.Checked;
            Properties.Settings.Default.ShowStatusBar = statusStrip1.Visible;
            Properties.Settings.Default.Save();
            //ConfigurationManager.AppSettings.Set("ShowStatusBar", statusStrip1.Visible.ToString());
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) 
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                OpenFile(files[0]);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!HandleUnsavedQuestion())
                e.Cancel = true;
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var replace = new ReplaceBox(replaceParams, this);
            if (Application.OpenForms[replace.Name] == null)
                replace.Show(this);
            else
                Application.OpenForms[replace.Name].Activate();
        }

        private void statusBarToolStripMenuItem_MouseEnter(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Enable/disable this status bar";
        }

        private void toolbarToolStripMenuItem_MouseEnter(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Enable/disable the toolbar";
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Directory.Exists(CurrentCacheDir))
                Directory.Delete(CurrentCacheDir, true);
        }
    }
}
