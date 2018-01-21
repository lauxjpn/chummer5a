/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
using Chummer.Backend;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
    public partial class frmSelectLifeModule : Form
    {
        public bool AddAgain { get; private set; }
        private readonly Character _objCharacter;
        private int _intStage;
        private String _strDefaultStageName;
        private XmlDocument _xmlDocument;
        private String _selectedId;
        private Regex searchRegex;


        private String _strWorkStage = null;
        
        public frmSelectLifeModule(Character objCharacter, int stage)
        {
            InitializeComponent();
            LanguageManager.TranslateWinForm(GlobalOptions.Language, this);
            _objCharacter = objCharacter;
            _intStage = stage;
            MoveControls();
        }

        private void frmSelectLifeModule_Load(object sender, EventArgs e)
        {
            MoveControls();

            _xmlDocument = XmlManager.Load("lifemodules.xml");
            String selectString = "chummer/stages/stage[@order = \"" + _intStage + "\"]";

            XmlNode stageNode = _xmlDocument.SelectSingleNode(selectString);
            if (stageNode != null)
            {
                _strWorkStage = _strDefaultStageName = stageNode.InnerText;

                BuildTree(GetSelectString());
            }
            else
            {
                _strWorkStage = _strDefaultStageName = "null";
            }
        }

        private void BuildTree(String stageString)
        {
            XmlNodeList matches = _xmlDocument.SelectNodes("chummer/modules/module" + stageString);
            treModules.Nodes.Clear();
            treModules.Nodes.AddRange(
                BuildList(matches));
        }

        private TreeNode[] BuildList(XmlNodeList xmlNodes)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            for (int i = 0; i < xmlNodes.Count; i++)
            {
                XmlNode xmlNode = xmlNodes[i];

                if (!chkLimitList.Checked || xmlNode.RequirementsMet(_objCharacter))
                {

                    TreeNode treNode = new TreeNode
                    {
                        Text = xmlNode["name"].InnerText
                    };
                    if (xmlNode["versions"] != null)
                    {
                        treNode.Nodes.AddRange(
                            BuildList(xmlNode.SelectNodes("versions/version[" + _objCharacter.Options.BookXPath() + "or not(source)]")));
                    }

                    treNode.Tag = xmlNode["id"].InnerText;
                    if (searchRegex != null)
                    {
                        if (searchRegex.IsMatch(treNode.Text))
                        {
                            nodes.Add(treNode);
                        }
                        else if (treNode.Nodes.Count != 0)
                        {
                            nodes.Add(treNode);
                        }
                    }
                    else
                    {
                        nodes.Add(treNode);
                    }
                    
                }
            }

            return nodes.ToArray();
        }
        
        private void cmdOK_Click(object sender, EventArgs e)
        {
            AddAgain = false;
            DialogResult = DialogResult.OK;
        }

        private void cmdOKAdd_Click(object sender, EventArgs e)
        {
            AddAgain = true;
            DialogResult = DialogResult.OK;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void MoveControls()
        {
            int intWidth = Math.Max(lblBPLabel.Width, lblSourceLabel.Width);
            lblBP.Left = lblBPLabel.Left + intWidth + 6;
            lblSource.Left = lblSourceLabel.Left + intWidth + 6;

            lblSearch.Left = txtSearch.Left - 6 - lblSearch.Width;
        }

        private void treModules_AfterSelect(object sender, TreeViewEventArgs e)
        {

            bool blnSelectAble;
            if (e.Node.Nodes.Count == 0)
            {
                blnSelectAble = true;
            }
            else
            {
                //Select any node that have an id node equal to tag
                String selectString = "//*[id = \"" + e.Node.Tag + "\"]/selectable";
                XmlNode node = _xmlDocument.SelectSingleNode(selectString);
                //if it contains >selectable>True</selectable>, yes or </selectable>
                //set button to selectable, otherwise not
                blnSelectAble = (node != null && (node.InnerText == bool.TrueString || node.OuterXml.EndsWith("/>")));
            }

            _selectedId = (string)e.Node.Tag;
            XmlNode selectedNodeInfo = Quality.GetNodeOverrideable(_selectedId, XmlManager.Load("lifemodules.xml", GlobalOptions.Language));

            if (selectedNodeInfo != null)
            {
                cmdOK.Enabled = blnSelectAble;
                cmdOKAdd.Enabled = blnSelectAble;

                lblBP.Text = selectedNodeInfo["karma"]?.InnerText ?? string.Empty;
                lblSource.Text = selectedNodeInfo["source"]?.InnerText ?? string.Empty + ' ' + selectedNodeInfo["page"]?.InnerText ?? string.Empty;

                lblStage.Text = selectedNodeInfo["stage"]?.InnerText ?? string.Empty;
            }
            else
            {
                lblBP.Text = "ERROR";
                lblStage.Text = "ERROR";
                lblSource.Text = "ERROR";

                cmdOK.Enabled = false;
                cmdOKAdd.Enabled = false;
            }

        }

        public XmlNode SelectedNode
        {
            get { return Quality.GetNodeOverrideable(_selectedId, XmlManager.Load("lifemodules.xml", GlobalOptions.Language)); }
        }

        private void treModules_DoubleClick(object sender, EventArgs e)
        {
            if (cmdOK.Enabled)
            {
                AddAgain = false;
                cmdOK_Click(sender, e);
            }
        }

        private void chkLimitList_Click(object sender, EventArgs e)
        {
            cboStage.BeginUpdate();
            lblStage.Visible = chkLimitList.Checked;
            cboStage.Visible = !chkLimitList.Checked;

            if (cboStage.Visible)
            {
                if (cboStage.DataSource == null)
                {
                    List<ListItem> Stages = new List<ListItem>()
                    {
                        new ListItem("0", LanguageManager.GetString("String_All", GlobalOptions.Language))
                    };

                    XmlNodeList xnodes = _xmlDocument.SelectNodes("/chummer/stages/stage");
                    foreach (XmlNode xnode in xnodes)
                    {
                        XmlAttribute attrib = xnode.Attributes["order"];
                        if (attrib != null)
                        {
                            Stages.Add(new ListItem(xnode.Attributes["order"].Value, xnode.InnerText));
                        }
                    }

                    //Sort based on integer value of key
                    Stages.Sort((x, y) =>
                    {
                        int yint = 0;
                        if (int.TryParse(x.Value, out int xint))
                        {
                            if (int.TryParse(y.Value, out yint))
                            {
                                return xint - yint;
                            }
                            else
                            {
                                return 1;
                            }
                        }
                        else
                        {
                            if (int.TryParse(y.Value, out yint))
                            {
                                return -1;
                            }
                            else
                            {
                                return 0;
                            }
                        }
                    });

                    cboStage.ValueMember = "Value";
                    cboStage.DisplayMember = "Name";
                    cboStage.DataSource = Stages;
                }

                ListItem selectedItem = ((List<ListItem>) cboStage.DataSource).Find(x => x.Value == _intStage.ToString());
                if (!string.IsNullOrEmpty(selectedItem.Name))
                    cboStage.SelectedItem = selectedItem;

            }
            else
            {
                _strWorkStage = _strDefaultStageName;
                BuildTree(GetSelectString());
            }
            cboStage.EndUpdate();
        }

        private void cboStage_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string strSelected = (string) cboStage.SelectedValue;
            if (strSelected == "0")
            {
                _strWorkStage = null;
                BuildTree(GetSelectString());
            }
            else
            {
                string strNodeSelect = "chummer/stages/stage[@order = \"" + strSelected + "\"]";
                _strWorkStage = _xmlDocument.SelectSingleNode(strNodeSelect).InnerText;
                BuildTree(GetSelectString());
            }
            
        }
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                searchRegex = null;
            }
            else
            {
                try
                {
                    searchRegex = new Regex(txtSearch.Text, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    //No other way to check for a valid regex that i know of
                }
            }
            
            BuildTree(GetSelectString());
        }

        private string GetSelectString()
        {
            string working = "[(" + _objCharacter.Options.BookXPath();

            ///chummer/modules/module//name[contains(., "C")]/..["" = string.Empty]
            /// /chummer/modules/module//name[contains(., "can")]/..[id]

            //if (!string.IsNullOrWhiteSpace(_strSearch))
            //{
            //    working = string.Format("//name[contains(., \"{0}\")]..[", _strSearch);
            //    before = true;
            //}
            if (!string.IsNullOrWhiteSpace(_strWorkStage))
            {
                working += ") and (stage = \"" + _strWorkStage + '\"';
            }
            working += ")]";


            return working;
        }
    }
}
