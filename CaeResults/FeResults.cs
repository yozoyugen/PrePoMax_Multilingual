﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Collections;
using System.IO;
using System.IO.Compression;
using static CaeGlobals.Geometry2;
using System.Numerics;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;

namespace CaeResults
{
    [Serializable]
    public class FeResults //: ISerializable
    {
        // Variables                                                                                                                
        [NonSerialized]
        private Dictionary<int, int> _nodeIdsLookUp;            // [globalId][resultsId] for values
        [NonSerialized]
        private OrderedDictionary<FieldData, Field> _fields;
        [NonSerialized]
        private OrderedDictionary<string, Field> _fieldDataHashField;
        [NonSerialized]
        private Dictionary<int, FeNode> _undeformedNodes;
        [NonSerialized]
        private float _scale;
        [NonSerialized]
        private int _stepId;
        [NonSerialized]
        private int _stepIncrementId;        
        [NonSerialized]
        private bool _complexResultChanged;
        //
        public static readonly string[][] ComplexFieldNames = new string[][] { new string[] { FOFieldNames.Disp,
                                                                                              FOFieldNames.DispR,
                                                                                              FOFieldNames.DispI,
                                                                                              FOFieldNames.DispMag,
                                                                                              FOFieldNames.DispPha,
                                                                                              FOFieldNames.DispMax,
                                                                                              FOFieldNames.DispMaxAng,
                                                                                              FOFieldNames.DispMin,
                                                                                              FOFieldNames.DispMinAng},
                                                                               new string[] { FOFieldNames.Stress,
                                                                                              FOFieldNames.StressR,
                                                                                              FOFieldNames.StressI,
                                                                                              FOFieldNames.StressMag,
                                                                                              FOFieldNames.StressPha,
                                                                                              FOFieldNames.StressMax,
                                                                                              FOFieldNames.StressMaxAng,
                                                                                              FOFieldNames.StressMin,
                                                                                              FOFieldNames.StressMinAng},
                                                                               new string[] { FOFieldNames.ZZStr,
                                                                                              FOFieldNames.ZZStrR,
                                                                                              FOFieldNames.ZZStrI ,
                                                                                              FOFieldNames.ZZStrMag,
                                                                                              FOFieldNames.ZZStrPha,
                                                                                              FOFieldNames.ZZStrMax,
                                                                                              FOFieldNames.ZZStrMaxAng,
                                                                                              FOFieldNames.ZZStrMin,
                                                                                              FOFieldNames.ZZStrMinAng},
                                                                               new string[] { FOFieldNames.ToStrain,
                                                                                              FOFieldNames.ToStraiR,
                                                                                              FOFieldNames.ToStraiI,
                                                                                              FOFieldNames.ToStraiMag,
                                                                                              FOFieldNames.ToStraiPha,
                                                                                              FOFieldNames.ToStraiMax,
                                                                                              FOFieldNames.ToStraiMaxAng,
                                                                                              FOFieldNames.ToStraiMin,
                                                                                              FOFieldNames.ToStraiMinAng},
                                                                               new string[] { FOFieldNames.MeStrain,
                                                                                              FOFieldNames.MeStraiR,
                                                                                              FOFieldNames.MeStraiI,
                                                                                              FOFieldNames.MeStraiMag,
                                                                                              FOFieldNames.MeStraiPha,
                                                                                              FOFieldNames.MeStraiMax,
                                                                                              FOFieldNames.MeStraiMaxAng,
                                                                                              FOFieldNames.MeStraiMin,
                                                                                              FOFieldNames.MeStraiMinAng},
                                                                               new string[] { FOFieldNames.Forc,
                                                                                              FOFieldNames.ForcR,
                                                                                              FOFieldNames.ForcI,
                                                                                              FOFieldNames.ForcMag,
                                                                                              FOFieldNames.ForcPha,
                                                                                              FOFieldNames.ForcMax,
                                                                                              FOFieldNames.ForcMaxAng,
                                                                                              FOFieldNames.ForcMin,
                                                                                              FOFieldNames.ForcMinAng},
                                                                               new string[] { FOFieldNames.HError,
                                                                                              FOFieldNames.HErrorR,
                                                                                              FOFieldNames.HErrorI,
                                                                                              FOFieldNames.HErrorMag,
                                                                                              FOFieldNames.HErrorPha,
                                                                                              FOFieldNames.HErrorMax,
                                                                                              FOFieldNames.HErrorMaxAng,
                                                                                              FOFieldNames.HErrorMin,
                                                                                              FOFieldNames.HErrorMinAng},
                                                                               new string[] { FOFieldNames.Error,
                                                                                              FOFieldNames.ErrorR,
                                                                                              FOFieldNames.ErrorI,
                                                                                              FOFieldNames.ErrorMag,
                                                                                              FOFieldNames.ErrorPha,
                                                                                              FOFieldNames.ErrorMax,
                                                                                              FOFieldNames.ErrorMaxAng,
                                                                                              FOFieldNames.ErrorMin,
                                                                                              FOFieldNames.ErrorMinAng}};
        //
        private string _hashName;
        private string _fileName;
        private FeMesh _mesh;
        private OrderedDictionary<string, ResultFieldOutput> _resultFieldOutputs;
        private OrderedDictionary<string, ResultHistoryOutput> _resultHistoryOutputs;
        private HistoryResults _history;
        private DateTime _dateTime;
        private UnitSystem _unitSystem;
        private string _deformationFieldOutputName;
        private ComplexResultTypeEnum _complexResultType;
        private float _complexAngleDeg;



        // Properties                                                                                                               
        public Dictionary<int, FeNode> UndeformedNodes { get { return _undeformedNodes; } }
        public string HashName { get { return _hashName; } set { _hashName = value; } }
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public FeMesh Mesh { get { return _mesh; } set { _mesh = value; } }
        public DateTime DateTime { get { return _dateTime; } set { _dateTime = value; } }
        public UnitSystem UnitSystem { get { return _unitSystem; } set { _unitSystem = value; } }
        public string DeformationFieldOutputName
        {
            get
            {
                OrderedDictionary<string, string> possibleNames = GetPossibleDeformationFieldOutputNamesMap();
                foreach (var entry in possibleNames)
                {
                    if (entry.Value == _deformationFieldOutputName) return entry.Key;
                }
                return "";
            }
            set
            {
                string newDeformationFieldOutputName;
                OrderedDictionary<string, string> possibleNames = GetPossibleDeformationFieldOutputNamesMap();
                if (possibleNames.TryGetValue(value, out newDeformationFieldOutputName)) { }
                else newDeformationFieldOutputName = possibleNames.Values.First();
                // Reset global mesh deformation settings if needed
                if (newDeformationFieldOutputName != _deformationFieldOutputName)
                {
                    _deformationFieldOutputName = newDeformationFieldOutputName;
                    ResetScaleStepIncrement();
                }
            }
        }


        // Constructor                                                                                                              
        public FeResults(string fileName, UnitSystem unitSystem)
        {
            _fileName = fileName;
            _hashName = Tools.GetRandomString(8);
            _mesh = null;
            _resultFieldOutputs = new OrderedDictionary<string, ResultFieldOutput>("ResultFieldOutputs",
                                                                                   StringComparer.OrdinalIgnoreCase);
            _resultHistoryOutputs = new OrderedDictionary<string, ResultHistoryOutput>("ResultHistoryOutputs",
                                                                                       StringComparer.OrdinalIgnoreCase);
            _nodeIdsLookUp = null;
            _fields = new OrderedDictionary<FieldData, Field>("Fields");
            _fieldDataHashField = new OrderedDictionary<string, Field>("HashFieldPairs");
            _history = null;
            if (unitSystem == null) _unitSystem = new UnitSystem();
            else _unitSystem = unitSystem;
            _deformationFieldOutputName = FOFieldNames.Disp;
            _complexResultChanged = false;
        }
        //public FeResults(SerializationInfo info, StreamingContext context)
        //{

        //}
        //// ISerialization
        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    // Using typeof() works also for null fields
        //    //info.AddValue("_meshRepresentation", _meshRepresentation, typeof(MeshRepresentation));
        //    //info.AddValue("_meshRefinements", _meshRefinements, typeof(OrderedDictionary<string, FeMeshRefinement>));
        //    //info.AddValue("_parts", _parts, typeof(OrderedDictionary<string, BasePart>));
        //    //info.AddValue("_nodeSets", _nodeSets, typeof(OrderedDictionary<string, FeNodeSet>));
        //    //info.AddValue("_elementSets", _elementSets, typeof(OrderedDictionary<string, FeElementSet>));
        //    //info.AddValue("_surfaces", _surfaces, typeof(OrderedDictionary<string, FeSurface>));
        //    //info.AddValue("_referencePoints", _referencePoints, typeof(OrderedDictionary<string, FeReferencePoint>));
        //    //info.AddValue("_maxNodeId", _maxNodeId, typeof(int));
        //    //info.AddValue("_maxElementId", _maxElementId, typeof(int));
        //    //info.AddValue("_boundingBox", _boundingBox, typeof(BoundingBox));
        //}

        // Static methods                                                                                                           
        public static void WriteToBinaryWriter(FeResults results, BinaryWriter bw)
        {
            if (results == null)
            {
                bw.Write(-1);
            }
            else
            {
                bw.Write(1);
                // Mesh
                Dictionary<int, FeNode> tmp = results.Mesh.Nodes;
                results.Mesh.Nodes = results._undeformedNodes;
                FeMesh.WriteToBinaryWriter(results.Mesh, bw);
                results.Mesh.Nodes = tmp;
                // Node lookup
                if (results._nodeIdsLookUp == null) bw.Write(-1);
                else
                {
                    bw.Write(1);
                    //
                    bw.Write(results._nodeIdsLookUp.Count);
                    foreach (var entry in results._nodeIdsLookUp)
                    {
                        bw.Write(entry.Key);
                        bw.Write(entry.Value);
                    }
                }
                // Fields
                if (results._fields == null) bw.Write(-1);
                else
                {
                    // Set complex real
                    ComplexResultTypeEnum prevComplexResultType = results._complexResultType;
                    float prevComplexAngleDeg = results._complexAngleDeg;
                    results.SetComplexResultTypeAndAngle(ComplexResultTypeEnum.Real, 0);
                    // Delete complex
                    results.RemoveComplexResults();
                    //
                    bw.Write(1);
                    //
                    bw.Write(results._fields.Count);
                    foreach (var entry in results._fields)
                    {
                        entry.Value.RemoveInvariants();
                        //
                        FieldData.WriteToFile(entry.Key, bw);
                        Field.WriteToBinaryWriter(entry.Value, bw);
                        //
                        entry.Value.ComputeInvariants();
                    }
                    // Prepare complex
                    results.PrepareComplexResults();
                    // Reset complex
                    results.SetComplexResultTypeAndAngle(prevComplexResultType, prevComplexAngleDeg);
                }
            }
        }
        public static void WriteToFileStream(FeResults results, FileStream fileStream, CompressionLevel compressionLevel)
        {
            if (results == null)
            {
                // Write the result state
                Tools.WriteIntToFileStream(fileStream, -1);
            }
            else
            {
                // Write the result state
                Tools.WriteIntToFileStream(fileStream, 1);
                // Write the results mesh data
                using (BinaryWriter bw = new BinaryWriter(new MemoryStream()))
                {
                    Dictionary<int, FeNode> tmp = results.Mesh.Nodes;
                    results.Mesh.Nodes = results._undeformedNodes;
                    FeMesh.WriteToBinaryWriter(results.Mesh, bw);
                    results.Mesh.Nodes = tmp;
                    // Node lookup
                    if (results._nodeIdsLookUp == null) bw.Write(-1);
                    else
                    {
                        bw.Write(1);
                        //
                        bw.Write(results._nodeIdsLookUp.Count);
                        foreach (var entry in results._nodeIdsLookUp)
                        {
                            bw.Write(entry.Key);
                            bw.Write(entry.Value);
                        }
                    }
                    // Rewind the writer
                    bw.Flush();
                    bw.BaseStream.Position = 0;
                    // Compress the writer
                    byte[] compressedData = Tools.Compress(bw.BaseStream, compressionLevel);
                    // Write the length of the compressed data
                    Tools.WriteIntToFileStream(fileStream, compressedData.Length);
                    // Write the compressed data
                    fileStream.Write(compressedData, 0, compressedData.Length);
                }
                // Fields
                if (results._fields == null)
                {
                    // Write the field state
                    Tools.WriteIntToFileStream(fileStream, -1);
                }
                else
                {
                    // Write the field state
                    Tools.WriteIntToFileStream(fileStream, 1);
                    // Set complex to real
                    ComplexResultTypeEnum prevComplexResultType = results._complexResultType;
                    float prevComplexAngleDeg = results._complexAngleDeg;
                    results.SetComplexResultTypeAndAngle(ComplexResultTypeEnum.Real, 0);
                    // Delete complex results
                    results.RemoveComplexResults();
                    // Write the number of fields
                    Tools.WriteIntToFileStream(fileStream, results._fields.Count);
                    //
                    foreach (var entry in results._fields)
                    {
                        entry.Value.RemoveInvariants();
                        //
                        using (BinaryWriter bw = new BinaryWriter(new MemoryStream()))
                        {
                            FieldData.WriteToFile(entry.Key, bw);
                            Field.WriteToBinaryWriter(entry.Value, bw);
                            // Rewind the writer
                            bw.Flush();
                            bw.BaseStream.Position = 0;
                            // Compress the writer
                            byte[] compressedData = Tools.Compress(bw.BaseStream, compressionLevel);
                            // Write the length of the compressed data
                            Tools.WriteIntToFileStream(fileStream, compressedData.Length);
                            // Write the compressed data
                            fileStream.Write(compressedData, 0, compressedData.Length);
                        }
                        //
                        entry.Value.ComputeInvariants();
                    }
                    // Prepare complex
                    results.PrepareComplexResults();
                    // Reset complex
                    results.SetComplexResultTypeAndAngle(prevComplexResultType, prevComplexAngleDeg);
                }
            }
        }
        public static void ReadFromBinaryReader(FeResults results, BinaryReader br, int version)
        {
            int numItems;
            FieldData fieldData;
            Field field;
            //
            int exist = br.ReadInt32();
            if (exist == 1)
            {
                // Mesh
                FeMesh.ReadFromBinaryReader(results.Mesh, br, version);
                results.InitializeUndeformedNodes();
                // Node lookup
                exist = br.ReadInt32();
                if (exist == 1)
                {
                    numItems = br.ReadInt32();
                    results._nodeIdsLookUp = new Dictionary<int, int>();
                    for (int i = 0; i < numItems; i++) results._nodeIdsLookUp.Add(br.ReadInt32(), br.ReadInt32());
                }
                // Fields
                exist = br.ReadInt32();
                if (exist == 1)
                {
                    numItems = br.ReadInt32();
                    results._fields = new OrderedDictionary<FieldData, Field>("Fields");
                    results._fieldDataHashField = new OrderedDictionary<string, Field>("HashFieldPairs");
                    for (int i = 0; i < numItems; i++)
                    {
                        fieldData = FieldData.ReadFromFile(br, version);
                        field = Field.ReadFromBinaryReader(br, version);
                        if (field != null)
                        {
                            //
                            field.ComputeInvariants();
                            //
                            results.AddField(fieldData, field);
                        }
                    }
                    // Prepare complex
                    results.PrepareComplexResults();
                    // Compatibility v1.5.3
                    if (results._resultFieldOutputs == null)
                        results._resultFieldOutputs = new OrderedDictionary<string, ResultFieldOutput>("ResultFieldOutputs",
                                                                                                       StringComparer.OrdinalIgnoreCase);
                }
            }
            // Compatibility v2.1.0
            if (results._resultHistoryOutputs == null)
                results._resultHistoryOutputs = new OrderedDictionary<string, ResultHistoryOutput>("ResultHistoryOutputs",
                                                                                                   StringComparer.OrdinalIgnoreCase);
        }
        public static void ReadFromFileStream(FeResults results, FileStream fileStream, int version)
        {
            int numItems;
            int numOfBytes;
            FieldData fieldData;
            Field field;
            byte[] compressedData;
            // Read the result state
            int exist = Tools.ReadIntFromFileStream(fileStream);
            if (exist == 1)
            {
                // Mesh
                numOfBytes = Tools.ReadIntFromFileStream(fileStream);
                compressedData = new byte[numOfBytes];
                fileStream.Read(compressedData, 0, compressedData.Length);
                //
                using (MemoryStream memoryStream = new MemoryStream(compressedData))
                using (BinaryReader br = new BinaryReader(Tools.Decompress(memoryStream)))
                {
                    FeMesh.ReadFromBinaryReader(results.Mesh, br, version);
                    //
                    results.InitializeUndeformedNodes();
                    // Node lookup
                    exist = br.ReadInt32();
                    if (exist == 1)
                    {
                        numItems = br.ReadInt32();
                        results._nodeIdsLookUp = new Dictionary<int, int>();
                        for (int i = 0; i < numItems; i++) results._nodeIdsLookUp.Add(br.ReadInt32(), br.ReadInt32());
                    }
                }
                // Read the fields state
                exist = Tools.ReadIntFromFileStream(fileStream);
                if (exist == 1)
                {
                    numItems = Tools.ReadIntFromFileStream(fileStream);
                    results._fields = new OrderedDictionary<FieldData, Field>("Fields");
                    results._fieldDataHashField = new OrderedDictionary<string, Field>("HashFieldPairs");
                    for (int i = 0; i < numItems; i++)
                    {
                        numOfBytes = Tools.ReadIntFromFileStream(fileStream);
                        compressedData = new byte[numOfBytes];
                        fileStream.Read(compressedData, 0, compressedData.Length);
                        //
                        using (MemoryStream memoryStream = new MemoryStream(compressedData))
                        using (BinaryReader br = new BinaryReader(Tools.Decompress(memoryStream)))
                        {
                            fieldData = FieldData.ReadFromFile(br, version);
                            field = Field.ReadFromBinaryReader(br, version);
                        }
                        //
                        if (field != null)
                        {
                            field.ComputeInvariants();
                            //
                            results.AddField(fieldData, field);
                        }
                    }
                    // Prepare complex
                    results.PrepareComplexResults();
                    // Compatibility v1.5.3
                    if (results._resultFieldOutputs == null)
                        results._resultFieldOutputs = new OrderedDictionary<string, ResultFieldOutput>("ResultFieldOutputs",
                                                                                                       StringComparer.OrdinalIgnoreCase);
                }
            }
            // Compatibility v2.1.0
            if (results._resultHistoryOutputs == null)
                results._resultHistoryOutputs = new OrderedDictionary<string, ResultHistoryOutput>("ResultHistoryOutputs",
                                                                                                   StringComparer.OrdinalIgnoreCase);
        }


