﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using DynamicTypeDescriptor;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using System.Drawing.Design;

namespace PrePoMax
{
    [Serializable]
    public class ViewImportedPressureLoad : ViewLoad
    {
        // Variables                                                                                                                
        private ImportedPressure _importedPressure;


        // Properties                                                                                                               
        public override string Name { get { return _importedPressure.Name; } set { _importedPressure.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the load.")]
        [Id(3, 2)]
        public string SurfaceName { get { return _importedPressure.SurfaceName; } set {_importedPressure.SurfaceName = value;} }
        ////
        [CategoryAttribute("Pressure magnitude")]
        [OrderedDisplayName(0, 10, "Import from file")]
        [DescriptionAttribute("Select the file from which the pressure results will be imported.")]
        [EditorAttribute(typeof(FilteredFileNameEditor), typeof(UITypeEditor))]
        [Id(1, 3)]
        public string FileName
        {
            get { return _importedPressure.FileName; }
            set
            {
                _importedPressure.FileName = value;
                //
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Pressure magnitude")]
        [OrderedDisplayName(1, 10, "Time")]
        [DescriptionAttribute("Select the time at which the pressure will be imported.")]
        [Id(2, 3)]
        public string PressureTime
        {
            get { return _importedPressure.PressureTime; }
            set
            {
                _importedPressure.PressureTime = value;
                //
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Pressure magnitude")]
        [OrderedDisplayName(2, 10, "Variable name")]
        [DescriptionAttribute("Select the name of the pressure variable that will be imported.")]
        [Id(3, 3)]
        public string PressureVariableName
        {
            get { return _importedPressure.PressureVariableName; }
            set
            {
                _importedPressure.PressureVariableName = value;
                //
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Pressure magnitude")]
        [OrderedDisplayName(3, 10, "Interpolator")]
        [DescriptionAttribute("Select the interpolation type. The closest node method takes the value from the closest node " + 
            "on the source mesh while the closest point method interpolates the closest three nodal values.")]
        [Id(4, 3)]
        public CaeResults.InterpolatorEnum Interpolator
        {
            get { return _importedPressure.InterpolatorType; }
            set { _importedPressure.InterpolatorType = value; }
        }
        //
        [CategoryAttribute("Pressure magnitude")]
        [OrderedDisplayName(4, 10, "Magnitude factor")]
        [DescriptionAttribute("Value of the pressure magnitude scale factor.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(5, 3)]
        public EquationString MagnitudeFactor
        {
            get { return _importedPressure.MagnitudeFactor.Equation; }
            set { _importedPressure.MagnitudeFactor.Equation = value; }
        }
        ////
        [CategoryAttribute("Pressure phase")]
        [OrderedDisplayName(0, 10, "Phase")]
        [DescriptionAttribute("Value of the pressure phase.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        [Id(1, 4)]
        public EquationString Phase
        {
            get { return _importedPressure.PhaseDeg.Equation; }
            set { _importedPressure.PhaseDeg.Equation = value; }
        }
        ////
        [CategoryAttribute("Imported geometry")]
        [OrderedDisplayName(0, 10, "Scale factor")]
        [DescriptionAttribute("Value of the imported geometry scale factor.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(1, 5)]
        public EquationString GeometryScaleFactor
        {
            get { return _importedPressure.GeometryScaleFactor.Equation; }
            set { _importedPressure.GeometryScaleFactor.Equation = value; }
        }
        //
        public override string AmplitudeName
        {
            get { return _importedPressure.AmplitudeName; }
            set { _importedPressure.AmplitudeName = value; }
        }
        [Browsable(false)]
        public override string CoordinateSystemName
        {
            get { return _importedPressure.CoordinateSystemName; }
            set { _importedPressure.CoordinateSystemName = value; }
        }
        public override System.Drawing.Color Color
        {
            get { return _importedPressure.Color; }
            set { _importedPressure.Color = value; }
        }


        // Constructors                                                                                                             
        public ViewImportedPressureLoad(ImportedPressure importedPressure)
        {
            _importedPressure = importedPressure;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            SetBase(_importedPressure, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // Phase
            DynamicCustomTypeDescriptor.GetProperty(nameof(Phase)).SetIsBrowsable(_importedPressure.Complex);
        }


        // Methods                                                                                                                  
        public override Load GetBase()
        {
            return _importedPressure;
        }
        public void PopulateDropDownLists(string[] surfaceNames, string[] amplitudeNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            PopulateAmplitudeNames(amplitudeNames);
            //
            UpdateVisibility();
        }
        private void UpdateVisibility()
        {
            bool browsable = false;
            //
            Dictionary<string, string[]> timeResultVariableNames =
                CaeResults.OpenFoamFileReader.GetTimeResultScalarVariableNames(_importedPressure.FileName);
            //
            if (timeResultVariableNames != null && timeResultVariableNames.Count > 0)
            {
                //
                DynamicCustomTypeDescriptor.PopulateProperty(nameof(PressureTime), timeResultVariableNames.Keys.ToArray());
                if (PressureTime == null || !timeResultVariableNames.ContainsKey(PressureTime))
                    PressureTime = timeResultVariableNames.Keys.First();
                //
                DynamicCustomTypeDescriptor.PopulateProperty(nameof(PressureVariableName), timeResultVariableNames[PressureTime]);
                if (PressureVariableName == null || !timeResultVariableNames[PressureTime].Contains(PressureVariableName))
                    PressureVariableName = timeResultVariableNames[PressureTime].First();
                //
                browsable = true;
            }
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(PressureTime)).SetIsBrowsable(browsable);
            DynamicCustomTypeDescriptor.GetProperty(nameof(PressureVariableName)).SetIsBrowsable(browsable);
        }
        public void UpdateFileBrowserDialog()
        {
            FilteredFileNameEditor.Filter = "OpenFOAM files|*.foam";
        }
    }
}
