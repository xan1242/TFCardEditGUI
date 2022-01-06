using System;
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

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public TFCard[] ImportDB;
        public int ImportedCardsCount;
        public int CurrentlySelectedCard;
        

        int DisplayedCardsCount = 0;
        bool bCurrentlySearching = false;

        string ClipboardURL;
        string CurrentFilename;
        char CurrentLang = 'E';
        bool bJapaneseLangDetected = false;
        Font DefaultWestStyle = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
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
            listView1.Clear();
            listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            colCardID,
            colName,
            colKind,
            colLevel,
            colATK,
            colDEF,
            colType,
            colAttr,
            colIcon,
            colRarity,
            colPassword,
            colCardExists,
            colDescription});

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
                textBox1.Font = new Font(DefaultWestStyle, FontStyle.Italic);
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
            int li = 0;

            if (listView1.Items.Count != 0)
            {
                listView1.Clear();
                listView1.Columns.AddRange(new ColumnHeader[] {
                colCardID,
                colName,
                colKind,
                colLevel,
                colATK,
                colDEF,
                colType,
                colAttr,
                colIcon,
                colRarity,
                colPassword,
                colCardExists,
                colDescription});
            }

            for (int i = 0; i < ImportedCardsCount; i++)
            {
                AddListViewItem(li, i);
                li++;

            }
            DisplayedCardsCount = li;
        }

        bool FilterCheckMinMaxATK(CardFilterParams filterParams, int ci)
        {
            if ((filterParams.MaxATK >= 0) && (filterParams.MinATK >= 0))
            {
                if ((ImportDB[ci].ATK <= filterParams.MaxATK) && (ImportDB[ci].ATK >= filterParams.MinATK))
                    return true;
                else
                    return false;
            }
            else
            {
                if (filterParams.MaxATK >= 0)
                {
                    if (ImportDB[ci].ATK <= filterParams.MaxATK)
                        return true;
                    else
                        return false;
                }

                if (filterParams.MinATK >= 0)
                {
                    if (ImportDB[ci].ATK >= filterParams.MinATK)
                        return true;
                    else
                        return false;
                }
            }
            return true;
        }
        bool FilterCheckMinMaxDEF(CardFilterParams filterParams, int ci)
        {
            if ((filterParams.MaxDEF >= 0) && (filterParams.MinDEF >= 0))
            {
                if ((ImportDB[ci].DEF <= filterParams.MaxDEF) && (ImportDB[ci].DEF >= filterParams.MinDEF))
                    return true;
                else
                    return false;
            }
            else
            {
                if (filterParams.MaxDEF >= 0)
                {
                    if (ImportDB[ci].DEF <= filterParams.MaxDEF)
                        return true;
                    else
                        return false;
                }

                if (filterParams.MinDEF >= 0)
                {
                    if (ImportDB[ci].DEF >= filterParams.MinDEF)
                        return true;
                    else
                        return false;
                }
            }
            return true;
        }

        bool FilterCheckMinMaxLevel(CardFilterParams filterParams, int ci)
        {
            if ((filterParams.MaxLevel >= 0) && (filterParams.MinLevel >= 0))
            {
                if ((ImportDB[ci].Level <= filterParams.MaxLevel) && (ImportDB[ci].Level >= filterParams.MinLevel))
                    return true;
                else
                    return false;
            }
            else
            {
                if (filterParams.MaxLevel >= 0)
                {
                    if (ImportDB[ci].Level <= filterParams.MaxLevel)
                        return true;
                    else
                        return false;
                }

                if (filterParams.MinLevel >= 0)
                {
                    if (ImportDB[ci].Level >= filterParams.MinLevel)
                        return true;
                    else
                        return false;
                }
            }
            return true;
        }


        bool FilterCheckMinMaxPassword(CardFilterParams filterParams, int ci)
        {
            if ((filterParams.MaxPassword >= 0) && (filterParams.MinPassword >= 0))
            {
                if ((ImportDB[ci].Password <= filterParams.MaxPassword) && (ImportDB[ci].Password >= filterParams.MinPassword))
                    return true;
                else
                    return false;
            }
            else
            {
                if (filterParams.MaxPassword >= 0)
                {
                    if (ImportDB[ci].Password <= filterParams.MaxPassword)
                        return true;
                    else
                        return false;
                }

                if (filterParams.MinPassword >= 0)
                {
                    if (ImportDB[ci].Password >= filterParams.MinPassword)
                        return true;
                    else
                        return false;
                }
            }
            return true;
        }
        bool FilterCheckMinMaxCardID(CardFilterParams filterParams, int ci)
        {
            if ((filterParams.MaxCardID >= 0) && (filterParams.MinCardID >= 0))
            {
                if ((ImportDB[ci].CardID <= filterParams.MaxCardID) && (ImportDB[ci].CardID >= filterParams.MinCardID))
                    return true;
                else
                    return false;
            }
            else
            {
                if (filterParams.MaxCardID >= 0)
                {
                    if (ImportDB[ci].CardID <= filterParams.MaxCardID)
                        return true;
                    else
                        return false;
                }

                if (filterParams.MinCardID >= 0)
                {
                    if (ImportDB[ci].CardID >= filterParams.MinCardID)
                        return true;
                    else
                        return false;
                }
            }
            return true;
        }

        bool FilterCheckMinMaxValues(CardFilterParams filterParams, int ci)
        {
            return FilterCheckMinMaxATK(filterParams, ci) || FilterCheckMinMaxDEF(filterParams, ci) || FilterCheckMinMaxLevel(filterParams, ci) || FilterCheckMinMaxPassword(filterParams, ci) || FilterCheckMinMaxCardID(filterParams, ci);
        }

        bool FilterCheckName(CardFilterParams filterParams, int ci)
        {
            if (filterParams.MatchCase)
            {
                if (filterParams.MatchExact)
                {
                    if (ImportDB[ci].Name.Equals(filterParams.Name))
                        return true;
                }
                else if (ImportDB[ci].Name.Contains(filterParams.Name))
                    return true;
            }
            if (filterParams.MatchExact)
            {
                if (ImportDB[ci].Name.ToUpper().Equals(filterParams.Name.ToUpper()))
                    return true;
            }
            else if (ImportDB[ci].Name.ToUpper().Contains(filterParams.Name.ToUpper()))
                return true;

            return false;
        }

        bool FilterCheckKinds(CardFilterParams filterParams, int ci)
        {
            if ((ImportDB[ci].Kind == CardKinds.Normal) && filterParams.Kind[(int)CardKinds.Normal])
                return true;

            if (((ImportDB[ci].Kind == CardKinds.Effect) || (ImportDB[ci].Kind == CardKinds.Toon) || (ImportDB[ci].Kind == CardKinds.Spirit) || (ImportDB[ci].Kind == CardKinds.Union)) && filterParams.Kind[(int)CardKinds.Effect])
                return true;

            if (((ImportDB[ci].Kind == CardKinds.Fusion) || (ImportDB[ci].Kind == CardKinds.FusionEffect)) && filterParams.Kind[(int)CardKinds.Fusion])
                return true;

            if (((ImportDB[ci].Kind == CardKinds.Ritual) || (ImportDB[ci].Kind == CardKinds.RitualEffect)) && filterParams.Kind[(int)CardKinds.Ritual])
                return true;

            if ((ImportDB[ci].Kind == CardKinds.Token) && filterParams.Kind[(int)CardKinds.Token])
                return true;

            if ((ImportDB[ci].Kind == CardKinds.Toon) && filterParams.Kind[(int)CardKinds.Toon])
                return true;

            if ((ImportDB[ci].Kind == CardKinds.Spirit) && filterParams.Kind[(int)CardKinds.Spirit])
                return true;

            if ((ImportDB[ci].Kind == CardKinds.Union) && filterParams.Kind[(int)CardKinds.Union])
                return true;

            if ((ImportDB[ci].Kind == CardKinds.Spell) && filterParams.Kind[(int)CardKinds.Spell])
                return true;

            if ((ImportDB[ci].Kind == CardKinds.Trap) && filterParams.Kind[(int)CardKinds.Trap])
                return true;

            return false;
        }

        bool FilterCheckTypes(CardFilterParams filterParams, int ci)
        {
            if ((ImportDB[ci].Type == CardTypes.Dragon) && filterParams.Type[(int)CardTypes.Dragon])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Zombie) && filterParams.Type[(int)CardTypes.Zombie])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Fiend) && filterParams.Type[(int)CardTypes.Fiend])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Pyro) && filterParams.Type[(int)CardTypes.Pyro])
                return true;

            if ((ImportDB[ci].Type == CardTypes.SeaSerpent) && filterParams.Type[(int)CardTypes.SeaSerpent])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Rock) && filterParams.Type[(int)CardTypes.Rock])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Machine) && filterParams.Type[(int)CardTypes.Machine])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Fish) && filterParams.Type[(int)CardTypes.Fish])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Dinosaur) && filterParams.Type[(int)CardTypes.Dinosaur])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Insect) && filterParams.Type[(int)CardTypes.Insect])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Beast) && filterParams.Type[(int)CardTypes.Beast])
                return true;

            if ((ImportDB[ci].Type == CardTypes.BeastWarrior) && filterParams.Type[(int)CardTypes.BeastWarrior])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Plant) && filterParams.Type[(int)CardTypes.Plant])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Aqua) && filterParams.Type[(int)CardTypes.Aqua])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Warrior) && filterParams.Type[(int)CardTypes.Warrior])
                return true;

            if ((ImportDB[ci].Type == CardTypes.WingedBeast) && filterParams.Type[(int)CardTypes.WingedBeast])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Fairy) && filterParams.Type[(int)CardTypes.Fairy])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Spellcaster) && filterParams.Type[(int)CardTypes.Spellcaster])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Thunder) && filterParams.Type[(int)CardTypes.Thunder])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Reptile) && filterParams.Type[(int)CardTypes.Reptile])
                return true;

            if ((ImportDB[ci].Type == CardTypes.DivineBeast) && filterParams.Type[(int)CardTypes.DivineBeast])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Spell) && filterParams.Type[(int)CardTypes.Spell])
                return true;

            if ((ImportDB[ci].Type == CardTypes.Trap) && filterParams.Type[(int)CardTypes.Trap])
                return true;

            if ((ImportDB[ci].Type == CardTypes.None) && filterParams.Type[(int)CardTypes.None])
                return true;

            return false;
        }

        bool FilterCheckAttr(CardFilterParams filterParams, int ci)
        {
            if ((ImportDB[ci].Attr == CardAttributes.LIGHT) && filterParams.Attr[(int)CardAttributes.LIGHT])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.DARK) && filterParams.Attr[(int)CardAttributes.DARK])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.WATER) && filterParams.Attr[(int)CardAttributes.WATER])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.FIRE) && filterParams.Attr[(int)CardAttributes.FIRE])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.EARTH) && filterParams.Attr[(int)CardAttributes.EARTH])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.WIND) && filterParams.Attr[(int)CardAttributes.WIND])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.DIVINE) && filterParams.Attr[(int)CardAttributes.DIVINE])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.SPELL) && filterParams.Attr[(int)CardAttributes.SPELL])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.TRAP) && filterParams.Attr[(int)CardAttributes.TRAP])
                return true;
            if ((ImportDB[ci].Attr == CardAttributes.None) && filterParams.Attr[(int)CardAttributes.None])
                return true;

            return false;
        }
        bool FilterCheckIcon(CardFilterParams filterParams, int ci)
        {
            if ((ImportDB[ci].Icon == CardIcons.Counter) && filterParams.Icon[(int)CardIcons.Counter])
                return true;
            if ((ImportDB[ci].Icon == CardIcons.Field) && filterParams.Icon[(int)CardIcons.Field])
                return true;
            if ((ImportDB[ci].Icon == CardIcons.Equip) && filterParams.Icon[(int)CardIcons.Equip])
                return true;
            if ((ImportDB[ci].Icon == CardIcons.Continuous) && filterParams.Icon[(int)CardIcons.Continuous])
                return true;
            if ((ImportDB[ci].Icon == CardIcons.QuickPlay) && filterParams.Icon[(int)CardIcons.QuickPlay])
                return true;
            if ((ImportDB[ci].Icon == CardIcons.RitualSpell) && filterParams.Icon[(int)CardIcons.RitualSpell])
                return true;
            if ((ImportDB[ci].Icon == CardIcons.None) && filterParams.Icon[(int)CardIcons.None])
                return true;
            return false;
        }

        bool FilterCheckRarity(CardFilterParams filterParams, int ci)
        {
            if ((ImportDB[ci].Rarity == CardRarity.Common) && filterParams.Rarity[(int)CardRarity.Common])
                return true;
            if ((ImportDB[ci].Rarity == CardRarity.Rare) && filterParams.Rarity[(int)CardRarity.Rare])
                return true;
            if ((ImportDB[ci].Rarity == CardRarity.SuperRare) && filterParams.Rarity[(int)CardRarity.SuperRare])
                return true;
            if ((ImportDB[ci].Rarity == CardRarity.UltraRare) && filterParams.Rarity[(int)CardRarity.UltraRare])
                return true;
            if ((ImportDB[ci].Rarity == CardRarity.UltimateRare) && filterParams.Rarity[(int)CardRarity.UltimateRare])
                return true;
            return false;
        }

        void FilterListView(CardFilterParams filterParams) // TODO: MAKE THIS WITH VIRTUAL LISTVIEW!!!
        {
            int li = 0;
            int ci; // curr. card index
            int[] cids = new int[DisplayedCardsCount];
            int cidcopycounter = 0;
            bool bAdding = true;

            // copy card IDs from the listview before clearing them...
            foreach (ListViewItem item in listView1.Items)
            {
                cids[cidcopycounter] = Int32.Parse(item.SubItems[0].Text);
                cidcopycounter++;
            }

            listView1.Clear();
            listView1.Columns.AddRange(new ColumnHeader[] {
            colCardID,
            colName,
            colKind,
            colLevel,
            colATK,
            colDEF,
            colType,
            colAttr,
            colIcon,
            colRarity,
            colPassword,
            colCardExists, 
            colDescription});

            for (int i = 0; i < DisplayedCardsCount; i++)
            {
                ci = SearchCardIndexByID(cids[i]);

                if (!string.IsNullOrEmpty(filterParams.Name) && !FilterCheckName(filterParams, ci))
                    bAdding = false;

                if (filterParams.bAreAnyFiltersEnabled_Kind() && !FilterCheckKinds(filterParams, ci))
                    bAdding = false;

                if (filterParams.bAreAnyFiltersEnabled_Type() && !FilterCheckTypes(filterParams, ci))
                    bAdding = false;

                if (filterParams.bAreAnyFiltersEnabled_Attr() && !FilterCheckAttr(filterParams, ci))
                    bAdding = false;

                if (filterParams.bAreAnyFiltersEnabled_Icon() && !FilterCheckIcon(filterParams, ci))
                    bAdding = false;

                if (filterParams.bAreAnyFiltersEnabled_Rarity() && !FilterCheckRarity(filterParams, ci))
                    bAdding = false;

                if (filterParams.MaxATK >= 0 && !(ImportDB[ci].ATK <= filterParams.MaxATK))
                    bAdding = false;
                if (filterParams.MinATK >= 0 && !(ImportDB[ci].ATK >= filterParams.MinATK))
                    bAdding = false;

                if (filterParams.MaxDEF >= 0 && !(ImportDB[ci].DEF <= filterParams.MaxDEF))
                    bAdding = false;
                if (filterParams.MinDEF >= 0 && !(ImportDB[ci].DEF >= filterParams.MinDEF))
                    bAdding = false;

                if (filterParams.MaxLevel >= 0 && !(ImportDB[ci].Level <= filterParams.MaxLevel))
                    bAdding = false;
                if (filterParams.MinLevel >= 0 && !(ImportDB[ci].Level >= filterParams.MinLevel))
                    bAdding = false;

                if (filterParams.MaxPassword >= 0 && !(ImportDB[ci].Password <= filterParams.MaxPassword))
                    bAdding = false;
                if (filterParams.MinPassword >= 0 && !(ImportDB[ci].Password >= filterParams.MinPassword))
                    bAdding = false;

                if (filterParams.MaxCardID >= 0 && !(ImportDB[ci].CardID <= filterParams.MaxCardID))
                    bAdding = false;
                if (filterParams.MinCardID >= 0 && !(ImportDB[ci].CardID >= filterParams.MinCardID))
                    bAdding = false;

                if (ImportDB[ci].CardExistFlag != filterParams.CardExists)
                    bAdding = false;

                if (bAdding)
                {
                    AddListViewItem(li, ci);
                    li++;
                }

                bAdding = true;
            }

            DisplayedCardsCount = li;
        }

        public bool InitiateCardSearch(CardSearchParams searchParams)
        {
            string dispstr;
            bCurrentlySearching = true;
            if (cardSearch.Search(searchParams, listView1))
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
            }
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
            if (File.Exists(Path + "\\CARD_SamePict_" + CurrentLang.ToString() + ".bin") && !File.Exists("workehp\\CARD_SamePict_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\CARD_SamePict_" + CurrentLang.ToString() + ".bin", "workehp\\CARD_SamePict_" + CurrentLang.ToString() + ".bin");
            if (File.Exists(Path + "\\CARD_Sort_" + CurrentLang.ToString() + ".bin") && !File.Exists("workehp\\CARD_Sort_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\CARD_Sort_" + CurrentLang.ToString() + ".bin", "workehp\\CARD_Sort_" + CurrentLang.ToString() + ".bin");
            if (File.Exists(Path + "\\CARD_Top_" + CurrentLang.ToString() + ".bin") && !File.Exists("workehp\\CARD_Top_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\CARD_Top_" + CurrentLang.ToString() + ".bin", "workehp\\CARD_Top_" + CurrentLang.ToString() + ".bin");
            if (File.Exists(Path + "\\DLG_Indx_" + CurrentLang.ToString() + ".bin") && !File.Exists("workehp\\DLG_Indx_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\DLG_Indx_" + CurrentLang.ToString() + ".bin", "workehp\\DLG_Indx_" + CurrentLang.ToString() + ".bin");
            if (File.Exists(Path + "\\DLG_Text_" + CurrentLang.ToString() + ".bin") && !File.Exists("workehp\\DLG_Text_" + CurrentLang.ToString() + ".bin"))
                File.Copy(Path + "\\DLG_Text_" + CurrentLang.ToString() + ".bin", "workehp\\DLG_Text_" + CurrentLang.ToString() + ".bin");

            if (File.Exists(Path + "\\CARD_Genre.bin") && !File.Exists(Path + "workehp\\CARD_Genre.bin"))
                File.Copy(Path + "\\CARD_Genre.bin", "workehp\\CARD_Genre.bin");
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
                ImportDB[CardImporterCounter].Description = ImportDB[CardImporterCounter].Description.Replace('^', '\n');

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
                ExportIni.Sections.GetSectionData(ImportDB[i].CardID.ToString()).Keys.AddKey("Description", ImportDB[i].Description.Replace('\n', '^'));
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
                ExportCardDB("workehp.ini", 20);
                if (!Directory.Exists("workehp"))
                    Directory.CreateDirectory("workehp");
                ConvertIniToDB("workehp.ini", "workehp", CurrentLang.ToString());
                toolStripProgressBar1.Value += 10;

                // check if all necessary files are in the work folder, if not, ask the user to find them
                while (bFileCheckLoop)
                {
                    bFileCheckLoop = false;
                    if (!File.Exists("workehp\\CARD_SamePict_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: CARD_SamePict_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "CARD_SamePict_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists("workehp\\CARD_Sort_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: CARD_Sort_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "CARD_Sort_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists("workehp\\CARD_Top_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: CARD_Top_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "CARD_Top_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists("workehp\\DLG_Indx_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: DLG_Indx_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "DLG_Indx_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists("workehp\\DLG_Text_" + CurrentLang.ToString() + ".bin"))
                    {
                        bFileCheckLoop = true;
                        toolStripStatusLabel1.Text = "Missing: DLG_Text_" + CurrentLang.ToString() + ".bin! Please point to the directory containing the files.";
                        openFileDialog2.FileName = "DLG_Text_" + CurrentLang.ToString() + ".bin";
                        var openFileResult = openFileDialog2.ShowDialog();
                        if (openFileResult != DialogResult.OK)
                            return;
                        CopyFilesForEHP(Path.GetDirectoryName(openFileDialog2.FileName));
                    }
                    if (!File.Exists("workehp\\CARD_Genre.bin"))
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

                PackEHP("workehp", Filename);
                toolStripProgressBar1.Value += 10;
            }
            else if (string.Compare(Path.GetExtension(Filename), ".bin") == 0)
            {
                ExportCardDB("workehp.ini", 10);
                ConvertIniToDB("workehp.ini", Path.GetDirectoryName(Filename), CurrentLang.ToString());
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

            if (string.Compare(Path.GetExtension(FileName), ".ehp") == 0)
            {
                if (File.Exists("workehp.ini"))
                    File.Delete("workehp.ini");
                if (Directory.Exists("workehp"))
                    Directory.Delete("workehp", true);


                // invoke the toolchain...
                ExtractEHP(FileName, "workehp");
                if (!File.Exists("workehp\\CARD_Name_" + CurrentLang.ToString() + ".bin"))
                {
                    toolStripStatusLabel1.Text = "Invalid EhFolder! Please use only cardinfo_* ehp files.";
                    if (File.Exists("workehp.ini"))
                        File.Delete("workehp.ini");
                    if (Directory.Exists("workehp"))
                        Directory.Delete("workehp", true);

                    return;
                }
                ConvertDBToIni("workehp", "workehp.ini", CurrentLang.ToString());
                ImportCardDB("workehp.ini");
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

                ConvertDBToIni(currentDirectory, "workehp.ini", CurrentLang.ToString());
                ImportCardDB("workehp.ini");
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
            toolStripStatusLabel1.Text = "Filter / narrow down the currently visible entries (stackable)";
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
            GenerateListView();
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
    }
}