        // Methods                                                                                                                  
        public string[] CheckValidity(List<Tuple<NamedClass, string>> items)
        {
            // Tuple<NamedClass, string>   ...   Tuple<invalidItem, stepName>
            List<string> invalidItems = new List<string>();
            bool valid;
            // Result field outputs
            HashSet<string> names;
            ResultFieldOutput existingResultFieldOutput;
            List<ResultFieldOutput> fieldIindependencySequence = GetResultFieldOutputIndependencySequence();
            Dictionary<string, string[]> filedNameComponentNames = GetAllVisibleFiledNameComponentNames();
            //
            foreach (var resultFieldOutput in fieldIindependencySequence)
            {
                valid = true;
                names = new HashSet<string>(filedNameComponentNames.Keys);
                string[] parentNames = resultFieldOutput.GetParentNames();
                names.IntersectWith(parentNames);
                valid &= names.Count == parentNames.Length;
                //
                if (valid)
                {
                    // Check parents for validity
                    foreach (var name in parentNames)
                    {
                        if (_resultFieldOutputs.TryGetValue(name, out existingResultFieldOutput))
                            valid &= existingResultFieldOutput.Valid;
                    }
                    //
                    if (valid)
                    {
                        if (resultFieldOutput is ResultFieldOutputLimit rfol)
                        {
                            // Parent components
                            names = new HashSet<string>(filedNameComponentNames[rfol.FieldName]);
                            valid &= names.Contains(rfol.ComponentName);
                            //
                            if (rfol.LimitPlotBasedOn == LimitPlotBasedOnEnum.Parts)
                                names = new HashSet<string>(_mesh.Parts.Keys);
                            else if (rfol.LimitPlotBasedOn == LimitPlotBasedOnEnum.ElementSets)
                            {
                                names = new HashSet<string>(_mesh.ElementSets.Keys);
                                names.Add(ResultFieldOutputLimit.AllElementsName);
                            }
                            else throw new NotSupportedException();
                            //
                            names.IntersectWith(rfol.ItemNameLimit.Keys);
                            valid &= names.Count == rfol.ItemNameLimit.Count;
                        }
                        else if (resultFieldOutput is ResultFieldOutputEnvelope rfoen)
                        {
                            // Parent components
                            names = new HashSet<string>(filedNameComponentNames[rfoen.FieldName]);
                            valid &= names.Contains(rfoen.ComponentName);
                        }
                        else if (resultFieldOutput is ResultFieldOutputEquation rfoeq)
                        {
                            names = new HashSet<string>(filedNameComponentNames.Keys);
                            names.IntersectWith(parentNames);
                            valid &= names.Count == parentNames.Length;
                            //
                            if (valid)
                            {
                                // Check parents for validity
                            }
                        }
                        else if (resultFieldOutput is ResultFieldOutputCoordinateSystemTransform rfocst)
                        {
                            valid &= _mesh.CoordinateSystems.ContainsValidKey(rfocst.CoordinateSystemName);
                            DataTypeEnum dataType = GetFieldDataType(rfocst.FieldName);
                            valid &= dataType == DataTypeEnum.Vector || dataType == DataTypeEnum.Tensor;
                            valid &= DoesFieldContainsAllNecessaryComponents(rfocst.FieldName);
                        }
                        else throw new NotSupportedException();
                    }
                }
                //
                SetItemValidity(resultFieldOutput, valid, items);
                if (!valid && resultFieldOutput.Active) invalidItems.Add("Field output: " + resultFieldOutput.Name);
            }
            // Result history outputs
            ResultHistoryOutput existingResultHistoryOutput;
            List<ResultHistoryOutput> historyIndependencySequence = GetResultHistoryOutputIndependencySequence();
            //
            foreach (var resultHistoryOutput in historyIndependencySequence)
            {
                // Parent names
                string[] parentNames = resultHistoryOutput.GetParentNames();
                valid = true;
                //
                if (resultHistoryOutput is ResultHistoryOutputFromField rhoff)
                {
                    names = new HashSet<string>(GetVisibleFieldNames());
                    valid &= names.Contains(rhoff.FieldName);
                    //
                    if (valid)
                    {
                        // Check if USER DEFINED FILED is used and if it is valid
                        if (_resultFieldOutputs.TryGetValue(rhoff.FieldName, out existingResultFieldOutput))
                            valid &= existingResultFieldOutput.Valid;
                        //
                        if (valid)
                        {
                            parentNames = rhoff.ComponentNames;
                            names = new HashSet<string>(filedNameComponentNames[rhoff.FieldName]);
                        }
                    }
                }
                else if (resultHistoryOutput is ResultHistoryOutputFromEquation)
                {
                    names = new HashSet<string>(_history.Sets.Keys);
                    names.IntersectWith(parentNames);
                    valid &= names.Count == parentNames.Length;
                    //
                    if (valid)
                    {
                        // Check parents for validity
                        foreach (var name in parentNames)
                        {
                            if (_resultHistoryOutputs.TryGetValue(name, out existingResultHistoryOutput))
                                valid &= existingResultHistoryOutput.Valid;
                        }
                    }
                }
                //
                SetItemValidity(resultHistoryOutput, valid, items);
                if (!valid && resultHistoryOutput.Active) invalidItems.Add("History output: " + resultHistoryOutput.Name);
            }
            return invalidItems.ToArray();
        }
        private void SetItemValidity(NamedClass item, bool validity, List<Tuple<NamedClass, string>> items)
        {
            // only changed items are added for the update
            if (item.Valid != validity)
            {
                item.Valid = validity;
                items.Add(new Tuple<NamedClass, string>(item, null));
            }
        }
        public void Preprocess()
        {
            ComputeVisibleFieldInvariants();
            PrepareComplexResults();
        }
        // Mesh                                     
        public void SetMesh(FeMesh mesh, Dictionary<int, int> nodeIdsLookUp)
        {
            _mesh = new FeMesh(mesh, mesh.Parts.Keys.ToArray());
            //
            List<BasePart> parts = new List<BasePart>();
            foreach (var entry in _mesh.Parts)
            {
                if (!(entry.Value is ResultPart)) parts.Add(entry.Value);
            }
            //
            foreach (var part in parts)
            {
                _mesh.Parts.Replace(part.Name, part.Name, new ResultPart(part));
            }
            //
            InitializeUndeformedNodes();
            //
            _nodeIdsLookUp = nodeIdsLookUp;
        }
        public void ScaleAllParts(double scale)
        {
            _mesh.ScaleAllParts(scale);
            //
            InitializeUndeformedNodes();
        }
        public string[] AddPartsFromMesh(FeMesh mesh, string[] partNames)
        {
            _mesh.Nodes = _undeformedNodes;
            string[] addedPartNames = _mesh.AddPartsFromMesh(mesh, partNames, null, null, false);
            InitializeUndeformedNodes();
            return addedPartNames;
        }
        public string[] RemoveParts(string[] partNames)
        {
            _mesh.Nodes = _undeformedNodes;
            string[] removedPartNames;
            _mesh.RemoveParts(partNames, out removedPartNames, false);
            InitializeUndeformedNodes();
            return removedPartNames;
        }
        private void InitializeUndeformedNodes()
        {
            _undeformedNodes = new Dictionary<int, FeNode>(_mesh.Nodes.Count());
            foreach (var entry in _mesh.Nodes) _undeformedNodes.Add(entry.Key, new FeNode(entry.Value));
            ResetScaleStepIncrement();
        }
        private void ResetScaleStepIncrement()
        {
            _scale = -1;
            _stepId = -1;
            _stepIncrementId = -1;
        }
        public void AddResults(FeResults results)
        {
            int[] stepIds = GetAllStepIds();
            int lastStepId = stepIds.Last();
            int[] stepIncrementIds = GetStepIncrementIds(lastStepId);
            int lastStepIncrementId = stepIncrementIds.Last();
            string[] fieldNames = GetAllFieldNames();
            string[] componentNames;
            //
            float lastTime = 0;
            FieldData fieldData;
            foreach (var fieldName in fieldNames)
            {
                componentNames = GetFieldComponentNames(fieldName);
                foreach (var componentName in componentNames)
                {
                    fieldData = GetFieldData(fieldName, componentName, lastStepId, lastStepIncrementId);
                    if (fieldData.Valid)
                    {
                        if (fieldData.Time > lastTime) lastTime = fieldData.Time;
                    }
                }
            }
            //
            AddFieldOutputs(results, lastTime, lastStepId, lastStepIncrementId);
            AddHistoryOutputs(results, lastTime);
            //
            ReplaceAllResultFieldOutputs();
        }
        private void AddFieldOutputs(FeResults results, float lastTime, int lastStepId, int lastStepIncrementId)
        {
            Field lastWearDepthField = GetField(new FieldData(FOFieldNames.WearDepth, "", lastStepId, lastStepIncrementId), false);
            //if (lastWearDepthField == null) throw new NotSupportedException();
            //
            Field currentField;
            FieldData fieldData;
            FieldComponent fc1;
            FieldComponent fc2;
            FieldComponent fc3;
            float[] values1;
            float[] values2;
            float[] values3;
            float[] magnitude;
            //float[][] vectors = GetLocalVectors(FOFieldNames.WearDepth);
            float[][] vectors = SubtractMeshPositions(results, this);
            float[][] vectorsWD = GetLocalVectors(FOFieldNames.WearDepth);
            // Add mesh deformation
            foreach (var entry in results._fields.ToArray())    // copy to modify
            {
                fieldData = entry.Key;
                currentField = entry.Value;
                //
                if (fieldData.StepId == 0 && fieldData.StepIncrementId == 0) continue; // Zero increment - Find all occurrences!!!
                //
                if (fieldData.Name == FOFieldNames.WearDepth)
                {
                    // H1
                    values1 = currentField.GetComponentValues(FOComponentNames.H1);
                    magnitude = new float[values1.Length];
                    for (int i = 0; i < values1.Length; i++)
                    {
                        values1[i] += vectorsWD[0][i];
                        magnitude[i] += values1[i] * values1[i];
                    }
                    fc1 = new FieldComponent(FOComponentNames.H1, values1);
                    // H2
                    values2 = currentField.GetComponentValues(FOComponentNames.H2);
                    for (int i = 0; i < values2.Length; i++)
                    {
                        values2[i] += vectorsWD[1][i];
                        magnitude[i] += values2[i] * values2[i];
                    }
                    fc2 = new FieldComponent(FOComponentNames.H2, values2);
                    // H3
                    values3 = currentField.GetComponentValues(FOComponentNames.H3);
                    for (int i = 0; i < values3.Length; i++)
                    {
                        values3[i] += vectorsWD[2][i];
                        magnitude[i] += values3[i] * values3[i];
                    }
                    fc3 = new FieldComponent(FOComponentNames.H3, values3);
                    // Magnitude
                    for (int i = 0; i < magnitude.Length; i++) magnitude[i] = (float)Math.Sqrt(magnitude[i]);
                    // Create field
                    currentField = new Field(fieldData.Name);
                    currentField.AddComponent(FOComponentNames.All, magnitude);
                    currentField.AddComponent(fc1);
                    currentField.AddComponent(fc2);
                    currentField.AddComponent(fc3);
                    // Replace field
                    results.ReplaceOrAddField(fieldData, currentField);
                }
                else if (fieldData.Name == FOFieldNames.MeshDeformation || fieldData.Name == FOFieldNames.DispDeformationDepth)
                {
                    // U1
                    values1 = currentField.GetComponentValues(FOComponentNames.U1);
                    magnitude = new float[values1.Length];
                    for (int i = 0; i < values1.Length; i++)
                    {
                        values1[i] += vectors[0][i];
                        magnitude[i] += values1[i] * values1[i];
                    }
                    fc1 = new FieldComponent(FOComponentNames.U1, values1);
                    // U2
                    values2 = currentField.GetComponentValues(FOComponentNames.U2);
                    for (int i = 0; i < values2.Length; i++)
                    {
                        values2[i] += vectors[1][i];
                        magnitude[i] += values2[i] * values2[i];
                    }
                    fc2 = new FieldComponent(FOComponentNames.U2, values2);
                    // U3
                    values3 = currentField.GetComponentValues(FOComponentNames.U3);
                    for (int i = 0; i < values3.Length; i++)
                    {
                        values3[i] += vectors[2][i];
                        magnitude[i] += values3[i] * values3[i];
                    }
                    fc3 = new FieldComponent(FOComponentNames.U3, values3);
                    // Magnitude
                    for (int i = 0; i < magnitude.Length; i++) magnitude[i] = (float)Math.Sqrt(magnitude[i]);
                    // Create field
                    currentField = new Field(fieldData.Name);
                    currentField.AddComponent(FOComponentNames.All, magnitude);
                    currentField.AddComponent(fc1);
                    currentField.AddComponent(fc2);
                    currentField.AddComponent(fc3);
                    // Replace field
                    results.ReplaceOrAddField(fieldData, currentField);
                }
            }
            // Copy all fields
            foreach (var entry in results._fields)
            {
                fieldData = entry.Key;
                currentField = entry.Value;
                //
                if (fieldData.StepId == 0 && fieldData.StepIncrementId == 0) continue; // Zero increment - Find all occurrences!!!
                //
                fieldData.Time += lastTime;
                fieldData.StepId += lastStepId;
                //
                AddField(fieldData, currentField);
            }
        }
        private void AddHistoryOutputs(FeResults results, float lastTime)
        {
            if (results._history == null) return;
            // Append all entries
            HistoryResultEntries historyResultEntry;
            foreach (var setEntry in results._history.Sets)
            {
                foreach (var fieldEntry in setEntry.Value.Fields)
                {
                    foreach (var componentEntry in fieldEntry.Value.Components)
                    {
                        foreach (var entry in componentEntry.Value.Entries)
                        {
                            entry.Value.ShiftTime(lastTime);
                            historyResultEntry = GetHistoryResultEntry(setEntry.Key, fieldEntry.Key, componentEntry.Key, entry.Key);
                            if (historyResultEntry != null)
                            {
                                historyResultEntry.Append(entry.Value);
                            }
                        }
                    }
                }
            }
        }
        private static float[][] SubtractMeshPositions(FeResults results1, FeResults results2)
        {
            if (results1._undeformedNodes.Count == results2._undeformedNodes.Count)
            {
                int id;
                FeNode node1;
                FeNode node2;
                float[][] diff = new float[3][];
                diff[0] = new float[results1._undeformedNodes.Count];
                diff[1] = new float[results1._undeformedNodes.Count];
                diff[2] = new float[results1._undeformedNodes.Count];
                //
                foreach (var entry in results1._undeformedNodes)
                {
                    node1 = entry.Value;
                    node2 = results2._undeformedNodes[node1.Id];
                    id = results1._nodeIdsLookUp[node1.Id];
                    //
                    diff[0][id] = (float)(node1.X - node2.X);
                    diff[1][id] = (float)(node1.Y - node2.Y);
                    diff[2][id] = (float)(node1.Z - node2.Z);
                }
                return diff;
            }
            else  return null;
        }
        // Mesh deformation                         
        private string GetInternalDeformationFieldOutputName(int stepId, int stepIncrementId)
        {
            if (_deformationFieldOutputName == FOFieldNames.Disp)
            {
                if (_complexResultType == ComplexResultTypeEnum.Imaginary ||
                    _complexResultType == ComplexResultTypeEnum.Magnitude ||
                    _complexResultType == ComplexResultTypeEnum.Phase ||
                    _complexResultType == ComplexResultTypeEnum.Max ||
                    _complexResultType == ComplexResultTypeEnum.AngleAtMax ||
                    _complexResultType == ComplexResultTypeEnum.Min ||
                    _complexResultType == ComplexResultTypeEnum.AngleAtMin)
                {
                    FieldData fieldData = new FieldData(FOFieldNames.DispR, null, stepId, stepIncrementId);
                    Field field = GetField(fieldData, false);
                    if (field != null) return FOFieldNames.DispR;
                }
            }
            return _deformationFieldOutputName;
        }
        public void SetMeshDeformation(float scale, int stepId, int stepIncrementId)
        {
            if (scale != _scale || stepId != _stepId || stepIncrementId != _stepIncrementId || _complexResultChanged)
            {
                _scale = scale;
                _stepId = stepId;
                _stepIncrementId = stepIncrementId;
                _complexResultChanged = false;
                //
                int resultNodeId;
                double[] offset;
                FeNode node;
                FeNode deformedNode;
                Dictionary<int, FeNode> deformedNodes = null;
                //
                if (_scale != 0)
                {
                    float[][] deformations = GetNodalMeshDeformations(_stepId, _stepIncrementId);
                    if (deformations != null)
                    {
                        deformedNodes = new Dictionary<int, FeNode>();
                        //
                        foreach (var entry in _mesh.Parts)
                        {
                            offset = entry.Value.Offset;
                            if (offset == null) offset = new double[3];
                            //
                            foreach (var nodeId in entry.Value.NodeLabels)
                            {
                                node = _undeformedNodes[nodeId];
                                // Result parts
                                if (_nodeIdsLookUp.TryGetValue(node.Id, out resultNodeId))
                                {
                                    deformedNode = new FeNode(node.Id,
                                                              node.X + offset[0] + _scale * deformations[0][resultNodeId],
                                                              node.Y + offset[1] + _scale * deformations[1][resultNodeId],
                                                              node.Z + offset[2] + _scale * deformations[2][resultNodeId]);
                                }
                                // Geometry parts
                                else
                                {
                                    if (offset[0] != 0 || offset[1] != 0 || offset[2] != 0)
                                    {
                                        deformedNode = new FeNode(node.Id,
                                                                  node.X + offset[0],
                                                                  node.Y + offset[1],
                                                                  node.Z + offset[2]);
                                    }
                                    else deformedNode = node;
                                }
                                // Check for merged nodes as in compound parts
                                if (!deformedNodes.ContainsKey(deformedNode.Id))
                                    deformedNodes.Add(deformedNode.Id, deformedNode);
                            }
                        }
                    }
                }
                if (deformedNodes == null)
                {
                    deformedNodes = new Dictionary<int, FeNode>();
                    foreach (var entry in _mesh.Parts)
                    {
                        offset = entry.Value.Offset;
                        //
                        foreach (var nodeId in entry.Value.NodeLabels)
                        {
                            node = _undeformedNodes[nodeId];

                            if (offset[0] != 0 || offset[1] != 0 || offset[2] != 0)
                            {
                                deformedNode = new FeNode(node.Id,
                                                          node.X + offset[0],
                                                          node.Y + offset[1],
                                                          node.Z + offset[2]);
                            }
                            else deformedNode = node;

                            // Result parts
                            //if (_nodeIdsLookUp.TryGetValue(node.Id, out resultNodeId))
                            //    deformedNode = new FeNode(node.Id, node.X + offset[0], node.Y + offset[1], node.Z + offset[2]);
                            // Geometry parts
                            //else deformedNode = node;


                            // Check for merged nodes as in compound parts
                            if (!deformedNodes.ContainsKey(deformedNode.Id))
                                deformedNodes.Add(deformedNode.Id, deformedNode);
                        }
                    }
                }
                //
                _mesh.Nodes = deformedNodes;
            }
        }
        public void SetPartDeformation(BasePart part, float scale, int stepId, int stepIncrementId)
        {
            // Reset global mesh deformation settings
            ResetScaleStepIncrement();
            //
            bool scaled = false;
            double[] offset;
            FeNode node;
            FeNode deformedNode;
            //
            if (scale != 0)
            {
                float[][] deformations = GetNodalMeshDeformations(stepId, stepIncrementId);
                if (deformations != null)
                {
                    scaled = true;
                    int resultNodeId;
                    offset = part.Offset;
                    //
                    foreach (var nodeId in part.NodeLabels)
                    {
                        resultNodeId = _nodeIdsLookUp[nodeId];
                        node = _undeformedNodes[nodeId];
                        //
                        deformedNode = new FeNode(node.Id,
                                                  node.X + offset[0] + scale * deformations[0][resultNodeId],
                                                  node.Y + offset[1] + scale * deformations[1][resultNodeId],
                                                  node.Z + offset[2] + scale * deformations[2][resultNodeId]);
                        _mesh.Nodes[deformedNode.Id] = deformedNode;
                    }
                }
            }
            if (!scaled)
            {
                offset = part.Offset;
                foreach (var nodeId in part.NodeLabels)
                {
                    node = _undeformedNodes[nodeId];
                    deformedNode = new FeNode(node.Id, node.X + offset[0], node.Y + offset[1], node.Z + offset[2]);
                    _mesh.Nodes[node.Id] = deformedNode;
                }
            }
        }
        public float[][] GetNodalMeshDeformations(int stepId, int stepIncrementId)
        {
            string deformationFieldOutputName = GetInternalDeformationFieldOutputName(stepId, stepIncrementId);
            return GetNodalMeshDeformations(deformationFieldOutputName, stepId, stepIncrementId);
        }
        public float[][] GetNodalMeshDeformations(string deformationFieldOutputName, int stepId, int stepIncrementId)
        {
            float[][] deformations = null;
            string[] componentNames = GetDeformationFieldOutputComponentNames(deformationFieldOutputName);
            string[] existingComponentNames = GetFieldComponentNames(deformationFieldOutputName);
            //
            if (existingComponentNames != null &&
                existingComponentNames.Contains(componentNames[0]) &&
                existingComponentNames.Contains(componentNames[1]) &&
                existingComponentNames.Contains(componentNames[2]))
            {
                Field deformationField = GetField(new FieldData(deformationFieldOutputName, "", stepId, stepIncrementId), false);
                if (deformationField == null) return null;
                //
                deformations = new float[3][];
                deformations[0] = deformationField.GetComponentValues(componentNames[0]);
                if (deformations[0] == null) return null;
                deformations[1] = deformationField.GetComponentValues(componentNames[1]);
                if (deformations[1] == null) return null;
                deformations[2] = deformationField.GetComponentValues(componentNames[2]);
                if (deformations[2] == null) return null;
            }
            return deformations;
        }
        public static OrderedDictionary<string, string> GetPossibleDeformationFieldOutputNamesMap()
        {
            OrderedDictionary<string, string> names =
                new OrderedDictionary<string, string>("Deformation Names", StringComparer.OrdinalIgnoreCase);
            names.Add("Displacements", FOFieldNames.Disp);
            names.Add("Forces", FOFieldNames.Forc);
            names.Add("Surface Normals", FOFieldNames.SurfaceNormal);
            names.Add("Wear Depths", FOFieldNames.WearDepth);
            names.Add("Mesh Deformation", FOFieldNames.MeshDeformation);
            names.Add("Disp&Def&Depth", FOFieldNames.DispDeformationDepth);
            return names;
        }
        public static string[] GetPossibleDeformationFieldOutputNames()
        {
            return GetPossibleDeformationFieldOutputNamesMap().Keys.ToArray();
        }
        public string[] GetExistingDeformationFieldOutputNames()
        {
            string[] fieldNames = GetAllFieldNames();
            OrderedDictionary<string, string> possibleNames = GetPossibleDeformationFieldOutputNamesMap();
            List<string> existingFieldNames = new List<string>();
            foreach (var entry in possibleNames)
            {
                if (fieldNames.Contains(entry.Value)) existingFieldNames.Add(entry.Key);
            }
            return existingFieldNames.ToArray();
        }
        private string[] GetDeformationFieldOutputComponentNames(string deformationFieldOutputName)
        {
            string[] componentNames = null;
            if (deformationFieldOutputName == FOFieldNames.Disp || deformationFieldOutputName == FOFieldNames.DispR)
            {
                componentNames = new string[] { FOComponentNames.U1, FOComponentNames.U2, FOComponentNames.U3 };
            }
            else if (deformationFieldOutputName == FOFieldNames.Forc)
            {
                componentNames = new string[] { FOComponentNames.F1, FOComponentNames.F2, FOComponentNames.F3 };
            }
            else if (deformationFieldOutputName == FOFieldNames.SurfaceNormal)
            {
                componentNames = new string[] { FOComponentNames.N1, FOComponentNames.N2, FOComponentNames.N3 };
            }
            else if (deformationFieldOutputName == FOFieldNames.WearDepth)
            {
                componentNames = new string[] { FOComponentNames.H1, FOComponentNames.H2, FOComponentNames.H3 };
            }
            else if (deformationFieldOutputName == FOFieldNames.MeshDeformation)
            {
                componentNames = new string[] { FOComponentNames.U1, FOComponentNames.U2, FOComponentNames.U3 };
            }
            else if (deformationFieldOutputName == FOFieldNames.DispDeformationDepth)
            {
                componentNames = new string[] { FOComponentNames.U1, FOComponentNames.U2, FOComponentNames.U3 };
            }
            return componentNames;
        }
        // Complex                                  
        public bool ContainsComplexResults()
        {
            foreach (var entry in _fields)
            {
                if (entry.Value.Complex) return true;
            }
            return false;
        }
        public void SetComplexResultTypeAndAngle(ComplexResultTypeEnum complexResultType, float complexAngleDeg,
                                                 FieldData fieldData = null)
        {
            if (complexResultType != _complexResultType ||
                (_complexResultType == ComplexResultTypeEnum.RealAtAngle && complexAngleDeg != _complexAngleDeg))
            {
                _complexResultType = complexResultType;
                _complexAngleDeg = complexAngleDeg;
                _complexResultChanged = true;
                // Compute the complex result
                SetComplexResult(fieldData);
                // If onlyThisField is used (animation) then also compute the deformations
                if (fieldData != null)
                {
                    string defFieldName = GetInternalDeformationFieldOutputName(fieldData.StepId, fieldData.StepIncrementId);
                    if (defFieldName != fieldData.Name)
                    {
                        fieldData = GetFieldData(defFieldName, null, fieldData.StepId, fieldData.StepIncrementId);
                        SetComplexResult(fieldData);
                    }
                }
            }
        }
        private int GetComplexResultTypeId()
        {
            if (_complexResultType == ComplexResultTypeEnum.Real) return 1;
            else if (_complexResultType == ComplexResultTypeEnum.Imaginary) return 2;
            else if (_complexResultType == ComplexResultTypeEnum.Magnitude) return 3;
            else if (_complexResultType == ComplexResultTypeEnum.Phase) return 4;
            else if (_complexResultType == ComplexResultTypeEnum.RealAtAngle) return -1;
            else if (_complexResultType == ComplexResultTypeEnum.Max) return 5;
            else if (_complexResultType == ComplexResultTypeEnum.AngleAtMax) return 6;
            else if (_complexResultType == ComplexResultTypeEnum.Min) return 7;
            else if (_complexResultType == ComplexResultTypeEnum.AngleAtMin) return 8;
            else throw new NotSupportedException();
        }
        public void PrepareComplexResults()
        {
            int stepId;
            int incrementId;
            int[] stepIds = GetAllStepIds();
            int[] incrementIds;
            float[] valuesR;
            float[] valuesI;
            float[] valuesMag;
            float[] valuesPha;
            FieldData fieldData;
            FieldData fieldDataR;
            FieldData fieldDataI;
            FieldData fieldDataMag;
            FieldData fieldDataPha;
            FieldData fieldDataMax;
            FieldData fieldDataMaxAng;
            FieldData fieldDataMin;
            FieldData fieldDataMinAng;
            Field field;
            Field fieldR;
            Field fieldI;
            Field fieldMag;
            Field fieldPha;
            Field fieldMax;
            Field fieldMaxAng;
            Field fieldMin;
            Field fieldMinAng;
            //
            for (int i = 0; i < stepIds.Length; i++)
            {
                stepId = stepIds[i];
                incrementIds = GetStepIncrementIds(stepId);
                for (int j = 0; j < incrementIds.Length; j++)
                {
                    incrementId = incrementIds[j];
                    //
                    foreach (var complexFieldNames in ComplexFieldNames)
                    {
                        fieldData = GetFieldData(complexFieldNames[0], null, stepId, incrementId);
                        field = GetField(fieldData, false);
                        fieldDataI = GetFieldData(complexFieldNames[2], null, stepId, incrementId);
                        fieldI = GetField(fieldDataI, false);
                        //
                        if (field != null && fieldI != null)
                        {
                            // Default
                            field.Complex = true;
                            ReplaceOrAddField(fieldData, field);
                            // Imaginary
                            fieldI.Complex = true;
                            fieldI.RemoveInvariants();
                            ReplaceOrAddField(fieldDataI, fieldI);
                            // Real                                             
                            fieldDataR = new FieldData(fieldData); // copy
                            fieldDataR.Name = complexFieldNames[1];
                            //
                            fieldR = new Field(field);  // copy
                            fieldR.Name = fieldDataR.Name;
                            //
                            ReplaceOrAddField(fieldDataR, fieldR);
                            // Magnitude                                        
                            fieldDataMag = new FieldData(fieldDataR);
                            fieldDataMag.Name = complexFieldNames[3];
                            //
                            fieldMag = new Field(fieldR);  // copy
                            fieldMag.Name = fieldDataMag.Name;
                            fieldMag.RemoveInvariants();
                            // Phase                                            
                            fieldDataPha = new FieldData(fieldDataR);
                            fieldDataPha.Name = complexFieldNames[4];
                            //
                            fieldPha = new Field(fieldR);  // copy
                            fieldPha.Name = fieldDataPha.Name;
                            fieldPha.RemoveInvariants();
                            // Max                                              
                            fieldDataMax = new FieldData(fieldData); // copy
                            fieldDataMax.Name = complexFieldNames[5];
                            //
                            fieldMax = new Field(field);  // copy
                            fieldMax.Name = fieldDataMax.Name;
                            fieldMax.DataState = DataStateEnum.UpdateComplexMinMax;
                            fieldMax.RemoveNonInvariants();
                            fieldMax.SetComponentValuesTo(-float.MaxValue);
                            //
                            ReplaceOrAddField(fieldDataMax, fieldMax);
                            // Max angle                                        
                            fieldDataMaxAng = new FieldData(fieldDataMax); // copy
                            fieldDataMaxAng.Name = complexFieldNames[6];
                            //
                            fieldMaxAng = new Field(fieldMax);  // copy
                            fieldMaxAng.Name = fieldDataMaxAng.Name;
                            fieldMaxAng.DataState = DataStateEnum.UpdateComplexMinMax;
                            fieldMaxAng.SetComponentValuesToZero();
                            //
                            ReplaceOrAddField(fieldDataMaxAng, fieldMaxAng);
                            // Min                                              
                            fieldDataMin = new FieldData(fieldData); // copy
                            fieldDataMin.Name = complexFieldNames[7];
                            //
                            fieldMin = new Field(field);  // copy
                            fieldMin.Name = fieldDataMin.Name;
                            fieldMin.DataState = DataStateEnum.UpdateComplexMinMax;
                            fieldMin.RemoveNonInvariants();
                            fieldMin.SetComponentValuesTo(float.MaxValue);
                            //
                            ReplaceOrAddField(fieldDataMin, fieldMin);
                            // Min angle                                        
                            fieldDataMinAng = new FieldData(fieldDataMin); // copy
                            fieldDataMinAng.Name = complexFieldNames[8];
                            //
                            fieldMinAng = new Field(fieldMin);  // copy
                            fieldMinAng.Name = fieldDataMinAng.Name;
                            fieldMinAng.DataState = DataStateEnum.UpdateComplexMinMax;
                            fieldMinAng.SetComponentValuesToZero();
                            //
                            ReplaceOrAddField(fieldDataMinAng, fieldMinAng);
                            // Compute magnitude and phase                      
                            string[] componentNames = fieldMag.GetComponentNames();
                            foreach (var componentName in componentNames)
                            {
                                if (!field.IsComponentInvariant(componentName))
                                {
                                    valuesR = fieldR.GetComponentValues(componentName);
                                    valuesI = fieldI.GetComponentValues(componentName);
                                    valuesMag = new float[valuesR.Length];
                                    valuesPha = new float[valuesR.Length];
                                    //
                                    for (int k = 0; k < valuesR.Length; k++)
                                    {
                                        valuesMag[k] = Tools.GetComplexMagnitude(valuesR[k], valuesI[k]);
                                        valuesPha[k] = Tools.GetComplexPhaseDeg(valuesR[k], valuesI[k]);
                                    }
                                    fieldMag.ReplaceComponent(componentName, new FieldComponent(componentName, valuesMag));
                                    fieldPha.ReplaceComponent(componentName, new FieldComponent(componentName, valuesPha));
                                }
                            }
                            //
                            ReplaceOrAddField(fieldDataMag, fieldMag);
                            ReplaceOrAddField(fieldDataPha, fieldPha);
                        }
                    }
                }
            }
        }
        public void RemoveComplexResults()
        {
            HashSet<string> fieldNamesToRemove = new HashSet<string>();
            for (int i = 0; i < ComplexFieldNames.Length; i++)
            {
                for (int j = 0; j < ComplexFieldNames[i].Length; j++)
                {
                    if (j != 0 && j != 2) fieldNamesToRemove.Add(ComplexFieldNames[i][j]);
                }
            }
            RemoveResultFieldOutputs(fieldNamesToRemove.ToArray());
        }
        public void SetComplexResult(FieldData onlyThisField)
        {
            if (_complexResultType == ComplexResultTypeEnum.RealAtAngle) SetComplexResultToAngle(onlyThisField);
            else SetToExistingComplexResult(onlyThisField);
            //
            SetResultFieldOutputLimitsToRecompute();
        }
        private void SetToExistingComplexResult(FieldData onlyThisField)
        {
            int stepId;
            int incrementId;
            int[] stepIds = GetAllStepIds();
            int[] incrementIds;
            FieldData fieldData;
            FieldData fieldDataExisting;
            Field field;
            Field fieldExisting;
            //
            int fieldNameId = GetComplexResultTypeId();
            //
            for (int i = 0; i < stepIds.Length; i++)
            {
                stepId = stepIds[i];
                if (onlyThisField != null && onlyThisField.StepId != stepId) continue;
                //
                incrementIds = GetStepIncrementIds(stepId);
                for (int j = 0; j < incrementIds.Length; j++)
                {
                    incrementId = incrementIds[j];
                    if (onlyThisField != null && onlyThisField.StepIncrementId != incrementId) continue;
                    //
                    foreach (var complexFieldNames in ComplexFieldNames)
                    {
                        if (onlyThisField != null && onlyThisField.Name != complexFieldNames[0]) continue;
                        //
                        fieldDataExisting = GetFieldData(complexFieldNames[fieldNameId], null, stepId, incrementId);
                        fieldExisting = GetField(fieldDataExisting, false);
                        //
                        if (fieldExisting != null)
                        {
                            fieldData = new FieldData(fieldDataExisting);
                            fieldData.Name = complexFieldNames[0];
                            //
                            field = new Field(fieldExisting);
                            field.Complex = true;
                            field.Name = complexFieldNames[0];
                            //
                            ReplaceOrAddField(fieldData, field);
                        }
                    }
                }
            }
        }
        public void SetComplexResultToAngle(FieldData onlyThisField)
        {
            int stepId;
            int incrementId;
            int[] stepIds = GetAllStepIds();
            int[] incrementIds;
            FieldData fieldData;
            FieldData fieldDataMag;
            FieldData fieldDataPha;
            Field field;
            Field fieldMag;
            Field fieldPha;
            float[] valuesMag;
            float[] valuesPha;
            float[] valuesAngle;
            //
            for (int i = 0; i < stepIds.Length; i++)
            {
                stepId = stepIds[i];
                if (onlyThisField != null && onlyThisField.StepId != stepId) continue;
                //
                incrementIds = GetStepIncrementIds(stepId);
                for (int j = 0; j < incrementIds.Length; j++)
                {
                    incrementId = incrementIds[j];
                    if (onlyThisField != null && onlyThisField.StepIncrementId != incrementId) continue;
                    //
                    foreach (var complexFieldNames in ComplexFieldNames)
                    {
                        if (onlyThisField != null && onlyThisField.Name != complexFieldNames[0]) continue;
                        //
                        fieldDataMag = GetFieldData(complexFieldNames[3], null, stepId, incrementId);
                        fieldMag = GetField(fieldDataMag, false);
                        fieldDataPha = GetFieldData(complexFieldNames[4], null, stepId, incrementId);
                        fieldPha = GetField(fieldDataPha, false);
                        //
                        if (fieldMag != null && fieldPha != null)
                        {
                            fieldData = new FieldData(fieldDataMag);
                            fieldData.Name = complexFieldNames[0];
                            //
                            field = new Field(fieldMag);
                            field.Name = complexFieldNames[0];
                            field.Complex = true;
                            //
                            string[] componentNames = field.GetComponentNames();
                            foreach (var componentName in componentNames)
                            {
                                if (!field.IsComponentInvariant(componentName))
                                {
                                    valuesMag = fieldMag.GetComponentValues(componentName);
                                    valuesPha = fieldPha.GetComponentValues(componentName);
                                    valuesAngle = new float[valuesMag.Length];
                                    //
                                    Parallel.For(0, valuesMag.Length, k =>
                                    //for (int k = 0; k < valuesMag.Length; k++)
                                    {
                                        valuesAngle[k] =
                                            Tools.GetComplexRealAtAngleFromMagAndPha(valuesMag[k], valuesPha[k], _complexAngleDeg);
                                    }
                                    );
                                    field.ReplaceComponent(componentName, new FieldComponent(componentName, valuesAngle));
                                }
                            }
                            field.ComputeInvariants();
                            //
                            ReplaceOrAddField(fieldData, field);
                        }
                    }
                }
            }
        }
        public void ComputeComplexMaxMin(FieldData onlyThisField)
        {
            if (onlyThisField == null) throw new NotSupportedException();
            //
            FieldData fieldData;
            FieldData fieldDataMag;
            FieldData fieldDataPha;
            FieldData fieldDataMax;
            FieldData fieldDataMaxAng;
            FieldData fieldDataMin;
            FieldData fieldDataMinAng;
            Field field;
            Field fieldMag;
            Field fieldPha;
            Field fieldMax;
            Field fieldMaxAng;
            Field fieldMin;
            Field fieldMinAng;
            //
            int stepId = onlyThisField.StepId;
            int incrementId = onlyThisField.StepIncrementId;
            string[] complexFieldNames;
            //
            for (int i = 0; i < ComplexFieldNames.Length; i++)
            {
                complexFieldNames = ComplexFieldNames[i];
                if (onlyThisField.Name == complexFieldNames[0])
                {                   
                    fieldDataMag = GetFieldData(complexFieldNames[3], null, stepId, incrementId);
                    fieldMag = GetField(fieldDataMag, false);
                    fieldDataPha = GetFieldData(complexFieldNames[4], null, stepId, incrementId);
                    fieldPha = GetField(fieldDataPha, false);
                    //
                    fieldDataMax = GetFieldData(complexFieldNames[5], null, stepId, incrementId);
                    fieldMax = GetField(fieldDataMax, false);
                    fieldDataMaxAng = GetFieldData(complexFieldNames[6], null, stepId, incrementId);
                    fieldMaxAng = GetField(fieldDataMaxAng, false);
                    fieldDataMin = GetFieldData(complexFieldNames[7], null, stepId, incrementId);
                    fieldMin = GetField(fieldDataMin, false);
                    fieldDataMinAng = GetFieldData(complexFieldNames[8], null, stepId, incrementId);
                    fieldMinAng = GetField(fieldDataMinAng, false);
                    //
                    if (fieldMag != null && fieldPha != null &&
                        fieldMax != null && fieldMaxAng != null && fieldMin != null && fieldMinAng != null)
                    {
                        if (fieldMax.GetComponentNames().Length > 0)
                        {
                            // Use field date with all components!!!
                            fieldData = GetFieldData(complexFieldNames[1], null, stepId, incrementId);  // take real
                            field = new Field(GetField(fieldData, false));                              // copy real
                            //
                            object myLock = new object();
                            int numOfAngles = 360;
                            float delta = 360f / numOfAngles;       // skip the final anlge which is the same as the first angle
                            Parallel.For(0, numOfAngles + 1, j =>
                            //for (int j = 0; j < numOfAngles; j++)
                            {
                                float[] valuesMag;
                                float[] valuesPha;
                                float[] valuesAngle;
                                //
                                float angle = j * delta;
                                Field fieldP = new Field(field);
                                //
                                string[] componentNames = fieldP.GetComponentNames();
                                foreach (var componentName in componentNames)
                                {
                                    if (!fieldP.IsComponentInvariant(componentName))
                                    {
                                        valuesMag = fieldMag.GetComponentValues(componentName);
                                        valuesPha = fieldPha.GetComponentValues(componentName);
                                        valuesAngle = new float[valuesMag.Length];
                                        //
                                        for (int k = 0; k < valuesMag.Length; k++)
                                        {
                                            valuesAngle[k] = Tools.GetComplexRealAtAngleFromMagAndPha(valuesMag[k],
                                                                                                      valuesPha[k], angle);
                                        }
                                        fieldP.ReplaceComponent(componentName, new FieldComponent(componentName, valuesAngle));
                                    }
                                }
                                fieldP.ComputeInvariants();
                                //
                                lock (myLock)
                                {
                                    Field.FindMax(fieldMax, fieldMaxAng, fieldP, angle);
                                    Field.FindMin(fieldMin, fieldMinAng, fieldP, angle);
                                }
                            }
                            );
                        }
                        //
                        fieldMax.DataState = DataStateEnum.OK;
                        fieldMaxAng.DataState = DataStateEnum.OK;
                        fieldMin.DataState = DataStateEnum.OK;
                        fieldMinAng.DataState = DataStateEnum.OK;
                        //
                        ReplaceOrAddField(fieldDataMax, fieldMax);
                        ReplaceOrAddField(fieldDataMaxAng, fieldMaxAng);
                        ReplaceOrAddField(fieldDataMin, fieldMin);
                        ReplaceOrAddField(fieldDataMinAng, fieldMinAng);
                    }
                }
                        
            }
        }
        // Units                                    
        public string GetFieldUnitAbbreviation(FieldData fieldData)
        {
            if (fieldData.Unit != null && fieldData.Unit.Length > 0) return fieldData.Unit;
            else
            {
                GetFieldUnitConverterAndAbbreviation(fieldData.Name, fieldData.Component, fieldData.StepId,
                                                    fieldData.StepIncrementId, out TypeConverter unitConverter,
                                                    out string unitAbbreviation);
                return unitAbbreviation;
            }
        }
        public string GetFieldUnitAbbreviation(string fieldDataName, string componentName, int stepId, int incrementId)
        {
            GetFieldUnitConverterAndAbbreviation(fieldDataName, componentName, stepId, incrementId,
                                                out TypeConverter unitConverter, out string unitAbbreviation);
            return unitAbbreviation;
        }
        public void GetFieldUnitConverterAndAbbreviation(string fieldDataName, string componentName,
                                                         int stepId, int incrementId,
                                                         out TypeConverter unitConverter,
                                                         out string unitAbbreviation)
        {
            if (_complexResultType == ComplexResultTypeEnum.Phase ||
                _complexResultType == ComplexResultTypeEnum.AngleAtMax ||
                _complexResultType == ComplexResultTypeEnum.AngleAtMin)  // speed up
            {
                FieldData fieldData = GetFieldData(fieldDataName, componentName, stepId, incrementId);
                if (fieldData.StepType == StepTypeEnum.SteadyStateDynamics)
                {
                    Field field = GetField(fieldData, false);
                    if (field.Complex)
                    {
                        unitConverter = new StringAngleDegConverter();
                        unitAbbreviation = StringAngleDegConverter.GetUnitAbbreviation();
                        return;
                    }
                }
            }
            //
            unitConverter = new DoubleConverter();
            unitAbbreviation = "?";
            try
            {
                switch (fieldDataName.ToUpper())
                {
                    case FOFieldNames.None:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "";
                        break;
                    case FOFieldNames.Disp:
                    case FOFieldNames.DispR:
                    case FOFieldNames.DispI:
                    case FOFieldNames.Distance: // Imported pressure
                        unitConverter = new StringLengthConverter();
                        unitAbbreviation = _unitSystem.LengthUnitAbbreviation;
                        break;
                    case FOFieldNames.PDisp:
                        switch (componentName.ToUpper())
                        {
                            case FOComponentNames.MAG1:
                            case FOComponentNames.MAG2:
                            case FOComponentNames.MAG3:
                                unitConverter = new StringLengthConverter();
                                unitAbbreviation = _unitSystem.LengthUnitAbbreviation;
                                break;
                            case FOComponentNames.PHA1:
                            case FOComponentNames.PHA2:
                            case FOComponentNames.PHA3:
                                unitConverter = new StringAngleDegConverter();
                                unitAbbreviation = StringAngleDegConverter.GetUnitAbbreviation();
                                break;
                        }
                        break;
                    case FOFieldNames.Velo:
                        unitConverter = new StringVelocityConverter();
                        unitAbbreviation = _unitSystem.VelocityUnitAbbreviation;
                        break;
                    case FOFieldNames.Stress:
                    case FOFieldNames.StressR:
                    case FOFieldNames.StressI:
                    case FOFieldNames.ZZStr:
                    case FOFieldNames.ZZStrR:
                    case FOFieldNames.ZZStrI:
                    case FOFieldNames.Imported: // Imported pressure
                        unitConverter = new StringPressureConverter();
                        unitAbbreviation = _unitSystem.PressureUnitAbbreviation;
                        break;
                    case FOFieldNames.PStress:
                        switch (componentName.ToUpper())
                        {
                            case FOComponentNames.MAGXX:
                            case FOComponentNames.MAGYY:
                            case FOComponentNames.MAGZZ:
                            case FOComponentNames.MAGXY:
                            case FOComponentNames.MAGYZ:
                            case FOComponentNames.MAGZX:
                                unitConverter = new StringPressureConverter();
                                unitAbbreviation = _unitSystem.PressureUnitAbbreviation;
                                break;
                            case FOComponentNames.PHAXX:
                            case FOComponentNames.PHAYY:
                            case FOComponentNames.PHAZZ:
                            case FOComponentNames.PHAXY:
                            case FOComponentNames.PHAYZ:
                            case FOComponentNames.PHAZX:
                                unitConverter = new StringAngleDegConverter();
                                unitAbbreviation = StringAngleDegConverter.GetUnitAbbreviation();
                                break;
                        }
                        break;
                    case FOFieldNames.ToStrain:
                    case FOFieldNames.ToStraiR:
                    case FOFieldNames.ToStraiI:
                    case FOFieldNames.MeStrain:
                    case FOFieldNames.MeStraiR:
                    case FOFieldNames.MeStraiI:
                    case FOFieldNames.Pe:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "/";
                        break;
                    case FOFieldNames.Forc:
                    case FOFieldNames.ForcR:
                    case FOFieldNames.ForcI:
                        unitConverter = new StringForceConverter();
                        unitAbbreviation = _unitSystem.ForceUnitAbbreviation;
                        break;
                    case FOFieldNames.Ener:
                        unitConverter = new StringEnergyPerVolumeConverter();
                        unitAbbreviation = _unitSystem.EnergyPerVolumeUnitAbbreviation;
                        break;
                    case FOFieldNames.Error:
                    case FOFieldNames.ErrorR:
                    case FOFieldNames.ErrorI:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "%";
                        break;
                    case FOFieldNames.Contact:
                        {
                            switch (componentName.ToUpper())
                            {
                                case FOComponentNames.COpen:
                                case FOComponentNames.CSlip1:
                                case FOComponentNames.CSlip2:
                                    unitConverter = new StringLengthConverter();
                                    unitAbbreviation = _unitSystem.LengthUnitAbbreviation;
                                    break;
                                case FOComponentNames.CPress:
                                case FOComponentNames.CShear1:
                                case FOComponentNames.CShear2:
                                    unitConverter = new StringPressureConverter();
                                    unitAbbreviation = _unitSystem.PressureUnitAbbreviation;
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    // Wear
                    case FOFieldNames.SlidingDistance:
                    case FOFieldNames.SurfaceNormal:
                    case FOFieldNames.WearDepth:
                    case FOFieldNames.MeshDeformation:
                    case FOFieldNames.DispDeformationDepth:
                        unitConverter = new StringLengthConverter();
                        unitAbbreviation = _unitSystem.LengthUnitAbbreviation;
                        break;
                    // Thermal
                    case FOFieldNames.NdTemp:
                        unitConverter = new StringTemperatureConverter();
                        unitAbbreviation = _unitSystem.TemperatureUnitAbbreviation;
                        break;
                    case FOFieldNames.Flux:
                        unitConverter = new StringPowerPerAreaConverter();
                        unitAbbreviation = _unitSystem.PowerPerAreaUnitAbbreviation;
                        break;
                    case FOFieldNames.Rfl:
                        unitConverter = new StringPowerConverter();
                        unitAbbreviation = _unitSystem.PowerUnitAbbreviation;
                        break;
                    case FOFieldNames.HError:
                    case FOFieldNames.HErrorI:
                    case FOFieldNames.HErrorR:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "%";
                        break;
                    // Sensitivity
                    case FOFieldNames.Norm:
                        unitConverter = new StringLengthConverter();
                        unitAbbreviation = _unitSystem.LengthUnitAbbreviation;
                        break;
                    case FOFieldNames.SenFreq:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "/";
                        break;
                    default:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "?";
                        // Limit
                        if (_resultFieldOutputs.TryGetValue(fieldDataName, out ResultFieldOutput rfo1) &&
                            rfo1 is ResultFieldOutputLimit rfol)
                        {
                            unitAbbreviation = "/";
                            break;
                        }
                        // Equation
                        else if (_resultFieldOutputs.TryGetValue(fieldDataName, out ResultFieldOutput rfo2) &&
                                 rfo2 is ResultFieldOutputEquation rfoeq)
                        {
                            unitAbbreviation = rfoeq.Unit;
                            break;
                        }
                        // OpenFOAM
                        if (stepId == 1 && incrementId == 0) unitAbbreviation = "/";    // User field outputs at 0 increment
                        else if (componentName.ToUpper() == "ALL" || componentName.ToUpper().StartsWith("VAL")) { }
                        else if (_unitSystem.UnitSystemType == UnitSystemType.UNIT_LESS) unitAbbreviation = "";
                        else if (Debugger.IsAttached) throw new NotSupportedException();
                        //
                        break;
                }
            }
            catch
            {
            }
        }
        //
        public string GetHistoryUnitAbbreviation(string fieldName, string componentName, int stepId, int incrementId)
        {
            GetHistoryUnitConverterAndAbbreviation(fieldName, componentName, stepId, incrementId ,
                                                  out TypeConverter unitConverter, out string unitAbbreviation);
            return unitAbbreviation;
        }
        public void GetHistoryUnitConverterAndAbbreviation(string fieldName, string componentName,
                                                          int stepId, int incrementId,
                                                          out TypeConverter unitConverter,
                                                          out string unitAbbreviation)
        {
            unitConverter = new DoubleConverter();
            unitAbbreviation = "?";
            string noSuffixName = HOFieldNames.GetNoSuffixName(fieldName);
            //
            try
            {
                switch (noSuffixName.ToUpper())
                {
                    case HOFieldNames.Time:
                        unitConverter = new StringTimeConverter();
                        unitAbbreviation = _unitSystem.TimeUnitAbbreviation;
                        break;
                    case HOFieldNames.Frequency:
                        unitConverter = new StringFrequencyConverter();
                        unitAbbreviation = _unitSystem.FrequencyUnitAbbreviation;
                        break;
                    case HOFieldNames.Buckling:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "/";
                        break;
                    case HOFieldNames.Displacements:
                    case HOFieldNames.RelativeContactDisplacement:
                    case HOFieldNames.CenterOgGravityCG:
                    case HOFieldNames.MeanSurfaceNormal:
                        if (componentName.ToUpper().StartsWith("UR"))
                        {
                            unitConverter = new StringAngleConverter();
                            unitAbbreviation = _unitSystem.AngleUnitAbbreviation;
                        }
                        else
                        {
                            unitConverter = new StringLengthConverter();
                            unitAbbreviation = _unitSystem.LengthUnitAbbreviation;
                        }
                        break;
                    case HOFieldNames.Velocities:
                        if (componentName.ToUpper().StartsWith("VR"))
                        {
                            unitConverter = new StringRotationalSpeedConverter();
                            unitAbbreviation = _unitSystem.RotationalSpeedUnitAbbreviation;
                        }
                        else
                        {
                            unitConverter = new StringVelocityConverter();
                            unitAbbreviation = _unitSystem.VelocityUnitAbbreviation;
                        }
                        break;
                    case HOFieldNames.SurfaceArea:
                        unitConverter = new StringAreaConverter();
                        unitAbbreviation = _unitSystem.AreaUnitAbbreviation;
                        break;
                    case HOFieldNames.Volume:
                    case HOFieldNames.TotalVolume:
                        unitConverter = new StringVolumeConverter();
                        unitAbbreviation = _unitSystem.VolumeUnitAbbreviation;
                        break;
                    case HOFieldNames.Forces:
                    case HOFieldNames.TotalForce:
                    case HOFieldNames.NormalSurfaceForce:
                    case HOFieldNames.ShearSurfaceForce:
                    case HOFieldNames.TotalSurfaceForce:
                        if (componentName.ToUpper().StartsWith("RM"))
                        {
                            unitConverter = new StringMomentConverter();
                            unitAbbreviation = _unitSystem.MomentUnitAbbreviation;
                        }
                        else
                        {
                            unitConverter = new StringForceConverter();
                            unitAbbreviation = _unitSystem.ForceUnitAbbreviation;
                        }
                        break;
                    case HOFieldNames.Stresses:
                    case HOFieldNames.ContactStress:
                        unitConverter = new StringPressureConverter();
                        unitAbbreviation = _unitSystem.PressureUnitAbbreviation;
                        break;
                    case HOFieldNames.Strains:
                    case HOFieldNames.MechanicalStrains:
                    case HOFieldNames.EquivalentPlasticStrains:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "/";
                        break;
                    case HOFieldNames.InternalEnergy:
                    case HOFieldNames.TotalInternalEnergy:
                    case HOFieldNames.ContactPrintEnergy:
                    case HOFieldNames.ContactSpringEnergy:
                        unitConverter = new StringEnergyConverter();
                        unitAbbreviation = _unitSystem.EnergyUnitAbbreviation;
                        break;
                    case HOFieldNames.InternalEnergyDensity:
                        unitConverter = new StringEnergyPerVolumeConverter();
                        unitAbbreviation = _unitSystem.EnergyPerVolumeUnitAbbreviation;
                        break;
                    case HOFieldNames.TotalNumberOfContactElements:
                        unitConverter = new DoubleConverter();
                        unitAbbreviation = "/";
                        break;
                    case HOFieldNames.MomentAboutOrigin:
                    case HOFieldNames.MomentAboutCG:
                        unitConverter = new StringMomentConverter();
                        unitAbbreviation = _unitSystem.MomentUnitAbbreviation;
                        break;
                    // Thermal
                    case HOFieldNames.Temperatures:
                        unitConverter = new StringTemperatureConverter();
                        unitAbbreviation = _unitSystem.TemperatureUnitAbbreviation;
                        break;
                    case HOFieldNames.HeatGeneration:
                    case HOFieldNames.TotalHeatGeneration:
                    case HOFieldNames.BodyHeating:
                    case HOFieldNames.TotalBodyHeating:
                        unitConverter = new StringPowerConverter();
                        unitAbbreviation = _unitSystem.PowerUnitAbbreviation;
                        break;
                    case HOFieldNames.HeatFlux:
                        unitConverter = new StringPowerPerAreaConverter();
                        unitAbbreviation = _unitSystem.PowerPerAreaUnitAbbreviation;
                        break;
                    // Error
                    case FOFieldNames.Error:
                    default:
                        string noSpacesName = noSuffixName.Replace(' ', '_');
                        GetFieldUnitConverterAndAbbreviation(noSpacesName.ToUpper(), componentName, stepId, incrementId,
                                                             out unitConverter, out unitAbbreviation);
                        if (unitAbbreviation == "?" && Debugger.IsAttached)
                            //throw new NotSupportedException();
                        break;
                        break;
                }
            }
            catch
            {
            }
        }
        //                                          
        public void CopyPartsFromMesh(FeMesh mesh)
        {
            _mesh.Parts.Clear();
            //
            ResultPart part;
            foreach (var entry in mesh.Parts)
            {
                part = new ResultPart(entry.Value);
                _mesh.Parts.Add(entry.Key, part);
                //
                foreach (var elementId in part.Labels) _mesh.Elements[elementId].PartId = part.PartId;
            }
        }
        public void CopyMeshItemsFromMesh(FeMesh mesh)
        {
            foreach (var entry in mesh.NodeSets)
            {
                _mesh.NodeSets.Add(entry.Key, entry.Value.DeepClone());
            }
            //
            string newName;
            FeElementSet newElementSet;
            foreach (var entry in mesh.ElementSets)
            {
                if (_mesh.ElementSets.ContainsKey(entry.Key))
                    newName = _mesh.ElementSets.GetNextNumberedKey(entry.Key);
                else newName = entry.Key;
                //
                newElementSet = entry.Value.DeepClone();
                newElementSet.Name = newName;
                //
                _mesh.ElementSets.Add(newElementSet.Name, newElementSet);
            }
            //
            foreach (var entry in mesh.Surfaces)
            {
                _mesh.Surfaces.Add(entry.Key, entry.Value.DeepClone());
            }
        }
        public void CopyFeatureItemsFromMesh(FeMesh mesh, int resultViewId)
        {
            FeReferencePoint referencePoint;
            foreach (var entry in mesh.ReferencePoints)
            {
                referencePoint = entry.Value.DeepClone();
                referencePoint.CreationData = null;
                referencePoint.CreationIds = null;
                referencePoint.CreatedFrom = FeReferencePointCreatedFrom.Coordinates;
                //
                _mesh.ReferencePoints.Add(entry.Key, referencePoint);
            }
            CoordinateSystem coordinateSystem;
            foreach (var entry in mesh.CoordinateSystems)
            {
                coordinateSystem = entry.Value.DeepClone();
                //
                coordinateSystem.CenterCreationData = null;
                coordinateSystem.CenterCreationIds = null;
                coordinateSystem.CenterCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                coordinateSystem.PointXCreationData = null;
                coordinateSystem.PointXCreationIds = null;
                coordinateSystem.PointXCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                coordinateSystem.PointXYCreationData = null;
                coordinateSystem.PointXYCreationIds = null;
                coordinateSystem.PointXYCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                //
                _mesh.CoordinateSystems.Add(entry.Key, coordinateSystem);
            }
        }
        //
        public void AddField(FieldData fieldData, Field field)
        {
            fieldData = new FieldData(fieldData);   // copy
            _fields.Add(fieldData, field);
            _fieldDataHashField.Add(fieldData.GetHashKey(), field);
        }
        public Field GetField(FieldData fieldData, bool update = true)
        {
            Field field;
            string hash = fieldData.GetHashKey();
            if (_fieldDataHashField.TryGetValue(hash, out field))
            {
                if (update)
                {
                    if (field.DataState == DataStateEnum.OK) { }
                    else if (field.DataState == DataStateEnum.UpdateComplexMinMax)
                    {
                        ComputeComplexMaxMin(fieldData);    // compute max/min
                        SetComplexResult(fieldData);        // set computed values to default field
                        //
                        if (_fieldDataHashField.TryGetValue(hash, out field))
                        {
                            return field;
                        }
                    }
                    else if (field.DataState == DataStateEnum.UpdateResultFieldOutput)
                    {
                        ComputeResultFieldOutput(fieldData);
                        //
                        if (_fieldDataHashField.TryGetValue(hash, out field))
                        {
                            return field;
                        }
                    }
                }
            }
            return field;
        }
        public void ReplaceOrAddField(FieldData fieldData, Field field)
        {
            // Find the result
            string hash = fieldData.GetHashKey();
            if (_fieldDataHashField.ContainsKey(hash))
            {
                foreach (var entry in _fields)
                {
                    if (entry.Key.Name.ToUpper() == fieldData.Name.ToUpper() &&
                        entry.Key.StepId == fieldData.StepId &&
                        entry.Key.StepIncrementId == fieldData.StepIncrementId)
                    {
                        _fields.Replace(entry.Key, fieldData, field);
                        _fieldDataHashField[hash] = field;
                        return;
                    }
                }
            }
            // New field
            else AddField(fieldData, field);
        }
        private void RemoveFields(string[] fieldOutputNames)
        {
            if (_fields != null)
            {
                List<FieldData> fieldsToRemove = new List<FieldData>();
                foreach (var entry in _fields)
                {
                    if (fieldOutputNames.Contains(entry.Key.Name)) fieldsToRemove.Add(entry.Key);
                }
                //
                foreach (var fieldToRemove in fieldsToRemove)
                {
                    _fields.Remove(fieldToRemove);
                    _fieldDataHashField.Remove(fieldToRemove.GetHashKey());
                }
            }
        }
        public void ComputeVisibleFieldInvariants()
        {
            Parallel.ForEach(_fields, entry =>
            //foreach (var entry in _fields)
            {
                if (FOFieldNames.IsVisible(entry.Key.Name)) entry.Value.ComputeInvariants();
            }
            );
        }
        //
        public string[] GetAllComponentNames()
        {
            HashSet<string> componentNames = new HashSet<string>();
            foreach (var entry in _fields)
            {
                componentNames.UnionWith(entry.Value.GetComponentNames());
            }
            return componentNames.ToArray();
        }
        public string[] GetFieldComponentNames(string fieldName)
        {
            foreach (var entry in _fields)
            {
                if (entry.Key.Name.ToUpper() == fieldName.ToUpper())
                {
                    return entry.Value.GetComponentNames();
                }
            }
            return null;
        }
        public FieldData[] GetAllFieldData()
        {
            return _fields.Keys.ToArray();
        }
        public FieldData GetFieldData(string name, string component, int stepId, int stepIncrementId, bool exactComponent = false)
        {
            FieldData result;
            // Empty results
            if (stepId == -1 && stepIncrementId == -1)
            {
                result = new FieldData(name, component, stepId, stepIncrementId);
                result.StepType = StepTypeEnum.Static;
                result.Valid = false;
                return result;
            }
            // Zero increment - Find all occurrences!!!
            else if (stepId == 1 && stepIncrementId == 0)
            {
                result = new FieldData(name, component, stepId, stepIncrementId);
                result.StepType = StepTypeEnum.Static;
                //result.Time = 0; // unnecessary since this result does not exits
                return result;
            }
            // Find the result
            foreach (var entry in _fields)
            {
                if (entry.Key.Name == name && entry.Key.StepId == stepId && entry.Key.StepIncrementId == stepIncrementId)
                {
                    if (exactComponent)
                    {
                        if (entry.Value.ContainsComponent(component))
                        {
                            result = new FieldData(entry.Key);
                            result.Component = component;
                            return result;
                        }
                    }
                    else
                    {
                        result = new FieldData(entry.Key);
                        result.Component = component;
                        return result;
                    }
                }
            }
            // Find other existing result
            foreach (var entry in _fields)
            {
                if (entry.Key.StepId == stepId && entry.Key.StepIncrementId == stepIncrementId)
                {
                    result = new FieldData(entry.Key);
                    result.Name = name;
                    result.Component = component;
                    result.Valid = false;
                    return result;
                }
            }
            // Nothing found
            result = new FieldData(name, component, stepId, stepIncrementId);
            result.StepType = StepTypeEnum.Static;
            result.Valid = false;
            return result;
        }
        public DataTypeEnum GetFieldDataType(string fieldName)
        {
            DataTypeEnum dataType = DataTypeEnum.None;
            foreach (var entry in _fields)
            {
                if (entry.Value.Name == fieldName)
                {
                    if (dataType == DataTypeEnum.None) dataType = entry.Value.DataType;
                    else if (dataType != entry.Value.DataType) return DataTypeEnum.None;
                }
            }
            return dataType;
        }
        public bool DoesFieldContainsAllNecessaryComponents(string fieldName)
        {
            foreach (var entry in _fields)
            {
                if (entry.Value.Name == fieldName)
                {
                    if (!entry.Value.ContainsAllNecessaryComponents()) return false;
                }
            }
            return true;
        }
        public FieldData GetFirstComponentOfTheFirstFieldAtLastIncrement()
        {
            string name = GetAllFieldNames()[0];
            string component = GetFieldComponentNames(name)[0];
            int stepId = GetAllStepIds().Last();
            int stepIncrementId = GetStepIncrementIds(stepId).Last();
            //
            return GetFieldData(name, component, stepId, stepIncrementId);
        }
        public FieldData GetFirstComponentOfTheFirstFieldAtDefaultIncrement()
        {
            string[] names = GetAllFieldNames();
            FieldData fieldData;
            if (names.Length == 0)
            {
                // There is no data
                fieldData = GetFieldData(FOFieldNames.None, FOComponentNames.None, -1, -1);
                fieldData.Valid = false;
            }
            else
            {
                int stepId = GetAllStepIds().Last();
                int stepIncrementId = GetStepIncrementIds(stepId).Last();
                string name = GetStepFieldNames(stepId)[0];
                string component = GetFieldComponentNames(name)[0];
                //
                fieldData = GetFieldData(name, component, stepId, stepIncrementId);
                if (fieldData == null)
                {
                    // There is no data
                    fieldData = GetFieldData(FOFieldNames.None, FOComponentNames.None, -1, -1);
                }
                else if (fieldData.StepType == StepTypeEnum.Frequency)
                {
                    stepIncrementId = GetStepIncrementIds(stepId).First();
                    fieldData = GetFieldData(name, component, stepId, stepIncrementId);
                }
            }
            return fieldData;
        }
        public string[] GetAllFieldNames()
        {
            // Use list for order
            HashSet<string> names = new HashSet<string>();
            foreach (var entry in _fields) names.Add(entry.Key.Name);
            return names.ToArray();
        }
        public string[] GetVisibleFieldNames()
        {
            // Use list for order
            HashSet<string> names = new HashSet<string>();
            foreach (var entry in _fields)
            {
                if (FOFieldNames.IsVisible(entry.Key.Name)) names.Add(entry.Key.Name);
            }
            return names.ToArray();
        }
        public NamedClass[] GetVisibleFieldsAsNamedItems()
        {
            string[] names = GetVisibleFieldNames();
            NamedClass[] fields = new NamedClass[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                fields[i] = new EmptyNamedClass(names[i]);
            }
            return fields;
        }
        public string[] GetStepFieldNames(int stepId)
        {
            List<string> names = new List<string>();
            foreach (var entry in _fields)
            {
                if (entry.Key.StepId == stepId && !names.Contains(entry.Key.Name))
                    names.Add(entry.Key.Name);
            }
            return names.ToArray();
        }
        public int[] GetAllStepIds()
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (var entry in _fields) ids.Add(entry.Key.StepId);
            //
            int[] sortedIds = ids.ToArray();
            Array.Sort(sortedIds);
            //
            return sortedIds;
        }
        public int[] GetStepIncrementIds(int stepId)
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (var entry in _fields)
            {
                if (entry.Key.StepId == stepId)
                {
                    if (entry.Key.StepType == StepTypeEnum.Static && stepId == 1 && ids.Count == 0)
                        ids.Add(0);   // Zero increment - Find all occurrences!!!
                    //
                    ids.Add(entry.Key.StepIncrementId);
                }
            }
            //
            int[] sortedIds = ids.ToArray();
            Array.Sort(sortedIds);
            //
            return sortedIds;
        }
        public Dictionary<int, int[]> GetExistingIncrementIds(string fieldName, string component,
                                                              int limitToStepId = -1, int limitTostepIncrementId = -1)
        {
            int[] stepIds;
            if (limitToStepId == -1) stepIds = GetAllStepIds();
            else stepIds = new int[] { limitToStepId };
            //
            Dictionary<int, int[]> existingIncrementIds = new Dictionary<int, int[]>();
            if (stepIds.Length == 0)
            {
                existingIncrementIds.Add(-1, new int[] { -1 });
            }
            else
            {
                List<int> stepIncrementIds;
                string fieldHash;
                HashSet<string> fieldHashes = GetAllFieldHashes();
                //
                foreach (int stepId in stepIds)
                {
                    stepIncrementIds = new List<int>();
                    foreach (int incrementId in GetStepIncrementIds(stepId))
                    {
                        if (limitTostepIncrementId != -1 && limitTostepIncrementId != incrementId) continue;
                        //
                        fieldHash = GetFieldHash(fieldName, component, stepId, incrementId);
                        //if (FieldExists(fieldName, component, stepId, incrementId)) stepIncrementIds.Add(incrementId);
                        if (fieldHashes.Contains(fieldHash)) stepIncrementIds.Add(incrementId);
                    }
                    existingIncrementIds.Add(stepId, stepIncrementIds.ToArray());
                }
            }
            return existingIncrementIds;
        }
        public Dictionary<int, int[]> GetAllExistingIncrementIds()
        {
            int[] stepIds = GetAllStepIds();
            Dictionary<int, int[]> existingIncrementIds = new Dictionary<int, int[]>();
            if (stepIds.Length == 0)
            {
                existingIncrementIds.Add(-1, new int[] { -1 });
            }
            else
            {
                if (!stepIds.Contains(1))
                {
                    existingIncrementIds.Add(1, new int[] { 0 });   // Zero increment - Find all occurrences!!!
                }
                foreach (int stepId in stepIds)
                {
                    existingIncrementIds.Add(stepId, GetStepIncrementIds(stepId));
                }
            }
            return existingIncrementIds;
        }
        public Dictionary<int, float> GetMaxStepTime()
        {
            int[] stepIds = GetAllStepIds();
            Dictionary<int, float> stepMaxTime = new Dictionary<int, float>();
            //
            if (stepIds.Length == 0)
            {
                stepMaxTime.Add(-1, -1);
            }
            else
            {
                foreach (var stepId in stepIds) stepMaxTime.Add(stepId, 0);
                if (!stepMaxTime.ContainsKey(0)) stepMaxTime.Add(0, 0); // Zero increment - Find all occurrences!!!
                //
                float time;
                foreach (var entry in _fields)
                {
                    stepMaxTime.TryGetValue(entry.Key.StepId, out time);
                    if (entry.Key.Time > time) stepMaxTime[entry.Key.StepId] = entry.Key.Time;
                }
            }
            return stepMaxTime;
        }
        public Dictionary<string, string[]> GetAllFiledNameComponentNames()
        {
            HashSet<string> componentNames;
            Dictionary<string, HashSet<string>> filedNameComponentNames = new Dictionary<string, HashSet<string>>();
            foreach (var entry in _fields)
            {
                if (filedNameComponentNames.TryGetValue(entry.Key.Name, out componentNames))
                    componentNames.UnionWith(entry.Value.GetComponentNames());
                else
                    filedNameComponentNames.Add(entry.Key.Name, new HashSet<string>(entry.Value.GetComponentNames()));
            }
            //
            Dictionary<string, string[]> filedNameComponentNamesArr = new Dictionary<string, string[]>();
            foreach (var entry in filedNameComponentNames)
            {
                filedNameComponentNamesArr.Add(entry.Key, entry.Value.ToArray());
            }
            return filedNameComponentNamesArr;
        }
        public Dictionary<string, string[]> GetAllVisibleFiledNameComponentNames()
        {
            HashSet<string> componentNames;
            Dictionary<string, HashSet<string>> filedNameComponentNames = new Dictionary<string, HashSet<string>>();
            foreach (var entry in _fields)
            {
                if (!FOFieldNames.IsVisible(entry.Key.Name)) continue;
                //
                if (filedNameComponentNames.TryGetValue(entry.Key.Name, out componentNames))
                    componentNames.UnionWith(entry.Value.GetComponentNames());
                else
                    filedNameComponentNames.Add(entry.Key.Name, new HashSet<string>(entry.Value.GetComponentNames()));
            }
            //
            Dictionary<string, string[]> filedNameComponentNamesArr = new Dictionary<string, string[]>();
            foreach (var entry in filedNameComponentNames)
            {
                filedNameComponentNamesArr.Add(entry.Key, entry.Value.ToArray());
            }
            return filedNameComponentNamesArr;
        }
        public bool FieldExists(string fieldName, string component, int stepId, int stepIncrementId)
        {
            foreach (var entry in _fields)
            {
                if ((entry.Key.Name.ToUpper() == fieldName.ToUpper() &&
                     entry.Value.ContainsComponent(component) &&
                     stepId == 1 && stepIncrementId == 0) // Zero increment - Find all occurrences!!!
                     ||
                    (entry.Key.Name.ToUpper() == fieldName.ToUpper() &&
                     entry.Value.ContainsComponent(component) &&
                     entry.Key.StepId == stepId &&
                     entry.Key.StepIncrementId == stepIncrementId))
                    return true;
            }
            return false;
        }
        private HashSet<string> GetAllFieldHashes()
        {
            HashSet<string> fieldHashes = new HashSet<string>();
            foreach (var fieldEntry in _fields)
            {
                foreach (var componentName in fieldEntry.Value.GetComponentNames())
                {
                    fieldHashes.Add(GetFieldHash(fieldEntry.Key.Name, componentName, 1, 0));
                    fieldHashes.Add(GetFieldHash(fieldEntry.Key.Name, componentName, fieldEntry.Key.StepId,
                                                 fieldEntry.Key.StepIncrementId));
                }
            }
            return fieldHashes;
        }
        private string GetFieldHash(string fieldName, string component, int stepId, int stepIncrementId)
        {
            return fieldName.ToUpper() + "_" + component.ToUpper() + "_" + stepId.ToString() + "_" + stepIncrementId.ToString();
        }
        //
        public float GetIncrementTime(int stepId, int stepIncrementId)
        {
            if (stepIncrementId == 0) return 0; // Zero increment - Find all occurrences!!!
            foreach (var entry in _fields)
            {
                if (entry.Key.StepId == stepId && entry.Key.StepIncrementId == stepIncrementId)
                {
                    return entry.Key.Time;
                }
            }
            return -1;
        }
        public float[] GetValues(FieldData fieldData, int[] globalNodeIds)
        {
            float[] values = null;
            bool zeroIncrement = false;
            //
            if (fieldData.Valid)
            {
                if (fieldData.StepIncrementId == 0)     // Zero increment - Find all occurrences!!!
                {
                    if (fieldData.StepId == 1)          // first step
                    {
                        values = new float[globalNodeIds.Length];
                        zeroIncrement = true;
                    }
                }
                //
                if (!zeroIncrement)
                {
                    Field field = GetField(fieldData);
                    //
                    float[] allValues = field.GetComponentValues(fieldData.Component);
                    if (allValues == null) return null; // the field became Valid = false during computation
                    //
                    values = new float[globalNodeIds.Length];
                    int globalId;
                    int localId;
                    for (int i = 0; i < globalNodeIds.Length; i++)
                    {
                        globalId = globalNodeIds[i];
                        if (_nodeIdsLookUp.TryGetValue(globalId, out localId) && localId >= 0 && localId < allValues.Length)
                            values[i] = allValues[localId];
                        else
                            return null;
                    }

                    //// Remove frst step
                    //fieldData.StepIncrementId = 1;
                    ////
                    //foreach (var entry in _fields)
                    //{
                    //    if (entry.Key.Name.ToUpper() == fieldData.Name.ToUpper() &&
                    //        entry.Key.StepId == fieldData.StepId &&
                    //        entry.Key.StepIncrementId == fieldData.StepIncrementId)
                    //    {
                    //        float[] allValues = entry.Value.GetComponentValues(fieldData.Component);
                    //        int globalId;
                    //        int localId;
                    //        for (int i = 0; i < globalNodeIds.Length; i++)
                    //        {
                    //            globalId = globalNodeIds[i];
                    //            if (_nodeIdsLookUp.TryGetValue(globalId, out localId) && localId >= 0 && localId < allValues.Length)
                    //                values[i] -= allValues[localId];
                    //            else
                    //                return null;
                    //        }
                    //        break;
                    //    }
                    //}
                }
            }
            return values;
        }
        public bool IsComponentInvariant(FieldData fieldData)
        {
            if (fieldData.StepIncrementId == 0)   // Zero increment - Find all occurrences!!!
            {
                if (fieldData.StepId == 1)        // first step
                    return false;
            }
            //
            foreach (var entry in _fields)
            {
                if (entry.Key.Name.ToUpper() == fieldData.Name.ToUpper() && 
                    entry.Key.StepId == fieldData.StepId && 
                    entry.Key.StepIncrementId == fieldData.StepIncrementId)
                {
                    return entry.Value.ContainsComponent(fieldData.Component) &&
                           entry.Value.IsComponentInvariant(fieldData.Component);
                }
            }
            return false;
        }
        public NodesExchangeData GetExtremeValues(string partName, FieldData fieldData)
        {
            return GetScaledExtremeValues(partName, fieldData, 0);
        }
        public NodesExchangeData GetScaledExtremeValues(string partName, FieldData fieldData, float relativeScale)
        {
            if (!fieldData.Valid) return null;
            //
            NodesExchangeData nodesData = new NodesExchangeData();
            nodesData.Ids = new int[2];
            nodesData.Coor = new double[2][];
            nodesData.Values = new float[2];
            int minId = -1;
            int maxId = -1;
            bool zeroIncrement = false;
            FeNode node;
            //
            if (fieldData.StepIncrementId == 0)     // Zero increment - Find all occurrences!!!
            {
                if (fieldData.StepId == 1)          // first step / zero increment
                {
                    minId = _nodeIdsLookUp.Keys.First();
                    maxId = minId;
                    nodesData.Ids[0] = minId;
                    nodesData.Ids[1] = maxId;
                    //
                    if (_mesh.Nodes.TryGetValue(minId, out node)) nodesData.Coor[0] = node.Coor;
                    else
                        nodesData.Coor[0] = new double[3];  // should not happen
                    //
                    if (_mesh.Nodes.TryGetValue(maxId, out node))nodesData.Coor[1] = node.Coor;
                    else
                        nodesData.Coor[1] = new double[3];  // should not happen
                    //
                    nodesData.Values[0] = 0;
                    nodesData.Values[1] = 0;
                    //
                    zeroIncrement = true;
                }
            }
            //
            if (!zeroIncrement)
            {                
                foreach (var fieldEntry in _fields)
                {
                    if (fieldEntry.Key.Name == fieldData.Name && fieldEntry.Key.StepId == fieldData.StepId &&
                        fieldEntry.Key.StepIncrementId == fieldData.StepIncrementId)
                    {
                        int id;
                        float value;
                        BasePart basePart;
                        bool firstNaN = true;
                        float[] values = fieldEntry.Value.GetComponentValues(fieldData.Component);
                        if (values == null) return null; // the field became Valid = false during computation
                        //
                        basePart = _mesh.Parts[partName];
                        // Initialize   
                        nodesData.Values[0] = float.MaxValue;
                        nodesData.Values[1] = -float.MaxValue;
                        // Get first value
                        if (_nodeIdsLookUp.TryGetValue(basePart.NodeLabels[0], out id) && id < values.Length)
                        {
                            value = values[id];
                            if (!float.IsNaN(value))
                            {
                                firstNaN = false;
                                nodesData.Values[0] = value;
                                nodesData.Values[1] = value;
                            }
                            minId = basePart.NodeLabels[0];
                            maxId = basePart.NodeLabels[0];
                        }
                        //
                        foreach (var nodeId in basePart.NodeLabels)
                        {
                            if (_nodeIdsLookUp.TryGetValue(nodeId, out id) && id < values.Length)
                            {
                                value = values[id];
                                if (!float.IsNaN(value))
                                {
                                    if (value < nodesData.Values[0])
                                    {
                                        nodesData.Values[0] = value;
                                        minId = nodeId;
                                    }
                                    else if (value > nodesData.Values[1])
                                    {
                                        nodesData.Values[1] = value;
                                        maxId = nodeId;
                                    }
                                }
                            }
                        }
                        //
                        if (relativeScale < 0)  // swap min and max
                        {
                            int tmp = minId;
                            minId = maxId;
                            maxId = tmp;
                            float tmpD = nodesData.Values[0];
                            nodesData.Values[0] = nodesData.Values[1];
                            nodesData.Values[1] = tmpD;
                        }
                        // Ids
                        nodesData.Ids[0] = minId;
                        nodesData.Ids[1] = maxId;
                        // Coordinates
                        nodesData.Coor[0] = _mesh.Nodes[minId].Coor;
                        nodesData.Coor[1] = _mesh.Nodes[maxId].Coor;
                        // Values
                        if (firstNaN && minId == maxId) // all values are NaN
                        {
                            nodesData.Values[0] = 0;
                            nodesData.Values[1] = 0;
                        }
                        else
                        {
                            nodesData.Values[0] *= relativeScale;
                            nodesData.Values[1] *= relativeScale;
                        }
                        //
                        break;
                    }
                }
            }
            return nodesData;
        }
        // Equations
        public void UpdateResultEquations()
        {
            FieldData sfFieldData;
            Field sfField;
            //
            Dictionary<int, int[]> stepIdIncrementIds = GetAllExistingIncrementIds();
            //
            foreach (var stepEntry in stepIdIncrementIds)
            {
                foreach (var incrementId in stepEntry.Value)
                {
                    foreach (var entry in _resultFieldOutputs)
                    {
                        if (entry.Value is ResultFieldOutputLimit rfol) { }
                        else if (entry.Value is ResultFieldOutputEnvelope rfoe) { }
                        else if (entry.Value is ResultFieldOutputCoordinateSystemTransform rfocst) { }
                        else if (entry.Value is ResultFieldOutputEquation rfoeq)
                        {
                            // Data
                            sfFieldData = GetFieldData(rfoeq.Name, ResultFieldOutputEquation.ComponentName,
                                                       stepEntry.Key, incrementId, true);
                            sfField = GetField(sfFieldData, false);
                            if (sfField != null) sfField.DataState = DataStateEnum.UpdateResultFieldOutput;
                        }
                        else if (Debugger.IsAttached)
                            throw new NotSupportedException();
                    }
                }
            }
            //
            foreach (var entry in _resultHistoryOutputs)
            {
                if (entry.Value is ResultHistoryOutputFromEquation rhofe)
                {
                    HistoryResultSet historyResultSet = GetHistorySetFromEquation(rhofe);
                    if (historyResultSet != null)
                    {
                        _history.Sets.Replace(rhofe.HistoryResultSet.Name, historyResultSet.Name, historyResultSet);
                        rhofe.HistoryResultSet = historyResultSet;
                    }
                }
            }
        }
        // Result field outputs                     
        public void AddResultFieldOutput(ResultFieldOutput resultFieldOutput)
        {
            _resultFieldOutputs.Add(resultFieldOutput.Name, resultFieldOutput);
            PrepareFieldsFromResultFieldOutput(resultFieldOutput);
        }
        private void PrepareFieldsFromResultFieldOutput(ResultFieldOutput resultFieldOutput)
        {
            FieldData fieldData;
            FieldData newFieldData = null;
            Field newField;
            //
            string newFieldName;
            string[] newComponentNames;
            string sourceFieldName;
            string sourceComponentName;
            string unit;
            //
            int numNodes = _mesh.Nodes.Count;
            //
            Dictionary<string, string[]> parameterNameFieldNameComponentName;
            Dictionary<int, int[]> stepIdIncrementIds = GetAllExistingIncrementIds();
            //
            foreach (var entry in stepIdIncrementIds)
            {
                foreach (var incrementId in entry.Value)
                {
                    if (entry.Key == 1 && incrementId == 0) continue;   // Zero increment - Find all occurrences!!!
                    //
                    if (resultFieldOutput is ResultFieldOutputLimit rfol)
                    {
                        sourceFieldName = rfol.FieldName;
                        sourceComponentName = rfol.ComponentName;
                        newFieldName = rfol.Name;
                        newComponentNames = rfol.GetComponentNames();
                        fieldData = GetFieldData(resultFieldOutput.Name, newComponentNames[0], entry.Key, incrementId, true);
                        unit = GetFieldUnitAbbreviation(fieldData);
                    }
                    else if (resultFieldOutput is ResultFieldOutputEnvelope rfoen)
                    {
                        // Get parent field
                        sourceFieldName = rfoen.FieldName;
                        sourceComponentName = rfoen.ComponentName;
                        newFieldName = rfoen.Name;
                        newComponentNames = rfoen.GetComponentNames();
                        fieldData = GetFieldData(sourceFieldName, sourceComponentName, entry.Key, incrementId, true);
                        unit = GetFieldUnitAbbreviation(fieldData);
                    }
                    else if (resultFieldOutput is ResultFieldOutputEquation rfoeq)
                    {
                        // Get parent field
                        CheckResultFieldOutputEquation(rfoeq.Equation, out _, out parameterNameFieldNameComponentName);
                        sourceFieldName = parameterNameFieldNameComponentName.First().Value[0];
                        sourceComponentName = parameterNameFieldNameComponentName.First().Value[1];
                        newFieldName = rfoeq.Name;
                        newComponentNames = rfoeq.GetComponentNames();
                        unit = rfoeq.Unit;
                    }
                    else if (resultFieldOutput is ResultFieldOutputCoordinateSystemTransform rfocst)
                    {
                        rfocst.SetComponentNames(GetFieldComponentNames(rfocst.FieldName));
                        //
                        sourceFieldName = rfocst.FieldName;
                        sourceComponentName = rfocst.GetComponentNames()[0];
                        newFieldName = rfocst.Name;
                        newComponentNames = rfocst.GetComponentNames();
                        fieldData = GetFieldData(sourceFieldName, sourceComponentName, entry.Key, incrementId, true);
                        unit = GetFieldUnitAbbreviation(fieldData);
                    }
                    else throw new NotSupportedException();
                    // Get parent field
                    fieldData = GetFieldData(sourceFieldName, sourceComponentName, entry.Key, incrementId, true);
                    // Prepare field
                    newField = new Field(newFieldName);
                    newField.DataState = DataStateEnum.UpdateResultFieldOutput;
                    //
                    foreach (var componentName in newComponentNames)
                    {
                        newFieldData = new FieldData(fieldData); // copy
                        newFieldData.Name = newFieldName;
                        newFieldData.Component = componentName;
                        newFieldData.Unit = unit;
                        newField.AddComponent(newFieldData.Component, new float[numNodes]);
                    }
                    AddField(newFieldData, newField);
                }
            }
        }
        public ResultFieldOutput GetResultFieldOutput(string resultFieldOutputName)
        {
            return _resultFieldOutputs[resultFieldOutputName];
        }
        public ResultFieldOutput[] GetResultFieldOutputs()
        {
            return _resultFieldOutputs.Values.ToArray();
        }
        public bool ContainsResultFieldOutput(string resultFieldOutputName)
        {
            return _resultFieldOutputs.ContainsKey(resultFieldOutputName);
        }
        public void ReplaceAllResultFieldOutputs()
        {
            foreach (var entry in _resultFieldOutputs.ToArray()) ReplaceResultFieldOutput(entry.Key, entry.Value);
        }
        public void ReplaceResultFieldOutput(string oldResultFieldOutputName, ResultFieldOutput resultFieldOutput)
        {
            RemoveFields(new string[] { oldResultFieldOutputName });
            _resultFieldOutputs.Replace(oldResultFieldOutputName, resultFieldOutput.Name, resultFieldOutput);
            PrepareFieldsFromResultFieldOutput(resultFieldOutput);
        }
        public void RemoveResultFieldOutputs(string[] fieldOutputNames)
        {
            RemoveFields(fieldOutputNames);
            foreach (var fieldOutputName in fieldOutputNames) _resultFieldOutputs.Remove(fieldOutputName);
        }
        public void RemoveResultFieldOutputComponents(string fieldOutputName, string[] componentNames)
        {
            if (_fields != null)
            {
                foreach (var entry in _fields)
                {
                    if (entry.Key.Name == fieldOutputName)
                    {
                        foreach (var componentName in componentNames)
                        {
                            entry.Value.RemoveComponent(componentName);
                        }
                    }
                }
            }
        }
        //
        private void SetResultFieldOutputLimitsToRecompute()
        {
            FieldData sfFieldData;
            Field sfField;
            ResultFieldOutput resultFieldOutput;
            //
            Dictionary<int, int[]> stepIdIncrementIds = GetAllExistingIncrementIds();
            //
            foreach (var stepEntry in stepIdIncrementIds)
            {
                foreach (var incrementId in stepEntry.Value)
                {
                    foreach (var entry in _resultFieldOutputs)
                    {
                        resultFieldOutput = entry.Value;
                        //
                        if (resultFieldOutput is ResultFieldOutputLimit rfol)
                        {
                            // Ratio
                            sfFieldData = GetFieldData(rfol.Name, FOComponentNames.Ratio, stepEntry.Key, incrementId, true);
                            sfField = GetField(sfFieldData, false);
                            if (sfField != null) sfField.DataState = DataStateEnum.UpdateResultFieldOutput;
                            // Safety factor
                            sfFieldData = GetFieldData(rfol.Name, FOComponentNames.SafetyFactor, stepEntry.Key,
                                                       incrementId, true);
                            sfField = GetField(sfFieldData, false);
                            if (sfField != null) sfField.DataState = DataStateEnum.UpdateResultFieldOutput;
                        }
                        else throw new NotSupportedException();
                    }
                }
            }
        }
        private void ComputeResultFieldOutput(FieldData fieldData)
        {
            List<ResultFieldOutput> independencyList = GetResultFieldOutputIndependencySequence();
            //
            foreach (var entry in _resultFieldOutputs)
            {
                if (fieldData.Name == entry.Value.Name && entry.Value.GetComponentNames().Contains(fieldData.Component))
                {
                    foreach (var indResultFieldOutput in independencyList)
                    {
                        ComputeFieldFromResultFieldOutput(indResultFieldOutput, fieldData.StepId, fieldData.StepIncrementId);
                    }
                }
            }
        }
        private void ComputeFieldFromResultFieldOutput(ResultFieldOutput resultFieldOutput, int stepId, int stepIncrementId)
        {
            FieldData sourceFieldData;
            Field sourceField = null;
            //
            int numOfNodes = _undeformedNodes.Count;
            string[] componentNames;
            float[][] values;
            //
            if (resultFieldOutput is ResultFieldOutputLimit rfol)
            {
                sourceFieldData = GetFieldData(rfol.FieldName, rfol.ComponentName, stepId, stepIncrementId, true);
                sourceField = GetField(sourceFieldData);
                componentNames = rfol.GetComponentNames();
                values = ComputeFieldFromResultFieldOutputLimit(rfol, sourceField);
            }
            else if (resultFieldOutput is ResultFieldOutputEnvelope rfoen)
            {
                componentNames = rfoen.GetComponentNames();
                values = ComputeFieldFromResultFieldOutputEnvelope(rfoen);
            }
            else if (resultFieldOutput is ResultFieldOutputEquation rfoeq)
            {
                componentNames = rfoeq.GetComponentNames();
                values = ComputeFieldFromResultFieldOutputEquation(rfoeq, stepId, stepIncrementId);
            }
            else if (resultFieldOutput is ResultFieldOutputCoordinateSystemTransform rfocst)
            {
                sourceFieldData = GetFieldData(rfocst.FieldName, rfocst.GetComponentNames()[0], stepId, stepIncrementId, true);
                sourceField = GetField(sourceFieldData);
                componentNames = rfocst.GetComponentNames();
                values = ComputeFieldFromResultFieldOutputCoordinateSystemTransform(rfocst, sourceField);
            }
            else throw new NotSupportedException();
            //
            int count = 0;
            FieldData newFieldData;
            Field newField;
            FieldComponent newComponent;
            foreach (var componentName in componentNames)
            {
                newFieldData = GetFieldData(resultFieldOutput.Name, componentName, stepId, stepIncrementId);
                newField = GetField(newFieldData, false);
                newComponent = new FieldComponent(newFieldData.Component, values[count++]);
                newField.ReplaceComponent(newComponent.Name, newComponent);
                newField.DataType = sourceField != null ? sourceField.DataType : DataTypeEnum.None;
                newField.DataState = DataStateEnum.OK;
                ReplaceOrAddField(newFieldData, newField);
            }
        }
        private float[][] ComputeFieldFromResultFieldOutputLimit(ResultFieldOutputLimit resultFieldOutput, Field sourceField)
        {
            int resultsNodeId;
            float itemLimit;
            float[] valuesSafety = null;
            float[] valuesRatio = null;
            float[] limits = null;
            //
            if (sourceField != null)
            {
                valuesRatio = sourceField.GetComponentValues(resultFieldOutput.ComponentName);
                if (valuesRatio != null)
                {
                    limits = new float[valuesRatio.Length];
                    //
                    if (resultFieldOutput.LimitPlotBasedOn == LimitPlotBasedOnEnum.Parts)
                    {
                        // Collect limits for parts
                        BasePart part;
                        foreach (var itemEntry in resultFieldOutput.ItemNameLimit)
                        {
                            if (_mesh.Parts.TryGetValue(itemEntry.Key, out part))
                            {
                                itemLimit = (float)itemEntry.Value;
                                if (itemLimit == 0) throw new NotSupportedException();
                                //
                                foreach (var nodeId in part.NodeLabels)
                                {
                                    resultsNodeId = _nodeIdsLookUp[nodeId];
                                    if (limits[resultsNodeId] != 0)
                                        limits[resultsNodeId] = Math.Min(limits[resultsNodeId], itemLimit);
                                    else limits[resultsNodeId] = itemLimit;
                                }
                            }
                        }
                    }
                    else if (resultFieldOutput.LimitPlotBasedOn == LimitPlotBasedOnEnum.ElementSets)
                    {
                        // Collect limits for element sets
                        FeElementSet elementSet;
                        foreach (var itemEntry in resultFieldOutput.ItemNameLimit)
                        {
                            if (_mesh.ElementSets.TryGetValue(itemEntry.Key, out elementSet) ||
                                itemEntry.Key == ResultFieldOutputLimit.AllElementsName)
                            {
                                if (itemEntry.Key == ResultFieldOutputLimit.AllElementsName)
                                    elementSet = new FeElementSet("tmp", _mesh.Elements.Keys.ToArray());
                                //
                                itemLimit = (float)itemEntry.Value;
                                if (itemLimit == 0) throw new NotSupportedException();
                                //
                                foreach (var nodeId in _mesh.GetNodeIdsFromElementSet(elementSet))
                                {
                                    resultsNodeId = _nodeIdsLookUp[nodeId];
                                    if (limits[resultsNodeId] != 0)
                                        limits[resultsNodeId] = Math.Min(limits[resultsNodeId], itemLimit);
                                    else limits[resultsNodeId] = itemLimit;
                                }
                            }
                        }
                    }
                    else throw new NotSupportedException();
                }
            }
            //
            if (valuesRatio != null)
            {
                valuesRatio = valuesRatio.ToArray(); // copy
                valuesSafety = valuesRatio.ToArray();
                // Compute values
                for (int i = 0; i < limits.Length; i++)
                {
                    // Ratio
                    valuesRatio[i] = valuesRatio[i] / limits[i];
                    // Safety factor
                    if (valuesSafety[i] == 0) valuesSafety[i] = float.NaN; // this can happen after some parts are renamed
                    else valuesSafety[i] = limits[i] / valuesSafety[i];
                }
            }
            //
            return new float[][] { valuesRatio, valuesSafety };
        }
        private float[][] ComputeFieldFromResultFieldOutputEnvelope(ResultFieldOutputEnvelope resultFieldOutput)
        {
            FieldData sourceFieldData;
            Field sourceField;
            //
            int stepId;
            int stepIncrementId;
            float[] values = null;
            float[] max = null;
            float[] min = null;
            float[] average = null;
            List<float[]> allValues = new List<float[]>();
            //
            Dictionary<int, int[]> stepIdIncrementId = GetAllExistingIncrementIds();
            foreach (var entry in stepIdIncrementId)
            {
                stepId = entry.Key;
                for (int i = 0; i < entry.Value.Length; i++)
                {
                    stepIncrementId = entry.Value[i];
                    sourceFieldData = GetFieldData(resultFieldOutput.FieldName, resultFieldOutput.ComponentName,
                                                   stepId, stepIncrementId, true);
                    sourceField = GetField(sourceFieldData);
                    //
                    if (sourceField != null)
                    {
                        values = sourceField.GetComponentValues(resultFieldOutput.ComponentName);
                        if (values != null) allValues.Add(values);
                    }
                }
            }
            //
            if (values.Count() > 0)
            {
                max = new float[values.Length];
                min = new float[values.Length];
                average = new float[values.Length];
                //
                for (int i = 0; i < values.Length; i++)
                {
                    max[i] = -float.MaxValue;
                    min[i] = float.MaxValue;
                }
                //
                foreach (var resultValues in allValues)
                {
                    for (int i = 0; i < resultValues.Length; i++)
                    {
                        if (resultValues[i] > max[i]) max[i] = resultValues[i];
                        if (resultValues[i] < min[i]) min[i] = resultValues[i];
                        average[i] += resultValues[i];
                    }
                }
                //
                for (int i = 0; i < average.Length; i++) average[i] /= allValues.Count();
            }
            //
            return new float[][] { max, min, average };
        }
        private float[][] ComputeFieldFromResultFieldOutputEquation(ResultFieldOutputEquation resultFieldOutput,
                                                                    int stepId, int stepIncrementId)
        {
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters.DeepCopy();
            try
            {
                FieldData sourceFieldData;
                Field sourceField;
                //
                int numValues = -1;
                float[] values = null;
                List<float[]> allValues = new List<float[]>();
                //
                Dictionary<string, string[]> parameterNameFieldNameComponentName;
                Dictionary<string, float[]> parameterNameValues = new Dictionary<string, float[]>();
                //
                CheckResultFieldOutputEquation(resultFieldOutput.Equation, out _, out parameterNameFieldNameComponentName);
                //
                string fieldName;
                string componentName;
                foreach (var parametreEntry in parameterNameFieldNameComponentName)
                {
                    fieldName = parametreEntry.Value[0];
                    componentName = parametreEntry.Value[1];
                    //
                    sourceFieldData = GetFieldData(fieldName, componentName, stepId, stepIncrementId, true);
                    sourceField = GetField(sourceFieldData);
                    //
                    if (sourceField != null)
                    {
                        values = sourceField.GetComponentValues(parametreEntry.Value[1]);
                        if (values != null)
                        {
                            numValues = values.Length;
                            parameterNameValues.Add(parametreEntry.Key, values);
                        }
                    }
                }
                //
                if (numValues < 0) return null;
                // Evaluate the equation
                foreach (var entry in parameterNameValues) MyNCalc.ExistingParameters[entry.Key] = entry.Value;
                values = MyNCalc.SolveArrayEquation(resultFieldOutput.Equation).ToFloat();
                //
                return new float[][] { values };
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MyNCalc.ExistingParameters = existingParameters;
            }
            
        }
        private float[][] ComputeFieldFromResultFieldOutputCoordinateSystemTransform(
            ResultFieldOutputCoordinateSystemTransform resultFieldOutput, Field sourceField)
        {
            float[][] values = null;
            CoordinateSystem coordinateSystem;
            _mesh.CoordinateSystems.TryGetValue(resultFieldOutput.CoordinateSystemName, out coordinateSystem);
            //
            if (sourceField != null && coordinateSystem != null)
            {
                int resultNodeId;
                int numOfComponents;
                string[] componentNames;
                float[][] sourceValues;
                double[] coor;
                Vec3D dx;
                Vec3D dy;
                Vec3D dz;
                Vec3D vector = new Vec3D();
                double[][] tensor;
                double[][] rotation;
                //
                FeNode node;
                Field resultField = new Field(sourceField);
                DataTypeEnum dataType = sourceField.DataType;
                // Remove invariants
                resultField.RemoveInvariants();
                // Prepare arrays
                componentNames = resultField.GetComponentNames();
                numOfComponents = componentNames.Length;
                values = new float[numOfComponents][];
                sourceValues = new float[numOfComponents][];
                // Get source values
                for (int i = 0; i < numOfComponents; i++)
                {
                    sourceValues[i] = resultField.GetComponentValues(componentNames[i]);
                    values[i] = new float[sourceValues[i].Length];
                }
                // Remove components
                for (int i = 0; i < numOfComponents; i++) resultField.RemoveComponent(componentNames[i]);
                // Scalar
                if (dataType == DataTypeEnum.Scalar)
                {
                }
                // Vector
                else if (dataType == DataTypeEnum.Vector)
                {
                    foreach (var entry in _undeformedNodes)
                    {
                        node = entry.Value;
                        resultNodeId = _nodeIdsLookUp[node.Id];
                        // Node coordinates
                        coor = node.Coor;
                        // Coordinate system directions at node
                        dx = coordinateSystem.DirectionX(coor);
                        dy = coordinateSystem.DirectionY(coor, dx); // using dx for speed
                        dz = coordinateSystem.DirectionZ(coor);
                        // Vector
                        vector.X = sourceValues[0][resultNodeId];
                        vector.Y = sourceValues[1][resultNodeId];
                        vector.Z = sourceValues[2][resultNodeId];
                        // Project vector on coordinate directions
                        values[0][resultNodeId] = (float)Vec3D.DotProduct(vector, dx);
                        values[1][resultNodeId] = (float)Vec3D.DotProduct(vector, dy);
                        values[2][resultNodeId] = (float)Vec3D.DotProduct(vector, dz);
                    }
                    // Add components
                    for (int i = 0; i < numOfComponents; i++) resultField.AddComponent(componentNames[i], values[i]);
                }
                // Tensor
                else if (dataType == DataTypeEnum.Tensor)
                {
                    tensor = Matrix.MatrixCreate(3, 3);
                    rotation = Matrix.MatrixCreate(3, 3);
                    foreach (var entry in _undeformedNodes)
                    {
                        node = entry.Value;
                        resultNodeId = _nodeIdsLookUp[node.Id];
                        // Node coordinates
                        coor = node.Coor;
                        // Coordinate system directions at node
                        dx = coordinateSystem.DirectionX(coor);
                        dy = coordinateSystem.DirectionY(coor, dx); // using dx for speed
                        dz = coordinateSystem.DirectionZ(coor);
                        // Tensor
                        tensor[0][0] = sourceValues[0][resultNodeId];
                        tensor[1][1] = sourceValues[1][resultNodeId];
                        tensor[2][2] = sourceValues[2][resultNodeId];
                        tensor[0][1] = sourceValues[3][resultNodeId];
                        tensor[1][0] = sourceValues[3][resultNodeId];
                        tensor[1][2] = sourceValues[4][resultNodeId];
                        tensor[2][1] = sourceValues[4][resultNodeId];
                        tensor[0][2] = sourceValues[5][resultNodeId];
                        tensor[2][0] = sourceValues[5][resultNodeId];
                        // Rotation matrix
                        rotation[0][0] = (float)dx.X;
                        rotation[0][1] = (float)dx.Y;
                        rotation[0][2] = (float)dx.Z;
                        rotation[1][0] = (float)dy.X;
                        rotation[1][1] = (float)dy.Y;
                        rotation[1][2] = (float)dy.Z;
                        rotation[2][0] = (float)dz.X;
                        rotation[2][1] = (float)dz.Y;
                        rotation[2][2] = (float)dz.Z;
                        // T' = R*T*RT - rotate tensor
                        tensor = Matrix.MatrixProduct(rotation, tensor);
                        rotation = Matrix.MatrixTranspose(rotation);
                        tensor = Matrix.MatrixProduct(tensor, rotation);
                        //
                        values[0][resultNodeId] = (float)tensor[0][0];
                        values[1][resultNodeId] = (float)tensor[1][1];
                        values[2][resultNodeId] = (float)tensor[2][2];
                        values[3][resultNodeId] = (float)tensor[0][1];
                        values[4][resultNodeId] = (float)tensor[1][2];
                        values[5][resultNodeId] = (float)tensor[2][0];
                    }

                    //Tensor - faster
                    //tensor = Matrix4x4.Identity;
                    //rotation = Matrix4x4.Identity;
                    //foreach (var entry in _undeformedNodes)
                    //{
                    //    node = entry.Value;
                    //    resultNodeId = _nodeIdsLookUp[node.Id];
                    //    // Node coordinates
                    //    coor = node.Coor;
                    //    // Coordinate system directions at node
                    //    dx = coordinateSystem.DirectionX(coor);
                    //    dy = coordinateSystem.DirectionY(coor, dx); // using dx for speed
                    //    dz = coordinateSystem.DirectionZ(coor);
                    //    // Tensor
                    //    tensor.M11 = sourceValues[0][resultNodeId];
                    //    tensor.M22 = sourceValues[1][resultNodeId];
                    //    tensor.M33 = sourceValues[2][resultNodeId];
                    //    tensor.M12 = sourceValues[3][resultNodeId];
                    //    tensor.M21 = sourceValues[3][resultNodeId];
                    //    tensor.M23 = sourceValues[4][resultNodeId];
                    //    tensor.M32 = sourceValues[4][resultNodeId];
                    //    tensor.M13 = sourceValues[5][resultNodeId];
                    //    tensor.M31 = sourceValues[5][resultNodeId];
                    //    // Rotation matrix
                    //    rotation.M11 = (float)dx.X;
                    //    rotation.M12 = (float)dx.Y;
                    //    rotation.M13 = (float)dx.Z;
                    //    rotation.M21 = (float)dy.X;
                    //    rotation.M22 = (float)dy.Y;
                    //    rotation.M23 = (float)dy.Z;
                    //    rotation.M31 = (float)dz.X;
                    //    rotation.M32 = (float)dz.Y;
                    //    rotation.M33 = (float)dz.Z;
                    //    //
                    //    //Matrix4x4.Invert(rotation, out rotationT);
                    //    rotationT = Matrix4x4.Transpose(rotation);
                    //    //
                    //    //tensor = Matrix4x4.Multiply(rotation, rotationT);
                    //    // T' = R*T*RT
                    //    tensor = Matrix4x4.Multiply(rotation, tensor);
                    //    tensor = Matrix4x4.Multiply(tensor, rotationT);
                    //    // Project vector on coordinate directions
                    //    values[0][resultNodeId] = tensor.M11;
                    //    values[1][resultNodeId] = tensor.M22;
                    //    values[2][resultNodeId] = tensor.M33;
                    //    values[3][resultNodeId] = tensor.M12;
                    //    values[4][resultNodeId] = tensor.M23;
                    //    values[5][resultNodeId] = tensor.M31;
                    //}


                    // Add components
                    for (int i = 0; i < numOfComponents; i++) resultField.AddComponent(componentNames[i], values[i]);
                }
                else throw new NotSupportedException();
                // Compute invariants
                resultField.ComputeInvariants();
                // Get all component values
                componentNames = resultField.GetComponentNames();
                numOfComponents = componentNames.Length;
                values = new float[numOfComponents][];
                for (int i = 0; i < numOfComponents; i++) values[i] = resultField.GetComponentValues(componentNames[i]);
            }
            //
            return values;
        }
        private List<ResultFieldOutput> GetResultFieldOutputIndependencySequence()
        {
            string[] parentFieldNames;
            HashSet<string> allNames = new HashSet<string>();
            ResultFieldOutput rfo;
            Node<string> node;
            Graph<string> dependencyGraph = new Graph<string>();
            Dictionary<string, Node<string>> nodes = new Dictionary<string, Node<string>>();
            //
            foreach (var entry in _resultFieldOutputs)
            {
                rfo = entry.Value;
                //
                allNames.Clear();
                allNames.Add(rfo.Name);
                parentFieldNames = rfo.GetParentNames();
                allNames.UnionWith(parentFieldNames);
                // Add nodes
                foreach (var name in allNames)
                {
                    if (!nodes.ContainsKey(name))
                    {
                        node = new Node<string>(name);
                        nodes.Add(name, node);
                        dependencyGraph.AddNode(node);
                    }
                }
                // Add connections
                foreach (var parentFieldName in parentFieldNames)
                {
                    dependencyGraph.AddDirectedEdge(nodes[rfo.Name], nodes[parentFieldName]);
                }
            }
            // Use only one sub-graph - NOT WORKING FOR DIRECTED GRAPHS
            //if (resultFieldOutput != null)
            //{
            //    List<Graph<string>> subGraphs = dependencyGraph.GetConnectedSubgraphs();
            //    foreach (var subGraph in subGraphs)
            //    {
            //        if (subGraph.Contains(resultFieldOutput.Name))
            //        {
            //            dependencyGraph = subGraph;
            //            break;
            //        }
            //    }
            //}
            //
            List<string> independencySequence = dependencyGraph.GetIndependencyList();
            //
            List<ResultFieldOutput> resultList = new List<ResultFieldOutput>();
            foreach (var name in independencySequence)
            {
                if (_resultFieldOutputs.TryGetValue(name, out rfo)) resultList.Add(rfo);
            }
            //
            return resultList;
        }
        // Equation
        public string[] GetPossibleFieldEquationParameters()
        {
            string variable;
            string separator = ResultFieldOutputEquation.EquationSeparator;
            List<string> possibleEquationParameters = new List<string>();
            //
            foreach (var entry in GetAllFiledNameComponentNames())
            {
                foreach (var componentName in entry.Value)
                {
                    variable = entry.Key + separator + componentName;
                    possibleEquationParameters.Add(variable);
                }
            }
            //
            return possibleEquationParameters.ToArray();
        }
        public string CheckResultFieldOutputEquation(string equation, out HashSet<string> parentNames,
            out Dictionary<string, string[]> parameterNameFieldNameComponentName)
        {
            parentNames = new HashSet<string>();
            parameterNameFieldNameComponentName = new Dictionary<string, string[]>();
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters.DeepCopy();
            //
            try
            {
                int count = 1;
                string[] possibleEquationParameters = GetPossibleFieldEquationParameters();
                foreach (var possibleParameter in possibleEquationParameters)
                {
                    MyNCalc.ExistingParameters[possibleParameter] = count++ * 0.111;
                }
                //
                HashSet<string> parameterNames;
                bool hasErrors = MyNCalc.HasErrors(equation, out parameterNames);
                //
                if (parameterNames == null || parameterNames.Count == 0 ||
                    parameterNames.Intersect(possibleEquationParameters).Count() == 0)
                    throw new CaeException("At least one filed output component must be used in the equation.");
                //
                string[] tmp;
                string[] splitter = new string[] { ResultFieldOutputEquation.EquationSeparator };
                //
                foreach (var parameterName in parameterNames)
                {
                    tmp = parameterName.Split(splitter, StringSplitOptions.None);
                    if (tmp.Length != 2) continue;
                    //
                    parentNames.Add(tmp[0]);
                    parameterNameFieldNameComponentName.Add(parameterName, new string[] { tmp[0], tmp[1] });
                }
                //
                if (parentNames.Count == 0)
                    throw new CaeException("The equation must contain at least one field output reference.");
                //
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                MyNCalc.ExistingParameters = existingParameters;
            }
        }
        //
        public bool AreResultFieldOutputsInCyclicDependance(string oldResultFieldOutputName, ResultFieldOutput resultFieldOutput)
        {
            Dictionary<string, ResultFieldOutput> resultFieldOutputs = new Dictionary<string, ResultFieldOutput>();
            foreach (var entry in _resultFieldOutputs)
            {
                if (entry.Key != oldResultFieldOutputName)
                    resultFieldOutputs.Add(entry.Key, entry.Value);
            }
            resultFieldOutputs.Add(resultFieldOutput.Name, resultFieldOutput);
            //
            return AreResultFieldOutputsInCyclicDependance(resultFieldOutputs);
        }
        private bool AreResultFieldOutputsInCyclicDependance(Dictionary<string, ResultFieldOutput> resultFieldOutputs)
        {
            int count = 0;
            bool result;
            foreach (var entry in resultFieldOutputs)
            {
                result = RecursiveCheck(ref count, entry.Value.Name, entry.Value.Name, resultFieldOutputs);
                if (result) return true;
            }
            return false;
        }
        private bool RecursiveCheck(ref int count, string firstName, string name,
                                    Dictionary<string, ResultFieldOutput> resultFieldOutputs)
        {
            if (count > 1000) return true;
            //
            ResultFieldOutput resultFieldOutput;
            if (resultFieldOutputs.TryGetValue(name, out resultFieldOutput))
            {
                string[] parents = resultFieldOutput.GetParentNames();
                if (parents == null) return false;
                //
                bool result;
                foreach (string parent in parents)
                {
                    if (parent == firstName) return true;
                    //
                    count++;
                    result = RecursiveCheck(ref count, firstName, parent, resultFieldOutputs);
                    if (result) return true;
                    count--;
                }
            }
            return false;
        }
        // History                                  
        public HistoryResults GetHistory()
        {
            return _history;
        }
        public void SetHistory(HistoryResults historyResults)
        {
            _history = historyResults;
        }
        
