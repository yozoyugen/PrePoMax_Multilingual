﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using CaeMesh;

namespace PrePoMax.Forms
{
    class FrmConstraint : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _constraintNames;
        private string _constraintToEditName;
        private ViewConstraint _viewConstraint;
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiSwapMasterSlave;
        private string _prevSelectionFormProperty;
        private Controller _controller;


        // Properties                                                                                                               
        public Constraint Constraint
        {
            get { return _viewConstraint != null ? _viewConstraint.GetBase() : null; }
            set
            {
                if (value is PointSpring ps) _viewConstraint = new ViewPointSpring(ps.DeepClone());
                else if (value is SurfaceSpring ss) _viewConstraint = new ViewSurfaceSpring(ss.DeepClone());
                else if (value is CompressionOnly co) _viewConstraint = new ViewCompressionOnly(co.DeepClone());
                else if (value is RigidBody rb) _viewConstraint = new ViewRigidBody(rb.DeepClone());
                else if (value is Tie tie) _viewConstraint = new ViewTie(tie.DeepClone());
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmConstraint(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewConstraint = null;
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cmsPropertyGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiSwapMasterSlave = new System.Windows.Forms.ToolStripMenuItem();
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.cmsPropertyGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbProperties
            // 
            this.gbProperties.Size = new System.Drawing.Size(310, 353);
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.Size = new System.Drawing.Size(298, 325);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 476);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 476);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 476);
            // 
            // cmsPropertyGrid
            // 
            this.cmsPropertyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiSwapMasterSlave});
            this.cmsPropertyGrid.Name = "cmsPropertyGrid";
            this.cmsPropertyGrid.Size = new System.Drawing.Size(174, 26);
            // 
            // tsmiSwapMasterSlave
            // 
            this.tsmiSwapMasterSlave.Name = "tsmiSwapMasterSlave";
            this.tsmiSwapMasterSlave.Size = new System.Drawing.Size(173, 22);
            this.tsmiSwapMasterSlave.Text = "Swap Master/Slave";
            this.tsmiSwapMasterSlave.Click += new System.EventHandler(this.tsmiSwapMasterSlave_Click);
            // 
            // FrmConstraint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 511);
            this.Name = "FrmConstraint";
            this.Text = "Edit Constraint";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.Controls.SetChildIndex(this.gbType, 0);
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.cmsPropertyGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            //
            base.OnVisibleChanged(e);
        }
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            // Deactivate selection limits
            _controller.Selection.EnableShellEdgeFaceSelection = false;
            //
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                if (itemTag is ViewError) _viewConstraint = null;
                else if (itemTag is ViewPointSpring vps)
                {
                    _viewConstraint = vps;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                }
                else if (itemTag is ViewSurfaceSpring vss)
                {
                    _viewConstraint = vss;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if ((vss.GetBase() as SurfaceSpring).TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewCompressionOnly vco)
                {
                    _viewConstraint = vco;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if ((vco.GetBase() as CompressionOnly).TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewRigidBody vrd)
                {
                    _viewConstraint = vrd;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                }
                else if (itemTag is ViewTie vt)
                {
                    _viewConstraint = vt;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if ((vt.GetBase() as Tie).TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else throw new NotImplementedException();
                //
                propertyGrid.SelectedObject = itemTag;
                //
                ShowHideSelectionForm();
                //
                HighlightConstraint();
                // Context menu
                if (propertyGrid.SelectedObject is ViewRigidBody) propertyGrid.ContextMenuStrip = null;
                // Swap Master/Slave
                else if (propertyGrid.SelectedObject is ViewTie) propertyGrid.ContextMenuStrip = cmsPropertyGrid;
            }
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            ShowHideContextMenu();
            //
            ShowHideSelectionForm();
            //
            HighlightConstraint();
            //
            base.OnPropertyGridSelectedGridItemChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewConstraint = (ViewConstraint)propertyGrid.SelectedObject;
            //
            if (Constraint == null) throw new CaeException("No constraint was selected.");
            // Check if the name exists
            CheckName(_constraintToEditName, Constraint.Name, _constraintNames, "constraint");
            // Check equations
            _viewConstraint.GetBase().CheckEquations();
            //
            if (Constraint is PointSpring ps)
            {
                if (ps.RegionType == RegionTypeEnum.Selection && (ps.CreationIds == null || ps.CreationIds.Length == 0))
                    throw new CaeException("The point spring region selection must contain at least one item.");
                //
                if (ps.GetSpringStiffnessValues().Length == 0)
                    throw new CaeException("At least one stiffness must be larger than 0.");
            }
            //
            if (Constraint is SurfaceSpring ss)
            {
                if (ss.RegionType == RegionTypeEnum.Selection && (ss.CreationIds == null || ss.CreationIds.Length == 0))
                    throw new CaeException("The surface spring region selection must contain at least one item.");
                //
                if (ss.GetSpringStiffnessValues().Length == 0)
                    throw new CaeException("At least one stiffness must be larger than 0.");
            }
            //
            if (Constraint is CompressionOnly co)
            {
                if (co.RegionType == RegionTypeEnum.Selection && (co.CreationIds == null || co.CreationIds.Length == 0))
                    throw new CaeException("The compression only constraint region selection must contain at least one item.");
                if (co.NonLinear == false && MessageBoxes.ShowWarningQuestionOKCancel("Using a linear solution procedure will " +
                    "linearize the compression only support. Continue?") == DialogResult.Cancel) 
                        throw new CaeException("BreakOnApply");
            }
            //
            if (Constraint is RigidBody rb && rb.RegionType == RegionTypeEnum.Selection &&
                (rb.CreationIds == null || rb.CreationIds.Length == 0))
                throw new CaeException("The rigid body region selection must contain at least one item.");
            //
            if (Constraint is Tie tie)
            {
                if (tie.MasterRegionType == RegionTypeEnum.Selection &&
                    (tie.MasterCreationIds == null || tie.MasterCreationIds.Length == 0))
                    throw new CaeException("The tie master region selection must contain at least one item.");
                //
                if (tie.SlaveRegionType == RegionTypeEnum.Selection &&
                    (tie.SlaveCreationIds == null || tie.SlaveCreationIds.Length == 0))
                    throw new CaeException("The tie slave region selection must contain at least one item.");
                // Check for errors with constructor
                bool twoD = _controller.Model.Properties.ModelSpace.IsTwoD();
                var tmp = new Tie(tie.Name, tie.MasterRegionName, tie.MasterRegionType,
                                  tie.SlaveRegionName, tie.SlaveRegionType, twoD);
            }
            // Create
            if (_constraintToEditName == null)
            {
                _controller.AddConstraintCommand(Constraint);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceConstraintCommand(_constraintToEditName, Constraint);
                _constraintToEditName = null; // prevents the execution of toInternal in OnHideOrClose
            }
            // Convert the constraint from internal to show it
            else
            {
                ConstraintInternal(false);
            }
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            // Deactivate selection limits
            _controller.Selection.EnableShellEdgeFaceSelection = false;
            // Convert the constraint from internal to show it
            ConstraintInternal(false);
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string constraintToEditName)
        {
            this.btnOkAddNew.Visible = constraintToEditName == null;
            //
            _propertyItemChanged = false;
            _constraintNames = null;
            _constraintToEditName = null;
            _viewConstraint = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            //
            _constraintNames = _controller.GetConstraintNames();
            _constraintToEditName = constraintToEditName;
            //
            string[] referencePointNames = _controller.GetModelReferencePointNames();
            string[] nodeSetNames = _controller.GetUserNodeSetNames();
            string[] surfaceNames = _controller.GetUserSurfaceNames();
            if (referencePointNames == null) referencePointNames = new string[0];
            if (nodeSetNames == null) nodeSetNames = new string[0];
            if (surfaceNames == null) surfaceNames = new string[0];
            //
            if (_constraintNames == null)
                throw new CaeException("The constraint names must be defined first.");
            // Populate list view
            PopulateListOfConstraints(referencePointNames, nodeSetNames, surfaceNames);
            // Create new constraint
            if (_constraintToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewConstraint = null;
                //
                HighlightConstraint(); // must be here if called from the menu
            }
            // Edit existing constraint
            else
            {
                // Get and convert a converted constraint back to selection
                Constraint = _controller.GetConstraint(_constraintToEditName); // to clone                
                SetCreationDataToSelection(Constraint);
                // Convert the constraint to internal to hide it
                ConstraintInternal(true);
                //
                int selectedId;
                if (_viewConstraint is ViewPointSpring vps)
                {
                    selectedId = lvTypes.FindItemWithText("Point Spring").Index;
                    // Master
                    if (vps.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vps.MasterRegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vps.NodeSetName, s => { vps.NodeSetName = s; });
                    else if (vps.MasterRegionType == RegionTypeEnum.ReferencePointName.ToFriendlyString())
                        CheckMissingValueRef(ref referencePointNames, vps.ReferencePointName, s => { vps.ReferencePointName = s; });
                    else throw new NotSupportedException();
                    //
                    vps.PopulateDropDownLists(nodeSetNames, referencePointNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = null;
                }
                else if (_viewConstraint is ViewSurfaceSpring vss)
                {
                    selectedId = lvTypes.FindItemWithText("Surface Spring").Index;
                    // Master
                    if (vss.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vss.MasterRegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vss.SurfaceName, s => { vss.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vss.PopulateDropDownLists(surfaceNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = null;
                }
                else if (_viewConstraint is ViewCompressionOnly vco)
                {
                    selectedId = lvTypes.FindItemWithText("Compression Only").Index;
                    // Master
                    if (vco.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vco.MasterRegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vco.SurfaceName, s => { vco.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vco.PopulateDropDownLists(surfaceNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = null;
                }
                else if (_viewConstraint is ViewRigidBody vrb)
                {
                    selectedId = lvTypes.FindItemWithText("Rigid Body").Index;
                    // Reference point
                    CheckMissingValueRef(ref referencePointNames, vrb.ReferencePointName, s => { vrb.ReferencePointName = s; });
                    // Slave
                    if (vrb.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vrb.SlaveRegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vrb.NodeSetName, s => { vrb.NodeSetName = s; });
                    else if (vrb.SlaveRegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vrb.SurfaceName, s => { vrb.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vrb.PopulateDropDownLists(referencePointNames, nodeSetNames, surfaceNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = null;
                }
                else if (_viewConstraint is ViewTie vt)
                {
                    selectedId = lvTypes.FindItemWithText("Tie").Index;
                    // Master
                    if (vt.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else CheckMissingValueRef(ref surfaceNames, vt.MasterSurfaceName, s => { vt.MasterSurfaceName = s; });
                    // Slave
                    if (vt.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else CheckMissingValueRef(ref surfaceNames, vt.SlaveSurfaceName, s => { vt.SlaveSurfaceName = s; });
                    //
                    vt.PopulateDropDownLists(surfaceNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = cmsPropertyGrid;
                }
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewConstraint;
                _preselectIndex = selectedId;
            }
            ShowHideSelectionForm();
            //
            return true;
        }
        private void SetCreationDataToSelection(Constraint constraint)
        {
            if (constraint is PointSpring ps && ps.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(ps) || // do not use BoundaryCondition.Valid
                    !_controller.Model.RegionValid(ps))
                {
                    // Region invalid
                    ps.CreationData = null;
                    ps.CreationIds = null;
                    _propertyItemChanged = true;
                }
                ps.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is SurfaceSpring ss && ss.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(ss) || // do not use BoundaryCondition.Valid
                   !_controller.Model.RegionValid(ss))
                {
                    // Region invalid
                    ss.CreationData = null;
                    ss.CreationIds = null;
                    _propertyItemChanged = true;
                }
                ss.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is CompressionOnly co && co.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(co) || // do not use BoundaryCondition.Valid
                   !_controller.Model.RegionValid(co))
                {
                    // Region invalid
                    co.CreationData = null;
                    co.CreationIds = null;
                    _propertyItemChanged = true;
                }
                co.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is RigidBody rb && rb.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(rb) || // do not use BoundaryCondition.Valid
                   !_controller.Model.RegionValid(rb))
                {
                    // Region invalid
                    rb.CreationData = null;
                    rb.CreationIds = null;
                    _propertyItemChanged = true;
                }
                rb.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is Tie tie)
            {
                if (tie.MasterCreationData != null)
                {
                    if (!_controller.Model.Mesh.Surfaces.ContainsValidKey(tie.MasterRegionName))
                    {
                        // Region invalid
                        tie.MasterCreationData = null;
                        tie.MasterCreationIds = null;
                        _propertyItemChanged = true;
                    }
                    tie.MasterRegionType = RegionTypeEnum.Selection;
                }
                if (tie.SlaveCreationData != null)
                {
                    if (!_controller.Model.Mesh.Surfaces.ContainsValidKey(tie.SlaveRegionName))
                    {
                        // Region invalid
                        tie.SlaveCreationData = null;
                        tie.SlaveCreationIds = null;
                        _propertyItemChanged = true;
                    }
                    tie.SlaveRegionType = RegionTypeEnum.Selection;
                }
            }
        }

        // Methods                                                                                                                  
        private void PopulateListOfConstraints(string[] referencePointNames, string[] nodeSetNames, string[] surfaceNames)
        {
            Color color = _controller.Settings.Pre.ConstraintSymbolColor;
            bool twoD = _controller.Model.Properties.ModelSpace.IsTwoD();
            // Populate list view
            ListViewItem item;
            // Point spring
            item = new ListViewItem("Point Spring");
            PointSpring pointSpring = new PointSpring(GetConstraintName("Point_Spring"), "", RegionTypeEnum.Selection, twoD, true);
            ViewPointSpring vps = new ViewPointSpring(pointSpring);
            vps.PopulateDropDownLists(nodeSetNames, referencePointNames);
            item.Tag = vps;
            vps.Color = color;
            lvTypes.Items.Add(item);
            // Surface spring
            item = new ListViewItem("Surface Spring");
            SurfaceSpring surfaceSpring = new SurfaceSpring(GetConstraintName("Surface_Spring"),
                                                            "", RegionTypeEnum.Selection, twoD, true);
            ViewSurfaceSpring vss = new ViewSurfaceSpring(surfaceSpring);
            vss.PopulateDropDownLists(surfaceNames);
            item.Tag = vss;
            vss.Color = color;
            lvTypes.Items.Add(item);
            // Compression only support
            item = new ListViewItem("Compression Only");
            CompressionOnly compressionOnly = new CompressionOnly(GetConstraintName("Compression_Only"),
                                                                  "", RegionTypeEnum.Selection, twoD);
            ViewCompressionOnly vco = new ViewCompressionOnly(compressionOnly);
            vco.PopulateDropDownLists(surfaceNames);
            item.Tag = vco;
            vco.Color = color;
            lvTypes.Items.Add(item);
            // Rigid body
            item = new ListViewItem("Rigid Body");
            if (referencePointNames.Length > 0)
            {
                RigidBody rb = new RigidBody(GetConstraintName("Rigid_Body"), referencePointNames[0],
                                             "", RegionTypeEnum.Selection, twoD);
                ViewRigidBody vrb = new ViewRigidBody(rb);
                vrb.PopulateDropDownLists(referencePointNames, nodeSetNames, surfaceNames);
                vrb.Color = color;
                item.Tag = vrb;
            }
            else item.Tag = new ViewError("There is no reference point defined for the rigid body constraint definition.");
            lvTypes.Items.Add(item);
            // Tie
            item = new ListViewItem("Tie");
            Tie tie = new Tie(GetConstraintName("Tie"), "", RegionTypeEnum.Selection, "", RegionTypeEnum.Selection, twoD);
            ViewTie vt = new ViewTie(tie);
            vt.PopulateDropDownLists(surfaceNames);
            item.Tag = vt;
            vt.MasterColor = color;
            vt.SlaveColor = color;
            lvTypes.Items.Add(item);
        }
        private string GetConstraintName(string namePrefix)
        {
            return _constraintNames.GetNextNumberedKey(namePrefix);
        }
        //
        private void ShowHideContextMenu()
        {
            propertyGrid.ContextMenuStrip = null;
            //
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (Constraint != null)
            {
                if (Constraint is Tie)
                {
                    if (property == nameof(ViewTie.MasterRegionType) || property == nameof(ViewTie.SlaveRegionType))
                        propertyGrid.ContextMenuStrip = cmsPropertyGrid;
                }
            }
        }
        private void tsmiSwapMasterSlave_Click(object sender, EventArgs e)
        {
            if (propertyGrid.SelectedObject is ViewTie vt)
            {
                if (Constraint is Tie tie)
                {
                    tie.SwapMasterSlave();
                    //
                    vt.UpdateRegionVisibility();
                    propertyGrid.Refresh();
                    //
                    _prevSelectionFormProperty = null;   // calls ItemSetDataEditor.SelectionForm.ResetSelection
                    ShowHideSelectionForm();
                    //
                    OnPropertyGridSelectedGridItemChanged();    // highlight
                    _propertyItemChanged = true;
                }
            }
        }
        //
        private void HighlightConstraint()
        {
            try
            {
                if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
                //
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                _controller.ClearSelectionHistory();
                //
                if (_viewConstraint == null) { }
                else if (Constraint is PointSpring ps)
                {
                    // Master
                    HighlightRegion(ps.MasterRegionType, ps.MasterRegionName, ps.CreationData, true, false);
                }
                else if (Constraint is SurfaceSpring ss)
                {
                    // Master
                    HighlightRegion(ss.MasterRegionType, ss.MasterRegionName, ss.CreationData, true, false);
                }
                else if (Constraint is CompressionOnly co)
                {
                    // Master
                    HighlightRegion(co.MasterRegionType, co.MasterRegionName, co.CreationData, true, false);
                }
                else if (Constraint is RigidBody rb)
                {
                    // Master
                    _controller.HighlightReferencePoint(rb.ReferencePointName);
                    // Slave
                    HighlightRegion(rb.SlaveRegionType, rb.SlaveRegionName, rb.CreationData, false, true);
                }
                else if (Constraint is Tie tie)
                {
                    if (property == nameof(ViewTie.MasterRegionType))
                    {
                        // Master
                        HighlightRegion(tie.MasterRegionType, tie.MasterRegionName, tie.MasterCreationData, true, false);
                    }
                    else if(property == nameof(ViewTie.SlaveRegionType))
                    {
                        // Slave
                        HighlightRegion(tie.SlaveRegionType, tie.SlaveRegionName, tie.SlaveCreationData, true, true);
                    }
                    else
                    {
                        // Master
                        HighlightRegion(tie.MasterRegionType, tie.MasterRegionName, tie.MasterCreationData, true, false);
                        // Slave
                        HighlightRegion(tie.SlaveRegionType, tie.SlaveRegionName, tie.SlaveCreationData, false, true);
                    }
                }
                else throw new NotSupportedException();
            }
            catch { }
        }
        private void HighlightRegion(RegionTypeEnum regionType, string regionName, Selection creationData,
                                     bool clear, bool useSecondaryHighlightColor)
        {
            if (regionType == RegionTypeEnum.NodeSetName)
                _controller.HighlightNodeSet(regionName, useSecondaryHighlightColor);
            else if (regionType == RegionTypeEnum.SurfaceName)
                _controller.HighlightSurface(regionName, useSecondaryHighlightColor);
            else if (regionType == RegionTypeEnum.ReferencePointName)
                _controller.HighlightReferencePoint(regionName);
            else if (regionType == RegionTypeEnum.Selection)
            {
                SetSelectItem();
                //
                if (creationData != null)
                {
                    _controller.Selection = creationData.DeepClone();
                    _controller.HighlightSelection(clear, true, useSecondaryHighlightColor);
                }
            }
        }
        private void ShowHideSelectionForm()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property != _prevSelectionFormProperty) ItemSetDataEditor.SelectionForm.ResetSelection(false);
            _prevSelectionFormProperty = property;
            //
            if (Constraint != null)
            {
                ItemSetDataEditor.SelectionForm.SetOnlyGeometrySelection(false, false);
                //
                if (Constraint is PointSpring ps && ps.RegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (Constraint is SurfaceSpring ss && ss.RegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (Constraint is CompressionOnly co && co.RegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (Constraint is RigidBody rb && rb.RegionType == RegionTypeEnum.Selection)
                {
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    ItemSetDataEditor.SelectionForm.SetOnlyGeometrySelection(true, false);
                }
                else if (Constraint is Tie tie)
                {
                    if (property == nameof(ViewTie.MasterRegionType) && tie.MasterRegionType == RegionTypeEnum.Selection)
                        ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    else if (property == nameof(ViewTie.SlaveRegionType) && tie.SlaveRegionType == RegionTypeEnum.Selection)
                        ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    else ItemSetDataEditor.SelectionForm.Hide();
                }
                else ItemSetDataEditor.SelectionForm.Hide();
            }
            else ItemSetDataEditor.SelectionForm.Hide();
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (Constraint == null) { }
            else if (Constraint is PointSpring ps)
            {
                if (ps.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToNode();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is SurfaceSpring ss)
            {
                if (ss.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToSurface();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is CompressionOnly co)
            {
                if (co.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToSurface();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is RigidBody rb)
            {
                if (rb.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToGeometry();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is Tie tie)
            {
                if ((tie.MasterRegionType == RegionTypeEnum.Selection && property == nameof(ViewTie.MasterRegionType)) ||
                    (tie.SlaveRegionType == RegionTypeEnum.Selection  && property == nameof(ViewTie.SlaveRegionType)))
                    _controller.SetSelectItemToSurface();
                else
                    _controller.SetSelectByToOff();
            }
            else throw new NotSupportedException();
        }
        private void ConstraintInternal(bool toInternal)
        {
            if (_constraintToEditName != null)
            {
                // Convert the constraint from/to internal to hide/show it
                _controller.GetConstraint(_constraintToEditName).Internal = toInternal;
                _controller.FeModelUpdate(UpdateType.RedrawSymbols);
            }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            bool changed = false;
            if (Constraint != null)
            {
                if (Constraint is PointSpring ps)
                {
                    if (ps.RegionType == RegionTypeEnum.Selection)
                    {
                        ps.CreationIds = ids;
                        ps.CreationData = _controller.Selection.DeepClone();
                        changed = true;
                    }
                }
                else if (Constraint is SurfaceSpring ss)
                {
                    if (ss.RegionType == RegionTypeEnum.Selection)
                    {
                        ss.CreationIds = ids;
                        ss.CreationData = _controller.Selection.DeepClone();
                        changed = true;
                    }
                }
                else if (Constraint is CompressionOnly co)
                {
                    if (co.RegionType == RegionTypeEnum.Selection)
                    {
                        co.CreationIds = ids;
                        co.CreationData = _controller.Selection.DeepClone();
                        changed = true;
                    }
                }
                else if (Constraint is RigidBody rb)
                {
                    if (rb.RegionType == RegionTypeEnum.Selection)
                    {
                        rb.CreationIds = ids;
                        rb.CreationData = _controller.Selection.DeepClone();
                        changed = true;
                    }
                }
                else if (Constraint is Tie tie)
                {
                    if (property == nameof(ViewTie.MasterRegionType) && tie.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        tie.MasterCreationIds = ids;
                        tie.MasterCreationData = _controller.Selection.DeepClone();
                        changed = true;
                    }
                    else if (property == nameof(ViewTie.SlaveRegionType) && tie.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        tie.SlaveCreationIds = ids;
                        tie.SlaveCreationData = _controller.Selection.DeepClone();
                        changed = true;
                    }
                }
                else throw new NotSupportedException();
                //
                if (changed)
                {
                    propertyGrid.Refresh();
                    //
                    _propertyItemChanged = true;
                }
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightConstraint();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return true;
            Constraint constraint = Constraint;
            //
            if (constraint != null)
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (constraint is PointSpring ps)
                {
                    if (ps.CreationData != null) return ps.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is SurfaceSpring ss)
                {
                    if (ss.CreationData != null) return ss.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is CompressionOnly co)
                {
                    if (co.CreationData != null) return co.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is RigidBody rb)
                {
                    if (rb.CreationData != null) return rb.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is Tie tie)
                {
                    if (property == nameof(ViewTie.MasterRegionType) && tie.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        if (tie.MasterCreationData != null) return tie.MasterCreationData.IsGeometryBased();
                        else return true;
                    }
                    else if (property == nameof(ViewTie.SlaveRegionType) && tie.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        if (tie.SlaveCreationData != null) return tie.SlaveCreationData.IsGeometryBased();
                        else return true;
                    }
                }
                else throw new NotSupportedException();
            }
            return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return defaultMode;
            Constraint constraint = Constraint;
            //
            if (constraint != null && IsSelectionGeometryBased())
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (constraint is PointSpring ps)
                {
                    if (ps.CreationData != null) return ps.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is SurfaceSpring ss)
                {
                    if (ss.CreationData != null) return ss.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is CompressionOnly co)
                {
                    if (co.CreationData != null) return co.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is RigidBody rb)
                {
                    if (rb.CreationData != null) return rb.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is Tie tie)
                {
                    if (property == nameof(ViewTie.MasterRegionType) && tie.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        if (tie.MasterCreationData != null) return tie.MasterCreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                    else if (property == nameof(ViewTie.SlaveRegionType) && tie.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        if (tie.SlaveCreationData != null) return tie.SlaveCreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                }
                else throw new NotSupportedException();
            }
            return defaultMode;
        }
    }
}
