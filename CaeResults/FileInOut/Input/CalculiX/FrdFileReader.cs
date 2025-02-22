﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using System.Globalization;

namespace CaeResults
{
    [Serializable]
    public static class FrdFileReader
    {
        static Dictionary<string, string> componentNameMap = new Dictionary<string, string>()
        {
            { "D1", FOComponentNames.U1},
            { "D2", FOComponentNames.U2},
            { "D3", FOComponentNames.U3},
            //
            { "SXX", FOComponentNames.S11},
            { "SYY", FOComponentNames.S22},
            { "SZZ", FOComponentNames.S33},
            { "SXY", FOComponentNames.S12},
            { "SYZ", FOComponentNames.S23},
            { "SZX", FOComponentNames.S13},
            //
            { "MEXX", FOComponentNames.ME11},
            { "MEYY", FOComponentNames.ME22},
            { "MEZZ", FOComponentNames.ME33},
            { "MEXY", FOComponentNames.ME12},
            { "MEYZ", FOComponentNames.ME23},
            { "MEZX", FOComponentNames.ME13},
            //
            { "EXX", FOComponentNames.E11},
            { "EYY", FOComponentNames.E22},
            { "EZZ", FOComponentNames.E33},
            { "EXY", FOComponentNames.E12},
            { "EYZ", FOComponentNames.E23},
            { "EZX", FOComponentNames.E13},
            //
            { "TEM(%)", "TEM"},
            { "STR(%)", "STR"}
        };
        private static readonly string _materialName = "MATERIAL_";
        private static readonly string _gapMaterialName = "MATERIAL_GAP";
        private static readonly string _springMaterialName = "MATERIAL_SPRING";
        //
        private static readonly int _gapMaterialId = -1;
        private static readonly int _springMaterialId = -2;
        //
        private static HashSet<int[]> _bemaNodeIds;


