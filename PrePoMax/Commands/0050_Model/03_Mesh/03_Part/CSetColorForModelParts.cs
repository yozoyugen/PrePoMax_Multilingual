﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CSetColorForModelParts : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private System.Drawing.Color _color;


        // Constructor                                                                                                              
        public CSetColorForModelParts(string[] partNames, System.Drawing.Color color)
            : base("Set color for model parts")
        {
            _partNames = partNames;
            _color = color;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetColorForModelParts(_partNames, _color);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + "Color = " + _color.ToString() + ": " + GetArrayAsString(_partNames);
        }
    }
}
