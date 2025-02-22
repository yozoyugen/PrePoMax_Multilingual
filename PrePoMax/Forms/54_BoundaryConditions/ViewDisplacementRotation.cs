﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Drawing;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewDisplacementRotation : ViewBoundaryCondition
    {
        // Variables                                                                                                                
        private DisplacementRotation _displacementRotation;


        // Properties                                                                                                               
        public override string Name { get { return _displacementRotation.Name; } set { _displacementRotation.Name = value; } }
        public override string NodeSetName { get { return _displacementRotation.RegionName; } set { _displacementRotation.RegionName = value; } }
        public override string ReferencePointName { get { return _displacementRotation.RegionName; } set { _displacementRotation.RegionName = value; } }
        public override string SurfaceName { get { return _displacementRotation.RegionName; } set { _displacementRotation.RegionName = value; } }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(0, 10, "U1")]
        [DescriptionAttribute("Displacement in the direction of the first axis.")]
        [TypeConverter(typeof(EquationLengthDOFConverter))]
        [Id(1, 3)]
        public EquationString U1
        {
            get { return _displacementRotation.U1.Equation; }
            set
            {
                _displacementRotation.U1.Equation = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(1, 10, "U2")]        
        [DescriptionAttribute("Displacement in the direction of the second axis.")]
        [TypeConverter(typeof(EquationLengthDOFConverter))]
        [Id(2, 3)]
        public EquationString U2
        {
            get { return _displacementRotation.U2.Equation; }
            set
            {
                _displacementRotation.U2.Equation = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(2, 10, "U3")]
        [DescriptionAttribute("Displacement in the direction of the third axis.")]
        [TypeConverter(typeof(EquationLengthDOFConverter))]
        [Id(3, 3)]
        public EquationString U3
        {
            get { return _displacementRotation.U3.Equation; }
            set
            {
                _displacementRotation.U3.Equation = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(3, 10, "UR1")]
        [DescriptionAttribute("Rotation around the first axis.")]
        [TypeConverter(typeof(EquationAngleDOFConverter))]
        [Id(4, 3)]
        public EquationString UR1
        {
            get { return _displacementRotation.UR1.Equation; }
            set
            {
                _displacementRotation.UR1.Equation = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(4, 10, "UR2")]
        [DescriptionAttribute("Rotation around the second axis.")]
        [TypeConverter(typeof(EquationAngleDOFConverter))]
        [Id(5, 3)]
        public EquationString UR2
        {
            get { return _displacementRotation.UR2.Equation; }
            set
            {
                _displacementRotation.UR2.Equation = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(5, 10, "UR3")]
        [DescriptionAttribute("Rotation around the third axis.")]
        [TypeConverter(typeof(EquationAngleDOFConverter))]
        [Id(6, 3)]
        public EquationString UR3
        {
            get { return _displacementRotation.UR3.Equation; }
            set
            {
                _displacementRotation.UR3.Equation = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Phase")]
        [OrderedDisplayName(0, 10, "Phase")]
        [DescriptionAttribute("Value of the boundary condition phase.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        [Id(1, 4)]
        public EquationString Phase
        {
            get { return _displacementRotation.PhaseDeg.Equation; }
            set { _displacementRotation.PhaseDeg.Equation = value; }
        }
        //
        public override string AmplitudeName
        {
            get { return _displacementRotation.AmplitudeName; }
            set { _displacementRotation.AmplitudeName = value; }
        }
        //
        public override string CoordinateSystemName
        {
            get { return _displacementRotation.CoordinateSystemName; }
            set { _displacementRotation.CoordinateSystemName = value; UpdateVisibility(); }
        }
        public override Color Color { get { return _displacementRotation.Color; } set { _displacementRotation.Color = value; } }


        // Constructors                                                                                                             
        public ViewDisplacementRotation(DisplacementRotation displacementRotation)
        {
            // The order is important
            _displacementRotation = displacementRotation;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ReferencePointName, nameof(ReferencePointName));
            //
            SetBase(_displacementRotation, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // 2D
            DynamicCustomTypeDescriptor.GetProperty(nameof(U3)).SetIsBrowsable(!_displacementRotation.TwoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(UR1)).SetIsBrowsable(!_displacementRotation.TwoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(UR2)).SetIsBrowsable(!_displacementRotation.TwoD);
            // Phase
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public override BoundaryCondition GetBase()
        {
            return _displacementRotation;
        }
        public void PopulateDropDownLists(string[] nodeSetNames, string[] surfaceNames, string[] referencePointNames,
                                          string[] amplitudeNames, string[] coordinateSystemNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.ReferencePointName, referencePointNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            PopulateAmplitudeNames(amplitudeNames);
            //
            PopulateCoordinateSystemNames(coordinateSystemNames);
            //
            UpdateVisibility();
        }
        public override void UpdateVisibility()
        {
            // Coordinate systems
            int numItems = DynamicCustomTypeDescriptor.GetProperty(nameof(CoordinateSystemName)).StatandardValues.Count;
            bool visible = RegionType != RegionTypeEnum.ReferencePointName.ToFriendlyString() && numItems > 1;
            DynamicCustomTypeDescriptor.GetProperty(nameof(CoordinateSystemName)).SetIsBrowsable(visible);
            // Rotations
            visible = _displacementRotation.CoordinateSystemName == BoundaryCondition.DefaultCoordinateSystemName;
            DynamicCustomTypeDescriptor.GetProperty(nameof(UR1)).SetIsBrowsable(visible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(UR2)).SetIsBrowsable(visible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(UR3)).SetIsBrowsable(visible);
            // Phase
            visible = _displacementRotation.Complex && !_displacementRotation.IsFreeFixedOrZero();
            DynamicCustomTypeDescriptor.GetProperty(nameof(Phase)).SetIsBrowsable(visible);
        }
    }

}