        // Methods                                                                                                                  
        static public FeResults Read(string fileName)
        {
            if (fileName != null && File.Exists(fileName))
            {
                if (fileName.ToUpper().Contains("RESULTSFORLASTITERATIONS.FRD"))
                {
                    CompareIntArray comparer = new CompareIntArray();
                    _bemaNodeIds = new HashSet<int[]>(comparer);
                }
                //
                string[] lines = Tools.ReadAllLines(fileName);
                if (lines == null) return null;
                //
                List<string[]> dataSets = GetDataSets(lines);
                //
                string setID;
                Dictionary<int, FeNode> nodes = null;
                Dictionary<int, int> nodeIdsLookUp = null;
                Dictionary<int, FeElement> elements = null;
                HashSet<int> gapMaterialIds = null;
                HashSet<int> springMaterialIds = null;
                Dictionary<int, string> materialIdMaterialName = null;
                Dictionary<int, List<int>> materialIdElementIds = null;
                //
                FeResults result = new FeResults(fileName, null);
                Field field;
                FieldData fieldData = null;
                FieldData prevFieldData = null;
                //
                bool constantWidth = true;
                //
                foreach (string[] dataSet in dataSets)
                {
                    setID = dataSet[0];
                    if (setID.StartsWith("    1C", StringComparison.Ordinal))  // Data
                    {
                        result.HashName = GetHashName(dataSet);
                        result.DateTime = GetDateTime(dataSet);
                        result.UnitSystem = GetUnitSystem(dataSet); // calls SetConverterUnits on the UnitSystem
                        GetMaterialIdsAndNames(dataSet, out materialIdMaterialName, out gapMaterialIds, out springMaterialIds);
                    }
                    else if (setID.StartsWith("    2C", StringComparison.Ordinal)) // Nodes
                    {
                        constantWidth = IsConstantWidth(dataSet);
                        nodes = GetNodes(dataSet, constantWidth, out nodeIdsLookUp);
                    }
                    else if (setID.StartsWith("    3C", StringComparison.Ordinal)) // Elements
                    {
                        elements = GetElements(dataSet, gapMaterialIds, springMaterialIds, out materialIdElementIds);
                        //elements = GetElementsFast(dataSet));
                    }
                    else if (setID.StartsWith("    1PSTEP", StringComparison.Ordinal)) // Fields
                    {
                        GetField(dataSet, constantWidth, prevFieldData, nodeIdsLookUp, out fieldData, out field);
                        result.AddField(fieldData, field);
                        prevFieldData = fieldData.DeepClone();
                    }
                }
                //
                result.Preprocess();
                //
                if (nodes != null && elements != null)
                {
                    RemoveErrorElements(nodes, ref elements);
                    //
                    FeMesh mesh = new FeMesh(nodes, elements, MeshRepresentation.Results);
                    mesh.ResetPartsColor();
                    result.SetMesh(mesh, nodeIdsLookUp);
                    // Material element sets
                    FeElementSet elementSet;
                    if (materialIdMaterialName != null && materialIdElementIds != null &&
                        materialIdMaterialName.Count <= materialIdElementIds.Count)
                    {
                        foreach (var entry in materialIdMaterialName)
                        {
                            if (materialIdElementIds.ContainsKey(entry.Key))
                            {
                                elementSet = new FeElementSet(entry.Value, materialIdElementIds[entry.Key].ToArray());
                                result.Mesh.AddElementSet(elementSet);
                            }
                        }
                    }
                    // Gap element sets
                    BasePart[] modifiedParts;
                    BasePart[] newParts;
                    List<string> partNamesToRemove = new List<string>();
                    string oldPartName;
                    if (gapMaterialIds.Count > 0 && materialIdElementIds.ContainsKey(_gapMaterialId))
                    {
                        elementSet = new FeElementSet(_gapMaterialName, materialIdElementIds[_gapMaterialId].ToArray());
                        result.Mesh.AddElementSet(elementSet);
                        result.Mesh.CreatePartsFromElementSets(new string[] { elementSet.Name }, out modifiedParts, out newParts);
                        // Rename parts
                        if (newParts.Count() == 1)
                        {
                            oldPartName = newParts[0].Name;
                            newParts[0].Name = "Gap_elements";
                            result.Mesh.Parts.Replace(oldPartName, newParts[0].Name, newParts[0]);
                        }
                        // Get part names to remove
                        foreach (var part in modifiedParts)
                        {
                            if (part.Labels.Length == 0) partNamesToRemove.Add(part.Name);
                        }
                        // Remove parts
                        if (partNamesToRemove.Count > 0) result.Mesh.RemoveParts(partNamesToRemove.ToArray(), out _, false);
                    }
                    // Spring element sets - *SPRING elements are not written into the frd file
                    if (springMaterialIds.Count > 0 && materialIdElementIds.ContainsKey(_springMaterialId))
                    {
                        elementSet = new FeElementSet(_springMaterialName, materialIdElementIds[_springMaterialId].ToArray());
                        result.Mesh.AddElementSet(elementSet);
                    }
                    //
                    return result;
                }
            }
            //
            return null;
        }
        static bool IsConstantWidth(string[] lines)
        {
            // sample frd lines
            // ccx 2.12 mkl
            // -1         1-1.35800E+03-3.18000E+02-9.99201E-13
            // -1      1300 7.15000E+02-3.19150E+02 8.66850E+02
            //
            // ccx 2.10 kwip
            // -1         1-1.35800E+03-3.18000E+02-9.99201E-13
            //
            // ccx 2.12 kwip
            // -1         1-1.35800E+03-3.18000E+02-9.99201E-13
            // -1      1300 7.15000E+02-3.19150E+02 8.66850E+02
            //
            // ccx 2.13 kwip
            // -1         1-1.35800E+03-3.18000E+02-9.99201E-13
            // -1      1300 7.15000E+02-3.19150E+02 8.66850E+02
            //
            // ccx 2.10 CalculiX-GE-OSS-2.10-win-x64
            // -1         1-1.35800E+003-3.18000E+002-9.99201E-013
            // -1      13007.15000E+002-3.19150E+0028.66850E+002
            // -1     160327.95006E+0024.15000E+0021.50000E+002
            //
            // line 0 is the line with the SetID, number of nodes ...
            //

            int len = lines[1].Length;
            for (int i = 2; i < lines.Length; i++)
            {
                if (lines[i].Length != len)
                    return false;
            }
            return true;


            int start = 13;
            string values = lines[1].Substring(start, lines[1].Length - start);
            if (values.Contains(' ')) return true;
            if (values[0] == ' ' && values[12] == ' ' && values[24] == ' ') return true;
            return false;
        }
        static private DateTime GetDateTime(string[] lines)
        {
            DateTime dateTime = new DateTime();
            DateTime time = new DateTime();
            string[] tmp;
            //
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("1UDATE"))
                {
                    tmp = lines[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    dateTime = DateTime.Parse(tmp[1]);
                }
                else if (lines[i].Contains("1UTIME"))
                {
                    tmp = lines[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    time = DateTime.Parse(tmp[1]);
                }
            }
            //
            dateTime = dateTime.AddHours(time.Hour);
            dateTime = dateTime.AddMinutes(time.Minute);
            dateTime = dateTime.AddSeconds(time.Second);
            //
            return dateTime;
        }
        static private string GetHashName(string[] lines)
        {
            string[] tmp;
            //
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].ToUpper().Contains("HASH"))
                {
                    tmp = lines[i].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in tmp)
                    {
                        if (part.ToUpper().Contains("HASH"))
                        {
                            tmp = part.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                            return tmp[1].Trim();
                        }
                    }
                
                }
            }
            //
            return Tools.GetRandomString(8);
        }
        static private UnitSystem GetUnitSystem(string[] lines)
        {
            string[] tmp;
            UnitSystemType unitSystemType = UnitSystemType.Undefined;
            //
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].ToUpper().Contains("UNIT SYSTEM"))
                {
                    tmp = lines[i].Split(new string[] { "Unit system:" }, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length == 2) Enum.TryParse(tmp[1], out unitSystemType);
                }
            }
            //
            return new UnitSystem(unitSystemType);
        }
        static private void GetMaterialIdsAndNames(string[] lines, out Dictionary<int, string> materialIdMaterialName,
                                                   out HashSet<int> gapMaterialIds, out HashSet<int> springMaterialIds)
        {
            //    1UMAT    1S235
            //    1UMAT    2SPRING
            //    1UMAT    3GAP
            int materialId;
            string materialName;
            string[] tmp;
            materialIdMaterialName = new Dictionary<int, string>();
            gapMaterialIds = new HashSet<int>();
            springMaterialIds = new HashSet<int>();
            //
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].ToUpper().Contains("1UMAT"))
                {
                    tmp = lines[i].Split(new string[] { "1UMAT" }, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length == 2)
                    {
                        materialId = int.Parse(tmp[1].Substring(0, 5));
                        materialName = _materialName + tmp[1].Substring(5, tmp[1].Length - 5).Trim();
                        //
                        if (materialName == _gapMaterialName) gapMaterialIds.Add(materialId);
                        else if (materialName == _springMaterialName) springMaterialIds.Add(materialId);
                        else materialIdMaterialName.Add(materialId, materialName);
                    }
                }
            }
        }
        static private List<string[]> GetDataSets(string[] lines)
        {
            List<string> dataSet = new List<string>();
            List<string[]> dataSets = new List<string[]>();
            //
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == " -3")
                {
                    dataSets.Add(dataSet.ToArray());
                    dataSet = new List<string>();
                }
                // Fast check for lines starting with: "    2C" or "    3C"
                else if (lines[i].Length > 5 &&
                         lines[i][5] == 'C' &&
                         (lines[i][4] == '2' || lines[i][4] == '3') &&
                         lines[i][3] == ' ' && lines[i][2] == ' ' && lines[i][1] == ' ' && lines[i][0] == ' ' &&
                         dataSet.Count > 0)
                {
                    dataSets.Add(dataSet.ToArray());
                    dataSet = new List<string>() { lines[i] };
                }
                else
                    dataSet.Add(lines[i]);
            }
            //
            return dataSets;
        }
        static private Dictionary<int, FeNode> GetNodes(string[] lines, bool constantWidth, out Dictionary<int, int> nodeIdsLookUp)
        {
            Dictionary<int, FeNode> nodes = new Dictionary<int, FeNode>();
            nodeIdsLookUp = new Dictionary<int, int>();
            int id;
            FeNode node;
            int width;
            string[] splitter = new string[] { " " };

            int start;

            if (constantWidth)
            {
                width = 12;
                // line 0 is the line with the SetID, number of nodes ...
                for (int i = 1; i < lines.Length; i++)
                {
                    start = 3;
                    id = int.Parse(lines[i].Substring(start, 10));
                    start += 10;
                    node = new FeNode();
                    node.Id = id;
                    node.X = double.Parse(lines[i].Substring(start, width));
                    start += width;
                    node.Y = double.Parse(lines[i].Substring(start, width));
                    start += width;
                    node.Z = double.Parse(lines[i].Substring(start, width));

                    nodes.Add(id, node);
                    nodeIdsLookUp.Add(id, i - 1);
                }
            }
            else
            {
                // line 0 is the line with the SetID, number of nodes ...
                for (int i = 1; i < lines.Length; i++)
                {
                    start = 3;
                    id = int.Parse(lines[i].Substring(start, 10));
                    start += 10;
                    node = new FeNode();
                    node.Id = id;

                    if (lines[i][start] == '-') width = 13;
                    else width = 12;
                    node.X = double.Parse(lines[i].Substring(start, width));
                    start += width;

                    if (lines[i][start] == '-') width = 13;
                    else width = 12;
                    node.Y = double.Parse(lines[i].Substring(start, width));
                    start += width;

                    if (lines[i][start] == '-') width = 13;
                    else width = 12;
                    node.Z = double.Parse(lines[i].Substring(start, width));

                    nodes.Add(id, node);
                    nodeIdsLookUp.Add(id, i - 1);
                }
            }


            return nodes;
        }
        static private Dictionary<int, FeElement> GetElements(string[] lines, HashSet<int> gapMaterialIds,
                                                              HashSet<int> springMaterialIds,
                                                              out Dictionary<int, List<int>> materialIdElementIds)
        {
            //        el.id   type       ?   mat.id      
            // -1      1413      3       0        1      
            //           n1        n1        n3        n4
            // -2       483       489       481       482
            Dictionary<int, FeElement> elements = new Dictionary<int, FeElement>();
            List<int> elementIds;
            materialIdElementIds = new Dictionary<int, List<int>>();
            int id;
            FrdFeDescriptorId feDescriptorId;
            FeElement element;
            int materialID;
            int[] nodeIds;
            string[] record1;
            string[] record2;
            string[] splitter = new string[] { " " };
            // Line 0 is the line with the SetID, number of elements
            for (int i = 1; i < lines.Length; i++)
            {
                record1 = lines[i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                id = int.Parse(record1[1]);
                feDescriptorId = (FrdFeDescriptorId)int.Parse(record1[2]);
                if (record1.Length == 5) materialID = int.Parse(record1[4]);    // ResultsForLastIterations has different output 
                else materialID = -1;
                //
                if (gapMaterialIds.Contains(materialID)) materialID = _gapMaterialId;
                else if (springMaterialIds.Contains(materialID)) materialID = _springMaterialId;
                //
                if (materialIdElementIds.TryGetValue(materialID, out elementIds)) elementIds.Add(id);
                else materialIdElementIds.Add(materialID, new List<int> { id });
                //
                switch (feDescriptorId)
                {
                    // LINEAR ELEMENTS                                                                                              
                    case FrdFeDescriptorId.BeamLinear:
                        // Beam element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        if (TryGetLinearBeamElement(id, record1, out element))
                        {
                            if (_bemaNodeIds != null)
                            {
                                nodeIds = element.NodeIds;
                                Array.Sort(nodeIds);
                                if (_bemaNodeIds.Contains(nodeIds))
                                    break;
                                else _bemaNodeIds.Add(nodeIds);
                            }
                            elements.Add(id, element);
                        }
                        break;
                    case FrdFeDescriptorId.ShellLinearTriangle:
                        // Triangle element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetLinearTriangleElement(id, record1);
                        elements.Add(id, element);
                        break;
                    case FrdFeDescriptorId.ShellLinearQuadrilateral:
                        // Quadrilateral element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetLinearQuadrilateralElement(id, record1);
                        elements.Add(id, element);
                        break;
                    case FrdFeDescriptorId.SolidLinearTetrahedron:
                        // Tetrahedron element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetLinearTetraElement(id, record1);
                        elements.Add(id, element);
                        break;
                    case FrdFeDescriptorId.SolidLinearWedge:
                        // Wedge element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetLinearWedgeElement(id, record1);
                        elements.Add(id, element);
                        break;
                    case FrdFeDescriptorId.SolidLinearHexahedron:
                        // Hexahedron element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetLinearHexaElement(id, record1);
                        elements.Add(id, element);
                        break;

                    // PARABOLIC ELEMENTS                                                                                           

                    case FrdFeDescriptorId.ShellParabolicTriangle:
                        // Triangle element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetParabolicTriangleElement(id, record1);
                        elements.Add(id, element);
                        break;
                    case FrdFeDescriptorId.ShellParabolicQuadrilateral:
                        // Quadrilateral element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetParabolicQuadrilateralElement(id, record1);
                        elements.Add(id, element);
                        break;
                    case FrdFeDescriptorId.SolidParabolicTetrahedron:
                        // Tetrahedron element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetParabolicTetraElement(id, record1);
                        elements.Add(id, element);
                        break;
                    case FrdFeDescriptorId.SolidParabolicWedge:
                        // Wedge element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        record2 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetParabolicWedgeElement(id, record1, record2);
                        elements.Add(id, element);
                        break;
                    case FrdFeDescriptorId.SolidParabolicHexahedron:
                        // Hexahedron element
                        record1 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        record2 = lines[++i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                        element = GetParabolicHexaElement(id, record1, record2);
                        elements.Add(id, element);
                        break;
                    default:
                        throw new Exception("The element type " + feDescriptorId.ToString() + " is not supported.");
                }
            }
            //
            return elements;
        }
        static private void RemoveErrorElements(Dictionary<int, FeNode> nodes, ref Dictionary<int, FeElement> elements)
        {
            List<int> elementsIdsToRemove = new List<int>();
            foreach (var entry in elements)
            {
                foreach (var nodeId in entry.Value.NodeIds)
                {
                    if (!nodes.ContainsKey(nodeId))
                    {
                        elementsIdsToRemove.Add(entry.Key);
                        break;
                    }
                }
            }
            foreach (var elementId in elementsIdsToRemove) elements.Remove(elementId);
        }
        static private void GetField(string[] lines, bool constantWidth, FieldData prevFieldData, Dictionary<int, int> nodeIdsLookUp,
                                     out FieldData fieldData, out Field field)
        {
            int lineNum = 0;
            int numOfVal;
            List<string> components;
            //
            GetFieldHeaderData(lines, ref lineNum, prevFieldData, out fieldData, out numOfVal);
            float[][] values = GetFieldValuesData(lines, ref lineNum, constantWidth, numOfVal, nodeIdsLookUp, out components);
            //
            field = new Field(fieldData.Name);
            for (int i = 0; i < components.Count; i++)
            {
                field.AddComponent(components[i], values[i]);
            }
        }

        static private void GetFieldHeaderData(string[] lines, ref int lineNum, FieldData prevFieldData,
                                               out FieldData fieldData, out int numOfVal)
        {
            {

                // STATIC STEP

                // 1. Record:
                // Format:(1X,' 100','C',6A1,E12.5,I12,20A1,I2,I5,10A1,I2)
                // Values: KEY,CODE,SETNAME,VALUE,NUMNOD,TEXT,ICTYPE,NUMSTP,ANALYS,
                //         FORMAT
                // Where: KEY    = 100
                //        CODE   = C
                //        SETNAME= Name (not used)
                //        VALUE  = Could be frequency, time or any numerical value
                //        NUMNOD = Number of nodes in this nodal results block
                //        TEXT   = Any text
                //        ICTYPE = Analysis type
                //                 0  static
                //                 1  time step
                //                 2  frequency
                //                 3  load step
                //                 4  user named
                //        NUMSTP = Step number
                //        ANALYS = Type of analysis (description)
                //        FORMAT = Format indicator
                //                 0  short format
                //                 1  long format 
                //                 2  binary format 


                //                              field#  stepIncrement#       step#            - COMMENTS
                //    1PSTEP                        25               1           2           
                //                    time numOfvalues              methodId increment#       - COMMENTS
                //  100CL  107 2.000000000         613                     0          7     1

                //    1PSTEP                        25           1           2               
                //  100CL  107 2.000000000         613                     0    7           1
                // -4  DISP        4    1                                                    
                // -5  D1          1    2    1    0                                          
                // -5  D2          1    2    2    0                                          
                // -5  D3          1    2    3    0                                          
                // -5  ALL         1    2    0    0    1ALL                                  
                // -1         1-9.27159E-04 1.48271E-04-1.46752E-04                          

                // FREQUENCY STEP

                //    1PSTEP                         5           1           2               
                //    1PGM                1.000000E+00                                       
                //    1PGK                7.155923E+05                                       
                //    1PHID                         -1                                       
                //    1PSUBC                         0                                       
                //    1PMODE                         1                            - frequency
                //  100CL  102 1.34633E+02        1329                     2    2MODAL      1
                // -4  DISP        4    1                                                    
                // -5  D1          1    2    1    0                                          
                // -5  D2          1    2    2    0                                          
                // -5  D3          1    2    3    0                                          
                // -5  ALL         1    2    0    0    1ALL                                  

                // BUCKLING STEP

                //                              field#  stepIncrement#       step#            - COMMENTS
                //    1PSTEP                         4           1           1               
                //  100CL  104  93.9434614        1329                     4    4           1
                //  -4  DISP        4    1                                                   
                //  -5  D1          1    2    1    0                                         
                //  -5  D2          1    2    2    0                                         
                //  -5  D3          1    2    3    0                                         
                //  -5  ALL         1    2    0    0    1ALL                                 

                // STEADY STATE DYNAMICS STEP

                //    1PSTEP                         3           0           2               
                //  100CL  103 0.00000E+00          21                     1    3           1
                // -4  DISP        4    1                                                    
                // -5  D1          1    2    1    0                                          
                // -5  D2          1    2    2    0                                          
                // -5  D3          1    2    3    0                                          
                // -5  ALL         1    2    0    0    1ALL                                  

                // LAST ITERATIONS

                //    1PSTEP              STP 2INC  12                                       
                //                                                     iteration#             - COMMENTS
                //  100CL  1051.0000000000        2251                     3    5           1
                // -4  DISP        4    1                                                    
                // -5  D1          1    2    1    0                                          
                // -5  D2          1    2    2    0                                          
                // -5  D3          1    2    3    0                                          
                // -5  ALL         1    2    0    0    1ALL                                  

                // FREQUENCY SENSITIVITY

                //    1PSTEP                         3           1           2               
                //  100CL  115 0.00000E+00        1393                     3   15           1
                // -4  SENFREQ     2    1                                                    
                // -5  DFDN        1    1    1    0                                          
                // -5  DFDNFIL     1    1    2    0                                          

            }
            //
            string line;
            string[] record;
            string[] splitter = new string[] { " " };
            //
            string name;
            int globalIncrementId = -1;
            StepTypeEnum type = StepTypeEnum.Static;
            float time;
            int stepId;
            int stepIncrementId = -1;
            int methodId;
            //
            record = lines[lineNum++].Split(splitter, StringSplitOptions.RemoveEmptyEntries);       // 1PSTEP
            // Last iterations
            if (record[2].Contains("INC"))
            {
                type = StepTypeEnum.LastIterations;
                stepId = int.Parse(record[record.Length - 1]);
            }
            else
            {
                stepIncrementId = int.Parse(record[2]);
                stepId = int.Parse(record[3]);
            }
            // Find 100C line - user field data
            while (!lines[lineNum].TrimStart().StartsWith("100C", StringComparison.Ordinal)) lineNum++;
            //
            line = lines[lineNum++];
            int position = line.IndexOf("CL") + 2;                                                  // +2 for CL
            line = line.Insert(position + 5, " ");                                                  // +5 for block id
            line = line.Insert(position, " ");
            //
            record = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);                   // 100CL
            globalIncrementId = int.Parse(record[1]) - 100;
            time = float.Parse(record[2]);
            numOfVal = int.Parse(record[3]);
            methodId = int.Parse(record[4]);
            // Steady state dynamics
            if (methodId == 1 && stepIncrementId == 0)
                type = StepTypeEnum.SteadyStateDynamics;
            // Sensitivity
            if (type == StepTypeEnum.Static && methodId == 3)   // method 3 is also used for last iterations type
            {
                if (prevFieldData.StepType == StepTypeEnum.Frequency || prevFieldData.StepType == StepTypeEnum.FrequencySensitivity)
                    type = StepTypeEnum.FrequencySensitivity;
            }
            //
            if (methodId == 4) type = StepTypeEnum.Buckling;                                            // buckling switch
            //
            if (type == StepTypeEnum.Buckling)
                GetStepAndStepIncrementIds(type, 1, globalIncrementId, prevFieldData, out stepId, out stepIncrementId);
            else if (type == StepTypeEnum.SteadyStateDynamics)
                GetStepAndStepIncrementIds(type, 1, globalIncrementId, prevFieldData, out stepId, out stepIncrementId);
            else if (type == StepTypeEnum.FrequencySensitivity)
                GetStepAndStepIncrementIds(type, 0, globalIncrementId, prevFieldData, out stepId, out stepIncrementId);
            // Check for modal analysis
            else if (record.Length > 5 && record[5].Contains("MODAL"))
            {
                type = StepTypeEnum.Frequency;
                record = lines[lineNum - 2].Split(splitter, StringSplitOptions.RemoveEmptyEntries); // 1PMODE
                int freqNum = int.Parse(record[1]);
                stepIncrementId = freqNum;
            }
            else if (type == StepTypeEnum.LastIterations)
            {
                stepIncrementId = int.Parse(record[5]);
            }
            //
            record = lines[lineNum++].Split(splitter, StringSplitOptions.RemoveEmptyEntries);       // -4  DISP
            name = record[1];
            //
            fieldData = new FieldData(name);
            fieldData.GlobalIncrementId = globalIncrementId;
            fieldData.StepType = type;
            fieldData.Time = time;
            fieldData.MethodId = methodId;
            fieldData.StepId = stepId;
            fieldData.StepIncrementId = stepIncrementId;
        }
        static private void GetStepAndStepIncrementIds(StepTypeEnum type, int startIncrementId, int globalIncrementId,
                                                       FieldData prevFieldData, out int stepId, out int stepIncrementId)
        {
            if (prevFieldData == null) // this is the first step
            {
                stepId = 1;
                stepIncrementId = startIncrementId;
            }
            else if (prevFieldData.StepType != type)   // this is the first increment in the new type step
            {
                stepId = prevFieldData.StepId + 1;
                stepIncrementId = startIncrementId;
            }
            else if (globalIncrementId != prevFieldData.GlobalIncrementId)    // this is the new increment in the new type step
            {
                stepId = prevFieldData.StepId;
                stepIncrementId = prevFieldData.StepIncrementId + 1;
            }
            else // this is the same increment
            {
                stepId = prevFieldData.StepId;
                stepIncrementId = prevFieldData.StepIncrementId;
            }
        }
        static private float[][] GetFieldValuesData(string[] lines, ref int lineNum, bool constantWidth, int numOfVal,
                                                    Dictionary<int, int> nodeIdsLookUp, out List<string> components)
        {
            string[] record;
            string[] splitter = new string[] { " " };
            // -4  DISP        4    1
            // -5  D1          1    2    1    0                                          
            // -5  D2          1    2    2    0                                          
            // -5  D3          1    2    3    0                                          
            // -5  ALL         1    2    0    0    1ALL                                  
            string componentName;
            string componentRename;
            components = new List<string>();
            while (lineNum < lines.Length)
            {
                record = lines[lineNum].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                //
                if (record[0] == "-5") 
                {
                    componentName = record[1];
                    if (componentNameMap.TryGetValue(componentName, out componentRename)) componentName = componentRename;
                    components.Add(componentName);
                } 
                else break;
                //
                lineNum++;
            }
            // Check if the line number equals the number of lines
            if (lines.Length - lineNum < numOfVal) numOfVal = lines.Length - lineNum;
            //
            float[][] values = new float[components.Count][];
            for (int i = 0; i < values.Length; i++) values[i] = new float[nodeIdsLookUp.Count];
            //
            bool directIds = nodeIdsLookUp.Count == numOfVal;
            int width;
            int lineNumInt = lineNum;
            int componentsCount = components.Count;
            //
            if (constantWidth)
            {
                // -1         1-2.70834E-01-1.05325E-01-1.05325E-01 6.25207E-02 1.80015E-08-1.33740E-02
                // -1         2-2.80622E-01-9.91633E-02-7.35318E-02-5.78751E-02-1.60376E-03-2.20396E-03
                // -1         3 2.28310E-01 8.87877E-02 8.87877E-02 4.85675E-02-3.63429E-08-2.93555E-03
                // -1         4 2.24848E-01 8.74410E-02 8.74410E-02-4.91735E-02 3.93390E-08-6.85921E-03
                //
                // -1         1 1.00000E+00 1.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -2           0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -2           0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -2           0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -2           0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -1         2 1.00000E+00 1.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -2           0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -2           0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -2           0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                // -2           0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00 0.00000E+00
                width = 12;
                //for (int i = 0; i < numOfVal; i++)
                Parallel.For(0, numOfVal, i =>  
                {
                    int lineId;
                    int nodeId;
                    int nodeValueId;
                    int start;
                    string line;
                    string substring;
                    float value;
                    //
                    lineId = lineNumInt + i;
                    line = lines[lineId];
                    // Node id
                    if (directIds) nodeValueId = i;
                    else
                    {
                        start = 3;
                        nodeId = int.Parse(line.Substring(start, 10));
                        if (!nodeIdsLookUp.TryGetValue(nodeId, out nodeValueId)) nodeValueId = -1;
                    }
                    // Values
                    start = 13;
                    for (int j = 0; j < componentsCount; j++)
                    {
                        // Is line to short - do results continue in the next line?
                        if (start + width > line.Length)
                        {
                            if (lines.Length > lineId + 1 && lines[lineId + 1].Trim().StartsWith("-2", StringComparison.Ordinal))
                            {
                                lineId++;
                                line = lines[lineId];
                                start = 13;
                            }
                            // No continuation found
                            else continue;
                        }
                        //
                        if (nodeValueId != -1)
                        {
                            substring = line.Substring(start, width);
                            if (!float.TryParse(substring, out value)) value = float.NaN;
                            // Try/catch version is a little faster
                            //try { value = float.Parse(substring); }
                            //catch { value = float.NaN; }
                            //
                            values[j][nodeValueId] = value;
                        }
                        start += width;
                    }
                }
                );
            }
            else
            {
                // -1         12.55297E-0036.74788E-003-2.86735E-001
                // -1         29.68149E-0045.40225E-003-2.82345E-001
                // -1         32.35261E-0031.24270E-002-2.97771E-001
                // -1         4-9.84216E-0035.62909E-003-2.73047E-001
                // -1         53.91981E-0031.22846E-002-2.98849E-001
                // -1         6-1.16413E-0027.22403E-003-2.74940E-001
                int lineId;
                int nodeId;
                int nodeValueId;
                int start;
                string line;
                //
                for (int i = 0; i < numOfVal; i++)
                {
                    lineId = i + lineNum;
                    line = lines[lineId];
                    // Node id
                    if (directIds) nodeValueId = i;
                    else
                    {
                        start = 3;
                        nodeId = int.Parse(line.Substring(start, 10));
                        if (!nodeIdsLookUp.TryGetValue(nodeId, out nodeValueId)) nodeValueId = -1;
                    }
                    // Values
                    start = 13;
                    for (int j = 0; j < components.Count; j++)
                    {
                        if (start >= line.Length) continue;
                        //
                        if (line[start] == '-') width = 13;
                        else width = 12;
                        //
                        if (start + width > line.Length) continue;
                        //
                        if (nodeValueId != -1) values[j][nodeValueId] = float.Parse(line.Substring(start, width));
                        start += width;
                    }
                }
            }
            return values;
        }
        
        
        // LINEAR ELEMENTS                                                                                              
        static private bool TryGetLinearBeamElement(int id, string[] record, out FeElement element)
        {
            element = null;
            //
            try
            {
                int[] nodes = new int[2];
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (!int.TryParse(record[i + 1], out nodes[i])) return false;
                }
                element = new LinearBeamElement(id, nodes);
                //
                return true;
            }
            catch
            {
                return false;
            }
        }
        static private LinearTriangleElement GetLinearTriangleElement(int id, string[] record)
        {
            int[] nodes = new int[3];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = int.Parse(record[i + 1]);
            }
            return new LinearTriangleElement(id, nodes);
        }
        static private LinearQuadrilateralElement GetLinearQuadrilateralElement(int id, string[] record)
        {
            int[] nodes = new int[4];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = int.Parse(record[i + 1]);
            }
            return new LinearQuadrilateralElement(id, nodes);
        }
        static private LinearTetraElement GetLinearTetraElement(int id, string[] record)
        {
            int[] nodes = new int[4];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = int.Parse(record[i + 1]);
            }
            return new LinearTetraElement(id, nodes);
        }
        static private LinearWedgeElement GetLinearWedgeElement(int id, string[] record)
        {
            int[] nodes = new int[6];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = int.Parse(record[i + 1]);
            }
            return new LinearWedgeElement(id, nodes);
        }
        static private LinearHexaElement GetLinearHexaElement(int id, string[] record)
        {
            int[] nodes = new int[8];
            //
            nodes[0] = int.Parse(record[1]);
            nodes[1] = int.Parse(record[2]);
            nodes[2] = int.Parse(record[3]);
            nodes[3] = int.Parse(record[4]);
            nodes[4] = int.Parse(record[5]);
            nodes[5] = int.Parse(record[6]);
            nodes[6] = int.Parse(record[7]);
            nodes[7] = int.Parse(record[8]);
            //
            return new LinearHexaElement(id, nodes);
        }


        // PARABOLIC ELEMENTS                                                                                           
        static private ParabolicTriangleElement GetParabolicTriangleElement(int id, string[] record)
        {
            int[] nodes = new int[6];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = int.Parse(record[i + 1]);
            }
            return new ParabolicTriangleElement(id, nodes);
        }
        static private ParabolicQuadrilateralElement GetParabolicQuadrilateralElement(int id, string[] record)
        {
            int[] nodes = new int[8];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = int.Parse(record[i + 1]);
            }
            return new ParabolicQuadrilateralElement(id, nodes);
        }
        static private ParabolicTetraElement GetParabolicTetraElement(int id, string[] record)
        {
            int[] nodes = new int[10];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = int.Parse(record[i + 1]);
            }
            return new ParabolicTetraElement(id, nodes);
        }
        static private ParabolicWedgeElement GetParabolicWedgeElement(int id, string[] record1, string[] record2)
        {
            int[] nodes = new int[15];
            for (int i = 0; i < 9; i++) // first 9 nodes are OK
            {
                nodes[i] = int.Parse(record1[i + 1]);
            }
            // Swap last 3 nodes with forelast 3 nodes
            nodes[9] = int.Parse(record2[3]);
            nodes[10] = int.Parse(record2[4]);
            nodes[11] = int.Parse(record2[5]);
            nodes[12] = int.Parse(record1[10]);
            nodes[13] = int.Parse(record2[1]);
            nodes[14] = int.Parse(record2[2]);
            //
            return new ParabolicWedgeElement(id, nodes);
        }
        static private ParabolicHexaElement GetParabolicHexaElement(int id, string[] record1, string[] record2)
        {
            int[] nodes = new int[20];
            // First 12 nodes are OK
            for (int i = 0; i < 10; i++)
            {
                nodes[i] = int.Parse(record1[i + 1]);
            }
            nodes[10] = int.Parse(record2[1]);
            nodes[11] = int.Parse(record2[2]);
            // Swap last 3 nodes with forelast 3 nodes
            nodes[12] = int.Parse(record2[7]);
            nodes[13] = int.Parse(record2[8]);
            nodes[14] = int.Parse(record2[9]);
            nodes[15] = int.Parse(record2[10]);
            //
            nodes[16] = int.Parse(record2[3]);
            nodes[17] = int.Parse(record2[4]);
            nodes[18] = int.Parse(record2[5]);
            nodes[19] = int.Parse(record2[6]);
            //
            return new ParabolicHexaElement(id, nodes);
        }

        //                                                                                                          


    }
}
