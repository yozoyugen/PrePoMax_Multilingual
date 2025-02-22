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
    class CRemoveMeshingParameters : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _meshingParametersNames;


        // Constructor                                                                                                              
        public CRemoveMeshingParameters(string[] meshingParametersNames)
            : base("Remove meshing parameters")
        {
            _meshingParametersNames = meshingParametersNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveMeshSetupItems(_meshingParametersNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_meshingParametersNames);
        }
    }
}
