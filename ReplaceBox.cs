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

namespace WindowsFormsApp1
{
    public partial class ReplaceBox : Form
    {
        public CardSearchParams searchParams; // this needs to be defined outside because we're using Show() to display this dialog, and when it's closed it purges it from memory...
        Form1 form1;

        public ReplaceBox(CardSearchParams inSearchParams, Form1 inForm1)
        {
            InitializeComponent();
            //searchParams = new CardSearchParams();

            searchParams = inSearchParams;
            form1 = inForm1;
        }

        void SaveCheckboxes()
        {
            searchParams.bSearchCardID =        radioButtonCardID.Checked;
            searchParams.bSearchName =          radioButtonName.Checked;
            searchParams.bSearchKind =          radioButtonKind.Checked;
            searchParams.bSearchLevel =         radioButtonLevel.Checked;
            searchParams.bSearchATK =           radioButtonATK.Checked;
            searchParams.bSearchDEF =           radioButtonDEF.Checked;
            searchParams.bSearchType =          radioButtonType.Checked;
            searchParams.bSearchAttr =          radioButtonAttr.Checked;
            searchParams.bSearchIcon =          radioButtonIcon.Checked;
            searchParams.bSearchRarity =        radioButtonRarity.Checked;
            searchParams.bSearchPassword =      radioButtonPassword.Checked;
            searchParams.bSearchCardExists =    radioButtonCardExists.Checked;

            searchParams.bMatchWhole = checkBoxMatch.Checked;
            searchParams.bMatchCase = checkBoxCase.Checked;
        }

        void LoadCheckboxes()
        {
            radioButtonCardID.Checked = searchParams.bSearchCardID;
            radioButtonName.Checked = searchParams.bSearchName;
            radioButtonKind.Checked = searchParams.bSearchKind;
            radioButtonLevel.Checked = searchParams.bSearchLevel;
            radioButtonATK.Checked = searchParams.bSearchATK;
            radioButtonDEF.Checked = searchParams.bSearchDEF;
            radioButtonType.Checked = searchParams.bSearchType;
            radioButtonAttr.Checked = searchParams.bSearchAttr;
            radioButtonIcon.Checked = searchParams.bSearchIcon;
            radioButtonRarity.Checked = searchParams.bSearchRarity;
            radioButtonPassword.Checked = searchParams.bSearchPassword;
            radioButtonCardExists.Checked = searchParams.bSearchCardExists;

            checkBoxMatch.Checked = searchParams.bMatchWhole;
            checkBoxCase.Checked = searchParams.bMatchCase;
        }


        private void ReplaceBox_Load(object sender, EventArgs e)
        {
            fastTextBoxFind.Text = searchParams.SearchString;

            LoadCheckboxes();

            // start pos at the corner of the parent window
            Location = new Point(Owner.Location.X + 40, Owner.Location.Y + 40);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveCheckboxes();

            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(fastTextBoxFind.Text))
            {
                button1.Enabled = false;
                buttonReplace.Enabled = false;
                buttonReplaceAll.Enabled = false;
            }
            else
            {
                button1.Enabled = true;
                buttonReplace.Enabled = true;
                buttonReplaceAll.Enabled = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveCheckboxes();

            DialogResult = DialogResult.OK;
            form1.InitiateCardSearch(searchParams);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            searchParams.SearchString = fastTextBoxFind.Text;
        }

        private void ReplaceBox_Activated(object sender, EventArgs e)
        {
            fastTextBoxFind.Focus();
        }
    }
}
