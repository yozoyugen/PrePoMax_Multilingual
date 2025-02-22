﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using CaeResults;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    public class DLoad : Load, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private string _surfaceName;                //ISerializable
        private RegionTypeEnum _regionType;         //ISerializable
        private EquationContainer _magnitude;       //ISerializable


        // Properties                                                                                                               
        public override string RegionName { get { return _surfaceName; } set { _surfaceName = value; } }
        public override RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public string SurfaceName { get { return _surfaceName; } set { _surfaceName = value; } }
        public EquationContainer Magnitude { get { return _magnitude; } set { SetMagnitude(value); } }


        // Constructors                                                                                                             
        public DLoad(string name, string surfaceName, RegionTypeEnum regionType, double magnitude,
                     bool twoD, bool complex, double phaseDeg)
            : base(name, twoD, complex, phaseDeg)
        {
            _surfaceName = surfaceName;
            _regionType = regionType;
            Magnitude = new EquationContainer(typeof(StringPressureConverter), magnitude);
        }
        public DLoad(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_surfaceName":
                        _surfaceName = (string)entry.Value; break;
                    case "_regionType":
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_magnitude":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueDouble)
                            Magnitude = new EquationContainer(typeof(StringPressureConverter), valueDouble);
                        else
                            SetMagnitude((EquationContainer)entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetMagnitude(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _magnitude, value, null, checkEquation);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _magnitude.CheckEquation();
        }
        //
        public FeResults GetPreview(FeMesh targetMesh, string resultName, UnitSystem unitSystem)
        {
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
            FeSurface surface = targetMesh.Surfaces[_surfaceName];
            FeNodeSet nodeSet = targetMesh.NodeSets[surface.NodeSetName];
            HashSet<int> nodeIds = new HashSet<int>(nodeSet.Labels);
            //
            float[] values = new float[allData.Nodes.Coor.Length];
            //
            for (int i = 0; i < values.Length; i++)
            {
                if (nodeIds.Contains(allData.Nodes.Ids[i])) values[i] = (float)_magnitude.Value;
                else values[i] = float.NaN;
            }
            //
            Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
            for (int i = 0; i < allData.Nodes.Coor.Length; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
            FeResults results = new FeResults(resultName, unitSystem);
            results.SetMesh(targetMesh, nodeIdsLookUp);
            // Add distances
            FieldData fieldData = new FieldData(FOFieldNames.Imported);
            fieldData.GlobalIncrementId = 1;
            fieldData.StepType = StepTypeEnum.Static;
            fieldData.Time = 1;
            fieldData.MethodId = 1;
            fieldData.StepId = 1;
            fieldData.StepIncrementId = 1;
            // Add values
            Field field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.PRESS, values);
            results.AddField(fieldData, field);
            //
            return results;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_surfaceName", _surfaceName, typeof(string));
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_magnitude", _magnitude, typeof(EquationContainer));
        }
    }
}