        //
        public void AddResultHistoryOutput(ResultHistoryOutput resultHistoryOutput)
        {
            // First try to create the historyResultSet to catch potential errors
            HistoryResultSet historyResultSet = GetHistorySetFromResultHistoryOutput(resultHistoryOutput);
            //
            if (historyResultSet != null)
            {
                // Add result output
                _resultHistoryOutputs.Add(resultHistoryOutput.Name, resultHistoryOutput);
                // Add set
                _history.Sets.Add(historyResultSet.Name, historyResultSet);
            }
        }
        public HistoryResultSet GetHistorySetFromResultHistoryOutput(ResultHistoryOutput resultHistoryOutput)
        {
            // Compatibility for version v1.2.1
            if (_history == null) _history = new HistoryResults("Tmp");
            //
            HistoryResultSet historyResultSet = null;
            //
            if (resultHistoryOutput is ResultHistoryOutputFromField rhoff)
            {
                historyResultSet = GetHistorySetFromField(rhoff);
            }
            else if (resultHistoryOutput is ResultHistoryOutputFromEquation rhofe)
            {
                historyResultSet = GetHistorySetFromEquation(rhofe);
            }
            //
            resultHistoryOutput.HistoryResultSet = historyResultSet;
            //
            return historyResultSet;
        }
        private HistoryResultSet GetHistorySetFromField(ResultHistoryOutputFromField resultHistoryOutput)
        {
            // Collect node ids
            int[] nodeIds = null;
            HistoryResultSet historyResultSet = null;
            //
            if (resultHistoryOutput.RegionType == RegionTypeEnum.NodeSetName)
            {
                nodeIds = _mesh.NodeSets[resultHistoryOutput.RegionName].Labels;
            }
            else if (resultHistoryOutput.RegionType == RegionTypeEnum.SurfaceName)
            {
                string nodeSetName = _mesh.Surfaces[resultHistoryOutput.RegionName].NodeSetName;
                nodeIds = _mesh.NodeSets[nodeSetName].Labels;
            }
            else if (resultHistoryOutput.RegionType == RegionTypeEnum.Selection)
            {
                nodeIds = resultHistoryOutput.CreationIds;
            }
            //
            if (nodeIds != null)
            {
                HistoryResultField historyResultField = new HistoryResultField(resultHistoryOutput.FieldName);
                // Create history result components
                List<HistoryResultComponent> components = new List<HistoryResultComponent>();
                foreach (var componentName in resultHistoryOutput.ComponentNames)
                    components.Add(new HistoryResultComponent(componentName));
                // Prepare component entries
                string name;
                foreach (var newComponent in components)
                {
                    newComponent.Entries.Clear();
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        name = nodeIds[i].ToString();
                        newComponent.Entries.Add(name, new HistoryResultEntries(name, false));
                    }
                }
                // Get all existing increments
                Dictionary<int, int[]> existingStepIncrementIds = GetExistingIncrementIds(resultHistoryOutput.FieldName,
                    components[0].Name, resultHistoryOutput.StepId, resultHistoryOutput.StepIncrementId);
                //
                string unit;
                float[] values;
                int resultNodeId;
                Field field;
                FieldData fieldData;
                foreach (var component in components)
                {
                    if (resultHistoryOutput.Harmonic) GetHarmonicFromHistoryOutput(nodeIds, resultHistoryOutput, component);
                    else
                    {
                        // Set complex
                        ComplexResultTypeEnum prevComplexResultType = _complexResultType;
                        float prevComplexAngleDeg = _complexAngleDeg;
                        SetComplexResultTypeAndAngle(resultHistoryOutput.ComplexResultType, (float)resultHistoryOutput.ComplexAngleDeg);
                        //
                        foreach (var existingStepEntry in existingStepIncrementIds)
                        {
                            foreach (var incrementId in existingStepEntry.Value)
                            {
                                fieldData = GetFieldData(resultHistoryOutput.FieldName, component.Name,
                                                         existingStepEntry.Key, incrementId);
                                field = GetField(fieldData);
                                // Filter complex components only to complex field outputs
                                if (resultHistoryOutput.ComplexResultType != ComplexResultTypeEnum.Real && !field.Complex) continue;
                                //
                                if (field != null)
                                {
                                    values = field.GetComponentValues(component.Name);
                                    if (values != null)
                                    {
                                        for (int i = 0; i < nodeIds.Length; i++)
                                        {
                                            name = nodeIds[i].ToString();
                                            resultNodeId = _nodeIdsLookUp[nodeIds[i]];
                                            component.Entries[name].Add(fieldData.Time, values[resultNodeId]);
                                        }
                                    }
                                }
                            }
                        }
                        // Reset complex
                        SetComplexResultTypeAndAngle(prevComplexResultType, prevComplexAngleDeg);
                        // Set phase unit
                        if (resultHistoryOutput.ComplexResultType == ComplexResultTypeEnum.Phase ||
                            resultHistoryOutput.ComplexResultType == ComplexResultTypeEnum.AngleAtMax ||
                            resultHistoryOutput.ComplexResultType == ComplexResultTypeEnum.AngleAtMin)
                        {
                            unit = StringAngleDegConverter.GetUnitAbbreviation();
                        }
                        else
                        {
                            GetHistoryUnitConverterAndAbbreviation(resultHistoryOutput.FieldName, component.Name, 0, 0,
                                                                   out _, out unit);
                        }
                        component.Unit = unit;
                    }
                    // Filter
                    if (resultHistoryOutput.Filter1 != null) component.ApplyFilter(resultHistoryOutput.Filter1);
                    if (resultHistoryOutput.Filter2 != null) component.ApplyFilter(resultHistoryOutput.Filter2);
                    // Add component to field
                    historyResultField.Components.Add(component.Name, component);
                }
                //
                historyResultSet = new HistoryResultSet(resultHistoryOutput.Name);
                historyResultSet.Harmonic = resultHistoryOutput.Harmonic;
                historyResultSet.Fields.Add(historyResultField.Name, historyResultField);
                //
                if (resultHistoryOutput.OutputNodeCoordinates != OutputNodeCoordinatesEnum.Off)
                {
                    HistoryResultField coordinatesField = GetNodeCoordinatesHistoryResultField(nodeIds, 
                        resultHistoryOutput.OutputNodeCoordinates, existingStepIncrementIds);
                    historyResultSet.Fields.Add(coordinatesField.Name, coordinatesField);
                }
            }
            //
            return historyResultSet;
        }
        private HistoryResultSet GetHistorySetFromEquation(ResultHistoryOutputFromEquation resultHistoryOutput)
        {
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters.DeepCopy();
            //
            try
            {
                HashSet<string> parentNames;
                Dictionary<string, HistoryResultComponent> parameterNameHistoryComponent;
                CheckResultHistoryOutputEquation(resultHistoryOutput.Equation, out parentNames, out parameterNameHistoryComponent);
                resultHistoryOutput.SetParentNames(parentNames.ToArray());
                //
                double[][] values;
                List<double> time = parameterNameHistoryComponent.First().Value.Entries.First().Value.Time;
                int numCol = parameterNameHistoryComponent.First().Value.Entries.Count();
                int numRow = parameterNameHistoryComponent.First().Value.Entries.First().Value.Values.Count();
                Dictionary<string, double[]> parameterNameValues = new Dictionary<string, double[]>();
                //
                bool entryNamesEqual = true;
                string[] entryNames = null;
                string[] currentEntryNames = null;
                //
                if (parameterNameHistoryComponent.Count > 0)
                {
                    foreach (var componentEntry in parameterNameHistoryComponent)
                    {
                        values = componentEntry.Value.GetAllValues();
                        // Check if components contain data for the same entry names
                        if (entryNames == null) entryNames = componentEntry.Value.Entries.Keys.ToArray();
                        if (entryNamesEqual)
                        {
                            currentEntryNames = componentEntry.Value.Entries.Keys.ToArray();
                            if (entryNames.Length != currentEntryNames.Length) entryNamesEqual = false;
                            if (entryNamesEqual)
                            {
                                for (int i = 0; i < entryNames.Length; i++)
                                {
                                    if (entryNames[i] != currentEntryNames[i]) { entryNamesEqual = false; break; }
                                }
                            }
                        }
                        //
                        parameterNameValues.Add(componentEntry.Key, values.ToFlatArray());
                    }
                }
                // Evaluate the array equation
                foreach (var entry in parameterNameValues) MyNCalc.ExistingParameters[entry.Key] = entry.Value;
                values = MyNCalc.SolveArrayEquation(resultHistoryOutput.Equation).ToJaggedArray(numRow);
                // Create result history set
                HistoryResultEntries historyResultEntries;
                HistoryResultComponent historyResultComponent =
                    new HistoryResultComponent(ResultHistoryOutputFromEquation.ComponentName);
                historyResultComponent.Unit = resultHistoryOutput.Unit;
                // Entry names
                if (!entryNamesEqual)
                {
                    for (int i = 0; i < numCol; i++) entryNames[i] = (i + 1).ToString();
                }
                //
                for (int j = 0; j < numCol; j++)
                {
                    historyResultEntries = new HistoryResultEntries(entryNames[j], false);
                    // Set time
                    historyResultEntries.Time = time.ToList();
                    // Set values
                    for (int i = 0; i < numRow; i++) historyResultEntries.Values.Add(values[i][j]);
                    //
                    historyResultComponent.Entries.Add(historyResultEntries.Name, historyResultEntries);
                }
                // Filter
                if (resultHistoryOutput.Filter1 != null) historyResultComponent.ApplyFilter(resultHistoryOutput.Filter1);
                if (resultHistoryOutput.Filter2 != null) historyResultComponent.ApplyFilter(resultHistoryOutput.Filter2);
                // Field
                HistoryResultField historyResultField = new HistoryResultField(ResultHistoryOutputFromEquation.FieldName);
                historyResultField.Components.Add(historyResultComponent.Name, historyResultComponent);
                // Set
                HistoryResultSet historyResultSet = new HistoryResultSet(resultHistoryOutput.Name);
                historyResultSet.Fields.Add(historyResultField.Name, historyResultField);
                //
                return historyResultSet;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                MyNCalc.ExistingParameters = existingParameters;
            }
        }
        private HistoryResultField GetNodeCoordinatesHistoryResultField(int[] nodeIds, OutputNodeCoordinatesEnum nodeCoorType,
                                                                        Dictionary<int, int[]> existingStepIncrementIds)
        {
            if (nodeIds != null && nodeCoorType != OutputNodeCoordinatesEnum.Off)
            {
                HistoryResultComponent componentX = new HistoryResultComponent(HOComponentNames.X);
                HistoryResultComponent componentY = new HistoryResultComponent(HOComponentNames.Y);
                HistoryResultComponent componentZ = new HistoryResultComponent(HOComponentNames.Z);
                // Units
                componentX.Unit = StringLengthConverter.GetUnitAbbreviation();
                componentY.Unit = StringLengthConverter.GetUnitAbbreviation();
                componentZ.Unit = StringLengthConverter.GetUnitAbbreviation();
                // Add components for the node coordinates
                List<HistoryResultComponent> allComponents = new List<HistoryResultComponent>();
                allComponents.Add(componentX);
                allComponents.Add(componentY);
                allComponents.Add(componentZ);
                // Add entries for each node id to the empty components
                string name;
                foreach (var newComponent in allComponents)
                {
                    newComponent.Entries.Clear();
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        name = nodeIds[i].ToString();
                        newComponent.Entries.Add(name, new HistoryResultEntries(name, false));
                    }
                }
                foreach (var existingStepEntry in existingStepIncrementIds)
                {
                    foreach (var incrementId in existingStepEntry.Value)
                    {
                        // Get node coordinates
                        GetNodeCoordinates(nodeIds, existingStepEntry.Key, incrementId, nodeCoorType,
                                           componentX, componentY, componentZ);
                    }
                }
                //
                HistoryResultField historyResultFieldCoor = new HistoryResultField(HOFieldNames.Coordinates);
                historyResultFieldCoor.Components.Add(componentX.Name, componentX);
                historyResultFieldCoor.Components.Add(componentY.Name, componentY);
                historyResultFieldCoor.Components.Add(componentZ.Name, componentZ);
                //
                return historyResultFieldCoor;
            }
            return null;
        }
        private void GetNodeCoordinates(int[] nodeIds, int stepId, int incrementId, OutputNodeCoordinatesEnum nodeCoorType,
                                        HistoryResultComponent x, HistoryResultComponent y, HistoryResultComponent z)
        {
            int resultNodeId;
            string name;
            float[] valuesX;
            float[] valuesY;
            float[] valuesZ;
            FeNode resultNode;
            FieldData fieldData = GetFieldData(FOFieldNames.Disp, "", stepId, incrementId);
            Field field = GetField(fieldData);
            //
            if (field != null)
            {
                valuesX = field.GetComponentValues(FOComponentNames.U1);
                valuesY = field.GetComponentValues(FOComponentNames.U2);
                valuesZ = field.GetComponentValues(FOComponentNames.U3);
                if (valuesX != null && valuesY != null && valuesZ != null)
                {
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        name = nodeIds[i].ToString();
                        resultNodeId = _nodeIdsLookUp[nodeIds[i]];
                        resultNode = _undeformedNodes[nodeIds[i]];
                        //
                        if (nodeCoorType == OutputNodeCoordinatesEnum.Undeformed)
                        {
                            x.Entries[name].Add(fieldData.Time, resultNode.X);
                            y.Entries[name].Add(fieldData.Time, resultNode.Y);
                            z.Entries[name].Add(fieldData.Time, resultNode.Z);
                        }
                        else if (nodeCoorType == OutputNodeCoordinatesEnum.Deformed)
                        {
                            x.Entries[name].Add(fieldData.Time, valuesX[resultNodeId] + resultNode.X);
                            y.Entries[name].Add(fieldData.Time, valuesY[resultNodeId] + resultNode.Y);
                            z.Entries[name].Add(fieldData.Time, valuesZ[resultNodeId] + resultNode.Z);
                        }
                    }
                }
            }
            else
            {
                if (!(stepId == 1 && incrementId == 0)) // Zero increment - Find all occurrences!!!
                {
                    // Return undeformed coordinates if no field is found
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        name = nodeIds[i].ToString();
                        resultNode = _undeformedNodes[nodeIds[i]];
                        x.Entries[name].Add(fieldData.Time, resultNode.X);
                        y.Entries[name].Add(fieldData.Time, resultNode.Y);
                        z.Entries[name].Add(fieldData.Time, resultNode.Z);
                    }
                }
            }
        }
        public void GetHarmonicFromHistoryOutput(int[] nodeIds, ResultHistoryOutputFromField rhoff,
                                                 HistoryResultComponent historyResultComponent)
        {
            int resultNodeId;
            string name;
            float[] values;
            Field field;
            FieldData fieldData = GetFieldData(rhoff.FieldName, historyResultComponent.Name, rhoff.StepId, rhoff.StepIncrementId);
            // Set complex
            ComplexResultTypeEnum prevComplexResultType = _complexResultType;
            float prevComplexAngleDeg = _complexAngleDeg;
            //
            for (int i = 0; i <= 360; i++)
            {
                SetComplexResultTypeAndAngle(ComplexResultTypeEnum.RealAtAngle, i, fieldData);
                //
                field = GetField(fieldData);
                // Filter complex components only to complex field outputs
                if (rhoff.ComplexResultType != ComplexResultTypeEnum.Real && !field.Complex) continue;
                //
                if (field != null)
                {
                    values = field.GetComponentValues(historyResultComponent.Name);
                    if (values != null)
                    {
                        for (int j = 0; j < nodeIds.Length; j++)
                        {
                            name = nodeIds[j].ToString();
                            resultNodeId = _nodeIdsLookUp[nodeIds[j]];
                            historyResultComponent.Entries[name].Add(i, values[resultNodeId]);
                        }
                    }
                }
            }
            // Reset complex
            SetComplexResultTypeAndAngle(prevComplexResultType, prevComplexAngleDeg);
        }
        //
        public ResultHistoryOutput[] GetResultHistoryOutputs()
        {
            return _resultHistoryOutputs.Values.ToArray();
        }
        public ResultHistoryOutput GetResultHistoryOutput(string resultHistoryOutputName)
        {
            return _resultHistoryOutputs[resultHistoryOutputName];
        }
        public HistoryResultSet GetHistoryResultSet(string setName)
        {
            HistoryResultSet set = null;
            //
            foreach (var setEntry in _history.Sets)
            {
                if (setEntry.Key.ToUpper() == setName.ToUpper())
                {
                    set = setEntry.Value;
                    break;
                }
            }
            //
            return set;
        }
        public HistoryResultField GetHistoryResultField(string setName, string fieldName)
        {
            HistoryResultSet set = GetHistoryResultSet(setName);
            HistoryResultField field = null;
            //
            if (set != null)
            {
                foreach (var fieldEntry in set.Fields)
                {
                    if (fieldEntry.Key.ToUpper() == fieldName.ToUpper())
                    {
                        field = fieldEntry.Value;
                        break;
                    }
                }
            }
            //
            return field;
        }
        public HistoryResultComponent GetHistoryResultComponent(string setName, string fieldName, string componentName)
        {
            HistoryResultField field = GetHistoryResultField(setName, fieldName);
            HistoryResultComponent component = null;
            //
            if (field != null)
            {
                foreach (var componentEntry in field.Components)
                {
                    if (componentEntry.Key.ToUpper() == componentName.ToUpper())
                    {
                        component = componentEntry.Value;
                        break;
                    }
                }
            }
            //
            return component;
        }
        public HistoryResultEntries GetHistoryResultEntry(string setName, string fieldName, string componentName, string entryName)
        {
            HistoryResultComponent component = GetHistoryResultComponent(setName, fieldName, componentName);
            HistoryResultEntries historyResultEntry = null;
            //
            if (component != null)
            {
                foreach (var entry in component.Entries)
                {
                    if (entry.Key.ToUpper() == entryName.ToUpper())
                    {
                        historyResultEntry = entry.Value;
                        break;
                    }
                }
            }
            //
            return historyResultEntry;
        }
        public NamedClass[] GetHistoryOutputsAsNamedItems()
        {
            string[] names = _history.Sets.Keys.ToArray();
            NamedClass[] historyOutputs = new NamedClass[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                historyOutputs[i] = new EmptyNamedClass(names[i]);
            }
            return historyOutputs;
        }
        public void GetHistoryOutputData(HistoryResultData historyData, out string[] columnNames, out object[][] rowBasedData,
                                         bool forCsv)
        {
            HistoryResultSet set = _history.Sets[historyData.SetName];
            HistoryResultField field = set.Fields[historyData.FieldName];
            HistoryResultComponent component = field.Components[historyData.ComponentName];
            string unit;
            if (component.Unit != null) unit = component.Unit;
            else unit = GetHistoryUnitAbbreviation(field.Name, component.Name, -1, -1);
            string frequencyColumnName = PrepareFrequencyUnits(component);
            //
            string timeUnit = "[" + GetHistoryUnitAbbreviation("Time", null, -1, -1) + "]";
            string frequencyUnit = "[" + GetHistoryUnitAbbreviation("Frequency", null, -1, -1) + "]";
            string angleUnit = "[" + StringAngleDegConverter.GetUnitAbbreviation() + "]";
            string delimiter;
            if (forCsv) delimiter = " ";
            else delimiter = Environment.NewLine;
            // Sorted time
            double[] sortedTime;
            Dictionary<double, int> timeRowId;
            // Do not sort the time points
            GetSortedTime(new HistoryResultComponent[] { component }, out sortedTime, out timeRowId, false);
            // Create the data array
            int numRow = sortedTime.Length;
            int numCol = component.Entries.Count + 1; // +1 for the time column
            columnNames = new string[numCol];
            rowBasedData = new object[numRow][];
            // Create rows
            for (int i = 0; i < numRow; i++) rowBasedData[i] = new object[numCol];
            // Add time column name
            if (frequencyColumnName != null) columnNames[0] = frequencyColumnName;
            else if (set.Harmonic) columnNames[0] = "Angle " + angleUnit;
            else columnNames[0] = "Time " + timeUnit + delimiter + "Frequency " + frequencyUnit;
            // Fill the data array
            for (int i = 0; i < sortedTime.Length; i++) rowBasedData[i][0] = sortedTime[i];
            // Add data column
            int col = 1;
            int row;
            double[] timePoints;
            double[] values;
            string entryUnit;
            foreach (var entry in component.Entries)
            {
                columnNames[col] = entry.Key;
                // Entry unit
                if (entry.Value.Unit != null) entryUnit = entry.Value.Unit;
                else entryUnit = unit;
                columnNames[col] += delimiter + "[" + entryUnit + "]";
                // Local
                if (entry.Value.Local) columnNames[col] += delimiter + "Local";
                //
                row = 0;
                timePoints = entry.Value.Time.ToArray();
                values = entry.Value.Values.ToArray();
                for (int i = 0; i < timePoints.Length; i++)
                {
                    row = timeRowId[timePoints[i]];
                    rowBasedData[row][col] = values[i];
                }
                col++;
            }
        }
        private string PrepareFrequencyUnits(HistoryResultComponent component)
        {
            string columnName = null;
            // Frequency data units
            if (component.Name == HOFieldNames.EigenvalueOutput)
            {
                foreach (var entry in component.Entries)
                {
                    if (entry.Key == HOComponentNames.EIGENVALUE) entry.Value.Unit = _unitSystem.EigenvalueUnitAbbreviation;
                    else if (entry.Key == HOComponentNames.OMEGA) entry.Value.Unit = _unitSystem.RotationalSpeedUnitAbbreviation;
                    else if (entry.Key == HOComponentNames.FREQUENCY) entry.Value.Unit = _unitSystem.FrequencyUnitAbbreviation;
                    else if (entry.Key == HOComponentNames.FREQUENCY_IM) entry.Value.Unit = _unitSystem.RotationalSpeedUnitAbbreviation;
                    else throw new NotSupportedException();
                }
                columnName = "Mode";
            }
            else if (component.Name == HOFieldNames.ParticipationFactors)
            {
                foreach (var entry in component.Entries)
                {
                    if (entry.Key == HOComponentNames.XCOMPONENT ||
                        entry.Key == HOComponentNames.YCOMPONENT ||
                        entry.Key == HOComponentNames.ZCOMPONENT)
                    {
                        entry.Value.Unit = "/";
                    }
                    else if (entry.Key == HOComponentNames.XROTATION ||
                             entry.Key == HOComponentNames.YROTATION ||
                             entry.Key == HOComponentNames.ZROTATION)
                    {
                        entry.Value.Unit = "/";
                    }
                }
                columnName = "Mode";
            }
            else if (component.Name == HOFieldNames.EffectiveModalMass)
            {
                foreach (var entry in component.Entries)
                {
                    if (entry.Key == HOComponentNames.XCOMPONENT ||
                        entry.Key == HOComponentNames.YCOMPONENT ||
                        entry.Key == HOComponentNames.ZCOMPONENT)
                    {
                        entry.Value.Unit = _unitSystem.MassUnitAbbreviation;
                    }
                    else if (entry.Key == HOComponentNames.XROTATION ||
                             entry.Key == HOComponentNames.YROTATION ||
                             entry.Key == HOComponentNames.ZROTATION)
                    {
                        entry.Value.Unit = _unitSystem.MassUnitAbbreviation;
                    }
                }
                columnName = "Mode";
            }
            else if (component.Name == HOFieldNames.TotalEffectiveModalMass || component.Name == HOFieldNames.TotalEffectiveMass)
            {
                foreach (var entry in component.Entries)
                {
                    if (entry.Key == HOComponentNames.XCOMPONENT ||
                        entry.Key == HOComponentNames.YCOMPONENT ||
                        entry.Key == HOComponentNames.ZCOMPONENT)
                    {
                        entry.Value.Unit = _unitSystem.MassUnitAbbreviation;
                    }
                    else if (entry.Key == HOComponentNames.XROTATION ||
                              entry.Key == HOComponentNames.YROTATION ||
                              entry.Key == HOComponentNames.ZROTATION)
                    {
                        entry.Value.Unit = _unitSystem.MassUnitAbbreviation;
                    }
                }
                columnName = "Total";
            }
            else if (component.Name == HOFieldNames.RelativeEffectiveModalMass)
            {
                foreach (var entry in component.Entries)
                {
                    if (entry.Key == HOComponentNames.XCOMPONENT ||
                        entry.Key == HOComponentNames.YCOMPONENT ||
                        entry.Key == HOComponentNames.ZCOMPONENT)
                    {
                        entry.Value.Unit = "/";
                    }
                    else if (entry.Key == HOComponentNames.XROTATION ||
                             entry.Key == HOComponentNames.YROTATION ||
                             entry.Key == HOComponentNames.ZROTATION)
                    {
                        entry.Value.Unit = "/";
                    }
                }
                columnName = "Mode";
            }
            else if (component.Name == HOFieldNames.RelativeTotalEffectiveModalMass)
            {
                foreach (var entry in component.Entries)
                {
                    if (entry.Key == HOComponentNames.XCOMPONENT ||
                        entry.Key == HOComponentNames.YCOMPONENT ||
                        entry.Key == HOComponentNames.ZCOMPONENT)
                    {
                        entry.Value.Unit = "/";
                    }
                    else if (entry.Key == HOComponentNames.XROTATION ||
                             entry.Key == HOComponentNames.YROTATION ||
                             entry.Key == HOComponentNames.ZROTATION)
                    {
                        entry.Value.Unit = "/";
                    }
                }
                columnName = "Total";
            }
            return columnName;
        }
        public void GetSortedTime(HistoryResultComponent[] components, out double[] sortedTime,
                                  out Dictionary<double, int> timeRowId, bool sort = true)
        {
            // Collect all time points
            HashSet<double> timePointsHash = new HashSet<double>();
            foreach (var component in components)
            {
                foreach (var entry in component.Entries)
                {
                    foreach (var time in entry.Value.Time) timePointsHash.Add(time);
                }
            }
            // Sort time points
            sortedTime = timePointsHash.ToArray();
            if (sort) Array.Sort(sortedTime);
            // Create a map of time point vs row id
            timeRowId = new Dictionary<double, int>();
            for (int i = 0; i < sortedTime.Length; i++) timeRowId.Add(sortedTime[i], i);
        }
        public bool ContainsResultHistoryOutput(string resultHistoryOutputName)
        {
            return _resultHistoryOutputs.ContainsKey(resultHistoryOutputName);
        }
        public void ReplaceResultHistoryOutput(string oldResultHistoryOutputName, ResultHistoryOutput resultHistoryOutput)
        {
            // First try to create the historyResultSet to catch potential errors
            HistoryResultSet historyResultSet = GetHistorySetFromResultHistoryOutput(resultHistoryOutput);
            //
            if (historyResultSet != null)
            {
                // Add result output
                _resultHistoryOutputs.Replace(oldResultHistoryOutputName, resultHistoryOutput.Name, resultHistoryOutput);
                // Add set
                _history.Sets.Replace(oldResultHistoryOutputName, historyResultSet.Name, historyResultSet);
            }
        }
        public void RemoveResultHistoryOutputs(string[] resultHistoryOutputNames)
        {
            RemoveResultHistoryResultSets(resultHistoryOutputNames);
            foreach (var historyOutputName in resultHistoryOutputNames) _resultHistoryOutputs.Remove(historyOutputName);
        }
        public void RemoveResultHistoryResultSets(string[] historyResultSetNames)
        {
            if (_history != null)
            {
                List<string> setsToRemove = new List<string>();
                foreach (var entry in _history.Sets)
                {
                    if (historyResultSetNames.Contains(entry.Key)) setsToRemove.Add(entry.Key);
                }
                //
                foreach (var setToRemove in setsToRemove)
                {
                    _history.Sets.Remove(setToRemove);
                }
            }
        }
        public void RemoveResultHistoryResultFields(string historyResultSetName, string[] historyResultFieldNames)
        {
            if (_history != null)
            {
                List<string> fieldsToRemove = new List<string>();
                foreach (var entry in _history.Sets[historyResultSetName].Fields)
                {
                    if (historyResultFieldNames.Contains(entry.Key)) fieldsToRemove.Add(entry.Key);
                }
                //
                foreach (var fieldToRemove in fieldsToRemove)
                {
                    _history.Sets[historyResultSetName].Fields.Remove(fieldToRemove);
                }
            }
        }
        public void RemoveResultHistoryResultComponents(string historyResultSetName,
                                                        string historyResultFieldName,
                                                        string[] historyResultComponentNames)
        {
            if (_history != null)
            {
                List<string> componentsToRemove = new List<string>();
                foreach (var entry in _history.Sets[historyResultSetName].Fields[historyResultFieldName].Components)
                {
                    if (historyResultComponentNames.Contains(entry.Key)) componentsToRemove.Add(entry.Key);
                }
                //
                foreach (var componentToRemove in componentsToRemove)
                {
                    _history.Sets[historyResultSetName].Fields[historyResultFieldName].Components.Remove(componentToRemove);
                }
            }
        }
        private List<ResultHistoryOutput> GetResultHistoryOutputIndependencySequence()
        {
            string[] parentHistoryNames;
            HashSet<string> allNames = new HashSet<string>();
            ResultHistoryOutput rho;
            Node<string> node;
            Graph<string> dependencyGraph = new Graph<string>();
            Dictionary<string, Node<string>> nodes = new Dictionary<string, Node<string>>();
            //
            foreach (var entry in _resultHistoryOutputs)
            {
                rho = entry.Value;
                //
                allNames.Clear();
                allNames.Add(rho.Name);
                parentHistoryNames = rho.GetParentNames();
                allNames.UnionWith(parentHistoryNames);
                // Add nodes
                foreach (var name in allNames)
                {
                    if (!nodes.ContainsKey(name))
                    {
                        node = new Node<string>(name);
                        nodes.Add(name, node);
                        dependencyGraph.AddNode(node);
                    }
                }
                // Add connections
                foreach (var parentHistoryName in parentHistoryNames)
                {
                    dependencyGraph.AddDirectedEdge(nodes[rho.Name], nodes[parentHistoryName]);
                }
            }
            // Use only one sub-graph - NOT WORKING FOR DIRECTED GRAPHS
            //if (resultHistoryOutput != null)
            //{
            //    List<Graph<string>> subGraphs = dependencyGraph.GetConnectedSubgraphs();
            //    foreach (var subGraph in subGraphs)
            //    {
            //        if (subGraph.Contains(resultHistoryOutput.Name))
            //        {
            //            dependencyGraph = subGraph;
            //            break;
            //        }
            //    }
            //}
            //
            List<string> independencySequence = dependencyGraph.GetIndependencyList();
            //
            List<ResultHistoryOutput> resultList = new List<ResultHistoryOutput>();
            foreach (var name in independencySequence)
            {
                if (_resultHistoryOutputs.TryGetValue(name, out rho)) resultList.Add(rho);
            }
            //
            return resultList;
        }
        // Equation
        public string[] GetPossibleHistoryEquationParameters()
        {
            string variable;
            string separator = ResultHistoryOutputFromEquation.EquationSeparator;
            List<string> possibleEquationParameters = new List<string>();
            //
            foreach (var setEntry in _history.Sets)
            {
                foreach (var fieldEntry in setEntry.Value.Fields)
                {
                    foreach (var componentEntry in fieldEntry.Value.Components)
                    {
                        variable = setEntry.Key + separator + fieldEntry.Key + separator + componentEntry.Key;
                        possibleEquationParameters.Add(variable);
                    }
                }
            }
            //
            return possibleEquationParameters.ToArray();
        }
        public string CheckResultHistoryOutputEquation(string equation, out HashSet<string> parentNames,
            out Dictionary<string, HistoryResultComponent> parameterNameHistoryComponent)
        {
            parentNames = new HashSet<string>();
            parameterNameHistoryComponent = new Dictionary<string, HistoryResultComponent>();
            OrderedDictionary<string, object> existingParameters = MyNCalc.ExistingParameters.DeepCopy();
            //
            try
            {
                int count = 1;
                string[] possibleEquationParameters = GetPossibleHistoryEquationParameters();
                foreach (var possibleParameter in possibleEquationParameters)
                {
                    MyNCalc.ExistingParameters[possibleParameter] = count++ * 0.111;
                }
                //
                HashSet<string> parameterNames;
                bool hasErrors = MyNCalc.HasErrors(equation, out parameterNames);
                //
                if (parameterNames == null || parameterNames.Count == 0 ||
                    parameterNames.Intersect(possibleEquationParameters).Count() == 0)
                    throw new CaeException("At least one history output component must be used in the equation.");
                //
                string[] tmp;
                string[] splitter = new string[] { ResultHistoryOutputFromEquation.EquationSeparator };
                HistoryResultSet historyResultSet;
                HistoryResultField historyResultField;
                HistoryResultComponent historyResultComponent;
                HistoryResultEntries historyResultEntry;
                //
                int numRows = -1;
                int numColumns = -1;
                //int numIncrements = -1;
                double[][] values;
                //HashSet<string> entryNames = null;
                foreach (var parameterName in parameterNames)
                {
                    tmp = parameterName.Split(splitter, StringSplitOptions.None);
                    if (tmp.Length != 3) continue;
                    //
                    if (!_history.Sets.TryGetValue(tmp[0], out historyResultSet))
                        throw new CaeException("The history output " + tmp[0] + " does not exist.");
                    if (!historyResultSet.Fields.TryGetValue(tmp[1], out historyResultField))
                        throw new CaeException("The field " + tmp[1] + " of the history output " + tmp[0] + " does not exist.");
                    if (!historyResultField.Components.TryGetValue(tmp[2], out historyResultComponent))
                        throw new CaeException("The component " + tmp[2] + " of the history output " + tmp[0] + " does not exist.");
                    //
                    values = historyResultComponent.GetAllValues();
                    if (numRows == -1) numRows = values.Length;
                    if (numColumns == -1) numColumns = values[0].Length;
                    //
                    if (numRows != values.Length)
                        throw new CaeException("All history output components used in the equation must contain the same number of " +
                                               "time increments (rows).");
                    //
                    if (numColumns != values[0].Length)
                        throw new CaeException("All history output components used in the equation must contain the same number of " +
                                               "items (columns).");
                    //
                    //if (entryNames == null) entryNames = new HashSet<string>(historyResultComponent.Entries.Keys);
                    //else if (entryNames.Union(historyResultComponent.Entries.Keys).Count() != entryNames.Count)
                    //    throw new CaeException("All selected history output components must contain the same items.");
                    //
                    //historyResultEntry = historyResultComponent.Entries.First().Value;
                    ////
                    //if (numIncrements == -1) numIncrements = historyResultEntry.Values.Count();
                    //else if (numIncrements != historyResultEntry.Values.Count())
                    //    throw new CaeException("All selected history output components must contain the same number " +
                    //                           "of time increments.");
                    //
                    parentNames.Add(tmp[0]);
                    parameterNameHistoryComponent.Add(parameterName, historyResultComponent);
                }
                //
                if (parentNames.Count == 0)
                    throw new CaeException("The equation must contain at least one history output reference.");
                //
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                MyNCalc.ExistingParameters = existingParameters;
            }
        }
        //
        public bool AreResultHistoryOutputsInCyclicDependance(string oldResultHistoryOutputName,
                                                              ResultHistoryOutput resultHistoryOutput)
        {
            Dictionary<string, ResultHistoryOutput> resultHistoryOutputs = new Dictionary<string, ResultHistoryOutput>();
            foreach (var entry in _resultHistoryOutputs)
            {
                if (entry.Key != oldResultHistoryOutputName)
                    resultHistoryOutputs.Add(entry.Key, entry.Value);
            }
            resultHistoryOutputs.Add(resultHistoryOutput.Name, resultHistoryOutput);
            //
            return AreResultHistoryOutputsInCyclicDependance(resultHistoryOutputs);
        }
        private bool AreResultHistoryOutputsInCyclicDependance(Dictionary<string, ResultHistoryOutput> resultHistoryOutputs)
        {
            int count = 0;
            bool result;
            foreach (var entry in resultHistoryOutputs)
            {
                result = RecursiveCheck(ref count, entry.Value.Name, entry.Value.Name, resultHistoryOutputs);
                if (result) return true;
            }
            return false;
        }
        private bool RecursiveCheck(ref int count, string firstName, string name,
                                    Dictionary<string, ResultHistoryOutput> resultHistoryOutputs)
        {
            if (count > 1000) return true;
            //
            ResultHistoryOutput resultHistoryOutput;
            if (resultHistoryOutputs.TryGetValue(name, out resultHistoryOutput))
            {
                string[] parents = resultHistoryOutput.GetParentNames();
                if (parents == null) return false;
                //
                bool result;
                foreach (string parent in parents)
                {
                    if (parent == firstName) return true;
                    //
                    count++;
                    result = RecursiveCheck(ref count, firstName, parent, resultHistoryOutputs);
                    if (result) return true;
                    count--;
                }
            }
            return false;
        }

        // Scaled results values
        public void GetNodesAndValues(FieldData fieldData, int[] nodeIds, out double[][] nodeCoor, out float[] values)
        {
            nodeCoor = new double[nodeIds.Length][];
            for (int i = 0; i < nodeIds.Length; i++)
            {
                nodeCoor[i] = Mesh.Nodes[nodeIds[i]].Coor;
            }
            values = GetValues(fieldData, nodeIds);
        }
        //
        public PartExchangeData GetAllNodesCellsAndValues(FieldData fData)
        {
            PartExchangeData pData = new PartExchangeData();
            _mesh.GetAllNodesAndCells(out pData.Nodes.Ids, out pData.Nodes.Coor, out pData.Cells.Ids,
                                      out pData.Cells.CellNodeIds, out pData.Cells.Types);
            if (!fData.Valid) pData.Nodes.Values = null;
            else pData.Nodes.Values = GetValues(fData, pData.Nodes.Ids);
            return pData;
        }
        public PartExchangeData GetSetNodesCellsAndValues(FeGroup elementSet, FieldData fData)
        {
            PartExchangeData pData = new PartExchangeData();
            _mesh.GetSetNodesAndCells(elementSet, out pData.Nodes.Ids, out pData.Nodes.Coor, out pData.Cells.Ids,
                                      out pData.Cells.CellNodeIds, out pData.Cells.Types);
            if (!fData.Valid) pData.Nodes.Values = null;
            else pData.Nodes.Values = GetValues(fData, pData.Nodes.Ids);
            return pData;
        }
        public PartExchangeData GetVisualizationNodesCellsAndValues(BasePart part, FieldData fData)
        {
            PartExchangeData pData = new PartExchangeData();
            _mesh.GetVisualizationNodesAndCells(part, out pData.Nodes.Ids, out pData.Nodes.Coor, out pData.Cells.Ids,
                                                out pData.Cells.CellNodeIds, out pData.Cells.Types);
            if (!fData.Valid) pData.Nodes.Values = null;
            else
            {
                pData.Nodes.Values = GetValues(fData, pData.Nodes.Ids);
                pData.ExtremeNodes = GetScaledExtremeValues(part.Name, fData, 1);
            }
            return pData;
        }
        public PartExchangeData GetEdgesNodesAndCells(FeGroup elementSet, FieldData fData)  // must use fData
        {
            PartExchangeData resultData = new PartExchangeData();
            _mesh.GetNodesAndCellsForModelEdges(elementSet, out resultData.Nodes.Ids, out resultData.Nodes.Coor,
                                                out resultData.Cells.CellNodeIds, out resultData.Cells.Types);
            return resultData;
        }
        public void GetUndeformedNodesAndCells(BasePart part, out double[][] nodeCoor, out int[][] cells, out int[] cellTypes)
        {
            int[] nodeIds, cellIds;
            Dictionary<int, FeNode> tmp = _mesh.Nodes;
            _mesh.Nodes = _undeformedNodes;
            _mesh.GetVisualizationNodesAndCells(part, out nodeIds, out nodeCoor, out cellIds, out cells, out cellTypes);
            // Add exploded view
            if (part.Offset != null && (part.Offset[0] != 0 || part.Offset[1] != 0 || part.Offset[2] != 0))
            {
                for (int i = 0; i < nodeCoor.Length; i++)
                {
                    nodeCoor[i][0] += part.Offset[0];
                    nodeCoor[i][1] += part.Offset[1];
                    nodeCoor[i][2] += part.Offset[2];
                }
            }
            //
            _mesh.Nodes = tmp;
        }
        public void GetUndeformedModelEdges(BasePart part, out double[][] nodeCoor, out int[][] cells, out int[] cellTypes)
        {
            int[] nodeIds;
            Dictionary<int, FeNode> tmp = _mesh.Nodes;
            _mesh.Nodes = _undeformedNodes;
            _mesh.GetNodesAndCellsForModelEdges(part, out nodeIds, out nodeCoor, out cells, out cellTypes);
            // Add exploded view
            if (part.Offset != null && (part.Offset[0] != 0 || part.Offset[1] != 0 || part.Offset[2] != 0))
            {
                for (int i = 0; i < nodeCoor.Length; i++)
                {
                    nodeCoor[i][0] += part.Offset[0];
                    nodeCoor[i][1] += part.Offset[1];
                    nodeCoor[i][2] += part.Offset[2];
                }
            }
            //
            _mesh.Nodes = tmp;
        }
        // Animation        
        public void GetScaleFactorAnimationData(BasePart part, FieldData fData, float scale, int numFrames,
                                                out PartExchangeData modelResultData,
                                                out PartExchangeData modelEdgesResultData,
                                                out PartExchangeData locatorResultData)
        {
            modelEdgesResultData = null;
            bool modelEdges = part.PartType.HasEdges() && part.Visualization.EdgeCells != null;
            // Undeforme part
            SetPartDeformation(part, 0, 0, 0);
            // Model
            modelResultData = new PartExchangeData();
            _mesh.GetVisualizationNodesAndCells(part, out modelResultData.Nodes.Ids, out modelResultData.Nodes.Coor,
                                                out modelResultData.Cells.Ids, out modelResultData.Cells.CellNodeIds,
                                                out modelResultData.Cells.Types);
            // Edges
            if (modelEdges)
            {
                modelEdgesResultData = new PartExchangeData();
                _mesh.GetNodesAndCellsForModelEdges(part, out modelEdgesResultData.Nodes.Ids,
                                                    out modelEdgesResultData.Nodes.Coor,
                                                    out modelEdgesResultData.Cells.CellNodeIds,
                                                    out modelEdgesResultData.Cells.Types);
            }
            // Locator
            locatorResultData = new PartExchangeData();
            _mesh.GetSetNodesAndCells(part, out locatorResultData.Nodes.Ids, out locatorResultData.Nodes.Coor, 
                                      out locatorResultData.Cells.Ids, out locatorResultData.Cells.CellNodeIds,
                                      out locatorResultData.Cells.Types);
            // Values
            if (!fData.Valid)
            {
                modelResultData.Nodes.Values = null;
                locatorResultData.Nodes.Values = null;
            }
            else
            {
                modelResultData.Nodes.Values = GetValues(fData, modelResultData.Nodes.Ids);
                modelResultData.ExtremeNodes = GetExtremeValues(part.Name, fData);
                locatorResultData.Nodes.Values = GetValues(fData, locatorResultData.Nodes.Ids);
            }
            // Aniumation
            modelResultData.NodesAnimation = new NodesExchangeData[numFrames];
            modelResultData.ExtremeNodesAnimation = new NodesExchangeData[numFrames];
            if (modelEdges) modelEdgesResultData.NodesAnimation = new NodesExchangeData[numFrames];
            locatorResultData.NodesAnimation = new NodesExchangeData[numFrames];
            //
            float[] ratios;
            if (fData.StepType == StepTypeEnum.Frequency || fData.StepType == StepTypeEnum.Buckling) ratios = GetRelativeModalScales(numFrames);
            else ratios = GetRelativeScales(numFrames);
            //
            float absoluteScale;
            float relativeScale;
            bool invariant = IsComponentInvariant(fData);
            //
            for (int i = 0; i < numFrames; i++)
            {
                relativeScale = ratios[i];
                absoluteScale = relativeScale * scale;
                // Deform mesh
                SetPartDeformation(part, absoluteScale, fData.StepId, fData.StepIncrementId);
                // Get scaled model
                modelResultData.NodesAnimation[i] = new NodesExchangeData();
                modelResultData.NodesAnimation[i].Coor = _mesh.GetNodeSetCoor(modelResultData.Nodes.Ids);
                // Get scaled edges
                if (modelEdges)
                {
                    modelEdgesResultData.NodesAnimation[i] = new NodesExchangeData();
                    modelEdgesResultData.NodesAnimation[i].Coor = _mesh.GetNodeSetCoor(modelEdgesResultData.Nodes.Ids);
                }
                // Get scaled locator
                locatorResultData.NodesAnimation[i] = new NodesExchangeData();
                locatorResultData.NodesAnimation[i].Coor = _mesh.GetNodeSetCoor(locatorResultData.Nodes.Ids);
                // Scale values
                if (invariant) relativeScale = Math.Abs(relativeScale);
                // Model
                ScaleValues(relativeScale, modelResultData.Nodes.Values, out modelResultData.NodesAnimation[i].Values);
                modelResultData.ExtremeNodesAnimation[i] = GetScaledExtremeValues(part.Name, fData, relativeScale);
                // Locator
                ScaleValues(relativeScale, locatorResultData.Nodes.Values, out locatorResultData.NodesAnimation[i].Values);
            }
        }
        public void GetTimeIncrementAnimationData(BasePart part, FieldData fData, float scale,
                                                  out PartExchangeData modelResultData,
                                                  out PartExchangeData modelEdgesResultData,
                                                  out PartExchangeData locatorResultData)
        {
            modelEdgesResultData = null;
            bool modelEdges = part.PartType.HasEdges() && part.Visualization.EdgeCells != null;
            // Undeforme part
            SetPartDeformation(part, 0, 0, 0);
            // Model
            modelResultData = GetVisualizationNodesCellsAndValues(part, fData);
            // Edges
            if (modelEdges) modelEdgesResultData = GetEdgesNodesAndCells(part, fData);
            // Locator
            locatorResultData = GetSetNodesCellsAndValues(part, fData);
            // Get all existing increments
            Dictionary<int, int[]> existingStepIncrementIds = GetExistingIncrementIds(fData.Name, fData.Component);
            // Count all existing increments
            int numFrames = 0;
            foreach (var entry in existingStepIncrementIds) numFrames += entry.Value.Length;
            // Create animation frames
            modelResultData.NodesAnimation = new NodesExchangeData[numFrames];
            modelResultData.ExtremeNodesAnimation = new NodesExchangeData[numFrames];
            if (modelEdges) modelEdgesResultData.NodesAnimation = new NodesExchangeData[numFrames];
            locatorResultData.NodesAnimation = new NodesExchangeData[numFrames];
            //
            PartExchangeData data;
            FieldData tmpFieldData = new FieldData(fData);  // create a copy
            int count = 0;
            foreach (var entry in existingStepIncrementIds)
            {
                tmpFieldData.StepId = entry.Key;
                foreach (var incrementId in entry.Value)
                {
                    tmpFieldData.StepIncrementId = incrementId;
                    // Deform part
                    SetPartDeformation(part, scale, tmpFieldData.StepId, tmpFieldData.StepIncrementId);
                    // Model
                    data = GetVisualizationNodesCellsAndValues(part, tmpFieldData);
                    modelResultData.NodesAnimation[count] = data.Nodes;
                    modelResultData.ExtremeNodesAnimation[count] = data.ExtremeNodes;
                    // Edges
                    if (modelEdges)
                    {
                        data = GetEdgesNodesAndCells(part, tmpFieldData);
                        modelEdgesResultData.NodesAnimation[count] = data.Nodes;
                    }
                    // Locator
                    data = GetSetNodesCellsAndValues(part, tmpFieldData);
                    locatorResultData.NodesAnimation[count] = data.Nodes;
                    //
                    count++;
                }
            }
        }
        //
        private float[] GetRelativeScales(int numFrames)
        {
            float ratio = 1f / (numFrames - 1);
            float[] ratios = new float[numFrames];
            for (int i = 0; i < numFrames; i++) ratios[i] = i * ratio;
            return ratios;
        }
        private float[] GetRelativeModalScales(int numFrames)
        {
            float ratio = 2f / (numFrames - 1);
            float[] ratios = new float[numFrames];
            for (int i = 0; i < numFrames; i++)
            {
                ratios[i] = (float)Math.Sin((-1 + i * ratio) * Math.PI / 2);     // end with 1
            }
            return ratios;
        }
        //
        public void ScaleNodeCoordinates(string deformationFieldOutputName, float scale, int stepId, int stepIncrementId,
                                         int[] globalNodeIds, ref double[][] nodes)
        {
            if (deformationFieldOutputName == FOFieldNames.Default)
                deformationFieldOutputName = GetInternalDeformationFieldOutputName(stepId, stepIncrementId);
            //
            if (scale != 0)
            {
                float[][] deformations = GetNodalMeshDeformations(deformationFieldOutputName, stepId, stepIncrementId);
                if (deformations != null)
                {
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        int resultNodeId;
                        // Scale only result parts
                        if (_nodeIdsLookUp.TryGetValue(globalNodeIds[i], out resultNodeId))
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                nodes[i][j] += scale * deformations[j][resultNodeId];
                            }
                        }
                    }
                }
            }
        }
        //
        public void ScaleValues(float scale, float[] values, out float[] scaledValues)
        {
            if (values == null) scaledValues = null;
            else
            {
                scaledValues = new float[values.Length];
                if (scale != 0)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        scaledValues[i] = values[i] * scale;
                    }
                }
            }
        }
        //
        public float GetMaxDeformation()
        {
            double fieldMax;
            double max = -double.MaxValue;
            double value;
            string deformationFieldOutputName;
            string[] componentNames = GetFieldComponentNames(_deformationFieldOutputName);
            //
            foreach (var entry in _fields)
            {
                deformationFieldOutputName = GetInternalDeformationFieldOutputName(entry.Key.StepId, entry.Key.StepIncrementId);
                if (entry.Key.Name == deformationFieldOutputName)
                {
                    fieldMax = 0;
                    //
                    foreach (var componentName in componentNames)
                    {
                        value = Math.Pow(entry.Value.GetComponentAbsMax(componentName), 2);
                        if (!double.IsNaN(value)) fieldMax += value;
                    }
                    //
                    if (fieldMax > 0) fieldMax = Math.Sqrt(fieldMax);
                    //
                    if (fieldMax > max) max = fieldMax;
                }
            }
            return (float)max;
        }
        public float GetMaxDeformation(int stepId, int stepIncrementId)
        {
            double max = -double.MaxValue;
            double value;
            string deformationFieldOutputName;
            foreach (var entry in _fields)
            {
                if (entry.Key.StepId == stepId && entry.Key.StepIncrementId == stepIncrementId)
                {
                    deformationFieldOutputName = GetInternalDeformationFieldOutputName(entry.Key.StepId, entry.Key.StepIncrementId);
                    //
                    if (entry.Key.Name == deformationFieldOutputName)
                    {
                        max = 0;
                        string[] componentNames = GetFieldComponentNames(deformationFieldOutputName);
                        //
                        foreach (var componentName in componentNames)
                        {
                            value = Math.Pow(entry.Value.GetComponentAbsMax(componentName), 2);
                            if (!double.IsNaN(value)) max += value;
                        }
                        //
                        if (max > 0) max = Math.Sqrt(max);
                        //
                        break;
                    }
                }
            }
            return (float)max;
        }
        //
        public double GetEdgeLength(int geometryEdgeId)
        {
            int[] itemTypePartIds = FeMesh.GetItemTypePartIdsFromGeometryId(geometryEdgeId);
            BasePart part = _mesh.GetPartFromId(itemTypePartIds[2]);
            int[] nodeIds = part.Visualization.GetOrderedNodeIdsForEdgeId(itemTypePartIds[0]);
            //
            return 0;
        }

        // Wear         
        public bool ComputeWear(int[] slipStepIds, Dictionary<int, double> nodeIdCoefficient,
                                int numOfSmoothingSteps, Dictionary<int, bool[]> nodeIdZeroDisplacements)
        {
            if (slipStepIds != null && slipStepIds.Length > 0 && CheckFieldAndHistoryTimes())
            {
                ComputeHistoryWearSlidingDistance();
                //
                HistoryResultComponent slidingDistanceAll =
                    GetHistoryResultComponent(HOSetNames.ContactWear, HOFieldNames.SlidingDistance, HOComponentNames.All);
                if (slidingDistanceAll != null)
                {
                    CreateAveragedFieldFromElementFaceHistory(FOFieldNames.SlidingDistance, slidingDistanceAll, true);
                }
                //
                HistoryResultField surfaceNormalField = GetHistoryResultField(HOSetNames.ContactWear, HOFieldNames.SurfaceNormal);
                if (surfaceNormalField != null)
                {
                    HistoryResultComponent all = surfaceNormalField.Components[HOComponentNames.All];
                    HistoryResultComponent n1 = surfaceNormalField.Components[HOComponentNames.N1];
                    HistoryResultComponent n2 = surfaceNormalField.Components[HOComponentNames.N2];
                    HistoryResultComponent n3 = surfaceNormalField.Components[HOComponentNames.N3];
                    //
                    CreateAveragedFieldFromElementFaceHistory(FOFieldNames.SurfaceNormal, all, false);
                    CreateAveragedFieldFromElementFaceHistory(FOFieldNames.SurfaceNormal, n1, false);
                    CreateAveragedFieldFromElementFaceHistory(FOFieldNames.SurfaceNormal, n2, false);
                    CreateAveragedFieldFromElementFaceHistory(FOFieldNames.SurfaceNormal, n3, false);
                }
                //
                return ComputeSlidingWearFields(slipStepIds, nodeIdCoefficient, numOfSmoothingSteps, nodeIdZeroDisplacements);
            }
            else return false;
        }
        private bool CheckFieldAndHistoryTimes()
        {
            HistoryResultField relativeContactDisplacement =
                GetHistoryResultField(HOSetNames.AllContactElements, HOFieldNames.RelativeContactDisplacement);
            if (relativeContactDisplacement != null)
            {
                HistoryResultComponent tang1 = relativeContactDisplacement.Components[HOComponentNames.Tang1];
                if (tang1 != null)
                {
                    // Sorted time
                    double[] historySortedTime;
                    Dictionary<double, int> timeRowId;
                    GetSortedTime(new HistoryResultComponent[] { tang1 }, out historySortedTime, out timeRowId);
                    //
                    int fieldIncrementsCount = 0;
                    int[] stepIds = GetAllStepIds();
                    for (int i = 0; i < stepIds.Length; i++)
                    {
                        int[] stepIncrementIds = GetStepIncrementIds(stepIds[i]);
                        fieldIncrementsCount += stepIncrementIds.Length;
                    }
                    //
                    if (historySortedTime.Length == fieldIncrementsCount - 1) // -1 for Zero increment - Find all occurrences!!!
                        return true;
                }
            }
            return false;
        }
        private void ComputeHistoryWearSlidingDistance()
        {
            HistoryResultField relativeContactDisplacement =
                GetHistoryResultField(HOSetNames.AllContactElements, HOFieldNames.RelativeContactDisplacement);
            //
            if (relativeContactDisplacement != null)
            {
                HistoryResultComponent tang1 = relativeContactDisplacement.Components[HOComponentNames.Tang1];
                HistoryResultComponent s1 = ComputeRelativeDisplacement(HOComponentNames.S1, tang1);
                //
                HistoryResultComponent tang2 = relativeContactDisplacement.Components[HOComponentNames.Tang2];
                HistoryResultComponent s2 = ComputeRelativeDisplacement(HOComponentNames.S2, tang2);
                //
                HistoryResultComponent all =
                    ComputeVectorMagnitude(HOComponentNames.All, new HistoryResultComponent[] { s1, s2 });
                //
                relativeContactDisplacement = new HistoryResultField(HOFieldNames.SlidingDistance);
                relativeContactDisplacement.Components.Add(all.Name, all);
                relativeContactDisplacement.Components.Add(s1.Name, s1);
                relativeContactDisplacement.Components.Add(s2.Name, s2);
               
                // Use tang1 since it contains only values != 0
                HistoryResultComponent[] normalComponents = GetNormalsFromElementFaceHistory(tang1);
                //
                HistoryResultField surfaceNormalsField = new HistoryResultField(HOFieldNames.SurfaceNormal);
                surfaceNormalsField.Components.Add(normalComponents[0].Name, normalComponents[0]);  // All
                surfaceNormalsField.Components.Add(normalComponents[1].Name, normalComponents[1]);  // N1
                surfaceNormalsField.Components.Add(normalComponents[2].Name, normalComponents[2]);  // N2
                surfaceNormalsField.Components.Add(normalComponents[3].Name, normalComponents[3]);  // N3
                //
                HistoryResultSet contactWear = new HistoryResultSet(HOSetNames.ContactWear);
                contactWear.Fields.Add(relativeContactDisplacement.Name, relativeContactDisplacement);
                contactWear.Fields.Add(surfaceNormalsField.Name, surfaceNormalsField);
                //
                _history.Sets.Add(contactWear.Name, contactWear);
            }
        }
        public bool ComputeSlidingWearFields(int[] slipStepIds, Dictionary<int, double> nodeIdCoefficient,
                                             int numOfSmoothingSteps, Dictionary<int, bool[]> nodeIdZeroDisplacements)
        {
            HistoryResultComponent slidingDistanceAll =
                GetHistoryResultComponent(HOSetNames.ContactWear, HOFieldNames.SlidingDistance, HOComponentNames.All);
            //
            if (slidingDistanceAll != null)
            {
                float[] coefficients = new float[_nodeIdsLookUp.Count()]; 
                if (nodeIdCoefficient != null)
                {
                    foreach (var entry in _nodeIdsLookUp) coefficients[entry.Value] = (float)nodeIdCoefficient[entry.Key];
                }
                else
                {
                    foreach (var entry in _nodeIdsLookUp) coefficients[entry.Value] = 0;
                }
                //
                float dh;
                float[] pressureValues;
                float[] slidingDistanceValues;
                float[] normalN1Values;
                float[] normalN2Values;
                float[] normalN3Values;
                float[] depthValuesMag;
                float[] depthValuesH1;
                float[] depthValuesH2;
                float[] depthValuesH3;
                float[] prevDepthValuesMag = null;
                float[] prevDepthValuesH1 = null;
                float[] prevDepthValuesH2 = null;
                float[] prevDepthValuesH3 = null;
                float[] dispValuesMag;
                float[] dispValuesU1;
                float[] dispValuesU2;
                float[] dispValuesU3;
                //
                Field pressureField;
                Field slidingDistanceField;
                Field depthField;
                Field normalField;
                Field dispField;
                Field meshUpdateField;
                //
                FieldData pressureData;
                FieldData slidingDistanceData;
                FieldData depthData;
                FieldData normalData;
                FieldData dispData;
                FieldData meshUpdateData;
                //
                int id;
                Vec3D normal = new Vec3D();
                //
                for (int i = 0; i < slipStepIds.Length; i++)
                {
                    int[] stepIncrementIds = GetStepIncrementIds(slipStepIds[i]);
                    //
                    for (int j = 0; j < stepIncrementIds.Length; j++)
                    {
                        if (slipStepIds[i] == 0 && stepIncrementIds[j] == 0) continue;
                        // Pressure
                        pressureData = GetFieldData(FOFieldNames.Contact, "", slipStepIds[i], stepIncrementIds[j]);
                        pressureField = GetField(pressureData);
                        if (pressureField != null)
                        {
                            pressureValues = pressureField.GetComponentValues(FOComponentNames.CPress);
                            // Disp
                            dispData = GetFieldData(FOFieldNames.Disp, "", slipStepIds[i], stepIncrementIds[j]);
                            dispField = GetField(dispData);
                            dispValuesMag = new float[pressureValues.Length];
                            dispValuesU1 = dispField.GetComponentValues(FOComponentNames.U1).ToArray(); // copy
                            dispValuesU2 = dispField.GetComponentValues(FOComponentNames.U2).ToArray(); // copy
                            dispValuesU3 = dispField.GetComponentValues(FOComponentNames.U3).ToArray(); // copy
                            // Sliding distance
                            slidingDistanceData = GetFieldData(FOFieldNames.SlidingDistance, "",
                                                               slipStepIds[i], stepIncrementIds[j]);
                            slidingDistanceField = GetField(slidingDistanceData);
                            slidingDistanceValues = slidingDistanceField.GetComponentValues(FOComponentNames.All);
                            // Normal
                            normalData = GetFieldData(FOFieldNames.SurfaceNormal, "", slipStepIds[i], stepIncrementIds[j]);
                            normalField = GetField(normalData);
                            normalN1Values = normalField.GetComponentValues(FOComponentNames.N1).ToArray();
                            normalN2Values = normalField.GetComponentValues(FOComponentNames.N2).ToArray();
                            normalN3Values = normalField.GetComponentValues(FOComponentNames.N3).ToArray();
                            // Adjust normals based on zero BCs inside the wear step
                            if (nodeIdZeroDisplacements != null)
                            {
                                foreach (var entry in nodeIdZeroDisplacements)
                                {
                                    id = _nodeIdsLookUp[entry.Key];
                                    if (entry.Value[0]) normal.X = 0;
                                    else normal.X = normalN1Values[id];
                                    if (entry.Value[1]) normal.Y = 0;
                                    else normal.Y = normalN2Values[id];
                                    if (entry.Value[2]) normal.Z = 0;
                                    else normal.Z = normalN3Values[id];
                                    //
                                    normal.Normalize();
                                    //
                                    normalN1Values[id] = (float)normal.X;
                                    normalN2Values[id] = (float)normal.Y;
                                    normalN3Values[id] = (float)normal.Z;
                                }
                            }
                            // Wear depth
                            depthValuesMag = new float[pressureValues.Length];
                            depthValuesH1 = new float[pressureValues.Length];
                            depthValuesH2 = new float[pressureValues.Length];
                            depthValuesH3 = new float[pressureValues.Length];
                            if (prevDepthValuesMag == null)
                            {
                                prevDepthValuesMag = new float[pressureValues.Length];
                                prevDepthValuesH1 = new float[pressureValues.Length];
                                prevDepthValuesH2 = new float[pressureValues.Length];
                                prevDepthValuesH3 = new float[pressureValues.Length];
                            }
                            //
                            for (int k = 0; k < depthValuesMag.Length; k++)
                            {
                                dh = coefficients[k] * pressureValues[k] * slidingDistanceValues[k];
                                depthValuesMag[k] = dh + prevDepthValuesMag[k];
                                depthValuesH1[k] = dh * normalN1Values[k] + prevDepthValuesH1[k];
                                depthValuesH2[k] = dh * normalN2Values[k] + prevDepthValuesH2[k];
                                depthValuesH3[k] = dh * normalN3Values[k] + prevDepthValuesH3[k];
                                // Disp with wear depth
                                dispValuesU1[k] += depthValuesH1[k];
                                dispValuesU2[k] += depthValuesH2[k];
                                dispValuesU3[k] += depthValuesH3[k];
                                dispValuesMag[k] = (float)Math.Sqrt(Math.Pow(dispValuesU1[k], 2) +
                                                                    Math.Pow(dispValuesU2[k], 2) +
                                                                    Math.Pow(dispValuesU3[k], 2));
                            }
                            //
                            prevDepthValuesMag = depthValuesMag.ToArray(); // must copy due to smoothing
                            prevDepthValuesH1 = depthValuesH1.ToArray();
                            prevDepthValuesH2 = depthValuesH2.ToArray();
                            prevDepthValuesH3 = depthValuesH3.ToArray();
                            // Smoothing
                            SmoothVectorField(ref depthValuesH1, ref depthValuesH2, ref depthValuesH3, ref depthValuesMag,
                                              numOfSmoothingSteps);
                            // Wear depth
                            depthData = new FieldData(FOFieldNames.WearDepth);
                            depthData.StepId = slipStepIds[i];
                            depthData.StepIncrementId = stepIncrementIds[j];
                            depthData.Time = dispData.Time;
                            depthData.StepType = StepTypeEnum.Static;
                            depthField = new Field(depthData.Name);
                            depthField.AddComponent(FOComponentNames.All, depthValuesMag);
                            depthField.AddComponent(FOComponentNames.H1, depthValuesH1);
                            depthField.AddComponent(FOComponentNames.H2, depthValuesH2);
                            depthField.AddComponent(FOComponentNames.H3, depthValuesH3);
                            AddField(depthData, depthField);
                            // Mesh update
                            meshUpdateData = new FieldData(FOFieldNames.MeshDeformation);
                            meshUpdateData.StepId = slipStepIds[i];
                            meshUpdateData.StepIncrementId = stepIncrementIds[j];
                            meshUpdateData.Time = dispData.Time;
                            meshUpdateData.StepType = StepTypeEnum.Static;
                            meshUpdateField = new Field(meshUpdateData.Name);
                            meshUpdateField.AddComponent(FOComponentNames.All, new float[pressureValues.Length]);
                            meshUpdateField.AddComponent(FOComponentNames.U1, new float[pressureValues.Length]);
                            meshUpdateField.AddComponent(FOComponentNames.U2, new float[pressureValues.Length]);
                            meshUpdateField.AddComponent(FOComponentNames.U3, new float[pressureValues.Length]);
                            AddField(meshUpdateData, meshUpdateField);
                            // Disp with wear depth
                            dispData.Name = FOFieldNames.DispDeformationDepth;
                            dispField = new Field(dispData.Name);
                            dispField.AddComponent(FOComponentNames.All, dispValuesMag);
                            dispField.AddComponent(FOComponentNames.U1, dispValuesU1);
                            dispField.AddComponent(FOComponentNames.U2, dispValuesU2);
                            dispField.AddComponent(FOComponentNames.U3, dispValuesU3);
                            AddField(dispData, dispField);
                           
                        }
                    }
                }
                //
                return true;
            }
            else return false;
        }
        private void SmoothVectorField(ref float[] component1, ref float[] component2, ref float[] component3,
                                       ref float[] componentMag, int numOfSmoothingSteps)
        {
            if (component1.Length != component2.Length && component2.Length != component3.Length &&
                component3.Length != componentMag.Length) throw new NotSupportedException();
            //
            if (numOfSmoothingSteps == 0) return;
            //
            double[] xyz = new double[3];
            HashSet<int> nodeIds = new HashSet<int>();
            Dictionary<int, double[]> globalVectors = new Dictionary<int, double[]>();
            //
            foreach (var entry in _nodeIdsLookUp)
            {
                xyz[0] = component1[entry.Value];
                xyz[1] = component2[entry.Value];
                xyz[2] = component3[entry.Value];
                //
                if (xyz[0] != 0 || xyz[1] != 0 || xyz[2] != 0)
                {
                    globalVectors.Add(entry.Key, new double[] { xyz[0], xyz[1], xyz[2] });
                    nodeIds.Add(entry.Key);
                }
            }
            // Smooth                                                                                                               
            bool contains;
            int surfaceId;
            int cellId;
            int[] cell;
            VisualizationData vis;
            List<int> surfaceIds;
            Dictionary<VisualizationData, List<int>> visualizationSurfaceIds = new Dictionary<VisualizationData, List<int>>();
            // Find surfaces that have deformed entire element faces
            foreach (var entry in _mesh.Parts)
            {
                vis = entry.Value.Visualization;
                for (int i = 0; i < vis.CellIdsByFace.Length; i++)
                {
                    surfaceId = i;
                    for (int j = 0; j < vis.CellIdsByFace[surfaceId].Length; j++)
                    {
                        cellId = vis.CellIdsByFace[surfaceId][j];
                        cell = vis.Cells[cellId];
                        //
                        contains = true;
                        for (int k = 0; k < cell.Length; k++)
                        {
                            if (!nodeIds.Contains(cell[k])) { contains = false; break; }
                        }
                        if (contains)
                        {
                            if (visualizationSurfaceIds.TryGetValue(vis, out surfaceIds)) surfaceIds.Add(surfaceId);
                            else visualizationSurfaceIds.Add(vis, new List<int> { surfaceId });
                            break;
                        }
                    }
                }
            }
            //
            int numOfNodes;
            double[] vector;
            HashSet<int> neighboursHash;
            Dictionary<int, HashSet<int>> nodeIdNeighbours = new Dictionary<int, HashSet<int>>();
            // Find node neighbours
            foreach (var entry in visualizationSurfaceIds)
            {
                vis = entry.Key;
                foreach (var visSurfaceId in entry.Value)
                {
                    foreach (var visCellId in vis.CellIdsByFace[visSurfaceId])
                    {
                        cell = vis.Cells[visCellId];
                        numOfNodes = cell.Length;
                        // Find node neighbours by element
                        for (int i = 0; i < numOfNodes; i++)
                        {
                            if (nodeIdNeighbours.TryGetValue(cell[i], out neighboursHash))
                                neighboursHash.UnionWith(cell);
                            else
                            {
                                neighboursHash = new HashSet<int>(cell);
                                nodeIdNeighbours.Add(cell[i], neighboursHash);
                            }
                            neighboursHash.Remove(cell[i]);
                        }
                    }
                }
            }
            // Smoothing loops
            Dictionary<int, double[]> smoothedVectors;
            for (int i = 0; i < numOfSmoothingSteps; i++)
            {
                smoothedVectors = new Dictionary<int, double[]>(); // must be here
                //
                foreach (var entry in nodeIdNeighbours)
                {
                    xyz = new double[3];
                    foreach (var nodeId in entry.Value)
                    {
                        if (globalVectors.TryGetValue(nodeId, out vector))
                        {
                            xyz[0] += vector[0];
                            xyz[1] += vector[1];
                            xyz[2] += vector[2];
                        }
                    }
                    if (xyz[0] != 0 || xyz[1] != 0 || xyz[2] != 0)
                    {
                        xyz[0] /= entry.Value.Count;
                        xyz[1] /= entry.Value.Count;
                        xyz[2] /= entry.Value.Count;
                        //
                        smoothedVectors.Add(entry.Key, xyz);
                    }
                }
                //
                globalVectors = smoothedVectors;
            }
            // Move midside nodes to to central positions                                                                           
            Dictionary<int, int[]> midNodeIdNeighbours = new Dictionary<int, int[]>();
            foreach (var entry in _mesh.Elements)
            {
                if (entry.Value is LinearTetraElement ||
                    entry.Value is LinearWedgeElement ||
                    entry.Value is LinearHexaElement) { }
                else if (entry.Value is ParabolicTetraElement pte)
                {
                    if (!midNodeIdNeighbours.ContainsKey(pte.NodeIds[4]))
                        midNodeIdNeighbours.Add(pte.NodeIds[4], new int[] { pte.NodeIds[0], pte.NodeIds[1] });
                    if (!midNodeIdNeighbours.ContainsKey(pte.NodeIds[5]))
                        midNodeIdNeighbours.Add(pte.NodeIds[5], new int[] { pte.NodeIds[1], pte.NodeIds[2] });
                    if (!midNodeIdNeighbours.ContainsKey(pte.NodeIds[6]))
                        midNodeIdNeighbours.Add(pte.NodeIds[6], new int[] { pte.NodeIds[2], pte.NodeIds[0] });
                    //
                    if (!midNodeIdNeighbours.ContainsKey(pte.NodeIds[7]))
                        midNodeIdNeighbours.Add(pte.NodeIds[7], new int[] { pte.NodeIds[0], pte.NodeIds[3] });
                    if (!midNodeIdNeighbours.ContainsKey(pte.NodeIds[8]))
                        midNodeIdNeighbours.Add(pte.NodeIds[8], new int[] { pte.NodeIds[1], pte.NodeIds[3] });
                    if (!midNodeIdNeighbours.ContainsKey(pte.NodeIds[9]))
                        midNodeIdNeighbours.Add(pte.NodeIds[9], new int[] { pte.NodeIds[2], pte.NodeIds[3] });
                }
                else if (entry.Value is ParabolicWedgeElement pwe)
                {
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[6]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[6], new int[] { pwe.NodeIds[0], pwe.NodeIds[1] });
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[7]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[7], new int[] { pwe.NodeIds[1], pwe.NodeIds[2] });
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[8]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[8], new int[] { pwe.NodeIds[2], pwe.NodeIds[0] });
                    //
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[9]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[9], new int[] { pwe.NodeIds[3], pwe.NodeIds[4] });
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[10]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[10], new int[] { pwe.NodeIds[4], pwe.NodeIds[5] });
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[11]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[11], new int[] { pwe.NodeIds[5], pwe.NodeIds[3] });
                    //
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[12]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[12], new int[] { pwe.NodeIds[0], pwe.NodeIds[3] });
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[13]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[13], new int[] { pwe.NodeIds[1], pwe.NodeIds[4] });
                    if (!midNodeIdNeighbours.ContainsKey(pwe.NodeIds[14]))
                        midNodeIdNeighbours.Add(pwe.NodeIds[14], new int[] { pwe.NodeIds[2], pwe.NodeIds[5] });
                }
                else if (entry.Value is ParabolicHexaElement phe)
                {
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[8]))
                        midNodeIdNeighbours.Add(phe.NodeIds[8], new int[] { phe.NodeIds[0], phe.NodeIds[1] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[9]))
                        midNodeIdNeighbours.Add(phe.NodeIds[9], new int[] { phe.NodeIds[1], phe.NodeIds[2] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[10]))
                        midNodeIdNeighbours.Add(phe.NodeIds[10], new int[] { phe.NodeIds[2], phe.NodeIds[3] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[11]))
                        midNodeIdNeighbours.Add(phe.NodeIds[11], new int[] { phe.NodeIds[3], phe.NodeIds[0] });
                    //
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[12]))
                        midNodeIdNeighbours.Add(phe.NodeIds[12], new int[] { phe.NodeIds[4], phe.NodeIds[5] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[13]))
                        midNodeIdNeighbours.Add(phe.NodeIds[13], new int[] { phe.NodeIds[5], phe.NodeIds[6] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[14]))
                        midNodeIdNeighbours.Add(phe.NodeIds[14], new int[] { phe.NodeIds[6], phe.NodeIds[7] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[15]))
                        midNodeIdNeighbours.Add(phe.NodeIds[15], new int[] { phe.NodeIds[7], phe.NodeIds[4] });
                    //
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[16]))
                        midNodeIdNeighbours.Add(phe.NodeIds[16], new int[] { phe.NodeIds[0], phe.NodeIds[4] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[17]))
                        midNodeIdNeighbours.Add(phe.NodeIds[17], new int[] { phe.NodeIds[1], phe.NodeIds[5] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[18]))
                        midNodeIdNeighbours.Add(phe.NodeIds[18], new int[] { phe.NodeIds[2], phe.NodeIds[6] });
                    if (!midNodeIdNeighbours.ContainsKey(phe.NodeIds[19]))
                        midNodeIdNeighbours.Add(phe.NodeIds[19], new int[] { phe.NodeIds[3], phe.NodeIds[7] });
                }
                else throw new NotSupportedException();
            }
            //
            double[] vector1;
            double[] vector2;
            double[] coor1;
            double[] coor2;
            double[] coorMid;
            double[] coorAvg = new double[3];
            int[] neighbours;
            foreach (var entry in globalVectors)
            {
                if (midNodeIdNeighbours.TryGetValue(entry.Key, out neighbours))
                {
                    if (!globalVectors.TryGetValue(neighbours[0], out vector1)) vector1 = new double[3];
                    if (!globalVectors.TryGetValue(neighbours[1], out vector2)) vector2 = new double[3];
                    //
                    coor1 = _mesh.Nodes[neighbours[0]].Coor.ToArray();
                    coor1[0] += vector1[0];
                    coor1[1] += vector1[1];
                    coor1[2] += vector1[2];
                    //
                    coor2 = _mesh.Nodes[neighbours[1]].Coor.ToArray();
                    coor2[0] += vector2[0];
                    coor2[1] += vector2[1];
                    coor2[2] += vector2[2];
                    //
                    coorAvg[0] = (coor1[0] + coor2[0]) / 2;
                    coorAvg[1] = (coor1[1] + coor2[1]) / 2;
                    coorAvg[2] = (coor1[2] + coor2[2]) / 2;
                    //
                    coorMid = _mesh.Nodes[entry.Key].Coor.ToArray();
                    xyz = entry.Value;
                    xyz[0] = coorAvg[0] - coorMid[0];
                    xyz[1] = coorAvg[1] - coorMid[1];
                    xyz[2] = coorAvg[2] - coorMid[2];
                }
            }
            // Copy data back to components                                                                                         
            for (int i = 0; i < component1.Length; i++)
            {
                component1[i] = 0; component2[i] = 0; component3[i] = 0; componentMag[i] = 0;
            }
            int locNodeId;
            foreach (var entry in globalVectors)
            {
                locNodeId = _nodeIdsLookUp[entry.Key];
                component1[locNodeId] = (float)entry.Value[0];
                component2[locNodeId] = (float)entry.Value[1];
                component3[locNodeId] = (float)entry.Value[2];
                componentMag[locNodeId] = (float)Math.Sqrt(entry.Value[0] * entry.Value[0] + entry.Value[1] * entry.Value[1] +
                                                           entry.Value[2] * entry.Value[2]);
            }
        }
        private float[][] GetLocalVectors(string fieldName)
        {
            string prevFieldOutputName = _deformationFieldOutputName;
            _deformationFieldOutputName = fieldName;
            //
            int[] stepIds = GetAllStepIds();
            int stepId = stepIds.Last();
            int[] stepIncrementIds = GetStepIncrementIds(stepId);
            int stepIncrementId = stepIncrementIds.Last();
            //
            float[][] vectors = GetNodalMeshDeformations(stepId, stepIncrementId);
            //
            _deformationFieldOutputName = prevFieldOutputName;
            //
            return vectors;
        }
        public Dictionary<int, double[]> GetGlobalNonZeroVectors(string fieldName)
        {
            double[] xyz = new double[3];
            float[][] vectors = GetLocalVectors(fieldName);
            HashSet<int> nodeIds = new HashSet<int>();
            Dictionary<int, double[]> globalVectors = new Dictionary<int, double[]>();
            //
            if (vectors != null)
            {
                foreach (var entry in _nodeIdsLookUp)
                {
                    xyz[0] = vectors[0][entry.Value];
                    xyz[1] = vectors[1][entry.Value];
                    xyz[2] = vectors[2][entry.Value];
                    //
                    if (xyz[0] != 0 || xyz[1] != 0 || xyz[2] != 0)
                    {
                        globalVectors.Add(entry.Key, new double[] { xyz[0], xyz[1], xyz[2] });
                        nodeIds.Add(entry.Key);
                    }
                }
            }
            //
            return globalVectors;
        }
        //
        private HistoryResultComponent ComputeRelativeDisplacement(string componentName, HistoryResultComponent tang1)
        {
            double[] time;
            double[] values;
            HistoryResultEntries itemData;
            HistoryResultComponent component = new HistoryResultComponent(componentName);
            //
            foreach (var entry in tang1.Entries)
            {
                time = entry.Value.Time.ToArray();
                values = entry.Value.Values.ToArray();
                itemData = new HistoryResultEntries(entry.Key, false);
                //
                itemData.Add(time[0], 0);   // must be here to keep the time in the component
                for (int i = 1; i < values.Length; i++)
                {
                    // Add only values != 0
                    if (values[i - 1] != 0 && values[i] != 0) itemData.Add(time[i], values[i] - values[i - 1]);
                }
                if (itemData.Time.Count > 0) component.Entries.Add(itemData.Name, itemData);
            }
            //
            return component;
        }
        private HistoryResultComponent ComputeVectorMagnitude(string componentName, HistoryResultComponent[] components)
        {
            // Sorted time
            double[] sortedTime;
            Dictionary<double, int> timeRowId;
            GetSortedTime(components, out sortedTime, out timeRowId);
            // Get all entry names
            HashSet<string> entryNamesHash = new HashSet<string>();
            foreach (var component in components)
            {
                if (entryNamesHash.Count == 0) entryNamesHash.UnionWith(component.Entries.Keys);
                else entryNamesHash.IntersectWith(component.Entries.Keys);
            }
            //
            double value;
            double[][] values = new double[components.Length][];
            HistoryResultEntries historyEntry;
            HistoryResultComponent magnitudeComponent = new HistoryResultComponent(componentName);
            foreach (var entryName in entryNamesHash)
            {
                // Get all entry values from all components
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i].Entries.TryGetValue(entryName, out historyEntry))
                        values[i] = GetAllEntryValues(historyEntry, timeRowId);
                    else values[i] = new double[timeRowId.Count];
                }
                // Compute the magnitude
                historyEntry = new HistoryResultEntries(entryName, false);
                for (int i = 0; i < sortedTime.Length; i++)
                {
                    value = 0;
                    for (int j = 0; j < components.Length; j++)
                    {
                        value += Math.Pow(values[j][i], 2);
                    }
                    historyEntry.Add(sortedTime[i], Math.Sqrt(value));
                }
                //
                if (historyEntry.Time.Count > 0) magnitudeComponent.Entries.Add(historyEntry.Name, historyEntry);
            }
            //
            return magnitudeComponent;
        }
        private HistoryResultComponent[] GetNormalsFromElementFaceHistory(HistoryResultComponent historyResultComponent)
        {
            HistoryResultComponent[] normalComponents = new HistoryResultComponent[4];
            normalComponents[0] = new HistoryResultComponent(HOComponentNames.All);
            normalComponents[1] = new HistoryResultComponent(HOComponentNames.N1);
            normalComponents[2] = new HistoryResultComponent(HOComponentNames.N2);
            normalComponents[3] = new HistoryResultComponent(HOComponentNames.N3);
            // Sorted time
            double[] sortedTime;
            Dictionary<double, int> timeRowId;
            GetSortedTime(new HistoryResultComponent[] { historyResultComponent }, out sortedTime, out timeRowId);
            // Get all values data for each node
            int faceId;
            int elementId;
            int count = 0;
            string[] tmp;
            double faceArea;
            double[] faceNormal;
            FeElement element;
            FeFaceName faceName;
            HistoryResultEntries normalAllEntry;
            HistoryResultEntries normal1Entry;
            HistoryResultEntries normal2Entry;
            HistoryResultEntries normal3Entry;
            //
            foreach (var entry in historyResultComponent.Entries)
            {
                tmp = entry.Key.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length == 2 && int.TryParse(tmp[0], out elementId) && int.TryParse(tmp[1], out faceId))
                {
                    switch (faceId)
                    {
                        case 1: faceName = FeFaceName.S1; break;
                        case 2: faceName = FeFaceName.S2; break;
                        case 3: faceName = FeFaceName.S3; break;
                        case 4: faceName = FeFaceName.S4; break;
                        case 5: faceName = FeFaceName.S5; break;
                        case 6: faceName = FeFaceName.S6; break;
                        default: throw new NotSupportedException();
                    }
                    //
                    if (_mesh.Elements.TryGetValue(elementId, out element))
                    {
                        _mesh.GetElementFaceNormalAndArea(elementId, faceName, out faceNormal, out faceArea);
                    }
                    else if (elementId == -1) // fix for when no history output exists - Find all occurrences!!!
                    {
                        faceName = FeFaceName.S1;
                        faceNormal = new double[] { 1, 0, 0 };
                        faceArea = 0;
                    }
                    else throw new NotSupportedException();
                    //
                    normalAllEntry = new HistoryResultEntries(entry.Key, false);
                    normal1Entry = new HistoryResultEntries(entry.Key, false);
                    normal2Entry = new HistoryResultEntries(entry.Key, false);
                    normal3Entry = new HistoryResultEntries(entry.Key, false);
                    //
                    foreach (var time in entry.Value.Time)
                    {
                        normalAllEntry.Add(time, 1);
                        normal1Entry.Add(time, faceNormal[0]);
                        normal2Entry.Add(time, faceNormal[1]);
                        normal3Entry.Add(time, faceNormal[2]);
                    }
                    //
                    normalComponents[0].Entries.Add(normalAllEntry.Name, normalAllEntry);
                    normalComponents[1].Entries.Add(normal1Entry.Name, normal1Entry);
                    normalComponents[2].Entries.Add(normal2Entry.Name, normal2Entry);
                    normalComponents[3].Entries.Add(normal3Entry.Name, normal3Entry);
                    
                }
                count++;
            }
            //
            return normalComponents;
        }
        private void CreateAveragedFieldFromElementFaceHistory(string fieldName,
                                                               HistoryResultComponent historyResultComponent,
                                                               bool areaAverage)
        {
            if (historyResultComponent != null)
            {
                // Sorted time
                double[] sortedTime;
                Dictionary<double, int> timeRowId;
                GetSortedTime(new HistoryResultComponent[] { historyResultComponent }, out sortedTime, out timeRowId);
                Dictionary<double, float[]> timeAvgValues =
                    GetNodalValuesFromElementFaceHistory(historyResultComponent, areaAverage);
                //
                bool newField;
                float[] values;
                Field field;
                FieldData fieldData = new FieldData(fieldName, historyResultComponent.Name, 0, 0);
                int count = 0;
                int[] stepIds = GetAllStepIds();
                int[] stepIncrementIds;
                //
                fieldData.StepType = StepTypeEnum.Static;
                //
                for (int i = 0; i < stepIds.Length; i++)
                {
                    stepIncrementIds = GetStepIncrementIds(stepIds[i]);
                    //
                    for (int j = 0; j < stepIncrementIds.Length; j++)
                    {
                        if (i == 0 && j == 0) continue; // Zero increment - Find all occurrences!!!
                        // Field
                        values = timeAvgValues[sortedTime[count]];
                        fieldData.StepId = stepIds[i];
                        fieldData.StepIncrementId = stepIncrementIds[j];
                        fieldData.Time = (float)sortedTime[count];
                        //
                        newField = false;
                        field = GetField(fieldData);
                        if (field == null)
                        {
                            field = new Field(fieldData.Name);
                            newField = true;
                        }
                        field.AddComponent(new FieldComponent(fieldData.Component, values));
                        if (newField) AddField(fieldData, field);
                        //
                        count++;
                    }
                }
            }
        }
        private double[] GetAllEntryValues(HistoryResultEntries historyEntry, Dictionary<double, int> timeRowId)
        {
            int id;
            double[] values;
            double[] timePoints;
            double[] allValues = new double[timeRowId.Count];
            //
            timePoints = historyEntry.Time.ToArray();
            values = historyEntry.Values.ToArray();
            for (int i = 0; i < timePoints.Length; i++)
            {
                id = timeRowId[timePoints[i]];
                allValues[id] = values[i];
            }
            return allValues;
        }
        private Dictionary<double, float[]> GetNodalValuesFromElementFaceHistory(HistoryResultComponent historyResultComponent,
                                                                                 bool areaAverage)
        {
            // Sorted time
            double[] sortedTime;
            Dictionary<double, int> timeRowId;
            GetSortedTime(new HistoryResultComponent[] { historyResultComponent }, out sortedTime, out timeRowId);
            // Average data structure
            AvgData avgData = new AvgData();
            foreach (var entry in _mesh.Parts)
            {
                avgData.AddRange(entry.Value.Visualization.GetAvgData().Nodes);
            }
            // Get all values data for each node
            int faceId;
            int elementId;
            int count = 0;
            string[] tmp;
            double faceArea;
            double[] faceNormal;
            double[][] historyValues = new double[historyResultComponent.Entries.Count][];
            FeElement element;
            FeFaceName faceName;
            AvgEntryData avgEntryData;
            Dictionary<int, AvgEntryData> valueIdAvgEntryData = new Dictionary<int, AvgEntryData>();
            //
            foreach (var entry in historyResultComponent.Entries)
            {
                tmp = entry.Key.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length == 2 && int.TryParse(tmp[0], out elementId) && int.TryParse(tmp[1], out faceId))
                {
                    switch (faceId)
                    {
                        case 1: faceName = FeFaceName.S1; break;
                        case 2: faceName = FeFaceName.S2; break;
                        case 3: faceName = FeFaceName.S3; break;
                        case 4: faceName = FeFaceName.S4; break;
                        case 5: faceName = FeFaceName.S5; break;
                        case 6: faceName = FeFaceName.S6; break;
                        default: throw new NotSupportedException();
                    }
                    //
                    if (_mesh.Elements.TryGetValue(elementId, out element))
                    {
                        _mesh.GetElementFaceNormalAndArea(elementId, faceName, out faceNormal, out faceArea);
                        //
                        avgEntryData = new AvgEntryData();
                        avgEntryData.NodeIds = element.GetNodeIdsFromFaceName(faceName);
                        avgEntryData.SurfaceId = 0;
                        avgEntryData.ElementId = elementId;
                        avgEntryData.Area = faceArea;
                        //
                        valueIdAvgEntryData.Add(count, avgEntryData);
                        historyValues[count] = GetAllEntryValues(entry.Value, timeRowId);
                    }
                    else if (elementId == -1) // fix for when no history output exists - Find all occurrences!!!
                    {
                        avgEntryData = new AvgEntryData();
                        avgEntryData.NodeIds = new int[0];
                        avgEntryData.SurfaceId = 0;
                        avgEntryData.ElementId = elementId;
                        avgEntryData.Area = 0;
                        //
                        valueIdAvgEntryData.Add(count, avgEntryData);
                        historyValues[count] = GetAllEntryValues(entry.Value, timeRowId);
                    }
                    else throw new NotSupportedException();
                }
                count++;
            }
            // Average
            float[] averagedValues;
            AvgData avgDataClone;
            Dictionary<int, double> averagedNodalValues;
            Dictionary<double, float[]> timeValues = new Dictionary<double, float[]>();
            for (int i = 0; i < sortedTime.Length; i++)
            {
                avgDataClone = new AvgData(avgData);
                //
                for (int j = 0; j < historyValues.Length; j++)
                {
                    avgEntryData = valueIdAvgEntryData[j];
                    for (int k = 0; k < avgEntryData.NodeIds.Length; k++)
                    {
                        avgDataClone.Nodes[avgEntryData.NodeIds[k]].Elements[avgEntryData.ElementId].Data.Add(
                            new Tuple<double, double>(historyValues[j][i], avgEntryData.Area));
                    }
                }
                //
                averagedValues = new float[_nodeIdsLookUp.Count];
                averagedNodalValues = avgDataClone.GetAveragedValues(areaAverage);
                foreach (var entry in averagedNodalValues)
                {
                    averagedValues[_nodeIdsLookUp[entry.Key]] = (float)entry.Value;
                }
                timeValues.Add(sortedTime[i], averagedValues);
            }
            //
            return timeValues;
        }
        // Select slip wear results
        public void KeepOnlySelectedSlipWearResults(OrderedDictionary<int, double> stepIdDuration, int[] slipStepIds,
                                                    SlipWearResultsEnum slipWearResultsToKeep)
        {
            Dictionary<int, float> stepIdMaxTime = GetMaxStepTime();
            //
            KeepOnlySelectedFieldSlipWearResults(slipStepIds, slipWearResultsToKeep);
            KeepOnlySelectedHistorySlipWearResults(stepIdDuration, slipStepIds, slipWearResultsToKeep);
        }
        private void KeepOnlySelectedFieldSlipWearResults(int[] slipStepIds, SlipWearResultsEnum slipWearResultsToKeep)
        {
            HashSet<int> slipStepIdsHash = new HashSet<int>(slipStepIds);
            //
            if (slipWearResultsToKeep == SlipWearResultsEnum.All) { }
            else if (slipWearResultsToKeep == SlipWearResultsEnum.SlipWearSteps)
            {
                OrderedDictionary<FieldData, Field> fields = new OrderedDictionary<FieldData, Field>("Fields");
                //
                foreach (var entry in _fields)
                {
                    if (slipStepIdsHash.Contains(entry.Key.StepId))
                    {
                        fields.Add(entry.Key, entry.Value);
                    }
                }
                _fields = fields;
            }
            else if (slipWearResultsToKeep == SlipWearResultsEnum.LastIncrementOfSlipWearSteps)
            {
                int lastIncrementId;
                OrderedDictionary<FieldData, Field> fields = new OrderedDictionary<FieldData, Field>("Fields");
                //
                foreach (var entry in _fields)
                {
                    if (slipStepIdsHash.Contains(entry.Key.StepId))
                    {
                        lastIncrementId = GetStepIncrementIds(entry.Key.StepId).Last();
                        //
                        if (entry.Key.StepIncrementId == lastIncrementId)
                            fields.Add(entry.Key, entry.Value);
                    }
                }
                _fields = fields;
            }
            else if (slipWearResultsToKeep == SlipWearResultsEnum.LastIncrementOfLastSlipWearStep)
            {
                int lastIncrementId;
                int lastStepId = slipStepIds.Last();
                OrderedDictionary<FieldData, Field> fields = new OrderedDictionary<FieldData, Field>("Fields");
                //
                foreach (var entry in _fields)
                {
                    if (slipStepIdsHash.Contains(entry.Key.StepId))
                    {
                        if (entry.Key.StepId == lastStepId)
                        {
                            lastIncrementId = GetStepIncrementIds(entry.Key.StepId).Last();
                            //
                            if (entry.Key.StepIncrementId == lastIncrementId)
                                fields.Add(entry.Key, entry.Value);
                        }
                    }
                }
                _fields = fields;
            }
            else throw new NotSupportedException();
        }
        private void KeepOnlySelectedHistorySlipWearResults(OrderedDictionary<int, double> stepIdDuration, int[] slipStepIds,
                                                            SlipWearResultsEnum slipWearResultsToKeep)
        {
            HashSet<int> slipStepIdsHash = new HashSet<int>(slipStepIds);
            //
            double sumTime = 0;
            OrderedDictionary<int, double> stepIdStartTime = new OrderedDictionary<int, double>("Step id - step time");
            foreach (var entry in stepIdDuration)
            {
                stepIdStartTime.Add(entry.Key, sumTime);
                sumTime += entry.Value;
            }
            //
            int stepId;
            double startTime;
            double endTime;
            List<double[]> minMaxTimeList = new List<double[]>();
            //
            if (slipWearResultsToKeep == SlipWearResultsEnum.All) 
            {
                minMaxTimeList.Add(new double[] { 0, sumTime });
            }
            else if (slipWearResultsToKeep == SlipWearResultsEnum.SlipWearSteps)
            {
                foreach (var entry in stepIdDuration)
                {
                    stepId = entry.Key;
                    //
                    if (slipStepIdsHash.Contains(stepId))
                    {
                        startTime = stepIdStartTime[stepId];
                        endTime = startTime + entry.Value;
                        minMaxTimeList.Add(new double[] { startTime, endTime });
                    }
                }
            }
            else if (slipWearResultsToKeep == SlipWearResultsEnum.LastIncrementOfSlipWearSteps)
            {
                foreach (var entry in stepIdDuration)
                {
                    stepId = entry.Key;
                    //
                    if (slipStepIdsHash.Contains(stepId))
                    {
                        startTime = stepIdStartTime[stepId];
                        endTime = startTime + entry.Value;
                        minMaxTimeList.Add(new double[] { endTime, endTime });
                    }
                }
            }
            else if (slipWearResultsToKeep == SlipWearResultsEnum.LastIncrementOfLastSlipWearStep)
            {
                stepId = slipStepIds.Last();
                //
                if (slipStepIdsHash.Contains(stepId))
                {
                    startTime = stepIdStartTime[stepId];
                    endTime = startTime + stepIdDuration[stepId];
                    minMaxTimeList.Add(new double[] { endTime, endTime });
                }
            }
            else throw new NotSupportedException();
            //
            double[][] minMaxTime = minMaxTimeList.ToArray();
            //
            List<string> entryNamesToRemove = new List<string>();
            foreach (var setEntry in _history.Sets)
            {
                foreach (var fieldEntry in setEntry.Value.Fields)
                {
                    foreach (var componentEntry in fieldEntry.Value.Components)
                    {
                        entryNamesToRemove.Clear();
                        //
                        foreach (var entry in componentEntry.Value.Entries)
                        {
                            entry.Value.KeepOnly(minMaxTime);
                            if (entry.Value.Time.Count == 0) entryNamesToRemove.Add(entry.Key);
                        }
                        // Remove empty
                        foreach (var entryName in entryNamesToRemove) componentEntry.Value.Entries.Remove(entryName);
                    }
                }
            }
        }
        // Superposition
        public void TestSuperposition()
        {
            FeNodeSet nodeSet;
            int[] nodeIds = null;
            if (_mesh.NodeSets.TryGetValue("Node_Set-1", out nodeSet))
            {
                nodeIds = new int[nodeSet.Labels.Length];
                for (int i = 0; i < nodeSet.Labels.Length; i++) nodeIds[i] = _nodeIdsLookUp[nodeSet.Labels[i]];
                Array.Sort(nodeIds);
            }
            //
            string[] componentNames = new string[] { FOComponentNames.S11,
                                                     FOComponentNames.S22,
                                                     FOComponentNames.S33,
                                                     FOComponentNames.S12,
                                                     FOComponentNames.S23,
                                                     FOComponentNames.S13 };
            //
            FieldData fieldData;
            Field field;
            int[] stepIds = GetAllStepIds();
            float[] componentValues;
            float[] filteredValues;
            float[][][] scv = new float[stepIds.Length][][];
            for (int i = 0; i < stepIds.Length; i++)
            {
                fieldData = new FieldData(FOFieldNames.Stress, "", stepIds[i], 1);
                field = GetField(fieldData);
                scv[i] = new float[componentNames.Length][];
                //
                for (int j = 0; j < componentNames.Length; j++)
                {
                    componentValues = field.GetComponentValues(componentNames[j]);
                    // Filter to node set
                    if (nodeIds != null)
                    {
                        filteredValues = new float[nodeIds.Length];
                        for (int k = 0; k < nodeIds.Length; k++)
                        {
                            filteredValues[k] = componentValues[nodeIds[k]];
                        }
                        componentValues = filteredValues;
                    }
                    //
                    scv[i][j] = componentValues;
                }
            }
            //
            float[][] weights = GetWeightsLC2(stepIds.Length);
            // Generate all possible combinations using efficient list manipulation
            List<List<float>> combinations = new List<List<float>>();
            combinations.Add(new List<float>());
            foreach (float[] list in weights)
            {
                List<List<float>> tempCombinations = new List<List<float>>();

                foreach (List<float> combination in combinations)
                {
                    foreach (float value in list)
                    {
                        List<float> newCombination = new List<float>(combination);
                        newCombination.Add(value);
                        tempCombinations.Add(newCombination);
                    }
                }
                combinations = tempCombinations;
            }
            //
            int count = 0;
            List<float> maxCombination;
            //
            Field resultField;
            object myLock = new object();
            //
            List<float>[] combinationsArray = combinations.ToArray();
            float[] maxMisses = new float[combinationsArray.Length];
            int[] indices = new int[combinationsArray.Length];
            Parallel.For(0, combinationsArray.Length, new ParallelOptions { MaxDegreeOfParallelism = 16 }, combNum =>
            //foreach (var combination in combinations)
            {
                float mises = 0;
                float maxMises = 0;
                float wi;
                float[] w;
                float[] valuesJ;
                float[] scvIJ;
                float[][] values;
                //
                w = combinationsArray[combNum].ToArray();
                values = new float[componentNames.Length][];
                //
                for (int i = 0; i < componentNames.Length; i++) values[i] = new float[scv[0][i].Length];
                //
                for (int i = 0; i < stepIds.Length; i++)
                {
                    wi = w[i];
                    for (int j = 0; j < componentNames.Length; j++)
                    {
                        valuesJ = values[j];
                        scvIJ = scv[i][j];
                        for (int k = 0; k < scvIJ.Length; k++)
                        {
                            valuesJ[k] += wi * scvIJ[k];
                        }
                    }
                }
                //
                float a, b, c;
                for (int i = 0; i < values[0].Length; i++)
                {
                    a = values[0][i] - values[1][i];
                    b = values[1][i] - values[2][i];
                    c = values[2][i] - values[0][i];
                    mises = (float)Math.Sqrt(0.5f * (a * a + b * b + c * c +
                        6 * (values[3][i] * values[3][i] + values[4][i] * values[4][i] + values[5][i] * values[5][i])));
                    //
                    {
                        if (mises > maxMises)
                        {
                            maxMises = mises;
                        }
                    }
                }
                indices[combNum] = combNum;
                maxMisses[combNum] = maxMises;
                //
                count++;
                if (count % 1000 == 0) { System.Diagnostics.Debug.WriteLine(count); }
            }
            );
            //
            Array.Sort(maxMisses, indices);
            //
            int index;
            string line;
            List<string> lines = new List<string>();
            for (int i = 0; i < indices.Length; i++)
            {
                index = indices[i];
                line = maxMisses[i].ToString() + ";";
                foreach (var value in combinationsArray[index]) line += value.ToString() + ";";
                lines.Add(line);
            }
            //
            File.WriteAllLines(@"C:\Temp\combinations.csv", lines.ToArray());
            //
            maxCombination = combinationsArray[indices.Last()];
            float maxMisesAll = maxMisses.Last();
            maxMisesAll = maxMisses.Last();
        }

        private float[][] GetWeightsLC1(int numSteps)
        {
            float[][] weights = new float[numSteps][];
            //
            weights[0] = new float[] { 1.1f };                  // pressure
            weights[1] = new float[] { 37000000 };                 // MAy
            weights[2] = new float[] { 10000, -10000 };         // FAx
            weights[3] = new float[] { 20000, -20000 };         // FAy
            weights[4] = new float[] { 43000, -43000 };         // FAz
            weights[5] = new float[] { 3000000, -3000000 };            // MAx
            weights[6] = new float[] { 21000000, -21000000 };         // MAz
            weights[7] = new float[] { 10000, -10000 };         // FCx
            weights[8] = new float[] { 5000, -5000 };           // FCy
            weights[9] = new float[] { 9000, -9000 };           // FCz
            weights[10] = new float[] { 3000000, -3000000 };         // MCx
            weights[11] = new float[] { 3100000, -3100000 };          // MCy
            weights[12] = new float[] { 1000000, -1000000 };          // MCz
            weights[13] = new float[] { 2000, -2000 };          // FDx
            weights[14] = new float[] { 1400, -1400 };          // FDy
            weights[15] = new float[] { 1400, -1400 };          // FDz
            weights[16] = new float[] { 700000, -700000 };            // MDx
            weights[17] = new float[] { 700000, -700000 };            // MDy
            weights[18] = new float[] { 500000, -500000 };            // MDz
            //
            return weights;
        }
        private float[][] GetWeightsLC2(int numSteps)
        {
            float[][] weights = new float[numSteps][];
            //
            weights[0] = new float[] { 1.1f };                  // pressure
            weights[1] = new float[] { 10000, -10000 };         // FCx
            weights[2] = new float[] { 5000, -5000 };           // FCy
            weights[3] = new float[] { 9000, -9000 };           // FCz
            weights[4] = new float[] { 2000, -2000 };          // FDx
            weights[5] = new float[] { 1400, -1400 };          // FDy
            weights[6] = new float[] { 1400, -1400 };          // FDz
            weights[7] = new float[] { 103000 };               // FEz
            weights[8] = new float[] { 3000000, -3000000 };         // MCx
            weights[9] = new float[] { 3100000, -3100000 };          // MCy
            weights[10] = new float[] { 1000000, -1000000 };          // MCz
            weights[11] = new float[] { 700000, -700000 };            // MDx
            weights[12] = new float[] { 700000, -700000 };            // MDy
            weights[13] = new float[] { 500000, -500000 };            // MDz
            //
            return weights;
        }

        public void TestLineProbe(int stepId, int incrementId)
        {
            FieldData fieldData;
            fieldData = new FieldData(FOFieldNames.Stress, FOComponentNames.S11, stepId, incrementId);
            FeElementSet allElements = new FeElementSet("all", _mesh.Elements.Keys.ToArray());
            PartExchangeData s11 = GetSetNodesCellsAndValues(allElements, fieldData);
            //


            


        }
    }

    public static class ListExtensions
    {
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
