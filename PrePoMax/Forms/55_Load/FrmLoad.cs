﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeModel;
using CaeGlobals;
using CaeMesh;
using CaeResults;

namespace PrePoMax.Forms
{
    class FrmLoad : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _loadNames;
        private string _loadToEditName;
        private ViewLoad _viewLoad;
        private Controller _controller;
        private Selection _selectionCopy;


        // Properties                                                                                                               
        // SetLoad and GetLoad to distinguish from Load event
        public Load FELoad 
        {
            get { return _viewLoad != null ? _viewLoad.GetBase() : null; }
            set
            {
                var clone = value.DeepClone();
                if (clone is CLoad cl) _viewLoad = new ViewCLoad(cl);
                else if (clone is MomentLoad ml) _viewLoad = new ViewMomentLoad(ml);
                else if (clone is DLoad dl) _viewLoad = new ViewDLoad(dl);
                else if (clone is HydrostaticPressure hpl) _viewLoad = new ViewHydrostaticPressureLoad(hpl);
                else if (clone is ImportedPressure ip) _viewLoad = new ViewImportedPressureLoad(ip);
                else if (clone is STLoad stl) _viewLoad = new ViewSTLoad(stl);
                else if (clone is ImportedSTLoad istl) _viewLoad = new ViewImportedSTLoad(istl);
                else if (clone is ShellEdgeLoad sel) _viewLoad = new ViewShellEdgeLoad(sel);
                else if (clone is GravityLoad gl) _viewLoad = new ViewGravityLoad(gl);
                else if (clone is CentrifLoad cfl) _viewLoad = new ViewCentrifLoad(cfl);
                else if (clone is PreTensionLoad ptl) _viewLoad = new ViewPreTensionLoad(ptl);
                // Thermal
                else if (clone is CFlux cf) _viewLoad = new ViewCFlux(cf);
                else if (clone is DFlux df) _viewLoad = new ViewDFlux(df);
                else if (clone is BodyFlux bf) _viewLoad = new ViewBodyFlux(bf);
                else if (clone is FilmHeatTransfer fht) _viewLoad = new ViewFilmHeatTransfer(fht);
                else if (clone is RadiationHeatTransfer rht) _viewLoad = new ViewRadiationHeatTransfer(rht);
                //
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmLoad(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewLoad = null;
            //
            this.Height = 560 + 3 * 19;
        }
        private void InitializeComponent()
        {
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbType
            // 
            this.gbType.Size = new System.Drawing.Size(330, 101);
            // 
            // lvTypes
            // 
            this.lvTypes.Size = new System.Drawing.Size(318, 73);
            // 
            // gbProperties
            // 
            this.gbProperties.Size = new System.Drawing.Size(330, 371);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Size = new System.Drawing.Size(318, 343);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(180, 496);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(261, 496);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(99, 496);
            // 
            // FrmLoad
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(354, 531);
            this.MinimumSize = new System.Drawing.Size(370, 550);
            this.Name = "FrmLoad";
            this.Text = "Edit Load";
            this.EnabledChanged += new System.EventHandler(this.FrmLoad_EnabledChanged);
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers
        private void FrmLoad_EnabledChanged(object sender, EventArgs e)
        {
            if (!Enabled)
            {
                _selectionCopy = _controller.Selection.DeepClone();
            }
            //
            ShowHideSelectionForm();
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
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                _controller.Selection.LimitSelectionToFirstGeometryType = false;
                _controller.Selection.EnableShellEdgeFaceSelection = false;
                _controller.Selection.LimitSelectionToShellEdges = false;
                //
                if (itemTag is ViewError) _viewLoad = null;
                else if (itemTag is ViewCLoad vcl) _viewLoad = vcl;
                else if (itemTag is ViewMomentLoad vml) _viewLoad = vml;
                else if (itemTag is ViewDLoad vdl)
                {
                    _viewLoad = vdl;
                    // Set a filter in order for S1, S2,... to include the same element types
                    _controller.Selection.LimitSelectionToFirstGeometryType = true;
                    // 2D
                    if (vdl.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewHydrostaticPressureLoad vhpl)
                {
                    _viewLoad = vhpl;
                    // Set a filter in order for S1, S2,... to include the same element types
                    _controller.Selection.LimitSelectionToFirstGeometryType = true;
                    // 2D
                    if (vhpl.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewImportedPressureLoad vipl)
                {
                    vipl.UpdateFileBrowserDialog();
                    _viewLoad = vipl;
                    // Set a filter in order for S1, S2,... to include the same element types
                    _controller.Selection.LimitSelectionToFirstGeometryType = true;
                    // 2D
                    if (vipl.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewSTLoad vstl)
                {
                    _viewLoad = vstl;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if (vstl.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewImportedSTLoad vistl)
                {
                    vistl.UpdateFileBrowserDialog();
                    _viewLoad = vistl;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if (vistl.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewShellEdgeLoad vsel)
                {
                    _viewLoad = vsel;
                    _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewGravityLoad vgl) _viewLoad = vgl;
                else if (itemTag is ViewCentrifLoad vcfl) _viewLoad = vcfl;
                else if (itemTag is ViewPreTensionLoad vprl) _viewLoad = vprl;
                // Thermal
                else if (itemTag is ViewCFlux vcf) _viewLoad = vcf;
                else if (itemTag is ViewDFlux vdf)
                {
                    _viewLoad = vdf;
                    // Set a filter in order for S1, S2,... to include the same element types
                    _controller.Selection.LimitSelectionToFirstGeometryType = true;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if (vdf.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewBodyFlux vbf) _viewLoad = vbf;
                else if (itemTag is ViewFilmHeatTransfer vfht)
                {
                    _viewLoad = vfht;
                    // Set a filter in order for S1, S2,... to include the same element types
                    _controller.Selection.LimitSelectionToFirstGeometryType = true;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if (vfht.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewRadiationHeatTransfer vrht)
                {
                    _viewLoad = vrht;
                    // Set a filter in order for S1, S2,... to include the same element types
                    _controller.Selection.LimitSelectionToFirstGeometryType = true;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if (vrht.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else throw new NotImplementedException();
                //
                ShowHideSelectionForm();
                //
                propertyGrid.SelectedObject = itemTag;
                //
                HighlightLoad();
            }
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            if (propertyGrid.SelectedGridItem.PropertyDescriptor != null)
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (_viewLoad is ViewImportedSTLoad vistl)
                {
                    HighlightLoad();
                }
            }
            //
            base.OnPropertyGridSelectedGridItemChanged();
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewLoad.RegionType))
            {
                ShowHideSelectionForm();
                //
                HighlightLoad();
            }
            else if (property == nameof(_viewLoad.CoordinateSystemName))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewCLoad vcl &&
                     (property == nameof(vcl.NodeSetName) || property == nameof(vcl.ReferencePointName)))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewMomentLoad vml &&
                     (property == nameof(vml.NodeSetName) || property == nameof(vml.ReferencePointName)))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewDLoad vdl && property == nameof(vdl.SurfaceName))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewHydrostaticPressureLoad vhpl && 
                (property == nameof(vhpl.SurfaceName) ||
                 property == nameof(vhpl.X1) || property == nameof(vhpl.Y1) || property == nameof(vhpl.Z1) ||
                 property == nameof(vhpl.X2) || property == nameof(vhpl.Y2) || property == nameof(vhpl.Z2)))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewImportedPressureLoad vipl && property == nameof(vipl.SurfaceName))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewSTLoad vstl && property == nameof(vstl.SurfaceName))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewImportedSTLoad vistl &&
                     (property == nameof(vistl.SurfaceName) || property == nameof(vistl.FileName) ||
                      property == nameof(vistl.GeometryScaleFactor)))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewShellEdgeLoad vsel && property == nameof(vsel.SurfaceName))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewGravityLoad vgl &&
                     (property == nameof(vgl.PartName) || property == nameof(vgl.ElementSetName) ||
                      property == nameof(vgl.MassSectionName)))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewCentrifLoad vcfl &&
                     (property == nameof(vcfl.PartName) || property == nameof(vcfl.ElementSetName) ||
                      property == nameof(vcfl.MassSectionName) ||
                      property == nameof(vcfl.X) || property == nameof(vcfl.Y) || property == nameof(vcfl.Z)))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewPreTensionLoad vptl && property == nameof(vptl.SurfaceName))
            {
                HighlightLoad();
            }
            // Thermal
            else if (_viewLoad is ViewCFlux vcf && property == nameof(vcf.NodeSetName))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewDFlux vdf && property == nameof(vdf.SurfaceName))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewBodyFlux vbf &&
                     (property == nameof(vbf.PartName) || property == nameof(vbf.ElementSetName)))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewFilmHeatTransfer vfht && property == nameof(vfht.SurfaceName))
            {
                HighlightLoad();
            }
            else if (_viewLoad is ViewRadiationHeatTransfer vrht && property == nameof(vrht.SurfaceName))
            {
                HighlightLoad();
            }
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewLoad = (ViewLoad)propertyGrid.SelectedObject;
            //
            if (_viewLoad == null) throw new CaeException("No load selected.");
            // Check if the name exists
            CheckName(_loadToEditName, FELoad.Name, _loadNames, "load");
            // Check equations
            _viewLoad.GetBase().CheckEquations();
            //
            if (FELoad.RegionType == RegionTypeEnum.Selection &&
                (FELoad.CreationIds == null || FELoad.CreationIds.Length == 0))
                throw new CaeException("The load selection must contain at least one item.");
            //
            // check for 0 values
            if (FELoad is CLoad cl)
            {
                if (cl.Magnitude.Value == 0)
                    throw new CaeException("At least one force component must not be equal to 0.");
                if ((cl.RegionType == RegionTypeEnum.Selection && cl.CreationIds.Length > 5) ||
                    (cl.RegionType == RegionTypeEnum.NodeSetName &&
                     _controller.Model.Mesh.NodeSets[cl.RegionName].Labels.Length > 5))
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel(
                        "The concentrated force load will apply the entered load magnitude to all selected nodes. Continue?") ==
                        DialogResult.Cancel)
                        throw new CaeException("BreakOnApply");
                }
            }
            else if (FELoad is MomentLoad ml)
            {
                if (ml.Magnitude.Value == 0)
                    throw new CaeException("At least one moment component must not be equal to 0.");
                if ((ml.RegionType == RegionTypeEnum.Selection && ml.CreationIds.Length > 5) ||
                    (ml.RegionType == RegionTypeEnum.NodeSetName &&
                     _controller.Model.Mesh.NodeSets[ml.RegionName].Labels.Length > 5))
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel(
                        "The moment load will apply the entered load magnitude to all selected nodes. Continue?") ==
                        DialogResult.Cancel)
                        throw new CaeException("BreakOnApply");
                }
            }
            else if (FELoad is DLoad dl)
            {
                if (dl.Magnitude.Value == 0)
                    throw new CaeException("The pressure load magnitude must not be equal to 0.");
            }
            else if (FELoad is HydrostaticPressure hpl)
            {
                if (!hpl.IsProperlyDefined(out string error)) throw new CaeException(error);
            }
            else if (FELoad is ImportedPressure ip)
            {
                if (!ip.IsProperlyDefined(out string error)) throw new CaeException(error);
            }
            else if (FELoad is STLoad stl)
            {
                if (stl.Magnitude.Value == 0)
                    throw new CaeException("At least one surface traction load component must not be equal to 0.");
            }
            else if (FELoad is ImportedSTLoad istl)
            {
                if (!istl.IsProperlyDefined(out string error)) throw new CaeException(error);
            }
            else if (FELoad is ShellEdgeLoad sel)
            {
                if (sel.Magnitude.Value == 0)
                    throw new CaeException("The shell edge load magnitude must not be equal to 0.");
            }
            else if (FELoad is GravityLoad gl)
            {
                if (gl.Magnitude.Value == 0)
                    throw new CaeException("At least one gravity load component must not be equal to 0.");
            }
            else if (FELoad is CentrifLoad cfl)
            {
                if (cfl.N1.Value == 0 && cfl.N2.Value == 0 && cfl.N3.Value == 0)
                    throw new CaeException("At least one axis direction component must not be equal to 0.");
                if (cfl.RotationalSpeed2 == 0)
                    throw new CaeException("Rotational speed must not be equal to 0.");
            }
            else if (FELoad is PreTensionLoad ptl)
            {
                if (!ptl.AutoComputeDirection && ptl.X.Value == 0 && ptl.Y.Value == 0 && ptl.Z.Value == 0)
                    throw new CaeException("At least one pre-tension direction component must not be equal to 0.");
                if (ptl.Type == PreTensionLoadType.Force && ptl.GetMagnitudeValue() == 0)
                    throw new CaeException("Pre-tension magnitude must not be equal to 0.");
            }
            // Thermal
            else if (FELoad is CFlux cf)
            {
                if ((cf.RegionType == RegionTypeEnum.Selection && cf.CreationIds.Length > 5) ||
                    (cf.RegionType == RegionTypeEnum.NodeSetName &&
                     _controller.Model.Mesh.NodeSets[cf.RegionName].Labels.Length > 5))
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel(
                        "The concentrated flux load will apply the entered flux magnitude to all selected nodes. Continue?") ==
                        DialogResult.Cancel)
                        throw new CaeException("BreakOnApply");
                }
            }
            else if (FELoad is DFlux df) { }
            else if (FELoad is BodyFlux bf) { }
            else if (FELoad is FilmHeatTransfer fht) { }
            else if (FELoad is RadiationHeatTransfer rht)
            {
                if (rht.CavityRadiation && (rht.CavityName == null || rht.CavityName == ""))
                    throw new CaeException("For cavity radiation a cavity name must be specified.");
            }
            // Create
            if (_loadToEditName == null)
            {
                _controller.AddLoadCommand(_stepName, FELoad);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceLoadCommand(_stepName, _loadToEditName, FELoad);
                _loadToEditName = null; // prevents the execution of toInternal in OnHideOrClose
            }
            // Convert the load from internal to show it
            else
            {
                LoadInternal(false);
            }
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            // Deactivate selection limits
            _controller.Selection.LimitSelectionToFirstGeometryType = false;
            _controller.Selection.EnableShellEdgeFaceSelection = false;
            _controller.Selection.LimitSelectionToShellEdges = false;
            // Convert the load from internal to show it
            LoadInternal(false);
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string loadToEditName)
        {
            this.btnOkAddNew.Visible = loadToEditName == null;
            // Clear
            _propertyItemChanged = false;
            _stepName = null;
            _loadNames = null;
            _loadToEditName = null;
            _viewLoad = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            //
            _stepName = stepName;
            _loadNames = _controller.GetAllLoadNames();
            _loadToEditName = loadToEditName;
            //
            string[] partNames = _controller.GetModelPartNames();
            string[] nodeSetNames = _controller.GetUserNodeSetNames();
            string[] elementSetNames = _controller.GetUserElementSetNames();
            // All element based surfaces
            string[] elementBasedSurfaceNames = _controller.GetUserSurfaceNames(FeSurfaceType.Element);
            // Only element based surfaces based on shell edges
            string[] shellEdgeSurfaceNames = _controller.GetUserSurfaceNames(FeSurfaceType.Element,
                                                                             FeSurfaceFaceTypes.ShellEdgeFaces);
            // Only element based surfaces based on solid faces
            string[] solidFaceSurfaceNames = _controller.GetUserSurfaceNames(FeSurfaceType.Element,
                                                                             FeSurfaceFaceTypes.SolidFaces);
            string[] referencePointNames = _controller.GetModelReferencePointNames();
            string[] massSectionNames = _controller.GetMassSectionNames();
            string[] amplitudeNames = _controller.GetAmplitudeNames();
            string[] coordinateSystemNames = _controller.GetModelCoordinateSystemNames();
            //
            if (partNames == null) partNames = new string[0];
            if (nodeSetNames == null) nodeSetNames = new string[0];
            if (elementSetNames == null) elementSetNames = new string[0];
            if (elementBasedSurfaceNames == null) elementBasedSurfaceNames = new string[0];
            if (shellEdgeSurfaceNames == null) shellEdgeSurfaceNames = new string[0];
            if (referencePointNames == null) referencePointNames = new string[0];
            if (massSectionNames == null) massSectionNames = new string[0];
            if (amplitudeNames == null) amplitudeNames = new string[0];
            if (coordinateSystemNames == null) coordinateSystemNames = new string[0];
            //
            if (_loadNames == null) throw new CaeException("The load names must be defined first.");
            //
            // Only element based surfaces based on solid and shell faces
            string[] noEdgeSurfaceNames = elementBasedSurfaceNames.Except(shellEdgeSurfaceNames).ToArray();
            //
            PopulateListOfLoads(partNames, nodeSetNames, elementSetNames, referencePointNames, elementBasedSurfaceNames,
                                solidFaceSurfaceNames, noEdgeSurfaceNames, shellEdgeSurfaceNames, massSectionNames,
                                amplitudeNames, coordinateSystemNames);
            // Check if this step supports any loads
            if (lvTypes.Items.Count == 0) return false;
            // Create new load
            if (_loadToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewLoad = null;
                //
                HighlightLoad(); // must be here if called from the menu
            }
            else
            {
                // Get and convert a converted load back to selection
                FELoad = _controller.GetLoad(stepName, _loadToEditName); // to clone
                if (FELoad.CreationData != null)
                {
                    if (!_controller.Model.IsLoadRegionValid(FELoad) ||
                        !_controller.Model.RegionValid(FELoad))     // do not use FELoad.Valid
                    {
                        // Not valid
                        FELoad.CreationData = null;
                        FELoad.CreationIds = null;
                        _propertyItemChanged = true;
                    }
                    FELoad.RegionType = RegionTypeEnum.Selection;
                }
                bool twoD = FELoad.TwoD;
                // Convert the load to internal to hide it
                LoadInternal(true);
                // Check for deleted amplitudes
                if (_viewLoad.AmplitudeName != null && _viewLoad.AmplitudeName != CaeModel.Load.DefaultAmplitudeName)
                    CheckMissingValueRef(ref amplitudeNames, _viewLoad.AmplitudeName, a => { _viewLoad.AmplitudeName = a; });
                // Check for deleted coordinate systems
                if (_viewLoad.CoordinateSystemName != CaeModel.Load.DefaultCoordinateSystemName)
                    CheckMissingValueRef(ref coordinateSystemNames, _viewLoad.CoordinateSystemName,
                                         s => { _viewLoad.CoordinateSystemName = s; });
                //
                int selectedId;
                if (_viewLoad is ViewCLoad vcl)
                {
                    selectedId = lvTypes.FindItemWithText("Concentrated Force").Index;
                    // Check for deleted regions
                    if (vcl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vcl.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vcl.NodeSetName, s => { vcl.NodeSetName = s; });
                    else if (vcl.RegionType == RegionTypeEnum.ReferencePointName.ToFriendlyString())
                        CheckMissingValueRef(ref referencePointNames, vcl.ReferencePointName, s => { vcl.ReferencePointName = s; });
                    else throw new NotSupportedException();
                    //
                    vcl.PopulateDropDownLists(nodeSetNames, referencePointNames, amplitudeNames, coordinateSystemNames);
                }
                else if (_viewLoad is ViewMomentLoad vml)
                {
                    selectedId = lvTypes.FindItemWithText("Moment").Index;
                    // Check for deleted regions
                    if (vml.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vml.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vml.NodeSetName, s => { vml.NodeSetName = s; });
                    else if (vml.RegionType == RegionTypeEnum.ReferencePointName.ToFriendlyString())
                        CheckMissingValueRef(ref referencePointNames, vml.ReferencePointName, s => { vml.ReferencePointName = s; });
                    else throw new NotSupportedException();
                    //
                    vml.PopulateDropDownLists(nodeSetNames, referencePointNames, amplitudeNames);
                }
                else if (_viewLoad is ViewDLoad vdl)
                {
                    string[] surfaceNames;
                    if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                    else surfaceNames = noEdgeSurfaceNames.ToArray();
                    //
                    selectedId = lvTypes.FindItemWithText("Uniform Pressure").Index;
                    // Check for deleted regions
                    if (vdl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vdl.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vdl.SurfaceName, s => { vdl.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vdl.PopulateDropDownLists(surfaceNames, amplitudeNames);
                }
                else if (_viewLoad is ViewHydrostaticPressureLoad vhpl)
                {
                    string[] surfaceNames;
                    if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                    else surfaceNames = elementBasedSurfaceNames.ToArray();
                    //
                    selectedId = lvTypes.FindItemWithText("Hydrostatic Pressure").Index;
                    // Check for deleted regions
                    if (vhpl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vhpl.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vhpl.SurfaceName, s => { vhpl.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vhpl.PopulateDropDownLists(surfaceNames, amplitudeNames);
                }
                else if (_viewLoad is ViewImportedPressureLoad vipl)
                {
                    string[] surfaceNames;
                    if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                    else surfaceNames = elementBasedSurfaceNames.ToArray();
                    //
                    selectedId = lvTypes.FindItemWithText("Imported Pressure").Index;
                    // Check for deleted regions
                    if (vipl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vipl.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vipl.SurfaceName, s => { vipl.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vipl.PopulateDropDownLists(surfaceNames, amplitudeNames);
                }
                else if (_viewLoad is ViewSTLoad vstl)
                {
                    string[] surfaceNames;
                    if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                    else surfaceNames = elementBasedSurfaceNames.ToArray();
                    //
                    selectedId = lvTypes.FindItemWithText("Surface Traction").Index;
                    // Check for deleted regions
                    if (vstl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vstl.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vstl.SurfaceName, s => { vstl.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vstl.PopulateDropDownLists(surfaceNames, amplitudeNames, coordinateSystemNames);
                }
                else if (_viewLoad is ViewImportedSTLoad vistl)
                {
                    string[] surfaceNames;
                    if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                    else surfaceNames = elementBasedSurfaceNames.ToArray();
                    //
                    selectedId = lvTypes.FindItemWithText("Imported Surface Traction").Index;
                    // Check for deleted regions
                    if (vistl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vistl.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vistl.SurfaceName, s => { vistl.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vistl.PopulateDropDownLists(surfaceNames, amplitudeNames);
                }
                else if (_viewLoad is ViewShellEdgeLoad vsel)
                {
                    selectedId = lvTypes.FindItemWithText("Normal Shell Edge Load").Index;
                    // Check for deleted regions
                    if (vsel.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vsel.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref shellEdgeSurfaceNames, vsel.SurfaceName, s => { vsel.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vsel.PopulateDropDownLists(shellEdgeSurfaceNames, amplitudeNames);
                }
                else if (_viewLoad is ViewGravityLoad vgl)
                {
                    selectedId = lvTypes.FindItemWithText("Gravity").Index;
                    // Check for deleted regions
                    if (vgl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vgl.RegionType == RegionTypeEnum.PartName.ToFriendlyString())
                        CheckMissingValueRef(ref partNames, vgl.PartName, s => { vgl.PartName = s; });
                    else if (vgl.RegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                        CheckMissingValueRef(ref elementSetNames, vgl.ElementSetName, s => { vgl.ElementSetName = s; });
                    else if (vgl.RegionType == RegionTypeEnum.MassSection.ToFriendlyString())
                        CheckMissingValueRef(ref massSectionNames, vgl.MassSectionName, s => { vgl.MassSectionName = s; });
                    else throw new NotSupportedException();
                    //
                    vgl.PopulateDropDownLists(partNames, elementSetNames, massSectionNames, amplitudeNames);
                }
                else if (_viewLoad is ViewCentrifLoad vcfl)
                {
                    selectedId = lvTypes.FindItemWithText("Centrifugal Load").Index;
                    // Check for deleted regions
                    if (vcfl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vcfl.RegionType == RegionTypeEnum.PartName.ToFriendlyString())
                        CheckMissingValueRef(ref partNames, vcfl.PartName, s => { vcfl.PartName = s; });
                    else if (vcfl.RegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                        CheckMissingValueRef(ref elementSetNames, vcfl.ElementSetName, s => { vcfl.ElementSetName = s; });
                    else if (vcfl.RegionType == RegionTypeEnum.MassSection.ToFriendlyString())
                        CheckMissingValueRef(ref massSectionNames, vcfl.MassSectionName, s => { vcfl.MassSectionName = s; });
                    else throw new NotSupportedException();
                    // Check for change in axisymmetric
                    bool isLoadAxiSymmetric = ((CentrifLoad)vcfl.GetBase()).Axisymmetric;
                    bool isModelAxiSymmetric = _controller.Model.Properties.ModelSpace == ModelSpaceEnum.Axisymmetric;
                    if (isLoadAxiSymmetric != isModelAxiSymmetric)
                    {
                        vcfl.Axisymmetric = isModelAxiSymmetric;
                        _propertyItemChanged = true;
                    }
                    //
                    vcfl.PopulateDropDownLists(partNames, elementSetNames, massSectionNames, amplitudeNames);
                }
                else if (_viewLoad is ViewPreTensionLoad vptl)
                {
                    selectedId = lvTypes.FindItemWithText("Pre-tension").Index;
                    // Check for deleted regions
                    if (vptl.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vptl.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref elementBasedSurfaceNames, vptl.SurfaceName, s => { vptl.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vptl.PopulateDropDownLists(solidFaceSurfaceNames, amplitudeNames);
                }
                // Thermal                                                                                                          
                else if (_viewLoad is ViewCFlux vcf)
                {
                    selectedId = lvTypes.FindItemWithText("Concentrated Flux").Index;
                    // Check for deleted regions
                    if (vcf.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vcf.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vcf.NodeSetName, s => { vcf.NodeSetName = s; });
                    else throw new NotSupportedException();
                    //
                    vcf.PopulateDropDownLists(nodeSetNames, amplitudeNames);
                }
                else if (_viewLoad is ViewDFlux vdf)
                {
                    string[] surfaceNames;
                    if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                    else surfaceNames = noEdgeSurfaceNames.ToArray();
                    //
                    selectedId = lvTypes.FindItemWithText("Surface Flux").Index;
                    // Check for deleted regions
                    if (vdf.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vdf.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vdf.SurfaceName, s => { vdf.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vdf.PopulateDropDownLists(surfaceNames, amplitudeNames);
                }
                else if (_viewLoad is ViewBodyFlux vbf)
                {
                    selectedId = lvTypes.FindItemWithText("Body Flux").Index;
                    // Check for deleted regions
                    if (vbf.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vbf.RegionType == RegionTypeEnum.PartName.ToFriendlyString())
                        CheckMissingValueRef(ref partNames, vbf.PartName, s => { vbf.PartName = s; });
                    else if (vbf.RegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                        CheckMissingValueRef(ref elementSetNames, vbf.ElementSetName, s => { vbf.ElementSetName = s; });
                    else throw new NotSupportedException();
                    //
                    vbf.PopulateDropDownLists(partNames, elementSetNames, amplitudeNames);
                }
                else if (_viewLoad is ViewFilmHeatTransfer vfht)
                {
                    string[] surfaceNames;
                    if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                    else surfaceNames = noEdgeSurfaceNames.ToArray();
                    //
                    selectedId = lvTypes.FindItemWithText("Convective Film").Index;
                    // Check for deleted regions
                    if (vfht.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vfht.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vfht.SurfaceName, s => { vfht.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vfht.PopulateDropDownLists(surfaceNames, amplitudeNames);
                }
                else if (_viewLoad is ViewRadiationHeatTransfer vrht)
                {
                    string[] surfaceNames;
                    if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                    else surfaceNames = noEdgeSurfaceNames.ToArray();
                    //
                    selectedId = lvTypes.FindItemWithText("Radiation").Index;
                    // Check for deleted regions
                    if (vrht.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vrht.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vrht.SurfaceName, s => { vrht.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vrht.PopulateDropDownLists(surfaceNames, amplitudeNames);
                }
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewLoad;
                _preselectIndex = selectedId;
            }
            ShowHideSelectionForm();
            //
            return true;
        }


        // Methods                                                                                                                  
        private void PopulateListOfLoads(string[] partNames, string[] nodeSetNames, string[] elementSetNames, 
                                         string[] referencePointNames, string[] elementBasedSurfaceNames,
                                         string[] solidFaceSurfaceNames, string[] noEdgeSurfaceNames,
                                         string[] shellEdgeSurfaceNames, string[] massSectionNames,
                                         string[] amplitudeNames, string[] coordinateSystemNames)
        {
            Step step = _controller.GetStep(_stepName);
            System.Drawing.Color color = _controller.Settings.Pre.LoadSymbolColor;
            bool twoD = _controller.Model.Properties.ModelSpace.IsTwoD();
            bool complex = step is SteadyStateDynamicsStep;
            // Populate list view
            ListViewItem item;
            string name;
            string loadName;
            // Concentrated force -  node set, reference points
            name = "Concentrated Force";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            CLoad cLoad = new CLoad(loadName, "", RegionTypeEnum.Selection, 0, 0, 0, twoD, complex, 0);
            if (step.IsLoadSupported(cLoad))
            {
                ViewCLoad vcl = new ViewCLoad(cLoad);
                vcl.PopulateDropDownLists(nodeSetNames, referencePointNames, amplitudeNames, coordinateSystemNames);
                vcl.Color = color;
                item.Tag = vcl;
                lvTypes.Items.Add(item);
            }
            // Moment
            name = "Moment";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            MomentLoad momentLoad = new MomentLoad(loadName, "", RegionTypeEnum.Selection, 0, 0, 0, twoD, complex, 0);
            if (step.IsLoadSupported(momentLoad))
            {
                ViewMomentLoad vml = new ViewMomentLoad(momentLoad);
                vml.PopulateDropDownLists(nodeSetNames, referencePointNames, amplitudeNames);
                vml.Color = color;
                item.Tag = vml;
                lvTypes.Items.Add(item);
            }
            // Pressure
            name = "Uniform Pressure";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            DLoad dLoad = new DLoad(loadName, "", RegionTypeEnum.Selection, 0, twoD, complex, 0);
            if (step.IsLoadSupported(dLoad))
            {
                string[] surfaceNames;
                if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                else surfaceNames = noEdgeSurfaceNames.ToArray();
                //
                ViewDLoad vdl = new ViewDLoad(dLoad);
                vdl.PopulateDropDownLists(surfaceNames, amplitudeNames);
                vdl.Color = color;
                item.Tag = vdl;
                lvTypes.Items.Add(item);
            }
            // Hydrostatic pressure
            name = "Hydrostatic Pressure";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            HydrostaticPressure hpLoad = new HydrostaticPressure(loadName, "", RegionTypeEnum.Selection, twoD, complex, 0);
            if (step.IsLoadSupported(hpLoad))
            {
                string[] surfaceNames;
                if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                else surfaceNames = elementBasedSurfaceNames.ToArray();
                //
                ViewHydrostaticPressureLoad vhpl = new ViewHydrostaticPressureLoad(hpLoad);
                vhpl.PopulateDropDownLists(surfaceNames, amplitudeNames);
                vhpl.Color = color;
                item.Tag = vhpl;
                lvTypes.Items.Add(item);
            }
            // Pressure
            name = "Imported Pressure";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            ImportedPressure ipLoad = new ImportedPressure(loadName, "", RegionTypeEnum.Selection, twoD, complex, 0);
            if (step.IsLoadSupported(ipLoad))
            {
                string[] surfaceNames;
                if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                else surfaceNames = noEdgeSurfaceNames.ToArray();
                //
                ViewImportedPressureLoad vipl = new ViewImportedPressureLoad(ipLoad);
                vipl.PopulateDropDownLists(surfaceNames, amplitudeNames);
                vipl.Color = color;
                item.Tag = vipl;
                lvTypes.Items.Add(item);
            }
            // Surface traction
            name = "Surface Traction";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            STLoad sTLoad = new STLoad(loadName, "", RegionTypeEnum.Selection, 0, 0, 0, twoD, complex, 0);
            if (step.IsLoadSupported(sTLoad))
            {
                string[] surfaceNames;
                if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                else surfaceNames = elementBasedSurfaceNames.ToArray();
                //
                ViewSTLoad vstl = new ViewSTLoad(sTLoad);
                vstl.PopulateDropDownLists(surfaceNames, amplitudeNames, coordinateSystemNames);
                vstl.Color = color;
                item.Tag = vstl;
                lvTypes.Items.Add(item);
            }
            // Imported surface traction
            name = "Imported Surface Traction";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            ImportedSTLoad istl = new ImportedSTLoad(loadName, "", RegionTypeEnum.Selection, twoD, complex, 0);
            if (step.IsLoadSupported(istl))
            {
                string[] surfaceNames;
                if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                else surfaceNames = noEdgeSurfaceNames.ToArray();
                //
                ViewImportedSTLoad vistl = new ViewImportedSTLoad(istl);
                vistl.PopulateDropDownLists(surfaceNames, amplitudeNames);
                vistl.Color = color;
                item.Tag = vistl;
                lvTypes.Items.Add(item);
            }
            // Shell edge load
            name = "Normal Shell Edge Load";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            ShellEdgeLoad shellEdgeLoad = new ShellEdgeLoad(loadName, "", RegionTypeEnum.Selection, 0, twoD, complex, 0);
            if (step.IsLoadSupported(shellEdgeLoad) && !twoD)
            {
                ViewShellEdgeLoad vsel = new ViewShellEdgeLoad(shellEdgeLoad);
                vsel.PopulateDropDownLists(shellEdgeSurfaceNames, amplitudeNames);
                vsel.Color = color;
                item.Tag = vsel;
                lvTypes.Items.Add(item);
            }
            // Gravity load -  part, element sets
            name = "Gravity";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            GravityLoad gravityLoad = new GravityLoad(loadName, "", RegionTypeEnum.Selection, twoD, complex, 0);
            if (step.IsLoadSupported(gravityLoad))
            {
                ViewGravityLoad vgl = new ViewGravityLoad(gravityLoad);
                vgl.PopulateDropDownLists(partNames, elementSetNames, massSectionNames, amplitudeNames);
                vgl.Color = color;
                item.Tag = vgl;
                lvTypes.Items.Add(item);
            }
            // Centrifugal load -  part, element sets
            name = "Centrifugal Load";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            bool axisymmetric = _controller.Model.Properties.ModelSpace == ModelSpaceEnum.Axisymmetric;
            CentrifLoad centrifLoad = new CentrifLoad(loadName, "", RegionTypeEnum.Selection, twoD, axisymmetric, complex, 0);
            ViewCentrifLoad vcfl = new ViewCentrifLoad(centrifLoad);
            if (step.IsLoadSupported(gravityLoad))
            {
                vcfl.PopulateDropDownLists(partNames, elementSetNames, massSectionNames, amplitudeNames);
                vcfl.Color = color;
                item.Tag = vcfl;
                lvTypes.Items.Add(item);
            }
            // Pre-tension load
            name = "Pre-tension";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            PreTensionLoad preTensionLoad = new PreTensionLoad(loadName, "", RegionTypeEnum.Selection, 0, twoD);
            if (step.IsLoadSupported(preTensionLoad) && !twoD)
            {
                ViewPreTensionLoad vptl = new ViewPreTensionLoad(preTensionLoad);
                vptl.PopulateDropDownLists(solidFaceSurfaceNames, amplitudeNames);
                vptl.Color = color;
                item.Tag = vptl;
                lvTypes.Items.Add(item);
            }
            // Thermal                                                                                                              
            // Concentrated flux -  node set
            name = "Concentrated Flux";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            CFlux cFlux = new CFlux(loadName, "", RegionTypeEnum.Selection, 0, twoD);
            if (step.IsLoadSupported(cFlux))
            {
                ViewCFlux vcf = new ViewCFlux(cFlux);
                vcf.PopulateDropDownLists(nodeSetNames, amplitudeNames);
                vcf.Color = color;
                item.Tag = vcf;
                lvTypes.Items.Add(item);
            }
            // Surface flux
            name = "Surface Flux";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            DFlux dFlux = new DFlux(loadName, "", RegionTypeEnum.Selection, 0, twoD);
            if (step.IsLoadSupported(dFlux))
            {
                string[] surfaceNames;
                if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                else surfaceNames = noEdgeSurfaceNames.ToArray();
                //
                ViewDFlux vdf = new ViewDFlux(dFlux);
                vdf.PopulateDropDownLists(surfaceNames, amplitudeNames);
                vdf.Color = color;
                item.Tag = vdf;
                lvTypes.Items.Add(item);
            }
            // Body flux
            name = "Body Flux";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            BodyFlux bFlux = new BodyFlux(loadName, "", RegionTypeEnum.Selection, 0, twoD);
            if (step.IsLoadSupported(bFlux))
            {
                ViewBodyFlux vbf = new ViewBodyFlux(bFlux);
                vbf.PopulateDropDownLists(partNames, elementSetNames, amplitudeNames);
                vbf.Color = color;
                item.Tag = vbf;
                lvTypes.Items.Add(item);
            }
            // Film heat transfer
            name = "Convective Film";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            FilmHeatTransfer filmHeatTransfer = new FilmHeatTransfer(loadName, "", RegionTypeEnum.Selection, 0, 0, twoD);
            if (step.IsLoadSupported(filmHeatTransfer))
            {
                string[] surfaceNames;
                if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                else surfaceNames = noEdgeSurfaceNames.ToArray();
                //
                ViewFilmHeatTransfer vfht = new ViewFilmHeatTransfer(filmHeatTransfer);
                vfht.PopulateDropDownLists(surfaceNames, amplitudeNames);
                vfht.Color = color;
                item.Tag = vfht;
                lvTypes.Items.Add(item);
            }
            // Radiation heat transfer
            name = "Radiation";
            loadName = GetLoadName(name);
            item = new ListViewItem(name);
            RadiationHeatTransfer radiationHeatTransfer = new RadiationHeatTransfer(loadName, "", RegionTypeEnum.Selection,
                                                                                    0, 1, twoD);
            if (step.IsLoadSupported(radiationHeatTransfer))
            {
                string[] surfaceNames;
                if (twoD) surfaceNames = shellEdgeSurfaceNames.ToArray();
                else surfaceNames = noEdgeSurfaceNames.ToArray();
                //
                ViewRadiationHeatTransfer vrht = new ViewRadiationHeatTransfer(radiationHeatTransfer);
                vrht.PopulateDropDownLists(surfaceNames, amplitudeNames);
                vrht.Color = color;
                item.Tag = vrht;
                lvTypes.Items.Add(item);
            }
        }
        private string GetLoadName(string name)
        {
            if (name == null || name == "") name = "Load";
            name = name.Replace(' ', '_');
            name = _loadNames.GetNextNumberedKey(name);
            //
            return name;
        }
        private void HighlightLoad()
        {
            try
            {
                _controller.ClearSelectionHistory();
                Load load = FELoad;
                //
                if (_viewLoad == null) { }
                else if (load is CLoad ||
                         load is MomentLoad ||
                         load is DLoad ||
                         load is HydrostaticPressure ||
                         load is ImportedPressure ||
                         load is STLoad ||
                         load is ImportedSTLoad ||
                         load is ShellEdgeLoad ||
                         load is GravityLoad ||
                         load is CentrifLoad ||
                         load is PreTensionLoad ||
                         // Thermal
                         load is CFlux ||
                         load is DFlux ||
                         load is BodyFlux ||
                         load is FilmHeatTransfer ||
                         load is RadiationHeatTransfer)
                {
                    if (load.RegionType == RegionTypeEnum.NodeSetName ||
                        load.RegionType == RegionTypeEnum.ReferencePointName ||
                        load.RegionType == RegionTypeEnum.SurfaceName ||
                        load.RegionType == RegionTypeEnum.PartName ||
                        load.RegionType == RegionTypeEnum.ElementSetName)
                    {
                        _controller.Highlight3DObjects(new object[] { load.RegionName });
                    }
                    else if (load.RegionType == RegionTypeEnum.MassSection)
                    {
                        Section section = _controller.Model.Sections[load.RegionName];
                        _controller.Highlight3DObjects(new object[] { section.RegionName });
                    }
                    else if (load.RegionType == RegionTypeEnum.Selection)
                    {
                        SetSelectItem();
                        //
                        if (load.CreationData != null)
                        {
                            _controller.Selection = load.CreationData.DeepClone();
                            _controller.HighlightSelection();
                        }
                    }
                    else throw new NotImplementedException();
                    // Secondary selection
                    if (load is HydrostaticPressure hp)
                    {
                        double[][] nodeCoor = new double[][] { new double[] { hp.X1.Value, hp.Y1.Value, hp.Z1.Value },
                                                               new double[] { hp.X2.Value, hp.Y2.Value, hp.Z2.Value } };
                        _controller.HighlightNodes(nodeCoor, true);
                    }
                    else if (load is ImportedSTLoad istl)
                    {
                        string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                        if (property == nameof(istl.FileName) || property == nameof(istl.GeometryScaleFactor))
                        {
                            if (istl.Interpolator == null) istl.ImportLoad();
                            //
                            if (istl.Interpolator != null && istl.Interpolator.CloudPoints != null)
                            {
                                CloudPoint[] cloudPoints = istl.Interpolator.CloudPoints;
                                double[][] nodeCoor = new double[cloudPoints.Length][];
                                //
                                for (int i = 0; i < cloudPoints.Length; i++)
                                {
                                    nodeCoor[i] = new double[] { cloudPoints[i].Coor[0], cloudPoints[i].Coor[1], cloudPoints[i].Coor[2] };
                                }
                                _controller.HighlightNodes(nodeCoor, true);
                            }
                        }
                    }
                    else if (load is CentrifLoad cf)
                    {
                        double[][] nodeCoor = new double[][] { new double[] { cf.X.Value, cf.Y.Value, cf.Z.Value } };
                        _controller.HighlightNodes(nodeCoor, true);
                    }
                }
                else throw new NotSupportedException();
                //
                if (load != null && load.CoordinateSystemName != null)
                    _controller.HighlightCoordinateSystem(load.CoordinateSystemName);
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            if (FELoad != null && FELoad.RegionType == RegionTypeEnum.Selection && Enabled)
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            else
                ItemSetDataEditor.SelectionForm.Hide();
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (FELoad != null && FELoad.RegionType == RegionTypeEnum.Selection)
            {
                if (FELoad is null) { }
                else if (FELoad is CLoad) _controller.SetSelectItemToNode();
                else if (FELoad is MomentLoad) _controller.SetSelectItemToNode();
                else if (FELoad is DLoad) _controller.SetSelectItemToSurface();
                else if (FELoad is HydrostaticPressure) _controller.SetSelectItemToSurface();
                else if (FELoad is ImportedPressure) _controller.SetSelectItemToSurface();
                else if (FELoad is STLoad) _controller.SetSelectItemToSurface();
                else if (FELoad is ImportedSTLoad) _controller.SetSelectItemToSurface();
                else if (FELoad is ShellEdgeLoad) _controller.SetSelectItemToSurface();
                else if (FELoad is GravityLoad) _controller.SetSelectItemToPart();
                else if (FELoad is CentrifLoad) _controller.SetSelectItemToPart();
                else if (FELoad is PreTensionLoad) _controller.SetSelectItemToSurface();
                // Thermal
                else if (FELoad is CFlux) _controller.SetSelectItemToNode();
                else if (FELoad is DFlux) _controller.SetSelectItemToSurface();
                else if (FELoad is BodyFlux) _controller.SetSelectItemToPart();
                else if (FELoad is FilmHeatTransfer) _controller.SetSelectItemToSurface();
                else if (FELoad is RadiationHeatTransfer) _controller.SetSelectItemToSurface();
                //
                else throw new NotSupportedException();
            }
            else _controller.SetSelectByToOff();
        }
        private void LoadInternal(bool toInternal)
        {
            if (_stepName != null && _loadToEditName != null)
            {
                // Convert the load from/to internal to hide/show it
                _controller.GetLoad(_stepName, _loadToEditName).Internal = toInternal;
                _controller.FeModelUpdate(UpdateType.RedrawSymbols);
            }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (Enabled)
            {
                if (FELoad != null && FELoad.RegionType == RegionTypeEnum.Selection)
                {
                    if (FELoad is CLoad ||
                        FELoad is MomentLoad ||
                        FELoad is DLoad ||
                        FELoad is HydrostaticPressure ||
                        FELoad is ImportedPressure ||
                        FELoad is STLoad ||
                        FELoad is ImportedSTLoad ||
                        FELoad is ShellEdgeLoad ||
                        FELoad is GravityLoad ||
                        FELoad is CentrifLoad ||
                        FELoad is PreTensionLoad ||
                        FELoad is CFlux ||
                        FELoad is DFlux ||
                        FELoad is BodyFlux ||
                        FELoad is FilmHeatTransfer ||
                        FELoad is RadiationHeatTransfer)
                    {
                        FELoad.CreationIds = ids;
                        FELoad.CreationData = _controller.Selection.DeepClone();
                        //
                        propertyGrid.Refresh();
                        //
                        _propertyItemChanged = true;
                        //
                        Highlight();
                    }
                    else throw new NotSupportedException();
                }
            }
            else
            {
                if (ids != null && ids.Length > 0)
                {
                    bool changed = false;
                    string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                    //
                    if (_viewLoad is ViewCentrifLoad vcl)
                    {
                        if (property == nameof(vcl.CenterPointItemSet))
                        {
                            if (ids.Length == 1)
                            {
                                FeNode node = _controller.Model.Mesh.Nodes[ids[0]];
                                vcl.X = new EquationString(node.X.ToString());
                                vcl.Y = new EquationString(node.Y.ToString());
                                vcl.Z = new EquationString(node.Z.ToString());
                                changed = true;
                            }
                        }
                    }
                    else if (_viewLoad is ViewHydrostaticPressureLoad vhpl)
                    {
                        if (property == nameof(vhpl.FirstPointItemSet))
                        {
                            if (ids.Length == 1)
                            {
                                FeNode node = _controller.Model.Mesh.Nodes[ids[0]];
                                vhpl.X1 = new EquationString(node.X.ToString());
                                vhpl.Y1 = new EquationString(node.Y.ToString());
                                vhpl.Z1 = new EquationString(node.Z.ToString());
                                changed = true;
                            }
                        }
                        else if (property == nameof(vhpl.SecondPointItemSet))
                        {
                            if (ids.Length == 1)
                            {
                                FeNode node = _controller.Model.Mesh.Nodes[ids[0]];
                                vhpl.X2 = new EquationString(node.X.ToString());
                                vhpl.Y2 = new EquationString(node.Y.ToString());
                                vhpl.Z2 = new EquationString(node.Z.ToString());
                                changed = true;
                            }
                        }
                        else if (property == nameof(vhpl.PressureDirectionItemSet))
                        {
                            if (ids.Length == 2)
                            {
                                FeNode node1 = _controller.Model.Mesh.Nodes[ids[0]];
                                FeNode node2 = _controller.Model.Mesh.Nodes[ids[1]];
                                vhpl.N1 = new EquationString((node2.X - node1.X).ToString());
                                vhpl.N2 = new EquationString((node2.Y - node1.Y).ToString());
                                vhpl.N3 = new EquationString((node2.Z - node1.Z).ToString());
                                changed = true;
                            }
                        }
                    }
                    //
                    if (changed)
                    {
                        Enabled = true; // must be first for the selection to work
                        //
                        propertyGrid.Refresh();
                        //
                        _propertyItemChanged = true;
                        //
                        _controller.Selection = _selectionCopy;
                        Highlight();
                    }
                }
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (Enabled && !_closing) HighlightLoad();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            Load load = FELoad;
            //
            if (load.CreationData != null) return load.CreationData.IsGeometryBased();
            else return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            Load load = FELoad;
            //
            if (load.CreationData != null && IsSelectionGeometryBased()) return load.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }

    }
}
