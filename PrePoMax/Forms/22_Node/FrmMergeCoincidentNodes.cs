﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeMesh;
using CaeGlobals;

namespace PrePoMax.Forms
{
    class FrmMergeCoincidentNodes : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private ViewMergeCoincidentNodes _viewMergeCoincidentNodes;
        private Button btnPreview;
        private Controller _controller;


        // Constructors                                                                                                             
        public FrmMergeCoincidentNodes(Controller controller) 
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewMergeCoincidentNodes = null;
            //
            SelectionClear = _controller.Selection.Clear;
        }
        private void InitializeComponent()
        {
            this.btnPreview = new System.Windows.Forms.Button();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnPreview
            // 
            this.btnPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPreview.Image = global::PrePoMax.Properties.Resources.Show;
            this.btnPreview.Location = new System.Drawing.Point(45, 376);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(28, 23);
            this.btnPreview.TabIndex = 18;
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // FrmBoundaryLayer
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Controls.Add(this.btnPreview);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "FrmMergeCoincidentNodes";
            this.Text = "Merge Coincident Nodes";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.Controls.SetChildIndex(this.btnPreview, 0);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers                                                                                                           
        private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                _viewMergeCoincidentNodes = (ViewMergeCoincidentNodes)propertyGrid.SelectedObject;
                //
                if (_viewMergeCoincidentNodes.GeometryIds != null && _viewMergeCoincidentNodes.GeometryIds.Length > 0)
                {
                    Dictionary<int, int> oldIdNewId = _controller.GetCoincidentNodeMap(_viewMergeCoincidentNodes.GetBase());
                    if (oldIdNewId.Count == 0)
                        throw new CaeException("There are no coincident nodes in the current selection.");
                    //
                    HighlightMergeCoincidentNodes();
                    _controller.PreviewMergeCoincidentNodes(_viewMergeCoincidentNodes.GetBase());
                }
                else
                {
                    // Select all
                    MergeCoincidentNodes vmcn = _viewMergeCoincidentNodes.GetBase().DeepClone();
                    vmcn.CreationData = new Selection();
                    vmcn.CreationData.SelectItem = vtkSelectItem.Node;
                    vmcn.CreationData.Add(new SelectionNodeIds(vtkSelectOperation.Add, true));
                    //
                    HighlightMergeCoincidentNodes();
                    _controller.PreviewMergeCoincidentNodes(vmcn);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        // Overrides                                                                                                                
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            //
            base.OnVisibleChanged(e);
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewMergeCoincidentNodes = (ViewMergeCoincidentNodes)propertyGrid.SelectedObject;
            MergeCoincidentNodes vmcn = _viewMergeCoincidentNodes.GetBase();
            //
            if (vmcn.GeometryIds == null || vmcn.GeometryIds.Length == 0)
            {
                // Select all
                vmcn.CreationData = new Selection();
                vmcn.CreationData.SelectItem = vtkSelectItem.Node;
                vmcn.CreationData.Add(new SelectionNodeIds(vtkSelectOperation.Add, true));
            }
            //
            Dictionary<int, int> oldIdNewId = _controller.GetCoincidentNodeMap(vmcn);
            if (oldIdNewId.Count == 0)
                throw new CaeException("There are no coincident nodes in the current selection.");
            // Create
            _controller.MergeCoincidentNodesCommand(vmcn);
            //
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string meshRefinementToEditName)
        {
            _propertyItemChanged = false;
            _viewMergeCoincidentNodes = null;
            propertyGrid.SelectedObject = null;
            //
            _viewMergeCoincidentNodes = new ViewMergeCoincidentNodes();
            _controller.Selection.Clear();
            //
            propertyGrid.SelectedObject = _viewMergeCoincidentNodes;
            propertyGrid.Select();
            //
            SetSelectItem();
            //
            ShowHideSelectionForm();
            //
            return true;
        }


        // Methods                                                                                                                         
        private void HighlightMergeCoincidentNodes()
        {
            try
            {
                if (_controller != null)
                {
                    _controller.ClearSelectionHistory();
                    SetSelectItem();
                    // Surface.CreationData is set to null when the CreatedFrom is changed
                    if (_viewMergeCoincidentNodes.CreationData != null)
                    {
                        _controller.Selection = _viewMergeCoincidentNodes.CreationData.DeepClone(); // Deep copy to not clear
                        _controller.HighlightSelection();
                    }
                }
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
        }
        private void SetSelectItem()
        {
            _controller.SetSelectItemToNode();
        }
        public void SelectionChanged(int[] ids)
        {
            if (_viewMergeCoincidentNodes != null)
            {
                _viewMergeCoincidentNodes.GeometryIds = ids;
                _viewMergeCoincidentNodes.CreationData = _controller.Selection.DeepClone();
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
            }
        }

        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightMergeCoincidentNodes();
        }

        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            // Always use geometry based selection
            return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            // Always use default selection mode
            return defaultMode;
        }

    }


}
