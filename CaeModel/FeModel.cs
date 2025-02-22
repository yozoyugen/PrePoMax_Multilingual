﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using Calculix = FileInOut.Output.Calculix;
using System.IO;
using System.Drawing;
using System.Data;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using static System.Collections.Specialized.BitVector32;
using System.Net.NetworkInformation;
using System.Xml.Linq;
using FileInOut.Input;
using CaeResults;
using System.CodeDom;

namespace CaeModel
{
    [Serializable]
    public class FeModel : ISerializable
    {
        // Variables                                                                                                                
        private string _hashName;                                                               //ISerializable
        private FeMesh _geometry;                                                               //ISerializable
        private FeMesh _mesh;                                                                   //ISerializable
        private EquationParameterCollection _parameters;                                        //ISerializable
        private OrderedDictionary<string, Material> _materials;                                 //ISerializable
        private OrderedDictionary<string, Section> _sections;                                   //ISerializable
        private OrderedDictionary<string, Constraint> _constraints;                             //ISerializable
        private OrderedDictionary<string, SurfaceInteraction> _surfaceInteractions;             //ISerializable
        private OrderedDictionary<string, ContactPair> _contactPairs;                           //ISerializable
        private OrderedDictionary<string, Amplitude> _amplitudes;                               //ISerializable
        private OrderedDictionary<string, InitialCondition> _initialConditions;                 //ISerializable
        private StepCollection _stepCollection;                                                 //ISerializable
        private OrderedDictionary<int[], Calculix.CalculixUserKeyword> _calculixUserKeywords;   //ISerializable
        private ModelProperties _properties;                                                    //ISerializable
        private UnitSystem _unitSystem;                                                         //ISerializable


        // Properties                                                                                                               
        public string Name { get; set; }
        public string HashName { get { return _hashName; } }
        public FeMesh Geometry { get { return _geometry; } }
        public FeMesh Mesh { get { return _mesh; } }
        public EquationParameterCollection Parameters { get { return _parameters; } }
        public OrderedDictionary<string, Material> Materials { get { return _materials; } }
        public OrderedDictionary<string, Section> Sections { get { return _sections; } }
        public OrderedDictionary<string, Constraint> Constraints { get { return _constraints; } }
        public OrderedDictionary<string, SurfaceInteraction> SurfaceInteractions { get { return _surfaceInteractions; } }
        public OrderedDictionary<string, ContactPair> ContactPairs { get { return _contactPairs; } }
        public OrderedDictionary<string, Amplitude> Amplitudes { get { return _amplitudes; } }
        public OrderedDictionary<string, InitialCondition> InitialConditions { get { return _initialConditions; } }
        public StepCollection StepCollection { get { return _stepCollection; } }
        public OrderedDictionary<int[], Calculix.CalculixUserKeyword> CalculixUserKeywords 
        { 
            get { return _calculixUserKeywords; } 
            set 
            {
                _calculixUserKeywords = value;
            } 
        }
        public ModelProperties Properties { get { return _properties; } set { _properties = value; } }
        public UnitSystem UnitSystem
        {
            get { return _unitSystem; }
            set { _unitSystem = value; }
        }


