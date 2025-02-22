﻿using CaeGlobals;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeModel;
using PrePoMax.Commands;

namespace PrePoMax.Forms
{
    public partial class FrmMaterialLibrary : Form
    {
        // Variables                                                                                                                
        private Controller _controller;
        private bool _modelChanged;
        private FrmMaterial _frmMaterial;
        private int _yPadding;
        private object _previousControl;
        private bool _prevClickDouble;
        static bool _collapsed = true;


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public FrmMaterialLibrary(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _modelChanged = false;
            _previousControl = null;
            //
            _controller.Model.UnitSystem.SetConverterUnits();
        }


        // Event handlers                                                                                                           
        private void FrmMaterialLibrary_Load(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model != null)
                {
                    ListViewItem item;
                    foreach (var entry in _controller.Model.Materials)
                    {
                        item = lvModelMaterials.Items.Add(entry.Value.Name);
                        item.Name = entry.Value.Name;
                        item.Tag = entry.Value; // do not clone, to determine if the material changed
                    }
                    if (lvModelMaterials.Items.Count > 0)
                    {
                        lvModelMaterials.Items[0].Selected = true;
                        _previousControl = lvModelMaterials;
                    }
                }
                // Load material libraries
                string fileName = Path.Combine(Application.StartupPath, Globals.MaterialLibraryFileName);
                LoadMaterialLibraryFromFile(fileName);
                //
                foreach (var materialLibraryFile in _controller.Settings.General.GetMaterialLibraryFiles())
                {
                    LoadMaterialLibraryFromFile(materialLibraryFile);
                }
                //
                TreeNode materialNode;
                GetNodeContainingFirstMaterial(cltvLibrary.Nodes[0], out materialNode);
                if (materialNode != null) cltvLibrary.SelectedNode = materialNode;
                else cltvLibrary.SelectedNode = cltvLibrary.Nodes[0];
                if (_previousControl == null) _previousControl = cltvLibrary;
                //
                _frmMaterial = new FrmMaterial(_controller);
                _frmMaterial.Text = "Preview Material Properties";
                _frmMaterial.VisibleChanged += _frmMaterial_VisibleChanged;
                _frmMaterial.PrepareFormForPreview();
                //
                if (cltvLibrary.SelectedNode.Tag != null)
                {
                    Material previewMaterial = (Material)cltvLibrary.SelectedNode.Tag.DeepClone();
                    _frmMaterial.Material = previewMaterial;
                }
                //
                _yPadding = gbLibraries.Bottom - gbLibraryMaterials.Top;
                //
                gbLibraries.IsCollapsed = _collapsed;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void _frmMaterial_VisibleChanged(object sender, EventArgs e)
        {
            if (_frmMaterial.Visible) { }
            else
            {
                if (cbPreview.Checked) cbPreview.Checked = false;
            }

        }
        private void gbLibraries_OnCollapsedChanged(object sender)
        {
            int newPadding = gbLibraries.Bottom - gbLibraryMaterials.Top;
            if (newPadding != _yPadding)
            {
                int delta = newPadding - _yPadding;
                gbLibraryMaterials.Top += delta;
                gbModelMaterials.Top += delta;
                btnCopyToModel.Top += delta;
                btnCopyToLibrary.Top += delta;
                btnMoveUp.Top += delta;
                btnMoveDown.Top += delta;
                //
                gbLibraryMaterials.Height -= delta;
                gbModelMaterials.Height -= delta;
            }
            _collapsed = gbLibraries.IsCollapsed;
        }
        // Libraries
        private void btnNew_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Material library files | *.lib";
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        MaterialLibraryItem materialLibrary = new MaterialLibraryItem("Materials");
                        SaveMaterialLibraryToFile(saveFileDialog.FileName, materialLibrary);
                        //
                        LoadMaterialLibraryFromFile(saveFileDialog.FileName);
                        _controller.AddMaterialLibraryFile(saveFileDialog.FileName);
                        SetControlStates();
                    }
                }
            }
            catch
            { }
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Material library files | *.lib";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        LoadMaterialLibraryFromFile(openFileDialog.FileName);
                        _controller.AddMaterialLibraryFile(openFileDialog.FileName);
                        SetControlStates();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is CaeException ce) ExceptionTools.Show(this, ce);
            }
        }
        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lvLibraries.PossiblySelectedItems.Count == 1)
            {
                _controller.RemoveMaterialLibraryFile(lvLibraries.PossiblySelectedItems[0].Text);
                //
                int selectedId = lvLibraries.PossiblySelectedItems[0].Index;
                lvLibraries.SelectedIndices.Clear();
                lvLibraries.Items.RemoveAt(selectedId);
                //
                if (selectedId < lvLibraries.Items.Count) lvLibraries.Items[selectedId].Selected = true;
                else if (lvLibraries.Items.Count > 0) lvLibraries.Items[lvLibraries.Items.Count - 1].Selected = true;
                else if (lvLibraries.Items.Count == 0) SetControlStates();
            }
        }
        private void lvLibraries_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (lvLibraries.PossiblySelectedItems.Count == 1)
                {
                    MaterialLibraryItem mli = (MaterialLibraryItem)lvLibraries.PossiblySelectedItems[0].Tag;
                    ClearTree();
                    FillTree(mli, cltvLibrary.Nodes[0]);
                }
            }
            catch { }
        }
        private void LibraryChanged()
        {
            if (lvLibraries.PossiblySelectedItems.Count == 1)
            {
                MaterialLibraryItem materialLibrary = (MaterialLibraryItem)lvLibraries.PossiblySelectedItems[0].Tag;
                materialLibrary.Items.Clear();
                TreeNodesToItemList(cltvLibrary.Nodes[0], materialLibrary);
                //
                if (!lvLibraries.PossiblySelectedItems[0].Text.EndsWith("*")) lvLibraries.PossiblySelectedItems[0].Text += "*";
            }
        }
        private bool AnyLibraryChanged()
        {
            foreach (ListViewItem item in lvLibraries.Items)
            {
                if (item.Text.EndsWith("*")) return true;
            }
            return false;
        }
        private void SetControlStates()
        {
            bool enabled = true;
            if (lvLibraries.Items.Count == 0)
            {
                ClearTree();
                tbCategoryName.Text = "";
                //
                enabled = false;
            }
            //
            gbLibraryMaterials.Enabled = enabled;
            btnCopyToModel.Enabled = enabled;
            btnCopyToLibrary.Enabled = enabled;
        }
        private void ClearTree()
        {
            cltvLibrary.BeginUpdate();
            cltvLibrary.Nodes[0].Nodes.Clear();
            cltvLibrary.EndUpdate();
        }
        //
        private void cltvLibrary_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (cltvLibrary.SelectedNode != null)
                {
                    cltvLibrary.SelectedNode.EnsureVisible();
                    tbCategoryName.Text = cltvLibrary.SelectedNode.Text;
                    //
                    if (cltvLibrary.SelectedNode.Tag != null)
                    {
                        if (_frmMaterial != null)
                        {
                            Material previewMaterial = (Material)cltvLibrary.SelectedNode.Tag.DeepClone();
                            _frmMaterial.Material = previewMaterial;
                        }
                    }
                }
                if (!_prevClickDouble) _previousControl = cltvLibrary;
                _prevClickDouble = false;
            }
            catch
            { }
        }
        private void cltvLibrary_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (cltvLibrary.SelectedNode != null && cltvLibrary.SelectedNode.Tag != null)
            {
                btnCopyToModel_Click(null, null);
            }
            _prevClickDouble = true;
        }
        //
        private void lvModelMaterials_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                _previousControl = lvModelMaterials;
                //
                if (lvModelMaterials.SelectedItems != null && lvModelMaterials.SelectedItems.Count == 1 &&
                   lvModelMaterials.SelectedItems[0].Tag != null)
                {
                    if (_frmMaterial.Material != null)
                    {
                        Material previewMaterial = (Material)lvModelMaterials.SelectedItems[0].Tag.DeepClone();
                        _frmMaterial.Material = previewMaterial;
                    }
                }
            }
            catch
            { }
        }
        //
        private void tbCategoryName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;  // no beep
            }
        }
        private void tbCategoryName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnRename_Click(null, null);
        }
        //
        private void btnAddCategory_Click(object sender, EventArgs e)
        {
            try
            {
                TreeNode parentNode;
                if (cltvLibrary.SelectedNode != null) parentNode = cltvLibrary.SelectedNode;
                else parentNode = cltvLibrary.Nodes[0];

                if (parentNode.Tag == null)
                {
                    TreeNode node = parentNode.Nodes.Add("NewCategory");
                    node.Name = "NewCategory";

                    parentNode.Expand();
                    cltvLibrary.SelectedNode = node;
                    cltvLibrary.SelectedNode.EnsureVisible();
                    ApplyFormattingRecursive(node);
                    cltvLibrary.Focus();

                    tbCategoryName.Text = node.Name;
                    tbCategoryName.Focus();

                    LibraryChanged();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnDeleteFromLibrary_Click(object sender, EventArgs e)
        {
            TreeNode parent = cltvLibrary.SelectedNode.Parent;
            if (cltvLibrary.SelectedNode != null && parent != null)
            {
                int selectedId = cltvLibrary.SelectedNode.Index;                
                //
                parent.Nodes.Remove(cltvLibrary.SelectedNode);
                LibraryChanged();
                //
                if (selectedId == parent.Nodes.Count) selectedId--;
                if (selectedId >= 0) cltvLibrary.SelectedNode = parent.Nodes[selectedId];
                else cltvLibrary.SelectedNode = parent;
            }
        }
        private void btnRename_Click(object sender, EventArgs e)
        {
            try
            {
                if (cltvLibrary.SelectedNode != null && cltvLibrary.SelectedNode.Text != tbCategoryName.Text && cltvLibrary.SelectedNode.Parent != null)
                {
                    if (!cltvLibrary.SelectedNode.Parent.Nodes.ContainsKey(tbCategoryName.Text))
                    {
                        Material test = new Material(tbCategoryName.Text); // test the name
                        cltvLibrary.SelectedNode.Text = tbCategoryName.Text;
                        cltvLibrary.SelectedNode.Name = tbCategoryName.Text;
                        if (cltvLibrary.SelectedNode.Tag != null) ((Material)cltvLibrary.SelectedNode.Tag).Name = tbCategoryName.Text;
                        //
                        LibraryChanged();
                    }
                    else throw new CaeException("The node '" + cltvLibrary.SelectedNode.Parent.Text + 
                                                "' already contains the node named '" + tbCategoryName.Text + "'.");
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void btnDeleteFromModel_Click(object sender, EventArgs e)
        {
            if (lvModelMaterials.PossiblySelectedItems.Count == 1)
            {
                int selectedIndex = lvModelMaterials.PossiblySelectedItems[0].Index;
                lvModelMaterials.Items.Remove(lvModelMaterials.PossiblySelectedItems[0]);
                _modelChanged = true;
                //
                if (lvModelMaterials.Items.Count > 0)
                {
                    // Select the same index
                    if (selectedIndex < lvModelMaterials.Items.Count) lvModelMaterials.Items[selectedIndex].Selected = true;
                    // Select the last item
                    else lvModelMaterials.Items[selectedIndex - 1].Selected = true;
                }
                //
                lvModelMaterials.Focus();
            }
        }
        //
        private void btnCopyToLibrary_Click(object sender, EventArgs e)
        {
            try
            {
                if (lvModelMaterials.PossiblySelectedItems.Count == 1 && cltvLibrary.SelectedNode != null)
                {
                    TreeNode categoryNode;
                    if (cltvLibrary.SelectedNode.Tag == null) categoryNode = cltvLibrary.SelectedNode;  // Category
                    else categoryNode = cltvLibrary.SelectedNode.Parent;                                // Material
                    //
                    if (categoryNode == null)
                        throw new CaeException("Please select a library category to which the material should be added.");
                    //
                    string materialName = lvModelMaterials.PossiblySelectedItems[0].Text;
                    int count = 1;
                    while (categoryNode.Nodes.ContainsKey(materialName))
                    {
                        materialName = lvModelMaterials.PossiblySelectedItems[0].Text + "_Model-" + count;
                        count++;
                    }
                    //
                    ListViewItem libraryMaterialItem = lvModelMaterials.PossiblySelectedItems[0];
                    Material libraryMaterial = (Material)libraryMaterialItem.Tag.DeepClone();
                    libraryMaterial.Name = materialName;
                    // Check for equations
                    if (libraryMaterial.ContainsEquation())
                        throw new CaeException("A material containing equations cannot be added to the library.");
                    //
                    TreeNode newMaterialNode = categoryNode.Nodes.Add(libraryMaterial.Name);
                    newMaterialNode.Name = newMaterialNode.Text;
                    newMaterialNode.Tag = libraryMaterial;
                    //
                    categoryNode.Expand();
                    cltvLibrary.SelectedNode = newMaterialNode;
                    _previousControl = cltvLibrary;
                    //
                    LibraryChanged();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                _controller.Model.UnitSystem.SetConverterUnits();
            }
        }
        private void btnCopyToModel_Click(object sender, EventArgs e)
        {
            try
            {
                if (cltvLibrary.SelectedNode != null && cltvLibrary.SelectedNode.Tag != null)
                {
                    string materialName = cltvLibrary.SelectedNode.Text;
                    int count = 1;
                    while (lvModelMaterials.Items.ContainsKey(materialName))
                    {
                        materialName = cltvLibrary.SelectedNode.Text + "_Library-" + count;
                        count++;
                    }
                    ListViewItem modelMaterialItem = lvModelMaterials.Items.Add(materialName);
                    modelMaterialItem.Name = modelMaterialItem.Text;
                    //
                    Material modelMaterial = (Material)cltvLibrary.SelectedNode.Tag.DeepClone();
                    modelMaterial.Name = modelMaterialItem.Name;
                    modelMaterialItem.Tag = modelMaterial;
                    // Deselect
                    modelMaterialItem.Selected = true;
                    //
                    lvModelMaterials.Focus();
                    _previousControl = lvModelMaterials;
                    //
                    _modelChanged = true;
                    
                }
                else throw new CaeException("Please select the material in the library materials " +
                                            "to be copied to the model materials.");
            }
            catch (Exception ex)
            {                
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                _controller.Model.UnitSystem.SetConverterUnits();
            }
        }
        //
        private void cbPreview_CheckedChanged(object sender, EventArgs e)
        {
            if (cbPreview.Checked)
            {
                _frmMaterial.Location = new Point(Location.X + Width - 12, Location.Y);
                _frmMaterial.Show(this);
            }
            else _frmMaterial.Hide();
        }
        //
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (lvLibraries.PossiblySelectedItems.Count == 1)
                {
                    SaveMaterialLibrary(lvLibraries.PossiblySelectedItems[0]);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model != null && _modelChanged)
                {
                    string[] materialsToDelete = _controller.Model.Materials.Keys.ToArray();
                    if (materialsToDelete.Length > 0) _controller.RemoveMaterialsCommand(materialsToDelete);
                    //
                    foreach (ListViewItem item in lvModelMaterials.Items) _controller.AddMaterialCommand((Material)item.Tag);
                }
                Close();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void FrmMaterialLibrary_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (e.CloseReason == CloseReason.UserClosing && AnyLibraryChanged())
                {
                    DialogResult response = MessageBoxes.ShowWarningQuestionYesNoCancel(
                        "Save all material libraries before closing?");
                    if (response == DialogResult.Yes) SaveAllMaterialLibraries();
                    else if (response == DialogResult.Cancel) e.Cancel = true;
                }
            }
            catch
            { }
            finally { _controller.SetSelectByToDefault(); }
        }


        // Methods                                                                                                                  
        private void SaveAllMaterialLibraries()
        {
            foreach (ListViewItem item in lvLibraries.Items) SaveMaterialLibrary(item);
        }
        private void SaveMaterialLibrary(ListViewItem item)
        {
            if (item.Text.EndsWith("*"))
            {
                MaterialLibraryItem materialLibrary = (MaterialLibraryItem)item.Tag;
                SaveMaterialLibraryToFile(item.Name, materialLibrary);
                item.Text = item.Name;
            }
        }
        private void SaveMaterialLibraryToFile(string fileName, MaterialLibraryItem materialLibrary)
        {
            materialLibrary.DumpToFile(fileName);
        }
        private void TreeNodesToItemList(TreeNode node, MaterialLibraryItem materialLibraryItem)
        {
            materialLibraryItem.Expanded = node.IsExpanded;
            //
            foreach (TreeNode childNode in node.Nodes)
            {
                MaterialLibraryItem childItem = new MaterialLibraryItem(childNode.Name);
                if (childNode.Tag != null) childItem.Tag = (Material)childNode.Tag.DeepClone();
                materialLibraryItem.Items.Add(childItem);
                //
                TreeNodesToItemList(childNode, childItem);
            }
        }
        //
        private void LoadMaterialLibraryFromFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                foreach (ListViewItem item in lvLibraries.Items)
                {
                    if (item.Name == fileName) throw new CaeException("The selected material library is already open.");
                }
                MaterialLibraryItem materialLibrary = Tools.LoadDumpFromFile<MaterialLibraryItem>(fileName);
                //
                lvLibraries.SelectedIndices.Clear();
                ListViewItem libraryItem = lvLibraries.Items.Add(fileName);
                libraryItem.Name = libraryItem.Text;
                libraryItem.Tag = materialLibrary;
                libraryItem.Selected = true;
            }
        }
        private void FillTree(MaterialLibraryItem materialLibraryItem, TreeNode node)
        {
            node.TreeView.BeginUpdate();
            //
            ItemListToTreeNodes(materialLibraryItem, node);
            ApplyFormattingRecursive(cltvLibrary.Nodes[0]);
            //
            node.TreeView.EndUpdate();
        }
        private void ItemListToTreeNodes(MaterialLibraryItem materialLibraryItem, TreeNode node)
        {
            TreeNode childNode;
            foreach (MaterialLibraryItem childItem in materialLibraryItem.Items)
            {
                childNode = node.Nodes.Add(childItem.Name);
                childNode.Name = childItem.Name;
                //
                if (childItem.Tag != null) childNode.Tag = childItem.Tag.DeepClone();
                else ItemListToTreeNodes(childItem, childNode);
            }
            //
            if (materialLibraryItem.Expanded) node.Expand();
        }
        //
        private void ApplyFormattingRecursive(TreeNode node)
        {
            if (node.Tag == null) node.ForeColor = SystemColors.Highlight;
            else node.ForeColor = Color.Black;
            //
            foreach (TreeNode childNode in node.Nodes)
            {
                ApplyFormattingRecursive(childNode);
            }
        }
        //
        private void GetNodeContainingFirstMaterial(TreeNode node, out TreeNode firstNodeWithMaterial)
        {
            firstNodeWithMaterial = null;
            //
            if (node.Tag != null)
            {
                firstNodeWithMaterial = node;
                return;
            }
            //
            foreach (TreeNode childNode in node.Nodes)
            {
                GetNodeContainingFirstMaterial(childNode, out firstNodeWithMaterial);
                if (firstNodeWithMaterial != null) return;
            }
        }
        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            try
            {
                if (_previousControl == cltvLibrary)
                {
                    if (cltvLibrary.SelectedNode != null && cltvLibrary.SelectedNode.Tag != null)
                    {
                        TreeNode parent = cltvLibrary.SelectedNode.Parent;
                        int currentIndex = cltvLibrary.SelectedNode.Index;
                        TreeNode node = cltvLibrary.SelectedNode;
                        if (currentIndex > 0)
                        {
                            parent.Nodes.RemoveAt(currentIndex);
                            parent.Nodes.Insert(currentIndex - 1, node);
                        }
                        LibraryChanged();
                        cltvLibrary.Focus();
                    }
                }
                else if (_previousControl == lvModelMaterials)
                {
                    if (lvModelMaterials.PossiblySelectedItems != null && lvModelMaterials.PossiblySelectedItems[0].Tag != null)
                    {
                        int currentIndex = lvModelMaterials.PossiblySelectedItems[0].Index;
                        ListViewItem item = lvModelMaterials.Items[currentIndex];
                        if (currentIndex > 0)
                        {
                            lvModelMaterials.Items.RemoveAt(currentIndex);
                            lvModelMaterials.Items.Insert(currentIndex - 1, item);
                        }
                    }
                    _modelChanged = true;
                    lvModelMaterials.Focus();
                }
            }
            catch
            { }
        }
        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            try
            {
                if (_previousControl == cltvLibrary)
                {
                    if (cltvLibrary.SelectedNode != null && cltvLibrary.SelectedNode.Tag != null)
                    {
                        TreeNode parent = cltvLibrary.SelectedNode.Parent;
                        int currentIndex = cltvLibrary.SelectedNode.Index;
                        TreeNode node = cltvLibrary.SelectedNode;
                        if (currentIndex < parent.Nodes.Count - 1)
                        {
                            parent.Nodes.RemoveAt(currentIndex);
                            parent.Nodes.Insert(currentIndex + 1, node);
                        }
                        LibraryChanged();
                        cltvLibrary.Focus();
                    }
                }
                else if (_previousControl == lvModelMaterials)
                {
                    if (lvModelMaterials.PossiblySelectedItems != null && lvModelMaterials.PossiblySelectedItems[0].Tag != null)
                    {
                        int currentIndex = lvModelMaterials.PossiblySelectedItems[0].Index;
                        ListViewItem item = lvModelMaterials.Items[currentIndex];
                        if (currentIndex < lvModelMaterials.Items.Count - 1)
                        {
                            lvModelMaterials.Items.RemoveAt(currentIndex);
                            lvModelMaterials.Items.Insert(currentIndex + 1, item);
                        }
                    }
                    _modelChanged = true;
                    lvModelMaterials.Focus();
                }
            }
            catch
            { }
        }
    }
}
