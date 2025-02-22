﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.Runtime.Serialization;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeModel
{
    [Serializable]
    public enum IncrementationTypeEnum
    {
        Default,
        Automatic,
        Direct
    }
    //
    [Serializable]
    public enum SolverTypeEnum
    {
        Default,
        PaStiX,
        Pardiso,
        Spooles,
        //
        [StandardValue("IterativeScaling", DisplayName = "Iterative scaling")]
        IterativeScaling,
        //
        [StandardValue("IterativeCholesky", DisplayName = "Iterative Cholesky")]
        IterativeCholesky
    }

    [Serializable]
    public abstract class Step : NamedClass, ISerializable
    {
        // Variables                                                                                                                
        protected OrderedDictionary<string, HistoryOutput> _historyOutputs;             //ISerializable
        protected OrderedDictionary<string, FieldOutput> _fieldOutputs;                 //ISerializable
        protected OrderedDictionary<string, BoundaryCondition> _boundaryConditions;     //ISerializable
        protected OrderedDictionary<string, Load> _loads;                               //ISerializable
        protected OrderedDictionary<string, DefinedField> _definedFields;               //ISerializable
        protected bool _runAnalysis;                                                    //ISerializable
        protected bool _perturbation;                                                   //ISerializable
        protected bool _nlgeom;                                                         //ISerializable
        protected int _maxIncrements;                                                   //ISerializable
        protected IncrementationTypeEnum _incrementationType;                           //ISerializable
        protected SolverTypeEnum _solverType;                                           //ISerializable
        protected int _outputFrequency;                                                 //ISerializable
        protected StepControls _stepControls;                                           //ISerializable


        // Properties                                                                                                               
        public OrderedDictionary<string, HistoryOutput> HistoryOutputs { get { return _historyOutputs; } }
        public OrderedDictionary<string, FieldOutput> FieldOutputs { get { return _fieldOutputs; } }
        public OrderedDictionary<string, BoundaryCondition> BoundaryConditions { get { return _boundaryConditions; } }
        public OrderedDictionary<string, Load> Loads { get { return _loads; } }
        public OrderedDictionary<string, DefinedField> DefinedFields { get { return _definedFields; } }
        public bool RunAnalysis { get { return _runAnalysis; } set { _runAnalysis = value; } }
        public bool Perturbation { get { return _perturbation; } set { _perturbation = value; } }
        public bool Nlgeom { get { return _nlgeom; } set { _nlgeom = value; } }
        public int MaxIncrements { get { return _maxIncrements; } set { _maxIncrements = Math.Max(value, 1); } }
        public IncrementationTypeEnum IncrementationType { get { return _incrementationType; } set { _incrementationType = value; } }
        public SolverTypeEnum SolverType { get { return _solverType; } set { _solverType = value; } }
        public int OutputFrequency
        {
            get { return _outputFrequency; }
            set
            {
                if (value != int.MinValue && value < 0) throw new Exception("The frequency value must be larger or equal to 0.");
                _outputFrequency = value;
            }
        }
        public StepControls StepControls { get { return _stepControls; } set { _stepControls = value; } }
        

        // Constructors                                                                                                             
        public Step()
            :this("Step")
        { 
        }
        public Step(string name)
            : base(name) 
        {
            StringComparer sc = StringComparer.OrdinalIgnoreCase;
            //
            _historyOutputs = new OrderedDictionary<string, HistoryOutput>("History Outputs", sc);
            _fieldOutputs = new OrderedDictionary<string, FieldOutput>("Field Outputs", sc);
            _boundaryConditions = new OrderedDictionary<string, BoundaryCondition>("Boundary Conditions", sc);
            _loads = new OrderedDictionary<string, Load>("Loads", sc);
            _definedFields = new OrderedDictionary<string, DefinedField>("Defined Fields", sc);
            _runAnalysis = true;
            _perturbation = false;
            _nlgeom = false;
            _maxIncrements = 100;
            _incrementationType = IncrementationTypeEnum.Default;
            _outputFrequency = int.MinValue;
            _stepControls = new StepControls();
        }
        public Step(SerializationInfo info, StreamingContext context)
            :base(info, context)
        {
            _incrementationType = IncrementationTypeEnum.Automatic;         // Compatibility for version v.0.9.0
            // Compatibility for version v.1.3.5
            _runAnalysis = true;
            _outputFrequency = int.MinValue;
            
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_historyOutputs":
                        _historyOutputs = (OrderedDictionary<string, HistoryOutput>)entry.Value; break;
                    case "_fieldOutputs":
                        _fieldOutputs = (OrderedDictionary<string, FieldOutput>)entry.Value; break;
                    case "_boundayConditions":  // Compatibility for version v.1.3.5
                    case "_boundaryConditions":
                        _boundaryConditions = (OrderedDictionary<string, BoundaryCondition>)entry.Value; break;
                    case "_loads":
                        _loads = (OrderedDictionary<string, Load>)entry.Value; break;
                    case "_definedFields":
                        _definedFields = (OrderedDictionary<string, DefinedField>)entry.Value; break;
                    case "_runAnalysis":
                        _runAnalysis = (bool)entry.Value; break;
                    case "_perturbation":
                        _perturbation = (bool)entry.Value; break;
                    case "_nlgeom":
                        _nlgeom = (bool)entry.Value; break;
                    case "_maxIncrements":
                        _maxIncrements = (int)entry.Value; break;
                    case "_incrementationType":
                        _incrementationType = (IncrementationTypeEnum)entry.Value; break;
                    case "_solverType":
                        _solverType = (SolverTypeEnum)entry.Value; break;
                    case "_outputFrequency":
                        _outputFrequency = (int)entry.Value; break;
                    case "_stepControls":
                        _stepControls = (StepControls)entry.Value; break;
                        //default:
                        //    throw new NotSupportedException();
                }
            }
            // Compatibility for version v.1.0.0
            if (_definedFields == null)
                _definedFields = new OrderedDictionary<string, DefinedField>("Defined Fields", StringComparer.OrdinalIgnoreCase);
            // Compatibility for version v.2.0.9
            if (_stepControls == null) _stepControls = new StepControls();
        }


        // Methods                                                                                                                  
        public void AddHistoryOutput(HistoryOutput historyOutput)
        {
            _historyOutputs.Add(historyOutput.Name, historyOutput);
        }
        public void AddFieldOutput(FieldOutput fieldOutput)
        {
            _fieldOutputs.Add(fieldOutput.Name, fieldOutput);
        }
        public abstract bool IsBoundaryConditionSupported(BoundaryCondition boundaryCondition);
        public bool AddBoundaryCondition(BoundaryCondition boundaryCondition)
        {
            if (IsBoundaryConditionSupported(boundaryCondition))
            {
                boundaryCondition.Complex = this is SteadyStateDynamicsStep;
                _boundaryConditions.Add(boundaryCondition.Name, boundaryCondition);
                return true;
            }
            return false;
        }
        public bool ReplaceBoundaryCondition(string oldBoundaryConditionName, BoundaryCondition boundaryCondition)
        {
            if (IsBoundaryConditionSupported(boundaryCondition))
            {
                boundaryCondition.Complex = this is SteadyStateDynamicsStep;
                _boundaryConditions.Replace(oldBoundaryConditionName, boundaryCondition.Name, boundaryCondition);
                return true;
            }
            return false;
        }
        public bool IsLoadSupported(Load load)
        {
            return IsLoadTypeSupported(load.GetType());
        }
        public abstract bool IsLoadTypeSupported(Type loadType);
        public bool AddLoad(Load load)
        {
            if (IsLoadSupported(load))
            {
                load.Complex = this is SteadyStateDynamicsStep;
                _loads.Add(load.Name, load);
                return true;
            }
            return false;
        }
        public bool ReplaceLoad(string oldLoadName, Load load)
        {
            if (IsLoadSupported(load))
            {
                load.Complex = this is SteadyStateDynamicsStep;
                _loads.Replace(oldLoadName, load.Name, load);
                return true;
            }
            return false;
        }
        public abstract bool IsDefinedFieldSupported(DefinedField definedField);
        public bool AddDefinedField(DefinedField definedField)
        {
            if (IsDefinedFieldSupported(definedField))
            {
                _definedFields.Add(definedField.Name, definedField);
                return true;
            }
            return false;
        }
        public int GetNumberOfStepControls()
        {
            if (_stepControls != null && _stepControls.Parameters != null) return _stepControls.Parameters.Count;
            else return 0;
        }

        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_historyOutputs", _historyOutputs, typeof(OrderedDictionary<string, HistoryOutput>));
            info.AddValue("_fieldOutputs", _fieldOutputs, typeof(OrderedDictionary<string, FieldOutput>));
            info.AddValue("_boundaryConditions", _boundaryConditions, typeof(OrderedDictionary<string, BoundaryCondition>));
            info.AddValue("_loads", _loads, typeof(OrderedDictionary<string, Load>));
            info.AddValue("_definedFields", _definedFields, typeof(OrderedDictionary<string, DefinedField>));
            info.AddValue("_runAnalysis", _runAnalysis, typeof(bool));
            info.AddValue("_perturbation", _perturbation, typeof(bool));
            info.AddValue("_nlgeom", _nlgeom, typeof(bool));
            info.AddValue("_maxIncrements", _maxIncrements, typeof(int));
            info.AddValue("_incrementationType", _incrementationType, typeof(IncrementationTypeEnum));
            info.AddValue("_solverType", _solverType, typeof(SolverTypeEnum));
            info.AddValue("_outputFrequency", _outputFrequency, typeof(int));
            info.AddValue("_stepControls", _stepControls, typeof(StepControls));
        }
    }
}
