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
    class FrmThickenShellMesh : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private ViewThickenShellMesh _viewThickenShellMesh;
        private Button btnPreview;
        private Controller _controller;


        // Properties                                                                                                               
        public ThickenShellMesh ThickenShellMesh
        {
            get { return _viewThickenShellMesh != null ? (ThickenShellMesh)_viewThickenShellMesh.GetBase() : null; }
            set { _viewThickenShellMesh = new ViewThickenShellMesh(value.DeepClone()); }
        }


        // Constructors                                                                                                             
        public FrmThickenShellMesh(Controller controller) 
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewThickenShellMesh = null;
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
            // FrmThickenShellMesh
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Controls.Add(this.btnPreview);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "FrmThickenShellMesh";
            this.Text = "Thicken Shell Mesh";
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
                ThickenShellMesh thickenShellMesh = ThickenShellMesh;
                //
                if (thickenShellMesh.CreationIds != null && thickenShellMesh.CreationIds.Length > 0)
                {
                    HighlightSelection();
                    _controller.PreviewThickenShellMesh(thickenShellMesh.PartNames, thickenShellMesh.Thickness,
                                                        thickenShellMesh.NumberOfLayers, thickenShellMesh.Offset,
                                                        thickenShellMesh.KeepModelEdges);
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
            ThickenShellMesh thickenShellMesh = ThickenShellMesh;
            //
            if (thickenShellMesh.CreationIds == null || thickenShellMesh.CreationIds.Length == 0)
                throw new CaeException("The thicken shell selection must contain at least one item.");
            //
            string error = _controller.IsMeshSetupItemProperlyDefined(thickenShellMesh);
            if (error != null) throw new CaeException(error);
            // Create
            _controller.ThickenShellMeshCommand(thickenShellMesh.PartNames, thickenShellMesh.Thickness,
                                                thickenShellMesh.NumberOfLayers, thickenShellMesh.Offset,
                                                thickenShellMesh.KeepModelEdges);
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
            if (_controller.Model.Properties.ModelSpace != CaeModel.ModelSpaceEnum.ThreeD)
                throw new CaeException("Solid mesh creation is possible only in 3D model space.");
            //
            _propertyItemChanged = false;
            _viewThickenShellMesh = null;
            propertyGrid.SelectedObject = null;
            //
            _viewThickenShellMesh = new ViewThickenShellMesh(new ThickenShellMesh("ThickenShellMesh"));
            _controller.Selection.Clear();
            //
            propertyGrid.SelectedObject = _viewThickenShellMesh;
            propertyGrid.Select();
            //
            SetSelectItem();
            //
            ShowHideSelectionForm();
            //
            HighlightSelection();
            //
            return true;
        }


        // Methods                                                                                                                         
        private void HighlightSelection()
        {
            try
            {
                if (_controller != null)
                {
                    ThickenShellMesh thickenShellMesh = ThickenShellMesh;
                    _controller.ClearSelectionHistory();
                    // ThickenShellMesh.CreationData is set to null when the CreatedFrom is changed
                    if (thickenShellMesh.CreationData != null)
                    {
                        _controller.Selection = thickenShellMesh.CreationData.DeepClone(); // Deep copy to not clear
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
            _controller.SetSelectItemToPart();
        }
        public void SelectionChanged(int[] ids)
        {
            if (ThickenShellMesh != null)
            {
                ThickenShellMesh.CreationIds = ids;
                ThickenShellMesh.PartNames = _controller.DisplayedMesh.GetPartNamesFromGeometryIds(ids);
                ThickenShellMesh.CreationData = _controller.Selection.DeepClone();
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightSelection();
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
