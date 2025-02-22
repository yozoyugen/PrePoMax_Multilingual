using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public class ViewMembraneSection : ViewSection
    {
        // Variables                                                                                                                
        protected CaeModel.MembraneSection _membraneSection;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Thickness")]
        [DescriptionAttribute("Set the membrane thickness.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        public EquationString Thickness
        {
            get { return _membraneSection.Thickness.Equation; }
            set { _membraneSection.Thickness.Equation = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 10, "Offset")]
        [DescriptionAttribute("Set the offset of the membrane mid-surface in regard to the selected geometry. "
                              + "The unit is the membrane thickness.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Offset
        {
            get { return _membraneSection.Offset.Equation; }
            set { _membraneSection.Offset.Equation = value; }
        }


        // Constructors                                                                                                             
        public ViewMembraneSection(CaeModel.MembraneSection membraneSection)
        {
            _membraneSection = membraneSection;
            SetBase(_membraneSection);
        }


        // Methods                                                                                                                  


    }

}