        // Constructors                                                                                                             
        public FeModel(string name, UnitSystem unitSystem, OrderedDictionary<string, EquationParameter> overriddenParameters = null)
        {
            StringComparer sc = StringComparer.OrdinalIgnoreCase;
            //
            Name = name;
            _hashName = Tools.GetRandomString(8);
            _geometry = new FeMesh(MeshRepresentation.Geometry);
            _mesh = new FeMesh(MeshRepresentation.Mesh);
            _parameters = new EquationParameterCollection();
            _materials = new OrderedDictionary<string, Material>("Materials", sc);
            _sections = new OrderedDictionary<string, Section>("Sections", sc);
            _constraints = new OrderedDictionary<string, Constraint>("Constraints", sc);
            _surfaceInteractions = new OrderedDictionary<string, SurfaceInteraction>("Surface Tractions", sc);
            _contactPairs = new OrderedDictionary<string, ContactPair>("Contact Pairs", sc);
            _initialConditions = new OrderedDictionary<string, InitialCondition>("Initial Conditions", sc);
            _amplitudes = new OrderedDictionary<string, Amplitude>("Amplitudes", sc);
            _stepCollection = new StepCollection();
            _properties = new ModelProperties();
            if (unitSystem == null) _unitSystem = new UnitSystem();
            else _unitSystem = unitSystem;
            // Set overridden parameters
            if (overriddenParameters != null)
            {
                foreach (var entry in overriddenParameters) _parameters.AddOverriddenParameter(entry.Key, entry.Value);
            }
            //
            UpdateNCalcParameters();
        }
        public FeModel(SerializationInfo info, StreamingContext context)
        {
            StringComparer sc = StringComparer.OrdinalIgnoreCase;
            // Compatibility for version v.0.6.0
            _surfaceInteractions = new OrderedDictionary<string, SurfaceInteraction>("Surface Tractions", sc);
            _contactPairs = new OrderedDictionary<string, ContactPair>("Contact Pairs", sc);
            // Compatibility for version v.0.7.0
            _unitSystem = new UnitSystem();
            // Compatibility for version v.0.8.0
            _hashName = Tools.GetRandomString(8);
            // Compatibility for version v.1.0.0
            _initialConditions = new OrderedDictionary<string, InitialCondition>("Initial Conditions", sc);
            // Compatibility for version v.1.2.1
            _amplitudes = new OrderedDictionary<string, Amplitude>("Amplitudes", sc);
            // Compatibility for version v.1.4.0
            _parameters = new EquationParameterCollection();
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "<Name>k__BackingField":               // Compatibility for version v.0.5.1
                    case "_name":
                        Name = (string)entry.Value; break;
                    case "_geometry":
                        _geometry = (FeMesh)entry.Value; break;
                    case "_mesh":
                        _mesh = (FeMesh)entry.Value; break;
                    case "_parameters":                         // Compatibility for version v.2.1.11
                        if (entry.Value is OrderedDictionary<string, EquationParameter> dic)
                        {
                            dic.OnDeserialization(null);
                            _parameters = new EquationParameterCollection(dic);
                        }
                        else _parameters = (EquationParameterCollection)entry.Value;
                        break;
                    case "_materials":
                        if (entry.Value is Dictionary<string, Material> md)
                        {
                            // Compatibility for version v.0.5.1
                            md.OnDeserialization(null);
                            _materials = new OrderedDictionary<string, Material>("Materials", md, sc);
                        }
                        else if (entry.Value is OrderedDictionary<string, Material> mod) _materials = mod;
                        else if (entry.Value == null) _materials = null;
                        else throw new NotSupportedException();
                        break;
                    case "_sections":
                        if (entry.Value is Dictionary<string, Section> sd)
                        {
                            // Compatibility for version v.0.5.1
                            sd.OnDeserialization(null);
                            _sections = new OrderedDictionary<string, Section>("Sections", sd, sc);
                        }
                        else if (entry.Value is OrderedDictionary<string, Section> sod) _sections = sod;
                        else if (entry.Value == null) _sections = null;
                        else throw new NotSupportedException();
                        break;
                    case "_constraints":
                        if (entry.Value is Dictionary<string, Constraint> cd)   
                        {
                            // Compatibility for version v.0.5.1
                            cd.OnDeserialization(null);
                            _constraints = new OrderedDictionary<string, Constraint>("Constraints", cd, sc);
                        }
                        else if (entry.Value is OrderedDictionary<string, Constraint> cod) _constraints = cod;
                        else if (entry.Value == null) _constraints = null;
                        else throw new NotSupportedException();
                        break;
                    case "_surfaceInteractions":
                        _surfaceInteractions = (OrderedDictionary<string, SurfaceInteraction>)entry.Value; break;
                    case "_contactPairs":
                        _contactPairs = (OrderedDictionary<string, ContactPair>)entry.Value; break;
                    case "_amplitudes":
                        _amplitudes = (OrderedDictionary<string, Amplitude>)entry.Value; break;
                    case "_initialConditions":
                        _initialConditions = (OrderedDictionary<string, InitialCondition>)entry.Value; break;
                    case "_stepCollection":
                        _stepCollection = (StepCollection)entry.Value; break;
                    case "_calculixUserKeywords":
                        if (entry.Value is Dictionary<int[], Calculix.CalculixUserKeyword> cukd)
                        {
                            // Compatibility for version v.0.5.1
                            cukd.OnDeserialization(null);
                            _calculixUserKeywords = new OrderedDictionary<int[], Calculix.CalculixUserKeyword>("Keywords", cukd);
                        }
                        else if (entry.Value is OrderedDictionary<int[], Calculix.CalculixUserKeyword> cukod)
                            _calculixUserKeywords = cukod;
                        else if (entry.Value == null) _calculixUserKeywords = null;
                        else throw new NotSupportedException();
                        break;
                    case "_properties":
                        _properties = (ModelProperties)entry.Value;
                        break;
                    case "_unitSystem":
                        _unitSystem = (UnitSystem)entry.Value; break;
                    case "_hashName":
                        _hashName = (string)entry.Value; break;
                    default:
                        throw new NotSupportedException();
                }
            }
            //
            _unitSystem.SetConverterUnits();
            //
            _parameters.OnDeserialization(null);    // call it to load the dictionary immediately
            UpdateNCalcParameters();
        }


        // Methods                                                                                                                  
        public static void WriteToBinaryWriter(FeModel model, BinaryWriter bw)
        {
            // Write geometry
            if (model == null || model.Geometry == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                FeMesh.WriteToBinaryWriter(model.Geometry, bw);
            }
            // Write mesh
            if (model == null || model.Mesh == null)
            {
                bw.Write((int)0);
            }
            else
            {
                bw.Write((int)1);
                FeMesh.WriteToBinaryWriter(model.Mesh, bw);
            }
        }
        public static void ReadFromBinaryReader(FeModel model, BinaryReader br, int version)
        {
            // Read geometry
            int geometryExists = br.ReadInt32();
            if (geometryExists == 1)
            {
                FeMesh.ReadFromBinaryReader(model.Geometry, br, version);
            }
            // Read mesh
            int meshExists = br.ReadInt32();
            if (meshExists == 1)
            {
                FeMesh.ReadFromBinaryReader(model.Mesh, br, version);
            }
        }
        // Check                                                                                    
        public string[] CheckValidity(List<Tuple<NamedClass, string>> items)
        {
            // Tuple<NamedClass, string>   ...   Tuple<invalidItem, stepName>
            if (_mesh == null) return new string[0];
            //
            List<string> invalidItems = new List<string>();
            bool valid = false;
            invalidItems.AddRange(_geometry.CheckValidity(items, IsMeshSetupItemProperlyDefined));
            invalidItems.AddRange(_mesh.CheckValidity(items, IsMeshSetupItemProperlyDefined));
            // Materials
            Material material;
            foreach (var entry in _materials)
            {
                material = entry.Value;
                valid = true;
                foreach (var property in material.Properties)
                {
                    // Check equations
                    valid &= property.TryCheckEquations();
                }
                //
                SetItemValidity(null, material, valid, items);
                if (!valid && material.Active) invalidItems.Add("Material: " + material.Name);
            }
            // Sections
            Section section;
            foreach (var entry in _sections)
            {
                section = entry.Value;
                //
                valid = IsSectionRegionValid(section);
                //
                if (section is SolidSection || section is ShellSection || section is MembraneSection)
                    valid &= _materials.ContainsValidKey(section.MaterialName);
                else if (section is PointMassSection || section is DistributedMassSection) { }
                else throw new NotSupportedException();
                // Check equations
                valid &= section.TryCheckEquations();
                //
                SetItemValidity(null, section, valid, items);
                if (!valid && section.Active) invalidItems.Add("Section: " + section.Name);
            }
            // Constraints
            Constraint constraint;
            foreach (var entry in _constraints)
            {
                constraint = entry.Value;
                //
                valid = IsConstraintRegionValid(constraint);
                // Check equations
                valid &= constraint.TryCheckEquations();
                //
                SetItemValidity(null, constraint, valid, items);
                if (!valid && constraint.Active) invalidItems.Add("Constraint: " + constraint.Name);
            }
            // Contact pairs
            ContactPair contactPair;
            foreach (var entry in _contactPairs)
            {
                contactPair = entry.Value;
                valid = _surfaceInteractions.ContainsValidKey(contactPair.SurfaceInteractionName)
                        && _mesh.Surfaces.ContainsValidKey(contactPair.SlaveRegionName)
                        && _mesh.Surfaces.ContainsValidKey(contactPair.MasterRegionName);
                //
                SetItemValidity(null, contactPair, valid, items);
                if (!valid && contactPair.Active) invalidItems.Add("Contact pair: " + contactPair.Name);
            }
            // Initial conditions
            InitialCondition initialCondition;
            foreach (var entry in _initialConditions)
            {
                initialCondition = entry.Value;
                //
                valid = IsInitialConditionRegionValid(initialCondition);
                // Check equations
                valid &= initialCondition.TryCheckEquations();
                //
                SetItemValidity(null, initialCondition, valid, items);
                if (!valid && initialCondition.Active) invalidItems.Add("Initial condition: " + initialCondition.Name);
            }
            // Steps
            HistoryOutput historyOutput;
            BoundaryCondition bc;
            Load load;
            DefinedField definedField;
            //
            foreach (var step in _stepCollection.StepsList)
            {
                // History output
                foreach (var hoEntry in step.HistoryOutputs)
                {
                    historyOutput = hoEntry.Value;
                    //
                    valid = IsHistoryOutputRegionValid(historyOutput);
                    //
                    SetItemValidity(step.Name, historyOutput, valid, items);
                    if (!valid && historyOutput.Active) invalidItems.Add("History output: " + step.Name + ", " + historyOutput.Name);
                }
                // Boundary conditions
                foreach (var bcEntry in step.BoundaryConditions)
                {
                    bc = bcEntry.Value;
                    // Region
                    valid = IsBoundaryConditionRegionValid(bc);
                    // Amplitude
                    if (bc.AmplitudeName != BoundaryCondition.DefaultAmplitudeName &&
                        !_amplitudes.ContainsValidKey(bc.AmplitudeName)) valid = false;
                    // Coordinate system
                    if (bc.CoordinateSystemName != BoundaryCondition.DefaultCoordinateSystemName &&
                        !_mesh.CoordinateSystems.ContainsValidKey(bc.CoordinateSystemName)) valid = false;
                    // Check equations
                    valid &= bc.TryCheckEquations();
                    //
                    SetItemValidity(step.Name, bc, valid, items);
                    if (!valid && bc.Active) invalidItems.Add("Boundary condition: " + step.Name + ", " + bc.Name);
                }
                // Loads
                foreach (var loadEntry in step.Loads)
                {
                    load = loadEntry.Value;
                    // Region
                    valid = IsLoadRegionValid(load);
                    // D2 vs axisymmetric
                    if (valid && load is CentrifLoad cf)
                    {
                        if (_properties.ModelSpace == ModelSpaceEnum.PlaneStress ||
                            _properties.ModelSpace == ModelSpaceEnum.PlaneStrain)
                        {
                            if (cf.Axisymmetric == true) valid = false;
                        }
                        else if (_properties.ModelSpace == ModelSpaceEnum.Axisymmetric)
                        {
                            if (cf.Axisymmetric == false) valid = false;
                        }
                    }
                    // Amplitude
                    if (load.AmplitudeName != Load.DefaultAmplitudeName && 
                        !_amplitudes.ContainsValidKey(load.AmplitudeName)) valid = false;
                    // Coordinate system
                    if (load.CoordinateSystemName != Load.DefaultCoordinateSystemName &&
                        !_mesh.CoordinateSystems.ContainsValidKey(load.CoordinateSystemName)) valid = false;
                    // Check equations
                    valid &= load.TryCheckEquations();
                    //
                    SetItemValidity(step.Name, load, valid, items);
                    if (!valid && load.Active) invalidItems.Add("Load: " + step.Name + ", " + load.Name);
                }
                // Defined fields
                foreach (var fieldEntry in step.DefinedFields)
                {
                    definedField = fieldEntry.Value;
                    //
                    valid = IsDefinedFieldRegionValid(definedField);
                    // Check equations
                    valid &= definedField.TryCheckEquations();
                    //
                    SetItemValidity(step.Name, definedField, valid, items);
                    if (!valid && definedField.Active) invalidItems.Add("Defined field: " + step.Name + ", " + definedField.Name);
                }
            }
            //
            return invalidItems.ToArray();
        }
        private void Check2D(FeMesh mesh)
        {
            // Check for 2D geometry - the same check in ImportGeometry
            if (_properties.ModelSpace.IsTwoD())
            {
                if (!mesh.BoundingBox.Is2D())
                    throw new CaeException("The selected file does not contain 2D geometry in x-y plane.");
                else if (!Check2DSurfaceNormals(mesh))
                {
                    MessageBoxes.ShowWarning("The imported geometry contains faces oriented in the wrong direction (negative Z). " +
                                             "To use them in the analysis, change their orientation.");
                }
            }
        }
        private bool Check2DSurfaceNormals(FeMesh mesh)
        {
            int cellId;
            int[] cell;
            FeNode normal;
            foreach (var entry in mesh.Parts)
            {
                for (int i = 0; i < entry.Value.Visualization.CellIdsByFace.Length; i++)
                {
                    if (entry.Value.Visualization.CellIdsByFace[i].Length > 0)
                    {
                        cellId = entry.Value.Visualization.CellIdsByFace[i][0];
                        cell = entry.Value.Visualization.Cells[cellId];
                        normal = mesh.ComputeNormalFromFaceCellIndices(cell);
                        if (normal.Z < 0) return false;
                    }
                }
            }
            return true;
        }
        public bool IsSectionRegionValid(Section section)
        {
            bool valid;
            //
            if (section is SolidSection ss)
            {
                valid =
                    (ss.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(ss.RegionName))
                     || (ss.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(ss.RegionName));
            }
            else if (section is ShellSection shs)
            {
                valid =
                    (shs.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(shs.RegionName))
                     || (shs.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(shs.RegionName));
            }
            else if (section is MembraneSection ms)
            {
                valid =
                    (ms.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(ms.RegionName))
                     || (ms.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(ms.RegionName));
            }
            else if (section is PointMassSection pms)
            {
                valid =
                    (pms.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(pms.RegionName))
                     || (pms.RegionType == RegionTypeEnum.ReferencePointName &&
                     _mesh.ReferencePoints.ContainsValidKey(pms.RegionName));
            }
            else if (section is DistributedMassSection dms)
            {
                valid = dms.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(dms.RegionName);
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsInitialConditionRegionValid(InitialCondition initialCondition)
        {
            bool valid;
            //
            if (initialCondition is InitialTemperature it)
            {
                valid = (it.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(it.RegionName)) ||
                        (it.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(it.RegionName));
            }
            else if (initialCondition is InitialTranslationalVelocity itv)
            {
                valid = (itv.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(itv.RegionName)) ||
                        (itv.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(itv.RegionName)) ||
                        (itv.RegionType == RegionTypeEnum.ReferencePointName
                         && _mesh.ReferencePoints.ContainsValidKey(itv.RegionName));
            }
            else if (initialCondition is InitialAngularVelocity iav)
            {
                valid = (iav.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(iav.RegionName)) ||
                        (iav.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(iav.RegionName)) ||
                        (iav.RegionType == RegionTypeEnum.ReferencePointName
                         && _mesh.ReferencePoints.ContainsValidKey(iav.RegionName));
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsHistoryOutputRegionValid(HistoryOutput historyOutput)
        {
            bool valid;
            //
            if (historyOutput is NodalHistoryOutput nho)
            {
                valid = (nho.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(nho.RegionName))
                        || (nho.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(nho.RegionName))
                        || (nho.RegionType == RegionTypeEnum.ReferencePointName
                        && _mesh.ReferencePoints.ContainsValidKey(nho.RegionName));
            }
            else if (historyOutput is ElementHistoryOutput eho)
            {
                valid = _mesh.ElementSets.ContainsValidKey(eho.RegionName);
            }
            else if (historyOutput is ContactHistoryOutput cho)
            {
                valid = _contactPairs.ContainsValidKey(cho.RegionName);
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsConstraintRegionValid(Constraint constraint)
        {
            bool valid;
            //
            if (constraint is PointSpring ps)
            {
                valid = (ps.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(ps.RegionName))
                        || (ps.RegionType == RegionTypeEnum.ReferencePointName
                           && _mesh.ReferencePoints.ContainsValidKey(ps.RegionName));
            }
            else if (constraint is SurfaceSpring ss)
            {
                valid = ss.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(ss.RegionName);
            }
            else if (constraint is CompressionOnly co)
            {
                valid = co.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(co.RegionName);
            }
            else if (constraint is RigidBody rb)
            {
                valid = (rb.ReferencePointName != null && _mesh.ReferencePoints.ContainsValidKey(rb.ReferencePointName))
                        && ((rb.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(rb.RegionName))
                        || (rb.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(rb.RegionName)));
            }
            else if (constraint is Tie t)
            {
                valid = _mesh.Surfaces.ContainsValidKey(t.SlaveRegionName) && _mesh.Surfaces.ContainsValidKey(t.MasterRegionName)
                        && (t.SlaveRegionName != t.MasterRegionName);
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsBoundaryConditionRegionValid(BoundaryCondition bc)
        {
            bool valid;
            //
            if (bc is FixedBC fix)
            {
                valid = (fix.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(fix.RegionName))
                        || (fix.RegionType == RegionTypeEnum.SurfaceName && (_mesh.Surfaces.ContainsValidKey(fix.RegionName)))
                        || (fix.RegionType == RegionTypeEnum.ReferencePointName
                        && (_mesh.ReferencePoints.ContainsValidKey(fix.RegionName)));
            }
            else if (bc is DisplacementRotation dr)
            {
                valid = (dr.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(dr.RegionName))
                        || (dr.RegionType == RegionTypeEnum.SurfaceName && (_mesh.Surfaces.ContainsValidKey(dr.RegionName)))
                        || (dr.RegionType == RegionTypeEnum.ReferencePointName
                        && (_mesh.ReferencePoints.ContainsValidKey(dr.RegionName)));
            }
            else if (bc is SubmodelBC sm)
            {
                valid = (sm.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(sm.RegionName))
                        || (sm.RegionType == RegionTypeEnum.SurfaceName && (_mesh.Surfaces.ContainsValidKey(sm.RegionName)));
            }
            else if (bc is TemperatureBC tmp)
            {
                valid = (tmp.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(tmp.RegionName))
                        || (tmp.RegionType == RegionTypeEnum.SurfaceName && (_mesh.Surfaces.ContainsValidKey(tmp.RegionName)));
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsLoadRegionValid(Load load)
        {
            bool valid;
            FeSurface s;
            //
            if (load is CLoad cl)
            {
                valid = (cl.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(cl.RegionName))
                        || (cl.RegionType == RegionTypeEnum.ReferencePointName
                        && (_mesh.ReferencePoints.ContainsValidKey(cl.RegionName)));
            }
            else if (load is MomentLoad ml)
            {
                valid = (ml.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(ml.RegionName))
                        || (ml.RegionType == RegionTypeEnum.ReferencePointName
                        && (_mesh.ReferencePoints.ContainsValidKey(ml.RegionName)));
            }
            else if (load is DLoad dl)
            {
                valid = (_mesh.Surfaces.TryGetValue(dl.SurfaceName, out s) && s.Valid);
            }
            else if (load is HydrostaticPressure hpl)
            {
                valid = (_mesh.Surfaces.TryGetValue(hpl.SurfaceName, out s) && s.Valid);
            }
            else if (load is ImportedPressure ip)
            {
                valid = (_mesh.Surfaces.TryGetValue(ip.SurfaceName, out s) && s.Valid);
            }
            else if (load is STLoad stl)
            {
                valid = (_mesh.Surfaces.TryGetValue(stl.SurfaceName, out s) && s.Valid);
            }
            else if (load is ImportedSTLoad istl)
            {
                valid = (_mesh.Surfaces.TryGetValue(istl.SurfaceName, out s) && s.Valid);
            }
            else if (load is ShellEdgeLoad sel)
            {
                valid = (_mesh.Surfaces.TryGetValue(sel.SurfaceName, out s) && s.Valid);
            }
            else if (load is GravityLoad gl)
            {
                valid = (gl.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(gl.RegionName)) ||
                        (gl.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(gl.RegionName)) ||
                        (gl.RegionType == RegionTypeEnum.MassSection && _sections.ContainsValidKey(gl.RegionName)) ||
                        (gl.RegionType == RegionTypeEnum.Selection && _mesh.GetPartNamesFromPartIds(gl.CreationIds) != null &&
                        _mesh.GetPartNamesFromPartIds(gl.CreationIds).Length == gl.CreationIds.Length);
            }
            else if (load is CentrifLoad cf)
            {
                valid = (cf.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(cf.RegionName)) ||
                        (cf.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsValidKey(cf.RegionName)) ||
                        (cf.RegionType == RegionTypeEnum.MassSection && _sections.ContainsValidKey(cf.RegionName)) ||
                        (cf.RegionType == RegionTypeEnum.Selection && _mesh.GetPartNamesFromPartIds(cf.CreationIds) != null &&
                        _mesh.GetPartNamesFromPartIds(cf.CreationIds).Length == cf.CreationIds.Length);
            }
            else if (load is PreTensionLoad ptl)
            {
                valid = (_mesh.Surfaces.TryGetValue(ptl.SurfaceName, out s) && s.Valid && s.Type == FeSurfaceType.Element);
            }
            // Thermal
            else if (load is CFlux cfl)
            {
                valid = (cfl.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(cfl.RegionName));
            }
            else if (load is DFlux df)
            {
                valid = (_mesh.Surfaces.TryGetValue(df.SurfaceName, out s) && s.Valid);
            }
            else if (load is BodyFlux bf)
            {
                valid = (bf.RegionType == RegionTypeEnum.PartName && _mesh.Parts.ContainsValidKey(bf.RegionName))
                        || (bf.RegionType == RegionTypeEnum.ElementSetName
                        && _mesh.ElementSets.ContainsValidKey(bf.RegionName)
                        || (bf.RegionType == RegionTypeEnum.Selection && _mesh.GetPartNamesFromPartIds(bf.CreationIds) != null &&
                           _mesh.GetPartNamesFromPartIds(bf.CreationIds).Length == bf.CreationIds.Length));
            }
            else if (load is FilmHeatTransfer fht)
            {
                valid = (_mesh.Surfaces.TryGetValue(fht.SurfaceName, out s) && s.Valid);
            }
            else if (load is RadiationHeatTransfer rht)
            {
                valid = (_mesh.Surfaces.TryGetValue(rht.SurfaceName, out s) && s.Valid);
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        public bool IsDefinedFieldRegionValid(DefinedField definedField)
        {
            bool valid;
            //
            if (definedField is DefinedTemperature dt)
            {
                if (dt.Type == DefinedTemperatureTypeEnum.ByValue)
                {
                    valid = (dt.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsValidKey(dt.RegionName)) ||
                            (dt.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsValidKey(dt.RegionName));
                }
                else if (dt.Type == DefinedTemperatureTypeEnum.FromFile)
                {
                    // Defined field created from file needs no selection
                    valid = true;
                }
                else throw new NotSupportedException();
            }
            else throw new NotSupportedException();
            //
            return valid;
        }
        private void SetItemValidity(string stepName, NamedClass item, bool validity, List<Tuple<NamedClass, string>> items)
        {
            if (item.Valid != validity)
            {
                item.Valid = validity;
                items.Add(new Tuple<NamedClass, string>(item, stepName));
            }
        }
        public bool RegionValid(IMultiRegion multiRegion)
        {
            if (multiRegion.RegionType == RegionTypeEnum.NodeSetName && _mesh.NodeSets.ContainsKey(multiRegion.RegionName))
                return _mesh.NodeSets[multiRegion.RegionName].Valid;
            else if (multiRegion.RegionType == RegionTypeEnum.ElementSetName && _mesh.ElementSets.ContainsKey(multiRegion.RegionName))
                return _mesh.ElementSets[multiRegion.RegionName].Valid;
            else if (multiRegion.RegionType == RegionTypeEnum.SurfaceName && _mesh.Surfaces.ContainsKey(multiRegion.RegionName))
                return _mesh.Surfaces[multiRegion.RegionName].Valid;
            //
            return false;
        }
        //
        public int[] GetSectionAssignments(out Dictionary<int, int> elementIdSectionId)
        {
            int sectionId = 0;
            elementIdSectionId = new Dictionary<int, int>();
            //
            foreach (var entry in _sections)
            {
                if (entry.Value is MassSection) continue;
                //
                if (entry.Value.RegionType == RegionTypeEnum.PartName)
                {
                    foreach (var elementId in _mesh.Parts[entry.Value.RegionName].Labels)
                    {
                        if (elementIdSectionId.ContainsKey(elementId)) elementIdSectionId[elementId] = sectionId;
                        else elementIdSectionId.Add(elementId, sectionId);
                    }
                }
                else if (entry.Value.RegionType == RegionTypeEnum.ElementSetName)
                {
                    if (entry.Value is SolidSection)
                    {
                        FeElementSet elementSet = _mesh.ElementSets[entry.Value.RegionName];
                        if (elementSet.CreatedFromParts)
                        {
                            string[] partNames = _mesh.GetPartNamesFromPartIds(elementSet.Labels);
                            foreach (var partName in partNames)
                            {
                                foreach (var elementId in _mesh.Parts[partName].Labels)
                                {
                                    if (elementIdSectionId.ContainsKey(elementId)) elementIdSectionId[elementId] = sectionId;
                                    else elementIdSectionId.Add(elementId, sectionId);
                                }
                            }
                        }
                        else
                        {
                            foreach (var elementId in elementSet.Labels)
                            {
                                if (elementIdSectionId.ContainsKey(elementId)) elementIdSectionId[elementId] = sectionId;
                                else elementIdSectionId.Add(elementId, sectionId);
                            }
                        }
                    }
                    else if (entry.Value is ShellSection || entry.Value is MembraneSection)
                    {
                        FeElementSet elementSet = _mesh.ElementSets[entry.Value.RegionName];
                        foreach (var elementId in elementSet.Labels)
                        {
                            if (elementIdSectionId.ContainsKey(elementId)) elementIdSectionId[elementId] = sectionId;
                            else elementIdSectionId.Add(elementId, sectionId);
                        }
                    }
                    else throw new NotSupportedException();
                }
                else throw new NotSupportedException();
                //
                sectionId++;
            }
            // Not assigned
            IEnumerable<int> unAssignedElementIds = _mesh.Elements.Keys.Except(elementIdSectionId.Keys);
            int[] unAssignedElementIdsArray = new int[unAssignedElementIds.Count()];
            int count = 0;
            foreach (var elementId in unAssignedElementIds)
            {
                elementIdSectionId.Add(elementId, -1);
                unAssignedElementIdsArray[count++] = elementId;
            }
            //
            return unAssignedElementIdsArray;
        }
        public void GetMaterialAssignments(out Dictionary<int, int> elementIdMaterialId)
        {
            // Get element section ids
            Dictionary<int, int> elementIdSectionId;
            GetSectionAssignments(out elementIdSectionId);
            // Get material ids
            int count = 0;
            Dictionary<string, int> materialId = new Dictionary<string, int>();
            foreach (var entry in _materials) materialId.Add(entry.Value.Name, count++);
            // Get a map of section materials
            count = 0;
            Dictionary<int, int> sectionIdMaterialId = new Dictionary<int, int>();
            sectionIdMaterialId.Add(-1, -1);    // add missing section
            foreach (var entry in _sections) sectionIdMaterialId.Add(count++, materialId[entry.Value.MaterialName]);
            // Use the map
            elementIdMaterialId = new Dictionary<int, int>();
            foreach (var entry in elementIdSectionId) elementIdMaterialId.Add(entry.Key, sectionIdMaterialId[entry.Value]);
        }
        public void GetNodalSafetyFactorLimits()
        {
            Dictionary<int, int> elementIdMaterialId;
            GetMaterialAssignments(out elementIdMaterialId);
            //
            int count = 0;
            Dictionary<int, double> materialIdSafetyFactor = new Dictionary<int, double>();
            foreach (var entry in _materials)
            {
                //if (safetyFactor != null) materialIdSafetyFactor.Add(count++, safetyFactor.SafetyFactorLimitValue);
                //else materialIdSafetyFactor.Add(count++, double.NaN);
            }
            //
            Dictionary<int, double> nodeIdSafetyFactorLimit = new Dictionary<int, double>();
        }
        public bool AreSlipWearCoefficientsDefined(out Dictionary<int, double> materialIdCoefficient)
        {
            int count = 0;
            bool containsWear = false;
            double coefficient;
            materialIdCoefficient = new Dictionary<int, double>();
            // For each material check if the material has wear coefficients defined
            foreach (var entry in _materials)
            {
                coefficient = 0;
                foreach (var property in entry.Value.Properties)
                {
                    if (property is SlipWear sw)
                    {
                        coefficient = sw.WearCoefficient.Value / sw.Hardness.Value * _properties.CyclesIncrement;
                        containsWear = true;
                        break;
                    }
                }
                //
                materialIdCoefficient.Add(count++, coefficient);
            }
            return containsWear;
        }
        public Dictionary<int, double> GetNodalSlipWearCoefficients()
        {
            double coefficient;
            Dictionary<int, double> materialIdCoefficient;
            bool containsWear = AreSlipWearCoefficientsDefined(out materialIdCoefficient);
            // If wear coefficients are defined
            if (containsWear)
            {
                Dictionary<int, int> elementIdMaterialId;
                GetMaterialAssignments(out elementIdMaterialId);
                // Get wear coefficient for each element
                Dictionary<int, double> elementIdCoefficient = new Dictionary<int, double>();
                foreach (var entry in elementIdMaterialId)
                {
                    elementIdCoefficient.Add(entry.Key, materialIdCoefficient[entry.Value]);
                }
                // Get wear coefficients for each node
                HashSet<double> allCoefficients;
                Dictionary<int, HashSet<double>> nodeIdAllCoefficients = new Dictionary<int, HashSet<double>>();
                FeElement element;
                foreach (var entry in elementIdCoefficient)
                {
                    element = _mesh.Elements[entry.Key];
                    foreach (var nodeId in element.NodeIds)
                    {
                        if (nodeIdAllCoefficients.TryGetValue(nodeId, out allCoefficients)) allCoefficients.Add(entry.Value);
                        else nodeIdAllCoefficients.Add(nodeId, new HashSet<double>() { entry.Value });
                    }
                }
                // Compute the average
                Dictionary<int, double> nodeIdCoefficient = new Dictionary<int, double>();
                foreach (var entry in nodeIdAllCoefficients)
                {
                    coefficient = 0;
                    foreach (var value in entry.Value) coefficient += value;
                    coefficient /= entry.Value.Count();
                    //
                    nodeIdCoefficient.Add(entry.Key, coefficient);
                }
                //
                return nodeIdCoefficient;
            }
            else return null;
        }
        public void GetSectionThicknessAssignments(out Dictionary<int, int> elementIdSectionThicknessId)
        {
            // Get element section ids
            Dictionary<int, int> elementIdSectionId;
            GetSectionAssignments(out elementIdSectionId);
            // Get thicknesses
            int count = 0;
            double thickness;
            List<int> sectionIds;
            Dictionary<double, List<int>> thicknessSectionIds = new Dictionary<double, List<int>>();
            foreach (var entry in _sections)
            {
                thickness = GetSectionThickness(entry.Value);
                //
                if (thicknessSectionIds.TryGetValue(thickness, out sectionIds)) sectionIds.Add(count);
                else thicknessSectionIds.Add(thickness, new List<int>() { count });
                count++;
            }
            double[] sortedThicknesses = thicknessSectionIds.Keys.ToArray();
            Array.Sort(sortedThicknesses);
            // Get a map of section thicknesses
            Dictionary<int, int> sectionIdSectionThicknessId = new Dictionary<int, int>();
            sectionIdSectionThicknessId.Add(-1, -1);
            if (sortedThicknesses.Length > 0 && sortedThicknesses[0] == -1) count = -1;
            else count = 0;
            foreach (var sortedThickness in sortedThicknesses)
            {
                foreach (var sectionId in thicknessSectionIds[sortedThickness])
                {
                    sectionIdSectionThicknessId.Add(sectionId, count);
                }
                count++;
            }
            // Use the map
            elementIdSectionThicknessId = new Dictionary<int, int>();
            foreach (var entry in elementIdSectionId)
                elementIdSectionThicknessId.Add(entry.Key, sectionIdSectionThicknessId[entry.Value]);
        }
        public double GetSectionThickness(Section section)
        {
            if (_properties.ModelSpace.IsTwoD() && section is SolidSection sos) return sos.Thickness.Value;
            else if (section is ShellSection shs) return shs.Thickness.Value;
            else if (section is MembraneSection ms) return ms.Thickness.Value;
            else return -1;
        }
        //
        public void RemoveLostUserKeywords(Action<int> SetNumberOfUserKeywords)
        {
            try
            {
                FileInOut.Output.CalculixFileWriter.RemoveLostUserKeywords(this);
                SetNumberOfUserKeywords?.Invoke(_calculixUserKeywords.Count);
            }
            catch { }
        }        
        // Import                                                                                   
        public string[] ImportGeometryFromStlFile(string fileName)
        {
            FeMesh mesh = StlFileReader.Read(fileName);
            // Shading
            foreach (var entry in mesh.Parts) entry.Value.SmoothShaded = true;
            //
            string[] addedPartNames = ImportGeometry(mesh, GetReservedPartNames());
            //
            return addedPartNames;
        }        
        public string[] ImportGeometryFromBrepFile(string visFileName, string brepFileName)
        {
            FeMesh mesh = VisFileReader.Read(visFileName);
            //
            if (mesh.Parts.GetValueByIndex(0) is GeometryPart gp)
            {
                gp.CADFileDataFromFile(brepFileName);
                gp.SmoothShaded = true;
            }
            //
            string[] addedPartNames = ImportGeometry(mesh, GetReservedPartNames());
            //
            return addedPartNames;
        }        
        public void ImportMeshFromVolFile(string fileName)
        {
            FeMesh mesh = VolFileReader.Read(fileName, ElementsToImport.Shell | ElementsToImport.Solid);
            //
            ImportMesh(mesh, GetReservedPartNames(), true, false);
        }
        public bool ImportMeshFromMmgFile(string fileName,
                                          ElementsToImport elementsToImport = ElementsToImport.All,
                                          bool convertToSecondOrder = false)
        {
            FeMesh mesh = MmgFileReader.Read3D(fileName, elementsToImport, MeshRepresentation.Mesh, convertToSecondOrder);
            //
            bool noErrors = true;
            foreach (var entry in mesh.Parts)
            {
                if (entry.Value is GeometryPart gp && gp.HasErrors)
                {
                    noErrors = false;
                    break;
                }
            }
            //
            ImportMesh(mesh, GetReservedPartNames(), true, false);
            //
            return noErrors;
        }
        
        public void ImportGeneratedMeshFromMeshFile(string fileName, BasePart part, bool convertToSecondOrder,
                                                    bool splitCompoundMesh, bool mergeCompoundParts)
        {
            ElementsToImport elementsToImport;
            GeometryPart subPart;
            if (part.PartType == PartType.SolidAsShell) elementsToImport = ElementsToImport.Solid;
            else if (part.PartType == PartType.Shell) elementsToImport = ElementsToImport.Shell;
            else if (part.PartType == PartType.Compound)
            {
                subPart = _geometry.Parts[(part as CompoundGeometryPart).SubPartNames[0]] as GeometryPart;
                if (subPart.PartType == PartType.SolidAsShell) elementsToImport = ElementsToImport.Solid;
                else if (subPart.PartType == PartType.Shell) elementsToImport = ElementsToImport.Shell;
                else throw new NotSupportedException();
            }
            else throw new NotSupportedException();
            // Get part names - determine, if one part or multiple parts are needed
            string[] prevPartNames;
            if (part is CompoundGeometryPart cgp)
            {
                if (mergeCompoundParts) prevPartNames = new string[] { part.Name };
                else prevPartNames = cgp.SubPartNames.ToArray();
            }
            else prevPartNames = new string[] { part.Name };
            // Called after meshing in PrePoMax - the parts are sorted by id
            FeMesh mesh;
            if (Path.GetExtension(fileName) == ".vol")
                mesh = VolFileReader.Read(fileName, elementsToImport, convertToSecondOrder);
            else if (Path.GetExtension(fileName) == ".mesh")
                mesh = MmgFileReader.Read(fileName, elementsToImport, MeshRepresentation.Mesh, convertToSecondOrder);
            else if (Path.GetExtension(fileName) == ".inp")
                mesh = InpFileReader.ReadMesh(fileName, elementsToImport, convertToSecondOrder);
            else throw new NotSupportedException();
            // Split compound mesh
            if (splitCompoundMesh) mesh.SplitSolidCompoundMesh();
            // Merge parts if only one part is expected
            string[] partNames = mesh.Parts.Keys.ToArray();
            if (prevPartNames.Length != partNames.Length)
            {
                if (prevPartNames.Length == 1 && partNames.Length > 1)
                {
                    mesh.MergeMeshParts(mesh.Parts.Keys.ToArray(), out _, out _);
                    partNames = mesh.Parts.Keys.ToArray();
                }
                else throw new NotSupportedException();
            }
            // Rename the imported part/s
            BasePart[] importedParts = new BasePart[partNames.Length];
            for (int i = 0; i < partNames.Length; i++)
            {
                importedParts[i] = mesh.Parts[partNames[i]];
                importedParts[i].Name = prevPartNames[i];
            }
            mesh.Parts.Clear();
            for (int i = 0; i < partNames.Length; i++) mesh.Parts.Add(importedParts[i].Name, importedParts[i]);
            //
            ImportMesh(mesh, null, false);
            // Recolor parts at the end of the import
            foreach (var importedPart in importedParts)
            {
                if (_geometry.Parts.ContainsKey(importedPart.Name) && _mesh.Parts.ContainsKey(importedPart.Name))
                    _mesh.Parts[importedPart.Name].Color = _geometry.Parts[importedPart.Name].Color;
            }
        }
        public static bool DeepCompare(object obj, object another)
        {
            if (ReferenceEquals(obj, another))
                return true;
            if ((obj == null) || (another == null))
                return false;
            //Compare two object's class, return false if they are difference
            if (obj.GetType() != another.GetType())
                return false;

            var result = true;
            //Get all properties of obj
            //And compare each other
            foreach (var property in obj.GetType().GetProperties())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                if (!objValue.Equals(anotherValue))
                    result = false;
            }

            return result;
        }

        public static bool CompareEx(object obj, object another)
        {
            if (ReferenceEquals(obj, another))
                return true;
            if ((obj == null) || (another == null))
                return false;
            if (obj.GetType() != another.GetType())
                return false;

            //properties: int, double, DateTime, etc, not class
            if (!obj.GetType().IsClass) return obj.Equals(another);

            var result = true;
            foreach (var property in obj.GetType().GetProperties())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                //Recursion
                if (!DeepCompare(objValue, anotherValue))
                    result = false;
            }
            return result;
        }
        public void ImportGeneratedRemeshFromMeshFile(string fileName, int[] elementIds, BasePart part,
                                                      bool convertToSecondOrder, Dictionary<int[], FeNode> midNodes)
        {
            // Remove elements from the mesh
            HashSet<int> possiblyUnrefNodeIds = new HashSet<int>();
            foreach (var elId in elementIds) possiblyUnrefNodeIds.UnionWith(_mesh.Elements[elId].NodeIds);  // contains midside nodes
            HashSet<int> removedNodeIds =
                _mesh.RemoveElementsByIds(new HashSet<int>(elementIds), possiblyUnrefNodeIds, false, false, true);
            HashSet<int> borderNodeIds = new HashSet<int>(possiblyUnrefNodeIds.Except(removedNodeIds));     // contains midside nodes
            Dictionary<int, FeNode> borderNodes = new Dictionary<int, FeNode>();
            foreach (var ndId in borderNodeIds) borderNodes.Add(ndId, _mesh.Nodes[ndId]);                   // contains midside nodes
            HashSet<int> remainingNodeIds = new HashSet<int>(_mesh.Nodes.Keys.Except(removedNodeIds));
            // Read the mmg file and renumber nodes and elements
            double epsilon = 1E-6;
            double max = part.BoundingBox.GetDiagonal();
            Dictionary<string, Dictionary<int, int>> partNameNewSurfIdOldSurfId = new Dictionary<string, Dictionary<int, int>>();
            Dictionary<string, Dictionary<int, int>> partNameNewEdgeIdOldEdgeId = new Dictionary<string, Dictionary<int, int>>();
            FeMesh mmgMesh = MmgFileReader.Read(fileName, ElementsToImport.Shell,
                                                                MeshRepresentation.Mesh, convertToSecondOrder,
                                                                _mesh.MaxNodeId + 1, _mesh.MaxElementId + 1,
                                                                borderNodes, midNodes,
                                                                epsilon * max,
                                                                partNameNewSurfIdOldSurfId, partNameNewEdgeIdOldEdgeId);
            // Get surface nodes before modification
            Dictionary<int, HashSet<int>> surfaceIdNodeIds = part.Visualization.GetSurfaceIdNodeIds();
            foreach (var entry in surfaceIdNodeIds) entry.Value.ExceptWith(removedNodeIds);
            // Get edge nodes before modification
            Dictionary<int, HashSet<int>> edgeIdNodeIds = part.Visualization.GetEdgeIdNodeIds();
            foreach (var entry in edgeIdNodeIds) entry.Value.ExceptWith(removedNodeIds);
            // Add elements to mesh                                                                                     
            FeElement element;
            HashSet<Type> elementTypes = new HashSet<Type>();
            foreach (var entry in mmgMesh.Elements)
            {
                element = entry.Value;
                element.PartId = part.PartId;
                elementTypes.Add(element.GetType());
                _mesh.Elements.Add(entry.Key, element);
            }
            // Add elements to part
            HashSet<int> newPartElementIds = new HashSet<int>(part.Labels);
            newPartElementIds.UnionWith(mmgMesh.Elements.Keys);
            part.Labels = newPartElementIds.ToArray();
            Array.Sort(part.Labels);
            // Add element types to part
            if (part is MeshPart mp) mp.AddElementTypes(elementTypes.ToArray());
            // Add nodes to mesh                                                                                        
            foreach (var entry in mmgMesh.Nodes)
            {
                if (!_mesh.Nodes.ContainsKey(entry.Key)) _mesh.Nodes.Add(entry.Key, entry.Value);
            }
            // Add nodes to part
            HashSet<int> newPartNodeIds = new HashSet<int>(part.NodeLabels);
            newPartNodeIds.UnionWith(mmgMesh.Nodes.Keys);
            part.NodeLabels = newPartNodeIds.ToArray();
            Array.Sort(part.NodeLabels);
            // Update node ids
            _mesh.UpdateMaxNodeAndElementIds();
            _mesh.UpdateNodeIdElementIds();
            // Model vertices                                                                                           
            HashSet<int> vertexNodeIds = new HashSet<int>();
            // Get vertices from part - only those that were not removed
            foreach (var nodeId in part.Visualization.VertexNodeIds)
            {
                if (!removedNodeIds.Contains(nodeId)) vertexNodeIds.Add(nodeId);
            }
            // Get vertices from mmgPart
            foreach (var entry in mmgMesh.Parts) vertexNodeIds.UnionWith(entry.Value.Visualization.VertexNodeIds);
            // Model edges                                                                                              
            int elementId = mmgMesh.MaxElementId + 1;
            LinearBeamElement beamElement;
            List<FeElement1D> edgeElements = new List<FeElement1D>();
            // Get model edges from part - only edges that are not completely removed
            int[] key;
            CompareIntArray comparer = new CompareIntArray();
            HashSet<int[]> edgeKeys = new HashSet<int[]>(comparer);
            //
            foreach (var edgeCell in part.Visualization.EdgeCells)
            {
                if (remainingNodeIds.Contains(edgeCell[0]) && remainingNodeIds.Contains(edgeCell[1]))
                {
                    key = Tools.GetSortedKey(edgeCell[0], edgeCell[1]);
                    if (!edgeKeys.Contains(key))
                    {
                        beamElement = new LinearBeamElement(elementId++, key);
                        edgeElements.Add(beamElement);
                        edgeKeys.Add(key);
                    }
                }
            }
            // Get model edges from mmgPart
            foreach (var entry in mmgMesh.Parts)
            {
                foreach (var edgeCell in entry.Value.Visualization.EdgeCells)
                {
                    key = Tools.GetSortedKey(edgeCell[0], edgeCell[1]);
                    if (!edgeKeys.Contains(key))
                    {
                        beamElement = new LinearBeamElement(elementId++, key);
                        edgeElements.Add(beamElement);
                        edgeKeys.Add(key);
                    }
                }
            }
            // Add edge elements to mesh
            foreach (var edgeElement in edgeElements) _mesh.Elements.Add(edgeElement.Id, edgeElement);
            // Extract visualization                                                                                    
            _mesh.ExtractShellPartVisualization(part, false, -1);
            //
            _mesh.ConvertLineFeElementsToEdges(vertexNodeIds, false, part.Name);
            // Renumber surfaces                                                                                        
            BasePart mmgPart;
            Dictionary<int, HashSet<int>> itemIdNodeIds;
            foreach (var partNewSurfIdOldSurfId in partNameNewSurfIdOldSurfId)
            {
                mmgPart = mmgMesh.Parts[partNewSurfIdOldSurfId.Key];
                itemIdNodeIds = mmgPart.Visualization.GetSurfaceIdNodeIds();
                //
                foreach (var newSurfIdOldSurfId in partNewSurfIdOldSurfId.Value)
                {
                    surfaceIdNodeIds[newSurfIdOldSurfId.Value].UnionWith(itemIdNodeIds[newSurfIdOldSurfId.Key]);
                    surfaceIdNodeIds[newSurfIdOldSurfId.Value].IntersectWith(part.NodeLabels);
                }
            }
            _mesh.RenumberVisualizationSurfaces(part, surfaceIdNodeIds);
            // Renumber edges                                                                                           
            foreach (var partNewEdgeIdOldEdgeId in partNameNewEdgeIdOldEdgeId)
            {
                if (mmgMesh.Parts.TryGetValue(partNewEdgeIdOldEdgeId.Key, out mmgPart))
                {
                    itemIdNodeIds = mmgPart.Visualization.GetEdgeIdNodeIds();
                    //
                    foreach (var newEdgeIdOldEdgeId in partNewEdgeIdOldEdgeId.Value)
                    {
                        edgeIdNodeIds[newEdgeIdOldEdgeId.Value].UnionWith(itemIdNodeIds[newEdgeIdOldEdgeId.Key]);
                        edgeIdNodeIds[newEdgeIdOldEdgeId.Value].IntersectWith(part.NodeLabels);
                    }
                }
            }
            _mesh.RenumberPartVisualizationEdges(part, edgeIdNodeIds);
            //
            _mesh.RemoveElementsByType<FeElement1D>();
        }
        public List<string> ImportModelFromInpFile(string fileName, Action<string> WriteDataToOutput)
        {
            OrderedDictionary<int[], Calculix.CalculixUserKeyword> indexedUserKeywords;
            InpFileReader.Read(fileName,
                               ElementsToImport.Solid | ElementsToImport.Shell, 
                               this,
                               WriteDataToOutput,
                               out indexedUserKeywords);
            //
            CalculixUserKeywords = indexedUserKeywords;
            //
            return InpFileReader.Errors;
        }
        public List<string> ImportMaterialsFromInpFile(string fileName, Action<string> WriteDataToOutput)
        {
            FeModel model = new FeModel("Imported Materials", _unitSystem);
            model.Properties.ModelSpace = _properties.ModelSpace;
            // Add existing model materials to account for indexing of material user keywords
            string existingMaterialName;
            foreach (var entry in _materials)
            {
                existingMaterialName = "_internal_for_indices_" + entry.Key;
                model.Materials.Add(existingMaterialName, entry.Value.DeepClone());
            }
            List<string> existingMaterialNames = model.Materials.Keys.ToList();
            //
            InpFileReader.Read(fileName,
                               ElementsToImport.Solid | ElementsToImport.Shell,
                               model,
                               WriteDataToOutput,
                               out OrderedDictionary<int[], Calculix.CalculixUserKeyword>  indexedUserKeywords);
            //
            CalculixUserKeywords = indexedUserKeywords;
            // Remove existing model materials
            foreach (var materialName in existingMaterialNames) model.Materials.Remove(materialName);
            //
            string name;
            foreach (var entry in model.Materials)
            {
                name = entry.Key;
                if (_materials.ContainsKey(name))
                {
                    name += "_Imported";
                    if (_materials.ContainsKey(name))
                    {
                        name = _materials.GetNextNumberedKey(name);
                    }
                }
                entry.Value.Name = name;
                _materials.Add(name, entry.Value);
            }
            //
            return InpFileReader.Errors;
        }
        public void ImportMeshFromUnvFile(string fileName)
        {
            FeMesh mesh = UnvFileReader.Read(fileName, ElementsToImport.Shell | ElementsToImport.Solid);
            //
            ImportMesh(mesh, GetReservedPartNames(), true, false);
        }
        public void ImportMeshFromObjFile(string fileName)
        {
            FeMesh mesh = ObjFileReader.Read(fileName);
            //
            ImportMesh(mesh, GetReservedPartNames(), true, false);
        }
        //
        private string[] ImportGeometry(FeMesh mesh, ICollection<string> reservedPartNames)
        {
            Check2D(mesh);
            //
            if (_geometry == null)
            {                
                _geometry = new FeMesh(MeshRepresentation.Geometry);
                mesh.ResetPartsColor();
            }
            //
            string[] addedPartNames = _geometry.AddMesh(mesh, reservedPartNames, GetReservedPartIds());
            return addedPartNames;
        }
        public void ImportMesh(FeMesh mesh, ICollection<string> reservedPartNames, bool forceRenameParts = true,
                               bool renumberNodesAndElements = true)
        {
            Check2D(mesh);
            //
            if (_mesh == null)
            {
                _mesh = new FeMesh(MeshRepresentation.Mesh);
                mesh.ResetPartsColor();
            }
            _mesh.AddMesh(mesh, reservedPartNames, GetReservedPartIds(), forceRenameParts, renumberNodesAndElements);
        }
        // Setters                                                                                  
        public void SetMesh(FeMesh mesh)
        {
            _mesh = mesh;
        }
        // Getters                                                                                  
        public string[] GetAllMeshEntityNames()
        {
            if (_mesh != null) return _mesh.GetAllMeshEntityNames();
            return new string[0];
        }
        public HashSet<string> GetReservedPartNames()
        {
            HashSet<string> reservedPartNames = new HashSet<string>();
            if (_geometry != null && _geometry.Parts != null) reservedPartNames.UnionWith(_geometry.Parts.Keys);
            reservedPartNames.UnionWith(GetAllMeshEntityNames());
            return reservedPartNames;
        }
        public HashSet<int> GetReservedPartIds()
        {
            HashSet<int> reservedPartIds = new HashSet<int>();
            if (_geometry != null && _geometry.Parts != null) reservedPartIds.UnionWith(_geometry.GetAllPartIds());
            if (_mesh != null && _mesh.Parts != null) reservedPartIds.UnionWith(_mesh.GetAllPartIds());
            return reservedPartIds;
        }
        // Mesh setup items
        public string IsMeshSetupItemProperlyDefined(MeshSetupItem meshSetupItem)
        {
            if (meshSetupItem is MeshingParameters) return null;
            else if (meshSetupItem is FeMeshRefinement) return null;
            else if (meshSetupItem is ShellGmsh sg) return IsShellGmshProperlyDefined(sg);
            else if (meshSetupItem is ThickenShellMesh tsm) return IsThickenShellMeshProperlyDefined(tsm);
            else if (meshSetupItem is TetrahedralGmsh tg) return IsTetrahedralGmshProperlyDefined(tg);
            else if (meshSetupItem is TransfiniteMesh tm) return IsTransfiniteMeshProperlyDefined(tm);
            else if (meshSetupItem is ExtrudeMesh em) return IsExtrudeMeshProperlyDefined(em);
            else if (meshSetupItem is SweepMesh sm) return IsSweepMeshProperlyDefined(sm);
            else if (meshSetupItem is RevolveMesh rm) return IsRevolveMeshProperlyDefined(rm);
            else throw new NotSupportedException("MeshSetupItemTypeException");
        }
        private string IsShellGmshProperlyDefined(ShellGmsh shellGmsh)
        {
            BasePart part;
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(shellGmsh.CreationIds);
            //
            foreach (var partId in partIds)
            {
                part = _geometry.GetPartFromId(partId);
                //
                if (part.PartType != PartType.Shell)
                    return "The shell gmsh setup item can only be defined on shell parts.";
            }
            return null;
        }
        private string IsThickenShellMeshProperlyDefined(ThickenShellMesh thickenShellMesh)
        {
            if (thickenShellMesh.PartNames != null && thickenShellMesh.PartNames.Length > 0)
            {
                BasePart part;
                foreach (var partName in thickenShellMesh.PartNames)
                {
                    _geometry.Parts.TryGetValue(partName, out part);
                    // Is mesh setup item defined on the mesh part
                    if (part == null) part = _mesh.Parts[partName];
                    if (part == null)
                        return "The part " + partName + " cannot be found neither in geometry parts neither in model parts.";
                    //
                    if (part.PartType != PartType.Shell)
                        return "The thicken shell feature can only be used on shell parts.";
                    if (part.Visualization.IsNonManifold())
                        return "The thicken shell feature can only be used on manifold shell parts.";
                }
            }
            else return "No parts are defined for the thicken shell mesh setup item.";
            //
            return null;
        }
        private string IsTetrahedralGmshProperlyDefined(TetrahedralGmsh tetrahedralGmsh)
        {
            BasePart part;
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(tetrahedralGmsh.CreationIds);
            //
            foreach (var partId in partIds)
            {
                part = _geometry.GetPartFromId(partId);
                //
                if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                    return "The tetrahedral gmsh setup item can only be defined on solid parts.";
            }
            return null;
        }
        private string IsTransfiniteMeshProperlyDefined(TransfiniteMesh transfiniteMesh)
        {
            GeometryPart part;
            int[] partIds = FeMesh.GetPartIdsFromGeometryIds(transfiniteMesh.CreationIds);
            //
            int num3sided;
            int num4sided;
            HashSet<int> triSurfaceEdgeIds = new HashSet<int>();
            foreach (var partId in partIds)
            {
                part = (GeometryPart)_geometry.GetPartFromId(partId);
                //
                if (!part.IsCADPart)
                    return "The transfinite gmsh setup item cannot be defined on a stl based part.";
                if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                    return "The transfinite gmsh setup item can only be defined on solid parts.";
                //
                //if (System.Diagnostics.Debugger.IsAttached || transfiniteMesh.AllowPrismElements) return null;
                if (transfiniteMesh.AllowPyramidElements) return null;
                else
                {
                    if (part.Visualization.FaceCount == 5 || part.Visualization.FaceCount == 6)
                    {
                        num3sided = 0;
                        num4sided = 0;
                        triSurfaceEdgeIds.Clear();
                        for (int i = 0; i < part.Visualization.FaceCount; i++)
                        {
                            if (part.Visualization.FaceEdgeIds[i].Length == 3)
                            {
                                num3sided++;
                                triSurfaceEdgeIds.UnionWith(part.Visualization.FaceEdgeIds[i]);
                            }
                            else if (part.Visualization.FaceEdgeIds[i].Length == 4) num4sided++;
                        }
                        if (!(num4sided == 6 || (num4sided == 3 && num3sided == 2)))
                            return "The transfinite gmsh setup item can only be defined on solid parts with 3-sided and " +
                                   "4-sided faces.";
                        if (num4sided == 3 && num3sided == 2 && triSurfaceEdgeIds.Count != 6)
                            return "The transfinite gmsh setup item can only be defined on solid parts with non touching " +
                                   "3-sided faces.";
                    }
                    else return "The transfinite gmsh setup item can only be defined on solid parts with 5 or 6 faces.";
                }
            }
            return null;
        }
        private string IsExtrudeMeshProperlyDefined(ExtrudeMesh extrudeMesh)
        {
            string error = null;
            extrudeMesh.Direction = null;
            extrudeMesh.ExtrudeCenter = null;
            //
            if (extrudeMesh.AlgorithmMesh2D == CaeMesh.Meshing.GmshAlgorithmMesh2DEnum.QuasiStructuredQuad)
                return "The extrude mesh setup item cannot use the quasi-structured quad algorithm.";
            //
            if (extrudeMesh.ElementSizeType == CaeMesh.Meshing.ElementSizeTypeEnum.MultiLayerd &&
                (extrudeMesh.LayerSizes.Length != extrudeMesh.NumOfElementsPerLayer.Length))
                return "The number of layers must be equal for layer sizes and number of elements per layer.";
            //
            int[] selectedPartIds = FeMesh.GetPartIdsFromGeometryIds(extrudeMesh.CreationIds);
            //
            if (selectedPartIds.Length == 1)
            {
                // Get part
                int partId = selectedPartIds[0];
                GeometryPart part = (GeometryPart)_geometry.GetPartFromId(partId);
                //
                if (IsPartCompoundSubPart(part.Name))
                    return "The extrude mesh setup item cannot be defined on a compound part.";
                if (!part.IsCADPart)
                    return "The extrude mesh setup item cannot be defined on a stl based part.";
                if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                    return "The extrude mesh setup item cannot be defined on a shell part.";
                //
                VisualizationData vis = part.Visualization;
                // Get surface ids
                HashSet<int> surfaceIds = new HashSet<int>();
                for (int i = 0; i < extrudeMesh.CreationIds.Length; i++)
                    surfaceIds.Add(FeMesh.GetItemIdFromGeometryId(extrudeMesh.CreationIds[i]));
                // Do selected surfaces form a connected surface
                if (vis.AreSurfacesConnected(surfaceIds.ToArray()))
                {
                    Dictionary<int, HashSet<int>> vertexIdEdgeId = vis.GetVertexIdEdgeIds();
                    HashSet<int> surfaceVertices = vis.GetVertexNodeIdsForSurfaceIds(surfaceIds.ToArray());
                    HashSet<int> surfaceEdgeIds = vis.GetEdgeIdsForSurfaceIds(surfaceIds.ToArray());
                    //
                    HashSet<int> directionEdgeIds = new HashSet<int>();
                    foreach (var vertexId in surfaceVertices)
                        directionEdgeIds.UnionWith(vertexIdEdgeId[vertexId].Except(surfaceEdgeIds));
                    //
                    double directionLength = vis.GetEqualEdgeLengthOrNegative(directionEdgeIds);
                    if (directionLength > 0)
                    {
                        bool straightEdges = false;
                        if (vis.EdgeTypes != null)
                        {
                            HashSet<GeomCurveType> edgeTypes = new HashSet<GeomCurveType>();
                            foreach (var directionEdgeId in directionEdgeIds) edgeTypes.Add(vis.EdgeTypes[directionEdgeId]);
                            //
                            if (edgeTypes.Count() == 1 && edgeTypes.First() == GeomCurveType.Line) straightEdges = true;
                        }
                        if (!straightEdges)
                        {
                            straightEdges = true;
                            double curvature;
                            foreach (var directionEdgeId in directionEdgeIds)
                            {
                                curvature = vis.GetMaxEdgeCurvature(directionEdgeId, _geometry.Nodes);
                                if (curvature > 0.001)
                                {
                                    straightEdges = false;
                                    break;
                                }
                            }
                        }
                        if (straightEdges)
                        {
                            int[] edgeNodes;
                            HashSet<int> nodeIds;
                            Vec3D normalizedDirection = null;
                            BoundingBox bb = new BoundingBox();
                            foreach (var directionEdgeId in directionEdgeIds)
                            {
                                // Get all edge node ids
                                nodeIds = vis.GetNodeIdsForEdgeId(directionEdgeId);
                                // Reduce edge node ids to vertices
                                edgeNodes = nodeIds.Intersect(vis.VertexNodeIds).ToArray();
                                if (edgeNodes.Length != 2) throw new NotSupportedException();
                                // First node must be on the selected surface
                                if (!surfaceVertices.Contains(edgeNodes[0]))
                                    (edgeNodes[0], edgeNodes[1]) = (edgeNodes[1], edgeNodes[0]);
                                // Compute the extrude direction
                                normalizedDirection = _geometry.ComputeDirectionFromEdgeCellIndices(edgeNodes, edgeNodes[0]);
                                bb.IncludeCoor(normalizedDirection.Coor);
                            }
                            //
                            if (normalizedDirection != null && bb.GetDiagonal() < 0.0017) // approximately 0.1° difference
                            {
                                extrudeMesh.Direction = (directionLength * normalizedDirection).Coor;
                                //
                                bb = _geometry.GetBoundingBoxForNodeIds(surfaceVertices.ToArray());
                                extrudeMesh.ExtrudeCenter = bb.GetCenter();
                                //
                                // Round
                                double d = part.BoundingBox.GetDiagonal();
                                int digits = (int)Math.Log10(d) - 6;
                                if (digits > 0) digits = 0;
                                else if (digits < 0) digits *= -1;
                                //
                                extrudeMesh.Direction[0] = Math.Round(extrudeMesh.Direction[0], digits);
                                extrudeMesh.Direction[1] = Math.Round(extrudeMesh.Direction[1], digits);
                                extrudeMesh.Direction[2] = Math.Round(extrudeMesh.Direction[2], digits);
                                //
                                extrudeMesh.ExtrudeCenter[0] = Math.Round(extrudeMesh.ExtrudeCenter[0], digits);
                                extrudeMesh.ExtrudeCenter[1] = Math.Round(extrudeMesh.ExtrudeCenter[1], digits);
                                extrudeMesh.ExtrudeCenter[2] = Math.Round(extrudeMesh.ExtrudeCenter[2], digits);
                                //
                            }
                            else error = "The selected surfaces have direction edges pointing in different direction.";
                        }
                        else error = "The selected surfaces have a direction edge that is not a straight line.";
                    }
                    else error = "The selected surfaces have direction edges of different lengths.";
                }
                else error = "The selected surfaces are not connected.";
            }
            else error = "The surfaces of more than one part are selected.";
            //
            return error == null ? null : error + " The extruded mesh cannot be created.";
        }
        private string IsSweepMeshProperlyDefined(SweepMesh sweepMesh)
        {
            string error = null;
            sweepMesh.Direction = null;
            sweepMesh.SweepCenter = null;
            //
            if (sweepMesh.ElementSizeType == CaeMesh.Meshing.ElementSizeTypeEnum.MultiLayerd &&
                (sweepMesh.LayerSizes.Length != sweepMesh.NumOfElementsPerLayer.Length))
                return "The number of layers must be equal for layer sizes and number of elements per layer.";
            //
            int[] selectedPartIds = FeMesh.GetPartIdsFromGeometryIds(sweepMesh.CreationIds);
            //
            if (selectedPartIds.Length == 1)
            {
                // Get part
                int partId = selectedPartIds[0];
                GeometryPart part = (GeometryPart)_geometry.GetPartFromId(partId);
                //
                if (IsPartCompoundSubPart(part.Name))
                    return "The sweep mesh setup item cannot be defined on a compound part.";
                if (!part.IsCADPart)
                    return "The sweep mesh setup item cannot be defined on a stl based part.";
                if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                    return "The sweep mesh setup item cannot be defined on a shell part.";
                //
                VisualizationData vis = part.Visualization;
                // Get surface ids
                HashSet<int> sourceSurfaceIds = new HashSet<int>();
                for (int i = 0; i < sweepMesh.CreationIds.Length; i++)
                    sourceSurfaceIds.Add(FeMesh.GetItemIdFromGeometryId(sweepMesh.CreationIds[i]));
                // Do selected surfaces form a connected surface
                Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds = vis.GetSurfaceIdSurfaceNeighbourIds();
                if (vis.AreSurfacesConnected(sourceSurfaceIds.ToArray(), surfaceIdSurfaceNeighbourIds))
                {
                    // Are there any free edge loops with a single edge  - cylinder with a single seam line
                    int singleEdgeLoops = 0;
                    HashSet<int>[] freeEdgeLoops = vis.GetFreeEdgeLoops(sourceSurfaceIds.ToArray());
                    for (int i = 0; i < freeEdgeLoops.Length; i++)
                    {
                        if (freeEdgeLoops[i].Count == 1) singleEdgeLoops++;
                    }
                    if (singleEdgeLoops == 0)
                    {
                        bool loop = true;
                        HashSet<int> neighbours;
                        HashSet<int> neighbourSurfaceIds;
                        List<int[]> layerSideSurfaceIds = new List<int[]>();
                        HashSet<int> visitedSurfaceIds = new HashSet<int>(sourceSurfaceIds);
                        //
                        Node<int> node;
                        Graph<int> connections;
                        Dictionary<int, Node<int>> surfaceIdNode;
                        List<Graph<int>> surfaceLoops;
                        //
                        while (true)
                        {
                            // New neighbours
                            neighbourSurfaceIds = new HashSet<int>();
                            // Find next neighbours
                            foreach (var visitedSurfaceId in visitedSurfaceIds)
                            {
                                neighbourSurfaceIds.UnionWith(surfaceIdSurfaceNeighbourIds[visitedSurfaceId]);
                            }
                            neighbourSurfaceIds.ExceptWith(visitedSurfaceIds);
                            // Get neighbours of neighbours
                            neighbours = new HashSet<int>();
                            foreach (var id in neighbourSurfaceIds) neighbours.UnionWith(surfaceIdSurfaceNeighbourIds[id]);
                            neighbours.ExceptWith(visitedSurfaceIds);   // remove visited
                            neighbours.ExceptWith(neighbourSurfaceIds); // remove self
                            // Are neighbours creating a closed surface compound
                            if (neighbours.Count == 0) loop = false;
                            // Check that neighbours form a loop
                            else
                            {
                                // Split surfaces into connected groups
                                connections = new Graph<int>();
                                surfaceIdNode = new Dictionary<int, Node<int>>();
                                //
                                foreach (var neighbourSurfaceId in neighbourSurfaceIds)
                                {
                                    node = new Node<int>(neighbourSurfaceId);
                                    connections.AddNode(node);
                                    surfaceIdNode.Add(neighbourSurfaceId, node);
                                }
                                foreach (var neighbour1Id in neighbourSurfaceIds)
                                {
                                    foreach (var neighbour2Id in surfaceIdSurfaceNeighbourIds[neighbour1Id])
                                    {
                                        if (neighbourSurfaceIds.Contains(neighbour2Id))
                                            connections.AddUndirectedEdge(surfaceIdNode[neighbour1Id],
                                                                          surfaceIdNode[neighbour2Id]);
                                    }
                                }
                                surfaceLoops = connections.GetConnectedSubgraphs();
                                //
                                if (surfaceLoops.Count() != freeEdgeLoops.Count()) loop = false;
                                else
                                {
                                    foreach (var surfaceLoop in surfaceLoops)
                                    {
                                        // One cylindrical face
                                        if (surfaceLoop.Count == 1) loop = false;
                                        // Two cylindrical faces
                                        else if (surfaceLoop.Count == 2)
                                        {
                                            foreach (var id in surfaceLoop.GetValues())
                                            {
                                                neighbours =
                                                    surfaceIdSurfaceNeighbourIds[id].Intersect(neighbourSurfaceIds).ToHashSet();
                                                if (neighbours.Count() != 1) { loop = false; break; }
                                            }
                                        }
                                        else
                                        {
                                            foreach (var id in surfaceLoop.GetValues())
                                            {
                                                neighbours =
                                                    surfaceIdSurfaceNeighbourIds[id].Intersect(neighbourSurfaceIds).ToHashSet();
                                                //
                                                if (neighbours.Count() != 2) { loop = false; break; }
                                            }
                                        }
                                    }
                                }
                            }
                            //
                            if (!loop) break;
                            //
                            layerSideSurfaceIds.Add(neighbourSurfaceIds.ToArray());
                            visitedSurfaceIds.UnionWith(neighbourSurfaceIds);
                        }
                        //
                        int[] targetSurfaceIds = surfaceIdSurfaceNeighbourIds.Keys.Except(visitedSurfaceIds).ToArray();
                        if (targetSurfaceIds.Length == 1)
                        {
                            int[] sideSurfaceIds = visitedSurfaceIds.Except(sourceSurfaceIds).ToArray();
                            //
                            if (sideSurfaceIds.Length > 0)
                            {
                                // Are all side surfaces 4-sided
                                bool fourSided = true;
                                foreach (var surfaceId in sideSurfaceIds)
                                {
                                    if (vis.FaceEdgeIds[surfaceId].Length != 4 && !vis.IsSurfaceACylinderLike(surfaceId, out _))
                                    {
                                        fourSided = false;
                                        break;
                                    }
                                }
                                //
                                if (fourSided)
                                {
                                    Dictionary<int, HashSet<int>> vertexIdEdgeId = vis.GetVertexIdEdgeIds();
                                    HashSet<int> surfaceEdgeIds = vis.GetEdgeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
                                    HashSet<int> surfaceVertices = vis.GetVertexNodeIdsForSurfaceIds(sourceSurfaceIds.ToArray());
                                    //
                                    HashSet<int> directionEdgeIds = new HashSet<int>();
                                    foreach (var vertexId in surfaceVertices)
                                        directionEdgeIds.UnionWith(vertexIdEdgeId[vertexId].Except(surfaceEdgeIds));
                                    //
                                    int[] edgeNodes;
                                    HashSet<int> nodeIds;
                                    Vec3D normalizedDirection;
                                    Vec3D averageDirection = new Vec3D();
                                    BoundingBox bb;
                                    int edgeCellId;
                                    int[] edgeCell;
                                    foreach (var directionEdgeId in directionEdgeIds)
                                    {
                                        // Get all edge node ids
                                        nodeIds = vis.GetNodeIdsForEdgeId(directionEdgeId);
                                        // Reduce edge node ids to vertices
                                        edgeNodes = nodeIds.Intersect(vis.VertexNodeIds).ToArray();
                                        if (edgeNodes.Length != 2) throw new NotSupportedException();
                                        // First node must be on the selected surface
                                        if (!surfaceVertices.Contains(edgeNodes[0]))
                                            (edgeNodes[0], edgeNodes[1]) = (edgeNodes[1], edgeNodes[0]);
                                        // Find the first edge cell for direction
                                        for (int i = 0; i < vis.EdgeCellIdsByEdge[directionEdgeId].Length; i++)
                                        {
                                            edgeCellId = vis.EdgeCellIdsByEdge[directionEdgeId][i];
                                            edgeCell = vis.EdgeCells[edgeCellId];
                                            if (edgeNodes[0] == edgeCell[0])
                                            {
                                                edgeNodes[1] = edgeCell[1];
                                                break;
                                            }
                                            else if (edgeNodes[0] == edgeCell[1])
                                            {
                                                edgeNodes[1] = edgeCell[0];
                                                break;
                                            }
                                        }
                                        // Compute the sweep direction
                                        normalizedDirection =
                                            _geometry.ComputeDirectionFromEdgeCellIndices(edgeNodes, edgeNodes[0]);
                                        averageDirection += normalizedDirection;
                                    }
                                    //
                                    averageDirection.Normalize();
                                    sweepMesh.Direction = averageDirection.Coor;
                                    //
                                    bb = _geometry.GetBoundingBoxForNodeIds(surfaceVertices.ToArray());
                                    sweepMesh.SweepCenter = bb.GetCenter();
                                    // Round
                                    double d = part.BoundingBox.GetDiagonal();
                                    int digits = (int)Math.Log10(d) - 6;
                                    if (digits > 0) digits = 0;
                                    else if (digits < 0) digits *= -1;
                                    //
                                    sweepMesh.Direction[0] = Math.Round(sweepMesh.Direction[0], digits);
                                    sweepMesh.Direction[1] = Math.Round(sweepMesh.Direction[1], digits);
                                    sweepMesh.Direction[2] = Math.Round(sweepMesh.Direction[2], digits);
                                    //
                                    sweepMesh.SweepCenter[0] = Math.Round(sweepMesh.SweepCenter[0], digits);
                                    sweepMesh.SweepCenter[1] = Math.Round(sweepMesh.SweepCenter[1], digits);
                                    sweepMesh.SweepCenter[2] = Math.Round(sweepMesh.SweepCenter[2], digits);
                                    //
                                    int[][][] layerGroupEdgeIds = GetDirectionEdges(vis, surfaceIdSurfaceNeighbourIds,
                                                                                    sideSurfaceIds,
                                                                                    layerSideSurfaceIds);
                                    // Gmsh numbering
                                    for (int i = 0; i < sideSurfaceIds.Length; i++)
                                    {
                                        sideSurfaceIds[i] = FeMesh.GmshTopologyId(sideSurfaceIds[i], partId);
                                    }
                                    sweepMesh.SideSurfaceIds = sideSurfaceIds;
                                    //
                                    for (int i = 0; i < layerGroupEdgeIds.Length; i++)
                                    {
                                        for (int j = 0; j < layerGroupEdgeIds[i].Length; j++)
                                        {
                                            for (int z = 0; z < layerGroupEdgeIds[i][j].Length; z++)
                                            {
                                                layerGroupEdgeIds[i][j][z] =
                                                    FeMesh.GmshTopologyId(layerGroupEdgeIds[i][j][z], partId);
                                            }
                                        }
                                    }
                                    sweepMesh.LayerGroupEdgeIds = layerGroupEdgeIds;
                                }
                                else error = "The sweep side surfaces are not 4-sided surfaces.";
                            }
                            else error = "The sweep side surfaces do not form a closed loop.";
                        }
                        else error = "There are more than 1 target surfaces.";

                        // Get target surfaces
                        
                    }
                    else error = "The selected surface/s contain free edge loop/s with a single edge (a hole or an extrusion).";
                }
                else error = "The selected surfaces are not connected.";
            }
            else error = "The surfaces of more than one part are selected.";
            //
            return error == null ? null : error + " The swept mesh cannot be created.";
        }
        private string IsRevolveMeshProperlyDefined(RevolveMesh revolveMesh)
        {
            string error = null;
            revolveMesh.AxisDirection = null;
            revolveMesh.AxisCenter = null;
            revolveMesh.AngleDeg = -1;
            revolveMesh.MiddleR = -1;
            //
            if (revolveMesh.AlgorithmMesh2D == CaeMesh.Meshing.GmshAlgorithmMesh2DEnum.QuasiStructuredQuad)
                return "The revolve mesh setup item cannot use the quasi-structured quad algorithm.";
            //
            if (revolveMesh.ElementSizeType == CaeMesh.Meshing.ElementSizeTypeEnum.MultiLayerd &&
                (revolveMesh.LayerSizes.Length != revolveMesh.NumOfElementsPerLayer.Length))
                return "The number of layers must be equal for layer sizes and number of elements per layer.";
            //
            int[] selectedPartIds = FeMesh.GetPartIdsFromGeometryIds(revolveMesh.CreationIds);
            //
            if (selectedPartIds.Length == 1)
            {
                // Get part
                int partId = selectedPartIds[0];
                GeometryPart part = (GeometryPart)_geometry.GetPartFromId(partId);
                if (IsPartCompoundSubPart(part.Name))
                    return "The revolve mesh setup item cannot be defined on a compound part.";
                if (!part.IsCADPart)
                    return "The revolve mesh setup item cannot be defined on a stl based part.";
                if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell)
                    return "The revolve mesh setup item cannot be defined on a shell part.";
                //
                VisualizationData vis = part.Visualization;
                // Get surface ids
                HashSet<int> surfaceIds = new HashSet<int>();
                for (int i = 0; i < revolveMesh.CreationIds.Length; i++)
                    surfaceIds.Add(FeMesh.GetItemIdFromGeometryId(revolveMesh.CreationIds[i]));
                // Do selected surfaces form a connected surface
                if (vis.AreSurfacesConnected(surfaceIds.ToArray()))
                {
                    Dictionary<int, HashSet<int>> vertexIdEdgeId = vis.GetVertexIdEdgeIds();
                    HashSet<int> surfaceVertices = vis.GetVertexNodeIdsForSurfaceIds(surfaceIds.ToArray());
                    HashSet<int> surfaceEdgeIds = vis.GetEdgeIdsForSurfaceIds(surfaceIds.ToArray());
                    //
                    HashSet<int> directionEdgeIds = new HashSet<int>();
                    foreach (var vertexId in surfaceVertices)
                        directionEdgeIds.UnionWith(vertexIdEdgeId[vertexId].Except(surfaceEdgeIds));
                    //
                    bool arcEdges = false;
                    if (vis.EdgeTypes != null)
                    {
                        HashSet<GeomCurveType> edgeTypes = new HashSet<GeomCurveType>();
                        foreach (var directionEdgeId in directionEdgeIds) edgeTypes.Add(vis.EdgeTypes[directionEdgeId]);
                        //
                        if (edgeTypes.Count() == 1 && edgeTypes.First() == GeomCurveType.Circle) arcEdges = true;
                    }
                    if (!arcEdges)
                    {
                        arcEdges = vis.IsEdgeRadiusConstant(directionEdgeIds, _geometry.Nodes);
                    }
                    if (arcEdges)
                    {
                        double r;
                        double arcAngleDeg;
                        double[] axisCenter;
                        double[] axisDirection;
                        vis.GetArcEdgeDataForEdgeIds(directionEdgeIds, surfaceVertices, _geometry.Nodes,
                                                     out r, out arcAngleDeg, out axisCenter, out axisDirection);
                        if (r > 0 && arcAngleDeg > 0)
                        {
                            revolveMesh.AxisDirection = axisDirection;
                            revolveMesh.AxisCenter = axisCenter;
                            revolveMesh.AngleDeg = arcAngleDeg;
                            revolveMesh.MiddleR = r;
                            // Round
                            double d = part.BoundingBox.GetDiagonal();
                            int digits = (int)Math.Log10(d) - 6;
                            if (digits > 0) digits = 0;
                            else if (digits < 0) digits *= -1;
                            //
                            revolveMesh.AxisDirection[0] = Math.Round(revolveMesh.AxisDirection[0], digits);
                            revolveMesh.AxisDirection[1] = Math.Round(revolveMesh.AxisDirection[1], digits);
                            revolveMesh.AxisDirection[2] = Math.Round(revolveMesh.AxisDirection[2], digits);
                            //
                            revolveMesh.AxisCenter[0] = Math.Round(revolveMesh.AxisCenter[0], digits);
                            revolveMesh.AxisCenter[1] = Math.Round(revolveMesh.AxisCenter[1], digits);
                            revolveMesh.AxisCenter[2] = Math.Round(revolveMesh.AxisCenter[2], digits);
                            //
                            revolveMesh.AngleDeg = Math.Round(revolveMesh.AngleDeg, 4);
                            //
                            revolveMesh.MiddleR = Math.Round(revolveMesh.MiddleR, digits);
                        }
                        else error = "The selected surfaces have direction edges with different arc angles or axes.";
                    }
                    else error = "The selected surfaces have direction edges of different arc radii.";
                }
                else error = "The selected surfaces are not connected.";
            }
            else error = "The surfaces of more than one part are selected.";
            //
            return error == null ? null : error + " The revolve mesh cannot be created.";
        }
        public bool IsPartCompoundSubPart(string partName)
        {
            if (_geometry == null) return false;
            //
            foreach (var entry in _geometry.Parts)
            {
                if (entry.Value is CompoundGeometryPart cgp)
                {
                    if (cgp.SubPartNames.Contains(partName)) return true;
                }
            }
            return false;
        }
        private int[][][] GetDirectionEdges(VisualizationData vis, Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds,
                                            int[] surfaceIds, List<int[]> layerSurfaceIds)
        {
            // Split into groups
            Node<int> node;
            Graph<int> connections = new Graph<int>();
            Dictionary<int, Node<int>> surfaceIdNode = new Dictionary<int, Node<int>>();
            //
            foreach (var surfaceId in surfaceIds)
            {
                node = new Node<int>(surfaceId);
                connections.AddNode(node);
                surfaceIdNode.Add(surfaceId, node);
            }
            HashSet<int> surfaceIdsHash = surfaceIds.ToHashSet();
            foreach (var surfaceId in surfaceIds)
            {
                foreach (var neighbourId in surfaceIdSurfaceNeighbourIds[surfaceId])
                {
                    if (surfaceIdsHash.Contains(neighbourId))
                        connections.AddUndirectedEdge(surfaceIdNode[surfaceId], surfaceIdNode[neighbourId]);
                }
            }
            List<Graph<int>> subGraphs = connections.GetConnectedSubgraphs();
            //
            int count = 0;
            HashSet<int>[] groupSurfaceId = new HashSet<int>[subGraphs.Count];
            foreach (var subGraph in subGraphs) groupSurfaceId[count++] = subGraph.GetValues().ToHashSet();
            // Create group mask
            Dictionary<int, int> surfaceIdGroupId = new Dictionary<int, int>();
            for (int i = 0; i < groupSurfaceId.Length; i++)
            {
                foreach (var surfaceId in groupSurfaceId[i])
                {
                    surfaceIdGroupId.Add(surfaceId, i);
                }
            }
            // Create layer mask
            Dictionary<int, int> surfaceIdLayerId = new Dictionary<int, int>();
            count = 0;
            foreach (var layer in layerSurfaceIds)
            {
                for (int i = 0; i < layer.Length; i++)
                {
                    surfaceIdLayerId.Add(layer[i], count);
                }
                count++;
            }
            //
            HashSet<int>[][] layerGroupSurfaceIds = new HashSet<int>[layerSurfaceIds.Count][];
            for (int i = 0; i < layerGroupSurfaceIds.Length; i++)
            {
                layerGroupSurfaceIds[i] = new HashSet<int>[groupSurfaceId.Length];
                for (int j = 0; j < layerGroupSurfaceIds[i].Length; j++) layerGroupSurfaceIds[i][j] = new HashSet<int>();
            }
            //
            int groupId;
            int layerId;
            for (int i = 0; i < surfaceIds.Length; i++)
            {
                groupId = surfaceIdGroupId[surfaceIds[i]];
                layerId = surfaceIdLayerId[surfaceIds[i]];
                layerGroupSurfaceIds[layerId][groupId].Add(surfaceIds[i]);
            }
            //
            int[][][] layerGroupEdgeIds = new int[layerSurfaceIds.Count][][];
            for (int i = 0; i < layerGroupEdgeIds.Length; i++)
            {
                layerGroupEdgeIds[i] = new int[groupSurfaceId.Length][];
                for (int j = 0; j < layerGroupEdgeIds[i].Length; j++)
                    layerGroupEdgeIds[i][j] = GetSharedEdgeIds(vis, layerGroupSurfaceIds[i][j]);
            }
            //
            return layerGroupEdgeIds;
        }
        private int[] GetSharedEdgeIds(VisualizationData vis, HashSet<int> surfaceIds)
        {
            int directionEdgeId;
            Dictionary<int, int> edgeIdCount = new Dictionary<int, int>();
            foreach (var surfaceId in surfaceIds)
            {
                if (vis.IsSurfaceACylinderLike(surfaceId, out directionEdgeId))
                {
                    if (edgeIdCount.ContainsKey(directionEdgeId)) edgeIdCount[directionEdgeId] += 2;
                    else edgeIdCount[directionEdgeId] = 2;
                }
                else
                {
                    foreach (var edgeId in vis.FaceEdgeIds[surfaceId])
                    {
                        if (edgeIdCount.ContainsKey(edgeId)) edgeIdCount[edgeId]++;
                        else edgeIdCount[edgeId] = 1;
                    }
                }
            }
            List<int> edgeIds = new List<int>();
            foreach (var entry in edgeIdCount)
            {
                if (entry.Value == 2) edgeIds.Add(entry.Key);
            }
            return edgeIds.ToArray();
        }
        // Springs                                                                                  
        public PointSpringData[] GetPointSpringsFromSurfaceSpring(SurfaceSpring spring)
        {
            List<PointSpringData> springs = new List<PointSpringData>();
            //
            if (spring.GetSpringDirections().Length != 0)
            {
                double area;
                Dictionary<int, double> nodalStiffnesses;
                GetDistributedNodalValuesFromSurface(spring.RegionName, out nodalStiffnesses, out area);
                //
                if (spring.StiffnessPerArea) area = 1;      // account for the stiffness type
                //
                double k1ByArea = spring.K1.Value / area;
                double k2ByArea = spring.K2.Value / area;
                double k3ByArea = spring.K3.Value / area;
                //
                foreach (var entry in nodalStiffnesses)
                {
                    if (entry.Value != 0)
                    {
                        springs.Add(new PointSpringData(spring.Name + "_" + entry.Key.ToString(), entry.Key,
                                    k1ByArea  * entry.Value,
                                    k2ByArea  * entry.Value,
                                    k3ByArea  * entry.Value));
                    }
                }
            }
            //
            return springs.ToArray();
        }
        // Loads                                                                                    
        public CLoad[] GetNodalCLoadsFromSurfaceTraction(STLoad load)
        {
            List<CLoad> loads = new List<CLoad>();
            //
            if (load.Magnitude.Value != 0)
            {
                double area;
                Dictionary<int, double> nodalForces;
                GetDistributedNodalValuesFromSurface(load.SurfaceName, out nodalForces, out area);
                //
                //
                double f1ByArea = load.F1.Value / area;
                double f2ByArea = load.F2.Value / area;
                double f3ByArea = load.F3.Value / area;
                //
                CLoad cLoad;
                double phaseDeg = load.PhaseDeg.Value;
                foreach (var entry in nodalForces)
                {
                    if (entry.Value != 0)
                    {
                        cLoad = new CLoad("_CLoad_" + entry.Key.ToString(), entry.Key,
                                          f1ByArea * entry.Value,
                                          f2ByArea * entry.Value,
                                          f3ByArea * entry.Value, load.TwoD, load.Complex, phaseDeg, true);
                        cLoad.AmplitudeName = load.AmplitudeName;
                        loads.Add(cLoad);
                    }
                }
            }
            //
            return loads.ToArray();
        }
        public void GetDistributedNodalValuesFromSurface(string surfaceName,
                                                         out Dictionary<int, double> nodalValues,
                                                         out double aSum)
        {
            nodalValues = new Dictionary<int, double>();
            aSum = 0;
            //
            int nodeId;
            int sectionId = 0;
            int[] nodeIds;
            double A;
            double thickness;
            double[] equValue;
            FeElement element;
            Dictionary<int, int> elementIdSectionId;
            Dictionary<int, double> sectionIdThickness = new Dictionary<int, double>();
            // Get element thicknesses
            GetSectionAssignments(out elementIdSectionId);
            foreach (var entry in _sections)
            {
                thickness = entry.Value.Thickness.Value;
                sectionIdThickness.Add(sectionId++, thickness);
            }
            //
            FeSurface surface = _mesh.Surfaces[surfaceName];
            if (surface.ElementFaces == null) return;
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    A = element.GetArea(entry.Key, _mesh.Nodes);
                    // Is shell edge face
                    if (element is FeElement2D element2D && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2)
                    {
                        sectionId = elementIdSectionId[elementId];
                        if (sectionId == -1) throw new CaeException("Missing section assignment at element " + elementId +
                                                                    " from part " + _mesh.GetPartFromId(element.PartId) + ".");
                        thickness = sectionIdThickness[sectionId];
                        A *= thickness;
                    }
                    aSum += A;
                    nodeIds = element.GetNodeIdsFromFaceName(entry.Key);
                    equValue = element.GetEquivalentForcesFromFaceName(entry.Key);
                    //
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        nodeId = nodeIds[i];
                        if (nodalValues.ContainsKey(nodeId)) nodalValues[nodeId] += A * equValue[i];
                        else nodalValues.Add(nodeId, A * equValue[i]);
                    }
                }
            }
        }
        public CLoad[] GetNodalCLoadsFromImportedSurfaceTraction(ImportedSTLoad load)
        {
            Dictionary<int, int> elementIdSectionId;
            double[] sectionIdThickness = new double[_sections.Count];
            // Get element thicknesses
            GetSectionAssignments(out elementIdSectionId);
            //
            int sectionId = 0;
            double thickness;
            string surfaceName = load.SurfaceName;
            foreach (var entry in _sections)
            {
                thickness = entry.Value.Thickness.Value;
                sectionIdThickness[sectionId++] = thickness;
            }
            // Surface
            FeSurface surface = _mesh.Surfaces[surfaceName];
            if (surface.ElementFaces == null) return null;
            //
            int nodeId;
            int[] nodeIds;
            double A;
            double[] forcePerArea;
            double[] force;
            double[] nodalForce;
            double[] faceNormal;
            double[][] forcePerAreaByValueId;
            double[][] nodalForceMagnitudes;
            FeElement element;
            Dictionary<int, double[]> nodeIdForcePerArea = new Dictionary<int, double[]>();
            Dictionary<int, double[]> nodeIdForce = new Dictionary<int, double[]>();
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    // Node ids
                    nodeIds = element.GetNodeIdsFromFaceName(entry.Key);
                    //
                    _mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out double[] faceCenter, out faceNormal,
                                                        out bool shellElement);
                    //
                    A = element.GetArea(entry.Key, _mesh.Nodes);
                    // Account for 2D area when an edge is selected
                    if (element is FeElement2D element2D && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2)
                    {
                        sectionId = elementIdSectionId[elementId];
                        if (sectionId == -1) throw new CaeException("Missing section assignment at element " + elementId +
                                                                    " from part " + _mesh.GetPartFromId(element.PartId) + ".");
                        thickness = sectionIdThickness[sectionId];
                        A *= thickness;
                    }
                    // Force per area
                    forcePerAreaByValueId = new double[load.Interpolator.NumValues][];
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        nodeId = nodeIds[i];
                        if (!nodeIdForcePerArea.TryGetValue(nodeId, out forcePerArea))
                        {
                            forcePerArea = load.GetForcePerAreaForPoint(_mesh.Nodes[nodeId].Coor);
                            nodeIdForcePerArea.Add(nodeId, forcePerArea);
                        }
                        for (int j = 0; j < forcePerAreaByValueId.Length; j++)
                        {
                            if (forcePerAreaByValueId[j] == null) forcePerAreaByValueId[j] = new double[nodeIds.Length];
                            forcePerAreaByValueId[j][i] = forcePerArea[j];
                        }
                    }
                    // Force magnitudes without area
                    nodalForceMagnitudes = new double[load.Interpolator.NumValues][];
                    for (int i = 0; i < nodalForceMagnitudes.Length; i++)
                    {
                        nodalForceMagnitudes[i] = element.GetEquivalentForcesFromFaceName(entry.Key, forcePerAreaByValueId[i]);
                    }
                    // Force vectors
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        force = new double[] { A * nodalForceMagnitudes[0][i],
                                               A * nodalForceMagnitudes[1][i],
                                               A * nodalForceMagnitudes[2][i] };
                        //
                        if (!nodeIdForce.TryGetValue(nodeIds[i], out nodalForce))
                        {
                            nodalForce = new double[3];
                            nodeIdForce.Add(nodeIds[i], nodalForce);
                        }
                        nodalForce[0] += force[0];
                        nodalForce[1] += force[1];
                        nodalForce[2] += force[2];
                    }
                    
                }
            }
            // Concentrated loads
            CLoad cLoad;
            List<CLoad> loads = new List<CLoad>();
            double phaseDeg = load.PhaseDeg.Value;
            foreach (var entry in nodeIdForce)
            {
                if (entry.Value[0] != 0 || entry.Value[1] != 0 || entry.Value[2] != 0)
                {
                    cLoad = new CLoad("_CLoad_" + entry.Key.ToString(), entry.Key,
                                      entry.Value[0],
                                      entry.Value[1],
                                      entry.Value[2],
                                      load.TwoD, load.Complex, phaseDeg, true);
                    //
                    cLoad.AmplitudeName = load.AmplitudeName;
                    //
                    loads.Add(cLoad);
                }
            }
            //
            return loads.ToArray();
        }
        public CLoad[] GetNodalCLoadsFromVariablePressureLoad(VariablePressure load)
        {
            Dictionary<int, int> elementIdSectionId;
            double[] sectionIdThickness = new double[_sections.Count];
            // Get element thicknesses
            GetSectionAssignments(out elementIdSectionId);
            //
            int sectionId = 0;
            double thickness;
            string surfaceName = load.SurfaceName;
            foreach (var entry in _sections)
            {
                thickness = entry.Value.Thickness.Value;
                sectionIdThickness[sectionId++] = thickness;
            }
            // Surface
            FeSurface surface = _mesh.Surfaces[surfaceName];
            if (surface.ElementFaces == null) return null;
            //
            int nodeId = -1;
            int[] nodeIds;
            double A;
            double sign;
            double pressure;
            double[] force;
            double[] nodalForce;
            double[] faceNormal;
            double[] nodalPressures;
            double[] nodalForceMagnitudes;
            FeElement element;
            Dictionary<int, double> nodeIdPressure = new Dictionary<int, double>();
            Dictionary<int, double[]> nodeIdForce = new Dictionary<int, double[]>();
            //
            int count;
            FeFaceName faceName;
            FeElement expandedElement;
            int[] expandedNodeIds;
            Dictionary<int, FeNode> expandedNodes;
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    // Node ids
                    nodeIds = element.GetNodeIdsFromFaceName(entry.Key);
                    //
                    _mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out double[] faceCenter, out faceNormal,
                                                        out bool shellElement);
                    //
                    if (Properties.ModelSpace == ModelSpaceEnum.Axisymmetric)
                    {
                        GetExpandedAxisymmetricElementFromNodeIds(nodeIds, out expandedElement, out expandedNodes);
                        expandedNodeIds = expandedElement.NodeIds;
                        faceName = FeFaceName.S1;
                        //
                        A = expandedElement.GetArea(faceName, expandedNodes) * 180 / 0.01;
                        // Pressure
                        nodalPressures = new double[expandedNodeIds.Length];
                        for (int i = 0; i < nodalPressures.Length; i++)
                        {
                            nodeId = expandedNodeIds[i];
                            pressure = load.GetPressureForPoint(expandedNodes[nodeId].Coor);
                            nodalPressures[i] = pressure;
                        }
                        // Force magnitudes without area
                        nodalForceMagnitudes = expandedElement.GetEquivalentForcesFromFaceName(faceName, nodalPressures);
                        // Invert face normal in case of S1 or S2 shell face
                        sign = 1;
                        // Force vectors
                        for (int i = 0; i < expandedNodeIds.Length; i++)
                        {
                            force = new double[] {
                                sign * A * nodalForceMagnitudes[i] * faceNormal[0],
                                sign * A * nodalForceMagnitudes[i] * faceNormal[1],
                                sign * A * nodalForceMagnitudes[i] * faceNormal[2]};
                            //
                            if (expandedElement.Id == 31)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[1];
                            }
                            else if (expandedElement.Id == 32)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[0];
                            }
                            else if (expandedElement.Id == 41)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 4) nodeId = nodeIds[0];
                            }
                            else if (expandedElement.Id == 61)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 4) nodeId = nodeIds[2];
                                else if (expandedNodeIds[i] == 5) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 6) nodeId = nodeIds[2];
                            }
                            else if (expandedElement.Id == 62)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 4) nodeId = nodeIds[2];
                                else if (expandedNodeIds[i] == 5) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 6) nodeId = nodeIds[2];
                            }
                            else if (expandedElement.Id == 81)
                            {
                                if (expandedNodeIds[i] == 1) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 2) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 3) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 4) nodeId = nodeIds[0];
                                else if (expandedNodeIds[i] == 5) nodeId = nodeIds[2];
                                else if (expandedNodeIds[i] == 6) nodeId = nodeIds[1];
                                else if (expandedNodeIds[i] == 7) nodeId = nodeIds[2];
                                else if (expandedNodeIds[i] == 8) nodeId = nodeIds[0];
                            }
                            //
                            if (!nodeIdForce.TryGetValue(nodeId, out nodalForce))
                            {
                                nodalForce = new double[3];
                                nodeIdForce.Add(nodeId, nodalForce);
                            }
                            nodalForce[0] += force[0];
                            nodalForce[1] += force[1];
                            nodalForce[2] += force[2];
                        }
                    }
                    else
                    {
                        A = element.GetArea(entry.Key, _mesh.Nodes);
                        // Account for 2D area when an edge is selected
                        if (element is FeElement2D element2D && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2)
                        {
                            sectionId = elementIdSectionId[elementId];
                            if (sectionId == -1) throw new CaeException("Missing section assignment at element " + elementId +
                                                                        " from part " + _mesh.GetPartFromId(element.PartId) + ".");
                            thickness = sectionIdThickness[sectionId];
                            A *= thickness;
                        }
                        // Pressure
                        nodalPressures = new double[nodeIds.Length];
                        for (int i = 0; i < nodalPressures.Length; i++)
                        {
                            nodeId = nodeIds[i];
                            if (!nodeIdPressure.TryGetValue(nodeId, out pressure))
                            {
                                pressure = load.GetPressureForPoint(_mesh.Nodes[nodeId].Coor);
                                nodeIdPressure.Add(nodeId, pressure);
                            }
                            nodalPressures[i] = pressure;
                        }
                        // Force magnitudes without area
                        nodalForceMagnitudes = element.GetEquivalentForcesFromFaceName(entry.Key, nodalPressures);
                        // Invert face normal in case of S1 or S2 shell face
                        if (shellElement && (entry.Key == FeFaceName.S1 || entry.Key == FeFaceName.S2)) sign = -1;
                        else sign = 1;
                        // Force vectors
                        for (int i = 0; i < nodeIds.Length; i++)
                        {
                            force = new double[] {
                                sign * A * nodalForceMagnitudes[i] * faceNormal[0],
                                sign * A * nodalForceMagnitudes[i] * faceNormal[1],
                                sign * A * nodalForceMagnitudes[i] * faceNormal[2]};
                            //
                            if (!nodeIdForce.TryGetValue(nodeIds[i], out nodalForce))
                            {
                                nodalForce = new double[3];
                                nodeIdForce.Add(nodeIds[i], nodalForce);
                            }
                            nodalForce[0] += force[0];
                            nodalForce[1] += force[1];
                            nodalForce[2] += force[2];
                        }
                    }
                }
            }
            // Concentrated loads
            CLoad cLoad;
            List<CLoad> loads = new List<CLoad>();
            double phaseDeg = load.PhaseDeg.Value;
            foreach (var entry in nodeIdForce)
            {
                if (entry.Value[0] != 0 || entry.Value[1] != 0 || entry.Value[2] != 0)
                {
                    cLoad = new CLoad("_CLoad_" + entry.Key.ToString(), entry.Key,
                                      entry.Value[0],
                                      entry.Value[1],
                                      entry.Value[2],
                                      load.TwoD, load.Complex, phaseDeg, true);
                    //
                    cLoad.AmplitudeName = load.AmplitudeName;
                    //
                    loads.Add(cLoad);
                }
            }
            //
            return loads.ToArray();
        }
        public void GetExpandedAxisymmetricElementFromNodeIds(int[] nodeIds, out FeElement expandedElement,
                                                             out Dictionary<int, FeNode> expandedNodes)
        {
            expandedElement = null;
            expandedNodes = new Dictionary<int, FeNode>();
            // Node coor
            double[][] coor = new double[nodeIds.Length][];
            for (int i = 0; i < nodeIds.Length; i++) coor[i] = _mesh.Nodes[nodeIds[i]].Coor;
            //
            double cos = Math.Cos(Math.PI / 180 * 0.01);
            double sin = Math.Sin(Math.PI / 180 * 0.01);
            //
            if (nodeIds.Length == 2)
            {
                // Triangle
                if (coor[0][0] == 0)
                {
                    expandedNodes.Add(1, new FeNode(1, coor[0]));
                    expandedNodes.Add(2, new FeNode(2, coor[1][0] * cos, coor[1][1], coor[1][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[1][0] * cos, coor[1][1], -coor[1][0] * sin));
                    expandedElement = new LinearTriangleElement(31, new int[] { 1, 2, 3 });
                }
                // Triangle
                else if (coor[1][0] == 0)
                {
                    expandedNodes.Add(1, new FeNode(1, coor[1]));
                    expandedNodes.Add(2, new FeNode(2, coor[0][0] * cos, coor[0][1], coor[0][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[0][0] * cos, coor[0][1], -coor[0][0] * sin));
                    expandedElement = new LinearTriangleElement(32, new int[] { 1, 2, 3 });
                }
                // Quad
                else
                {
                    expandedNodes.Add(1, new FeNode(1, coor[0][0] * cos, coor[0][1], coor[0][0] * sin));
                    expandedNodes.Add(2, new FeNode(2, coor[1][0] * cos, coor[1][1], coor[1][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[1][0] * cos, coor[1][1], -coor[1][0] * sin));
                    expandedNodes.Add(4, new FeNode(4, coor[0][0] * cos, coor[0][1], -coor[0][0] * sin));
                    expandedElement = new LinearQuadrilateralElement(41, new int[] { 1, 2, 3, 4 });
                }
            }
            else if (nodeIds.Length == 3)
            {
                // Triangle
                if (coor[0][0] == 0)
                {
                    expandedNodes.Add(1, new FeNode(1, coor[0]));
                    expandedNodes.Add(2, new FeNode(2, coor[1][0] * cos, coor[1][1], coor[1][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[1][0] * cos, coor[1][1], -coor[1][0] * sin));
                    //
                    expandedNodes.Add(4, new FeNode(4, coor[2][0] * cos, coor[2][1], coor[2][0] * sin));
                    expandedNodes.Add(5, new FeNode(5, coor[1]));
                    expandedNodes.Add(6, new FeNode(6, coor[2][0] * cos, coor[2][1], -coor[2][0] * sin));
                    //
                    expandedElement = new ParabolicTriangleElement(61, new int[] { 1, 2, 3, 4, 5, 6 });
                }
                // Triangle
                else if (coor[1][0] == 0)
                {
                    expandedNodes.Add(1, new FeNode(1, coor[1]));
                    expandedNodes.Add(2, new FeNode(2, coor[0][0] * cos, coor[0][1], coor[0][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[0][0] * cos, coor[0][1], -coor[0][0] * sin));
                    //
                    expandedNodes.Add(4, new FeNode(4, coor[2][0] * cos, coor[2][1], coor[2][0] * sin));
                    expandedNodes.Add(5, new FeNode(5, coor[0]));
                    expandedNodes.Add(6, new FeNode(6, coor[2][0] * cos, coor[2][1], -coor[2][0] * sin));
                    //
                    expandedElement = new ParabolicTriangleElement(62, new int[] { 1, 2, 3, 4, 5, 6 });
                }
                // Quad
                else
                {
                    expandedNodes.Add(1, new FeNode(1, coor[0][0] * cos, coor[0][1], coor[0][0] * sin));
                    expandedNodes.Add(2, new FeNode(2, coor[1][0] * cos, coor[1][1], coor[1][0] * sin));
                    expandedNodes.Add(3, new FeNode(3, coor[1][0] * cos, coor[1][1], -coor[1][0] * sin));
                    expandedNodes.Add(4, new FeNode(4, coor[0][0] * cos, coor[0][1], -coor[0][0] * sin));
                    //
                    expandedNodes.Add(5, new FeNode(5, coor[2][0] * cos, coor[2][1], coor[2][0] * sin));
                    expandedNodes.Add(6, new FeNode(6, coor[1]));
                    expandedNodes.Add(7, new FeNode(7, coor[2][0] * cos, coor[2][1], -coor[2][0] * sin));
                    expandedNodes.Add(8, new FeNode(8, coor[0]));
                    //
                    expandedElement = new ParabolicQuadrilateralElement(81, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 });
                }
            }
            else throw new NotSupportedException();
        }
        public DLoad[] GetNodalDLoadsFromVariablePressureLoad_(VariablePressure load)
        {
            // Surface
            FeSurface surface = _mesh.Surfaces[load.SurfaceName];
            if (surface.ElementFaces == null) return null;
            //
            double sign;
            double pressure;
            double[] faceNormal;
            FeElement element;
            DLoad dLoad;
            List<DLoad> loads = new List<DLoad>();
            //
            foreach (var entry in surface.ElementFaces)
            {
                foreach (var elementId in _mesh.ElementSets[entry.Value].Labels)
                {
                    element = _mesh.Elements[elementId];
                    //
                    _mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out double[] faceCenter, out faceNormal,
                                                        out bool shellElement);
                    // Pressure
                    pressure = load.GetPressureForPoint(faceCenter);
                    // Pressure loads
                    if (pressure != 0)
                    {
                        dLoad = new DLoad(entry.Key.ToString(), elementId.ToString(), RegionTypeEnum.ElementId,
                                          pressure, load.TwoD, load.Complex, load.PhaseDeg.Value);
                        dLoad.AmplitudeName = load.AmplitudeName;
                        loads.Add(dLoad);
                    }
                }
            }
            //
            return loads.ToArray();
        }
        public DLoad[] GetNodalDLoadsFromVariablePressureLoad(VariablePressure load)
        {
            // Surface
            FeSurface surface = _mesh.Surfaces[load.SurfaceName];
            if (surface.ElementFaces == null) return null;
            //
            int elementCount = 0;
            foreach (var entry in surface.ElementFaces) elementCount += _mesh.ElementSets[entry.Value].Labels.Length;
            DLoad[] loads = new DLoad[elementCount];
            //
            elementCount = 0;
            foreach (var entry in surface.ElementFaces) // this are faces S1, S2, ...
            {
                // Parallel
                Parallel.For(0, _mesh.ElementSets[entry.Value].Labels.Length, i =>
                //for (int i = 0; i < _mesh.ElementSets[entry.Value].Labels.Length; i++)
                {
                    int elementId = _mesh.ElementSets[entry.Value].Labels[i];
                    double[] faceCenter;
                    _mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out faceCenter, out _, out _);
                    // Pressure
                    double pressure = load.GetPressureForPoint(faceCenter);
                    // Pressure loads
                    if (pressure != 0)
                    {
                        DLoad dLoad = new DLoad(entry.Key.ToString(), elementId.ToString(), RegionTypeEnum.ElementId,
                                          pressure, load.TwoD, load.Complex, load.PhaseDeg.Value);
                        dLoad.AmplitudeName = load.AmplitudeName;
                        loads[elementCount + i] = dLoad;
                    }
                }
                );
                elementCount += _mesh.ElementSets[entry.Value].Labels.Length;
            }
            //
            return loads.ToArray();
        }
        public InitialTranslationalVelocity[] GetTranslationalVelocities(InitialAngularVelocity initialAngularVelocity,
                                                                         Dictionary<string, int[]> referencePointsNodeIds)
        {
            Dictionary<int, double[]> nodeIdCoor = new Dictionary<int, double[]>();
            if (initialAngularVelocity.RegionType == RegionTypeEnum.NodeSetName)
            {
                FeNodeSet nodeSet = _mesh.NodeSets[initialAngularVelocity.RegionName];
                for (int i = 0; i < nodeSet.Labels.Length; i++)
                {
                    nodeIdCoor[nodeSet.Labels[i]] = _mesh.Nodes[nodeSet.Labels[i]].Coor;
                }
            }
            else if (initialAngularVelocity.RegionType == RegionTypeEnum.SurfaceName)
            {
                string nodeSetName = _mesh.Surfaces[initialAngularVelocity.RegionName].NodeSetName;
                FeNodeSet nodeSet = _mesh.NodeSets[nodeSetName];
                for (int i = 0; i < nodeSet.Labels.Length; i++)
                {
                    nodeIdCoor[nodeSet.Labels[i]] = _mesh.Nodes[nodeSet.Labels[i]].Coor;
                }
            }
            else if (initialAngularVelocity.RegionType == RegionTypeEnum.ReferencePointName)
            {
                FeReferencePoint rp = _mesh.ReferencePoints[initialAngularVelocity.RegionName];
                nodeIdCoor[referencePointsNodeIds[rp.Name][0]] = rp.Coor();
            }
            //
            Dictionary<int, double[]> nodeIdVelocity;
            initialAngularVelocity.GetTranslationalVelocities(nodeIdCoor, out nodeIdVelocity);
            //
            int count = 0;
            int nodeId;
            InitialTranslationalVelocity[] translationalVelocities = new InitialTranslationalVelocity[nodeIdVelocity.Count];
            foreach (var entry in nodeIdVelocity)
            {
                nodeId = entry.Key;
                // Is the node is on the axis
                if (nodeId < 0)
                {
                    // If the node is a translational node of the reference point, get a rotational node instead
                    foreach (var rpEntry in referencePointsNodeIds)
                    {
                        if (rpEntry.Value[0] == -nodeId)
                        {
                            nodeId = rpEntry.Value[1];
                            break;
                        }
                    }
                }
                //
                translationalVelocities[count++] =
                    new InitialTranslationalVelocity("_iniTransVel_" + nodeId.ToString(), nodeId,
                                                     entry.Value[0], entry.Value[1], entry.Value[2],
                                                     initialAngularVelocity.TwoD, true);
            }
            //
            return translationalVelocities;
        }
        // Parameters                                                                               
        public void UpdateNCalcParameters()
        {
            try
            {
                MyNCalc.ExistingParameters = new OrderedDictionary<string, object>("Parameters");
                foreach (var entry in _parameters) MyNCalc.ExistingParameters.Add(entry.Key, entry.Value.Value);
            }
            catch
            {
                MessageBoxes.ShowError("The parameters could not be evaluated. Check the parameter equations.");
            }
        }
        // 3D - 2D                                                                                  
        public void UpdateMeshPartsElementTypes(bool allowMixedModel)
        {
            Dictionary<Type, HashSet<Enum>> elementTypeEnums = _properties.ModelSpace.GetAvailableElementTypes(allowMixedModel);
            if (_mesh != null) _mesh.UpdatePartsElementTypes(elementTypeEnums);
        }
        // Boundary displacement method                                                             
        public FeModel PrepareBdmModel(Dictionary<int, double[]> deformations)
        {
            // Mesh
            FeModel bdmModel = new FeModel("BDMmodel", _unitSystem);
            bdmModel.Properties = _properties;
            bdmModel.SetMesh(_mesh.DeepCopy());
            // Materials
            Material materialElastic = new Material("Elastic");
            materialElastic.AddProperty(new Elastic(new double[][] { new double[] { 1000, 0, 0 } }));
            bdmModel._materials.Add(materialElastic.Name, materialElastic);
            // Sections
            Section section;
            foreach (var entry in _sections)
            {
                section = entry.Value.DeepClone();
                section.MaterialName = materialElastic.Name;
                bdmModel.Sections.Add(section.Name, section);
            }
            // Constraints
            Constraint constraint;
            foreach (var entry in _constraints)
            {
                constraint = entry.Value.DeepClone();
                bdmModel.Constraints.Add(constraint.Name, constraint);
            }
            // Amplitudes
            Amplitude amplitude;
            foreach (var entry in _amplitudes)
            {
                amplitude = entry.Value.DeepClone();
                bdmModel.Amplitudes.Add(amplitude.Name, amplitude);
            }
            // Steps
            StaticStep staticStep = new StaticStep("Step-1");
            bdmModel._stepCollection.AddStep(staticStep, false);
            BoundaryDisplacementStep boundaryDisplacementStep = _stepCollection.GetBoundaryDisplacementStep();
            if (boundaryDisplacementStep == null)
                throw new CaeException("The boundary displacement step is missing.");
            // Add existing boundary conditions
            bool twoD = false;
            BoundaryCondition bc;
            foreach (var entry in boundaryDisplacementStep.BoundaryConditions)
            {
                bc = entry.Value.DeepClone();
                staticStep.AddBoundaryCondition(bc);
                twoD = bc.TwoD;
            }
            // Create BDM boundary conditions
            string name;
            double[] xyz;
            FeNodeSet nodeSet;
            DisplacementRotation displacementRotation;
            //
            if (deformations != null)
            {
                foreach (var entry in deformations)
                {
                    // Node set
                    name = bdmModel.Mesh.NodeSets.GetNextNumberedKey("BDM", "_" + entry.Key);
                    nodeSet = new FeNodeSet(name, new int[] { entry.Key });
                    bdmModel.Mesh.NodeSets.Add(nodeSet.Name, nodeSet);
                    // Boundary condition
                    xyz = entry.Value;
                    name = staticStep.BoundaryConditions.GetNextNumberedKey("BDM-" + entry.Key);
                    displacementRotation = new DisplacementRotation(name, nodeSet.Name, RegionTypeEnum.NodeSetName, twoD,
                                                                    false, 0);
                    if (xyz[0] != 0) displacementRotation.U1.SetEquationFromValue(xyz[0]);
                    if (xyz[1] != 0) displacementRotation.U2.SetEquationFromValue(xyz[1]);
                    if (xyz[2] != 0) displacementRotation.U3.SetEquationFromValue(xyz[2]);
                    staticStep.AddBoundaryCondition(displacementRotation);
                }
            }
            //
            return bdmModel;
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_name", Name, typeof(string));
            info.AddValue("_geometry", _geometry, typeof(FeMesh));
            info.AddValue("_mesh", _mesh, typeof(FeMesh));
            info.AddValue("_parameters", _parameters, typeof(EquationParameterCollection));
            info.AddValue("_materials", _materials, typeof(OrderedDictionary<string, Material>));
            info.AddValue("_sections", _sections, typeof(OrderedDictionary<string, Section>));
            info.AddValue("_constraints", _constraints, typeof(OrderedDictionary<string, Constraint>));
            info.AddValue("_surfaceInteractions", _surfaceInteractions, typeof(OrderedDictionary<string, SurfaceInteraction>));
            info.AddValue("_contactPairs", _contactPairs, typeof(OrderedDictionary<string, ContactPair>));
            info.AddValue("_amplitudes", _amplitudes, typeof(OrderedDictionary<string, Amplitude>));
            info.AddValue("_initialConditions", _initialConditions, typeof(OrderedDictionary<string, InitialCondition>));
            info.AddValue("_stepCollection", _stepCollection, typeof(StepCollection));
            info.AddValue("_calculixUserKeywords", _calculixUserKeywords, typeof(OrderedDictionary<int[], Calculix.CalculixUserKeyword>));
            info.AddValue("_properties", _properties, typeof(ModelProperties));
            info.AddValue("_unitSystem", _unitSystem, typeof(UnitSystem));
            info.AddValue("_hashName", _hashName, typeof(string));
        }

    }
}
