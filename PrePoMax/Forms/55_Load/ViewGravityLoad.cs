﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewGravityLoad : ViewLoad
    {
        // Variables                                                                                                                
        private CaeModel.GravityLoad _gLoad;


        // Properties                                                                                                               
        public override string Name { get { return _gLoad.Name; } set { _gLoad.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Part")]
        [DescriptionAttribute("Select the part for the creation of the load.")]
        [Id(3, 2)]
        public string PartName { get { return _gLoad.RegionName; } set { _gLoad.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Element set")]
        [DescriptionAttribute("Select the element set for the creation of the load.")]
        [Id(4, 2)]
        public string ElementSetName { get { return _gLoad.RegionName; } set { _gLoad.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(4, 10, "Mass section")]
        [DescriptionAttribute("Select the mass section for the creation of the load.")]
        [Id(5, 2)]
        public string MassSectionName { get { return _gLoad.RegionName; } set { _gLoad.RegionName = value; } }
        //
        [CategoryAttribute("Gravity components")]
        [OrderedDisplayName(0, 10, "F1")]
        [DescriptionAttribute("Value of the gravity component in the direction of the first axis.")]
        [TypeConverter(typeof(EquationAccelerationConverter))]
        [Id(1, 3)]
        public EquationString F1 { get { return _gLoad.F1.Equation; } set { _gLoad.F1.Equation = value; } }
        //
        [CategoryAttribute("Gravity components")]
        [OrderedDisplayName(1, 10, "F2")]
        [DescriptionAttribute("Value of the gravity component in the direction of the second axis.")]
        [TypeConverter(typeof(EquationAccelerationConverter))]
        [Id(2, 3)]
        public EquationString F2 { get { return _gLoad.F2.Equation; } set { _gLoad.F2.Equation = value; } }
        //
        [CategoryAttribute("Gravity components")]
        [OrderedDisplayName(2, 10, "F3")]
        [DescriptionAttribute("Value of the gravity component in the direction of the third axis.")]
        [TypeConverter(typeof(EquationAccelerationConverter))]
        [Id(3, 3)]
        public EquationString F3 { get { return _gLoad.F3.Equation; } set { _gLoad.F3.Equation = value; } }
        //
        [CategoryAttribute("Gravity magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("Value of the gravity load magnitude.")]
        [TypeConverter(typeof(EquationAccelerationConverter))]
        [Id(1, 4)]
        public EquationString Magnitude { get { return _gLoad.Magnitude.Equation; } set { _gLoad.Magnitude.Equation = value; } }
        //
        [CategoryAttribute("Gravity phase")]
        [OrderedDisplayName(0, 10, "Phase")]
        [DescriptionAttribute("Value of the gravity load phase.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        [Id(1, 5)]
        public EquationString Phase { get { return _gLoad.PhaseDeg.Equation; } set { _gLoad.PhaseDeg.Equation = value; } }
        ////
        public override string AmplitudeName { get { return _gLoad.AmplitudeName; } set { _gLoad.AmplitudeName = value; } }
        [Browsable(false)]
        public override string CoordinateSystemName
        {
            get { return _gLoad.CoordinateSystemName; }
            set { _gLoad.CoordinateSystemName = value; }
        }
        public override System.Drawing.Color Color { get { return _gLoad.Color; } set { _gLoad.Color = value; } }


        // Constructors                                                                                                             
        public ViewGravityLoad(CaeModel.GravityLoad gLoad)
        {
            // The order is important
            _gLoad = gLoad;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.PartName, nameof(PartName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ElementSetName, nameof(ElementSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.MassSection, nameof(MassSectionName));
            //
            SetBase(_gLoad, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // 2D
            DynamicCustomTypeDescriptor.GetProperty(nameof(F3)).SetIsBrowsable(!gLoad.TwoD);
            // Phase
            DynamicCustomTypeDescriptor.GetProperty(nameof(Phase)).SetIsBrowsable(gLoad.Complex);
        }


        // Methods                                                                                                                  
        public override CaeModel.Load GetBase()
        {
            return _gLoad;
        }
        public void PopulateDropDownLists(string[] partNames, string[] elementSetNames, string[] massSectionNames,
                                          string[] amplitudeNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.PartName, partNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.ElementSetName, elementSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.MassSection, massSectionNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            PopulateAmplitudeNames(amplitudeNames);
        }
    }

}
