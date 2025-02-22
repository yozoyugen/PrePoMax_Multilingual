﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;


namespace PrePoMax.Commands
{
    [Serializable]
    class CSetMeshingParameters : PreprocessCommand, ICommandWithDialog, ISerializable
    {
        // Variables                                                                                                                


        // Constructors                                                                                                             
        public CSetMeshingParameters(string[] partNames, MeshingParameters meshingParameters)
            : base("Set meshing parameters")
        {
            //
            // this class is needed for compatibility for version v1.3.5
            //
        }
        public CSetMeshingParameters(SerializationInfo info, StreamingContext context)
            : base("")
        {
            // Compatibility for version v1.3.5
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            // Compatibility for version v1.3.5
            return true;
        }
        public bool ExecuteWithDialog(Controller receiver)
        {
            // Compatibility for version v1.3.5
            return Execute(receiver);
        }
        public override string GetCommandString()
        {
            // Compatibility for version v1.3.5
            return base.GetCommandString();
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Compatibility for version v1.3.5
        }
    }
}
