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
    internal class CalSpecificHeat : CalculixKeyword
    {
        // Variables                                                                                                                
        private SpecificHeat _specificHeat;
        private bool _temperatureDependent;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalSpecificHeat(SpecificHeat specificHeat, bool temperatureDependent)
        {
            _specificHeat = specificHeat;
            _temperatureDependent = temperatureDependent;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Specific heat{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            EquationContainer[][] data = _specificHeat.SpecificHeatTemp;
            for (int i = 0; i < data.Length; i++)
            {
                if (_temperatureDependent)
                    sb.AppendFormat("{0}, {1}{2}", data[i][0].Value.ToCalculiX16String(), data[i][1].Value.ToCalculiX16String(),
                                    Environment.NewLine);
                else
                {
                    sb.AppendFormat("{0}{1}", data[i][0].Value.ToCalculiX16String(), Environment.NewLine);
                    break;
                }
            }
            return sb.ToString();
        }
    }
}
