﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using CaeResults;
using System.IO;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    public class ImportedPressure : VariablePressure, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private string _fileName;                       //ISerializable
        private string _pressureTime;                   //ISerializable
        private string _pressureVariableName;           //ISerializable
        private InterpolatorEnum _interpolatorType;     //ISerializable
        private EquationContainer _magnitudeFactor;     //ISerializable
        private EquationContainer _geomScaleFactor;     //ISerializable
        //
        private FileInfo _oldFileInfo;                  //ISerializable
        //
        [NonSerialized]
        private ResultsInterpolator _interpolator;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public string PressureTime { get { return _pressureTime; } set { _pressureTime = value; } }
        public string PressureVariableName { get { return _pressureVariableName; } set { _pressureVariableName = value; } }
        public InterpolatorEnum InterpolatorType { get { return _interpolatorType; } set { _interpolatorType = value; } }
        public EquationContainer MagnitudeFactor
        {
            get { return _magnitudeFactor; }
            set { SetMagnitudeFactor(value); }
        }
        public EquationContainer GeometryScaleFactor
        {
            get { return _geomScaleFactor; }
            set { SetGeomScaleFactor(value); }
        }


        // Constructors                                                                                                             
        public ImportedPressure(string name, string surfaceName, RegionTypeEnum regionType, bool twoD,
                                bool complex, double phaseDeg)
            : base(name, surfaceName, regionType, twoD, complex, phaseDeg)
        {
            _fileName = null;
            _pressureTime = null;
            _pressureVariableName = null;
            _interpolatorType = InterpolatorEnum.ClosestNode;
            MagnitudeFactor = new EquationContainer(typeof(StringDoubleConverter), 1);
            GeometryScaleFactor = new EquationContainer(typeof(StringDoubleConverter), 1);
            //
            _oldFileInfo = null;
        }
        public ImportedPressure(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Compatibility for version v1.4.0
            if (_regionType == RegionTypeEnum.PartName) _regionType = RegionTypeEnum.Selection;
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_fileName":
                        _fileName = (string)entry.Value; break;
                    case "_pressureTime":
                        _pressureTime = (string)entry.Value; break;
                    case "_pressureVariableName":
                        _pressureVariableName = (string)entry.Value; break;
                    case "_interpolatorType":
                        _interpolatorType = (InterpolatorEnum)entry.Value; break;
                    case "_magnitudeFactor":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueMag)
                            MagnitudeFactor = new EquationContainer(typeof(StringDoubleConverter), valueMag);
                        else
                            SetMagnitudeFactor((EquationContainer)entry.Value, false);
                        break;
                    case "_geomScaleFactor":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueSF)
                            GeometryScaleFactor = new EquationContainer(typeof(StringDoubleConverter), valueSF);
                        else
                            SetGeomScaleFactor((EquationContainer)entry.Value, false);
                        break;
                    case "_oldFileInfo":
                        _oldFileInfo = (FileInfo)entry.Value; break;
                    default:
                        break;
                }
            }
            // Compatibility for version v1.4.0
            if (_magnitudeFactor == null) MagnitudeFactor = new EquationContainer(typeof(StringDoubleConverter), 1);
            if (_geomScaleFactor == null) GeometryScaleFactor = new EquationContainer(typeof(StringDoubleConverter), 1);
        }


        // Methods                                                                                                                  
        private void SetMagnitudeFactor(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _magnitudeFactor, value, null, checkEquation);
        }
        private void SetGeomScaleFactor(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _geomScaleFactor, value, null, checkEquation);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _magnitudeFactor.CheckEquation();
            _geomScaleFactor.CheckEquation();
        }
        //
        public bool IsProperlyDefined(out string error)
        {
            error = "";
            if (!File.Exists(_fileName))
            {
                error = "The selected file does not exist.";
                return false;
            }
            Dictionary<string, string[]> timeResultVariableNames = OpenFoamFileReader.GetTimeResultScalarVariableNames(_fileName);
            if (timeResultVariableNames.Count == 0)
            {
                error = "The selected OpenFOAM folder does not contain results.";
                return false;
            }
            string[] variables;
            if (timeResultVariableNames.TryGetValue(_pressureTime, out variables))
            {
                if (!variables.Contains(_pressureVariableName))
                {
                    error = "The selected OpenFOAM folder does not contain results for variable: " + _pressureVariableName + ".";
                    return false;
                }
            }
            else
            {
                error = "The selected OpenFOAM folder does not contain results for time: " + _pressureTime + ".";
                return false;
            }
            //
            return true;
        }
        public bool IsInitialized()
        {
            return _interpolator != null;
        }
        public void ImportPressure(UnitSystem unitSystem)
        {
            bool updateData = false;
            FileInfo fileInfo = new FileInfo(_fileName);
            //
            if (fileInfo.Exists)
            {
                if (_interpolator == null) updateData |= true;  // each time the load is changed it is Cloned -> _interpolator = null
                //
                else if (_oldFileInfo == null) updateData = true;
                else if (fileInfo.Name != _oldFileInfo.Name) updateData = true;
                // Files have the same name - check if newer
                else if (fileInfo.LastWriteTimeUtc < _oldFileInfo.LastWriteTimeUtc) updateData = true;
            }
            else
            {
                string missingFile = "The file from which the pressure should be imported does not exist.";
                throw new CaeException(missingFile);
            }
            //
            if (updateData)
            {
                _oldFileInfo = fileInfo;
                // Get results
                FeResults results =
                    OpenFoamFileReader.Read(_fileName, double.Parse(_pressureTime), _pressureVariableName, unitSystem);
                if (results == null) throw new CaeException("No pressure was imported.");
                // Scale geometry
                if (_geomScaleFactor.Value != 1) results.ScaleAllParts(_geomScaleFactor.Value);
                // Get pressure field data
                FieldData[] fieldData = results.GetAllFieldData(); // use GetResults for the first time to check existance
                Dictionary<string, string[]> filedNameComponentNames = results.GetAllFiledNameComponentNames();
                if (fieldData == null || fieldData.Length != 1) throw new CaeException("Pressure field could not be found.");
                //
                FieldData pressureData = fieldData[0];
                //
                string[] componentNames;
                filedNameComponentNames.TryGetValue(fieldData[0].Name, out componentNames);
                if (componentNames.Length != 1) throw new CaeException("Component of the pressure field could not be found.");
                //
                pressureData.Component = componentNames[0];
                // Initialize interpolator
                _interpolator = new ResultsInterpolator(results.GetAllNodesCellsAndValues(pressureData));
            }
        }
        public FeResults GetPreview(FeMesh targetMesh, string resultName, UnitSystem unitSystem)
        {
            ImportPressure(unitSystem);
            //
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
            FeSurface surface = targetMesh.Surfaces[_surfaceName];
            FeNodeSet nodeSet = targetMesh.NodeSets[surface.NodeSetName];
            HashSet<int> nodeIds = new HashSet<int>(nodeSet.Labels);
            //
            float[] distancesAll = new float[allData.Nodes.Coor.Length];
            float[] distances1 = new float[allData.Nodes.Coor.Length];
            float[] distances2 = new float[allData.Nodes.Coor.Length];
            float[] distances3 = new float[allData.Nodes.Coor.Length];
            float[] values = new float[allData.Nodes.Coor.Length];
            //
            Parallel.For(0, values.Length, i =>
            //for (int i = 0; i < values.Length; i++)
            {
                double[] distance;
                double value;
                //
                if (nodeIds.Contains(allData.Nodes.Ids[i]))
                {
                    GetPressureAndDistanceForPoint(allData.Nodes.Coor[i], out distance, out value);
                    //
                    distances1[i] = (float)distance[0];
                    distances2[i] = (float)distance[1];
                    distances3[i] = (float)distance[2];
                    distancesAll[i] = (float)Math.Sqrt(distance[0] * distance[0] +
                                                       distance[1] * distance[1] +
                                                       distance[2] * distance[2]);
                    values[i] = (float)value;
                }
                else
                {
                    distances1[i] = float.NaN;
                    distances2[i] = float.NaN;
                    distances3[i] = float.NaN;
                    distancesAll[i] = float.NaN;
                    values[i] = float.NaN;
                }
            }
            );
            //
            Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
            for (int i = 0; i < allData.Nodes.Coor.Length; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
            FeResults results = new FeResults(resultName, unitSystem);
            results.SetMesh(targetMesh, nodeIdsLookUp);
            // Add distances
            FieldData fieldData = new FieldData(FOFieldNames.Distance);
            fieldData.GlobalIncrementId = 1;
            fieldData.StepType = StepTypeEnum.Static;
            fieldData.Time = 1;
            fieldData.MethodId = 1;
            fieldData.StepId = 1;
            fieldData.StepIncrementId = 1;
            // Distances
            Field field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.All, distancesAll);
            field.AddComponent(FOComponentNames.D1, distances1);
            field.AddComponent(FOComponentNames.D2, distances2);
            field.AddComponent(FOComponentNames.D3, distances3);
            results.AddField(fieldData, field);
            // Add values
            fieldData = new FieldData(fieldData);
            fieldData.Name = FOFieldNames.Imported;
            //
            field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.PRESS, values);
            results.AddField(fieldData, field);
            //
            return results;
        }
        public void GetPressureAndDistanceForPoint(double[] point, out double[] distance, out double value)
        {
            _interpolator.InterpolateAt(point, _interpolatorType, out distance, out value);
            value *= _magnitudeFactor.Value;
        }
        public override double GetPressureForPoint(double[] point)
        {
            _interpolator.InterpolateAt(point, _interpolatorType, out double[] distance, out double value);
            return value * _magnitudeFactor.Value;
        }
        
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_fileName", _fileName, typeof(string));
            info.AddValue("_pressureTime", _pressureTime, typeof(string));
            info.AddValue("_pressureVariableName", _pressureVariableName, typeof(string));
            info.AddValue("_interpolatorType", _interpolatorType, typeof(InterpolatorEnum));
            info.AddValue("_magnitudeFactor", _magnitudeFactor, typeof(EquationContainer));
            info.AddValue("_geomScaleFactor", _geomScaleFactor, typeof(EquationContainer));
            info.AddValue("_oldFileInfo", _oldFileInfo, typeof(FileInfo));
        }
    }
}
