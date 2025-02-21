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
    class CHideContactPairs : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _contactPairNames;


        // Constructor                                                                                                              
        public CHideContactPairs(string[] contactPairNames)
            : base("Hide contact pairs")
        {
            _contactPairNames = contactPairNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.HideContactPairs(_contactPairNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_contactPairNames);
        }
    }
}
