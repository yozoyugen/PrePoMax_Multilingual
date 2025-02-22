﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    public enum DefaultMeshSizeTypeEnum
    {
        Absolute,
        Relative
    }
    public class ViewMeshingParameters : ViewMeshSetupItem
    {
        // Variables                                                                                                                
        protected bool _settingsView;
        protected MeshingParameters _parameters;


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [Description("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _parameters.Name; } set { _parameters.Name = value; } }
        //
        [Category("Data")]
        [OrderedDisplayName(1, 10, "Mesh settings")]
        [Description("Turn on advanced mesh settings.")]
        [Id(2, 1)]
        public bool AdvancedView
        {
            get { return _parameters.AdvancedView; }
            set
            {
                _parameters.AdvancedView = value;
                if (_parameters.AdvancedView == false) _parameters.RelativeSize = _parameters.DefaultSizeIsRelative;
                //
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the mesh setup item.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(0, 10, "Mesh size definition")]
        [Description("Select the mesh size definition.")]
        [Id(1, 3)]
        public bool Relative
        {
            get { return _parameters.RelativeSize; }
            set
            {
                _parameters.RelativeSize = value;
                UpdateVisibility();
            }
        }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(1, 10, "Max element factor")]
        [Description("The relative factor for the maximum element size in regard to the bounding box diagonal.")]
        [Id(2, 3)]
        public virtual double FactorMax { get { return _parameters.FactorMax; } set { _parameters.FactorMax = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(2, 10, "Min element factor")]
        [Description("The relative factor for the minimum element size in regard to the bounding box diagonal.")]
        [Id(3, 3)]
        public virtual double FactorMin { get { return _parameters.FactorMin; } set { _parameters.FactorMin = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(3, 10, "Max element size")]
        [Description("The value for the maximum element size.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 3)]
        public virtual double MaxH { get { return _parameters.MaxH; } set { _parameters.MaxH = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(4, 10, "Min element size")]
        [Description("The value for the minimum element size.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(5, 3)]
        public virtual double MinH { get { return _parameters.MinH; } set { _parameters.MinH = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(5, 10, "Grading")]
        [Description("The value of the mesh grading (0 => uniform mesh; 1 => aggressive local grading).")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(6, 3)]
        public double Grading { get { return _parameters.Grading; } set { _parameters.Grading = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(6, 10, "Elements per edge")]
        [Description("Number of elements to generate per edge of the geometry.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(7, 3)]
        public double ElementsPerEdge { get { return _parameters.ElementsPerEdge; } set { _parameters.ElementsPerEdge = value; } }
        //
        [Category("Mesh size")]
        [OrderedDisplayName(7, 10, "Elements per curvature")]
        [Description("Number of elements to generate per curvature radius.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(8, 3)]
        public double ElementsPerCurve { get { return _parameters.ElementsPerCurve; } set { _parameters.ElementsPerCurve = value; } }
        // Hausdorff factor
        [Category("Mesh size")]
        [OrderedDisplayName(8, 10, "Hausdorff factor")]
        [Description("The relative factor for the Hausdorff distance in regard to the bounding box diagonal.")]
        [Id(9, 3)]
        public double FactorHausdorff { get { return _parameters.FactorHausdorff; } set { _parameters.FactorHausdorff = value; } }
        // Maximal Hausdorff distance for the boundaries approximation.
        [Category("Mesh size")]
        [OrderedDisplayName(9, 10, "Hausdorff")]
        [Description("Maximal Hausdorff distance for the boundaries approximation. " +
                     "A value of 0.01 is a suitable value for an object of size 1 in each direction.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(10, 3)]
        public double Hausdorff { get { return _parameters.Hausdorff; } set { _parameters.Hausdorff = value; } }
        //
        //
        [Category("Mesh optimization")]
        [OrderedDisplayName(0, 10, "Optimize steps 2D")]
        [Description("Number of optimize steps to use for 2-D mesh optimization.")]
        [Id(1, 4)]
        public int OptimizeSteps2D { get { return _parameters.OptimizeSteps2D; } set { _parameters.OptimizeSteps2D = value; } }
        //
        [Category("Mesh optimization")]
        [OrderedDisplayName(1, 10, "Optimize steps 3D")]
        [Description("Number of optimize steps to use for 3-D mesh optimization.")]
        [Id(2, 4)]
        public int OptimizeSteps3D { get { return _parameters.OptimizeSteps3D; } set { _parameters.OptimizeSteps3D = value; } }
        //
        //
        [Category("Mesh type")]
        [OrderedDisplayName(0, 10, "Second order")]
        [Description("Create second order elements.")]
        [Id(1, 5)]
        public bool SecondOrder
        {
            get {return _parameters.SecondOrder;}
            set
            {
                _parameters.SecondOrder = value;
                UpdateVisibility();
            }
        }
        //
        [Category("Mesh type")]
        [OrderedDisplayName(1, 10, "Midside nodes on geometry")]
        [Description("Create midside nodes on geometry.")]
        [Id(2, 5)]
        public bool MidsideNodesOnGeometry { get { return _parameters.MidsideNodesOnGeometry; } set { _parameters.MidsideNodesOnGeometry = value; } }
        //
        [Category("Mesh type")]
        [OrderedDisplayName(2, 10, "Quad-dominated mesh")]
        [Description("Use quad-dominated mesh for shell parts.")]
        [Id(3, 5)]
        public bool QuadDominated { get { return _parameters.QuadDominated; } set { _parameters.QuadDominated = value; } }
        //
        //
        [Category("Mesh operations")]
        [OrderedDisplayName(0, 10, "Split compound mesh")]
        [Description("Split compound part mesh to unconnected part meshes.")]
        [Id(1, 6)]
        public bool SplitCompoundMesh
        {
            get { return _parameters.SplitCompoundMesh; }
            set { _parameters.SplitCompoundMesh = value; }
        }
        //
        [Category("Mesh operations")]
        [OrderedDisplayName(1, 10, "Merge compound parts")]
        [Description("Merge compound part mesh to a single mesh part.")]
        [Id(2, 6)]
        public bool MergeCompoundParts
        {
            get { return _parameters.MergeCompoundParts; }
            set { _parameters.MergeCompoundParts = value; }
        }
        //
        [Category("Mesh operations")]
        [OrderedDisplayName(2, 10, "Keep model edges")]
        [Description("Select Yes to keep and No to ignore the model edges.")]
        [Id(3, 6)]
        public bool KeepModelEdges { get { return _parameters.KeepModelEdges; } set { _parameters.KeepModelEdges = value; } }


        // Constructors                                                                                                             
        public ViewMeshingParameters(MeshingParameters parameters)
            : this(parameters, parameters.RelativeSize)
        {
        }
        public ViewMeshingParameters(MeshingParameters parameters, bool defaultSizeIsRelative)
            : this(parameters, defaultSizeIsRelative, parameters.AdvancedView)
        {
        }
        public ViewMeshingParameters(MeshingParameters parameters, bool defaultSizeIsRelative, bool advancedView)
        {
            _parameters = parameters;
            _parameters.AdvancedView = advancedView;
            _parameters.DefaultSizeIsRelative = defaultSizeIsRelative;
            //
            _dctd = ProviderInstaller.Install(this);
            InitializeRegion();
            // Category sorting
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;    // seems not to work
            // Now lets display Yes/No instead of True/False
            _dctd.RenameBooleanProperty(nameof(AdvancedView), "Advanced", "Basic");
            _dctd.RenameBooleanProperty(nameof(Relative), "Relative", "Absolute");
            _dctd.RenameBooleanPropertyToYesNo(nameof(SecondOrder));
            _dctd.RenameBooleanPropertyToYesNo(nameof(MidsideNodesOnGeometry));
            _dctd.RenameBooleanPropertyToYesNo(nameof(QuadDominated));
            _dctd.RenameBooleanPropertyToYesNo(nameof(SplitCompoundMesh));
            _dctd.RenameBooleanPropertyToYesNo(nameof(MergeCompoundParts));
            _dctd.RenameBooleanPropertyToYesNo(nameof(KeepModelEdges));
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public override MeshSetupItem GetBase()
        {
            return _parameters;
        }
        
        protected void UpdateVisibility()
        {
            bool advanced = _parameters.AdvancedView || _settingsView;
            _dctd.GetProperty(nameof(Relative)).SetIsBrowsable(advanced);
            //
            bool visible = (_parameters.RelativeSize && advanced) || _settingsView;
            _dctd.GetProperty(nameof(FactorMax)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(FactorMin)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(MaxH)).SetIsBrowsable(!visible);
            _dctd.GetProperty(nameof(MinH)).SetIsBrowsable(!visible);
            //
            visible = (_parameters.UseMmg && _parameters.RelativeSize) || _settingsView;
            _dctd.GetProperty(nameof(FactorHausdorff)).SetIsBrowsable(visible); // mmg only
            //
            _dctd.GetProperty(nameof(Grading)).SetIsBrowsable(!_parameters.UseMmg && advanced);
            _dctd.GetProperty(nameof(ElementsPerEdge)).SetIsBrowsable(!_parameters.UseMmg);
            _dctd.GetProperty(nameof(ElementsPerCurve)).SetIsBrowsable(!_parameters.UseMmg);
            _dctd.GetProperty(nameof(Hausdorff)).SetIsBrowsable(_parameters.UseMmg && !_parameters.RelativeSize);   // mmg only
            _dctd.GetProperty(nameof(OptimizeSteps2D)).SetIsBrowsable(!_parameters.UseMmg && advanced);
            _dctd.GetProperty(nameof(OptimizeSteps3D)).SetIsBrowsable(!_parameters.UseMmg && advanced);
            _dctd.GetProperty(nameof(SecondOrder)).SetIsBrowsable(true);
            _dctd.GetProperty(nameof(MidsideNodesOnGeometry)).SetIsBrowsable(!_parameters.UseMmg && advanced);
            _dctd.GetProperty(nameof(QuadDominated)).SetIsBrowsable(!_parameters.UseMmg && advanced);
            _dctd.GetProperty(nameof(SplitCompoundMesh)).SetIsBrowsable(!_parameters.UseMmg && advanced);
            _dctd.GetProperty(nameof(MergeCompoundParts)).SetIsBrowsable(!_parameters.UseMmg && advanced);
            _dctd.GetProperty(nameof(KeepModelEdges)).SetIsBrowsable(_parameters.UseMmg || _settingsView);  // mmg only
            // To show/hide the MediumNodesOnGeometry property
            if (!_parameters.UseMmg)
            {
                _dctd.GetProperty(nameof(MidsideNodesOnGeometry)).SetIsBrowsable(_parameters.SecondOrder && advanced);
                _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            }
        }
    }
}
