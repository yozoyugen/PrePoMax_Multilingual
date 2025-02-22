﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using UnitsNet.Units;
using UnitsNet;

namespace CaeGlobals
{
    public class EquationLengthDOFConverter : StringLengthDOFConverter
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public EquationLengthDOFConverter()
        {
        }


        // Methods                                                                                                                  
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Initializes the standard values list with string defaults.
            string initialValueStr = (string)ConvertTo(context, null, 0, typeof(string));
            values = new ArrayList(new EquationString[] { new EquationString(_free),
                                                          new EquationString(_fixed),
                                                          new EquationString(initialValueStr) });
            // Passes the local integer array.
            StandardValuesCollection svc = new StandardValuesCollection(values);
            return svc;
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // Convert from string to equation
            return EquationToString.ConvertFromStringToEquationString(context, culture, value, base.ConvertFrom);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // Convert from equation to string
            return EquationToString.ConvertToStringFromEquationString(context, culture, value, destinationType,
                                                                      base.ConvertFrom, base.ConvertTo);
        }
    }


}