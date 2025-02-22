﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using CaeResults;
using System.Runtime.Serialization;
using System.Data;

namespace CaeModel
{
    [Serializable]
    public class InitialTemperature : InitialCondition, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private EquationContainer _temperature;         //ISerializable


        // Properties                                                                                                               
        public EquationContainer Temperature { get { return _temperature; } set { SetTemp(value); } }


        // Constructors                                                                                                             
        public InitialTemperature(string name, string regionName, RegionTypeEnum regionType)
            : base(name, regionName, regionType, false)
        {
            Temperature = new EquationContainer(typeof(StringTemperatureConverter), 0, null);
        }
        public InitialTemperature(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_temperature":
                        // Compatibility for version v2.2.3
                        if (entry.Value is double valueT)
                            Temperature = new EquationContainer(typeof(StringTemperatureConverter), valueT);
                        else
                            SetTemp((EquationContainer)entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetTemp(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _temperature, value, null, null, checkEquation);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _temperature.CheckEquation();
        }
        // IPreviewable
        public FeResults GetPreview(FeMesh targetMesh, string resultName, UnitSystem unitSystem)
        {
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
            FeNodeSet nodeSet;
            if (RegionType == RegionTypeEnum.NodeSetName)
            {
                nodeSet = targetMesh.NodeSets[RegionName];
            }
            else if (RegionType == RegionTypeEnum.SurfaceName)
            {
                FeSurface surface = targetMesh.Surfaces[RegionName];
                nodeSet = targetMesh.NodeSets[surface.NodeSetName];
            }
            else throw new NotSupportedException();
            //
            HashSet<int> nodeIds = new HashSet<int>(nodeSet.Labels);
            //
            float[] values = new float[allData.Nodes.Coor.Length];
            float temp = (float)_temperature.Value;
            //
            for (int i = 0; i < values.Length; i++)
            {
                if (nodeIds.Contains(allData.Nodes.Ids[i])) values[i] = temp;
                else values[i] = float.NaN;
            }
            //
            Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
            for (int i = 0; i < allData.Nodes.Coor.Length; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
            FeResults results = new FeResults(resultName, unitSystem);
            results.SetMesh(targetMesh, nodeIdsLookUp);
            // Add group
            FieldData fieldData = new FieldData(FOFieldNames.NdTemp);
            fieldData.GlobalIncrementId = 1;
            fieldData.StepType = StepTypeEnum.Static;
            fieldData.Time = 1;
            fieldData.MethodId = 1;
            fieldData.StepId = 1;
            fieldData.StepIncrementId = 1;
            // Add values
            Field field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.T, values);
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
            info.AddValue("_temperature", _temperature, typeof(EquationContainer));
        }
    }
}
