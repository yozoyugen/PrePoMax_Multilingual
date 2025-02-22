﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalSTLoad : CalculixKeyword
    {
        // Variables                                                                                                                
        private STLoad _load;
        private CLoad[] _cLoad;
        private ComplexLoadTypeEnum _complexLoadType;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalSTLoad(FeModel model, STLoad load, ComplexLoadTypeEnum complexLoadType)
        {
            _load = load;
            _cLoad = model.GetNodalCLoadsFromSurfaceTraction(_load);
            _complexLoadType = complexLoadType;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _load.Name);
            string amplitude = "";
            if (_load.AmplitudeName != Load.DefaultAmplitudeName) amplitude = ", Amplitude=" + _load.AmplitudeName;
            //
            string loadCase = GetComplexLoadCase(_complexLoadType);
            //
            sb.AppendFormat("*Cload{0}{1}{2}", amplitude, loadCase, Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            //
            if (_cLoad != null)
            {
                List<int> directions = new List<int>();
                foreach (var cLoad in _cLoad)
                {
                    directions.Clear();
                    if (cLoad.F1.Value != 0) directions.Add(1);
                    if (cLoad.F2.Value != 0) directions.Add(2);
                    if (cLoad.F3.Value != 0) directions.Add(3);
                    //
                    foreach (var dir in directions)
                    {
                        sb.AppendFormat("{0}, {1}, {2}", cLoad.NodeId, dir,
                                        (ratio * cLoad.GetComponent(dir - 1)).ToCalculiX16String());
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }
    }
}
