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
    class CReplaceMeshingParameters : PreprocessCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private string _oldMeshingParametersName;
        private MeshingParameters _meshingParameters;


        // Constructor                                                                                                              
        public CReplaceMeshingParameters(string oldMeshingParametersName, MeshingParameters newMeshingParameters)
            : base("Edit meshing parameters")
        {
            _oldMeshingParametersName = oldMeshingParametersName;
            _meshingParameters = newMeshingParameters.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceMeshSetupItem(_oldMeshingParametersName, _meshingParameters.DeepClone());
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            _meshingParameters = (MeshingParameters)receiver.EditMeshSetupItemByForm(_meshingParameters.DeepClone());
            return Execute(receiver);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldMeshingParametersName + ", " + _meshingParameters.ToString();
        }
    }
}
