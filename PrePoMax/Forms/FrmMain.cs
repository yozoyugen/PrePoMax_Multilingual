﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using PrePoMax.Forms;
using CaeGlobals;
using UserControls;
using CaeJob;
using System.Reflection;
using CaeModel;
using CaeMesh;
using CaeResults;
using vtkControl;
using System.Threading;
using System.Runtime.InteropServices;
using PrePoMax.Commands;
using System.Timers;
using PrePoMax.Properties;
using System.Runtime;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Diagnostics;
using FileInOut.Output;
using System.Security.Cryptography;
using System.Runtime.Remoting.Messaging;
using System.Resources;

namespace PrePoMax
{
    public partial class FrmMain : MainMouseWheelManagedForm
    {
        // Variables                                                                                                                
        #region Variables ##########################################################################################################
        //
        FrmSplash _splash;
        //
        private vtkControl.vtkControl _vtk;
        private ModelTree _modelTree;
        private Controller _controller;
        private CommandLineOptions _cmdOptions;
        private string[] outputLines;
        private AdvisorControl _advisorControl;
        private KeyboardHook _keyboardHook;
        private Dictionary<ViewGeometryModelResults, int> _selectedSymbolIndex;
        private Stack<bool[]> _prevMenuStates;
        private bool _closeAfterRegeneration;
        private string _appLanguage = "en"; // my code
        //
        private Point _formLocation;
        private List<Form> _allForms;
        private FrmSectionView _frmSectionView;
        private FrmExplodedView _frmExplodedView;
        private FrmSelectEntity _frmSelectEntity;
        private FrmSelectGeometry _frmSelectGeometry;
        private FrmSelectItemSet _frmSelectItemSet;
        private FrmNewModel _frmNewModel;
        private FrmEditCommands _frmEditCommands;
        private FrmRegenerate _frmRegenerate;
        private FrmAnalyzeGeometry _frmAnalyzeGeometry;
        private FrmMeshSetupItem _frmMeshSetupItem;
        private FrmModelProperties _frmModelProperties;
        private FrmCalculixKeywordEditor _frmCalculixKeywordEditor;
        private FrmBoundaryLayer _frmBoundaryLayer;
        private FrmRemeshingParameters _frmRemeshingParameters;
        private FrmThickenShellMesh _frmThickenShellMesh;
        private FrmSplitPartMeshUsingSurface _frmSplitPartMeshUsingSurface;
        private FrmMergeCoincidentNodes _frmMergeCoincidentNodes;
        private FrmElementQuality _frmElementQuality;
        private FrmPartProperties _frmPartProperties;
        private FrmTranslate _frmTranslate;
        private FrmScale _frmScale;
        private FrmRotate _frmRotate;
        private FrmNodeSet _frmNodeSet;
        private FrmElementSet _frmElementSet;
        private FrmSurface _frmSurface;
        private FrmReferencePoint _frmReferencePoint;
        private FrmCoordinateSystem _frmCoordinateSystem;
        private FrmMaterial _frmMaterial;
        private FrmSection _frmSection;
        private FrmConstraint _frmConstraint;
        private FrmSurfaceInteraction _frmSurfaceInteraction;
        private FrmContactPair _frmContactPair;
        private FrmSearchContactPairs _frmSearchContactPairs;
        private FrmInitialCondition _frmInitialCondition;
        private FrmAmplitude _frmAmplitude;
        private FrmStep _frmStep;
        private FrmStepControls _frmStepControls;
        private FrmHistoryOutput _frmHistoryOutput;
        private FrmFieldOutput _frmFieldOutput;
        private FrmBC _frmBoundaryCondition;
        private FrmLoad _frmLoad;
        private FrmDefinedField _frmDefinedField;
        private FrmSettings _frmSettings;
        private FrmQuery _frmQuery;
        private FrmFind _frmFind;
        private FrmAnalysis _frmAnalysis;
        private FrmMonitor _frmMonitor;
        private FrmAnimation _frmAnimation;
        private FrmResultFieldOutput _frmResultFieldOutput;
        private FrmResultHistoryOutput _frmResultHistoryOutput;
        private FrmViewResultHistoryOutput _frmViewResultHistoryOutput;
        private FrmHistoryResultSetExporter _frmHistoryResultSetExporter;
        private FrmTransformation _frmTransformation;
        //
        #endregion  ################################################################################################################

        #region Properties #########################################################################################################
        public Controller Controller { get { return _controller; } set { _controller = value; } }
        public ViewGeometryModelResults GetCurrentView()
        {
            return _controller.CurrentView;
        }
        public void SetCurrentView(ViewGeometryModelResults view)
        {
            // This gets called from: _controller.CurrentView
            InvokeIfRequired(() =>
            {
                if (view == ViewGeometryModelResults.Geometry)
                {
                    _modelTree.SetGeometryTab();
                    if (_controller.Model != null) UpdateUnitSystem(_controller.Model.UnitSystem);
                }
                else if (view == ViewGeometryModelResults.Model)
                {
                    _modelTree.SetModelTab();
                    if (_controller.Model != null) UpdateUnitSystem(_controller.Model.UnitSystem);
                }
                else if (view == ViewGeometryModelResults.Results)
                {
                    _modelTree.SetResultsTab();
                    if (_controller.CurrentResult != null) UpdateUnitSystem(_controller.CurrentResult.UnitSystem);
                    InitializeResultWidgetPositions();
                }
                else throw new NotSupportedException();
                //
                if (_advisorControl != null)
                {
                    ViewType viewType = GetViewType(view);
                    //
                    _advisorControl.PrepareControls(viewType);
                }
                //
                SetMenuAndToolStripVisibility();
                // This calls the saved action
                SetCurrentEdgesVisibilities(_controller.CurrentEdgesVisibility);    // highlights selected buttons
                //
                this.ActiveControl = null;
            });
        }
        public void SetCurrentEdgesVisibilities(vtkControl.vtkEdgesVisibility edgesVisibility)
        {
            InvokeIfRequired(() =>
            {
                // Highlight selected buttons
                tsbShowWireframeEdges.Checked = edgesVisibility == vtkControl.vtkEdgesVisibility.Wireframe;
                tsbShowElementEdges.Checked = edgesVisibility == vtkControl.vtkEdgesVisibility.ElementEdges;
                tsbShowModelEdges.Checked = edgesVisibility == vtkControl.vtkEdgesVisibility.ModelEdges;
                tsbShowNoEdges.Checked = edgesVisibility == vtkControl.vtkEdgesVisibility.NoEdges;
                //
                _vtk.EdgesVisibility = edgesVisibility;
                //
                UpdateHighlight();
            });
        }
        public bool ScreenUpdating { get { return _modelTree.ScreenUpdating; } set { _modelTree.ScreenUpdating = value; } }
        public bool RenderingOn { get { return _vtk.RenderingOn; } set { _vtk.RenderingOn = value; } }
        private ViewType GetViewType(ViewGeometryModelResults view)
        {
            ViewType viewType;
            if (view == ViewGeometryModelResults.Geometry) viewType = ViewType.Geometry;
            else if (view == ViewGeometryModelResults.Model) viewType = ViewType.Model;
            else if (view == ViewGeometryModelResults.Results) viewType = ViewType.Results;
            else throw new NotSupportedException();
            return viewType;
        }

        #endregion  ################################################################################################################


        // Constructors                                                                                                             
        public FrmMain(CommandLineOptions cmdOptions)
        {
            // Initialize               
            InitializeComponent();
            //SettingsContainer settings = new SettingsContainer();
            //settings.LoadFromFile();
            //if (settings.General.Maximized)
            //{
            //    Rectangle resolution = Screen.FromControl(this).Bounds;
            //    this.Location = new Point(0, 0);
            //    this.Size = new Size(resolution.Width, resolution.Height);
            //    this.WindowState = FormWindowState.Maximized;
            //}
            _vtk = null;
            _controller = null;
            _modelTree = null;
            _prevMenuStates = new Stack<bool[]>();
            _closeAfterRegeneration = false;
            _cmdOptions = cmdOptions;
            CommandLineOptions.CheckForErrors(_cmdOptions); // make sure options are compatible
            //
            MessageBoxes.ParentForm = this;
            //
            if (_cmdOptions.ShowGui == "No")
            {
                MessageBoxes.WriteDataToOutput = WriteDataToOutput;
                AutoClosingMessageBox.WriteDataToOutput = WriteDataToOutput;
            }
        }
        // Event handling                                                                                                           
        private void FrmMain_Load(object sender, EventArgs e)
        {
            if (TestWriteAccess() == false)
            {
                MessageBoxes.ShowError("PrePoMax has no write access for the folder: " + Application.StartupPath +
                                       Environment.NewLine + Environment.NewLine +
                                       "To run PrePoMax, move the base PrePoMax folder to another, non-protected folder.");
                Close();
                return;
            }
            //
            //SettingsContainer settings = new SettingsContainer();
            //settings.LoadFromFile();
            //if (settings.General.Maximized)
            //{
            //    Rectangle resolution = Screen.FromControl(this).Bounds;
            //    this.Location = new Point(0, 0);
            //    this.Size = new Size(resolution.Width, resolution.Height);
            //    this.WindowState = FormWindowState.Maximized;
            //}
            Text = Globals.ProgramName;
            this.TopMost = true;
            //
            if (_cmdOptions.ShowGui == "Yes")
            {
                _splash = new FrmSplash { TopMost = true };
                Task.Run(() => _splash.ShowDialog());
            }
            //
            try
            {
                // Edit annotation text box
                panelControl.Controls.Remove(aeAnnotationTextEditor);
                this.Controls.Add(aeAnnotationTextEditor);
                // Vtk
                _vtk = new vtkControl.vtkControl();
                panelControl.Parent.Controls.Add(_vtk);
                panelControl.SendToBack();
                // Menu
                tsmiColorAnnotations.DropDown.Closing += DropDown_Closing;
                // Tree
                _modelTree = new ModelTree();
                _modelTree.Name = "modelTree";
                //_modelTree.Location = new Point(0, 0);
                splitContainer1.Panel1.Controls.Add(this._modelTree);
                _modelTree.Dock = DockStyle.Fill;
                //_modelTree.Size = splitContainer1.Panel1.ClientSize;
                //_modelTree.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                //_modelTree.Dock = DockStyle.None;
                _modelTree.TabIndex = 0;
                _modelTree.RegenerateTreeCallBack = RegenerateTreeCallback;
                //
                _modelTree.GeometryMeshResultsEvent += ModelTree_ViewEvent;
                _modelTree.SelectEvent += ModelTree_Select;
                _modelTree.ClearSelectionEvent += Clear3DSelection;
                _modelTree.CreateEvent += ModelTree_CreateEvent;
                _modelTree.EditEvent += ModelTree_Edit;
                _modelTree.SetPartColorEvent += ModelTree_SetPartColorEvent;
                _modelTree.ResetPartColorEvent += ModelTree_ResetPartColorEvent;
                _modelTree.EditStepControlsEvent += EditStepControls;
                _modelTree.QueryEvent += ModelTree_Query;
                _modelTree.DuplicateEvent += ModelTree_DuplicateEvent;
                _modelTree.PropagateEvent += ModelTree_PropagateEvent;
                _modelTree.PreviewEvent += ModelTree_PreviewEvent;
                _modelTree.CreateCompoundPart += CreateAndImportCompoundPart;
                _modelTree.SwapPartGeometries += SwapPartGeometries;
                _modelTree.PreviewEdgeMesh += PreviewEdgeMeshAsync;
                _modelTree.CreateMeshEvent += CreatePartMeshes;
                _modelTree.CopyGeometryToResultsEvent += CopyGeometryPartsToResults;
                _modelTree.EditCalculixKeywords += EditCalculiXKeywords;
                _modelTree.MergeParts += MergeModelParts;
                _modelTree.MergeResultParts += MergeResultParts;
                _modelTree.ConvertElementSetsToMeshParts += ConvertElementSetsToMeshParts;
                _modelTree.MaterialLibrary += ShowMaterialLibrary;
                _modelTree.SearchContactPairs += SearchContactPairs;
                _modelTree.SwapMasterSlave += ModelTree_SwapMasterSlave;
                _modelTree.MergeByMasterSlave += ModelTree_MergeByMasterSlave;
                _modelTree.HideShowEvent += ModelTree_HideShowEvent;
                _modelTree.SetTransparencyEvent += ModelTree_SetTransparencyEvent;
                _modelTree.ColorContoursVisibilityEvent += ModelTree_ColorContoursVisibilityEvent;
                _modelTree.RunEvent += RunAnalysis;
                _modelTree.CheckModelEvent += CheckModel;
                _modelTree.MonitorEvent += MonitorAnalysis;
                _modelTree.ResultsEvent += ResultsAnalysis;
                _modelTree.KillEvent += KillAnalysis;
                _modelTree.ActivateDeactivateEvent += ModelTree_ActivateDeactivateEvent;
                _modelTree.DeleteEvent += ModelTree_Delete;
                _modelTree.FieldDataSelectEvent += ModelTree_FieldDataSelectEvent;
                _modelTree.RenderingOff += () => _vtk.RenderingOn = false;
                _modelTree.RenderingOn += () => _vtk.RenderingOn = true;
                // Strip menus
                tsFile.Location = new Point(0, 0);
                tsViews.Location = new Point(tsFile.Left + tsFile.Width, 0);
                tsSymbols.Location = new Point(tsViews.Left + tsViews.Width, 0);
                tsResultDeformation.Location = new Point(0, tsFile.Height);
                tsResults.Location = new Point(tsResultDeformation.Left + tsResultDeformation.Width, tsFile.Height);
                tscbSymbols.SelectedIndexChanged += tscbSymbols_SelectedIndexChanged;
                // Controller
                _controller = new Controller(this);
                // Vtk
                _vtk.OnMouseLeftButtonUpSelection += SelectPointOrArea;
                _vtk.Controller_GetAnnotationText += _controller.GetAnnotationText;
                _vtk.Controller_GetNodeActorData = _controller.GetNodeActorData;
                _vtk.Controller_GetCellActorData = _controller.GetCellActorData;
                _vtk.Controller_GetCellFaceActorData = _controller.GetCellFaceActorData;
                _vtk.Controller_GetEdgeActorData = _controller.GetEdgeActorData;
                _vtk.Controller_GetSurfaceEdgesActorDataFromElementId = _controller.GetSurfaceEdgesActorDataFromElementId;
                _vtk.Controller_GetSurfaceEdgesActorDataFromNodeAndElementIds =
                    _controller.GetSurfaceEdgesActorDataFromNodeAndElementIds;
                _vtk.Controller_GetPartActorData = _controller.GetPartActorData;
                _vtk.Controller_GetGeometryActorData = _controller.GetGeometryActorData;
                _vtk.Controller_GetGeometryVertexActorData = _controller.GetGeometryVertexActorData;
                _vtk.Controller_ActorsPicked = SelectBaseParts;
                _vtk.Form_ShowColorBarSettings = ShowColorBarSettings;
                _vtk.Form_ShowLegendSettings = ShowLegendSettings;
                _vtk.Form_ShowStatusBlockSettings = ShowStatusBlockSettings;
                _vtk.Form_EndEditArrowWidget = EndEditArrowAnnotation;
                _vtk.Form_WidgetPicked = AnnotationPicked;
                // Forms
                _formLocation = new Point(100, 100);
                _allForms = new List<Form>();
                //
                _frmSelectEntity = new FrmSelectEntity(_controller);
                AddFormToAllForms(_frmSelectEntity);
                //
                _frmSelectGeometry = new FrmSelectGeometry(_controller);
                AddFormToAllForms(_frmSelectGeometry);
                //
                _frmSelectItemSet = new FrmSelectItemSet(_controller);
                AddFormToAllForms(_frmSelectItemSet);
                //
                _frmSectionView = new FrmSectionView(_controller);
                AddFormToAllForms(_frmSectionView);
                //
                _frmExplodedView = new FrmExplodedView(_controller);
                _frmExplodedView.Clear3D = Clear3DSelection;
                AddFormToAllForms(_frmExplodedView);
                //
                _frmNewModel = new FrmNewModel(_controller);
                AddFormToAllForms(_frmNewModel);
                //
                _frmEditCommands = new FrmEditCommands(_controller);
                AddFormToAllForms(_frmEditCommands);
                //
                _frmRegenerate = new FrmRegenerate();
                AddFormToAllForms(_frmRegenerate);
                //
                _frmAnalyzeGeometry = new FrmAnalyzeGeometry(_controller);
                AddFormToAllForms(_frmAnalyzeGeometry);
                //
                _frmMeshSetupItem = new FrmMeshSetupItem(_controller);
                _frmMeshSetupItem.PreviewEdgeMeshAsync = PreviewEdgeMeshAsync;
                AddFormToAllForms(_frmMeshSetupItem);
                //
                _frmModelProperties = new FrmModelProperties(_controller);
                AddFormToAllForms(_frmModelProperties);
                //
                _frmBoundaryLayer = new FrmBoundaryLayer(_controller);
                AddFormToAllForms(_frmBoundaryLayer);
                //
                _frmRemeshingParameters = new FrmRemeshingParameters(_controller);
                AddFormToAllForms(_frmRemeshingParameters);
                //
                _frmThickenShellMesh = new FrmThickenShellMesh(_controller);
                AddFormToAllForms(_frmThickenShellMesh);
                //
                _frmSplitPartMeshUsingSurface = new FrmSplitPartMeshUsingSurface(_controller);
                _frmSplitPartMeshUsingSurface.SplitPartMeshUsingSurface += SplitPartMeshUsingSurface;
                AddFormToAllForms(_frmSplitPartMeshUsingSurface);
                //
                _frmMergeCoincidentNodes = new FrmMergeCoincidentNodes(_controller);
                AddFormToAllForms(_frmMergeCoincidentNodes);
                //
                _frmElementQuality = new FrmElementQuality(_controller);
                AddFormToAllForms(_frmElementQuality);
                //
                _frmPartProperties = new FrmPartProperties(_controller);
                AddFormToAllForms(_frmPartProperties);
                //
                _frmTranslate = new FrmTranslate(_controller);
                AddFormToAllForms(_frmTranslate);
                //
                _frmScale = new FrmScale(_controller);
                _frmScale.ScaleGeometryPartsAsync = ScaleGeometryPartsAsync;
                AddFormToAllForms(_frmScale);
                //
                _frmRotate = new FrmRotate(_controller);
                AddFormToAllForms(_frmRotate);
                //
                _frmNodeSet = new FrmNodeSet(_controller);
                AddFormToAllForms(_frmNodeSet);
                //
                _frmElementSet = new FrmElementSet(_controller);
                AddFormToAllForms(_frmElementSet);
                //
                _frmSurface = new FrmSurface(_controller);
                AddFormToAllForms(_frmSurface);
                //
                _frmReferencePoint = new FrmReferencePoint(_controller);
                AddFormToAllForms(_frmReferencePoint);
                //
                _frmCoordinateSystem = new FrmCoordinateSystem(_controller);
                AddFormToAllForms(_frmCoordinateSystem);
                //
                _frmMaterial = new FrmMaterial(_controller);
                AddFormToAllForms(_frmMaterial);
                //
                _frmSection = new FrmSection(_controller);
                AddFormToAllForms(_frmSection);
                //
                _frmConstraint = new FrmConstraint(_controller);
                AddFormToAllForms(_frmConstraint);
                //
                _frmSurfaceInteraction = new FrmSurfaceInteraction(_controller);
                AddFormToAllForms(_frmSurfaceInteraction);
                //
                _frmContactPair = new FrmContactPair(_controller);
                AddFormToAllForms(_frmContactPair);
                //
                _frmSearchContactPairs = new FrmSearchContactPairs(_controller);
                AddFormToAllForms(_frmSearchContactPairs);
                //
                _frmInitialCondition = new FrmInitialCondition(_controller);
                AddFormToAllForms(_frmInitialCondition);
                //
                _frmAmplitude = new FrmAmplitude(_controller);
                AddFormToAllForms(_frmAmplitude);
                //
                _frmStep = new FrmStep(_controller);
                AddFormToAllForms(_frmStep);
                //
                _frmStepControls = new FrmStepControls(_controller);
                AddFormToAllForms(_frmStepControls);
                //
                _frmHistoryOutput = new FrmHistoryOutput(_controller);
                AddFormToAllForms(_frmHistoryOutput);
                //
                _frmFieldOutput = new FrmFieldOutput(_controller);
                AddFormToAllForms(_frmFieldOutput);
                //
                _frmBoundaryCondition = new FrmBC(_controller);
                AddFormToAllForms(_frmBoundaryCondition);
                //
                _frmLoad = new FrmLoad(_controller);
                AddFormToAllForms(_frmLoad);
                //
                _frmDefinedField = new FrmDefinedField(_controller);
                AddFormToAllForms(_frmDefinedField);
                //
                _frmAnalysis = new FrmAnalysis(_controller);
                AddFormToAllForms(_frmAnalysis);
                //
                _frmMonitor = new FrmMonitor(_controller);
                _frmMonitor.KillJob += KillAnalysis;
                _frmMonitor.Results += ResultsAnalysis;
                AddFormToAllForms(_frmMonitor);
                //
                _frmSettings = new FrmSettings();
                _frmSettings.UpdateSettings += UpdateSettings;
                AddFormToAllForms(_frmSettings);
                //
                _frmQuery = new FrmQuery();
                _frmQuery.Form_WriteDataToOutput = WriteDataToOutput;
                _frmQuery.Form_RemoveAnnotations = tsbRemoveAnnotations_Click;
                AddFormToAllForms(_frmQuery);
                //
                _frmFind = new FrmFind();
                _frmFind.Form_RemoveAnnotations = tsbRemoveAnnotations_Click;
                AddFormToAllForms(_frmFind);
                //
                _frmAnimation = new FrmAnimation();
                _frmAnimation.Form_ControlsEnable = SetMenuAndToolStripVisibilityByAnimation;
                AddFormToAllForms(_frmAnimation);
                //
                _frmResultFieldOutput = new FrmResultFieldOutput(_controller);
                AddFormToAllForms(_frmResultFieldOutput);
                //
                _frmResultHistoryOutput = new FrmResultHistoryOutput(_controller);
                AddFormToAllForms(_frmResultHistoryOutput);
                //
                _frmViewResultHistoryOutput = new FrmViewResultHistoryOutput(_controller);
                AddFormToAllForms(_frmViewResultHistoryOutput);
                //
                _frmHistoryResultSetExporter = new FrmHistoryResultSetExporter(_controller);
                AddFormToAllForms(_frmHistoryResultSetExporter);
                //
                _frmTransformation = new FrmTransformation(_controller);
                AddFormToAllForms(_frmTransformation);
                // Deformation toolstrip
                InitializeDeformationComboBoxes();
                InitializeComplexComboBoxes();
                // Converters
                tstbDeformationFactor.UnitConverter = new StringDoubleConverter();
                tstbAngle.UnitConverter = new StringAngleDegConverter();
                // Create the Keyboard Hook
                _keyboardHook = new KeyboardHook();
                // Capture the events
                _keyboardHook.KeyDown += KeyboardHook_KeyDown;
                // Install the hook
                _keyboardHook.Install();
            }
            catch
            {
                // If no error the splash is closed latter
                _splash?.BeginInvoke((MethodInvoker)delegate () { _splash.Close(); });
            }
            finally
            {
                this.TopMost = false;
                // Set form size if visible - after top most
                if (_cmdOptions.ShowGui == "Yes") _controller.Settings.General.ApplyFormSize(this);
            }
            //
            if (!Debugger.IsAttached)
            {
                tsmiExportToGmshMesh.Visible = false;
                tsmiTest.Visible = false;
                tsmiCropStlPartWithCylinder.Visible = false;
                tsmiCropStlPartWithCube.Visible = false;
            }
        }
        //
        private async void FrmMain_Shown(object sender, EventArgs e)
        {
            // Set vtk control size
            UpdateVtkControlSize();
            Application.DoEvents(); // draws the menus
            //
            _vtk.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            // Set pass through control for the mouse wheel event
            this.PassThroughControl = _vtk;
            // Vtk
            // Reduce flicker
            _vtk.Visible = true;
            _vtk.ResetScaleBarPosition();   // start after the vtk size is set
            _vtk.SetZoomFactor(1000);       // set starting zoom larger that the object
            _controller.Redraw();
            // Move _vtk to the edge of the screen to hide the black background
            int left = _vtk.Left;
            _vtk.Left = _vtk.Width + 4;
            Application.DoEvents(); // draws the vtk
            _vtk.Visible = false;
            _vtk.Left = left;
            // Close splash 
            if (_splash != null) _splash.BeginInvoke((MethodInvoker)delegate () { _splash.Close(); });
            // At the end when vtk is loaded open the file
            string fileName = null;
            UnitSystemType unitSystemType = UnitSystemType.Undefined;
            //
            try
            {
                // Regeneration
                if (_cmdOptions.RegenerationFileName != null)
                {
                    WriteDataToOutput("Starting regeneration");
                    //
                    fileName = _cmdOptions.RegenerationFileName;
                    //
                    if (fileName == null)
                        throw new CaeException("The regeneration file name is null.");
                    else if (!File.Exists(fileName))
                        throw new CaeException("The regeneration file " + fileName + " does not exist.");
                    //
                    _controller.RegenerationWorkDirectory = _cmdOptions.WorkDirectory;
                    // Open
                    await Task.Run(() => OpenAsync(fileName, _controller.Open));
                    // Parameters
                    if (_cmdOptions.Parameters != null)
                        _controller.AddOverriddenParametersFromString(_cmdOptions.Parameters);
                    // Regenerate
                    //
                    await Task.Run(() => _controller.RegenerateHistoryCommands(false, false, _cmdOptions.GetRegenerateType()));
                    // Check for errors
                    if (_controller.GetCommandCollectionErrors() != null && _controller.GetCommandCollectionErrors().Count > 0)
                        throw new CaeException("Failed to regenerate some commands.");
                    // Overwrite
                    if (_cmdOptions.Overwrite == "Yes")
                    {
                        WriteDataToOutput("Overwrite: " + fileName);
                        _controller.SaveToPmx(fileName);
                    }
                    // Exit
                    if (_cmdOptions.ExitAfterRegeneration == "Yes")
                    {
                        _closeAfterRegeneration = true;
                        WriteDataToOutput("Close application.");
                        Close();
                    }
                }
                else
                {
                    string parameters = null;
                    // Try to recover unsaved progress due to crushed PrePoMax
                    if (File.Exists(_controller.GetHistoryFileNameBin()))
                    {
                        if (MessageBoxes.ShowWarningQuestionOKCancel("A recovery file from a previous PrePoMax session exists. " +
                                                                     "Would you like to try to recover it?") == DialogResult.OK)
                        {
                            fileName = _controller.GetHistoryFileNameBin();
                            //
                            bool regenerateAll = MessageBoxes.ShowQuestionYesNo("Regenerate All",
                                "Select Yes to regenerate all commands or No to regenerate FE model commands only. " +
                                "All commands can be regenerated later using Edit → Regenerate All.") == DialogResult.Yes;
                            if (regenerateAll) parameters = Globals.RegenerateAll;
                        }
                    }
                    if (fileName == null)
                    {
                        // Open file from exe arguments
                        if (_cmdOptions.FileName != null)
                        {
                            fileName = _cmdOptions.FileName;
                            //
                            if (_cmdOptions.UnitSystem != null)
                            {
                                if (!Enum.TryParse(_cmdOptions.UnitSystem.ToUpper(), out unitSystemType))
                                    throw new CaeException("The unit system type " + _cmdOptions.UnitSystem + " is not supported.");
                            }
                        }
                        // Check for open last file
                        else if (_controller.Settings.General.OpenLastFile) fileName = _controller.OpenedFileName;
                    }
                    //
                    if (File.Exists(fileName))
                    {
                        fileName = Path.GetFullPath(fileName);  // change local file name to global
                        string extension = Path.GetExtension(fileName).ToLower();
                        HashSet<string> importExtensions = GetFileImportExtensions();
                        //
                        if (extension == ".pmx" || extension == ".pmh" || extension == ".frd")
                            await Task.Run(() => OpenAsync(fileName, _controller.Open, true, null, parameters));
                        else if (importExtensions.Contains(extension))
                        {
                            // Create new model
                            if (New(ModelSpaceEnum.ThreeD, unitSystemType))
                            {
                                // Import
                                await _controller.ImportFileAsync(fileName, false);
                                // Set to null, otherwise the previous OpenedFileName gets overwritten on Save
                                _controller.OpenedFileName = null;
                            }
                        }
                        else MessageBoxes.ShowError("The file name extension is not supported.");
                        //
                        _vtk.SetFrontBackView(false, true);
                    }
                    else
                    {
                        _controller.CurrentView = ViewGeometryModelResults.Geometry;
                        //
                        UpdateRecentFilesThreadSafe(_controller.Settings.General.GetRecentFiles());
                    }
                }
            }
            catch (Exception ex)
            {
                // Regeneration
                if (_controller.BatchRegenerationMode)
                {
                    throw new CaeException(ex.Message, ex);
                }
                else
                {
                    ExceptionTools.Show(this, ex);
                    _controller.ModelChanged = false;   // hide messageBox
                    tsmiNew_Click(null, null);
                }
            }
            finally
            {
            }
        }
        private void FrmMain_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized && _frmAnimation != null && _frmAnimation.Visible)
                _frmAnimation.UpdateAnimation();
            //
            if (Debugger.IsAttached)
            {
                WriteDataToOutput(Width + " : " + Height);
            }
        }
        private async void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            string error = null;
            try
            {
                e.Cancel = false;   // close the form
                DialogResult response = DialogResult.None;
                // No write access
                if (_controller == null) return;
                // Analysis in progress - first check: it has to kill running analyses
                foreach (var entry in _controller.Jobs)
                {
                    if (entry.Value.JobStatus == JobStatus.Running)
                    {
                        if (!_controller.BatchRegenerationMode)
                            response = MessageBoxes.ShowWarningQuestionOKCancel("There is an analysis running. " +
                                "Closing will kill the analysis. Close anyway?");
                        else response = DialogResult.OK;
                        //
                        if (response == DialogResult.Cancel) e.Cancel = true;
                        else if (response == DialogResult.OK)
                        {
                            _controller.KillAllJobs();
                            error = "The analysis was killed by the user.";
                            throw new CaeException(error);
                        }
                        break;
                    }
                }
                // Regeneration in progress
                if (_controller.BatchRegenerationMode && !_closeAfterRegeneration)
                {
                    error = "PrePoMax closed by the user.";
                    throw new CaeException(error);
                }
                // Saving in progress
                if (tsslState.Text != Globals.ReadyText)
                {
                    if (!_controller.BatchRegenerationMode)
                        response = MessageBoxes.ShowWarningQuestionOKCancel("There is a task running. Close anyway?");
                    else response = DialogResult.OK;
                    //
                    if (response == DialogResult.Cancel) e.Cancel = true;
                    else if (response == DialogResult.OK && _controller.SavingFile)
                    {
                        // Wait for saving to finish
                        while (_controller.SavingFile) Thread.Sleep(100);
                    }
                }
                // Model changed
                else if (_controller.ModelChanged)
                {
                    if (!_controller.BatchRegenerationMode)
                        response = MessageBoxes.ShowWarningQuestionYesNoCancel("Save file before closing?");
                    else response = DialogResult.No;
                    //
                    if (response == DialogResult.Yes)
                    {
                        e.Cancel = true;                                // stop the form from closing before saving
                        await Task.Run(() => _controller.Save());       // save
                        Close();                                        // close the control
                    }
                    else if (response == DialogResult.Cancel) e.Cancel = true;
                }
                // Save form size and location and delete history files
                if (e.Cancel == false && _controller != null)
                {
                    if (!_controller.BatchRegenerationMode)
                    {
                        SettingsContainer settings = _controller.Settings;  // get a clone
                        settings.General.SaveFormSize(this);                // save form size
                        _controller.Settings = settings;                    // update values and save to file
                        //
                        _controller.DeleteHistoryFiles();
                        //
                        _vtk.Clear();
                        _vtk.Dispose();
                        _vtk = null;
                    }
                }
            }
            catch
            { 
                if (error != null) throw new CaeException(error);
            }
        }
        private void FrmMain_Move(object sender, EventArgs e)
        {
            if (_allForms == null) return;

            foreach (Form form in _allForms)
            {
                if (form.Visible)
                {
                    //form.Location = new Point(Left + _formLocation.X, Top + _formLocation.Y);
                }
            }
        }
        private void UpdateVtkControlSize()
        {
            // Update vtk control size
            Rectangle bounds = panelControl.Bounds;
            bounds.Inflate(-2, -2);
            if (_vtk.Bounds != bounds) _vtk.Bounds = bounds;
        }
        private bool TestWriteAccess()
        {
            try
            {
                string fileName = Tools.GetNonExistentRandomFileName(Application.StartupPath, ".test");
                File.WriteAllText(fileName, "");
                //
                File.Delete(fileName);
                //
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Forms
        private void itemForm_VisibleChanged(object sender, EventArgs e)
        {
            Form form = sender as Form;
            bool menusActive = !form.Visible;
            //
            if (form is FrmSelectItemSet) return;
            //
            if (form is FrmAnimation)
            {
                SetMenuAndToolStripVisibilityByAnimation(!form.Visible);
            }
            else
            {
                SetMenuAndToolStripVisibilityByItemForm(menusActive);
            }
            // This gets also called from item selection form: by angle, by edge ...
            if (menusActive)
            {
                UpdateHighlightFromTree();
                SaveFormLocation(form);
                //
                _controller.SetSelectByToDefault();
                //
                this.Focus();
            }
        }
        private void itemForm_Move(object sender, EventArgs e)
        {
            Form form = sender as Form;
            //Size screenSize =  Screen.GetWorkingArea(form).Size;
            //if (form.Left < 0) form.Left = 0;
            //else if (form.Left + form.Width > screenSize.Width) form.Left = screenSize.Width - form.Width;
            //if (form.Top < 0) form.Top = 0;
            //else if (form.Top + form.Height > screenSize.Height) form.Top = screenSize.Height - form.Height;
            SaveFormLocation(form);
        }
        // Keyboard
        private void KeyboardHook_KeyDown(KeyboardHook.VKeys vKey)
        {
            if (this == ActiveForm)
            {
                Keys key = (Keys)vKey;
                //
                if (key == Keys.Escape)
                {
                    if (!_vtk.IsRubberBandActive) CloseAllForms();
                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    //if (key == Keys.I) tsmiImportFile_Click(null, null);
                    //else if (key == Keys.N) tsmiNew_Click(null, null);
                    //else if (key == Keys.O) tsmiOpen_Click(null, null);
                    //else if (key == Keys.S) tsmiSave_Click(null, null);
                    //else if (key == Keys.X) tsmiExit_Click(null, null);
                }
                // Model tree
                else if (_modelTree.ActiveControl == null || !_modelTree.ActiveControl.Focused)
                {
                    Control focusedControl = FindFocusedControl(this);
                    // Check for toolstrip
                    if (focusedControl != null && focusedControl.Parent is ToolStripFocus) { }
                    // Check for annotation editor
                    else if (aeAnnotationTextEditor.Visible) { }
                    // Forward to tree
                    else _modelTree.cltv_KeyDown(this, new KeyEventArgs(key));
                }
            }
        }
        public static Control FindFocusedControl(Control control)
        {
            var container = control as IContainerControl;
            while (container != null)
            {
                control = container.ActiveControl;
                container = control as IContainerControl;
            }
            return control;
        }
        //
        private void timerOutput_Tick(object sender, EventArgs e)
        {
            tbOutput.Lines = outputLines;
            tbOutput.SelectionStart = tbOutput.Text.Length;
            tbOutput.ScrollToCaret();
            timerOutput.Stop();
        }

        #region ModelTree Events ###################################################################################################
        //
        internal void ModelTree_ViewEvent(ViewType viewType)
        {
            try
            {
                if ((viewType == ViewType.Geometry && GetCurrentView() == ViewGeometryModelResults.Geometry) ||
                    (viewType == ViewType.Model && GetCurrentView() == ViewGeometryModelResults.Model) ||
                    (viewType == ViewType.Results && GetCurrentView() == ViewGeometryModelResults.Results)) return;
                //
                CloseAllForms();
                _controller.SelectBy = vtkSelectBy.Default;
                //
                if (viewType == ViewType.Geometry) _controller.CurrentView = ViewGeometryModelResults.Geometry;
                else if (viewType == ViewType.Model) _controller.CurrentView = ViewGeometryModelResults.Model;
                else if (viewType == ViewType.Results) _controller.CurrentView = ViewGeometryModelResults.Results;
                else throw new NotSupportedException();
                //
                _advisorControl?.PrepareControls(viewType);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void ModelTree_Select(NamedClass[] items, bool mouseOver)
        {
            try
            {
                _controller.Highlight3DObjects(items, mouseOver);
            }
            catch { }
        }
        //
        private void ModelTree_CreateEvent(string nodeName, string stepName)
        {
            if (_controller.Model.Geometry != null && _controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                if (nodeName == _modelTree.MeshSetupItemsName) tsmiCreateMeshSetupItem_Click(null, null);
            }
            else if (_controller.Model.Mesh != null && _controller.CurrentView == ViewGeometryModelResults.Model)
            {
                // _controller.Model.Mesh defines the unit system and must
                if (nodeName == _modelTree.NodeSetsName) tsmiCreateNodeSet_Click(null, null);
                else if (nodeName == _modelTree.ElementSetsName) tsmiCreateElementSet_Click(null, null);
                else if (nodeName == _modelTree.SurfacesName) tsmiCreateSurface_Click(null, null);
                else if (nodeName == _modelTree.ModelReferencePointsName) tsmiCreateModelReferencePoint_Click(null, null);
                else if (nodeName == _modelTree.ModelCoordinateSystemsName) tsmiCreateModelCoordinateSystem_Click(null, null);
                else if (nodeName == _modelTree.MaterialsName) tsmiCreateMaterial_Click(null, null);
                else if (nodeName == _modelTree.SectionsName) tsmiCreateSection_Click(null, null);
                else if (nodeName == _modelTree.ConstraintsName) tsmiCreateConstraint_Click(null, null);
                else if (nodeName == _modelTree.SurfaceInteractionsName) tsmiCreateSurfaceInteraction_Click(null, null);
                else if (nodeName == _modelTree.ContactPairsName) tsmiCreateContactPair_Click(null, null);
                else if (nodeName == _modelTree.AmplitudesName) tsmiCreateAmplitude_Click(null, null);
                else if (nodeName == _modelTree.InitialConditionsName) tsmiCreateInitialCondition_Click(null, null);
                else if (nodeName == _modelTree.StepsName) tsmiCreateStep_Click(null, null);
                else if (nodeName == _modelTree.ModelHistoryOutputsName && stepName != null) CreateHistoryOutput(stepName);
                else if (nodeName == _modelTree.ModelFieldOutputsName && stepName != null) CreateFieldOutput(stepName);
                else if (nodeName == _modelTree.BoundaryConditionsName && stepName != null) CreateBoundaryCondition(stepName);
                else if (nodeName == _modelTree.LoadsName && stepName != null) CreateLoad(stepName);
                else if (nodeName == _modelTree.DefinedFieldsName && stepName != null) CreateDefinedField(stepName);
                else if (nodeName == _modelTree.AnalysesName) tsmiCreateAnalysis_Click(null, null);
            }
            else if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null &&
                     _controller.CurrentView == ViewGeometryModelResults.Results)
            {
                if (nodeName == _modelTree.ResultReferencePointsName) tsmiCreateResultReferencePoint_Click(null, null);
                else if (nodeName == _modelTree.ResultCoordinateSystemsName) tsmiCreateResultCoordinateSystem_Click(null, null);
                else if (nodeName == _modelTree.ResultFieldOutputsName) tsmiCreateResultFieldOutput_Click(null, null);
                else if (nodeName == _modelTree.ResultHistoryOutputsName) tsmiCreateResultHistoryOutput_Click(null, null);
            }
        }
        private void ModelTree_Edit(NamedClass namedClass, string stepName)
        {
            // Geometry
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                if (namedClass is GeometryPart) EditGeometryPart(namedClass.Name);
                else if (namedClass is MeshSetupItem) EditMeshSetupItem(namedClass.Name);
            }
            // Model
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                if (namedClass is EmptyNamedClass) // empty named class is used to transfer the name only
                {
                    if (namedClass.Name == typeof(FeModel).ToString()) tsmiEditModel_Click(null, null);
                }
                else if (namedClass is MeshPart) EditModelPart(namedClass.Name);
                else if (namedClass is FeNodeSet) EditNodeSet(namedClass.Name);
                else if (namedClass is FeElementSet) EditElementSet(namedClass.Name);
                else if (namedClass is FeSurface) EditSurface(namedClass.Name);
                else if (namedClass is FeReferencePoint) EditModelReferencePoint(namedClass.Name);
                else if (namedClass is CoordinateSystem) EditModelCoordinateSystem(namedClass.Name);
                else if (namedClass is Material) EditMaterial(namedClass.Name);
                else if (namedClass is Section) EditSection(namedClass.Name);
                else if (namedClass is CaeModel.Constraint) EditConstraint(namedClass.Name);
                else if (namedClass is SurfaceInteraction) EditSurfaceInteraction(namedClass.Name);
                else if (namedClass is ContactPair) EditContactPair(namedClass.Name);
                else if (namedClass is Amplitude) EditAmplitude(namedClass.Name);
                else if (namedClass is InitialCondition) EditInitialCondition(namedClass.Name);
                else if (namedClass is Step) EditStep(namedClass.Name);
                else if (namedClass is HistoryOutput) EditHistoryOutput(stepName, namedClass.Name);
                else if (namedClass is FieldOutput) EditFieldOutput(stepName, namedClass.Name);
                else if (namedClass is BoundaryCondition) EditBoundaryCondition(stepName, namedClass.Name);
                else if (namedClass is Load) EditLoad(stepName, namedClass.Name);
                else if (namedClass is DefinedField) EditDefinedField(stepName, namedClass.Name);
                else if (namedClass is AnalysisJob) EditAnalysis(namedClass.Name);
            }
            // Results
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                if (namedClass is ResultPart || namedClass is GeometryPart) EditResultPart(namedClass.Name);
                else if (namedClass is FeReferencePoint) EditResultReferencePoint(namedClass.Name);
                else if (namedClass is CoordinateSystem) EditResultCoordinateSystem(namedClass.Name);
                else if (namedClass is ResultFieldOutput rfo) EditResultFieldOutput(rfo.Name);
                else if (namedClass is ResultHistoryOutput rho) EditResultHistoryOutput(rho.Name);
                else if (namedClass is HistoryResultData hd) ViewResultHistoryOutputData(hd);
                else if (namedClass is FieldData) ShowLegendSettings();
            }
        }
        private void ModelTree_Query()
        {
            tsmiQuery_Click(null, null);
        }
        private void ModelTree_DuplicateEvent(NamedClass[] items, string[] stepNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                ApplyActionOnItems<MeshSetupItem>(items, DuplicateMeshSetupItems);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<FeNodeSet>(items, DuplicateNodeSets);
                ApplyActionOnItems<FeElementSet>(items, DuplicateElementSets);
                ApplyActionOnItems<FeSurface>(items, DuplicateSurfaces);
                ApplyActionOnItems<FeReferencePoint>(items, DuplicateModelReferencePoints);
                ApplyActionOnItems<CoordinateSystem>(items, DuplicateModelCoordinateSystems);
                //
                ApplyActionOnItems<Material>(items, DuplicateMaterials);
                ApplyActionOnItems<Section>(items, DuplicateSections);
                ApplyActionOnItems<CaeModel.Constraint>(items, DuplicateConstraints);
                ApplyActionOnItems<SurfaceInteraction>(items, DuplicateSurfaceInteractions);
                ApplyActionOnItems<ContactPair>(items, DuplicateContactPairs);
                ApplyActionOnItems<Amplitude>(items, DuplicateAmplitudes);
                ApplyActionOnItems<InitialCondition>(items, DuplicateInitialConditions);
                //
                ApplyActionOnItems<Step>(items, DuplicateSteps);
                ApplyActionOnItemsInStep<HistoryOutput>(items, stepNames, DuplicateHistoryOutputs);
                ApplyActionOnItemsInStep<FieldOutput>(items, stepNames, DuplicateFieldOutputs);
                ApplyActionOnItemsInStep<BoundaryCondition>(items, stepNames, DuplicateBoundaryConditions);
                ApplyActionOnItemsInStep<Load>(items, stepNames, DuplicateLoads);
                ApplyActionOnItemsInStep<DefinedField>(items, stepNames, DuplicateDefinedFields);
                //
                ApplyActionOnItems<AnalysisJob>(items, DuplicateAnalyses);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                ApplyActionOnItems<FeReferencePoint>(items, DuplicateResultReferencePoints);
                ApplyActionOnItems<CoordinateSystem>(items, DuplicateResultCoordinateSystems);
            }
        }
        private void ModelTree_PropagateEvent(NamedClass[] items, string[] stepNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItemsInStep<HistoryOutput>(items, stepNames, PropagateHistoryOutput);
                ApplyActionOnItemsInStep<FieldOutput>(items, stepNames, PropagateFieldOutput);
                ApplyActionOnItemsInStep<BoundaryCondition>(items, stepNames, PropagateBoundaryCondition);
                ApplyActionOnItemsInStep<Load>(items, stepNames, PropagateLoad);
                ApplyActionOnItemsInStep<DefinedField>(items, stepNames, PropagateDefinedField);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
            }
        }
        private void ModelTree_PreviewEvent(NamedClass[] items, string[] stepNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<InitialCondition>(items, PreviewInitialConditions);
                ApplyActionOnItemsInStep<Load>(items, stepNames, PreviewLoad);
                ApplyActionOnItemsInStep<DefinedField>(items, stepNames, PreviewDefinedField);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
            }
        }
        //
        private void ModelTree_SwapMasterSlave(NamedClass[] items)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            { }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<CaeModel.Constraint>(items, SwapMasterSlaveConstraints);
                ApplyActionOnItems<ContactPair>(items, SwapMasterSlaveContactPairs);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            { }
        }
        private void ModelTree_MergeByMasterSlave(NamedClass[] items)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            { }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<CaeModel.Constraint>(items, MergeByMasterSlaveConstraints);
                ApplyActionOnItems<ContactPair>(items, MergeByMasterSlaveContactPairs);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            { }
        }
        //
        private void ModelTree_HideShowEvent(NamedClass[] items, HideShowOperation operation, string[] stepNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                HideShowItems<GeometryPart>(items, operation, HideGeometryParts, ShowGeometryParts, ShowOnlyGeometryParts);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                HideShowItems<MeshPart>(items, operation, HideModelParts, ShowModelParts, ShowOnlyModelParts);
                HideShowItems<FeReferencePoint>(items, operation, HideModelReferencePoints,
                                                ShowModelReferencePoints, ShowOnlyModelReferencePoints);
                HideShowItems<CoordinateSystem>(items, operation, HideModelCoordinateSystems,
                                                ShowModelCoordinateSystems, ShowOnlyModelCoordinateSystems);
                HideShowItems<CaeModel.Constraint>(items, operation, HideConstraints, ShowConstraints, ShowOnlyConstraints);
                HideShowItems<ContactPair>(items, operation, HideContactPairs, ShowContactPairs, ShowOnlyContactPairs);
                HideShowItems<InitialCondition>(items, operation, HideInitialConditions, ShowInitialConditions,
                                                ShowOnlyInitialConditions);
                HideShowStepItems<BoundaryCondition>(items, operation, stepNames, HideBoundaryConditions,
                                                     ShowBoundaryConditions, ShowOnlyBoundaryConditions);
                HideShowStepItems<Load>(items, operation, stepNames, HideLoads, ShowLoads, ShowOnlyLoads);
                HideShowStepItems<DefinedField>(items, operation, stepNames, HideDefinedFields,
                                                ShowDefinedFields, ShowOnlyDefinedFields);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                HideShowItems<ResultPart>(items, operation, HideResultParts, ShowResultParts, ShowOnlyResultParts);
                HideShowItems<GeometryPart>(items, operation, HideResultParts, ShowResultParts, ShowOnlyResultParts);
                HideShowItems<FeReferencePoint>(items, operation, HideResultReferencePoints,
                                                ShowResultReferencePoints, ShowOnlyResultReferencePoints);
                HideShowItems<CoordinateSystem>(items, operation, HideResultCoordinateSystems,
                                                ShowResultCoordinateSystems, ShowOnlyResultCoordinateSystems);
            }
        }
        private void ModelTree_SetPartColorEvent(string[] partNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) SetColorForGeometryParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) SetColorForModelParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results) SetColorForResultParts(partNames);
        }
        private void ModelTree_ResetPartColorEvent(string[] partNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) ResetColorForGeometryParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) ResetColorForModelParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results) ResetColorForResultParts(partNames);
        }
        private void ModelTree_SetTransparencyEvent(string[] partNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) SetTransparencyForGeometryParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) SetTransparencyForModelParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results) SetTransparencyForResultParts(partNames);
        }
        private void ModelTree_ColorContoursVisibilityEvent(NamedClass[] items, bool colorContours)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                names.Add(items[i].Name);
            }
            //
            if (names.Count > 0)
            {
                if (colorContours) ColorContoursOnResultPart(names.ToArray());
                else ColorContoursOffResultPart(names.ToArray());
            }
        }
        //
        private void ModelTree_Delete(NamedClass[] items, string[] parentNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                ApplyActionOnItems<MeshSetupItem>(items, DeleteMeshSetupItems);
                // At last delete the parts
                ApplyActionOnItems<GeometryPart>(items, DeleteGeometryParts);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<FeNodeSet>(items, DeleteNodeSets);
                ApplyActionOnItems<FeElementSet>(items, DeleteElementSets);
                ApplyActionOnItems<FeSurface>(items, DeleteSurfaces);
                ApplyActionOnItems<FeReferencePoint>(items, DeleteModelReferencePoints);
                ApplyActionOnItems<CoordinateSystem>(items, DeleteModelCoordinateSystems);
                ApplyActionOnItems<Material>(items, DeleteMaterials);
                ApplyActionOnItems<Section>(items, DeleteSections);
                ApplyActionOnItems<CaeModel.Constraint>(items, DeleteConstraints);
                ApplyActionOnItems<SurfaceInteraction>(items, DeleteSurfaceInteractions);
                ApplyActionOnItems<ContactPair>(items, DeleteContactPairs);
                ApplyActionOnItems<Amplitude>(items, DeleteAmplitudes);
                ApplyActionOnItems<InitialCondition>(items, DeleteInitialConditions);
                // First delete step items and then steps
                ApplyActionOnItemsInStep<HistoryOutput>(items, parentNames, DeleteHistoryOutputs);
                ApplyActionOnItemsInStep<FieldOutput>(items, parentNames, DeleteFieldOutputs);
                ApplyActionOnItemsInStep<BoundaryCondition>(items, parentNames, DeleteBoundaryConditions);
                ApplyActionOnItemsInStep<Load>(items, parentNames, DeleteLoads);
                ApplyActionOnItemsInStep<DefinedField>(items, parentNames, DeleteDefinedFields);
                ApplyActionOnItems<Step>(items, DeleteSteps);
                //
                ApplyActionOnItems<AnalysisJob>(items, DeleteAnalyses);
                // At last delete the parts
                ApplyActionOnItems<MeshPart>(items, DeleteModelParts);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                ApplyActionOnItems<ResultPart>(items, DeleteResultParts);
                ApplyActionOnItems<GeometryPart>(items, DeleteResultParts);
                ApplyActionOnItems<FeReferencePoint>(items, DeleteResultReferencePoints);
                ApplyActionOnItems<CoordinateSystem>(items, DeleteResultCoordinateSystems);
                // First delete components and then field outputs
                ApplyActionOnItemsInStep<FieldData>(items, parentNames, DeleteResultFieldOutputComponents);
                ApplyActionOnItems<Field>(items, DeleteResultFieldOutputs);
                ApplyActionOnItems<ResultFieldOutput>(items, DeleteResultFieldOutputs);
                //
                DeleteResultHistoryComponents(items);
                ApplyActionOnItemsInStep<HistoryResultField>(items, parentNames, DeleteResultHistoryFields);
                ApplyActionOnItems<HistoryResultSet>(items, DeleteResultHistoryOutputs);
                ApplyActionOnItems<ResultHistoryOutput>(items, DeleteResultHistoryOutputs);
            }
        }
        private void ModelTree_ActivateDeactivateEvent(NamedClass[] items, bool activate, string[] stepNames)
        {
            _controller.ActivateDeactivateMultipleCommand(items, activate, stepNames);
        }
        private void ModelTree_FieldDataSelectEvent(string[] obj)
        {
            try
            {
                SetFieldData(obj[0], obj[1], GetCurrentFieldOutputStepId(), GetCurrentFieldOutputStepIncrementId());
            }
            catch { }
        }
        //                                                                                                                          
        private void HideShowItems<T>(NamedClass[] items, HideShowOperation operation, Action<string[]> Hide,
                                      Action<string[]> Show, Action<string[]> ShowOnly)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T) names.Add(items[i].Name);
            }
            if (names.Count > 0)
            {
                if (operation == HideShowOperation.Hide) Hide(names.ToArray());
                else if (operation == HideShowOperation.Show) Show(names.ToArray());
                else if (operation == HideShowOperation.ShowOnly) ShowOnly(names.ToArray());
                else throw new NotSupportedException();
            }
        }
        private void HideShowStepItems<T>(NamedClass[] items, HideShowOperation operation, string[] stepNames,
                                          Action<string, string[]> Hide,
                                          Action<string, string[]> Show,
                                          Action<string, string[]> ShowOnly)
        {
            Dictionary<string, List<string>> stepItems = new Dictionary<string, List<string>>();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T)
                {
                    if (stepItems.ContainsKey(stepNames[i])) stepItems[stepNames[i]].Add(items[i].Name);
                    else stepItems.Add(stepNames[i], new List<string>() { items[i].Name });
                }
            }
            if (stepItems.Count > 0)
            {
                foreach (var entry in stepItems)
                {
                    if (operation == HideShowOperation.Hide) Hide(entry.Key, entry.Value.ToArray());
                    else if (operation == HideShowOperation.Show) Show(entry.Key, entry.Value.ToArray());
                    else if (operation == HideShowOperation.ShowOnly) ShowOnly(entry.Key, entry.Value.ToArray());
                    else throw new NotSupportedException();
                }
            }
        }
        private void ApplyActionOnItems<T>(NamedClass[] items, Action<string[]> Action)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T) names.Add(items[i].Name);
            }
            if (names.Count > 0) Action(names.ToArray());
        }
        private void ApplyActionOnItemsInStep<T>(NamedClass[] items, string[] steps, Action<string, string> Action)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T) Action(steps[i], items[i].Name);
            }
        }
        private void ApplyActionOnItemsInStep<T>(NamedClass[] items, string[] parentNames, Action<string, string[]> Action)
        {
            List<string> itemList;
            Dictionary<string, List<string>> parentItems = new Dictionary<string, List<string>>();
            //
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T)
                {
                    if (parentItems.TryGetValue(parentNames[i], out itemList)) itemList.Add(items[i].Name);
                    else parentItems.Add(parentNames[i], new List<string>() { items[i].Name });
                }
            }
            if (parentItems.Count > 0)
            {
                foreach (var entry in parentItems)
                {
                    Action(entry.Key, entry.Value.ToArray());
                }
            }
        }
        //                                                                                                                          

        #endregion #################################################################################################################


        // Menus                                                                                                                    
        private void SetMenuAndToolStripVisibility()
        {
            Console.WriteLine("FrmMain::SetMenuAndToolStripVisibility");//my code
            //tsmiFile.Text = "ファイル" ;

            if (IsStateRegeneratingOrUndoing()) return;
            //
            InvokeIfRequired(() =>
            {
                //                      Disable                                                         
                // Main menu
                foreach (ToolStripMenuItem item in menuStripMain.Items) item.Enabled = false;
                tsmiFile.Enabled = true;
                tsmiTools.Enabled = true;
                tsmiHelp.Enabled = true;
                tsmiLanguage.Enabled = true; //my code
                // File menu
                foreach (ToolStripItem item in tsmiFile.DropDownItems) item.Enabled = false;
                // Tools menu
                tsmiParameters.Enabled = false;
                tsmiQuery.Enabled = false;
                tsmiFind.Enabled = false;
                // Toolbar File
                tsbImport.Enabled = false;
                tsbSave.Enabled = false;
                // Toolbar View
                tsViews.DisableMouseButtons = true;
                // Toolbar Symbols
                tslSymbols.Enabled = false;
                tscbSymbols.Enabled = false;
                UpdateSymbolsList();
                // Toolbar Results
                tsResultDeformation.Enabled = false;
                tsResults.Enabled = false;
                // Vtk
                bool vtkVisible = false;
                // Tree
                SetStateWorking(Globals.RenderingText);
                //                      Enable                                                          
                if (_controller.ModelInitialized || _controller.ResultsInitialized)
                {
                    // Main menu
                    tsmiEdit.Enabled = true;
                    // File menu
                    foreach (ToolStripItem item in tsmiFile.DropDownItems) item.Enabled = true;
                    tsmiImportFile.Enabled = _controller.ModelInitialized;
                    // Tools menu
                    tsmiParameters.Enabled = true;
                    tsmiQuery.Enabled = true;
                    tsmiFind.Enabled = true;
                    // Toolbar File
                    tsbImport.Enabled = _controller.ModelInitialized;
                    tsbSave.Enabled = true;
                }
                else
                {
                    // File menu
                    tsmiNew.Enabled = true;
                    tsmiOpen.Enabled = true;
                    tsmiOpenRecent.Enabled = true;
                    tsmiRunHistoryFile.Enabled = true;
                    tsmiExit.Enabled = true;
                }
                //
                bool setGeometryView = _controller.CurrentView == ViewGeometryModelResults.Geometry;
                bool setModelView = _controller.CurrentView == ViewGeometryModelResults.Model;
                bool setResultsView = _controller.CurrentView == ViewGeometryModelResults.Results;
                bool setEmptyView = (setGeometryView && !_controller.ModelInitialized) ||
                                    (setModelView && !_controller.ModelInitialized) ||
                                    (setResultsView && !_controller.ResultsInitialized);

                // Only for individual views !!!
                if (setEmptyView) { }
                else if (setGeometryView)
                {
                    // Main menu
                    tsmiView.Enabled = true;
                    tsmiGeometry.Enabled = true;
                    tsmiMesh.Enabled = true;
                    // Toolbar View
                    tsViews.DisableMouseButtons = false;
                    // Vtk
                    vtkVisible = true;
                }
                else if (setModelView)
                {
                    // Main menu
                    tsmiView.Enabled = true;
                    tsmiModel.Enabled = true;
                    tsmiProperty.Enabled = true;
                    tsmiInteraction.Enabled = true;
                    tsmiInitialCondition.Enabled = true;
                    tsmiAmplitude.Enabled = true;
                    tsmiStepMenu.Enabled = true;
                    tsmiBC.Enabled = true;
                    tsmiLoad.Enabled = true;
                    tsmiAnalysis.Enabled = true;
                    // Toolbar View
                    tsViews.DisableMouseButtons = false;
                    // Toolbar Symbols
                    tslSymbols.Enabled = true;
                    tscbSymbols.Enabled = true;
                    // Vtk
                    vtkVisible = true;
                }
                else if (setResultsView)
                {
                    // Main menu
                    tsmiView.Enabled = true;
                    tsmiResults.Enabled = true;
                    // Toolbar View
                    tsViews.DisableMouseButtons = false;
                    // Toolbar Symbols
                    tslSymbols.Enabled = true;
                    tscbSymbols.Enabled = true;
                    // Toolbar Results
                    tsResultDeformation.Enabled = true;
                    tsResults.Enabled = true;
                    // Vtk
                    vtkVisible = true;
                }
                // Tree
                SetStateReady(Globals.RenderingText);
                //                      Buttons                                                         
                tsbSectionView.Checked = _controller.IsSectionViewActive();
                tsbExplodedView.Checked = _controller.IsExplodedViewActive();
                tsbTransformation.Checked = _controller.AreTransformationsActive();
                //                      Icons                                                           
                UpdateResultsTypeIconStates();
                //
                UpdateComplexControlStates();
                //
                _vtk.Visible = vtkVisible;
            });
        }
        private void SetMenuAndToolStripVisibilityByItemForm(bool menusActive)
        {
            _modelTree.DisableMouse = !menusActive;
            menuStripMain.DisableMouseButtons = !menusActive;
            tsFile.DisableMouseButtons = !menusActive;
            tsSymbols.Enabled = menusActive; // changing the symbols clears the selection - unwanted during selection
        }
        private void SetMenuAndToolStripVisibilityBySetState(bool menusActive)
        {
            // Do not use Enable here - use DisableMouseButtons
            _modelTree.DisableMouse = !menusActive;
            menuStripMain.DisableMouseButtons = !menusActive;
            tsFile.DisableMouseButtons = !menusActive;
            tsViews.DisableMouseButtons = !menusActive;
            tsResultDeformation.DisableMouseButtons = !menusActive;
            tsResults.DisableMouseButtons = !menusActive;
        }
        private void SetMenuAndToolStripVisibilityByAnimation(bool menusActive)
        {
            _modelTree.DisableMouse = !menusActive;
            menuStripMain.DisableMouseButtons = !menusActive;
            // DisableMouseButtons does not work for combo boxes so use .Enabled
            tsFile.Enabled = menusActive;
            tsSymbols.Enabled = menusActive;
            tsResultDeformation.Enabled = menusActive;
            //
            tsbAnimate.Enabled = menusActive;   // first - removes the selection
            tsResults.Enabled = menusActive;    // second
            // Individual buttons
            tsbSectionView.Enabled = menusActive;
            tsbExplodedView.Enabled = menusActive;
            tsbQuery.Enabled = menusActive;
            tsbRemoveAnnotations.Enabled = menusActive;
            tsbShowAllParts.Enabled = menusActive;
            tsbHideAllParts.Enabled = menusActive;
            tsbInvertVisibleParts.Enabled = menusActive;
            //
            if (menusActive) UpdateComplexControlStates();
            
        }
        private void PushMenuStates()
        {
            List<bool> states = new List<bool>();
            states.Add(_modelTree.DisableMouse);                    // SetMenuAndToolStripVisibilityBySetState
            states.Add(menuStripMain.DisableMouseButtons);          // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsFile.DisableMouseButtons);                 // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsViews.DisableMouseButtons);                // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsResultDeformation.DisableMouseButtons);    // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsResults.DisableMouseButtons);              // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsSymbols.Enabled);                          // SetMenuAndToolStripVisibilityByItemForm
            //
            _prevMenuStates.Push(states.ToArray());
        }
        private void PopMenuStates()
        {
            if (_prevMenuStates != null && _prevMenuStates.Count > 0)
            {
                int n = 0;
                bool[] states = _prevMenuStates.Pop();
                //
                _modelTree.DisableMouse = states[n++];
                menuStripMain.DisableMouseButtons = states[n++];
                tsFile.DisableMouseButtons = states[n++];
                tsViews.DisableMouseButtons = states[n++];
                tsResultDeformation.DisableMouseButtons = states[n++];
                tsResults.DisableMouseButtons = states[n++];
                tsSymbols.Enabled = states[n++];
            }
            //else throw new NotSupportedException();
        }

        #region File menu ##########################################################################################################
        internal void tsmiNew_Click(object sender, EventArgs e)
        {
            New(ModelSpaceEnum.Undefined, UnitSystemType.Undefined);
        }
        private async void tsmiOpen_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = GetFileOpenFilter();
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string parameters = Globals.FromFileOpenMenu;
                        if (CheckBeforeOpen(openFileDialog.FileName))
                            await OpenAsync(openFileDialog.FileName, _controller.OpenFileCommand, true, null, parameters);
                        //
                        string extension = Path.GetExtension(openFileDialog.FileName).ToLower();
                        if (extension == ".frd") RunHistoryPostprocessing();    // must be here
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
                _controller.ModelChanged = false;   // hide message box
                tsmiNew_Click(null, null);
            }
        }
        private void tsmiRunHistoryFile_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "PrePoMax history|*.pmh";
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog() == DialogResult.OK) RunHistoryFile(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void tsmiImportFile_Click(object sender, EventArgs e)
        {
            ImportFile(false);
        }        
        internal async void tsmiSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized && !_controller.ResultsInitialized)
                    throw new CaeException("There is no model or results to save.");
                //
                SetStateWorking(Globals.SavingText);
                await Task.Run(() => _controller.Save());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.SavingText);
            }
        }
        private async void tsmiSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized && !_controller.ResultsInitialized)
                    throw new CaeException("There is no model or results to save.");
                //
                SetStateWorking(Globals.SavingAsText);
                await Task.Run(() => _controller.SaveAs());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.SavingAsText);
            }
        }
        //
        private void tsmiExportToStep_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Geometry;
                //
                if (_controller.Model.Geometry != null && _controller.Model.Geometry.Parts != null)
                {
                    SelectMultipleEntities("Parts", _controller.GetCADGeometryParts(), SaveCADPartsAsStep);
                }
                else throw new CaeException("No geometry to export.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportToBrep_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Geometry;
                //
                if (_controller.Model.Geometry != null && _controller.Model.Geometry.Parts != null)
                {
                    SelectMultipleEntities("Parts", _controller.GetCADGeometryParts(), SaveCADPartsAsBrep);
                }
                else throw new CaeException("No geometry to export.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportToStereolithography_Click(object sender, EventArgs e)
        {
            try
            {
                ViewGeometryModelResults currentView = GetCurrentView();
                if (currentView == ViewGeometryModelResults.Geometry)
                {
                    if (_controller.Model.Geometry != null && _controller.Model.Geometry.Parts != null)
                    {
                        SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SavePartsAsStl);
                    }
                    else throw new CaeException("No geometry parts to export.");
                }
                else if (currentView == ViewGeometryModelResults.Model)
                {
                    if (_controller.Model.Mesh != null && _controller.Model.Mesh.Parts != null)
                    {
                        SelectMultipleEntities("Parts", _controller.GetModelParts(), SavePartsAsStl);
                    }
                    else throw new CaeException("No mesh parts to export.");
                }
                else MessageBoxes.ShowError("Deformed mesh can only be exported while results are drawn.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportToCalculix_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Model;
                //
                if (CheckValidity())
                {
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "Calculix files | *.inp";
                        if (_controller.OpenedFileName != null)
                            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".inp";
                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            // the filter adds the extension to the file name
                            SetStateWorking(Globals.ExportingText);
                            _controller.ExportToCalculix(saveFileDialog.FileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private void tsmiExportToAbaqus_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Model;
                //
                if (CheckValidity())
                {
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "Abaqus files | *.inp";
                        if (_controller.OpenedFileName != null)
                            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".inp";
                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            // the filter adds the extension to the file name
                            SetStateWorking(Globals.ExportingText);
                            //_controller.ExportToCalculix(saveFileDialog.FileName);
                            _controller.ExportToAbaqus(saveFileDialog.FileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private void tsmiExportToGmshMesh_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Model;
                //
                if (_controller.Model.Mesh != null && _controller.Model.Mesh.Parts != null)
                {
                    SavePartsAsGmshMesh();
                }
                else throw new CaeException("No mesh to export.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportToMmgMesh_Click(object sender, EventArgs e)
        {
            try
            {
                //_controller.CurrentView = ViewGeometryModelResults.Geometry;
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                {
                    if (_controller.Model.Geometry != null && _controller.Model.Geometry.Parts != null)
                    {
                        SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SavePartsAsMmgMesh);
                    }
                    else throw new CaeException("No geometry to export.");
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    if (_controller.Model.Mesh != null && _controller.Model.Mesh.Parts != null)
                    {
                        SelectMultipleEntities("Parts", _controller.GetModelParts(), SavePartsAsMmgMesh);
                    }
                    else throw new CaeException("No mesh to export.");
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportToDeformedInp_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Results;
                //
                if (_controller.CurrentResult.Mesh != null && _controller.CurrentResult.Mesh.Parts != null)
                {
                    SelectMultipleEntities("Parts", _controller.GetResultParts(), SaveDeformedPartsAsInp);
                }
                else MessageBoxes.ShowError("There is no mesh to export.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }

        }
        private void tsmiExportToDeformedStl_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Results;
                //
                if (_controller.CurrentResult.Mesh != null && _controller.CurrentResult.Mesh.Parts != null)
                {
                    SelectMultipleEntities("Parts", _controller.GetResultParts(), SavePartsAsStl);
                }
                else MessageBoxes.ShowError("There is no mesh to export.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private bool New(ModelSpaceEnum modelSpace, UnitSystemType unitSystemType)
        {
            try
            {
                TopMost = true;    // fix the problem with Solidworks opened in the background
                //
                if ((_controller.ModelChanged || _controller.ModelInitialized || _controller.ResultsInitialized) &&
                    MessageBoxes.ShowWarningQuestionOKCancel("OK to close the current model?") != DialogResult.OK) return false;
                //
                _controller.DeInitialize();
                SetMenuAndToolStripVisibility();
                //
                bool update = false;
                // The model space and the unit system are undefined
                if (modelSpace == ModelSpaceEnum.Undefined || unitSystemType == UnitSystemType.Undefined)
                {
                    if (SelectNewModelProperties(true))
                    {
                        _controller.New();
                        SetNewModelProperties();
                        update = true;
                    }
                }
                else
                {
                    _controller.New();
                    _controller.SetNewModelPropertiesCommand(modelSpace, unitSystemType);
                    update = true;
                }
                //
                if (update)
                {
                    SetMenuAndToolStripVisibility();
                    //
                    _controller.ModelChanged = false; // must be here since adding a unit system changes the model
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                TopMost = false;
            }
            return true;
        }
        private bool CheckBeforeOpen(string fileName)
        {
            if (!File.Exists(fileName)) return false;
            //
            if (_controller.ModelChanged)
            {
                string extension = Path.GetExtension(fileName).ToLower();
                if (extension == ".pmx")
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to close the current model?") != DialogResult.OK)
                        return false;
                }
                else if ((extension == ".frd" || extension == ".foam") && _controller.AllResults.ContainsResult(fileName))
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to reopen the existing results?") != DialogResult.OK)
                        return false;
                }
            }
            return true;
        }
        private async Task OpenAsync(string fileName, Action<string, string> ActionOnFile, bool resetCamera = true,
                                     Action callback = null, string parameters = null)
        {
            bool stateSet = false;
            string stateText = Globals.OpeningText;
            try
            {
                string extension = Path.GetExtension(fileName).ToLower();
                if (extension == ".pmh") stateText = Globals.RegeneratingText;
                //
                if (SetStateWorking(stateText) || IsStateRegeneratingOrUndoing())
                {
                    stateSet = true;
                    await Task.Run(() => Open(fileName, ActionOnFile, resetCamera, parameters));
                    callback?.Invoke(); // close monitor window
                }
                else MessageBoxes.ShowWarning("Another task is already running.");
                // If the model space or the unit system are undefined
                if (_controller.ModelInitialized) IfNeededSelectAndSetNewModelProperties();
                if (_controller.ResultsInitialized) SelectResultsUnitSystem();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                if (stateSet) SetStateReady(stateText);
            }
        }
        public void Open(string fileName, Action<string, string> ActionOnFile, bool resetCamera = true, string parameters = null)
        {
            ActionOnFile(fileName, parameters);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds(true);
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) _controller.DrawGeometry(resetCamera);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) _controller.DrawModel(resetCamera);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                // Set the representation which also calls Draw
                _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
                //
                if (resetCamera) tsmiFrontView_Click(null, null);
            }
            else throw new NotSupportedException();
            //
            SetMenuAndToolStripVisibility();
        }
        public async void RunHistoryFile(string fileName)
        {
            try
            {
                CloseAllForms();
                Application.DoEvents();
                SetStateWorking(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = false;
                await Task.Run(() => _controller.RunHistoryFile(fileName));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = true;
                RegenerateTree();
                //
                SetMenuAndToolStripVisibility();
                // During regeneration the tree is empty
                if (_controller.CurrentResult != null) SelectFirstComponentOfFirstFieldOutput();
                //
                SetZoomToFit(true);
            }
        }
        private async void ImportFile(bool onlyMaterials)
        {
            Console.WriteLine("ImportFile:"); //my code
            try
            {
                if (!_controller.ModelInitialized)
                    throw new CaeException("There is no model to import into. First create a new model.");
                // If the model space or the unit system are undefined
                IfNeededSelectAndSetNewModelProperties();
                //
                string filter = GetFileImportFilter(onlyMaterials);
                string[] files = GetFileNamesToImport(filter);
                //
                if (files != null && files.Length > 0)
                {
                    _controller.ClearErrors();
                    //
                    SetStateWorking(Globals.ImportingText);
                    foreach (var file in files)
                    {
                        await _controller.ImportFileAsync(file, onlyMaterials);
                    }
                    SetFrontBackView(true, true);   // animate must be true in order for the scale bar to work correctly
                    //
                    int numErrors = _controller.GetNumberOfErrors();
                    if (numErrors > 0)
                    {
                        _controller.OutputErrors();
                        string message = "There were errors while importing the file/files.";
                        WriteDataToOutput(message);
                        AutoClosingMessageBox.ShowError(message, 3000);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ImportingText);
            }
        }
        private async void SaveCADPartsAsStep(string[] partNames)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Step files | *.stp";
                    if (_controller.OpenedFileName != null)
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".stp";
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // The filter adds the extension to the file name
                        SetStateWorking(Globals.ExportingText);
                        //
                        await Task.Run(() => _controller.ExportCADGeometryPartsAsStep(partNames, saveFileDialog.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private async void SaveCADPartsAsBrep(string[] partNames)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Brep files | *.brep";
                    if (_controller.OpenedFileName != null)
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".brep";
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // The filter adds the extension to the file name
                        SetStateWorking(Globals.ExportingText);
                        //
                        await Task.Run(() => _controller.ExportCADGeometryPartsAsBrep(partNames, saveFileDialog.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private async void SavePartsAsStl(string[] partNames)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Stereolithography files | *.stl";
                    if (_controller.OpenedFileName != null)
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".stl";
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // The filter adds the extension to the file name
                        SetStateWorking(Globals.ExportingText);
                        //
                        await Task.Run(() => _controller.ExportToStl(partNames, saveFileDialog.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private async void SavePartsAsGmshMesh()
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Gmsh mesh files | *.msh";
                    if (_controller.OpenedFileName != null)
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".msh";
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // The filter adds the extension to the file name
                        SetStateWorking(Globals.ExportingText);
                        //
                        await Task.Run(() => _controller.ExportGeometryPartsAsGmshMesh(saveFileDialog.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private async void SavePartsAsMmgMesh(string[] partNames)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Mmg files | *.mesh";
                    if (_controller.OpenedFileName != null)
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".mesh";
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // The filter adds the extension to the file name
                        SetStateWorking(Globals.ExportingText);
                        //
                        await Task.Run(() => _controller.ExportPartsAsMmgMesh(partNames, saveFileDialog.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private async void SaveDeformedPartsAsInp(string[] partNames)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Calculix files | *.inp";
                    if (_controller.OpenedFileName != null)
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".inp";
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // The filter adds the extension to the file name
                        SetStateWorking(Globals.ExportingText);
                        //
                        await Task.Run(() => _controller.ExportDeformedPartsToCalculix(partNames, saveFileDialog.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        //
        private void tsmiCloseCurrentResult_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.AllResults.Count <= 1) tsmiCloseAllResults_Click(null, null);
                else
                {
                    _controller.RemoveCurrentResult();
                    SetResultNames();
                    if (tscbResultNames.SelectedItem != null) SetCurrentResults(tscbResultNames.SelectedItem.ToString());
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCloseAllResults_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.ClearResults(); // calls this.ClearResults();
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Results) Clear3D();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //Recent
        public void UpdateRecentFilesThreadSafe(string[] fileNames)
        {
            InvokeIfRequired(UpdateRecentFiles, fileNames);
        }
        public void UpdateRecentFiles(string[] fileNames)
        {
            try
            {
                if (fileNames != null)
                {
                    tsmiOpenRecent.DropDownItems.Clear();
                    //
                    ToolStripMenuItem menuItem;
                    foreach (var fileName in fileNames)
                    {
                        menuItem = new ToolStripMenuItem(fileName.Replace("&", "&&"));
                        menuItem.Name = fileName;
                        menuItem.Click += tsmiRecentFile_Click;
                        tsmiOpenRecent.DropDownItems.Add(menuItem);
                    }
                    if (fileNames.Length > 0)
                    {
                        ToolStripSeparator separator = new ToolStripSeparator();
                        tsmiOpenRecent.DropDownItems.Add(separator);
                    }
                    menuItem = new ToolStripMenuItem("Clear Recent Files");
                    menuItem.Click += tsmiClearRecentFiles_Click;
                    tsmiOpenRecent.DropDownItems.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRecentFile_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = ((ToolStripMenuItem)sender).Name;
                if (CheckBeforeOpen(fileName)) OpenAsync(fileName, _controller.Open);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiClearRecentFiles_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.ClearRecentFiles();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        #endregion  ################################################################################################################

        #region Edit menu ##########################################################################################################
        private async void tsmiUndo_Click(object sender, EventArgs e)
        {
            try
            {
                SetFormLocation(_frmRegenerate);
                if (_frmRegenerate.ShowDialog() == DialogResult.OK)
                {
                    SetStateWorking(Globals.UndoingText);
                    _modelTree.ScreenUpdating = false;
                    //
                    await Task.Run(() => _controller.UndoHistory(_frmRegenerate.RegenerateType));
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.UndoingText);
                _modelTree.ScreenUpdating = true;
                _modelTree.RegenerateTree(_controller.Model, _controller.Jobs, _controller.CurrentResult);
                //
                SetMenuAndToolStripVisibility();
                //
                SetZoomToFit(true);
            }
        }
        private void tsmiRedo_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.RedoHistory();
                //
                SetMenuAndToolStripVisibility();
                //
                SetZoomToFit(true);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditHistory_Click(object sender, EventArgs e)
        {
            try
            {
                CloseAllForms();
                //
                _frmEditCommands.PrepareForm();
                //
                SetFormLocation(_frmEditCommands);
                if (_frmEditCommands.ShowDialog() ==  DialogResult.OK)
                {
                    _controller.SetCommands(_frmEditCommands.Commands);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void tsmiRegenerateHistory_Click(object sender, EventArgs e)
        {
            SetFormLocation(_frmRegenerate);
            if (_frmRegenerate.ShowDialog() == DialogResult.OK)
            {
                RegenerateHistory(false, false, _frmRegenerate.RegenerateType);
            }
        }
        private void tsmiRegenerateHistoryUsingOtherFiles_Click(object sender, EventArgs e)
        {
            SetFormLocation(_frmRegenerate);
            if (_frmRegenerate.ShowDialog() == DialogResult.OK)
            {
                RegenerateHistory(true, false, _frmRegenerate.RegenerateType);
            }
        }
        private void tsmiRegenerateHistoryWithRemeshing_Click(object sender, EventArgs e)
        {
            SetFormLocation(_frmRegenerate);
            if (_frmRegenerate.ShowDialog() == DialogResult.OK)
            {
                RegenerateHistory(false, true, _frmRegenerate.RegenerateType);
            }
        }
        //
        public async void RegenerateHistory(bool showFileDialog, bool showMeshDialog, RegenerateTypeEnum regenerateType)
        {
            try
            {
                CloseAllForms();
                Clear3D();
                Application.DoEvents();
                SetStateWorking(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = false;
                await Task.Run(() => _controller.RegenerateHistoryCommands(showFileDialog, showMeshDialog, regenerateType));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = true;
                RegenerateTree();
                //
                SetMenuAndToolStripVisibility();
                // During regeneration the tree is empty
                if (_controller.CurrentResult != null) SelectFirstComponentOfFirstFieldOutput(); 
                //
                SetZoomToFit(true);
            }
        }
        public void EnableDisableUndoRedo(string undo, string redo)
        {
            InvokeIfRequired(EnableDisable, undo, redo);
        }
        private void EnableDisable(string undo, string redo)
        {
            if (undo == null)
            {
                tsmiUndo.Text = "Undo";
                tsmiUndo.Enabled = false;
            }
            else
            {
                tsmiUndo.Text = "Undo - " + undo;
                tsmiUndo.Enabled = true;
            }

            if (redo == null)
            {
                tsmiRedo.Text = "Redo";
                tsmiRedo.Enabled = false;
            }
            else
            {
                tsmiRedo.Text = "Redo - " + redo;
                tsmiRedo.Enabled = true;
            }
        }

        #endregion  ################################################################################################################

        #region View menu ##########################################################################################################
        private void tsmiFrontView_Click(object sender, EventArgs e)
        {
            _vtk.SetFrontBackView(true, true);
        }
        private void tsmiBackView_Click(object sender, EventArgs e)
        {
            _vtk.SetFrontBackView(true, false);
        }
        private void tsmiTopView_Click(object sender, EventArgs e)
        {
            _vtk.SetTopBottomView(true, true);
        }
        private void tsmiBottomView_Click(object sender, EventArgs e)
        {
            _vtk.SetTopBottomView(true, false);
        }
        private void tsmiLeftView_Click(object sender, EventArgs e)
        {
            _vtk.SetLeftRightView(true, true);
        }
        private void tsmiRightView_Click(object sender, EventArgs e)
        {
            _vtk.SetLeftRightView(true, false);
        }
        //
        private void tsmiNormalView_Click(object sender, EventArgs e)
        {
            _vtk.SetNormalView(true);
        }
        private void tsmiVerticalView_Click(object sender, EventArgs e)
        {
            _vtk.SetVerticalView(true, true);
        }
        //
        private void tsmiIsometricView_Click(object sender, EventArgs e)
        {
            _vtk.SetIsometricView(true, true);
        }
        private void tsmiZoomToFit_Click(object sender, EventArgs e)
        {
            SetZoomToFit(true);
        }
        //
        private void tsmiShowWireframeEdges_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentEdgesVisibility = vtkControl.vtkEdgesVisibility.Wireframe;
            }
            catch { }
        }
        private void tsmiShowElementEdges_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentEdgesVisibility = vtkControl.vtkEdgesVisibility.ElementEdges;
            }
            catch { }
        }
        private void tsmiShowModelEdges_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentEdgesVisibility = vtkControl.vtkEdgesVisibility.ModelEdges;
            }
            catch { }
        }
        private void tsmiShowNoEdges_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentEdgesVisibility = vtkControl.vtkEdgesVisibility.NoEdges;
            }
            catch { }
        }
        //
        private void tsmiSectionView_Click(object sender, EventArgs e)
        {
            try
            {
                SinglePointDataEditor.ParentForm = _frmSectionView;
                SinglePointDataEditor.Controller = _controller;
                //
                ShowForm(_frmSectionView, tsmiSectionView.Text, null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void TurnSectionViewOnOff()
        {
            try
            {
                if (!_frmSectionView.Visible) _controller.TurnSectionViewOnOff();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExplodePartsText);
            }
        }
        private void tsmiExplodedView_Click(object sender, EventArgs e)
        {
            try
            {
                ExplodedViewParameters parameters = _controller.GetCurrentExplodedViewParameters();
                _frmExplodedView.SetExplodedViewParameters(parameters);
                //
                ShowForm(_frmExplodedView, _frmExplodedView.Text, null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void TurnExplodedViewOnOff(bool animate)
        {
            try
            {
                SetStateWorking(Globals.ExplodePartsText);
                _controller.TurnExplodedViewOnOff(animate);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExplodePartsText);
            }
        }
        
        // Hide/Show
        private void tsmiHideAllParts_Click(object sender, EventArgs e)
        {
            try
            {
                string[] partNames;
                if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                {
                    partNames = _controller.GetGeometryPartNames();
                    _controller.HideGeometryPartsCommand(partNames);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    partNames = _controller.GetModelPartNames();
                    _controller.HideModelPartsCommand(partNames);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    partNames = _controller.GetResultPartNames();
                    _controller.HideResultPartsCommand(partNames);
                }
            }
            catch { }
        }
        private void tsmiShowAllParts_Click(object sender, EventArgs e)
        {
            try
            {
                string[] partNames;
                if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                {
                    partNames = _controller.GetGeometryPartNames();
                    _controller.ShowGeometryPartsCommand(partNames);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    partNames = _controller.GetModelPartNames();
                    _controller.ShowModelPartsCommand(partNames);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    partNames = _controller.GetResultPartNames();
                    _controller.ShowResultPartsCommand(partNames);
                }
            }
            catch { }
        }
        private void tsmiInvertVisibleParts_Click(object sender, EventArgs e)
        {
            try
            {
                BasePart[] parts;
                List<string> partNamesToHide = new List<string>();
                List<string> partNamesToShow = new List<string>();

                if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                {
                    parts = _controller.GetGeometryParts();
                    foreach (var part in parts)
                    {
                        if (part.Visible) partNamesToHide.Add(part.Name);
                        else partNamesToShow.Add(part.Name);
                    }
                    if (partNamesToHide.Count > 0) _controller.HideGeometryPartsCommand(partNamesToHide.ToArray());
                    if (partNamesToShow.Count > 0) _controller.ShowGeometryPartsCommand(partNamesToShow.ToArray());
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    parts = _controller.GetModelParts();
                    foreach (var part in parts)
                    {
                        if (part.Visible) partNamesToHide.Add(part.Name);
                        else partNamesToShow.Add(part.Name);
                    }
                    if (partNamesToHide.Count > 0) _controller.HideModelPartsCommand(partNamesToHide.ToArray());
                    if (partNamesToShow.Count > 0) _controller.ShowModelPartsCommand(partNamesToShow.ToArray());
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    parts = _controller.GetResultParts();
                    if (parts !=null)
                    {
                        foreach (var part in parts)
                        {
                            if (part.Visible) partNamesToHide.Add(part.Name);
                            else partNamesToShow.Add(part.Name);
                        }
                    }
                    if (partNamesToHide.Count > 0) _controller.HideResultPartsCommand(partNamesToHide.ToArray());
                    if (partNamesToShow.Count > 0) _controller.ShowResultPartsCommand(partNamesToShow.ToArray());
                }
            }
            catch { }
        }
        // Annotate
        private void DropDown_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                e.Cancel = true;
        }
        private void tsmiAnnotateFaceOrientations_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateParts_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateMaterials_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateSections_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateSectionThicknesses_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateAllSymbols_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem tsmi)
            {
                // Turn off
                if (tsmi.Checked)
                {
                    ClearAnnotationStatus();  // first check and then clear
                    _controller.AnnotateWithColor = AnnotateWithColorEnum.None;
                }
                // Turn on
                else
                {
                    ClearAnnotationStatus();
                    //
                    tsmi.Checked = true;
                    tsmiAnnotateReferencePoints.Checked = true;
                    tsmiAnnotateConstraints.Checked = true;
                    tsmiAnnotateContactPairs.Checked = true;
                    tsmiAnnotateInitialConditions.Checked = true;
                    tsmiAnnotateBCs.Checked = true;
                    tsmiAnnotateLoads.Checked = true;
                    //
                    _controller.AnnotateWithColor = AnnotateWithColorEnum.ReferencePoints |
                                                    AnnotateWithColorEnum.Constraints |
                                                    AnnotateWithColorEnum.ContactPairs |
                                                    AnnotateWithColorEnum.InitialConditions |
                                                    AnnotateWithColorEnum.BoundaryConditions |
                                                    AnnotateWithColorEnum.Loads |
                                                    AnnotateWithColorEnum.DefinedFields;
                }
            }
        }
        private void tsmiAnnotateReferencePoints_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateConstraints_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateContactPairs_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateInitialConditions_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateBCs_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateLoads_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        //
        private AnnotateWithColorEnum ChangeAnnotationStatus(object sender)
        {
            if (sender is ToolStripMenuItem tsmi)
            {
                // Turn off
                if (tsmi.Checked) tsmi.Checked = false;
                // Turn on
                else
                {
                    // Only one possibility - Face orientations or parts
                    if (tsmi == tsmiAnnotateFaceOrientations || tsmi == tsmiAnnotateParts ||
                        tsmi == tsmiAnnotateMaterials || tsmi == tsmiAnnotateSections ||  
                        tsmi == tsmiAnnotateSectionThicknesses)
                    {
                        ClearAnnotationStatus();
                        tsmi.Checked = true;
                    }
                    // Symbols
                    else
                    {
                        tsmiAnnotateFaceOrientations.Checked = false;
                        tsmiAnnotateParts.Checked = false;
                        tsmiAnnotateMaterials.Checked = false;
                        tsmiAnnotateSections.Checked = false;
                        tsmiAnnotateSectionThicknesses.Checked = false;
                        //
                        tsmi.Checked = true;
                    }
                }
                //
                if (tsmiAnnotateFaceOrientations.Checked) return AnnotateWithColorEnum.FaceOrientation;
                else if (tsmiAnnotateParts.Checked) return AnnotateWithColorEnum.Parts;
                else if (tsmiAnnotateMaterials.Checked) return AnnotateWithColorEnum.Materials;
                else if (tsmiAnnotateSections.Checked) return AnnotateWithColorEnum.Sections;
                else if (tsmiAnnotateSectionThicknesses.Checked) return AnnotateWithColorEnum.SectionThicknesses;
                else
                {
                    AnnotateWithColorEnum status = AnnotateWithColorEnum.None;
                    if (tsmiAnnotateReferencePoints.Checked) status |= AnnotateWithColorEnum.ReferencePoints;
                    if (tsmiAnnotateConstraints.Checked) status |= AnnotateWithColorEnum.Constraints;
                    if (tsmiAnnotateContactPairs.Checked) status |= AnnotateWithColorEnum.ContactPairs;
                    if (tsmiAnnotateInitialConditions.Checked) status |= AnnotateWithColorEnum.InitialConditions;
                    if (tsmiAnnotateBCs.Checked) status |= AnnotateWithColorEnum.BoundaryConditions;
                    if (tsmiAnnotateLoads.Checked) status |= AnnotateWithColorEnum.Loads;
                    if (tsmiAnnotateDefinedFields.Checked) status |= AnnotateWithColorEnum.DefinedFields;
                    //
                    tsmiAnnotateAllSymbols.Checked = status.HasFlag(AnnotateWithColorEnum.ReferencePoints |
                                                                    AnnotateWithColorEnum.Constraints |
                                                                    AnnotateWithColorEnum.ContactPairs |
                                                                    AnnotateWithColorEnum.InitialConditions |
                                                                    AnnotateWithColorEnum.BoundaryConditions |
                                                                    AnnotateWithColorEnum.Loads |
                                                                    AnnotateWithColorEnum.DefinedFields);
                    //
                    return status;
                }
            }
            return AnnotateWithColorEnum.None;
        }
        //
        private void tsmiResultsUndeformed_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.Undeformed, UndeformedModelTypeEnum.None);
        }
        private void tsmiResultsDeformed_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.Deformed, UndeformedModelTypeEnum.None);
        }
        private void tsmiResultsColorContours_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.ColorContours, UndeformedModelTypeEnum.None);
        }
        private void tsmiResultsDeformedColorWireframe_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.ColorContours, UndeformedModelTypeEnum.WireframeBody);
        }

        private void tsmiResultsDeformedColorSolid_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.ColorContours, UndeformedModelTypeEnum.SolidBody);
        }
        private void SetUndeformedModelType(ViewResultsTypeEnum viewResultsType, UndeformedModelTypeEnum undeformedModelType)
        {
            _controller.SetUndeformedModelType(undeformedModelType);
            //
            if (GetCurrentView() == ViewGeometryModelResults.Results)
            {
                if (_frmAnimation.Visible) _frmAnimation.Hide();
                _controller.ViewResultsType = viewResultsType;
            }
            //
            UpdateResultsTypeIconStates();
        }
        private void UpdateResultsTypeIconStates()
        {
            tsbResultsUndeformed.Checked = false;
            tsbResultsDeformed.Checked = false;
            tsbResultsColorContours.Checked = false;
            tsbResultsUndeformedWireframe.Checked = false;
            tsbResultsUndeformedSolid.Checked = false;
            //
            if (GetCurrentView() == ViewGeometryModelResults.Results)
            {
                if (_controller.ViewResultsType == ViewResultsTypeEnum.Undeformed) tsbResultsUndeformed.Checked = true;
                else if (_controller.ViewResultsType == ViewResultsTypeEnum.Deformed) tsbResultsDeformed.Checked = true;
                else if (_controller.ViewResultsType == ViewResultsTypeEnum.ColorContours)
                {
                    if (_controller.GetUndeformedModelType() == UndeformedModelTypeEnum.None)
                        tsbResultsColorContours.Checked = true;
                    else if (_controller.GetUndeformedModelType() == UndeformedModelTypeEnum.WireframeBody)
                        tsbResultsUndeformedWireframe.Checked = true;
                    else if (_controller.GetUndeformedModelType() == UndeformedModelTypeEnum.SolidBody)
                        tsbResultsUndeformedSolid.Checked = true;
                }
            }
        }
        #endregion

        #region Geometry ###########################################################################################################
        internal void tsmiEditGeometryPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetGeometryParts(), EditGeometryPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditColorForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SetColorForGeometryParts);
                Clear3DSelection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Transform
        private void tsmiScaleGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), ScaleGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // End Transform
        private void tsmiCopyGeometryPartsToResults_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), CopyGeometryPartsToResults);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), HideGeometryParts);
                Clear3DSelection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), ShowGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), ShowOnlyGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetColorForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SetColorForGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetColorForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), ResetColorForGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetTransparencyForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SetTransparencyForGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), DeleteGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void tsmiCreateAndImportCompoundPart_Click(object sender, EventArgs e)
        {
            try
            {
                Clear3DSelection();
                SelectMultipleEntities("Parts", _controller.GetCADGeometryParts(), CreateAndImportCompoundPart, 2);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRegenerateCompoundPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetCompoundParts(), RegenerateCompoundParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSwapGeometryPartGeometries_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SwapPartGeometries, 2, 2);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Analyze geometry
        private void tsmiGeometryAnalyze_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), AnalyzeGeometry);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }

        }
        //                                                                                                                          
        private void EditGeometryPart(string partName)
        {
            _frmPartProperties.View = ViewGeometryModelResults.Geometry;
            ShowForm(_frmPartProperties, "Edit Part", partName);
        }
        // Transform
        private void ScaleGeometryParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmScale;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for scaling
            _frmScale.PartNames = partNames;
            //
            ShowForm(_frmScale, "Scale parts: " + partNames.ToShortString(), null);
        }
        public async Task ScaleGeometryPartsAsync(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            bool stateSet = false;
            try
            {
                if (SetStateWorking(Globals.TransformingText))
                {
                    stateSet = true;
                    //
                    if (partNames != null && partNames.Length > 0)
                    {
                        await Task.Run(() => _controller.ScaleGeometryPartsCommand(partNames, scaleCenter, scaleFactors, copy));
                    }
                }
                else MessageBoxes.ShowWarning("Another task is already running.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                if (stateSet) SetStateReady(Globals.TransformingText);
            }
        }
        // End Transform
        private void CopyGeometryPartsToResults(string[] partNames)
        {
            CloseAllForms();
            _controller.CopyGeometryPartsToResults(partNames);
        }
        private void HideGeometryParts(string[] partNames)
        {
            _controller.HideGeometryPartsCommand(partNames);            
        }
        private void ShowGeometryParts(string[] partNames)
        {
            _controller.ShowGeometryPartsCommand(partNames);
        }
        private void ShowOnlyGeometryParts(string[] partNames)
        {
            // If sub part is selected add the whole compound part
            HashSet<string> partsToShow = new HashSet<string>(partNames);
            foreach (var entry in _controller.Model.Geometry.Parts)
            {
                if (entry.Value is CompoundGeometryPart cgp)
                {
                    if (partNames.Contains(cgp.Name))
                    {
                        partsToShow.Add(cgp.Name);
                        partsToShow.UnionWith(cgp.SubPartNames);
                    }
                }
            }
            //
            HashSet<string> allNames = new HashSet<string>(_controller.Model.Geometry.Parts.Keys);
            allNames.ExceptWith(partsToShow);
            _controller.HideGeometryPartsCommand(allNames.ToArray());
            _controller.ShowGeometryPartsCommand(partsToShow.ToArray());
        }
        private void SetColorForGeometryParts(string[] partNames)
        {
            if (_controller.Model.Geometry == null) return;
            //
            using (FrmGetColor frmGetColor = new FrmGetColor())
            {
                Color color = _controller.Model.Geometry.Parts[partNames[0]].GetProperties().Color;
                SetFormLocation(frmGetColor);
                frmGetColor.PrepareForm("Set Part Color: " + partNames.ToShortString(), color);
                if (frmGetColor.ShowDialog() == DialogResult.OK)
                {
                    _controller.SetColorForGeometryPartsCommand(partNames, frmGetColor.Color);
                }
                SaveFormLocation(frmGetColor);
            }
        }
        private void ResetColorForGeometryParts(string[] partNames)
        {
            _controller.ResetColorForGeometryPartsCommand(partNames);
        }
        private void SetTransparencyForGeometryParts(string[] partNames)
        {
            if (_controller.Model.Geometry == null) return;
            //
            using (FrmGetValue frmGetValue = new FrmGetValue())
            {
                frmGetValue.NumOfDigits = 0;
                frmGetValue.MinValue = 25;
                frmGetValue.MaxValue = 255;
                SetFormLocation(frmGetValue);
                OrderedDictionary<string, double> presetValues =
                    new OrderedDictionary<string, double>("Preset Transparency values", StringComparer.OrdinalIgnoreCase);
                presetValues.Add("Semi-transparent", 128);
                presetValues.Add("Opaque", 255);
                string desc = "Enter the transparency between 0 and 255.\n" + "(0 - transparent; 255 - opaque)";
                frmGetValue.PrepareForm("Set Transparency: " + partNames.ToShortString(), "Transparency", desc, 128, presetValues);
                if (frmGetValue.ShowDialog() == DialogResult.OK)
                {
                    _controller.SetTransparencyForGeometryPartsCommand(partNames, (byte)frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void DeleteGeometryParts(string[] partNames)
        {
            GeometryPart[] parts = _controller.GetGeometryPartsWithoutSubParts();
            HashSet<string> deleteAblePartNames = new HashSet<string>();
            foreach (GeometryPart part in parts) deleteAblePartNames.Add(part.Name);
            deleteAblePartNames.IntersectWith(partNames);
            if (deleteAblePartNames.Count > 0)
            {
                partNames = deleteAblePartNames.ToArray();
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected parts?") == DialogResult.OK)
                {
                    _controller.RemoveGeometryPartsCommand(partNames.ToArray());
                }
            }
            else MessageBoxes.ShowError("Selected parts belong to a compound part and cannot be deleted:" + Environment.NewLine +
                                        partNames.ToRows());
        }
        //
        private async void CreateAndImportCompoundPart(string[] partNames)
        {
            try
            {
                SetStateWorking(Globals.CreatingCompoundText, true);
                //
                GeometryPart part;
                HashSet<PartType> stlPartTypes = new HashSet<PartType>();
                HashSet<PartType> cadPartTypes = new HashSet<PartType>();
                //
                string[] allPartNames = _controller.GetMeshablePartNames(partNames);
                foreach (var partName in allPartNames)
                {
                    part = (GeometryPart)_controller.Model.Geometry.Parts[partName];
                    if (part.IsCADPart) cadPartTypes.Add(part.PartType);
                    else stlPartTypes.Add(part.PartType);
                }
                if (stlPartTypes.Count + cadPartTypes.Count != 1)
                    throw new CaeException("Compound part can be made from only CAD or only stl based geometry parts " + 
                                           "of the same type.");
                await Task.Run(() => _controller.CreateAndImportCompoundPartCommand(partNames));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.CreatingCompoundText);
            }
        }
        private async void RegenerateCompoundParts(string[] compoundPartNames)
        {
            try
            {
                SetStateWorking(Globals.RegeneratingCompoundText, true);
                //
                string missingPartName = null;
                string errorCompoundPartName = null;
                CompoundGeometryPart part;
                foreach (var compoundPartName in compoundPartNames)
                {
                    part = (CompoundGeometryPart)_controller.Model.Geometry.Parts[compoundPartName];
                    if (part.CreatedFromPartNames != null && part.CreatedFromPartNames.Length > 1)
                    {
                        foreach (var createdFromPartName in part.CreatedFromPartNames)
                        {
                            if (!_controller.Model.Geometry.Parts.ContainsKey(createdFromPartName))
                            {
                                missingPartName = createdFromPartName;
                                errorCompoundPartName = compoundPartName;
                                break;
                            }
                        }
                        //
                        if (missingPartName != null) break;
                    }
                }
                if (missingPartName != null)
                    throw new CaeException("The part '" + missingPartName + "' that was used to create a compound part '" +
                                           errorCompoundPartName + "' is missing.");
                //
                await Task.Run(() => _controller.RegenerateCompoundPartsCommand(compoundPartNames));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.RegeneratingCompoundText);
            }
        }
        private void SwapPartGeometries(string[] partNames)
        {
            GeometryPart[] parts = _controller.GetGeometryPartsWithoutSubParts();
            GeometryPart part1 = _controller.GetGeometryPart(partNames[0]);
            GeometryPart part2 = _controller.GetGeometryPart(partNames[1]);
            if (parts.Contains(part1) && parts.Contains(part2))
            {
                if (part1 is CompoundGeometryPart || part2 is CompoundGeometryPart)
                    MessageBoxes.ShowError("Compound parts cannot be swapped.");
                else
                    _controller.SwapPartGeometriesCommand(partNames[0], partNames[1]);
            }
            else MessageBoxes.ShowError("Compound subparts cannot be swapped.");
        }
        // Analyze geometry
        private void AnalyzeGeometry(string[] partNames)
        {
            if (!_frmAnalyzeGeometry.Visible)
            {
                CloseAllForms();
                SetFormLocation((Form)_frmAnalyzeGeometry);
                _frmAnalyzeGeometry.PartNamesToAnalyze = partNames;
                _frmAnalyzeGeometry.Show();
            }
        }

        #endregion  ################################################################################################################

        #region Geometry CAD part menu   ###########################################################################################
        private async void tsmiFlipFaceNormalCAD_Click(object sender, EventArgs e)
        {
            try
            {
                // Must be outside the await part otherwise causes screen flickering
                AnnotateWithColorEnum _prevShowWithColor = _controller.AnnotateWithColor;
                _controller.AnnotateWithColor = AnnotateWithColorEnum.FaceOrientation;
                //
                await Task.Run(() =>
                {
                    _frmSelectGeometry.HideFormOnOK = false;
                    _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Surface;
                    _frmSelectGeometry.OnOKCallback = FlipFaces;
                    //
                    InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select faces to flip", null); });
                    while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                });
                //
                _controller.AnnotateWithColor = _prevShowWithColor;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.FlippingNormalsText);
            }
        }
        private async void tsmiSplitAFaceUsingTwoPoints_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    GeometrySelection surfaceSelection;
                    GeometrySelection verticesSelection;
                    while (true)
                    {
                        // Get a surface to split
                        _frmSelectGeometry.MaxNumberOfGeometryIds = 1;
                        _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Surface;
                        //
                        InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select a face to split", null); });
                        while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                        //
                        if (_frmSelectGeometry.DialogResult == DialogResult.OK)
                        {
                            surfaceSelection = _frmSelectGeometry.GeometrySelection.DeepClone();
                            // Get two vertices to split the surface
                            _frmSelectGeometry.MaxNumberOfGeometryIds = 2;
                            _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Vertex;
                            //
                            InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select splitting vertices", null); });
                            while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                            //
                            if (_frmSelectGeometry.DialogResult == DialogResult.OK)
                            {
                                verticesSelection = _frmSelectGeometry.GeometrySelection.DeepClone();
                                SplitAFaceUsingTwoPoints(surfaceSelection, verticesSelection);
                            }
                            else break;
                        }
                        else break;
                    }
                });
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.SplittingFacesText);
            }
        }
        private async void tsmiDefeature_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    _frmSelectGeometry.HideFormOnOK = false;
                    _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Surface;
                    _frmSelectGeometry.OnOKCallback = Defeature;
                    //
                    InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select faces to defeature", null); });
                    while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                });
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.DefeaturingText);
            }
        }
        //
        private void FlipFaces(GeometrySelection geometrySelection)
        {
            try
            {
                SetStateWorking(Globals.FlippingNormalsText);
                _controller.FlipFaceOrientationsCommand(geometrySelection);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SetStateReady(Globals.FlippingNormalsText);
            }
        }
        private void SplitAFaceUsingTwoPoints(GeometrySelection surfaceSelection, GeometrySelection verticesSelection)
        {
            try
            {
                SetStateWorking(Globals.SplittingFacesText);
                _controller.SplitAFaceUsingTwoPointsCommand(surfaceSelection, verticesSelection);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SetStateReady(Globals.SplittingFacesText);
            }
        }
        private void Defeature(GeometrySelection geometrySelection)
        {
            try
            {
                SetStateWorking(Globals.DefeaturingText, true);
                _controller.DefeatureCommand(geometrySelection);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SetStateReady(Globals.DefeaturingText);
            }
        }


        #endregion #################################################################################################################

        #region Geometry Stl part menu   ###########################################################################################
        private void tsmiFindStlEdgesByAngleForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetNonCADGeometryParts(), FindEdgesByAngleForGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiFlipStlPartFaceNormals_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetNonCADGeometryParts(), FlipStlPartSurfacesNormal);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSmoothStlPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetNonCADGeometryParts(), _controller.SmoothGeometryPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiDeleteStlPartFaces_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    _frmSelectGeometry.HideFormOnOK = false;
                    _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Surface;
                    _frmSelectGeometry.OnOKCallback = DeleteStlPartFaces;
                    //
                    InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select faces to delete", null); });
                    while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                });
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.DeletingFacesText);
            }
        }
        private void tsmiCropStlPartWithCylinder_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetGeometryParts(), _controller.CropGeometryPartWithCylinder);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCropStlPartWithCube_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetGeometryParts(), _controller.CropGeometryPartWithCube);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void FlipStlPartSurfacesNormal(string[] partNames)
        {
            _controller.FlipStlPartSurfacesNormalCommand(partNames);
        }
        private void FindEdgesByAngleForGeometryParts(string[] partNames)
        {
            using (FrmGetValue frmGetValue = new FrmGetValue())
            {
                SetUpFrmGetValueForEdgeAngle(frmGetValue, partNames);
                //
                if (frmGetValue.ShowDialog() == DialogResult.OK)
                {
                    _controller.FindEdgesByAngleForGeometryPartsCommand(partNames, frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void SetUpFrmGetValueForEdgeAngle(FrmGetValue frmGetValue, string[] partNames)
        {
            frmGetValue.NumOfDigits = 2;
            frmGetValue.MinValue = 0;
            frmGetValue.MaxValue = 90;
            SetFormLocation(frmGetValue);
            OrderedDictionary<string, double> presetValues =
                new OrderedDictionary<string, double>("Preset Transparency Values", StringComparer.OrdinalIgnoreCase);
            presetValues.Add("Default", CaeMesh.Globals.EdgeAngle);
            string desc = "Enter the face angle for model edges detection.";
            frmGetValue.PrepareForm("Find model edges: " + partNames.ToShortString(), "Angle", desc,
                                    CaeMesh.Globals.EdgeAngle, presetValues, new StringAngleDegConverter());
        }
        private void DeleteStlPartFaces(GeometrySelection geometrySelection)
        {
            SetStateWorking(Globals.DeletingFacesText);
            _controller.DeleteStlPartFacesCommand(geometrySelection);
            SetStateReady(Globals.DeletingFacesText);
        }
        
        #endregion #################################################################################################################

        #region Mesh ###############################################################################################################
        // Mesh setup item
        private void tsmiCreateMeshSetupItem_Click(object sender, EventArgs e)
        {
            try
            {
                CreateMeshSetupItem();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditMeshSetupItem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Mesh Setup Items", _controller.GetMeshSetupItems(), EditMeshSetupItem);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateMeshSetupItem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Mesh Setup Items", _controller.GetMeshSetupItems(), DuplicateMeshSetupItems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteMeshSetupItem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Mesh Setup Items", _controller.GetMeshSetupItems(), DeleteMeshSetupItems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Preview
        private void tsmiPreviewEdgeMesh_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), PreviewEdgeMesh);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Create mesh
        internal void tsmiCreateMesh_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), CreatePartMeshes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }            
        }
        // Mesh setup item
        private void CreateMeshSetupItem()
        {
            if (_controller.Model.Geometry == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmMeshSetupItem);
            ShowForm(_frmMeshSetupItem, "Create Mesh Setup Item", null);
        }
        private void EditMeshSetupItem(string meshSetupItemName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmMeshSetupItem);
            ShowForm(_frmMeshSetupItem, "Edit Mesh Setup Item", meshSetupItemName);
        }
        private void DuplicateMeshSetupItems(string[] meshSetupItemNames)
        {
            _controller.DuplicateMeshSetupItemsCommand(meshSetupItemNames);
        }
        private void DeleteMeshSetupItems(string[] meshSetupItemNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected mesh setup items?" + Environment.NewLine
                                                 + meshSetupItemNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveMeshSetupItemsCommand(meshSetupItemNames);
            }
        }
        public MeshSetupItem EditMeshingParametersFromHistory(MeshSetupItem meshSetupItem)
        {
            InvokeIfRequired(() =>
            {
                ItemSetDataEditor.SetForms(_frmSelectItemSet, true, null);
                //
                CloseAllForms();
                SetFormLocation(_frmMeshSetupItem);
                _frmMeshSetupItem.Text = "Redefine " + meshSetupItem.Name;
                //
                _frmMeshSetupItem.PrepareForm(null, null);
                _frmMeshSetupItem.SetMeshSetupItem(meshSetupItem.DeepClone());
                _frmMeshSetupItem.ShowDialog();
                //
                if (_frmMeshSetupItem.DialogResult == DialogResult.OK) meshSetupItem = _frmMeshSetupItem.MeshSetupItem;
            });
            //
            return meshSetupItem;
        }
        // Preview
        private async void PreviewEdgeMesh(string[] partNames)
        {
            await PreviewEdgeMeshAsync(partNames, null);
        }
        private async Task PreviewEdgeMeshAsync(string[] partNames, MeshSetupItem meshSetupItem)
        {
            bool stateSet = false;
            try
            {
                if (SetStateWorking(Globals.PreviewText))
                {
                    stateSet = true;
                    //
                    await Task.Run(() =>
                    {
                        foreach (var partName in partNames)
                        {
                            _controller.PreviewEdgeMesh(partName, meshSetupItem);
                        }
                    });
                }
                else MessageBoxes.ShowWarning("Another task is already running.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                if (stateSet) SetStateReady(Globals.PreviewText);
            }
        }
        // Create mesh
        private async void CreatePartMeshes(string[] partNames)
        {
            try
            {
                List<string> errors = new List<string>();
                SetStateWorking(Globals.MeshingText, true);
                MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0);
                Keys modifierKeys = Keys.Control;
                _modelTree.ClearTreeSelection(ViewType.Model);                
                //
                CloseAllForms();
                //
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                //
                GeometryPart part;
                foreach (var partName in partNames)
                {
                    try
                    {
                        part = _controller.GetGeometryPart(partName);
                        //
                        await Task.Run(() => _controller.CreateMeshCommand(partName));
                        // Check for the cancel button click
                        if (IsStateWorking())
                        {
                            _modelTree.SelectBasePart(e, modifierKeys, part, true);
                        }
                        else
                        {
                            errors.Add("Mesh generation canceled.");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is CaeException) errors.Add(partName + ": " + ex.Message);
                        errors.Add("Mesh generation failed for part " + partName +
                                   ". Check the geometry and/or adjust the meshing parameters.");
                    }
                }
                watch.Stop();
                if (partNames.Length > 1)
                {
                    WriteDataToOutput("");
                    WriteDataToOutput("Elapsed time [s]: " + watch.Elapsed.TotalSeconds.ToString());
                }
                //
                _controller.UpdateExplodedView(true);
                //
                if (errors.Count > 0)
                {
                    WriteDataToOutput("");
                    foreach (var error in errors) WriteDataToOutput(error);
                    MessageBoxes.ShowError("Errors occurred during meshing. Please check the output window.");
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.MeshingText);
            }
        }
        // Advisor
        internal void CreateDefaultMesh(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), CreateDefaultMeshes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void CreateUserDefinedMesh(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), CreateUserDefinedMeshes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void CreateDefaultMeshes(string[] partNames)
        {
            CreatePartMeshes(partNames);
        }
        private async void CreateUserDefinedMeshes(string[] partNames)
        {
            await Task.Run(() =>
            {
                InvokeIfRequired(() => { CreateMeshSetupItem(); });
                //
                bool firstTime = true;
                do
                {
                    Thread.Sleep(250);
                    if (firstTime)
                    {
                        Selection selection = new Selection();
                        selection.Add(new SelectionNodeIds(vtkSelectOperation.None, false,
                                                           _controller.Model.Geometry.GetPartIdsFomPartNames(partNames)));
                        selection.SelectItem = vtkSelectItem.Part;
                        selection.CurrentView = (int)ViewGeometryModelResults.Geometry;
                        _controller.Selection = selection;
                        _controller.SetSelectByToOff();
                        //
                        InvokeIfRequired(() => {
                            //_controller.HighlightSelection();
                            SelectionChanged(_controller.Model.Geometry.GetPartIdsFomPartNames(partNames));
                        });
                        //
                        firstTime = false;
                    }
                }
                while (_frmMeshSetupItem.Visible);

            });
            if (_frmMeshSetupItem.DialogResult == DialogResult.OK) CreatePartMeshes(partNames);
        }

        #endregion  ################################################################################################################

        #region Model edit  ########################################################################################################
        private void tsmiEditModel_Click(object sender, EventArgs e)
        {
            try
            {
                EditModel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditCalculiXKeywords_Click(object sender, EventArgs e)
        {
            try
            {
                EditCalculiXKeywords();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Tools
        private void tsmiFindEdgesByAngleForModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetNonCADModelParts(), FindEdgesByAngleForModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCreateBoundaryLayer_Click(object sender, EventArgs e)
        {
            try
            {
                CreateBoundaryLayer();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRemeshElements_Click(object sender, EventArgs e)
        {
            try
            {
                RemeshElements();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiThickenShellMesh_Click(object sender, EventArgs e)
        {
            try
            {
                ThickenShellMesh();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSplitPartMeshUsingSurface_Click(object sender, EventArgs e)
        {
            try
            {
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSplitPartMeshUsingSurface);
                ShowForm(_frmSplitPartMeshUsingSurface, "Split Part Mesh Using Surface", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiUpdateNodalCoordinatesFromFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized || _controller.Model.Mesh == null) return;
                //
                string fileName = GetFileNameToImport("Abaqus/Calculix inp files|*.inp");
                //
                SetStateWorking(Globals.ImportingText);
                await Task.Run(() => _controller.UpdateNodalCoordinatesFromFileCommand(fileName));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ImportingText);
            }
        }
        //                                                                                                                          
        private void EditModel()
        {
            if (_controller.Model.UnitSystem.UnitSystemType != UnitSystemType.Undefined)
                ShowForm(_frmModelProperties, "Edit Model", null);

        }
        private void EditCalculiXKeywords()
        {
            // This is also called from the model tree - needs try, catch
            try
            {
                if (CheckValidity())
                {
                    if (_frmCalculixKeywordEditor == null) _frmCalculixKeywordEditor = new FrmCalculixKeywordEditor();
                    //
                    _frmCalculixKeywordEditor.Keywords = _controller.GetCalculixModelKeywords();
                    _frmCalculixKeywordEditor.UserKeywords = _controller.GetCalculixUserKeywords();
                    //
                    if (_frmCalculixKeywordEditor.Keywords != null)
                    {
                        _frmCalculixKeywordEditor.PrepareForm(); // must be here to check for errors
                        if (_frmCalculixKeywordEditor.ShowDialog() == DialogResult.OK)
                        {
                            _controller.SetCalculixUserKeywordsCommand(_frmCalculixKeywordEditor.UserKeywords);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Tools
        private void FindEdgesByAngleForModelParts(string[] partNames)
        {
            using (FrmGetValue frmGetValue = new FrmGetValue())
            {
                SetUpFrmGetValueForEdgeAngle(frmGetValue, partNames);
                //
                if (frmGetValue.ShowDialog() == DialogResult.OK)
                {
                    _controller.FindEdgesByAngleForModelPartsCommand(partNames, frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void CreateBoundaryLayer()
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmBoundaryLayer);
            ShowForm(_frmBoundaryLayer, "Create Boundary Layer", null);
        }
        private void RemeshElements()
        {
            if (_controller.Model == null || _controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmRemeshingParameters);
            ShowForm(_frmRemeshingParameters, "Remeshing Parameters", null);
        }
        private void ThickenShellMesh()
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmThickenShellMesh);
            ShowForm(_frmThickenShellMesh, "Thicken Shell Mesh", null);
        }
        private async void SplitPartMeshUsingSurface(SplitPartMeshData splitPartMeshData)
        {
            try
            {
                if (!_controller.ModelInitialized || _controller.Model.Mesh == null) return;
                //
                SetStateWorking(Globals.MeshingText, true);
                await Task.Run(() => _controller.SplitPartMeshUsingSurfaceCommand(splitPartMeshData));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.MeshingText);
            }
        }
        public void ShowSplitMeshResults()
        {
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void UpdateNodalCoordinatesFromFile()
        {

        }

        #endregion  ################################################################################################################

        #region Node menu  #########################################################################################################
        private void tsmiRenumberAllNodes_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                //
                using (FrmGetValue frmGetValue = new FrmGetValue())
                {
                    frmGetValue.NumOfDigits = 0;
                    frmGetValue.MinValue = 1;
                    SetFormLocation(frmGetValue);
                    string desc = "Enter the starting node id for the node renumbering.";
                    frmGetValue.PrepareForm("Renumber Nodes", "Start node id", desc, 1, null);
                    if (frmGetValue.ShowDialog() == DialogResult.OK)
                    {
                        _controller.RenumberNodesCommand((int)frmGetValue.Value);
                    }
                    SaveFormLocation(frmGetValue);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeCoincidentNodes_Click(object sender, EventArgs e)
        {
            try
            {
                MergeCoincidentNodes();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //                                                                                                                          
        private void MergeCoincidentNodes()
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmMergeCoincidentNodes);
            ShowForm(_frmMergeCoincidentNodes, "Merge Coincident Nodes", null);
        }
        #endregion  ################################################################################################################

        #region Element menu  ######################################################################################################
        private void tsmiRenumberAllElements_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                //
                using (FrmGetValue frmGetValue = new FrmGetValue())
                {
                    frmGetValue.NumOfDigits = 0;
                    frmGetValue.MinValue = 1;
                    SetFormLocation(frmGetValue);
                    string desc = "Enter the starting element id for the element renumbering.";
                    frmGetValue.PrepareForm("Renumber Elements", "Start element id", desc, 1, null);
                    if (frmGetValue.ShowDialog() == DialogResult.OK)
                    {
                        _controller.RenumberElementsCommand((int)frmGetValue.Value);
                    }
                    SaveFormLocation(frmGetValue);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiElementQuality_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ElementQuality);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void ElementQuality(string[] partNames)
        {
            // Set all part names for quality analysis
            _frmElementQuality.PartNames = partNames;
            //
            ShowForm(_frmElementQuality, "Element Quality", null);
        }
        #endregion  ################################################################################################################

        #region Model part menu  ###################################################################################################
        private void tsmiEditModelPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetModelParts(), EditModelPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Transform
        private void tsmiTranslateModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), TranslateModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiScaleModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ScaleModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRotateModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), RotateModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void tsmiMergeModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), MergeModelParts, 2);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), HideModelParts);
                Clear3DSelection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ShowModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ShowOnlyModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetColorForModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), SetColorForModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetColorForModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ResetColorForModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetTransparencyForModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), SetTransparencyForModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), DeleteModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //                                                                                                                          
        private void EditModelPart(string partName)
        {
            _frmPartProperties.View = ViewGeometryModelResults.Model; 
            ShowForm(_frmPartProperties, "Edit Part", partName);
        }
        // Transform
        private void TranslateModelParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmTranslate;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for translation
            _frmTranslate.PartNames = partNames;    
            //
            ShowForm(_frmTranslate, "Translate Parts: " + partNames.ToShortString(), null);
        }
        private void ScaleModelParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmScale;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for scaling
            _frmScale.PartNames = partNames;    
            //
            ShowForm(_frmScale, "Scale Parts: " + partNames.ToShortString(), null);
        }
        private void RotateModelParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmRotate;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for rotation
            _frmRotate.PartNames = partNames;    
            //
            ShowForm(_frmRotate, "Rotate Parts: " + partNames.ToShortString(), null);
        }
        //
        private void MergeModelParts(string[] partNames)
        {
            if (_controller.AreModelPartsMergeable(partNames))
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to merge selected parts?") == DialogResult.OK)
                {
                    _controller.MergeModelPartsCommand(partNames);
                }
            }
            else MessageBoxes.ShowError("Selected parts are of a different type and thus cannot be merged.");
        }
        private void HideModelParts(string[] partNames)
        {
            _controller.HideModelPartsCommand(partNames);
        }
        private void ShowModelParts(string[] partNames)
        {
            _controller.ShowModelPartsCommand(partNames);
        }
        private void ShowOnlyModelParts(string[] partNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.Mesh.Parts.Keys);
            allNames.ExceptWith(partNames);
            _controller.HideModelPartsCommand(allNames.ToArray());
            _controller.ShowModelPartsCommand(partNames);
        }
        private void SetColorForModelParts(string[] partNames)
        {
            if (_controller.Model.Mesh == null) return;
            //
            using (FrmGetColor frmGetColor = new FrmGetColor())
            {
                Color color = _controller.Model.Mesh.Parts[partNames[0]].GetProperties().Color;
                SetFormLocation(frmGetColor);
                frmGetColor.PrepareForm("Set Part Color: " + partNames.ToShortString(), color);
                if (frmGetColor.ShowDialog() == DialogResult.OK)
                {
                    _controller.SetColorForModelPartsCommand(partNames, frmGetColor.Color);
                }
                SaveFormLocation(frmGetColor);
            }
        }
        private void ResetColorForModelParts(string[] partNames)
        {
            _controller.ResetColorForModelPartsCommand(partNames);
        }
        private void SetTransparencyForModelParts(string[] partNames)
        {
            if (_controller.Model.Mesh == null) return;
            //
            using (FrmGetValue frmGetValue = new FrmGetValue())
            {
                frmGetValue.NumOfDigits = 0;
                frmGetValue.MinValue = 25;
                frmGetValue.MaxValue = 255;
                SetFormLocation(frmGetValue);
                OrderedDictionary<string, double> presetValues
                    = new OrderedDictionary<string, double>("Preset Transparency Values", StringComparer.OrdinalIgnoreCase);
                presetValues.Add("Semi-transparent", 128);
                presetValues.Add("Opaque", 255);
                string desc = "Enter the transparency between 0 and 255.\n" + "(0 - transparent; 255 - opaque)";
                frmGetValue.PrepareForm("Set Transparency: " + partNames.ToShortString(), "Transparency", desc, 128, presetValues);
                if (frmGetValue.ShowDialog() == DialogResult.OK)
                {
                    _controller.SetTransparencyForModelPartsCommand(partNames, (byte)frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void DeleteModelParts(string[] partNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected parts?" + Environment.NewLine
                                                 + partNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveModelPartsCommand(partNames);
            }
        }
        
        #endregion  ################################################################################################################

        #region Node set menu  #####################################################################################################
        private void tsmiCreateNodeSet_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmNodeSet);
                ShowForm(_frmNodeSet, "Create Node Set", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditNodeSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Node Sets", _controller.GetUserNodeSets(), EditNodeSet);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateNodeSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Node Sets", _controller.GetUserNodeSets(), DuplicateNodeSets);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteNodeSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Node Sets", _controller.GetUserNodeSets(), DeleteNodeSets);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditNodeSet(string nodeSetName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmNodeSet);
            ShowForm(_frmNodeSet, "Edit Node Set", nodeSetName);
        }
        private void DuplicateNodeSets(string[] nodeSetNames)
        {
            _controller.DuplicateNodeSetsCommand(nodeSetNames);
        }
        private void DeleteNodeSets(string[] nodeSetNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected node sets?" + Environment.NewLine
                                                 + nodeSetNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveNodeSetsCommand(nodeSetNames);
            }
        }

        #endregion  ################################################################################################################

        #region Element set menu  ##################################################################################################
        private void tsmiCreateElementSet_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmElementSet);
                ShowForm(_frmElementSet, "Create Element Set", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditElementSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Element Sets", _controller.GetUserElementSets(), EditElementSet);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateElementSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Element Sets", _controller.GetUserElementSets(), DuplicateElementSets);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiConvertElementSetsToMeshParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Element Sets", _controller.GetUserElementSets(), ConvertElementSetsToMeshParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteElementSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Element Sets", _controller.GetUserElementSets(), DeleteElementSets);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditElementSet(string elementSetName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmElementSet);
            ShowForm(_frmElementSet, "Edit Element Set", elementSetName);
        }
        private void DuplicateElementSets(string[] elementSetNames)
        {
            _controller.DuplicateElementSetsCommand(elementSetNames);
        }
        private void ConvertElementSetsToMeshParts(string[] elementSetNames)
        {
            _controller.ConvertElementSetsToMeshPartsCommand(elementSetNames);
        }
        private void DeleteElementSets(string[] elementSetNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected element sets?" + Environment.NewLine
                                                 + elementSetNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveElementSetsCommand(elementSetNames);
            }
        }

        #endregion  ################################################################################################################

        #region Surface menu  ######################################################################################################
        private void tsmiCreateSurface_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSurface);
                ShowForm(_frmSurface, "Create Surface", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditSurface_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Surfaces", _controller.GetUserSurfaces(), EditSurface);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateSurface_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Surfaces", _controller.GetUserSurfaces(), DuplicateSurfaces);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteSurface_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Surfaces", _controller.GetUserSurfaces(), DeleteSurfaces);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditSurface(string surfaceName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSurface);
            ShowForm(_frmSurface, "Edit Surface", surfaceName);
        }
        private void DuplicateSurfaces(string[] surfaceNames)
        {
            _controller.DuplicateSurfacesCommand(surfaceNames);
        }
        private void DeleteSurfaces(string[] surfaceNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected surfaces?" + Environment.NewLine
                                                 + surfaceNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveSurfacesCommand(surfaceNames);
            }
        }

        #endregion  ################################################################################################################

        #region Model Reference point  #############################################################################################
        private void tsmiCreateModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                //
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmReferencePoint);
                ShowForm(_frmReferencePoint, "Create Model Reference Point", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Model Reference Points", _controller.GetAllModelReferencePoints(), EditModelReferencePoint);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       DuplicateModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       HideModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       ShowModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       ShowOnlyModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       DeleteModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditModelReferencePoint(string referencePointName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmReferencePoint);
            ShowForm(_frmReferencePoint, "Edit Reference Point", referencePointName);
        }
        private void DuplicateModelReferencePoints(string[] referencePointNames)
        {
            _controller.DuplicateModelReferencePointsCommand(referencePointNames);
        }
        private void HideModelReferencePoints(string[] referencePointNames)
        {
            _controller.HideModelReferencePointsCommand(referencePointNames);
        }
        private void ShowModelReferencePoints(string[] referencePointNames)
        {
            _controller.ShowModelReferencePointsCommand(referencePointNames);
        }
        private void ShowOnlyModelReferencePoints(string[] referencePointNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.GetModelReferencePointNames());
            allNames.ExceptWith(referencePointNames);
            _controller.HideModelReferencePointsCommand(allNames.ToArray());
            _controller.ShowModelReferencePointsCommand(referencePointNames);
        }
        private void DeleteModelReferencePoints(string[] referencePointNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected model reference points?" + Environment.NewLine
                                                 + referencePointNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveModelReferencePointsCommand(referencePointNames);
            }
        }

        #endregion  ################################################################################################################

        #region Model Coordinate system  ###########################################################################################
        private void tsmiCreateModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmCoordinateSystem);
                ShowForm(_frmCoordinateSystem, "Create Model Coordinate System", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                EditModelCoordinateSystem);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       DuplicateModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       HideModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       ShowModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       ShowOnlyModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       DeleteModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditModelCoordinateSystem(string coordinateSystemName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmCoordinateSystem);
            ShowForm(_frmCoordinateSystem, "Edit Coordinate System", coordinateSystemName);
        }
        private void DuplicateModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.DuplicateModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void HideModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.HideModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void ShowModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.ShowModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void ShowOnlyModelCoordinateSystems(string[] coordinateSystemNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.Mesh.CoordinateSystems.Keys);
            allNames.ExceptWith(coordinateSystemNames);
            _controller.HideModelCoordinateSystemsCommand(allNames.ToArray());
            _controller.ShowModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void DeleteModelCoordinateSystems(string[] coordinateSystemNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected model coordinate systems?" + Environment.NewLine
                                                 + coordinateSystemNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveModelCoordinateSystemsCommand(coordinateSystemNames);
            }
        }

        #endregion  ################################################################################################################

        #region Material menu  #####################################################################################################
        internal void tsmiCreateMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                ShowForm(_frmMaterial, "Create Material", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void CreateSimpleMaterial(object sender, EventArgs e)
        {
            _frmMaterial.UseSimpleEditor = true;
            tsmiCreateMaterial_Click(sender, e);
        }
        private void tsmiEditMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Materials", _controller.GetAllMaterials(), EditMaterial);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Materials", _controller.GetAllMaterials(), DuplicateMaterials);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private  void tsmiImportMaterial_Click(object sender, EventArgs e)
        {
            ImportFile(true);
        }
        private void tsmiExportMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Materials", _controller.GetAllMaterials(), ExportMaterials);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Materials", _controller.GetAllMaterials(), DeleteMaterials);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
           
        }
        //
        private void EditMaterial(string materialName)
        {
            ShowForm(_frmMaterial, "Edit Material", materialName);
        }
        private void DuplicateMaterials(string[] materialNames)
        {
            _controller.DuplicateMaterialsCommand(materialNames);
        }
        private async void ExportMaterials(string[] materialNames)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Model;
                //
                saveFileDialog.Filter = "Calculix files | *.inp";
                //
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // The filter adds the extension to the file name
                    SetStateWorking(Globals.ExportingText);
                    //
                    await Task.Run(() => _controller.ExportMaterials(materialNames, saveFileDialog.FileName));
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private void DeleteMaterials(string[] materialNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected materials?" + Environment.NewLine
                                                 + materialNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveMaterialsCommand(materialNames);
            }
        }
        //
        internal void tsmiMaterialLibrary_Click(object sender, EventArgs e)
        {
            try
            {
                ShowMaterialLibrary();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void ShowMaterialLibrary()
        {
            if (_controller.Model.Mesh != null)
            {
                FrmMaterialLibrary fml = new FrmMaterialLibrary(_controller);
                CloseAllForms();
                SetFormLocation(fml);
                fml.ShowDialog();                
            }
        }

        #endregion  ################################################################################################################

        #region Section menu  ######################################################################################################
        internal void tsmiCreateSection_Click(object sender, EventArgs e)
        {
            try
            {
                CreateSection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditSection_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Sections", _controller.GetAllSections(), EditSection);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateSection_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Sections", _controller.GetAllSections(), DuplicateSections);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDelete_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Sections", _controller.GetAllSections(), DeleteSections);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateSection()
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSection);
            ShowForm(_frmSection, "Create Section", null);
        }
        private void EditSection(string sectionName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSection);
            ShowForm(_frmSection, "Edit Section", sectionName);
        }
        private void DuplicateSections(string[] sectionNames)
        {
            _controller.DuplicateSectionsCommand(sectionNames);
        }
        private void DeleteSections(string[] sectionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected sections?" + Environment.NewLine
                                                 + sectionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveSectionsCommand(sectionNames);
            }
        }

        #endregion  ################################################################################################################

        #region Interaction menu  ##################################################################################################

        #region Constraint menu  ###################################################################################################
        private void tsmiCreateConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmConstraint);
                ShowForm(_frmConstraint, "Create Constraint", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditConstraint_Click(object sender, EventArgs e)
        {
             try
             {
                 SelectOneEntity("Constraints", _controller.GetAllConstraints(), EditConstraint);
             }
             catch (Exception ex)
             {
                 ExceptionTools.Show(this, ex);
             }
        }
        private void tsmiDuplicateConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), DuplicateConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSwapMasterSlaveConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), SwapMasterSlaveConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeByMasterSlaveConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), MergeByMasterSlaveConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), HideConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), ShowConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), DeleteConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditConstraint(string constraintName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmConstraint);
            ShowForm(_frmConstraint, "Edit Constraint", constraintName);
        }
        private void DuplicateConstraints(string[] constraintNames)
        {
            _controller.DuplicateConstraintsCommand(constraintNames);
        }
        private void SwapMasterSlaveConstraints(string[] constraintNames)
        {
            _controller.SwapMasterSlaveConstraintsCommand(constraintNames);
        }
        private void MergeByMasterSlaveConstraints(string[] constraintNames)
        {
            _controller.MergeByMasterSlaveConstraintsCommand(constraintNames);
        }
        private void HideConstraints(string[] constraintNames)
        {
            _controller.HideConstraintsCommand(constraintNames);
        }
        private void ShowConstraints(string[] constraintNames)
        {
            _controller.ShowConstraintsCommand(constraintNames);
        }
        private void ShowOnlyConstraints(string[] constraintNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.Constraints.Keys);
            allNames.ExceptWith(constraintNames);
            _controller.HideConstraintsCommand(allNames.ToArray());
            _controller.ShowConstraintsCommand(constraintNames);
        }
        private void DeleteConstraints(string[] constraintNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected constraints?" + Environment.NewLine
                                                 + constraintNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveConstraintsCommand(constraintNames);
            }
        }

        #endregion  ################################################################################################################
        
        #region Surface interaction menu  ##########################################################################################
        private void tsmiCreateSurfaceInteraction_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                ShowForm(_frmSurfaceInteraction, "Create surface interaction", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditSurfaceInteraction_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Surface interactions", _controller.GetAllSurfaceInteractions(), EditSurfaceInteraction);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateSurfaceInteraction_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Surface interactions", _controller.GetAllSurfaceInteractions(),
                                       DuplicateSurfaceInteractions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteSurfaceInteraction_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Surface interactions", _controller.GetAllSurfaceInteractions(), DeleteSurfaceInteractions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }

        }
        //
        private void EditSurfaceInteraction(string surfaceInteractionName)
        {
            ShowForm(_frmSurfaceInteraction, "Edit surface interaction", surfaceInteractionName);
        }
        private void DuplicateSurfaceInteractions(string[] surfaceInteractionNames)
        {
            _controller.DuplicateSurfaceInteractionsCommand(surfaceInteractionNames);
        }
        private void DeleteSurfaceInteractions(string[] surfaceInteractionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected surface interactions?" + Environment.NewLine
                                                 + surfaceInteractionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveSurfaceInteractionsCommand(surfaceInteractionNames);
            }
        }



        #endregion  ################################################################################################################

        #region Contact pair menu  #################################################################################################
        private void tsmiCreateContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmContactPair);
                ShowForm(_frmContactPair, "Create Contact Pair", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Contact pairs", _controller.GetAllContactPairs(), EditContactPair);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), DuplicateContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSwapMasterSlaveContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), SwapMasterSlaveContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeByMasterSlaveContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), MergeByMasterSlaveContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), HideContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), ShowContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), DeleteContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditContactPair(string contactPairName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmContactPair);
            ShowForm(_frmContactPair, "Edit Contact Pair", contactPairName);
        }
        private void DuplicateContactPairs(string[] contactPairNames)
        {
            _controller.DuplicateContactPairsCommand(contactPairNames);
        }
        private void SwapMasterSlaveContactPairs(string[] contactPairNames)
        {
            _controller.SwapMasterSlaveContactPairsCommand(contactPairNames);
        }
        private void MergeByMasterSlaveContactPairs(string[] contactPairNames)
        {
            _controller.MergeByMasterSlaveContactPairsCommand(contactPairNames);
        }
        private void HideContactPairs(string[] contactPairNames)
        {
            _controller.HideContactPairsCommand(contactPairNames);
        }
        private void ShowContactPairs(string[] contactPairNames)
        {
            _controller.ShowContactPairsCommand(contactPairNames);
        }
        private void ShowOnlyContactPairs(string[] contactPairNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.ContactPairs.Keys);
            allNames.ExceptWith(contactPairNames);
            _controller.HideContactPairsCommand(allNames.ToArray());
            _controller.ShowContactPairsCommand(contactPairNames);
        }
        private void DeleteContactPairs(string[] contactPairNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected contact pairs?" + Environment.NewLine
                                                 + contactPairNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveContactPairsCommand(contactPairNames);
            }
        }
        #endregion  ################################################################################################################

        private void tsmiSearchContactPairs_Click(object sender, EventArgs e)
        {
            try
            {
                SearchContactPairs();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void SearchContactPairs()
        {
            if (_controller.Model == null || _controller.Model.Mesh == null) return;
            //
            if (!_frmSearchContactPairs.Visible)
            {
                CloseAllForms();
                SetFormLocation(_frmSearchContactPairs);
                _frmSearchContactPairs.PrepareForm();
                _frmSearchContactPairs.Show(this);
            }
        }

        #endregion  ################################################################################################################

        #region Amplitude menu  ####################################################################################################
        private void tsmiCreateAmplitude_Click(object sender, EventArgs e)
        {
            try
            {
                CreateAmplitude();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditAmplitude_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Amplitudes", _controller.GetAllAmplitudes(), EditAmplitude);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateAmplitude_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Amplitudes", _controller.GetAllAmplitudes(), DuplicateAmplitudes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteAmplitude_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Amplitudes", _controller.GetAllAmplitudes(), DeleteAmplitudes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateAmplitude()
        {
            if (_controller.Model.Mesh == null) return;
            ShowForm(_frmAmplitude, "Create Amplitude", null);
        }
        private void EditAmplitude(string amplitudeName)
        {
            ShowForm(_frmAmplitude, "Edit Amplitude", amplitudeName);
        }
        private void DuplicateAmplitudes(string[] amplitudeNames)
        {
            _controller.DuplicateAmplitudesCommand(amplitudeNames);
        }
        private void DeleteAmplitudes(string[] amplitudeNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected amplitudes?" + Environment.NewLine
                                                 + amplitudeNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveAmplitudesCommand(amplitudeNames);
            }
        }

        #endregion  ################################################################################################################

        #region Initial condition menu  ############################################################################################
        private void tsmiCreateInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                CreateInitialCondition();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Initial Conditions", _controller.GetAllInitialConditions(), EditInitialCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Initial Conditions", _controller.GetAllInitialConditions(), DuplicateInitialConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPreviewInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Initial Conditions", _controller.GetAllInitialConditions(), PreviewInitialCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Initial Conditions", _controller.GetAllInitialConditions(), HideInitialConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Initial Conditions", _controller.GetAllInitialConditions(), ShowInitialConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Initial Conditions", _controller.GetAllInitialConditions(), DeleteInitialConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateInitialCondition()
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmInitialCondition);
            //
            SinglePointDataEditor.ParentForm = _frmInitialCondition;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmInitialCondition, "Create Initial Condition", null);
        }
        private void EditInitialCondition(string initialConditionName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmInitialCondition);
            //
            SinglePointDataEditor.ParentForm = _frmInitialCondition;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmInitialCondition, "Edit Initial Condition", initialConditionName);
        }
        private void DuplicateInitialConditions(string[] initialConditionNames)
        {
            _controller.DuplicateInitialConditionsCommand(initialConditionNames);
        }
        private void PreviewInitialConditions(string[] initialConditionNames)
        {
            foreach (var name in initialConditionNames) PreviewInitialCondition(name);
        }
        private void PreviewInitialCondition(string initialConditionName)
        {
            _controller.PreviewInitialCondition(initialConditionName);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void HideInitialConditions(string[] initialConditionNames)
        {
            _controller.HideInitialConditionsCommand(initialConditionNames);
        }
        private void ShowInitialConditions(string[] initialConditionNames)
        {
            _controller.ShowInitialConditionsCommand(initialConditionNames);
        }
        private void ShowOnlyInitialConditions(string[] initialConditionNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.InitialConditions.Keys);
            allNames.ExceptWith(initialConditionNames);
            _controller.HideInitialConditionsCommand(allNames.ToArray());
            _controller.ShowInitialConditionsCommand(initialConditionNames);
        }
        private void DeleteInitialConditions(string[] initialConditionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected initial conditions?" + Environment.NewLine
                                                 + initialConditionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveInitialConditionsCommand(initialConditionNames);
            }
        }

        #endregion  ################################################################################################################

        #region Step menu  #########################################################################################################
        internal void tsmiCreateStep_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                //
                int selectedIndex = -1;
                if (e is EventArgs<int> ea) selectedIndex = ea.Value;
                _frmStep.SetPreselectListViewItem(selectedIndex);
                //
                ShowForm(_frmStep, "Create Step", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditStep_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), EditStep);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditStepControls_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), EditStepControls);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateStep_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Steps", _controller.GetAllSteps(), DuplicateSteps);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteStep_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Steps", _controller.GetAllSteps(), DeleteSteps);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void EditStep(string stepName)
        {
            ShowForm(_frmStep, "Edit Step", stepName);
        }
        private void EditStepControls(string stepName)
        {
            ShowForm(_frmStepControls, "Edit Step Controls", stepName, null);
        }
        private void DuplicateSteps(string[] stepNames)
        {
            _controller.DuplicateStepsCommand(stepNames);
        }
        private void DeleteSteps(string[] stepNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected steps?" + Environment.NewLine
                                                 + stepNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveStepsCommand(stepNames);
            }
        }

        #endregion  ################################################################################################################

        #region History output menu  ###############################################################################################
        private void tsmiCreateHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateHistoryOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditHistoryOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateHistoryOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateHistoryOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteHistoryOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditHistoryOutput(string stepName)
        {
            SelectOneEntityInStep("History outputs", _controller.GetAllHistoryOutputs(stepName), stepName, EditHistoryOutput);
        }
        private void SelectAndDuplicateHistoryOutputs(string stepName)
        {
            SelectMultipleEntitiesInStep("History outputs", _controller.GetAllHistoryOutputs(stepName),
                                         stepName, DuplicateHistoryOutputs);
        }
        private void SelectAndPropagateHistoryOutput(string stepName)
        {
            SelectOneEntityInStep("History outputs", _controller.GetAllHistoryOutputs(stepName), stepName, PropagateHistoryOutput);
        }
        private void SelectAndDeleteHistoryOutputs(string stepName)
        {
            SelectMultipleEntitiesInStep("History outputs", _controller.GetAllHistoryOutputs(stepName),
                                         stepName, DeleteHistoryOutputs);
        }
        //
        private void CreateHistoryOutput(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmHistoryOutput);
            ShowForm(_frmHistoryOutput, "Create History Output", stepName, null);
        }
        private void EditHistoryOutput(string stepName, string historyOutputName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmHistoryOutput);
            ShowForm(_frmHistoryOutput, "Edit History Output", stepName, historyOutputName);
        }
        private void DuplicateHistoryOutputs(string stepName, string[] historyOutputNames)
        {
            _controller.DuplicateHistoryOutputsCommand(stepName, historyOutputNames);
        }
        private void PropagateHistoryOutput(string stepName, string historyOutputName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).HistoryOutputs.ContainsKey(historyOutputName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing history output " + historyOutputName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateHistoryOutputCommand(stepName, historyOutputName);
        }
        private void DeleteHistoryOutputs(string stepName, string[] historyOutputNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected history outputs from step " + stepName + "?"
                                                 + Environment.NewLine + historyOutputNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveHistoryOutputsCommand(stepName, historyOutputNames);
            }
        }
        #endregion  ################################################################################################################

        #region Field output menu  #################################################################################################
        private void tsmiCreateFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateFieldOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditFieldOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateFieldOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateFieldOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteFieldOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditFieldOutput(string stepName)
        {
            SelectOneEntityInStep("Field outputs", _controller.GetAllFieldOutputs(stepName), stepName, EditFieldOutput);
        }
        private void SelectAndDuplicateFieldOutputs(string stepName)
        {
            SelectMultipleEntitiesInStep("Field outputs", _controller.GetAllFieldOutputs(stepName),
                                         stepName, DuplicateFieldOutputs);
        }
        private void SelectAndPropagateFieldOutput(string stepName)
        {
            SelectOneEntityInStep("Field outputs", _controller.GetAllFieldOutputs(stepName), stepName, PropagateFieldOutput);
        }
        private void SelectAndDeleteFieldOutputs(string stepName)
        {
            SelectMultipleEntitiesInStep("Field outputs", _controller.GetAllFieldOutputs(stepName), stepName, DeleteFieldOutputs);
        }
        //
        private void CreateFieldOutput(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            //
            ShowForm(_frmFieldOutput, "Create Field Output", stepName, null);
        }
        private void EditFieldOutput(string stepName, string fieldOutputName)
        {
            ShowForm(_frmFieldOutput, "Edit Field Output", stepName, fieldOutputName);
        }
        private void DuplicateFieldOutputs(string stepName, string[] fieldOutputNames)
        {
            _controller.DuplicateFieldOutputsCommand(stepName, fieldOutputNames);
        }
        private void PropagateFieldOutput(string stepName, string fieldOutputName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).FieldOutputs.ContainsKey(fieldOutputName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing filed output " + fieldOutputName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateFieldOutputCommand(stepName, fieldOutputName);
        }
        private void DeleteFieldOutputs(string stepName, string[] fieldOutputNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected field outputs from step " + stepName + "?"
                                                 + Environment.NewLine + fieldOutputNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveFieldOutputsCommand(stepName, fieldOutputNames);
            }
        }

        #endregion  ################################################################################################################

        #region Boundary condition menu  ###########################################################################################
        internal void tsmiCreateBC_Click(object sender, EventArgs e)
        {
            try
            {
                int selectedIndex = -1;
                if (e is EventArgs<int> ea) selectedIndex = ea.Value;
                _frmBoundaryCondition.SetPreselectListViewItem(selectedIndex);
                //
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndHideBoundaryConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndShowBoundaryConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditBoundaryCondition(string stepName)
        {
            SelectOneEntityInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName), stepName,
                                  EditBoundaryCondition);
        }
        private void SelectAndDuplicateBoundaryCondition(string stepName)
        {
            SelectMultipleEntitiesInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                         stepName, DuplicateBoundaryConditions);
        }
        private void SelectAndPropagateBoundaryCondition(string stepName)
        {
            SelectOneEntityInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName), stepName,
                                  PropagateBoundaryCondition);
        }
        private void SelectAndHideBoundaryConditions(string stepName)
        {
            SelectMultipleEntitiesInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                         stepName, HideBoundaryConditions);
        }
        private void SelectAndShowBoundaryConditions(string stepName)
        {
            SelectMultipleEntitiesInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                         stepName, ShowBoundaryConditions);
        }
        private void SelectAndDeleteBoundaryCondition(string stepName)
        {
            SelectMultipleEntitiesInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                         stepName, DeleteBoundaryConditions);
        }
        //
        private void CreateBoundaryCondition(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmBoundaryCondition);
            ShowForm(_frmBoundaryCondition, "Create Boundary Condition", stepName, null);
        }
        private void EditBoundaryCondition(string stepName, string boundaryConditionName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmBoundaryCondition);
            ShowForm(_frmBoundaryCondition, "Edit Boundary Condition", stepName, boundaryConditionName);
        }
        private void DuplicateBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.DuplicateBoundaryConditionsCommand(stepName, boundaryConditionNames);
        }
        private void PropagateBoundaryCondition(string stepName, string boundaryConditionName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).BoundaryConditions.ContainsKey(boundaryConditionName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing boundary condition " + boundaryConditionName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateBoundaryConditionCommand(stepName, boundaryConditionName);
        }
        private void HideBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.HideBoundaryConditionCommand(stepName, boundaryConditionNames);
        }
        private void ShowBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.ShowBoundaryConditionCommand(stepName, boundaryConditionNames);
        }
        private void ShowOnlyBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            HashSet<string> allNames =
                new HashSet<string>(_controller.Model.StepCollection.GetStep(stepName).BoundaryConditions.Keys);
            allNames.ExceptWith(boundaryConditionNames);
            _controller.HideBoundaryConditionCommand(stepName, allNames.ToArray());
            _controller.ShowBoundaryConditionCommand(stepName, boundaryConditionNames);
        }
        private void DeleteBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected boundary conditions from step " + stepName + "?"
                                                 + Environment.NewLine + boundaryConditionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveBoundaryConditionsCommand(stepName, boundaryConditionNames);
            }
        }

        #endregion  ################################################################################################################

        #region Load menu  #########################################################################################################
        internal void tsmiCreateLoad_Click(object sender, EventArgs e)
        {
            try
            {
                int selectedIndex = -1;
                if (e is EventArgs<int> ea) selectedIndex = ea.Value;
                _frmLoad.SetPreselectListViewItem(selectedIndex);
                //
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateLoad);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditLoad);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateLoads);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateLoad);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPreviewLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPreviewLoad);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndHideLoads);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndShowLoads);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteLoads);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditLoad(string stepName)
        {
            SelectOneEntityInStep("Loads", _controller.GetStepLoads(stepName), stepName, EditLoad);
        }
        private void SelectAndDuplicateLoads(string stepName)
        {
            SelectMultipleEntitiesInStep("Loads", _controller.GetStepLoads(stepName), stepName, DuplicateLoads);
        }
        private void SelectAndPropagateLoad(string stepName)
        {
            SelectOneEntityInStep("Loads", _controller.GetStepLoads(stepName), stepName, PropagateLoad);
        }
        private void SelectAndPreviewLoad(string stepName)
        {
            SelectOneEntityInStep("Loads", _controller.GetStepLoads(stepName), stepName, PreviewLoad);
        }
        private void SelectAndHideLoads(string stepName)
        {
            SelectMultipleEntitiesInStep("Loads", _controller.GetStepLoads(stepName), stepName, HideLoads);
        }
        private void SelectAndShowLoads(string stepName)
        {
            SelectMultipleEntitiesInStep("Loads", _controller.GetStepLoads(stepName), stepName, ShowLoads);
        }
        private void SelectAndDeleteLoads(string stepName)
        {
            SelectMultipleEntitiesInStep("Loads", _controller.GetStepLoads(stepName), stepName, DeleteLoads);
        }
        //
        private void CreateLoad(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmLoad);
            //
            SinglePointDataEditor.ParentForm = _frmLoad;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmLoad, "Create Load", stepName, null);
        }
        private void EditLoad(string stepName, string loadName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmLoad);
            //
            SinglePointDataEditor.ParentForm = _frmLoad;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmLoad, "Edit Load", stepName, loadName);
        }
        private void DuplicateLoads(string stepName, string[] loadNames)
        {
            _controller.DuplicateLoadsCommand(stepName, loadNames);
        }
        private void PropagateLoad(string stepName, string loadName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).Loads.ContainsKey(loadName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing load " + loadName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateLoadCommand(stepName, loadName);
        }
        private void PreviewLoad(string stepName, string loadName)
        {
            _controller.PreviewLoad(stepName, loadName);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void HideLoads(string stepName, string[] loadNames)
        {
            _controller.HideLoadsCommand(stepName, loadNames);
        }
        private void ShowLoads(string stepName, string[] loadNames)
        {
            _controller.ShowLoadsCommand(stepName, loadNames);
        }
        private void ShowOnlyLoads(string stepName, string[] loadNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.StepCollection.GetStep(stepName).Loads.Keys);
            allNames.ExceptWith(loadNames);
            _controller.HideLoadsCommand(stepName, allNames.ToArray());
            _controller.ShowLoadsCommand(stepName, loadNames);
        }
        private void DeleteLoads(string stepName, string[] loadNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected loads from step " + stepName + "?"
                                                 + Environment.NewLine + loadNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveLoadsCommand(stepName, loadNames);
            }
        }

        #endregion  ################################################################################################################

        #region Defined field menu #################################################################################################
        private void tsmiCreateDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateDefinedField);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditDefinedField);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateDefinedFields);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateDefinedField);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPreviewDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPreviewDefinedField);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndHideDefinedFields);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndShowDefinedFields);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteDefinedFields);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditDefinedField(string stepName)
        {
            SelectOneEntityInStep("Defined fields", _controller.GetStepDefinedFields(stepName), stepName, EditDefinedField);
        }
        private void SelectAndDuplicateDefinedFields(string stepName)
        {
            SelectMultipleEntitiesInStep("Defined fields", _controller.GetStepDefinedFields(stepName),
                                         stepName, DuplicateDefinedFields);
        }
        private void SelectAndPropagateDefinedField(string stepName)
        {
            SelectOneEntityInStep("Defined fields", _controller.GetStepDefinedFields(stepName), stepName, PropagateDefinedField);
        }
        private void SelectAndPreviewDefinedField(string stepName)
        {
            SelectOneEntityInStep("Defined fields", _controller.GetStepDefinedFields(stepName), stepName, PreviewDefinedField);
        }
        private void SelectAndHideDefinedFields(string stepName)
        {
            SelectMultipleEntitiesInStep("Defined fields", _controller.GetStepDefinedFields(stepName),
                                         stepName, HideDefinedFields);
        }
        private void SelectAndShowDefinedFields(string stepName)
        {
            SelectMultipleEntitiesInStep("Defined fields", _controller.GetStepDefinedFields(stepName),
                                         stepName, ShowDefinedFields);
        }
        private void SelectAndDeleteDefinedFields(string stepName)
        {
            SelectMultipleEntitiesInStep("Defined fields", _controller.GetStepDefinedFields(stepName),
                                         stepName, DeleteDefinedFields);
        }
        //
        private void CreateDefinedField(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmDefinedField);
            ShowForm(_frmDefinedField, "Create Defined Field", stepName, null);
        }
        private void EditDefinedField(string stepName, string definedFieldName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmDefinedField);
            ShowForm(_frmDefinedField, "Edit Defined Field", stepName, definedFieldName);
        }
        private void DuplicateDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.DuplicateDefinedFieldsForStepCommand(stepName, definedFieldNames);
        }
        private void PropagateDefinedField(string stepName, string definedFieldName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).DefinedFields.ContainsKey(definedFieldName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing defined field " + definedFieldName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateDefinedFieldCommand(stepName, definedFieldName);
        }
        private void PreviewDefinedField(string stepName, string definedFieldName)
        {
            _controller.PreviewDefinedField(stepName, definedFieldName);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void HideDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.HideDefinedFieldsCommand(stepName, definedFieldNames);
        }
        private void ShowDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.ShowDefinedFieldsCommand(stepName, definedFieldNames);
        }
        private void ShowOnlyDefinedFields(string stepName, string[] definedFieldNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.StepCollection.GetStep(stepName).DefinedFields.Keys);
            allNames.ExceptWith(definedFieldNames);
            _controller.HideDefinedFieldsCommand(stepName, allNames.ToArray());
            _controller.ShowDefinedFieldsCommand(stepName, definedFieldNames);
        }
        private void DeleteDefinedFields(string stepName, string[] definedFieldNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected defined fields from step " + stepName + "?"
                                                 + Environment.NewLine + definedFieldNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveDefinedFieldsForStepCommand(stepName, definedFieldNames);
            }
        }
        
        #endregion  ################################################################################################################

        #region Tools ##############################################################################################################
        private void tsmiSettings_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_frmSettings.Visible)
                {
                    CloseAllForms();
                    SetFormLocation(_frmSettings);
                    _frmSettings.PrepareForm(_controller);
                    _frmSettings.Show();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
           
        }
        private void tsmiParameters_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model != null)
                {
                    FrmParametersEditor fpe = new FrmParametersEditor(_controller);
                    fpe.Icon = Icon;
                    fpe.Owner = this;
                    CloseAllForms();
                    SetFormLocation(fpe);
                    fpe.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiQuery_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_frmQuery.Visible)
                {
                    ClearSelection();
                    //
                    CloseAllForms();
                    SetFormLocation(_frmQuery);
                    _frmQuery.PrepareForm(_controller);
                    _frmQuery.Show();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiFind_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_frmFind.Visible)
                {
                    ClearSelection();
                    //
                    CloseAllForms();
                    SetFormLocation(_frmFind);
                    _frmFind.PrepareForm(_controller);
                    _frmFind.Show();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void ShowColorBarSettings()
        {
            if (_controller.AnnotateWithColor == AnnotateWithColorEnum.FaceOrientation ||
                _controller.AnnotateWithColor == AnnotateWithColorEnum.Parts ||
                _controller.AnnotateWithColor == AnnotateWithColorEnum.Materials ||
                _controller.AnnotateWithColor == AnnotateWithColorEnum.Sections ||
                _controller.AnnotateWithColor == AnnotateWithColorEnum.SectionThicknesses)
            {
                _frmSettings.SetSettingsToShow(Globals.ColorSettingsName);
            }
            else
            {
                _frmSettings.SetSettingsToShow(Globals.PreSettingsName);
            }
            tsmiSettings_Click(null, null);
        }
        private void ShowAnnotationSettings()
        {
            _frmSettings.SetSettingsToShow(Globals.AnnotationSettingsName);
            tsmiSettings_Click(null, null);
        }
        private void ShowLegendSettings()
        {
            _frmSettings.SetSettingsToShow(Globals.LegendSettingsName);
            tsmiSettings_Click(null, null);
        }
        private void ShowStatusBlockSettings()
        {
            _frmSettings.SetSettingsToShow(Globals.StatusBlockSettingsName);
            tsmiSettings_Click(null, null);
        }
        // Annotations
        private void StartEditArrowAnnotation(string name, Rectangle rectangle)
        {
            if (AnnotationContainer.IsAnnotationNameReserved(name)) return;
            //
            AnnotationBase annotation = _controller.Annotations.GetCurrentAnnotation(name);
            string text = annotation.GetAnnotationText();
            rectangle.Offset(-4, 4);
            //
            Point vtkLocation = this.PointToClient(_vtk.PointToScreen(_vtk.Location));
            Point location = new Point(vtkLocation.X + rectangle.X,
                                       vtkLocation.Y + (_vtk.Height - rectangle.Y - rectangle.Height));
            Rectangle vtkArea = new Rectangle(vtkLocation, _vtk.Size);
            //
            aeAnnotationTextEditor.Location = location;
            aeAnnotationTextEditor.Size = rectangle.Size;
            aeAnnotationTextEditor.MinSize = rectangle.Size;
            aeAnnotationTextEditor.ParentArea = vtkArea;
            aeAnnotationTextEditor.Text = text;
            aeAnnotationTextEditor.BringToFront();
            aeAnnotationTextEditor.Visible = true;
            aeAnnotationTextEditor.Tag = annotation;
            //
            _vtk.SelectBy = vtkSelectBy.Widget;
            //_vtk.DisableInteractor = true;
        }
        private void EndEditArrowAnnotation()
        {
            if (aeAnnotationTextEditor.Visible)
            {
                AnnotationBase annotation = (AnnotationBase)aeAnnotationTextEditor.Tag;
                string nonOverriddenText = annotation.GetNotOverriddenAnnotationText();
                //
                nonOverriddenText = nonOverriddenText.Replace("\r\n", "\n");
                string newText = aeAnnotationTextEditor.Text.Replace("\r\n", "\n");
                //
                if (newText.Length > 0 && newText != nonOverriddenText)
                    annotation.OverriddenText = aeAnnotationTextEditor.Text;
                else
                    annotation.OverriddenText = null;
                //
                aeAnnotationTextEditor.Visible = false;
                //
                //_vtk.DisableInteractor = false;
                _vtk.SelectBy = vtkSelectBy.Default;
                //
                _controller.Annotations.DrawAnnotations();  // redraw in both cases
            }
        }
        public override void LeftMouseDownOnForm(Control sender)
        {
            if (aeAnnotationTextEditor.Visible && !aeAnnotationTextEditor.IsOrContainsControl(sender))
                EndEditArrowAnnotation();
        }
        public void AnnotationPicked(MouseEventArgs e, Keys modifierKeys, string annotationName, Rectangle annotationRectangle)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                StartEditArrowAnnotation(annotationName, annotationRectangle);
            }
            else if (e.Button == MouseButtons.Right)
            {
                tsmiDeleteAnnotation.Tag = new object[] { annotationName, annotationRectangle };
                //
                bool reserved = AnnotationContainer.IsAnnotationNameReserved(annotationName);
                tsmiEditAnnotation.Enabled = !reserved;
                tsmiResetAnnotation.Enabled = !reserved;
                tsmiDeleteAnnotation.Enabled = !reserved;
                cmsAnnotation.Show(_vtk, new Point(e.X, _vtk.Height-  e.Y));
            }
        }
        private void tsmiEditAnnotation_Click(object sender, EventArgs e)
        {
            try
            {
                object[] tag = (object[])tsmiDeleteAnnotation.Tag;
                if (tag[0] is string annotationName && tag[1] is Rectangle annotationRectangle)
                {
                    StartEditArrowAnnotation(annotationName, annotationRectangle);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetAnnotation_Click(object sender, EventArgs e)
        {
            try
            {
                object[] tag = (object[])tsmiDeleteAnnotation.Tag;
                if (tag[0] is string annotationName)
                {
                    if (AnnotationContainer.IsAnnotationNameReserved(annotationName)) return;
                    //
                    _controller.Annotations.ResetAnnotation(annotationName);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiAnnotationSettings_Click(object sender, EventArgs e)
        {
            try
            {
                ShowAnnotationSettings();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteAnnotation_Click(object sender, EventArgs e)
        {
            try
            {
                object[] tag = (object[])tsmiDeleteAnnotation.Tag;
                if (tag[0] is string annotationName)
                {
                    if (AnnotationContainer.IsAnnotationNameReserved(annotationName)) return;
                    //
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected annotation?") == DialogResult.OK)
                    {
                        _controller.Annotations.RemoveCurrentArrowAnnotation(annotationName);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Settings
        private void UpdateSettings(Dictionary<string, ISettings> items)
        {
            _controller.Settings = new SettingsContainer(items);
        }

        // Unit system
        private void tsslUnitSystem_Click(object sender, EventArgs e)
        {
            try
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    if (GetCurrentView() == ViewGeometryModelResults.Geometry ||
                        GetCurrentView() == ViewGeometryModelResults.Model)
                    {
                        IfNeededSelectAndSetNewModelProperties();
                    }
                    else SelectResultsUnitSystem();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void IfNeededSelectAndSetNewModelProperties()
        {
            try
            {
                UnitSystemType unitSystemType = _controller.Model.UnitSystem.UnitSystemType;
                ModelSpaceEnum modelSpace = _controller.Model.Properties.ModelSpace;
                // If needed
                if (modelSpace == ModelSpaceEnum.Undefined || unitSystemType == UnitSystemType.Undefined)
                {
                    // Select and set
                    if (SelectNewModelProperties(false)) SetNewModelProperties();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private bool SelectNewModelProperties(bool cancelPossible)
        {
            DialogResult dialogResult = DialogResult.Cancel;
            //
            InvokeIfRequired(() =>
            {
                try
                {                    
                    // Disable the form during regenerate - check that the state is ready
                    if (tsslState.Text != Globals.RegeneratingText)
                    {
                        CloseAllForms();
                        SetFormLocation(_frmNewModel);
                        //_frmNewModel.SetLanguage(_appLanguage);
                        //
                        if (_frmNewModel.PrepareForm("", "New Model"))
                        {
                            _frmNewModel.SetCancelPossible(cancelPossible);
                            dialogResult = _frmNewModel.ShowDialog(this);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionTools.Show(this, ex);
                }
            });
            //
            return dialogResult == DialogResult.OK;
        }
        private void SetNewModelProperties()
        {
            InvokeIfRequired(() =>
            {
                try
                {
                    if (_frmNewModel.ModelSpace.IsTwoD())
                    {
                        if ((_controller.Model.Geometry != null && !_controller.Model.Geometry.BoundingBox.Is2D())
                            || (_controller.Model.Mesh != null && !_controller.Model.Mesh.BoundingBox.Is2D()))
                            throw new CaeException("Use of the 2D model space is not possible. The geometry or the mesh " +
                                                   "do not contain 2D geometry in x-y plane.");
                    }
                    _controller.SetNewModelPropertiesCommand(_frmNewModel.ModelSpace, _frmNewModel.UnitSystem.UnitSystemType);
                }
                catch (Exception ex)
                {
                    ExceptionTools.Show(this, ex);
                }
            });
        }
        public void SelectResultsUnitSystem()
        {
            InvokeIfRequired(() =>
            {
                try
                {
                    // Disable unit system selection during regenerate - check that the state is ready
                    if (tsslState.Text != Globals.RegeneratingText)
                    {
                        UnitSystemType unitSystemType = _controller.CurrentResult.UnitSystem.UnitSystemType;
                        //
                        if (unitSystemType == UnitSystemType.Undefined)
                        {
                            CloseAllForms();
                            SetFormLocation(_frmNewModel);
                            //
                            if (_frmNewModel.PrepareForm("", "Results"))
                            {
                                if (_frmNewModel.ShowDialog(this) == DialogResult.OK)
                                {
                                    _controller.SetResultsUnitSystem(_frmNewModel.UnitSystem.UnitSystemType);
                                }
                                else throw new NotSupportedException();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionTools.Show(this, ex);
                }
            });
        }
        public void UpdateUnitSystem(UnitSystem unitSystem)
        {
            tsslUnitSystem.Text = "Unit system: " + unitSystem.UnitSystemType.GetDescription();
            //
            SetScaleWidgetUnit(unitSystem);
        }
        #endregion  ################################################################################################################

        #region Analysis menu  #####################################################################################################
        private void tsmiCreateAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                ShowForm(_frmAnalysis, "Create Analysis", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), EditAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Analyses", _controller.GetAllJobs(), DuplicateAnalyses);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void tsmiRunAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), RunAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCheckModel_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), CheckModel);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMonitorAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), MonitorAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void tsmiResultsAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), ResultsAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiKillAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), KillAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Analyses", _controller.GetAllJobs(), DeleteAnalyses);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditAnalysis(string jobName)
        {
            ShowForm(_frmAnalysis, "Edit Analysis", jobName);
        }
        private void DuplicateAnalyses(string[] jobNames)
        {
            _controller.DuplicateJobsCommand(jobNames);
        }
        private void RunAnalysis(string jobName)
        {
            RunAnalysis(jobName, false);
        }
        private void CheckModel(string jobName)
        {
            RunAnalysis(jobName, true);
        }
        private void RunAnalysis(string jobName, bool onlyCheckModel)
        {
            // Check validity
            if (CheckValidity())
            {
                string workDirectory = _controller.Settings.GetWorkDirectory();
                //
                if (workDirectory == null || !Directory.Exists(workDirectory))
                    throw new Exception("The work directory of the analysis does not exist.");
                //
                AnalysisJob job = _controller.GetJob(jobName);
                if (job.JobStatus != JobStatus.Running)
                {
                    string inputFileName = _controller.GetCalculiXInpFileName(jobName);
                    if (File.Exists(inputFileName))
                    {
                        if (MessageBoxes.ShowWarningQuestionOKCancel("Overwrite existing analysis files?") != DialogResult.OK) return;
                    }
                    //
                    if (_controller.PrepareAndRunJobCommand(jobName, onlyCheckModel)) MonitorAnalysis(jobName);
                }
                else MessageBoxes.ShowError("The analysis is already running or is in queue.");
            }
        }
        private void MonitorAnalysis(string jobName)
        {
            try
            {
                CloseAllForms();
                SetFormLocation(_frmMonitor);
                _frmMonitor.PrepareForm(jobName);
                _frmMonitor.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void ResultsAnalysis(string jobName)
        {
            await Task.Run(() => _controller.OpenResultsCommand(jobName));
            //
            RunHistoryPostprocessing();
        }
        public void RunHistoryPostprocessing()
        {
            try
            {
                CloseAllForms();
                Application.DoEvents();
                SetStateWorking(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = false;
                _controller.RunHistoryPostprocessing();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = true;
                RegenerateTree();
                //
                SetMenuAndToolStripVisibility();
                // During regeneration the tree is empty
                if (_controller.CurrentResult != null) SelectFirstComponentOfFirstFieldOutput();
                //
                SetZoomToFit(true);
            }
        }
        public async void OpenAnalysisResults(string jobName, bool asynchronous = true)
        {
            AnalysisJob job = _controller.GetJob(jobName);
            //
            bool openResults = true;
            string parameters = Globals.FromMonitorForm;
            //
            if (!_controller.BatchRegenerationMode)
            {
                if (job.JobStatus == JobStatus.Running)
                {
                    string question = $"The analysis {job.Name} is still running. Continue?";
                    if (MessageBoxes.ShowWarningQuestionOKCancel(question) == DialogResult.OK)
                    {
                        openResults = true;
                        parameters += Globals.NameSeparator + Globals.OpenRunningJobResults;
                    }
                }
                else if (!job.IsUpToDate)
                {
                    string question = $"The result file was modified after the analysis {job.Name} completed. Continue?";
                    openResults = MessageBoxes.ShowWarningQuestionOKCancel(question) == DialogResult.OK;
                }
            }
            //
            if (openResults)
            {
                string resultsFile = job.ResultsFileName;
                //
                if (asynchronous)
                    await OpenAsync(Path.Combine(job.WorkDirectory, resultsFile),
                                    _controller.Open, false, CloseMonitorWindow,
                                    parameters);
                else
                    OpenAsync(Path.Combine(job.WorkDirectory, resultsFile),
                              _controller.Open, false, CloseMonitorWindow,
                              parameters).Wait();
            }
        }
        private void CloseMonitorWindow()
        {
            // This hides the monitor window
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
                _frmMonitor.DialogResult = DialogResult.OK;
        }
        private void KillAnalysis(string jobName)
        {
            if (_controller.GetJob(jobName).JobStatus == JobStatus.Running)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to kill selected analysis?") == DialogResult.OK)
                {
                    _controller.KillJob(jobName);
                }
            }
            else
            {
                MessageBoxes.ShowError("The analysis is not running.");
            }
        }
        private void DeleteAnalyses(string[] jobNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected analyses?" + Environment.NewLine
                                                 + jobNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveJobsCommand(jobNames);
            }
        }
        public void UpdateAnalysisProgress()
        {
            _frmMonitor.UpdateProgress();
        }
        //
        public AnalysisJob GetDefaultJob()
        {
            try
            {
                AnalysisJob analysisJob = null;
                //
                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate () { GetDefaultJob(out analysisJob); });
                }
                else
                {
                    GetDefaultJob(out analysisJob);
                }
                //
                return analysisJob;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
                return null;
            }
        }
        private void GetDefaultJob(out AnalysisJob defaultJob)
        {
            _frmAnalysis.PrepareForm(null, null);
            defaultJob = _frmAnalysis.Job;
        }
        public string GetDefaultJobName()
        {
            string name = null;
            //
            if (_controller.OpenedFileName != null)
            {
                name = NamedClass.GetErrorFreeName(Path.GetFileNameWithoutExtension(_controller.OpenedFileName),
                                                   "Analysis", null);
            }
            return name;
        }

        #endregion  ################################################################################################################

        #region Results  ###########################################################################################################
        public void ViewResultHistoryOutputData(HistoryResultData historyData)
        {
            try
            {
                if (!_frmViewResultHistoryOutput.Visible)
                {
                    CloseAllForms();
                    SetFormLocation(_frmViewResultHistoryOutput);
                    //
                    string[] columnNames;
                    object[][] rowBasedData;
                    _controller.GetHistoryOutputData(historyData, out columnNames, out rowBasedData, false);
                    //
                    _frmViewResultHistoryOutput.SetData(columnNames, rowBasedData);
                    _frmViewResultHistoryOutput.Show();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiTransformation_Click(object sender, EventArgs e)
        {
            try
            {
                SinglePointDataEditor.ParentForm = _frmTransformation;
                SinglePointDataEditor.Controller = _controller;
                //
                ShowForm(_frmTransformation, "Create Transformation", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiAppendResults_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Calculix result files|*.frd";
                    openFileDialog.Multiselect = false;
                    //
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        foreach (var fileName in openFileDialog.FileNames)
                        {
                            // Do not use: if (CheckBeforeOpen(fileName))
                            if (File.Exists(fileName)) await OpenAsync(fileName, _controller.AppendResult);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
                _controller.ModelChanged = false;   // hide message box
                tsmiNew_Click(null, null);
            }
        }

        #endregion  ################################################################################################################

        #region Result part menu  ##################################################################################################
        private void tsmiEditResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetResultParts(), EditResultPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeResultPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), MergeResultParts, 2);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), HideResultParts);
                Clear3DSelection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), ShowResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), ShowOnlyResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetColorForResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), SetColorForResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetColorForResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), ResetColorForResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetTransparencyForResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), SetTransparencyForResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiColorContoursOff_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts<ResultPart>(), ColorContoursOffResultPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiColorContoursOn_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts<ResultPart>(), ColorContoursOnResultPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), DeleteResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditResultPart(string partName)
        {
            _frmPartProperties.View = ViewGeometryModelResults.Results;
            ShowForm(_frmPartProperties, "Edit Part", partName);
        }
        private void MergeResultParts(string[] partNames)
        {
            if (_controller.AreResultPartsMergeable(partNames))
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to merge selected parts?") == DialogResult.OK)
                {
                    _controller.MergeResultPartsCommand(partNames);
                }
            }
            else MessageBoxes.ShowError("Selected parts are of a different type and thus cannot be merged.");
        }
        private void HideResultParts(string[] partNames)
        {
            _controller.HideResultPartsCommand(partNames);
        }
        private void ShowResultParts(string[] partNames)
        {
            _controller.ShowResultPartsCommand(partNames);
        }
        private void ShowOnlyResultParts(string[] partNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.CurrentResult.Mesh.Parts.Keys);
            allNames.ExceptWith(partNames);
            _controller.ShowResultPartsCommand(partNames);
            _controller.HideResultPartsCommand(allNames.ToArray());
        }
        private void SetColorForResultParts(string[] partNames)
        {
            if (_controller.AllResults.CurrentResult.Mesh == null) return;
            //
            using (FrmGetColor frmGetColor = new FrmGetColor())
            {
                Color color = _controller.AllResults.CurrentResult.Mesh.Parts[partNames[0]].GetProperties().Color;
                SetFormLocation(frmGetColor);
                frmGetColor.PrepareForm("Set Part Color: " + partNames.ToShortString(), color);
                if (frmGetColor.ShowDialog() == DialogResult.OK)
                {
                    _controller.SetColorForResultPartsCommand(partNames, frmGetColor.Color);
                }
                SaveFormLocation(frmGetColor);
            }
        }
        private void ResetColorForResultParts(string[] partNames)
        {
            _controller.ResetColorForResultPartsCommand(partNames);
        }
        private void SetTransparencyForResultParts(string[] partNames)
        {
            if (_controller.CurrentResult == null || _controller.CurrentResult.Mesh == null) return;
            //
            using (FrmGetValue frmGetValue = new FrmGetValue())
            {
                frmGetValue.NumOfDigits = 0;
                frmGetValue.MinValue = 25;
                frmGetValue.MaxValue = 255;
                SetFormLocation(frmGetValue);
                OrderedDictionary<string, double> presetValues =
                    new OrderedDictionary<string, double>("Preset Transparency Values", StringComparer.OrdinalIgnoreCase);
                presetValues.Add("Semi-transparent", 128);
                presetValues.Add("Opaque", 255);
                string desc = "Enter the transparency between 0 and 255.\n" + "(0 - transparent; 255 - opaque)";
                frmGetValue.PrepareForm("Set Transparency: " + partNames.ToShortString(), "Transparency", desc, 128, presetValues);
                if (frmGetValue.ShowDialog() == DialogResult.OK)
                {
                    _controller.SetTransparencyForResultPartsCommand(partNames, (byte)frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void ColorContoursOffResultPart(string[] partNames)
        {
            _controller.SetColorContoursForResultPartsCommand(partNames, false);
        }
        private void ColorContoursOnResultPart(string[] partNames)
        {
            _controller.SetColorContoursForResultPartsCommand(partNames, true);
        }
        private void DeleteResultParts(string[] partNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected parts?" + Environment.NewLine + 
                                                 partNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultPartsCommand(partNames);
            }
        }

        #endregion  ################################################################################################################

        #region Result Reference point  ############################################################################################
        private void tsmiCreateResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.AllResults.CurrentResult.Mesh == null) return;
                //
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmReferencePoint);
                ShowForm(_frmReferencePoint, "Create Result Reference Point", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Result Reference Points", _controller.GetAllResultReferencePoints(), EditResultReferencePoint);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       DuplicateResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       HideResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       ShowResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       ShowOnlyResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       DeleteResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditResultReferencePoint(string referencePointName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmReferencePoint);
            ShowForm(_frmReferencePoint, "Edit Reference Point", referencePointName);
        }
        private void DuplicateResultReferencePoints(string[] referencePointNames)
        {
            _controller.DuplicateResultReferencePointsCommand(referencePointNames);
        }
        private void HideResultReferencePoints(string[] referencePointNames)
        {
            _controller.HideResultReferencePointsCommand(referencePointNames);
        }
        private void ShowResultReferencePoints(string[] referencePointNames)
        {
            _controller.ShowResultReferencePointsCommand(referencePointNames);
        }
        private void ShowOnlyResultReferencePoints(string[] referencePointNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.GetResultReferencePointNames());
            allNames.ExceptWith(referencePointNames);
            _controller.HideResultReferencePointsCommand(allNames.ToArray());
            _controller.ShowResultReferencePointsCommand(referencePointNames);
        }
        private void DeleteResultReferencePoints(string[] referencePointNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected result reference points?" + Environment.NewLine
                                                 + referencePointNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultReferencePointsCommand(referencePointNames);
            }
        }

        #endregion  ################################################################################################################

        #region Result Coordinate system  ##########################################################################################
        private void tsmiCreateResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmCoordinateSystem);
                ShowForm(_frmCoordinateSystem, "Create Result Coordinate System", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                EditResultCoordinateSystem);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       DuplicateResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       HideResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       ShowResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       ShowOnlyResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       DeleteResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditResultCoordinateSystem(string coordinateSystemName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmCoordinateSystem);
            ShowForm(_frmCoordinateSystem, "Edit Coordinate System", coordinateSystemName);
        }
        private void DuplicateResultCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.DuplicateResultCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void HideResultCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.HideResultCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void ShowResultCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.ShowResultCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void ShowOnlyResultCoordinateSystems(string[] coordinateSystemNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.AllResults.CurrentResult.Mesh.CoordinateSystems.Keys);
            allNames.ExceptWith(coordinateSystemNames);
            _controller.HideResultCoordinateSystemsCommand(allNames.ToArray());
            _controller.ShowResultCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void DeleteResultCoordinateSystems(string[] coordinateSystemNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected result coordinate systems?" + Environment.NewLine
                                                 + coordinateSystemNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultCoordinateSystemsCommand(coordinateSystemNames);
            }
        }

        #endregion  ################################################################################################################

        #region Result field output  ###############################################################################################
        private void tsmiCreateResultFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                CreateResultFieldOutput();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditResultFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Field Outputs", _controller.GetResultFieldOutputs(), EditResultFieldOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Field Outputs", _controller.GetVisibleResultFieldOutputsAsNamedItems(),
                                       DeleteResultFieldOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateResultFieldOutput()
        {
            if (_controller.CurrentResult == null || _controller.CurrentResult.Mesh == null) return;
            if (_controller.CurrentResult.GetAllFiledNameComponentNames().Count == 0) return;
            //
            ShowForm(_frmResultFieldOutput, "Create Field Output", null);
        }
        private void EditResultFieldOutput(string resultFieldOutputName)
        {
            ShowForm(_frmResultFieldOutput, "Edit Field Output", resultFieldOutputName);
        }
        public void DeleteResultFieldOutputs(string[] fieldOutputNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected field outputs?" + Environment.NewLine
                                                 + fieldOutputNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultFieldOutputsCommand(fieldOutputNames);
            }
        }
        public void DeleteResultFieldOutputComponents(string fieldOutputName, string[] componentNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected components from field output " + fieldOutputName + "?"
                                                 + Environment.NewLine + componentNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultFieldOutputComponentsCommand(fieldOutputName, componentNames);
            }
        }

        #endregion  ################################################################################################################

        #region Result history output  #############################################################################################
        private void tsmiCreateResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                CreateResultHistoryOutput();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("History Outputs", _controller.GetResultHistoryOutputs(), EditResultHistoryOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                ShowForm(_frmHistoryResultSetExporter, "Export History Outputs", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("History Outputs", _controller.GetResultHistoryOutputsAsNamedItems(),
                                       DeleteResultHistoryOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateResultHistoryOutput()
        {
            if (_controller.CurrentResult == null || _controller.CurrentResult.Mesh == null) return;
            if (_controller.CurrentResult.GetAllFiledNameComponentNames().Count == 0) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmResultHistoryOutput);
            ShowForm(_frmResultHistoryOutput, "Create History Output", null);
        }
        private void EditResultHistoryOutput(string resultHistoryOutputName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmResultHistoryOutput);
            ShowForm(_frmResultHistoryOutput, "Edit History Output", resultHistoryOutputName);
        }
        public void DeleteResultHistoryOutputs(string[] resultHistoryOutputNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected history outputs?" + Environment.NewLine
                                                 + resultHistoryOutputNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultHistoryOutputsCommand(resultHistoryOutputNames);
            }
        }
        public void DeleteResultHistoryFields(string historyResultSetName, string[] historyResultFieldNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected fields from history output " + historyResultSetName + "?"
                                                 + Environment.NewLine + historyResultFieldNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultHistoryFieldsCommand(historyResultSetName, historyResultFieldNames);
            }
        }
        public void DeleteResultHistoryComponents(NamedClass[] items)
        {
            Dictionary<string, List<string>> parentItemNames;
            Dictionary<string, Dictionary<string, List<string>>> parentParentItemNames =
                new Dictionary<string, Dictionary<string, List<string>>>();
            //
            foreach (var item in items)
            {
                if (item is HistoryResultData hrd)
                {
                    if (parentParentItemNames.TryGetValue(hrd.SetName, out parentItemNames))
                    {
                        parentItemNames[hrd.FieldName].Add(hrd.ComponentName);
                    }
                    else
                    {
                        parentParentItemNames.Add(hrd.SetName, new Dictionary<string, List<string>>()
                                                  { { hrd.FieldName, new List<string>() { hrd.ComponentName} } });
                    }
                }
            }
            //
            string[] itemNames;
            foreach (var parentParentEntry in parentParentItemNames)
            {
                foreach (var parentEntry in parentParentEntry.Value)
                {
                    itemNames = parentEntry.Value.ToArray();
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected components from history field " +
                                                         parentEntry.Key + "?" + Environment.NewLine +
                                                         itemNames.ToRows()) == DialogResult.OK)
                    {
                        _controller.RemoveResultHistoryFieldsCommand(parentParentEntry.Key,
                                                                     parentEntry.Key,
                                                                     itemNames);
                    }
                }
            }
        }
        #endregion  ################################################################################################################

        #region Help menu  #########################################################################################################
        private void tsmiHomePage_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Globals.HomePage);
        }
        private void tsmiAbout_Click(object sender, EventArgs e)
        {
            FrmSplash frmSplash = new FrmSplash();
            frmSplash.ShowHelp = true;
            frmSplash.ShowDialog();
        }
        #endregion  ################################################################################################################

        #region Language menu (my code)  #########################################################################################################
        private void tsmiLanguageEnglish_Click(object sender, EventArgs e)
        {
            Console.WriteLine("tsmiLanguageEnglish_Click");

            _appLanguage = "en";
            //--- Button
            this.tsFile.Text = "File";
            this.tsbNew.Text = "New model";
            this.tsbOpen.Text = "Open file";
            this.tsbImport.Text = "Import file";
            this.tsbImport.ToolTipText = "Import file";
            this.tsbSave.Text = "Save to file";
            this.tsViews.Text = "Views";
            this.tsbZoomToFit.Text = "Zoom to fit";
            this.tsbFrontView.Text = "Front view";
            this.tsbBackView.Text = "Back view";
            this.tsbTopView.Text = "Top view";
            this.tsbBottomView.Text = "Bottom view";
            this.tsbLeftView.Text = "Left view";
            this.tsbRightView.Text = "Right view";
            this.tsbNormalView.Text = "Normal view";
            this.tsbVerticalView.Text = "Vertical view";
            this.tsbIsometric.Text = "Isometric view";
            this.tsbShowWireframeEdges.Text = "Wireframe";
            this.tsbShowElementEdges.Text = "Show element edges";
            this.tsbShowModelEdges.Text = "Show model edges";
            this.tsbShowNoEdges.Text = "No edges";
            this.tsbSectionView.Text = "Section view";
            this.tsbExplodedView.Text = "Exploded view";
            this.tsbQuery.Text = "Query";
            this.tsbRemoveAnnotations.Text = "Remove annotations";
            this.tsbShowAllParts.Text = "Show all parts";
            this.tsbHideAllParts.Text = "Hide all parts";
            this.tsbInvertVisibleParts.Text = "Invert visible parts";
            this.tslSymbols.Text = "Symbols";
            this.tscbSymbols.ToolTipText = "Select how symbols are displayed.";
            this.tslResultName.Text = "Result";
            this.tslDeformationVariable.Text = "Variable";
            this.tscbDeformationVariable.ToolTipText = "Select the deformation variable";
            this.tslDeformationType.Text = "Type";
            this.tscbDeformationType.ToolTipText = "Select the deformation type";
            this.tslDeformationFactor.Text = "Factor";
            this.tstbDeformationFactor.Text = "10";
            this.tstbDeformationFactor.ToolTipText = "Enter the deformation scale factor";
            this.tslComplex.Text = "Complex";
            this.tslAngle.Text = "Angle";
            this.tstbAngle.Text = "0 °";
            this.tsResults.Text = "Results";
            this.tsbResultsUndeformed.Text = "Undeformed";
            this.tsbResultsDeformed.Text = "Deformed";
            this.tsbResultsColorContours.Text = "Deformed with color contours";
            this.tsbResultsUndeformedWireframe.Text = "Show undeformed wireframe model";
            this.tsbResultsUndeformedSolid.Text = "Show undeformed solid model";
            this.tsbTransformation.Text = "Transformation";
            this.tsbFirstStepIncrement.Text = "First increment";
            this.tsbPreviousStepIncrement.Text = "Previous increment";
            this.tsbPreviousStepIncrement.ToolTipText = "Previous increment";
            this.tslStepIncrement.Text = "Step, Increment";
            this.tscbStepAndIncrement.ToolTipText = "Select increment";
            this.tsbNextStepIncrement.Text = "Next increment";
            this.tsbNextStepIncrement.ToolTipText = "Next increment";
            this.tsbLastStepIncrement.Text = "Last increment";
            this.tsbAnimate.Text = "Animate";
            
            //--- Menu
            tsmiFile.Text = "File";
                tsmiNew.Text = "New";
                tsmiOpen.Text = "Open";
                tsmiOpenRecent.Text = "Open Recent";
                tsmiRunHistoryFile.Text = "Run History File";
                tsmiImportFile.Text = "Import";
                tsmiSave.Text = "Save";
                tsmiSaveAs.Text = "Save As";
                tsmiExport.Text = "Export";
                tsmiCloseCurrentResult.Text = "Close Current Result";
                tsmiCloseAllResults.Text = "Close All Results";
                tsmiExit.Text = "Exit";
            tsmiEdit.Text = "Edit";
                tsmiUndo.Text = "Undo";
                tsmiRedo.Text = "Redo";
                tsmiEditHistory.Text = "Edit History";
                tsmiRegenerateHistory.Text = "Regenerate History";
                tsmiRegenerateHistoryUsingOtherFiles.Text = "Regenerate History Using Other Files";
                tsmiRegenerateHistoryWithRemeshing.Text = "Regenerate History With Remeshing";
            tsmiView.Text = "View";
                standardViewsToolStripMenuItem.Text = "Standard Views";
                    tsmiFrontView.Text = "Front View";
                    tsmiBackView.Text = "Back View";
                    tsmiTopView.Text = "Top View";
                    tsmiBottomView.Text = "Bottom View";
                    tsmiLeftView.Text = "Left View";
                    tsmiRightView.Text = "Right View";
                    tsmiNormalView.Text = "Normal View";
                    tsmiVerticalView.Text = "Vertical View";
                    tsmiIsometricView.Text = "Isometric View";
                tsmiZoomToFit.Text = "Zoom to Fit";
                tsmiShowWireframeEdges.Text = "Wireframe";
                tsmiShowElementEdges.Text = "Show Element Edges";
                tsmiShowModelEdges.Text = "Show Model Edges";
                tsmiShowNoEdges.Text = "No Edges";
                tsmiSectionView.Text = "Section View";
                tsmiExplodedView.Text = "Exploded View";
                tsmiShowAllParts.Text = "Show All";
                tsmiHideAllParts.Text = "Hide All";
                tsmiInvertVisibleParts.Text = "Invert Visible Parts";
                tsmiResultsUndeformed.Text = "Undeformed";
                tsmiResultsDeformed.Text = "Deformed";
                tsmiResultsColorContours.Text = "Deformed With Color Contours";
                tsmiResultsDeformedColorWireframe.Text = "Deformed && Color && Wireframe";
                tsmiResultsDeformedColorSolid.Text = "Deformed && Color && Solid";
                tsmiColorAnnotations.Text = "Color Annotations";
                    tsmiAnnotateFaceOrientations.Text = "Face Orientations";
                    tsmiAnnotateParts.Text = "Parts";
                    tsmiAnnotateMaterials.Text = "Materials";
                    tsmiAnnotateSections.Text = "Sections";
                    tsmiAnnotateSectionThicknesses.Text = "Section Thicknesses";
                    tsmiAnnotateAllSymbols.Text = "All Symbols";
                    tsmiAnnotateReferencePoints.Text = "Reference Points";
                    tsmiAnnotateConstraints.Text = "Constraints";
                    tsmiAnnotateContactPairs.Text = "Contact Pairs";
                    tsmiAnnotateInitialConditions.Text = "Initial conditions";
                    tsmiAnnotateBCs.Text = "BCs";
                    tsmiAnnotateLoads.Text = "Loads";
                    tsmiAnnotateDefinedFields.Text = "Defined Fields";
            tsmiGeometry.Text = "Geometry";
                tsmiGeometryPart.Text = "Part";
                    tsmiEditGeometryPart.Text = "Edit";
                    tsmiTransformGeometryParts.Text = "Transform";
                        tsmiScaleGeometryParts.Text = "Scale";

                    tsmiCopyGeometryPartsToResults.Text = "Copy Geometry to Results";

            tsmiHideGeometryParts.Text = "Hide";
            tsmiShowGeometryParts.Text = "Show";
            tsmiShowOnlyGeometryParts.Text = "Show Only";
            tsmiSetColorForGeometryParts.Text = "Set Color";
            tsmiResetColorForGeometryParts.Text = "Reset Color";
            tsmiSetTransparencyForGeometryParts.Text = "Set Transparency";
            tsmiDeleteGeometryParts.Text = "Delete";
            cADPartToolStripMenuItem.Text = "CAD Part";
            tsmiFlipFaceNormalCAD.Text = "Flip Face Normal";
            tsmiSplitAFaceUsingTwoPoints.Text = "Split a Face Using Two Points";
            tsmiDefeature.Text = "Defeature";
            tsmiStlPart.Text = "Stl Part";
            tsmiFindStlEdgesByAngleForGeometryParts.Text = "Find Model Edges by Angle";
            tsmiFlipStlPartFaceNormal.Text = "Flip Part Face Normals";
            tsmiSmoothStlPart.Text = "Smooth Part";
            tsmiDeleteStlPartFaces.Text = "Delete Part Faces";
            tsmiCropStlPartWithCylinder.Text = "Crop With Cylinder";
            tsmiCropStlPartWithCube.Text = "Crop With Cube";
            tsmiCreateAndImportCompoundPart.Text = "Create Compound Part";
            tsmiRegenerateCompoundPart.Text = "Regenerate Compound Part";
            tsmiSwapGeometryPartGeometries.Text = "Swap Part Geometries";
            tsmiGeometryAnalyze.Text = "Analyze";
            tsmiMesh.Text = "Mesh";
            this.tsmiMeshSetupItem.Text = "Mesh Setup Item";
            this.tsmiCreateMeshSetupItem.Text = "Create";
            this.tsmiEditMeshSetupItem.Text = "Edit";
            this.tsmiDuplicateMeshSetupItem.Text = "Duplicate";
            this.tsmiDeleteMeshSetupItem.Text = "Delete";
            this.tsmiPreviewEdgeMesh.Text = "Preview Edge Mesh";
            this.tsmiCreateMesh.Text = "Create Mesh";
            this.tsmiModel.Text = "Model";
            this.tsmiEditModel.Text = "Edit";
            this.tsmiEditCalculiXKeywords.Text = "Edit CalculiX Keywords";
            this.tsmiToolsParts.Text = "Tools";
            this.tsmiFindEdgesByAngleForModelParts.Text = "Find Model Edges By Angle";
            this.tsmiCreateBoundaryLayer.Text = "Create Boundary Layer";
            this.tsmiRemeshElements.Text = "Remesh Elements";
            this.tsmiThickenShellMesh.Text = "Thicken Shell Mesh";
            this.tsmiSplitPartMeshUsingSurface.Text = "Split Part Mesh Using Surface";
            this.tsmiUpdateNodalCoordinatesFromFile.Text = "Update Nodal Coordinates From File";
            this.tsmiNode.Text = "Node";
            this.tsmiRenumberAllNodes.Text = "Renumber All";
            this.tsmiMergeCoincidentNodes.Text = "Merge Coincident Nodes";
            this.tsmiElement.Text = "Element";
            this.tsmiRenumberAllElements.Text = "Renumber All";
            this.tsmiElementQuality.Text = "Element Quality";
            this.tsmiPart.Text = "Part";
            this.tsmiEditModelPart.Text = "Edit";
            this.tsmiTransformModelParts.Text = "Transform";
            this.tsmiTranslateModelParts.Text = "Translate";
            this.tsmiScaleModelParts.Text = "Scale";
            this.tsmiRotateModelParts.Text = "Rotate";
            this.tsmiMergeModelParts.Text = "Merge";
            this.tsmiHideModelParts.Text = "Hide";
            this.tsmiShowModelParts.Text = "Show";
            this.tsmiShowOnlyModelParts.Text = "Show Only";
            this.tsmiSetColorForModelParts.Text = "Set Color";
            this.tsmiResetColorForModelParts.Text = "Reset Color";
            this.tsmiSetTransparencyForModelParts.Text = "Set Transparency";
            this.tsmiDeleteModelParts.Text = "Delete";
            this.tsmiNodeSet.Text = "Node set";
            this.tsmiCreateNodeSet.Text = "Create";
            this.tsmiEditNodeSet.Text = "Edit";
            this.tsmiDuplicateNodeSet.Text = "Duplicate";
            this.tsmiDeleteNodeSet.Text = "Delete";
            this.tsmiElementSet.Text = "Element set";
            this.tsmiCreateElementSet.Text = "Create";
            this.tsmiEditElementSet.Text = "Edit";
            this.tsmiDuplicateElementSet.Text = "Duplicate";
            this.tsmiConvertElementSetsToMeshParts.Text = "Convert to Part";
            this.tsmiDeleteElementSet.Text = "Delete";
            this.tsmiSurface.Text = "Surface";
            this.tsmiCreateSurface.Text = "Create";
            this.tsmiEditSurface.Text = "Edit";
            this.tsmiDuplicateSurface.Text = "Duplicate";
            this.tsmiDeleteSurface.Text = "Delete";
            this.tsmiModelFeatures.Text = "Features";
            this.tsmiModelReferencePointTool.Text = "Reference point";
            this.tsmiCreateModelReferencePoint.Text = "Create";
            this.tsmiEditModelReferencePoint.Text = "Edit";
            this.tsmiDuplicateModelReferencePoint.Text = "Duplicate";
            this.tsmiHideModelReferencePoint.Text = "Hide";
            this.tsmiShowModelReferencePoint.Text = "Show";
            this.tsmiShowOnlyModelReferencePoint.Text = "Show Only";
            this.tsmiDeleteModelReferencePoint.Text = "Delete";
            this.tsmiModelCoordinateSystem.Text = "Coordinate System";
            this.tsmiCreateModelCoordinateSystem.Text = "Create";
            this.tsmiEditModelCoordinateSystem.Text = "Edit";
            this.tsmiDuplicateModelCoordinateSystem.Text = "Duplicate";
            this.tsmiHideModelCoordinateSystem.Text = "Hide";
            this.tsmiShowModelCoordinateSystem.Text = "Show";
            this.tsmiShowOnlyModelCoordinateSystem.Text = "Show Only";
            this.tsmiDeleteModelCoordinateSystem.Text = "Delete";
            this.tsmiProperty.Text = "Property";
            this.tsmiMaterial.Text = "Material";
            this.tsmiCreateMaterial.Text = "Create";
            this.tsmiEditMaterial.Text = "Edit";
            this.tsmiDuplicateMaterial.Text = "Duplicate";
            this.tsmiImportMaterial.Text = "Import from .inp";
            this.tsmiExportMaterial.Text = "Export to .inp";
            this.tsmiDeleteMaterial.Text = "Delete";
            this.tsmiMaterialLibrary.Text = "Material Library";
            this.tsmiSection.Text = "Section";
            this.tsmiCreateSection.Text = "Create";
            this.tsmiEditSection.Text = "Edit";
            this.tsmiDuplicateSection.Text = "Duplicate";
            this.tsmiDelete.Text = "Delete";
            this.tsmiInteraction.Text = "Interaction";
            this.tsmiConstraint.Text = "Constraint";
            this.tsmiCreateConstraint.Text = "Create";
            this.tsmiEditConstraint.Text = "Edit";
            this.tsmiDuplicateConstraint.Text = "Duplicate";
            this.tsmiSwapMasterSlaveConstraint.Text = "Swap Master/Slave";
            this.tsmiMergeByMasterSlaveConstraint.Text = "Merge by Master/Slave";
            this.tsmiHideConstraint.Text = "Hide";
            this.tsmiShowConstraint.Text = "Show";
            this.tsmiDeleteConstraint.Text = "Delete";
            this.tsmiContact.Text = "Contact";
            this.tsmiSurfaceInteraction.Text = "Surface Interaction";
            this.tsmiCreateSurfaceInteraction.Text = "Create";
            this.tsmiEditSurfaceInteraction.Text = "Edit";
            this.tsmiDuplicateSurfaceInteraction.Text = "Duplicate";
            this.tsmiDeleteSurfaceInteraction.Text = "Delete";
            this.contactPairToolStripMenuItem.Text = "Contact Pair";
            this.tsmiCreateContactPair.Text = "Create";
            this.tsmiEditContactPair.Text = "Edit";
            this.tsmiDuplicateContactPair.Text = "Duplicate";
            this.tsmiSwapMasterSlaveContactPair.Text = "Swap Master/Slave";
            this.tsmiMergeByMasterSlaveContactPair.Text = "Merge by Master/Slave";
            this.tsmiHideContactPair.Text = "Hide";
            this.tsmiShowContactPair.Text = "Show";
            this.tsmiDeleteContactPair.Text = "Delete";
            this.tsmiSearchContactPairs.Text = "Search Contact Pairs";
            this.tsmiAmplitude.Text = "Amplitude";
            this.tsmiCreateAmplitude.Text = "Create";
            this.tsmiEditAmplitude.Text = "Edit";
            this.tsmiDuplicateAmplitude.Text = "Duplicate";
            this.tsmiDeleteAmplitude.Text = "Delete";
            this.tsmiInitialCondition.Text = "Initial Condition";
            this.tsmiCreateInitialCondition.Text = "Create";
            this.tsmiEditInitialCondition.Text = "Edit";
            this.tsmiDuplicateInitialCondition.Text = "Duplicate";
            this.tsmiPreviewInitialCondition.Text = "Preview";
            this.tsmiHideInitialCondition.Text = "Hide";
            this.tsmiShowInitialCondition.Text = "Show";
            this.tsmiDeleteInitialCondition.Text = "Delete";
            this.tsmiStepMenu.Text = "Step";
            this.tsmiStep.Text = "Step";
            this.tsmiCreateStep.Text = "Create";
            this.tsmiEditStep.Text = "Edit";
            this.tsmiEditStepControls.Text = "Edit Controls";
            this.tsmiDuplicateStep.Text = "Duplicate";
            this.tsmiDeleteStep.Text = "Delete";
            this.tsmiHistoryOutput.Text = "History Output";
            this.tsmiCreateHistoryOutput.Text = "Create";
            this.tsmiEditHistoryOutput.Text = "Edit";
            this.tsmiDuplicateHistoryOutput.Text = "Duplicate";
            this.tsmiPropagateHistoryOutput.Text = "Propagate";
            this.tsmiDeleteHistoryOutput.Text = "Delete";
            this.tsmiFieldOutput.Text = "Field Output";
            this.tsmiCreateFieldOutput.Text = "Create";
            this.tsmiEditFieldOutput.Text = "Edit";
            this.tsmiDuplicateFieldOutput.Text = "Duplicate";
            this.tsmiPropagateFieldOutput.Text = "Propagate";
            this.tsmiDeleteFieldOutput.Text = "Delete";
            this.tsmiBC.Text = "BC";
            this.tsmiCreateBC.Text = "Create";
            this.tsmiEditBC.Text = "Edit";
            this.tsmiDuplicateBC.Text = "Duplicate";
            this.tsmiPropagateBC.Text = "Propagate";
            this.tsmiHideBC.Text = "Hide";
            this.tsmiShowBC.Text = "Show";
            this.tsmiDeleteBC.Text = "Delete";
            this.tsmiLoad.Text = "Load";
            this.tsmiCreateLoad.Text = "Create";
            this.tsmiEditLoad.Text = "Edit";
            this.tsmiDuplicateLoad.Text = "Duplicate";
            this.tsmiPreviewLoad.Text = "Preview";
            this.tsmiPropagateLoad.Text = "Propagate";
            this.tsmiHideLoad.Text = "Hide";
            this.tsmiShowLoad.Text = "Show";
            this.tsmiDeleteLoad.Text = "Delete";
            this.tsmiDefinedField.Text = "Defined Field";
            this.tsmiCreateDefinedField.Text = "Create";
            this.tsmiEditDefinedField.Text = "Edit";
            this.tsmiDuplicateDefinedField.Text = "Duplicate";
            this.tsmiPropagateDefinedField.Text = "Propagate";
            this.tsmiPreviewDefinedField.Text = "Preview";
            this.tsmiHideDefinedField.Text = "Hide";
            this.tsmiShowDefinedField.Text = "Show";
            this.tsmiDeleteDefinedField.Text = "Delete";
            this.tsmiAnalysis.Text = "Analysis";
            this.tsmiCreateAnalysis.Text = "Create";
            this.tsmiEditAnalysis.Text = "Edit";
            this.tsmiDuplicateAnalysis.Text = "Duplicate";
            this.tsmiRunAnalysis.Text = "Run";
            this.tsmiCheckModel.Text = "Check Model";
            this.tsmiMonitorAnalysis.Text = "Monitor";
            this.tsmiResultsAnalysis.Text = "Results";
            this.tsmiKillAnalysis.Text = "Kill";
            this.tsmiDeleteAnalysis.Text = "Delete";
            this.tsmiResults.Text = "Results";
            this.tsmiResultPart.Text = "Part";
            this.tsmiEditResultPart.Text = "Edit";
            this.tsmiMergeResultPart.Text = "Merge";
            this.tsmiHideResultParts.Text = "Hide";
            this.tsmiShowResultParts.Text = "Show";
            this.tsmiShowOnlyResultParts.Text = "Show Only";
            this.tsmiSetColorForResultParts.Text = "Set Color";
            this.tsmiResetColorForResultParts.Text = "Reset Color";
            this.tsmiSetTransparencyForResultParts.Text = "Set Transparency";
            this.tsmiColorContoursOff.Text = "Color Contours off";
            this.tsmiColorContoursOn.Text = "Color Contours on";
            this.tsmiDeleteResultParts.Text = "Delete";
            this.tsmiResultFeatures.Text = "Features";
            this.tsmiResultReferencePoints.Text = "Reference Points";
            this.tsmiCreateResultReferencePoint.Text = "Create";
            this.tsmiEditResultReferencePoint.Text = "Edit";
            this.tsmiDuplicateResultReferencePoint.Text = "Duplicate";
            this.tsmiHideResultReferencePoint.Text = "Hide";
            this.tsmiShowResultReferencePoint.Text = "Show";
            this.tsmiShowOnlyResultReferencePoint.Text = "Show Only";
            this.tsmiDeleteResultReferencePoint.Text = "Delete";
            this.tsmiResultCoordinateSystems.Text = "Coordinate Systems";
            this.tsmiCreateResultCoordinateSystem.Text = "Create";
            this.tsmiEditResultCoordinateSystem.Text = "Edit";
            this.tsmiDuplicateResultCoordinateSystem.Text = "Duplicate";
            this.tsmiHideResultCoordinateSystem.Text = "Hide";
            this.tsmiShowResultCoordinateSystem.Text = "Show";
            this.tsmiShowOnlyResultCoordinateSystem.Text = "Show Only";
            this.tsmiDeleteResultCoordinateSystem.Text = "Delete";
            this.tsmiResultFieldOutput.Text = "Field Output";
            this.tsmiCreateResultFieldOutput.Text = "Create";
            this.tsmiEditResultFieldOutput.Text = "Edit";
            this.tsmiDeleteResultFieldOutput.Text = "Delete";
            this.tsmiResultHistoryOutput.Text = "History Output";
            this.tsmiCreateResultHistoryOutput.Text = "Create";
            this.tsmiEditResultHistoryOutput.Text = "Edit";
            this.tsmiExportResultHistoryOutput.Text = "Export";
            this.tsmiDeleteResultHistoryOutput.Text = "Delete";
            this.tsmiTransformation.Text = "Transformation";
            this.tsmiAppendResults.Text = "Append Results";
            this.tsmiTools.Text = "Tools";
            this.tsmiSettings.Text = "Settings";
            this.tsmiParameters.Text = "Parameters";
            this.tsmiQuery.Text = "Query";
            this.tsmiFind.Text = "Find";
            this.tsmiHelp.Text = "Help";
            this.tsmiAdvisor.Text = "Advisor";
            this.tsmiHomePage.Text = "Home Page";
            this.tsmiAbout.Text = "About";
            this.tsmiTest.Text = "Test";
            this.tsmiLanguage.Text = "Language"; //my code
            this.tsmiLanguageEnglish.Text = "English"; //my code
            this.tsmiLanguageJapanese.Text = "Japanese"; //my code
            this.tsmiLanguageYours.Text = ""; //my code
            this.tsmiEditAnnotation.Text = "Edit";
            this.tsmiResetAnnotation.Text = "Reset";
            this.tsmiAnnotationSettings.Text = "Settings";
            this.tsmiDeleteAnnotation.Text = "Delete";
            this.statusStripMain.Text = "statusStrip1";
            this.tsslState.Text = "Ready";
            this.tsslCancel.Text = "Cancel";
            this.tsslEmpty.Text = " ";
            this.tsslUnitSystem.Text = "Unit system: Undefined";
            this.toolStripContainer1.Text = "toolStripContainer";
            this.tbOutput.Text = "Output text box";

            //--- UserControls
            _frmNewModel.gbType.Text = "Unit System Type";
            _frmNewModel.gbProperties.Text = "Units";
            _frmNewModel.gbModelSpace.Text = "Model Space";
            _frmNewModel.rb2DPlaneStress.Text = "2D Plane Stress";
            _frmNewModel.rb3D.Text = "3D";
            _frmNewModel.rb2DAxisymmetric.Text = "2D Axisymmetric";
            _frmNewModel.rb2DPlaneStrain.Text = "2D Plane Strain";
            //Console.WriteLine("_frmMeshSetupItem:"+ _frmMeshSetupItem.lvTypes.Items.Count);
            _frmMeshSetupItem.MeshingParametersText = "Meshing Parameters";
            _frmMeshSetupItem.MeshRefinementText = "Mesh Refinement";
            _frmMeshSetupItem.ShellGmshText = "Shell Gmsh";
            _frmMeshSetupItem.ThickenShellMeshText = "Thicken Shell Mesh";
            _frmMeshSetupItem.TetrahedralGmshText = "Tetrahedral Gmsh";
            _frmMeshSetupItem.TransfiniteMeshText = "Transfinite Mesh";
            _frmMeshSetupItem.ExtrudeMeshText = "Extrude Mesh";
            _frmMeshSetupItem.SweepMeshText = "Sweep Mesh";
            _frmMeshSetupItem.RevolveMeshText = "Revolve Mesh";


        _modelTree.SetLanguage("en");
            RegenerateTree();
        }
        private void tsmiLanguageJapanese_Click(object sender, EventArgs e)
        {
            Console.WriteLine("tsmiLanguageJapanese_Click");

            _appLanguage = "ja";
            //--- Button
            this.tsFile.Text = "ファイル";
            this.tsbNew.Text = "新規モデル";
            this.tsbOpen.Text = "ファイルを開く";
            this.tsbImport.Text = "ファイルをインポート";
            this.tsbImport.ToolTipText = "ファイルをインポート";
            this.tsbSave.Text = "ファイルに保存";
            this.tsViews.Text = "ビュー";
            this.tsbZoomToFit.Text = "ズーム＆フィット";
            this.tsbFrontView.Text = "フロントビュー";
            this.tsbBackView.Text = "バックビュー";
            this.tsbTopView.Text = "トップビュー";
            this.tsbBottomView.Text = "ボトムビュー";
            this.tsbLeftView.Text = "レフトビュー";
            this.tsbRightView.Text = "ライトビュー";
            this.tsbNormalView.Text = "法線ビュー";
            this.tsbVerticalView.Text = "垂直ビュー";
            this.tsbIsometric.Text = "アイソビュー";
            this.tsbShowWireframeEdges.Text = "ワイヤーフレーム";
            this.tsbShowElementEdges.Text = "要素エッジを表示";
            this.tsbShowModelEdges.Text = "モデルエッジを表示";
            this.tsbShowNoEdges.Text = "エッジなし";
            this.tsbSectionView.Text = "セクションビュー";
            this.tsbExplodedView.Text = "分解ビュー";
            this.tsbQuery.Text = "クエリ";
            this.tsbRemoveAnnotations.Text = "注釈を除く";
            this.tsbShowAllParts.Text = "全てのパートを表示";
            this.tsbHideAllParts.Text = "全てのパートを非表示";
            this.tsbInvertVisibleParts.Text = "表示パートを反転";
            this.tslSymbols.Text = "シンボル";
            this.tscbSymbols.ToolTipText = "シンボルの表示方法を選択";
            this.tslResultName.Text = "結果";
            this.tslDeformationVariable.Text = "変数";
            this.tscbDeformationVariable.ToolTipText = "変形変数を選択";
            this.tslDeformationType.Text = "タイプ";
            this.tscbDeformationType.ToolTipText = "変形タイプを選択";
            this.tslDeformationFactor.Text = "ファクター";
            this.tstbDeformationFactor.Text = "10";
            this.tstbDeformationFactor.ToolTipText = "変形スケールファクターを入力";
            this.tslComplex.Text = "Complex";
            this.tslAngle.Text = "角度";
            this.tstbAngle.Text = "0 °";
            this.tsResults.Text = "結果";
            this.tsbResultsUndeformed.Text = "原形";
            this.tsbResultsDeformed.Text = "変形";
            this.tsbResultsColorContours.Text = "カラーコンター変形図";
            this.tsbResultsUndeformedWireframe.Text = "原形ワイヤーフレームを表示";
            this.tsbResultsUndeformedSolid.Text = "原形ソリッドモデルを表示";
            this.tsbTransformation.Text = "変換";
            this.tsbFirstStepIncrement.Text = "最初のインクリメント";
            this.tsbPreviousStepIncrement.Text = "前のインクリメント";
            this.tsbPreviousStepIncrement.ToolTipText = "前のインクリメント";
            this.tslStepIncrement.Text = "ステップ, インクリメント";
            this.tscbStepAndIncrement.ToolTipText = "インクリメントを選択";
            this.tsbNextStepIncrement.Text = "次のインクリメント";
            this.tsbNextStepIncrement.ToolTipText = "次のインクリメント";
            this.tsbLastStepIncrement.Text = "最後のインクリメント";
            this.tsbAnimate.Text = "アニメーション";
            
            //--- Menu
            tsmiFile.Text = "ファイル";
                tsmiNew.Text = "新規";
                tsmiOpen.Text = "開く";
                tsmiOpenRecent.Text = "最近開いたファイル";
                tsmiRunHistoryFile.Text = "履歴ファイルを実行";
                tsmiImportFile.Text = "インポート";
                tsmiSave.Text = "保存";
                tsmiSaveAs.Text = "別名保存";
                tsmiExport.Text = "エクスポート";
                tsmiCloseCurrentResult.Text = "現在の結果を閉じる";
                tsmiCloseAllResults.Text = "全ての結果を閉じる";
                tsmiExit.Text = "終了";
            tsmiEdit.Text = "編集";
                tsmiUndo.Text = "元に戻す";
                tsmiRedo.Text = "やり直す";
                tsmiEditHistory.Text = "履歴を編集する";
                tsmiRegenerateHistory.Text = "履歴を再構築する";
                tsmiRegenerateHistoryUsingOtherFiles.Text = "他のファイルを使用して履歴を再構築する";
                tsmiRegenerateHistoryWithRemeshing.Text = "リメッシュで履歴を再構築する";
            tsmiView.Text = "ビュー";
                standardViewsToolStripMenuItem.Text = "標準ビュー";
                    tsmiFrontView.Text = "正面ビュー";
                    tsmiBackView.Text = "後面ビュー";
                    tsmiTopView.Text = "トップビュー";
                    tsmiBottomView.Text = "ボトムビュー";
                    tsmiLeftView.Text = "左ビュー";
                    tsmiRightView.Text = "右ビュー";
                    tsmiNormalView.Text = "法線ビュー";
                    tsmiVerticalView.Text = "垂直方向ビュー";
                    tsmiIsometricView.Text = "等角投影ビュー";
                tsmiZoomToFit.Text = "ズーム＆フィット";
                tsmiShowWireframeEdges.Text = "ワイヤーフレーム";
                tsmiShowElementEdges.Text = "要素エッジを表示";
                tsmiShowModelEdges.Text = "モデルエッジを表示";
                tsmiShowNoEdges.Text = "エッジなし";
                tsmiSectionView.Text = "セクションビュー";
                tsmiExplodedView.Text = "分解ビュー";
                tsmiShowAllParts.Text = "全て表示";
                tsmiHideAllParts.Text = "全て非表示";
                tsmiInvertVisibleParts.Text = "表示部分を反転";
                tsmiResultsUndeformed.Text = "原形図";
                tsmiResultsDeformed.Text = "変形図";
                tsmiResultsColorContours.Text = "コンターカラー変形図";
                tsmiResultsDeformedColorWireframe.Text = "変形図＆カラー＆ワイヤーフレーム";
                tsmiResultsDeformedColorSolid.Text = "変形図＆カラー＆ソリッド";
                tsmiColorAnnotations.Text = "色の注釈";
                    tsmiAnnotateFaceOrientations.Text = "面の向き";
                    tsmiAnnotateParts.Text = "パート";
                    tsmiAnnotateMaterials.Text = "材料特性";
                    tsmiAnnotateSections.Text = "要素特性";
                    tsmiAnnotateSectionThicknesses.Text = "セクション厚さ";
                    tsmiAnnotateAllSymbols.Text = "全てのシンボル";
                    tsmiAnnotateReferencePoints.Text = "参照点";
                    tsmiAnnotateConstraints.Text = "拘束";
                    tsmiAnnotateContactPairs.Text = "接触ペア";
                    tsmiAnnotateInitialConditions.Text = "初期条件";
                    tsmiAnnotateBCs.Text = "境界条件";
                    tsmiAnnotateLoads.Text = "荷重";
                    tsmiAnnotateDefinedFields.Text = "規定場";
            tsmiGeometry.Text = "ジオメトリ";
                tsmiGeometryPart.Text = "パート";
                tsmiEditGeometryPart.Text = "編集";
                tsmiTransformGeometryParts.Text = "変換";
                tsmiScaleGeometryParts.Text = "スケール";
                tsmiCopyGeometryPartsToResults.Text = "結果へジオメトリをコピー";
                tsmiHideGeometryParts.Text = "非表示";
                tsmiShowGeometryParts.Text = "表示";
                tsmiShowOnlyGeometryParts.Text = "1つだけ表示";
                tsmiSetColorForGeometryParts.Text = "色を設定";
                tsmiResetColorForGeometryParts.Text = "色をリセット";
                tsmiSetTransparencyForGeometryParts.Text = "透明度を設定";
                tsmiDeleteGeometryParts.Text = "削除";
                cADPartToolStripMenuItem.Text = "CADパート";
                tsmiFlipFaceNormalCAD.Text = "面法線を反転";
                tsmiSplitAFaceUsingTwoPoints.Text = "2点を使用して面を分割";
                tsmiDefeature.Text = "特徴";
                tsmiStlPart.Text = "STLパート";
                tsmiFindStlEdgesByAngleForGeometryParts.Text = "角度からモデルエッジを抽出";
                tsmiFlipStlPartFaceNormal.Text = "パート面法線を反転";
                tsmiSmoothStlPart.Text = "パートを平滑化";
                tsmiDeleteStlPartFaces.Text = "パート面を削除";
                tsmiCropStlPartWithCylinder.Text = "円柱で切り抜き";
                tsmiCropStlPartWithCube.Text = "立方体で切り抜き";
                tsmiCreateAndImportCompoundPart.Text = "複合パートを作成";
                tsmiRegenerateCompoundPart.Text = "複合パートを再構築";
                tsmiSwapGeometryPartGeometries.Text = "パートのジオメトリを入れ替え";
                tsmiGeometryAnalyze.Text = "解析";
            tsmiMesh.Text = "メッシュ";
                this.tsmiMeshSetupItem.Text = "メッシュ設定アイテム";
                this.tsmiCreateMeshSetupItem.Text = "作成";
                this.tsmiEditMeshSetupItem.Text = "編集";
                this.tsmiDuplicateMeshSetupItem.Text = "複製";
                this.tsmiDeleteMeshSetupItem.Text = "削除";
                this.tsmiPreviewEdgeMesh.Text = "メッシュエッジをプレビュー";
                this.tsmiCreateMesh.Text = "メッシュを作成";
            this.tsmiModel.Text = "モデル";
                this.tsmiEditModel.Text = "編集";
                this.tsmiEditCalculiXKeywords.Text = "CalculiXキーワードを編集";
                this.tsmiToolsParts.Text = "ツール";
                this.tsmiFindEdgesByAngleForModelParts.Text = "角度からモデルエッジを抽出";
                this.tsmiCreateBoundaryLayer.Text = "境界レイヤーを作成";
                this.tsmiRemeshElements.Text = "要素のリメッシュ";
                this.tsmiThickenShellMesh.Text = "メッシュを厚くする";
                this.tsmiSplitPartMeshUsingSurface.Text = "サーフェスを使用してパートメッシュを分割";
                this.tsmiUpdateNodalCoordinatesFromFile.Text = "ファイルから節点座標を更新";
                this.tsmiNode.Text = "節点";
                this.tsmiRenumberAllNodes.Text = "節点番号を付け直す";
                this.tsmiMergeCoincidentNodes.Text = "一致する節点を統合する";
                this.tsmiElement.Text = "要素";
                this.tsmiRenumberAllElements.Text = "要素番号を付け直す";
                this.tsmiElementQuality.Text = "要素のクオリティ";
                this.tsmiPart.Text = "パート";
                this.tsmiEditModelPart.Text = "編集";
                this.tsmiTransformModelParts.Text = "変換";
                this.tsmiTranslateModelParts.Text = "移動";
                this.tsmiScaleModelParts.Text = "スケール";
                this.tsmiRotateModelParts.Text = "回転";
                this.tsmiMergeModelParts.Text = "統合";
                this.tsmiHideModelParts.Text = "非表示";
                this.tsmiShowModelParts.Text = "表示";
                this.tsmiShowOnlyModelParts.Text = "1つだけ表示";
                this.tsmiSetColorForModelParts.Text = "色を設定";
                this.tsmiResetColorForModelParts.Text = "色のリセット";
                this.tsmiSetTransparencyForModelParts.Text = "透明度の設定";;
                this.tsmiDeleteModelParts.Text = "削除";
                this.tsmiNodeSet.Text = "節点集合";;
                this.tsmiCreateNodeSet.Text = "作成";
                this.tsmiEditNodeSet.Text = "編集";
                this.tsmiDuplicateNodeSet.Text = "複製";
                this.tsmiDeleteNodeSet.Text = "削除";
                this.tsmiElementSet.Text = "要素集合";
                this.tsmiCreateElementSet.Text = "作成";
                this.tsmiEditElementSet.Text = "編集";
                this.tsmiDuplicateElementSet.Text = "複製";
                this.tsmiConvertElementSetsToMeshParts.Text = "パートへ変換";;
                this.tsmiDeleteElementSet.Text = "削除";
                this.tsmiSurface.Text = "サーフェス";
                this.tsmiCreateSurface.Text = "作成";
                this.tsmiEditSurface.Text = "編集";
                this.tsmiDuplicateSurface.Text = "複製";
                this.tsmiDeleteSurface.Text = "削除";
                this.tsmiModelFeatures.Text = "フィーチャ";
                this.tsmiModelReferencePointTool.Text = "参照点";
                this.tsmiCreateModelReferencePoint.Text = "作成";
                this.tsmiEditModelReferencePoint.Text = "編集";
                this.tsmiDuplicateModelReferencePoint.Text = "複製";
                this.tsmiHideModelReferencePoint.Text = "非表示";
                this.tsmiShowModelReferencePoint.Text = "表示";
                this.tsmiShowOnlyModelReferencePoint.Text = "1つだけ表示";
                this.tsmiDeleteModelReferencePoint.Text = "削除";
                this.tsmiModelCoordinateSystem.Text = "座標系";
                this.tsmiCreateModelCoordinateSystem.Text = "作成";
                this.tsmiEditModelCoordinateSystem.Text = "編集";
                this.tsmiDuplicateModelCoordinateSystem.Text = "複製";
                this.tsmiHideModelCoordinateSystem.Text = "非表示";
                this.tsmiShowModelCoordinateSystem.Text = "表示";
                this.tsmiShowOnlyModelCoordinateSystem.Text = "1つだけ表示";
                this.tsmiDeleteModelCoordinateSystem.Text = "削除";
            this.tsmiProperty.Text = "特性";
                this.tsmiMaterial.Text = "材料特性";
                this.tsmiCreateMaterial.Text = "作成";
                this.tsmiEditMaterial.Text = "編集";
                this.tsmiDuplicateMaterial.Text = "複製";
                this.tsmiImportMaterial.Text = ".inpからインポート";
                this.tsmiExportMaterial.Text = ".inpへエクスポート";
                this.tsmiDeleteMaterial.Text = "削除";
                this.tsmiMaterialLibrary.Text = "材料特性ライブラリ";
                this.tsmiSection.Text = "要素特性";
                this.tsmiCreateSection.Text = "作成";
                this.tsmiEditSection.Text = "編集";
                this.tsmiDuplicateSection.Text = "複製";
                this.tsmiDelete.Text = "削除";
            this.tsmiInteraction.Text = "相互作用";
                this.tsmiConstraint.Text = "拘束";
                this.tsmiCreateConstraint.Text = "作成";
                this.tsmiEditConstraint.Text = "編集";
                this.tsmiDuplicateConstraint.Text = "複製";
                this.tsmiSwapMasterSlaveConstraint.Text = "マスター/スレーブ入れ替え";
                this.tsmiMergeByMasterSlaveConstraint.Text = "マスター/スレーブで統合";
                this.tsmiHideConstraint.Text = "非表示";
                this.tsmiShowConstraint.Text = "表示";
                this.tsmiDeleteConstraint.Text = "削除";
                this.tsmiContact.Text = "接触";
                this.tsmiSurfaceInteraction.Text = "サーフェス相互作用";
                this.tsmiCreateSurfaceInteraction.Text = "作成";
                this.tsmiEditSurfaceInteraction.Text = "編集";
                this.tsmiDuplicateSurfaceInteraction.Text = "複製";
                this.tsmiDeleteSurfaceInteraction.Text = "削除";
                this.contactPairToolStripMenuItem.Text = "接触ペア";
                this.tsmiCreateContactPair.Text = "作成";
                this.tsmiEditContactPair.Text = "編集";
                this.tsmiDuplicateContactPair.Text = "複製";
                this.tsmiSwapMasterSlaveContactPair.Text = "マスター/スレーブ入れ替え";
                this.tsmiMergeByMasterSlaveContactPair.Text = "マスター/スレーブで統合";
                this.tsmiHideContactPair.Text = "非表示";
                this.tsmiShowContactPair.Text = "表示";
                this.tsmiDeleteContactPair.Text = "削除";
                this.tsmiSearchContactPairs.Text = "接触ペアを探す";
            this.tsmiAmplitude.Text = "時間変化";
                this.tsmiCreateAmplitude.Text = "作成";
                this.tsmiEditAmplitude.Text = "編集";
                this.tsmiDuplicateAmplitude.Text = "複製";
                this.tsmiDeleteAmplitude.Text = "削除";
            this.tsmiInitialCondition.Text = "初期条件";
                this.tsmiCreateInitialCondition.Text = "作成";
                this.tsmiEditInitialCondition.Text = "編集";
                this.tsmiDuplicateInitialCondition.Text = "複製";
                this.tsmiPreviewInitialCondition.Text = "Preview";
                this.tsmiHideInitialCondition.Text = "非表示";
                this.tsmiShowInitialCondition.Text = "表示";
                this.tsmiDeleteInitialCondition.Text = "削除";
            this.tsmiStepMenu.Text = "ステップ";
                this.tsmiStep.Text = "ステップ";
                this.tsmiCreateStep.Text = "作成";
                this.tsmiEditStep.Text = "編集";
                this.tsmiEditStepControls.Text = "コントロールの編集";
                this.tsmiDuplicateStep.Text = "複製";
                this.tsmiDeleteStep.Text = "削除";
                this.tsmiHistoryOutput.Text = "履歴出力";
                this.tsmiCreateHistoryOutput.Text = "作成";
                this.tsmiEditHistoryOutput.Text = "編集";
                this.tsmiDuplicateHistoryOutput.Text = "複製";
                this.tsmiPropagateHistoryOutput.Text = "Propagate";
                this.tsmiDeleteHistoryOutput.Text = "削除";
                this.tsmiFieldOutput.Text = "フィールド出力";
                this.tsmiCreateFieldOutput.Text = "作成";
                this.tsmiEditFieldOutput.Text = "編集";
                this.tsmiDuplicateFieldOutput.Text = "複製";
                this.tsmiPropagateFieldOutput.Text = "Propagate";
                this.tsmiDeleteFieldOutput.Text = "削除";
                this.tsmiBC.Text = "境界条件";
                this.tsmiCreateBC.Text = "作成";
                this.tsmiEditBC.Text = "編集";
                this.tsmiDuplicateBC.Text = "複製";
                this.tsmiPropagateBC.Text = "Propagate";
                this.tsmiHideBC.Text = "非表示";
                this.tsmiShowBC.Text = "表示";
                this.tsmiDeleteBC.Text = "削除";
                this.tsmiLoad.Text = "荷重";
                this.tsmiCreateLoad.Text = "作成";
                this.tsmiEditLoad.Text = "編集";
                this.tsmiDuplicateLoad.Text = "複製";
                this.tsmiPreviewLoad.Text = "プレビュー";
                this.tsmiPropagateLoad.Text = "Propagate";
                this.tsmiHideLoad.Text = "非表示";
                this.tsmiShowLoad.Text = "表示";
                this.tsmiDeleteLoad.Text = "削除";
                this.tsmiDefinedField.Text = "規定場";
                this.tsmiCreateDefinedField.Text = "作成";
                this.tsmiEditDefinedField.Text = "編集";
                this.tsmiDuplicateDefinedField.Text = "複製";
                this.tsmiPropagateDefinedField.Text = "Propagate";
                this.tsmiPreviewDefinedField.Text = "プレビュー";
                this.tsmiHideDefinedField.Text = "非表示";
                this.tsmiShowDefinedField.Text = "表示";
                this.tsmiDeleteDefinedField.Text = "削除";
            this.tsmiAnalysis.Text = "解析";
                this.tsmiCreateAnalysis.Text = "作成";
                this.tsmiEditAnalysis.Text = "編集";
                this.tsmiDuplicateAnalysis.Text = "複製";
                this.tsmiRunAnalysis.Text = "実行";
                this.tsmiCheckModel.Text = "モデルチェック";
                this.tsmiMonitorAnalysis.Text = "モニター";
                this.tsmiResultsAnalysis.Text = "結果";
                this.tsmiKillAnalysis.Text = "停止";
                this.tsmiDeleteAnalysis.Text = "削除";
            this.tsmiResults.Text = "結果";
                this.tsmiResultPart.Text = "パート";
                this.tsmiEditResultPart.Text = "編集";
                this.tsmiMergeResultPart.Text = "統合";
                this.tsmiHideResultParts.Text = "非表示";
                this.tsmiShowResultParts.Text = "表示";
                this.tsmiShowOnlyResultParts.Text = "1つだけ表示";
                this.tsmiSetColorForResultParts.Text = "色を設定";
                this.tsmiResetColorForResultParts.Text = "色のリセット";
                this.tsmiSetTransparencyForResultParts.Text = "透明度の設定";;
                this.tsmiColorContoursOff.Text = "カラーコンターオフ";
                this.tsmiColorContoursOn.Text = "カラーコンターオン";
                this.tsmiDeleteResultParts.Text = "削除";
                this.tsmiResultFeatures.Text = "フィーチャ";
                this.tsmiResultReferencePoints.Text = "参照点";
                this.tsmiCreateResultReferencePoint.Text = "作成";
                this.tsmiEditResultReferencePoint.Text = "編集";
                this.tsmiDuplicateResultReferencePoint.Text = "複製";
                this.tsmiHideResultReferencePoint.Text = "非表示";
                this.tsmiShowResultReferencePoint.Text = "表示";
                this.tsmiShowOnlyResultReferencePoint.Text = "1つだけ表示";
                this.tsmiDeleteResultReferencePoint.Text = "削除";
                this.tsmiResultCoordinateSystems.Text = "座標系";
                this.tsmiCreateResultCoordinateSystem.Text = "作成";
                this.tsmiEditResultCoordinateSystem.Text = "編集";
                this.tsmiDuplicateResultCoordinateSystem.Text = "複製";
                this.tsmiHideResultCoordinateSystem.Text = "非表示";
                this.tsmiShowResultCoordinateSystem.Text = "表示";
                this.tsmiShowOnlyResultCoordinateSystem.Text = "1つだけ表示";
                this.tsmiDeleteResultCoordinateSystem.Text = "削除";
                this.tsmiResultFieldOutput.Text = "フィールド出力";
                this.tsmiCreateResultFieldOutput.Text = "作成";
                this.tsmiEditResultFieldOutput.Text = "編集";
                this.tsmiDeleteResultFieldOutput.Text = "削除";
                this.tsmiResultHistoryOutput.Text = "履歴出力";
                this.tsmiCreateResultHistoryOutput.Text = "作成";
                this.tsmiEditResultHistoryOutput.Text = "編集";
                this.tsmiExportResultHistoryOutput.Text = "エクスポート";
                this.tsmiDeleteResultHistoryOutput.Text = "削除";
                this.tsmiTransformation.Text = "変換";
                this.tsmiAppendResults.Text = "結果を追加";
            tsmiTools.Text = "ツール";
                tsmiSettings.Text = "設定";
                tsmiParameters.Text = "変数";
                tsmiQuery.Text = "クエリ";
                tsmiFind.Text = "検索";
            tsmiHelp.Text = "ヘルプ";
                tsmiAdvisor.Text = "アドバイザー";
                tsmiHomePage.Text = "ホームページ";
                tsmiAbout.Text = "PrePoMaxについて";
                tsmiTest.Text = "テスト";
            this.tsmiLanguage.Text = "言語"; //my code
                this.tsmiLanguageEnglish.Text = "英語"; //my code
                this.tsmiLanguageJapanese.Text = "日本語"; //my code
                this.tsmiLanguageYours.Text = ""; //my code 
            this.tsmiEditAnnotation.Text = "編集";
            this.tsmiResetAnnotation.Text = "リセット";
            this.tsmiAnnotationSettings.Text = "設定";
            this.tsmiDeleteAnnotation.Text = "削除";
            //this.tsslState.Text = "準備完了";  // -> can not open results
            this.tsslCancel.Text = "キャンセル";
            this.tsslEmpty.Text = " ";
            this.tsslUnitSystem.Text = "単位系: 定義なし";
            this.tbOutput.Text = "Output text box";

            //--- UserControls
            _frmNewModel.gbType.Text = "単位系タイプ";
            _frmNewModel.gbProperties.Text = "単位";
            _frmNewModel.gbModelSpace.Text = "モデル空間";
            _frmNewModel.rb2DPlaneStress.Text = "2次元平面応力";
            _frmNewModel.rb3D.Text = "3次元";
            _frmNewModel.rb2DAxisymmetric.Text = "2次元D 軸対称";
            _frmNewModel.rb2DPlaneStrain.Text = "2次元平面ひずみ";
            //Console.WriteLine("_frmMeshSetupItem:"+ _frmMeshSetupItem.lvTypes.Items.Count);
            //Console.WriteLine("_frmMeshSetupItem.lvTypes.Items[0].Text:"+ _frmMeshSetupItem.lvTypes.Items[0].Text);
            _frmMeshSetupItem.MeshingParametersText = "メッシュパラメータ";
            _frmMeshSetupItem.MeshRefinementText = "メッシュ修正";
            _frmMeshSetupItem.ShellGmshText = "シェルGmsh";
            _frmMeshSetupItem.ThickenShellMeshText = "シェルメッシュを厚くする";
            _frmMeshSetupItem.TetrahedralGmshText = "四面体Gmsh";
            _frmMeshSetupItem.TransfiniteMeshText = "Transfinite Mesh";
            _frmMeshSetupItem.ExtrudeMeshText = "押し出しメッシュ";
            _frmMeshSetupItem.SweepMeshText = "スイープメッシュ";
            _frmMeshSetupItem.RevolveMeshText = "回転メッシュ";


            _modelTree.SetLanguage("ja");
            RegenerateTree();
        }
        private void tsmiLanguageYours_Click(object sender, EventArgs e)
        {
            Console.WriteLine("tsmiLanguageYours_Click");
            //_appLanguage = "";
        }
        #endregion  ################################################################################################################


        #region Selection methods  #################################################################################################
        private void SelectOneEntity(string title, NamedClass[] entities, Action<string> OperateOnEntity)
        {
            if (entities == null || entities.Length == 0) return;
            // Only one entity exists
            if (entities.Length == 1)
            {
                OperateOnEntity(entities[0].Name);
            }
            // Multiple entities exists
            else
            {
                string[] preSelectedEntityNames = _modelTree.IntersectSelectionWithList(entities);
                //
                SetFormLocation(_frmSelectEntity);
                _frmSelectEntity.PrepareForm(title, false, entities, preSelectedEntityNames, null);
                _frmSelectEntity.OneEntitySelected = OperateOnEntity;
                _frmSelectEntity.Show();
            }
        }
        private void SelectOneEntityInStep(string title, NamedClass[] entities, string stepName,
                                           Action<string, string> OperateOnEntityInStep)
        {
            if (entities == null || entities.Length == 0) return;
            // Only one entity exists
            if (entities.Length == 1)
            {
                OperateOnEntityInStep(stepName, entities[0].Name);
            }
            // Multiple entities exists
            else
            {
                string[] preSelectedEntityNames = _modelTree.IntersectSelectionWithList(entities);
                //
                SetFormLocation(_frmSelectEntity);
                _frmSelectEntity.PrepareForm(title, false, entities, preSelectedEntityNames, stepName);
                _frmSelectEntity.OneEntitySelectedInStep = OperateOnEntityInStep;
                _frmSelectEntity.Show();
            }
        }
        private void SelectMultipleEntities(string title, NamedClass[] entities, Action<string[]> OperateOnMultipleEntities,
                                            int minNumberOfEntities = 1, int maxNumberOfEntities = int.MaxValue)
        {
            if (entities == null || entities.Length == 0 || entities.Length < minNumberOfEntities) return;
            // Only one entity exists
            if (entities.Length == 1)
            {
                if (minNumberOfEntities == 1) OperateOnMultipleEntities(entities.GetNames());
            }
            // Multiple entities exists
            else
            {
                string[] preSelectedEntityNames = _modelTree.IntersectSelectionWithList(entities);
                //
                SetFormLocation(_frmSelectEntity);
                _frmSelectEntity.PrepareForm(title, true, entities, preSelectedEntityNames, null);
                _frmSelectEntity.MultipleEntitiesSelected = OperateOnMultipleEntities;
                _frmSelectEntity.MinNumberOfEntities = minNumberOfEntities;
                _frmSelectEntity.MaxNumberOfEntities = maxNumberOfEntities;
                _frmSelectEntity.Show();
            }
        }
        private void SelectMultipleEntitiesInStep(string title, NamedClass[] entities, string stepName,
                                                  Action<string, string[]> OperateOnMultipleEntitiesInStep)
        {
            if (entities == null || entities.Length == 0) return;
            // Only one entity exists
            if (entities.Length == 1)
            {
                OperateOnMultipleEntitiesInStep(stepName, entities.GetNames());
            }
            // Multiple entities exists
            else
            {
                string[] preSelectedEntityNames = _modelTree.IntersectSelectionWithList(entities);
                //
                _frmSelectEntity.Location = new Point(Left + _formLocation.X, Top + _formLocation.Y);
                _frmSelectEntity.PrepareForm(title, true, entities, preSelectedEntityNames, stepName);
                _frmSelectEntity.MultipleEntitiesSelectedInStep = OperateOnMultipleEntitiesInStep;
                _frmSelectEntity.Show();
            }
        }

        #endregion  ################################################################################################################

        #region Mouse selection methods  ###########################################################################################
        public void SelectPointOrArea(double[] pickedPoint, double[] selectionDirection,
                                      double[][] planeParameters, bool completelyInside,
                                      vtkSelectOperation selectOperation, string[] pickedPartNames)
        {
            PushMenuStates();
            //
            SetStateWorking(Globals.SelectionText);
            //
            _controller.SelectPointOrArea(pickedPoint, selectionDirection,
                                          planeParameters, completelyInside,
                                          selectOperation, pickedPartNames);
            //
            int[] ids = _controller.GetSelectionIds();
            // Must be here since it calls Clear which calls SelectionChanged
            if (_frmSectionView != null && _frmSectionView.Visible) _frmSectionView.PickedIds(ids);
            if (_frmTranslate != null && _frmTranslate.Visible) _frmTranslate.PickedIds(ids);
            if (_frmScale != null && _frmScale.Visible) _frmScale.PickedIds(ids);
            if (_frmRotate != null && _frmRotate.Visible) _frmRotate.PickedIds(ids);
            if (_frmQuery != null && _frmQuery.Visible) _frmQuery.PickedIds(ids);
            if (_frmTransformation != null && _frmTransformation.Visible) _frmTransformation.PickedIds(ids);
            //
            SelectionChanged(ids);
            //
            SetStateReady(Globals.SelectionText);
            //
            PopMenuStates();
        }
        public void SelectionChanged(int[] ids = null)
        {
            if (ids == null) ids = _controller.GetSelectionIds();
            //
            if (_frmMeshSetupItem != null && _frmMeshSetupItem.Visible) _frmMeshSetupItem.SelectionChanged(ids);
            if (_frmSelectGeometry != null && _frmSelectGeometry.Visible) _frmSelectGeometry.SelectionChanged(ids);
            //
            if (_frmBoundaryLayer != null && _frmBoundaryLayer.Visible) _frmBoundaryLayer.SelectionChanged(ids);
            if (_frmRemeshingParameters != null && _frmRemeshingParameters.Visible) _frmRemeshingParameters.SelectionChanged(ids);
            if (_frmThickenShellMesh != null && _frmThickenShellMesh.Visible) _frmThickenShellMesh.SelectionChanged(ids);
            if (_frmSplitPartMeshUsingSurface != null && _frmSplitPartMeshUsingSurface.Visible)
                _frmSplitPartMeshUsingSurface.SelectionChanged(ids);
            if (_frmMergeCoincidentNodes != null && _frmMergeCoincidentNodes.Visible)
                _frmMergeCoincidentNodes.SelectionChanged(ids);
            if (_frmNodeSet != null && _frmNodeSet.Visible) _frmNodeSet.SelectionChanged(ids);
            if (_frmElementSet != null && _frmElementSet.Visible) _frmElementSet.SelectionChanged(ids);
            if (_frmSurface != null && _frmSurface.Visible) _frmSurface.SelectionChanged(ids);
            if (_frmReferencePoint != null && _frmReferencePoint.Visible) _frmReferencePoint.SelectionChanged(ids);
            if (_frmCoordinateSystem != null && _frmCoordinateSystem.Visible) _frmCoordinateSystem.SelectionChanged(ids);
            if (_frmSection != null && _frmSection.Visible) _frmSection.SelectionChanged(ids);
            if (_frmConstraint != null && _frmConstraint.Visible) _frmConstraint.SelectionChanged(ids);
            if (_frmContactPair != null && _frmContactPair.Visible) _frmContactPair.SelectionChanged(ids);
            if (_frmInitialCondition != null && _frmInitialCondition.Visible) _frmInitialCondition.SelectionChanged(ids);
            //
            if (_frmHistoryOutput != null && _frmHistoryOutput.Visible) _frmHistoryOutput.SelectionChanged(ids);
            if (_frmBoundaryCondition != null && _frmBoundaryCondition.Visible) _frmBoundaryCondition.SelectionChanged(ids);
            if (_frmLoad != null && _frmLoad.Visible) _frmLoad.SelectionChanged(ids);
            if (_frmDefinedField != null && _frmDefinedField.Visible) _frmDefinedField.SelectionChanged(ids);
            //
            if (_frmResultHistoryOutput != null && _frmResultHistoryOutput.Visible) _frmResultHistoryOutput.SelectionChanged(ids);
        }
        public void SetSelectBy(vtkSelectBy selectBy)
        {
            InvokeIfRequired(() => _vtk.SelectBy = selectBy);
        }
        public void SetSelectItem(vtkSelectItem selectItem)
        {
            InvokeIfRequired(() => _vtk.SelectItem = selectItem);
        }
        public void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds,
                                              out int[] cellFaceNodeIds, string[] selectionPartNames = null)
        {
            elementId = -1;
            edgeNodeIds = null;
            cellFaceNodeIds = null;
            try
            {
                if (selectionPartNames != null) _vtk.SetSelectableActorsFilter(selectionPartNames);
                _vtk.GetGeometryPickProperties(point, out elementId, out edgeNodeIds, out cellFaceNodeIds);
            }
            catch { }
            finally
            {
                if (selectionPartNames != null) _vtk.SetSelectableActorsFilter(null);
            }
        }
        public double GetSelectionPrecision()
        {
            return _vtk.GetSelectionPrecision();
        }
        public void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, string[] selectionPartNames,
                                                    out int[] pointIds, out int[] cellIds)
        {
            cellIds = null;
            pointIds = null;
            try
            {
                if (selectionPartNames != null) _vtk.SetSelectableActorsFilter(selectionPartNames);
                _vtk.GetPointAndCellIdsInsideFrustum(planeParameters, out pointIds, out cellIds);
            }
            catch { }
            finally
            {
                if (selectionPartNames != null) _vtk.SetSelectableActorsFilter(null);
            }
        }

        #endregion  ################################################################################################################
        //
        #region Child form methods  ################################################################################################
        private void AddFormToAllForms(Form form)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Icon = Icon;
            form.Owner = this;
            form.Move += itemForm_Move;
            form.VisibleChanged += itemForm_VisibleChanged;
            _allForms.Add(form);
        }
        private void ShowForm(IFormBase form, string text, string itemToEditName)
        {
            ShowForm(form, text, null, itemToEditName);
        }
        private void ShowForm(IFormBase form, string text, string stepName, string itemToEditName)
        {
            if (!form.Visible)
            {
                CloseAllForms();
                SetFormLocation((Form)form);
                form.Text = text;
                if (itemToEditName != null) form.Text += ": " + itemToEditName;
                if (form.PrepareForm(stepName, itemToEditName)) form.Show();
                else
                {
                    if (stepName != null)
                    {
                        string itemName = text.Replace("Create ", "").Replace("Edit ", "").ToLower();
                        MessageBoxes.ShowWarning("Creating/editing of a " + itemName + " in the step: "
                                                 + stepName + " is not supported.");
                    }
                }
            }
        }
        private void SetFormLocation(Form form)
        {
            Rectangle screenBounds;
            bool intersects = false;
            Rectangle formSize = form.ClientRectangle;
            Point location = new Point(Left + _formLocation.X, Top + _formLocation.Y);
            Rectangle locationRect = new Rectangle(location, new Size(1, 1));
            //
            foreach (var screen in Screen.AllScreens)
            {
                screenBounds = screen.Bounds;
                if (screenBounds.IntersectsWith(locationRect))
                {
                    intersects = true;
                    // Size
                    if (formSize.Width > screenBounds.Width) formSize.Width = screenBounds.Width;
                    if (formSize.Height > screenBounds.Height) formSize.Height = screenBounds.Height;
                    // Location X
                    if (location.X < screenBounds.Left) location.X = screenBounds.Left;
                    else if (location.X + formSize.Width > screenBounds.Left + screenBounds.Width)
                        location.X = screenBounds.Left + screenBounds.Width - formSize.Width;
                    // Location Y
                    if (location.Y < screenBounds.Top) location.Y = screenBounds.Top;
                    else if (location.Y + formSize.Height > screenBounds.Top + screenBounds.Height)
                        location.Y = screenBounds.Top + screenBounds.Height - formSize.Height;
                }
            }
            //
            if (!intersects) location = Location;
            //
            form.Location = location;
        }
        private void SaveFormLocation(Form form)
        {
            _formLocation.X = form.Location.X - Left;
            _formLocation.Y = form.Location.Y - Top;
        }
        public void CloseAllForms()
        {
            InvokeIfRequired(() =>
            {
                if (_allForms != null)
                {
                    foreach (var form in _allForms)
                    {
                        if (form.Visible) form.Hide();
                    }
                }
           });
        }

        #endregion  ################################################################################################################

        // Toolbars                                                                                                                 
        #region File toolbar #######################################################################################################
        private void tsbNew_Click(object sender, EventArgs e)
        {
            tsmiNew_Click(null, null);
        }
        
        private void tsbOpen_Click(object sender, EventArgs e)
        {
            tsmiOpen_Click(null, null);
        }
        private void tsbImport_Click(object sender, EventArgs e)
        {
            tsmiImportFile_Click(null, null);
        }
        private void tsbSave_Click(object sender, EventArgs e)
        {
            tsmiSave_Click(null, null);
        }
        #endregion  ################################################################################################################

        #region View toolbar  ######################################################################################################
        private void tsbFrontView_Click(object sender, EventArgs e)
        {
            tsmiFrontView_Click(null, null);
        }
        private void tsbBackView_Click(object sender, EventArgs e)
        {
            tsmiBackView_Click(null, null);
        }
        private void tsbTopView_Click(object sender, EventArgs e)
        {
            tsmiTopView_Click(null, null);
        }
        private void tsbBottomView_Click(object sender, EventArgs e)
        {
            tsmiBottomView_Click(null, null);
        }
        private void tsbLeftView_Click(object sender, EventArgs e)
        {
            tsmiLeftView_Click(null, null);
        }
        private void tsbRightView_Click(object sender, EventArgs e)
        {
            tsmiRightView_Click(null, null);
        }
        //
        private void tsbNormalView_Click(object sender, EventArgs e)
        {
            tsmiNormalView_Click(null, null);
        }
        private void tsbVerticalView_Click(object sender, EventArgs e)
        {
            tsmiVerticalView_Click(null, null);
        }
        //
        private void tsbIsometric_Click(object sender, EventArgs e)
        {
            tsmiIsometricView_Click(null, null);
        }
        //
        private void tsbZoomToFit_Click(object sender, EventArgs e)
        {
            tsmiZoomToFit_Click(null, null);
        }
        //
        private void tsbShowWireframeEdges_Click(object sender, EventArgs e)
        {
            tsmiShowWireframeEdges_Click(null, null);
        }
        private void tsbShowElementEdges_Click(object sender, EventArgs e)
        {
            tsmiShowElementEdges_Click(null, null);
        }
        private void tsbShowModelEdges_Click(object sender, EventArgs e)
        {
            tsmiShowModelEdges_Click(null, null);
        }
        private void tsbShowNoEdges_Click(object sender, EventArgs e)
        {
            tsmiShowNoEdges_Click(null, null);
        }
        //
        private void tsbSectionView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) TurnSectionViewOnOff();
            else if (e.Button == MouseButtons.Right) tsmiSectionView_Click(null, null);
        }
        private void tsbExplodedView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) TurnExplodedViewOnOff(true);
            else if (e.Button == MouseButtons.Right) tsmiExplodedView_Click(null, null);
        }
        //
        private void tsbHideAllParts_Click(object sender, EventArgs e)
        {
            tsmiHideAllParts_Click(sender, e);
        }
        private void tsbShowAllParts_Click(object sender, EventArgs e)
        {
            tsmiShowAllParts_Click(sender, e);
        }
        private void tsbInvertVisibleParts_Click(object sender, EventArgs e)
        {
            tsmiInvertVisibleParts_Click(sender, e);
        }
        //
        private void tsbQuery_Click(object sender, EventArgs e)
        {
            tsmiQuery_Click(sender, e);
        }
        private void tsbRemoveAnnotations_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Annotations.GetCurrentAnnotationNames().Length > 0)
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete current view annotations?") == DialogResult.OK)
                    {
                        _controller.Annotations.RemoveCurrentArrowAnnotations();
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        #endregion  ################################################################################################################

        #region Symbol toolbar  ####################################################################################################
        private void tscbSymbols_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If BC or load from one step-1 is selected its selection requires the step-1 to be selected.
            // Changing the step symbols is not possible -> Clear selection
            _controller.ClearAllSelection();
            //
            _controller.DrawSymbols(tscbSymbols.SelectedItem.ToString(), false);
            // Save the selected index
            _selectedSymbolIndex[_controller.CurrentView] = tscbSymbols.SelectedIndex;
            //
            this.ActiveControl = null;
        }
        public void SelectLastSymbolName()
        {
            tscbSymbols.SelectedIndex = tscbSymbols.Items.Count - 1;
        }
        public void UpdateSymbolsList()
        {
            InvokeIfRequired(() =>
            {
                ClearSymbolsDropDown();
                if (_controller.CurrentView == ViewGeometryModelResults.Geometry) { }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    tscbSymbols.Items.Add("Model");
                    tscbSymbols.Items.AddRange(_controller.GetStepNames());
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                    tscbSymbols.Items.Add("Results");
                // Retrieve the selected index
                int index = int.MaxValue;
                if (_selectedSymbolIndex != null && !_selectedSymbolIndex.TryGetValue(_controller.CurrentView, out index))
                {
                    index = tscbSymbols.Items.Count - 1;
                }
                // Check the index
                index = Math.Min(index, tscbSymbols.Items.Count - 1);
                // Set the index
                tscbSymbols.SelectedIndex = index;
            });
        }
        private void ClearSymbolsDropDown()
        {
            tscbSymbols.Items.Clear();
            tscbSymbols.Items.Add("None");
            tscbSymbols.SelectedIndexChanged -= tscbSymbols_SelectedIndexChanged;
            tscbSymbols.SelectedIndex = 0;
            tscbSymbols.SelectedIndexChanged += tscbSymbols_SelectedIndexChanged;
        }
        public void SelectOneStepInSymbolsForStepList(string stepName)
        {
            InvokeIfRequired(() =>
            {
                int index = -1;
                for (int i = 0; i < tscbSymbols.Items.Count; i++)
                {
                    if (tscbSymbols.Items[i].ToString() == stepName)
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1)
                {
                    tscbSymbols.SelectedIndexChanged -= tscbSymbols_SelectedIndexChanged;
                    tscbSymbols.SelectedIndex = index;
                    _selectedSymbolIndex[_controller.CurrentView] = index;
                    tscbSymbols.SelectedIndexChanged += tscbSymbols_SelectedIndexChanged;
                    //
                    _controller.DrawSymbols(tscbSymbols.SelectedItem.ToString(), false); // do not highlight!
                }
            });
        }

        #endregion  ################################################################################################################

        #region Deformation toolbar  ###############################################################################################
        private void tscbResultNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string currentResultName = _controller.AllResults.GetCurrentResultName();
                string newResultName = tscbResultNames.SelectedItem.ToString();
                if (newResultName != currentResultName)
                {
                    _controller.SetCurrentResultsCommand(newResultName);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }      
        private void tscbDeformationVariable_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                _controller.Redraw();
                this.ActiveControl = null;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tscbDeformationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Enable scale factor text box if needed
                UpdateScaleFactorTextBoxState();
                //
                _controller.Redraw();
                this.ActiveControl = null;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tstbDeformationFactor_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    _controller.Redraw();
                    this.ActiveControl = null;
                    // No beep
                    e.SuppressKeyPress = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tstbDeformationFactor_EnabledChanged(object sender, EventArgs e)
        {
            if (tstbDeformationFactor.Enabled) UpdateScaleFactorTextBoxState();
        }
        // Complex
        private void tscbComplex_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Enable angle text box if needed
                UpdateAngleTextBoxState();
                //
                _controller.Redraw();
                this.ActiveControl = null;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tstbAngle_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    tstbAngle.Text = Tools.GetPhase360(tstbAngle.Value).ToString();
                    //
                    _controller.Redraw();
                    this.ActiveControl = null;
                    // No beep
                    e.SuppressKeyPress = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        public void SetResultNames()
        {
            InvokeIfRequired(() =>
            {
                tscbResultNames.Items.Clear();
                string[] allResultNames = _controller.AllResults.GetResultNames();
                if (allResultNames != null && allResultNames.Length > 0)
                {
                    foreach (var name in allResultNames) tscbResultNames.Items.Add(name);
                    // Drop down width
                    int maxWidth = GetMaxStringWidth(allResultNames, tscbResultNames.Font);
                    tscbResultNames.DropDownWidth = maxWidth;
                    //
                    string currentResultName = _controller.AllResults.GetCurrentResultName();
                    if (currentResultName != null) tscbResultNames.SelectedItem = currentResultName;
                    //
                    ResizeResultNamesComboBox();
                }
            });
        }
        public void SetCurrentResults(string resultsName)
        {
            InvokeIfRequired(() =>
            {
                ResizeResultNamesComboBox(); // must be here
                //
                string currentResultName = _controller.AllResults.GetCurrentResultName();
                if (resultsName != currentResultName)
                {
                    // Clear
                    ClearActiveTreeSelection();
                    Clear3D();
                    // Set results
                    _controller.AllResults.SetCurrentResults(resultsName);
                    // Regenerate tree
                    RegenerateTree();
                    // Get first component of the first field for the last increment in the last step
                    if (_controller.ResultsInitialized) _controller.CurrentFieldData =
                            _controller.AllResults.CurrentResult.GetFirstComponentOfTheFirstFieldAtDefaultIncrement();
                    //
                    if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
                    {
                        // Reset the previous step and increment
                        SetAllStepAndIncrementIds();
                        // Set last increment
                        SetDefaultStepAndIncrementIds();
                        // Show the selection in the results tree
                        SelectFirstComponentOfFirstFieldOutput();
                        //
                        _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
                        //
                        SetMenuAndToolStripVisibility();
                        //tsmiZoomToFit_Click(null, null);    // different results have different views
                        SetCurrentEdgesVisibilities(_controller.CurrentEdgesVisibility);
                    }
                    //
                    UpdateComplexControlStates();
                    // Running this by from a command must change the results name
                    if (resultsName != tscbResultNames.SelectedItem.ToString())
                    {
                        tscbResultNames.SelectedIndexChanged -= tscbResultNames_SelectedIndexChanged;
                        tscbResultNames.SelectedItem = resultsName;
                        tscbResultNames.SelectedIndexChanged += tscbResultNames_SelectedIndexChanged;
                    }
                }
                //
                this.ActiveControl = null;
            });
        }
        private void ResizeResultNamesComboBox()
        {
            string[] allResultNames = new string[] { tscbResultNames.SelectedItem.ToString() };
            int currentWidth = GetMaxStringWidth(allResultNames, tscbResultNames.Font);
            // Control width
            currentWidth += 20; // to account for the drop down arrow
            if (currentWidth < 125) currentWidth = 125;
            else if (currentWidth > 400) currentWidth = 400;
            tscbResultNames.Size = new Size(currentWidth, tscbResultNames.Height);
            //
            Application.DoEvents();
        }
        private int GetMaxStringWidth(IEnumerable<string> items, Font font)
        {
            int maxWidth = 0;
            using (Graphics graphics = CreateGraphics())
            {
                foreach (string item in items)
                {
                    SizeF area = graphics.MeasureString(item, font);
                    maxWidth = Math.Max((int)area.Width, maxWidth);
                }
            }
            return maxWidth;
        }
        private void InitializeDeformationComboBoxes()
        {
            tscbDeformationVariable.Items.Clear();
            string[] variableNames = FeResults.GetPossibleDeformationFieldOutputNames();
            tscbDeformationVariable.Items.AddRange(variableNames);
            tscbDeformationVariable.SelectedIndex = 0;  // Displacements
            //
            tscbDeformationType.Items.Clear();
            Type type = typeof(DeformationScaleFactorTypeEnum);
            string[] typeNames = Enum.GetNames(type);
            for (int i = 0; i < typeNames.Length; i++)
            {
                typeNames[i] = ((DeformationScaleFactorTypeEnum)Enum.Parse(type, typeNames[i])).GetDisplayedName();
            }
            tscbDeformationType.Items.AddRange(typeNames);
            tscbDeformationType.SelectedIndex = 2;      // Automatic

            //if (controller.Results != null)
            //    vps.PopulateDropDownList(controller.Results.GetExistingDeformationFieldOutputNames());
            //else
            //    vps.PopulateDropDownList(CaeResults.FeResults.GetPossibleDeformationFieldOutputNames());
        }
        private void UpdateScaleFactorTextBoxState()
        {
            tslDeformationFactor.Enabled = GetDeformationType() == DeformationScaleFactorTypeEnum.UserDefined;
            tstbDeformationFactor.Enabled = tslDeformationFactor.Enabled;
        }
        public string GetDeformationVariable()
        {
            if (InvokeRequired) return (string)Invoke(new Func<string>(GetDeformationVariable));
            //
            return tscbDeformationVariable.SelectedItem.ToString();
        }
        public DeformationScaleFactorTypeEnum GetDeformationType()
        {
            // Invoke
            if (InvokeRequired)
                return (DeformationScaleFactorTypeEnum)Invoke(new Func<DeformationScaleFactorTypeEnum>(GetDeformationType));
            //
            string displayName = tscbDeformationType.SelectedItem.ToString();
            DeformationScaleFactorTypeEnum[] scaleFactorTypes =
                (DeformationScaleFactorTypeEnum[])Enum.GetValues(typeof(DeformationScaleFactorTypeEnum));
            //
            for (int i = 0; i < scaleFactorTypes.Length; i++)
            {
                if (displayName == scaleFactorTypes[i].GetDisplayedName()) return scaleFactorTypes[i];
            }
            //
            throw new NotSupportedException();
        }
        public float GetDeformationFactor()
        {
            return (float)tstbDeformationFactor.Value;
        }
        // Complex
        private void InitializeComplexComboBoxes()
        {
            tscbComplex.Items.Clear();
            //
            Type type = typeof(ComplexResultTypeEnum);
            string[] typeNames = Enum.GetNames(type);
            for (int i = 0; i < typeNames.Length; i++)
            {
                typeNames[i] = ((ComplexResultTypeEnum)Enum.Parse(type, typeNames[i])).GetDisplayedName();
            }
            tscbComplex.Items.AddRange(typeNames);
            tscbComplex.SelectedIndex = 0;  // Real
            //
            UpdateComplexControlStates();
        }
        private void UpdateComplexControlStates()
        {
            bool visible;
            if (_controller.ContainsComplexResults)
            {
                visible = true;
                bool enabled = true;
                //
                tslComplex.Enabled = enabled;
                tscbComplex.Enabled = enabled;
                if (enabled) UpdateAngleTextBoxState();
                else
                {
                    tslAngle.Enabled = false;
                    tstbAngle.Enabled = false;
                }
            }
            else
            {
                visible = false;
            }
            //
            SetComplexControlsVisibility(visible);
        }
        private void SetComplexControlsVisibility(bool visible)
        {
            tslComplex.Visible = visible;
            tscbComplex.Visible = visible;
            tslAngle.Visible = visible;
            tstbAngle.Visible = visible;
        }
        private void UpdateAngleTextBoxState()
        {
            bool enabled = GetComplexResultType() == ComplexResultTypeEnum.RealAtAngle;
            tslAngle.Enabled = enabled;
            tstbAngle.Enabled = enabled;
        }
        public ComplexResultTypeEnum GetComplexResultType()
        {
            // Invoke
            if (InvokeRequired)
                return (ComplexResultTypeEnum)Invoke(new Func<ComplexResultTypeEnum>(GetComplexResultType));
            //
            string displayName = tscbComplex.SelectedItem.ToString();
            ComplexResultTypeEnum[] complexResultTypes = (ComplexResultTypeEnum[])Enum.GetValues(typeof(ComplexResultTypeEnum));
            //
            for (int i = 0; i < complexResultTypes.Length; i++)
            {
                if (displayName == complexResultTypes[i].GetDisplayedName()) return complexResultTypes[i];
            }
            //
            throw new NotSupportedException();
        }
        public double GetComplexAngleDeg()
        {
            return tstbAngle.Value;
        }

        #endregion  ################################################################################################################

        #region Results field toolbar  #############################################################################################

        private void tsbResultsUndeformed_Click(object sender, EventArgs e)
        {
            tsmiResultsUndeformed_Click(null, null);
        }
        private void tsbResultsDeformed_Click(object sender, EventArgs e)
        {
            tsmiResultsDeformed_Click(null, null);
        }
        private void tsbResultsColorContours_Click(object sender, EventArgs e)
        {
            tsmiResultsColorContours_Click(null, null);
        }
        private void tsbResultsUndeformedWireframe_Click(object sender, EventArgs e)
        {
            tsmiResultsDeformedColorWireframe_Click(null, null);
        }
        private void tsbResultsUndeformedSolid_Click(object sender, EventArgs e)
        {
            tsmiResultsDeformedColorSolid_Click(null, null);
        }
        private void tsbTransformation_Click(object sender, EventArgs e)
        {
            tsmiTransformation_Click(null, null);
        }
        //
        private void FieldOutput_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                FieldData current = _controller.CurrentFieldData;
                SetFieldData(current.Name, current.Component, GetCurrentFieldOutputStepId(), GetCurrentFieldOutputStepIncrementId());
            }
            catch
            { }
        }
        private void tsbPreviousStepIncrement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tscbStepAndIncrement.Enabled && tscbStepAndIncrement.SelectedIndex > 0) 
                    tscbStepAndIncrement.SelectedIndex--;
            }
            catch
            { }
        }
        private void tsbNextStepIncrement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tscbStepAndIncrement.Enabled && tscbStepAndIncrement.SelectedIndex < tscbStepAndIncrement.Items.Count - 1) 
                    tscbStepAndIncrement.SelectedIndex++;
            }
            catch
            { }
        }
        private void tsbFirstStepIncrement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tscbStepAndIncrement.Enabled) tscbStepAndIncrement.SelectedIndex = 0;
            }
            catch
            { }
        }
        private void tsbLastStepIncrement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tscbStepAndIncrement.Enabled) tscbStepAndIncrement.SelectedIndex = tscbStepAndIncrement.Items.Count - 1;
            }
            catch
            { }
        }
        private void tsbAnimate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.ViewResultsType != ViewResultsTypeEnum.Undeformed &&
                    _controller.GetResultStepIDs().Length > 0 && !_frmAnimation.Visible)
                {
                    CloseAllForms();
                    SetMenuAndToolStripVisibilityByAnimation(false);
                    SetFormLocation(_frmAnimation);
                    _frmAnimation.PrepareForm(this, _controller);
                    if (_frmAnimation.DialogResult == DialogResult.Abort) SetMenuAndToolStripVisibilityByAnimation(true);
                    else _frmAnimation.Show();
                }
            }
            catch
            { }
        }

        #endregion  ################################################################################################################

        #region Status strip  ######################################################################################################
        public void SetState(string text, bool working)
        {
            InvokeIfRequired(() =>
            {
                tsslState.Text = text;
                // Progress
                if (working) tspbProgress.Style = ProgressBarStyle.Marquee;
                else tspbProgress.Style = ProgressBarStyle.Blocks;
                tspbProgress.Visible = working;
                // Rendering - vtk
                if (text == Globals.ExplodePartsText) _vtk.RenderingOn = true;
                else _vtk.RenderingOn = !working;
                _vtk.Enabled = !working;
                //_vtk.Visible = !working;
                // Hack
                //if (!working) _vtk.Left = 1;
                //else _vtk.Left = 100000;
                //
                bool menusActive = !working;
                SetMenuAndToolStripVisibilityBySetState(menusActive);
            });
        }
        public void SetStateReady(string currentText)
        {
            if (tsslState.Text == currentText) // check that the same command is being canceled
            {
                SetState(Globals.ReadyText, false);
                tsslCancel.Visible = false;
            }
        }
        public bool SetStateWorking(string text, bool showCancelButton = false)
        {
            if (tsslState.Text == Globals.ReadyText) // check that the state is ready
            {
                SetState(text, true);
                tsslCancel.Visible = showCancelButton;
                return true;
            }
            return false;
        }
        private bool IsStateWorking()
        {
            return tsslState.Text != Globals.ReadyText;
        }
        public bool IsStateOpening()
        {
            return tsslState.Text == Globals.OpeningText;
        }
        public bool IsStateRegeneratingOrUndoing()
        {
            return tsslState.Text == Globals.RegeneratingText || tsslState.Text == Globals.UndoingText;
        }
        //
        private void tsslCancel_MouseDown(object sender, MouseEventArgs e)
        {
            tsslCancel.BorderStyle = Border3DStyle.Sunken;
        }
        private void tsslCancel_MouseUp(object sender, MouseEventArgs e)
        {
            tsslCancel.BorderStyle = Border3DStyle.Raised;
        }
        private void tsslCancel_MouseLeave(object sender, EventArgs e)
        {
            tsslCancel.BorderStyle = Border3DStyle.Raised;
        }
        private void tsslCancel_Click(object sender, EventArgs e)
        {
            _controller.StopExecutableJob();
        }
        #endregion  ################################################################################################################

        // Methods                                                                                                                  
        #region Methods
        public void SetTitle(string title)
        {
            InvokeIfRequired(() => this.Text = title);
        }
        private bool CheckValidity()
        {
            string[] invalidItems = _controller.CheckAndUpdateModelValidity();
            if (invalidItems.Length > 0)
            {
                string text = "The model contains active invalid items:" + Environment.NewLine;
                foreach (var item in invalidItems) text += Environment.NewLine + item;
                text += Environment.NewLine + Environment.NewLine + "Continue?";
                return MessageBoxes.ShowWarningQuestionOKCancel(text) == DialogResult.OK;
            }
            return true;
        }

        public double[] GetBoundingBox()
        {
            // xMin, xMax, yMin, yMax, zMin, zMax
            return _vtk.GetBoundingBox();
        }
        public double[] GetBondingBoxSize()
        {
            return _vtk.GetBoundingBoxSize();
        }
        public string GetFileNameToOpen()
        {
            return GetFileNameToOpen(GetFileOpenFilter());
        }
        public string GetFileNameToOpen(string filter)
        {
            string fileName = null;
            InvokeIfRequired(() =>
            {
                openFileDialog.Filter = filter;
                openFileDialog.FileName = "";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = openFileDialog.FileName;
                }
            });
            return fileName;
        }
        public string GetFileNameToImport(bool onlyMaterials)
        {
            return GetFileNameToImport(GetFileImportFilter(onlyMaterials));
        }
        public string GetFileNameToImport(string filter)
        {
            string fileName = null;
            InvokeIfRequired(() =>
            {
                openFileDialog.Filter = filter;
                openFileDialog.FileName = "";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = openFileDialog.FileName;
                }
            });
            return fileName;
        }
        public string GetFileNameToSaveAs()
        {
            string fileName = null;
            InvokeIfRequired(() =>
            {
                saveFileDialog.Filter = "PrePoMax files | *.pmx";
                //
                fileName = Path.GetFileName(_controller.OpenedFileName);
                saveFileDialog.FileName = fileName;
                //
                saveFileDialog.OverwritePrompt = true;
                //
                if (saveFileDialog.ShowDialog() == DialogResult.OK) fileName = saveFileDialog.FileName;
                else fileName = null;
            });
            return fileName;
        }
        public string[] GetFileNamesToImport(string filter)
        {
            string[] fileNames = null;
            InvokeIfRequired(() =>
            {
                // create new dialog to enable multiFilter
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Multiselect = true;
                    openFileDialog.Filter = filter;
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        fileNames = openFileDialog.FileNames;
                    }
                }
            });
            return fileNames;
        }
        private string GetFileOpenFilter()
        {
            string filter;
            // Debugger attached
            if (Debugger.IsAttached)
            {
                filter = "All files|*.pmx;*.pmh;*.frd;*.dat;*.foam" +
                         "|PrePoMax files|*.pmx" +
                         "|PrePoMax history|*.pmh" +
                         "|Calculix result files|*.frd" +
                         "|Calculix dat files|*.dat" +       // added .dat file
                         "|OpenFoam files|*.foam";

            }
            // No debugger
            else
            {
                filter = "All files|*.pmx;*.pmh;*.frd;*.foam" +
                         "|PrePoMax files|*.pmx" +
                         "|PrePoMax history|*.pmh" +
                         "|Calculix result files|*.frd" +
                         "|OpenFoam files|*.foam";
            }
            return filter;
        }
        private string GetFileImportFilter(bool onlyMaterials)
        {
            if (onlyMaterials) return "Abaqus/Calculix inp files|*.inp";
            // Debugger attached
            string filter;
            if (Debugger.IsAttached)
            {
                filter = "All supported files|*.stp;*.step;*.igs;*.iges;*.brep;*.stl;*.unv;*.vol;*.inp;*.mesh;*.obj" + 
                         "|Step files|*.stp;*.step" +
                         "|Iges files|*.igs;*.iges" +
                         "|Brep files|*.brep" +
                         "|Stereolithography files|*.stl" +
                         "|Universal files|*.unv" +
                         "|Netgen files|*.vol" +
                         "|Abaqus/Calculix inp files|*.inp" +
                         "|Mmg mesh files|*.mesh" +
                         "|Wavefront obj files|*.obj";              // obj mesh reader added

            }
            // No debugger
            else
            {
                filter = "All supported files|*.stp;*.step;*.igs;*.iges;*.brep;*.stl;*.unv;*.vol;*.inp;*.mesh" +
                         "|Step files|*.stp;*.step" +
                         "|Iges files|*.igs;*.iges" +
                         "|Brep files|*.brep" +
                         "|Stereolithography files|*.stl" +
                         "|Universal files|*.unv" +
                         "|Netgen files|*.vol" +
                         "|Abaqus/Calculix inp files|*.inp" +
                         "|Mmg mesh files|*.mesh";
            }
            return filter;
        }
        private HashSet<string> GetFileImportExtensions()
        {
            string[] tmp = GetFileImportFilter(false).Split(new string[] { "*", "\"", ";", "|" },
                                                            StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> extensions = new HashSet<string>();
            foreach (var entry in tmp)
            {
                if (entry.StartsWith(".")) extensions.Add(entry);
            }
            return extensions;
        }

        #region Clear  #############################################################################################################
        public void ClearControls()
        {
            InvokeIfRequired(() =>
            {
                _vtk.Clear();
                _vtk.RemoveAllArrowWidgets();
                _modelTree.Clear();
                outputLines = new string[0];
                tbOutput.Text = "";
                ClearResults();
                ClearAnnotationStatus();
                _selectedSymbolIndex = new Dictionary<ViewGeometryModelResults, int>();
            });
        }
        public void ClearResults()
        {
            InvokeIfRequired(() =>
            {
                tscbResultNames.Items.Clear();
                tscbResultNames.Size = new Size(125, tscbResultNames.Height);
                tscbStepAndIncrement.Items.Clear();
                _modelTree.ClearResults();
                //
                SetMenuAndToolStripVisibility();
            });
        }
        public void Clear3D()
        {
            InvokeIfRequired(_vtk.Clear);
        }
        public void ClearButKeepParts()
        {
            InvokeIfRequired(_vtk.ClearButKeepParts);
        }
        public void ClearSelection()
        {
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
        }
        public void Clear3DSelection()
        {
            InvokeIfRequired(() => _vtk.ClearSelection());
        }
        public void ClearActiveTreeSelection()
        {
            InvokeIfRequired(_modelTree.ClearActiveTreeSelection);
        }
        private void ClearAnnotationStatus()
        {
            tsmiAnnotateFaceOrientations.Checked = false;
            tsmiAnnotateParts.Checked = false;
            tsmiAnnotateMaterials.Checked = false;
            tsmiAnnotateSections.Checked = false;
            tsmiAnnotateSectionThicknesses.Checked = false;
            tsmiAnnotateReferencePoints.Checked = false;
            tsmiAnnotateConstraints.Checked = false;
            tsmiAnnotateContactPairs.Checked = false;
            tsmiAnnotateInitialConditions.Checked = false;
            tsmiAnnotateBCs.Checked = false;
            tsmiAnnotateLoads.Checked = false;
            tsmiAnnotateAllSymbols.Checked = false;
            //
            HideColorBar();
        }

        #endregion  ################################################################################################################

        #region vtkControl  ########################################################################################################
        // vtkControl
        public void SetFrontBackView(bool animate, bool front)
        {
            InvokeIfRequired(_vtk.SetFrontBackView, animate, front);
        }
        public void SetZoomToFit(bool animate)
        {
            InvokeIfRequired(_vtk.SetZoomToFit, animate);
        }
        public double[] GetViewPlaneNormal()
        {
            if (this.InvokeRequired)
            {
                return (double[])this.Invoke((MethodInvoker)delegate () { _vtk.GetViewPlaneNormal(); });
            }
            else
            {
                return _vtk.GetViewPlaneNormal();
            }
        }
        public void AdjustCameraDistanceAndClipping()
        {
            InvokeIfRequired(_vtk.AdjustCameraDistanceAndClipping);
        }
        public void UpdateScalarsAndRedraw()
        {
            InvokeIfRequired(_vtk.UpdateScalarsAndRedraw);
        }
        public void UpdateScalarsAndCameraAndRedraw()
        {
            InvokeIfRequired(_vtk.UpdateScalarsAndCameraAndRedraw);
        }
        // Section view
        public void CreateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor)
        {
            InvokeIfRequired(_vtk.CreateSectionView, point, normal, lightenColors, sectionColor);
            InvokeIfRequired(() => { tsbSectionView.Checked = true; });
        }
        public void UpdateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor)
        {
            InvokeIfRequired(_vtk.UpdateSectionView, point, normal, lightenColors, sectionColor);
            InvokeIfRequired(() => { tsbSectionView.Checked = true; });
        }
        public void RemoveSectionView()
        {
            InvokeIfRequired(_vtk.RemoveSectionView);
            InvokeIfRequired(() => { tsbSectionView.Checked = false; });
        }
        // Exploded view
        public void PreviewExplodedView(Dictionary<string, double[]> partOffsets, bool animate, int timeMs)
        {
            InvokeIfRequired(_vtk.PreviewExplodedView, partOffsets, animate, timeMs);
        }
        public void RemovePreviewedExplodedView(string[] partNames)
        {
            InvokeIfRequired(_vtk.RemovePreviewedExplodedView, partNames);
        }
        public void SetExplodedViewStatus(bool status)
        {
            InvokeIfRequired(() => { tsbExplodedView.Checked = status; });
        }
        // Transformations
        public void AddSymmetry(int symmetryPlane, double[] symmetryPoint)
        {
            InvokeIfRequired(_vtk.AddSymmetry, symmetryPlane, symmetryPoint);
        }
        public void AddLinearPattern(double[] displacement, int numOfItems)
        {
            InvokeIfRequired(_vtk.AddLinearPattern, displacement, numOfItems);
        }
        public void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numOfItems)
        {
            InvokeIfRequired(_vtk.AddCircularPattern, axisPoint, axisNormal, angle, numOfItems);
        }
        public void ApplyTransformations()
        {
            InvokeIfRequired(_vtk.ApplyTransforms);
        }
        public void SetTransformationsStatus(bool status)
        {
            InvokeIfRequired(() => { tsbTransformation.Checked = status; });
        }
        public void HideTransformedActors()
        {
            InvokeIfRequired(_vtk.HideTransformedActors);
        }
        public void ShowTransformedActors()
        {
            InvokeIfRequired(_vtk.ShowTransformedActors);
        }
        //
        public vtkMaxActor Add3DNodes(vtkMaxActorData nodeData)
        {
            vtkMaxActor actor = null;
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate () { actor = _vtk.AddPoints(nodeData); });
            }
            else
            {
                actor = _vtk.AddPoints(nodeData);
            }
            return actor;
        }
        public vtkMaxActor Add3DCells(vtkMaxActorData cellData)
        {
            vtkMaxActor actor = null;
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate () { actor = _vtk.AddCells(cellData); });
            }
            else
            {
                actor = _vtk.AddCells(cellData);
            }
            return actor;
        }
        public void AddActor(vtkMaxActor actor)
        {
            InvokeIfRequired(_vtk.AddActor, actor);
        }
        public void AddScalarFieldOn3DCells(vtkControl.vtkMaxActorData actorData, bool update)
        {
            InvokeIfRequired(_vtk.AddScalarFieldOnCells, actorData, update);
        }
        public bool AddAnimatedScalarFieldOn3DCells(vtkControl.vtkMaxActorData actorData)
        {
            return _vtk.AddAnimatedScalarFieldOnCells(actorData);
        }
        public void UpdateActorSurfaceScalarField(string actorName, float[] values, NodesExchangeData extremeNodes,
                                                  float[] frustumCellLocatorValues, bool update)
        {
            InvokeIfRequired(_vtk.UpdateActorScalarField, actorName, values, extremeNodes, frustumCellLocatorValues, update);
        }
        public void UpdateActorColorContoursVisibility(string[] actorNames, bool colorContour)
        {
            InvokeIfRequired(_vtk.UpdateActorsColorContoursVisibility, actorNames, colorContour);
        }
        public void AddSphereActor(vtkControl.vtkMaxActorData actorData, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddSphereActor, actorData, symbolSize);
        }
        public void AddCoordinateAxis(vtkMaxActorData data, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddCoordinateAxis, data, symbolSize);
        }
        public void AddCaptionActor(string name, string caption, Color color, double[] position,
                                    double[] offsetVector, double fontScaleFactor, vtkRendererLayer layer)
        {
            InvokeIfRequired(_vtk.AddCaptionActor, name, caption, color, position, offsetVector, fontScaleFactor, layer);
        }
        public void AddOrientedDisplacementConstraintActor(vtkControl.vtkMaxActorData actorData, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddOrientedDisplacementConstraintActor, actorData, symbolSize);
        }
        public void AddOrientedRotationalConstraintActor(vtkControl.vtkMaxActorData actorData, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddOrientedRotationalConstraintActor, actorData, symbolSize);
        }
        public void AddOrientedArrowsActor(vtkControl.vtkMaxActorData actorData, double symbolSize, bool invert = false, 
                                           double relativeSize = 1)
        {
            InvokeIfRequired(_vtk.AddOrientedArrowsActor, actorData, symbolSize, invert, relativeSize);
        }
        public void AddOrientedDoubleArrowsActor(vtkControl.vtkMaxActorData actorData, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddOrientedDoubleArrowsActor, actorData, symbolSize);
        }
        public void AddOrientedSpringActor(vtkControl.vtkMaxActorData actorData, double symbolSize, bool invert = false)
        {
            InvokeIfRequired(_vtk.AddOrientedSpringActor, actorData, symbolSize, invert);
        }
        public void AddOrientedThermosActor(vtkControl.vtkMaxActorData actorData, double symbolSize, bool invert = false)
        {
            InvokeIfRequired(_vtk.AddOrientedThermoActor, actorData, symbolSize, invert);
        }
        public void AddOrientedFluxActor(vtkControl.vtkMaxActorData actorData, double symbolSize, bool center, bool invert)
        {
            InvokeIfRequired(_vtk.AddOrientedFluxActor, actorData, symbolSize, center, invert);
        }
        //
       
        //
        public bool ContainsActor(string actorName)
        {
            return _vtk.ContainsActor(actorName);
        }
        public void HighlightActor(string actorName)
        {
            InvokeIfRequired(_vtk.HighlightActor, actorName);
        }
        public void UpdateActor(string oldName, string newName, Color newColor)
        {
            InvokeIfRequired(_vtk.UpdateActor, oldName, newName, newColor);
        }
        public void HideActors(string[] actorNames, bool updateColorContours)
        {
            InvokeIfRequired(_vtk.HideActors, actorNames, updateColorContours);
        }
        public void ShowActors(string[] actorNames, bool updateColorContours)
        {
            InvokeIfRequired(_vtk.ShowActors, actorNames, updateColorContours);
        }
        // Settings                                             
        public void SetCoorSysVisibility(bool visibility)
        {
            InvokeIfRequired(_vtk.SetCoorSysVisibility, visibility);
        }
        // Scale bar
        public void SetScaleWidgetVisibility(bool visibility)
        {
            InvokeIfRequired(_vtk.SetScaleWidgetVisibility, visibility);
        }
        private void SetScaleWidgetUnit(UnitSystem unitSystem)
        {
            string unit = "";
            if (unitSystem.UnitSystemType != UnitSystemType.Undefined) unit = unitSystem.LengthUnitAbbreviation;
            //
            InvokeIfRequired(_vtk.SetScaleWidgetUnit, unit);
        }
        // Scalar bar
        public void InitializeResultWidgetPositions()
        {
            InvokeIfRequired(_vtk.InitializeResultWidgetPositions);
        }
        public void SetScalarBarColorSpectrum(vtkControl.vtkMaxColorSpectrum colorSpectrum)
        {
            InvokeIfRequired(_vtk.SetScalarBarColorSpectrum, colorSpectrum);
        }
        public void SetScalarBarNumberFormat(string numberFormat)
        {
            InvokeIfRequired(_vtk.SetScalarBarNumberFormat, numberFormat);
        }
        public void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string complexComponent,
                                     string minMaxType)
        {
            InvokeIfRequired(_vtk.SetScalarBarText, fieldName, componentName, unitAbbreviation, complexComponent, minMaxType);
        }
        public void DrawLegendBackground(bool drawBackground)
        {
            InvokeIfRequired(_vtk.DrawScalarBarBackground, drawBackground);
        }
        public void DrawLegendBorder(bool drawBorder)
        {
            InvokeIfRequired(_vtk.DrawScalarBarBorder, drawBorder);
        }
        // Color bar
        public void InitializeColorBarWidgetPosition()
        {
            InvokeIfRequired(_vtk.InitializeColorBarWidgetPosition);
        }
        public void SetColorBarColorsAndLabels(Color[] colors, string[] labels)
        {
            InvokeIfRequired(_vtk.SetColorBarColorsAndLabels, colors, labels);
        }
        public void AddColorBarColorsAndLabels(Color[] colors, string[] labels)
        {
            InvokeIfRequired(_vtk.AddColorBarColorsAndLabels, colors, labels);
        }
        public void DrawColorBarBackground(bool drawBackground)
        {
            InvokeIfRequired(_vtk.DrawColorBarBackground, drawBackground);
        }
        public void DrawColorBarBorder(bool drawBorder)
        {
            InvokeIfRequired(_vtk.DrawColorBarBorder, drawBorder);
        }
        public void HideColorBar()
        {
            InvokeIfRequired(_vtk.HideColorBar);
        }
        // Status bar
        public void SetStatusBlockVisibility(bool draw)
        {
            InvokeIfRequired(_vtk.SetStatusBlockVisibility, draw);
        }
        public void DrawStatusBlockBackground(bool drawBackground)
        {
            InvokeIfRequired(_vtk.DrawStatusBlockBackground, drawBackground);
        }
        public void DrawStatusBlockBorder(bool drawBorder)
        {
            InvokeIfRequired(_vtk.DrawStatusBlockBorder, drawBorder);
        }
        public void SetStatusBlock(string name, DateTime dateTime, float analysisTime, string unit, string deformationVariable,
                                   float scaleFactor, vtkControl.vtkMaxFieldDataType fieldType, int stepNumber, int incrementNumber)
        {
            InvokeIfRequired(_vtk.SetStatusBlock, name, dateTime, analysisTime, unit, deformationVariable, scaleFactor,
                             fieldType, stepNumber, incrementNumber);
        }
        // General
        public void SetBackground(bool gradient, Color topColor, Color bottomColor, bool redraw)
        {
            InvokeIfRequired(_vtk.SetBackground, gradient, topColor, bottomColor, redraw);
        }
        public void SetLighting(double ambient, double diffuse, bool redraw)
        {
            InvokeIfRequired(_vtk.SetLighting, ambient, diffuse, redraw);
        }
        public void SetSmoothing(bool pointSmoothing, bool lineSmoothing, bool redraw)
        {
            InvokeIfRequired(_vtk.SetSmoothing, pointSmoothing, lineSmoothing, redraw);
        }
        // Highlight
        public void SetHighlightColor(Color primaryHighlightColor, Color secondaryHighlightColor)
        {
            InvokeIfRequired(_vtk.SetHighlightColor, primaryHighlightColor, secondaryHighlightColor);
        }
        public void SetMouseHighlightColor(Color mouseHighlightColor)
        {
            InvokeIfRequired(_vtk.SetMouseHighlightColor, mouseHighlightColor);
        }
        // Symbols
        public void SetDrawSymbolEdges(bool drawSilhouettes)
        {
            InvokeIfRequired(_vtk.SetDrawSymbolEdges, drawSilhouettes);
        }
        //
        public void CropPartWithCylinder(string partName, double r, string fileName)
        {
            InvokeIfRequired(_vtk.CropPartWithCylinder, partName, r, fileName);
        }
        public void CropPartWithCube(string partName, double a, string fileName)
        {
            InvokeIfRequired(_vtk.CropPartWithCube, partName, a, fileName);
        }
        public void SmoothPart(string partName, double a, string fileName)
        {
            InvokeIfRequired(_vtk.SmoothPart, partName, a, fileName);
        }
        // User pick
        public void ActivateUserPick()
        {
            _vtk.UserPick = true;
        }
        public void DeactivateUserPick()
        {
            _vtk.UserPick = false;
        }
        #endregion  ################################################################################################################

        #region Results  ###########################################################################################################
        // Results
        public void SetFieldData(string name, string component)
        {
            InvokeIfRequired(() =>
            {
                SetFieldData(name, component, GetCurrentFieldOutputStepId(), GetCurrentFieldOutputStepIncrementId());
            });
        }
        public void SetFieldData(string name, string component, int stepId, int stepIncrementId)
        {
            FieldData fieldData = new FieldData(name, component, stepId, stepIncrementId);
            FieldData currentData = _controller.CurrentFieldData;
            // In case the currentData is null exit
            if (currentData == null) return;
            //
            if (!fieldData.Equals(currentData)) // update results only if field data changed
            {
                // Stop and update animation data only if field data changed
                if (_frmAnimation.Visible) _frmAnimation.Hide();
                //
                if (fieldData.Name == currentData.Name && fieldData.Component == currentData.Component)
                {
                    // Step id or increment id changed                                              

                    // Find the chosen data; also contains info about type of step ...
                    fieldData = _controller.CurrentResult.GetFieldData(fieldData.Name,
                                                                       fieldData.Component,
                                                                       fieldData.StepId,
                                                                       fieldData.StepIncrementId,
                                                                       true);
                    // Update controller field data
                    _controller.CurrentFieldData = fieldData;
                    // Draw deformation or field data
                    if (_controller.ViewResultsType != ViewResultsTypeEnum.Undeformed) _controller.DrawResults(false);
                }
                else
                {
                    // Field of field component changed                                                 

                    // Update controller field data; this is used for the SetStepAndIncrementIds to detect missing ids
                    _controller.CurrentFieldData = fieldData;
                    // Find the existing chosen data; also contains info about type of step ...
                    fieldData = _controller.CurrentResult.GetFieldData(fieldData.Name,
                                                                       fieldData.Component,
                                                                       fieldData.StepId,
                                                                       fieldData.StepIncrementId,
                                                                       true);
                    // Update controller field data
                    _controller.CurrentFieldData = fieldData;
                    // Draw field data
                    if (_controller.ViewResultsType == ViewResultsTypeEnum.ColorContours) _controller.UpdatePartsScalarFields();
                }
                //
                UpdateComplexControlStates();
                // Move focus from step and step increment dropdown menus
                this.ActiveControl = null;
            }
        }
        public void SetAllStepAndIncrementIds(bool reset = false)
        {
            InvokeIfRequired(() =>
            {
                // Save current step and increment id
                string currentStepIncrement = (string)tscbStepAndIncrement.SelectedItem;
                string[] prevStepIncrementIds = null;
                if (!reset && currentStepIncrement != null)
                {
                    prevStepIncrementIds = currentStepIncrement.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                }
                // Set all increments
                tscbStepAndIncrement.SelectedIndexChanged -= FieldOutput_SelectionChanged;  // detach event
                tscbStepAndIncrement.Items.Clear();
                Dictionary<int, int[]> allIds = _controller.CurrentResult.GetAllExistingIncrementIds();
                int lastStepId = 1;
                int lastIncrementId = 0;
                foreach (var entry in allIds)
                {
                    foreach (int incrementId in entry.Value)
                    {
                        tscbStepAndIncrement.Items.Add(entry.Key.ToString() + ", " + incrementId);
                        lastIncrementId = incrementId;
                    }
                    lastStepId = entry.Key;
                }
                tscbStepAndIncrement.SelectedIndexChanged += FieldOutput_SelectionChanged;  // reattach event
                // Reselect previous step and increment
                if (prevStepIncrementIds != null)
                {
                    int stepId = Math.Min(int.Parse(prevStepIncrementIds[0]), lastStepId);
                    int incrementId = Math.Min(int.Parse(prevStepIncrementIds[1]), lastIncrementId);
                    SetStepAndIncrementIds(stepId, incrementId);
                }
                else SetDefaultStepAndIncrementIds();
            });
        }
        public void SetStepAndIncrementIds(int stepId, int incrementId)
        {
            InvokeIfRequired(() =>
            {
                string stepIncrement = stepId + ", " + incrementId;
                // Set the combo box
                if (tscbStepAndIncrement.Items.Contains(stepIncrement))
                {
                    tscbStepAndIncrement.SelectedIndexChanged -= FieldOutput_SelectionChanged;
                    tscbStepAndIncrement.SelectedItem = stepIncrement;
                    // Set the step and increment if the combo box set was successful
                    FieldData data = _controller.CurrentFieldData;
                    data.StepId = stepId;
                    data.StepIncrementId = incrementId;
                    _controller.CurrentFieldData = data;   // to correctly update the increment time
                    tscbStepAndIncrement.SelectedIndexChanged += FieldOutput_SelectionChanged;
                }
                else SetDefaultStepAndIncrementIds();
            });
        }
        public void SetDefaultStepAndIncrementIds()
        {
            string[] tmp;
            FieldData fieldData = _controller.CurrentFieldData;
            if (fieldData != null)
            {
                if (fieldData.StepId == -1 && fieldData.StepIncrementId == -1) return;
                else SetStepAndIncrementIds(fieldData.StepId, fieldData.StepIncrementId);
            }
            //
            return;
            //
            if (_controller.CurrentFieldData.StepType == CaeResults.StepTypeEnum.Frequency)
            {
                string firstStepIncrement = (string)tscbStepAndIncrement.Items[tscbStepAndIncrement.Items.Count - 1];
            }
            else
            {
                string lastStepIncrement = (string)tscbStepAndIncrement.Items[tscbStepAndIncrement.Items.Count - 1];
                tmp = lastStepIncrement.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                
            }
        }
        public int GetCurrentFieldOutputStepId()
        {
            string selectedStepIncrement = (string)tscbStepAndIncrement.SelectedItem;
            string[] tmp = selectedStepIncrement.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            return int.Parse(tmp[0]);
        }
        public int GetCurrentFieldOutputStepIncrementId()
        {
            string selectedStepIncrement = (string)tscbStepAndIncrement.SelectedItem;
            string[] tmp = selectedStepIncrement.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            return int.Parse(tmp[1]);
        }
        public void SetAnimationAcceleration(bool animationAcceleration)
        {
            InvokeIfRequired(_vtk.SetAnimationAcceleration, animationAcceleration);
        }
        public void SetAnimationFrameData(float[] time, int[] stepId, int[] stepIncrementId, float[] scale,
                                          double[] allFramesScalarRange, vtkMaxAnimationType animationType)
        {
            InvokeIfRequired(_vtk.SetAnimationFrameData, time, stepId, stepIncrementId, scale, allFramesScalarRange,
                             animationType);
        }
        public void SetAnimationFrame(int frameNum, bool scalarRangeFromAllFrames)
        {
            InvokeIfRequired(_vtk.SetAnimationFrame, frameNum, scalarRangeFromAllFrames);
        }
        public void SaveAnimationAsAVI(string fileName, int[] firstLastFrame, int step, int fps, bool scalarRangeFromAllFrames,
                                       bool swing, bool encoderOptions)
        {
            InvokeIfRequired(_vtk.SaveAnimationAsAVI, fileName, firstLastFrame, step, fps, scalarRangeFromAllFrames,
                             swing, encoderOptions);
        }
        public void SaveAnimationAsImages(string fileName, int[] firstLastFrame, int step, bool scalarRangeFromAllFrames,
                                          bool swing)
        {
            InvokeIfRequired(_vtk.SaveAnimationAsImages, fileName, firstLastFrame, step, scalarRangeFromAllFrames, swing);
        }
        // Widgets
        public void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint,
                                   bool drawBackground, bool drawBorder, bool visible)
        {
            InvokeIfRequired(_vtk.AddArrowWidget, name, text, numberFormat, anchorPoint, drawBackground, drawBorder, visible);
        }
        public void RemoveAllArrowWidgets()
        {
            InvokeIfRequired(_vtk.RemoveAllArrowWidgets);
        }
        public void RemoveArrowWidgets(string[] widgetNames)
        {
            InvokeIfRequired(_vtk.RemoveArrowWidgets, widgetNames);
        }

        #endregion  ################################################################################################################

        #region Tree  ##############################################################################################################
        // Tree
        public void RegenerateTreeCallback()
        {
            Console.WriteLine("FrmMain::RegenerateTreeCallback");
            RegenerateTree();
        }
        public void RegenerateTree(bool remeshing = false)
        {
            Console.WriteLine("FrmMain::RegenerateTree");
            InvokeIfRequired(_modelTree.RegenerateTree, _controller.Model, _controller.Jobs, _controller.CurrentResult, remeshing);
            InvokeIfRequired(UpdateSymbolsList);
            InvokeIfRequired(SelectLastSymbolName);
        }
        public void AddTreeNode(ViewGeometryModelResults view, NamedClass item, string parentName)
        {
            ViewType viewType = GetViewType(view);
            //
            InvokeIfRequired(_modelTree.AddTreeNode, viewType, item, parentName);
            if (item is Step) UpdateSymbolsList();
        }
        public void UpdateTreeNode(ViewGeometryModelResults view, string oldItemName, NamedClass item, string parentName,
                                   bool updateSelection = true)
        {
            ViewType viewType = GetViewType(view);
            //
            InvokeIfRequired(_modelTree.UpdateTreeNode, viewType, oldItemName, item, parentName, updateSelection);
            if (item is Step) UpdateSymbolsList();
        }
        public void SwapTreeNode(ViewGeometryModelResults view, string firstItemName, NamedClass firstItem,
                                string secondItemName, NamedClass secondItem, string parentName)
        {
            ViewType viewType = GetViewType(view);
            //
            InvokeIfRequired(_modelTree.SwapTreeNodes, viewType, firstItemName, firstItem, secondItemName,
                             secondItem, parentName);
            //if (item is Step) UpdateOneStepInSymbolsForStepList(oldItemName, item.Name);
        }
        public void RemoveTreeNode<T>(ViewGeometryModelResults view, string nodeName, string parentName) where T : NamedClass
        {
            ViewType viewType = GetViewType(view);
            //
            InvokeIfRequired(_modelTree.RemoveTreeNode<T>, viewType, nodeName, parentName);
            if (typeof(T) == typeof(Step)) UpdateSymbolsList();
        }
        public bool[][] GetTreeExpandCollapseState()
        {
            if (InvokeRequired)
            {
                return (bool[][])Invoke((Func<bool[][]>)delegate
                    { return _modelTree.GetAllTreesExpandCollapseState(out string[][] afterNodeNames); });
            }
            else
            {
                return _modelTree.GetAllTreesExpandCollapseState(out string[][] afterNodeNames);
            }
        }
        public void SetTreeExpandCollapseState(bool[][] states)
        {
            InvokeIfRequired(_modelTree.SetAllTreeExpandCollapseState, states);
        }
        public void UpdateHighlight()
        {
            if (_allForms == null) return;
            //
            List<IFormHighlight> highlightForms = new List<IFormHighlight>();
            foreach (var aForm in _allForms)
            {
                // Do not count the Query form
                if (aForm.Visible && (aForm is IFormHighlight ihf)) highlightForms.Add(ihf);
            }
            if (highlightForms.Count == 0) UpdateHighlightFromTree();
            else if (highlightForms.Count == 1) highlightForms[0].Highlight();
            else throw new NotSupportedException();
        }
        public void UpdateHighlightFromTree()
        {
            InvokeIfRequired(_modelTree.UpdateHighlight);
        }
        public void SelectBaseParts(string[] partNames)
        {
            MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0);
            InvokeIfRequired(SelectBaseParts, e, Keys.None, partNames);
        }
        public void SelectBaseParts(MouseEventArgs e, Keys modifierKeys, string[] partNames)
        {
            if ((partNames != null && partNames.Length == 0) || 
                (partNames != null && partNames.Length > 0 && partNames[0] == null))
            {
                if (modifierKeys != Keys.Shift && modifierKeys != Keys.Control) _controller.ClearAllSelection();
            }
            else
            {
                //
                if (e.Clicks == 2) _modelTree.EditSelectedPart();
                else
                {
                    int count = 0;
                    BasePart[] parts;
                    int numOfSelectedTreeNodes = 0;
                    //
                    if (GetCurrentView() == ViewGeometryModelResults.Geometry)
                        parts = _controller.GetGeometryPartsForSelection(partNames);
                    else if (GetCurrentView() == ViewGeometryModelResults.Model)
                        parts = _controller.GetModelParts(partNames);
                    else if (GetCurrentView() == ViewGeometryModelResults.Results)
                        parts = _controller.GetResultParts(partNames);
                    else throw new NotSupportedException();
                    //
                    foreach (var part in parts)
                    {
                        numOfSelectedTreeNodes = _modelTree.SelectBasePart(e, modifierKeys, part, false);
                        count++;
                        //
                        if (count == 1 && modifierKeys == Keys.None) modifierKeys |= Keys.Shift;
                    }
                    _modelTree.UpdateHighlight();
                    //
                    if (numOfSelectedTreeNodes > 0 && e.Button == MouseButtons.Right)
                    {
                        _modelTree.ShowContextMenu(_vtk, e.X, _vtk.Height - e.Y);
                    }
                }
            }
        }
        public void SelectFirstComponentOfFirstFieldOutput()
        {
            InvokeIfRequired(_modelTree.SelectFirstComponentOfFirstFieldOutput);
        }
        //
        public void SetNumberOfModelUserKeywords(int numOfUserKeywords)
        {
            InvokeIfRequired(_modelTree.SetNumberOfUserKeywords, numOfUserKeywords);
        }
        
        #endregion  ################################################################################################################

        // Output
        public void WriteDataToOutput(string data)
        {
            if (data == null) return;
            // 20 chars is an empty line with date
            if (data.Length == 0 && (outputLines.Length > 0 && outputLines.Last().Length == 20)) return;
            //
            InvokeIfRequired(() =>
            {
                data = data.Replace("\r\n", "\n");
                data = data.Replace('\r', '\n');
                string[] lines = data.Split('\n');
                //
                foreach (var line in lines)
                {
                    WriteLineToOutputWithDate(line);
                    //
                    Console.WriteLine(line);
                }
                //
                timerOutput.Start();
            });
        }
        private void WriteLineToOutputWithDate(string data)
        {
            if (outputLines != null)
            {
                int numColDate = 20;
                int numCol = (int)((tbOutput.Width - 60) / tbOutput.TextCharWidth);
                int numRows = 100;      // number of displayed lines
                //
                List<string> lines = new List<string>(outputLines);
                List<string> wrappedLines = new List<string>();
                //
                while (numColDate + data.Length > numCol)
                {
                    wrappedLines.Add(data.Substring(0, numCol - numColDate) + "...");
                    data = data.Substring(numCol - numColDate);
                }
                wrappedLines.Add(data);
                //
                foreach (var wrappedLine in wrappedLines)
                {
                    lines.Add(DateTime.Now.ToString("MM/dd/yy HH:mm:ss").PadRight(numColDate) + wrappedLine);
                }
                //
                int firstLine = Math.Max(0, lines.Count - numRows);
                int numLines = Math.Min(lines.Count, numRows);
                //
                outputLines = new string[numLines];
                Array.Copy(lines.ToArray(), firstLine, outputLines, 0, numLines);
            }
        }

        #region Invoke  ############################################################################################################
        // Invoke
        public void InvokeIfRequired(Action action)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { action(); });
            }
            else
            {
                action();
            }
        }
        public object InvokeIfRequired(Func<object> function)
        {
            if (this.InvokeRequired)
            {
                return (object)this.Invoke((MethodInvoker)delegate () { function(); });
            }
            else
            {
                return function();
            }
        }
        public void InvokeIfRequired<T>(Action<T> action, T parameter)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { action(parameter); });
            }
            else
            {
                action(parameter);
            }
        }
        public void InvokeIfRequired<T1,T2>(Action<T1, T2> action, T1 parameter1, T2 parameter2)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { action(parameter1, parameter2); });
            }
            else
            {
                action(parameter1, parameter2);
            }
        }
        public void InvokeIfRequired<T1, T2, T3>(Action<T1, T2, T3> action, T1 parameter1, T2 parameter2, T3 parameter3)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { action(parameter1, parameter2, parameter3); });
            }
            else
            {
                action(parameter1, parameter2, parameter3);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 parameter1, T2 parameter2, T3 parameter3, 
                                                     T4 parameter4)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { action(parameter1, parameter2, parameter3, parameter4); });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 parameter1, T2 parameter2,
                                     T3 parameter3, T4 parameter4, T5 parameter5)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { action(parameter1, parameter2, parameter3, parameter4, parameter5); });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 parameter1, T2 parameter2, 
                                     T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate () { action(parameter1, parameter2, parameter3, parameter4, 
                                                         parameter5, parameter6); });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 parameter1, 
                                     T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate () { action(parameter1, parameter2, parameter3, parameter4, parameter5,
                                                         parameter6, parameter7); });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, T1 parameter1,
                                     T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7,
                                     T8 parameter8)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate () {
                    action(parameter1, parameter2, parameter3, parameter4, parameter5,
                    parameter6, parameter7, parameter8);
                });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action,
                                     T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6,
                                     T7 parameter7, T8 parameter8, T9 parameter9)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate () {
                    action(parameter1, parameter2, parameter3, parameter4, parameter5,
                    parameter6, parameter7, parameter8, parameter9);
                });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8, parameter9);
            }
        }



































        #endregion  ################################################################################################################

        #endregion

        private void tsmiTest_Click(object sender, EventArgs e)
        {
            try
            {
                //_vtk.SwitchLights();
                //_controller.TestCreateSurface();
                //AnimateModel58();
                //_vtk.Export(GetFileNameToSaveAs());
                //TestSpring();
                //TestSuperposition();
                //TestNormals();
                //TestWearResults();
                //TestMmg();
                //TestGmshReadMesh();
                //TestDefeature();
                //AnimateRotation();
                TestParameterization();
                //TestImportedStLoad();
            }
            catch
            {

            }
        }
        private void TestImportedStLoad()
        {
            int n = 32;
            int count = 0;
            double delta = 2.0 / (n - 1);
            double[][] coor = new double[n * n][];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    coor[count] = new double[] { -1 + i * delta, -1 + j * delta, 0 };
                    count++;
                }
            }
            //
            double v1 = -1;
            double v2 = -2;
            double v3 = -3;
            double v4 = -4;
            double x;
            double y;

            double minX = 13.15;
            double maxX = minX + 300;
            double minY = 20.87;
            double maxY = minY + 300;
            double z = 129.5;

            count = 0;
            double[] values = new double[n * n];
            string[] lines = new string[n * n];
            for (int i = 0; i < values.Length; i++)
            {
                x = coor[count][0];
                y = coor[count][1];
                values[count] = v1 * 0.25 * (1 - x) * (1 - y) +
                                v2 * 0.25 * (1 + x) * (1 - y) +
                                v3 * 0.25 * (1 + x) * (1 + y) +
                                v4 * 0.25 * (1 - x) * (1 + y);
                //
                coor[count][0] = minX + (maxX - minX) * (x + 1) / 2;
                coor[count][1] = minY + (maxY - minY) * (y + 1) / 2;
                coor[count][2] = z;
                //
                lines[count] = string.Format("{0} {1} {2} {3} {4} {5}{6}", coor[count][0], coor[count][1], coor[count][2],
                                                                           0, 0, values[count], Environment.NewLine);
                //
                count++;

            }

            File.WriteAllLines(@"c:\Temp\load.txt", lines);
        }
        private void TestMmg()
        {
            string partName1 = "Solid_part-1";
            string partName2 = "Solid_part-2";
            string fileName = @"C:\Temp\mmg\tmp.sol";
            //
            //_controller.ExportSignedDistance(fileName, partName2, partName1);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void TestWearResults()
        {
            string fileName = @"C:\Temp\Ignatijev\Model8\Analysis-1.frd";
            _controller.ReadFrdFileAsWear(fileName);
        }
        private void TestNormals()
        {
            string partName = "Shell_part-1";
            _controller.SuppressExplodedView(new string[] { partName });
            //
            MeshPart part = _controller.GetModelPart(partName);
            GeometryPart geometryPart = _controller.GetGeometryPart(partName);
            //
            Dictionary<int, FeNode[]> faceIdNodes = new Dictionary<int, FeNode[]>();
            //
            HashSet<int> nodeIds;
            List<FeNode> nodes;
            for (int i = 0; i < part.Visualization.FaceCount; i++)
            {
                nodes = new List<FeNode>();
                nodeIds = part.Visualization.GetNodeIdsForSurfaceId(i);
                foreach (var nodeId in nodeIds)
                {
                    nodes.Add(_controller.Model.Mesh.Nodes[nodeId]);
                }
                //
                faceIdNodes.Add(FeMesh.GmshTopologyId(i, part.PartId), nodes.ToArray());
            }
            //
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return;
            }
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            //
            File.WriteAllText(brepFileName, geometryPart.CADFileData);
            GmshData gmshData = new GmshData();
            gmshData.GeometryFileName = brepFileName;
            gmshData.FaceIdNodes = faceIdNodes;
            _controller.Model.Geometry.GetPartTopologyForGmsh(geometryPart.Name, ref gmshData);
            _controller.ResumeExplodedViews(false);
            //
            GmshAPI gmshAPI = new GmshAPI(gmshData, WriteDataToOutput);
            string error = gmshAPI.GetOccNormals();
            Dictionary<int, List<Vec3D>> normals = gmshAPI.GmshData.NodeIdNormals;
        }
        private void TestGmshReadMesh()
        {
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //

            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return;
            }
            string meshFileName = Path.Combine(workDirectory, Globals.GmshMeshFileName);
            //
            if (File.Exists(meshFileName)) File.Delete(meshFileName);
            //
            GmshMshFileWriter.Write(meshFileName, _controller.Model.Mesh);
            //
            GmshData gmshData = new GmshData();
            gmshData.MeshFileName = meshFileName;
            //
            GmshAPI gmshAPI = new GmshAPI(gmshData, null);
            string error = gmshAPI.GetElementQualities();
        }
        private void TestDefeature()
        {
            _controller.Defeature(null);
        }
        private void TestParameterization()
        {
            _controller.ViewParameterization();
            _vtk.Invalidate();
        }
        private void TestSuperposition()
        {
            _controller.CurrentResult.TestSuperposition();
        }
        private void TestEquation()
        {
            EquationContainer equationContainer1;
            EquationContainer equationContainer2;
            for (int i = 0; i < 10000; i++)
            {
                equationContainer1 = new EquationContainer(typeof(StringLengthConverter), 1.4);
                equationContainer2 = new EquationContainer(typeof(StringLengthConverter), 1);
                EquationContainer.SetAndCheck(ref equationContainer2, equationContainer1, null, false);
            }
        }
        private void TestSpring()
        {
            int[] directions;
            PointSpring pointSpring;
            PointSpringData pointSpringData;
            List<CaeModel.Constraint> constraints = new List<CaeModel.Constraint>();
            for (int i = 0; i < 10000; i++)
            {
                pointSpring = new PointSpring("spring", 101, 1, 2, 3, false, false);
                directions = pointSpring.GetSpringDirections();
            }
            directions = null;
            //
            for (int i = 0; i < 10000; i++)
            {
                pointSpringData = new PointSpringData("spring", 101, 1, 2, 3);
                directions = pointSpringData.GetSpringDirections();
            }
        }
        private void AnimateRotation()
        {
            tbOutput.Clear();
            Thread.Sleep(1000);
            int nFrames = 360;
            double delta = 360.0 / nFrames;
            double angle;
            double alpha;
            string[] partNames = new string[] { "Solid_part-1" };
            //
            //AnimateTransparency("Solid_part-1", 230, 20, 10);
            string fileName = @"c:\Temp\screen";
            _controller.SetTransparencyForGeometryParts(partNames, 255);
            _vtk.RenderToPNG(fileName + 0.ToString().PadLeft(3, '0') + ".png");
            //
            for (int i = 1; i <= nFrames; i++)
            {
                //_vtk.Rotate(-delta, 0, 0);
                _vtk.Rotate(0, 0, delta);
                angle = i * delta;

                if (i == nFrames / 4)
                    _controller.SetTransparencyForGeometryParts(partNames, (byte)100);
                else if (i == nFrames / 2)
                    _controller.SetTransparencyForGeometryParts(partNames, (byte)24);
                else if (i == 3 * nFrames / 4)
                    _controller.SetTransparencyForGeometryParts(partNames, (byte)100);



                //if (angle > 80 && angle < 180)
                //{
                //    alpha =  (angle - 80) / (180 - 80);    // 0...1
                //    alpha = (1 - alpha) * 250;
                //    _controller.SetTransparencyForGeometryParts(partNames, (byte)alpha);
                //}
                //if (angle > 270)
                //{
                //    alpha = (angle - 270) / (90);    // 0...1
                //    alpha = (alpha) * 250;
                //    _controller.SetTransparencyForGeometryParts(partNames, (byte)alpha);
                //}

                //Thread.Sleep(10);
                //Application.DoEvents();
                _vtk.RenderToPNG(fileName + i.ToString().PadLeft(3, '0') + ".png");
            }


            //
            //_vtk.AnimateZoomToFactor(3000, 100);
            //
            //if (true)
            //{
            //    int alphaStart = 230;
            //    int alphaEnd = 50;
            //    int alphaStep = 5;
            //    AnimateTransparency("Shell_part-7", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-6", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-11", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-1", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-10", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-2", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-14", alphaStart, alphaEnd, alphaStep);
            //}
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(6000, 1500);
            ////
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            ////
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            ////
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(3000, 1500);
        }
        private void AnimateModel58()
        {
            tbOutput.Clear();
            Thread.Sleep(3000);
            //
            //_vtk.AnimateZoomToFactor(3000, 100);
            //
            if (true)
            {
                int alphaStart = 230;
                int alphaEnd = 50;
                int alphaStep = 5;
                AnimateTransparency("Shell_part-7", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-6", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-11", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-1", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-10", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-2", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-14", alphaStart, alphaEnd, alphaStep);
            }
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(6000, 1500);
            ////
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            ////
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            ////
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(3000, 1500);
        }
        private void AnimateSierra()
        {
            tbOutput.Clear();
            Thread.Sleep(3000);
            //
            _vtk.AnimateZoomToFactor(800, 100);
            //
            if (true)
            {
                int alphaStart = 230;
                int alphaEnd = 50;
                int alphaStep = 5;
                AnimateTransparency("Shell_part-16", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-4", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-1", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-17", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-12", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-9", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-7", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-13", alphaStart, alphaEnd, alphaStep);
            }
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(2000, 1500);
            //
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            //
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            //
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(800, 1500);
        }
        private void AnimateTransparency(string partName, int alphaFrom, int alphaTo, int alphaStep)
        {
            string[] partNames = new string[] { partName };

            if (alphaFrom > alphaTo)
            {
                for (int i = alphaFrom; i >= alphaTo; i -= alphaStep)
                {
                    //_controller.SetTransparencyForResultParts(partNames, (byte)i);
                    _controller.SetTransparencyForGeometryParts(partNames, (byte)i);
                    Application.DoEvents();
                }
            }
            
        }
        private void timerTest_Tick(object sender, EventArgs e)
        {
            //TestAnimation();
            //TestSelection1();
            TestSelection2();
        }
        private void TestAnimation()
        {
            try
            {
                timerTest.Interval = 10;
                //timerTest.Stop();

                string[] names = new string[] { CaeResults.FOFieldNames.Stress, CaeResults.FOFieldNames.Disp };
                string[] components = new string[] { "SZZ", "D2" };

                CaeResults.FieldData currentData = _controller.CurrentFieldData;
                int i = 0;
                if (currentData.Component == components[i]) i++;

                int len = names.Length;

                SetFieldData(names[i % len], components[i % len], 1, 1);
            }
            catch
            { }
        }
        private void TestSelection1()
        {
            try
            {
                timerTest.Interval = 100;
                //
                string[] allPartNames = _controller.GetGeometryPartNames();
                HashSet<string> selectedPartNames = new HashSet<string>();
                //
                Random rand = new Random();
                for (int i = 0; i < 100; i++)
                {
                    selectedPartNames.Add(allPartNames[(int)(rand.NextDouble() * allPartNames.Length - 1)]);
                }
                Clear3DSelection();
                _controller.HighlightGeometryParts(selectedPartNames.ToArray());
                //
                Application.DoEvents();
            }
            catch
            { }
        }
        private void TestSelection2()
        {
            try
            {
                timerTest.Interval = 50;
                //
                Random rand = new Random();
                int x1 = _vtk.Width / 4 + rand.Next(_vtk.Width / 2);
                int y1 = _vtk.Height / 4 + rand.Next(_vtk.Height / 2);

                //x1 = 523;
                //y1 = 421;

                Clear3DSelection();
                _vtk.Pick(x1, y1, false, 0, 0);
                //
                Application.DoEvents();
            }
            catch
            { }
        }
        internal void tsmiAdvisor_Click(object sender, EventArgs e)
        {
            if (!_controller.ModelInitialized) return;
            // Change the wizard check state
            tsmiAdvisor.Checked = !tsmiAdvisor.Checked;
            // Add wizard panel
            if (tsmiAdvisor.Checked == true)
            {
                Control parent = panelControl.Parent;
                if (parent == splitContainer2.Panel1)
                {
                    // First remove the vtk control and panel border
                    parent.Controls.Remove(_vtk);
                    parent.Controls.Remove(panelControl);
                    // Split container
                    SplitContainer splitContainer = new SplitContainer();
                    splitContainer.FixedPanel = FixedPanel.Panel2;
                    splitContainer.Dock = DockStyle.Fill;
                    parent.Controls.Add(splitContainer);
                    // Set the Panel 2 size - min 100 max 300
                    splitContainer.SplitterDistance = Math.Max(100, Math.Max(parent.Width - 300, (int)(parent.Width * 0.8)));
                    // Panel 1 - LEFT
                    splitContainer.Panel1.Controls.Add(_vtk);
                    splitContainer.Panel1.Controls.Add(panelControl);
                    panelControl.SendToBack();
                    // Update vtk control size
                    UpdateVtkControlSize();
                    // Panel 2 - RIGHT
                    _advisorControl = AdvisorCreator.CreateControl(this);
                    //
                    splitContainer.Panel2.Controls.Add(_advisorControl);
                    _advisorControl.Dock = DockStyle.Fill;
                    _advisorControl.UpdateDesign();
                    _advisorControl.AutoScroll = true;
                }
            }
            // Remove wizard panel
            else
            {
                Control parent = panelControl.Parent;
                if (parent is SplitterPanel && parent != splitContainer2.Panel1)
                {
                    // First remove the vtk control and panel border
                    parent.Controls.Remove(_vtk);
                    parent.Controls.Remove(panelControl);
                    // Remove added split container
                    splitContainer2.Panel1.Controls.Clear();
                    _advisorControl = null;
                    // Add controls back
                    splitContainer2.Panel1.Controls.Add(panelControl);
                    splitContainer2.Panel1.Controls.Add(_vtk);
                    panelControl.SendToBack();
                    // Update vtk control size
                    UpdateVtkControlSize();
                }
            }
        }

       
    }
}

