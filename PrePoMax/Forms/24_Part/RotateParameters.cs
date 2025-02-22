﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using CaeModel;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;

namespace PrePoMax.Forms
{
    [Serializable]
    public class RotateParameters
    {
        // Variables                                                                                                                      
        private DynamicCustomTypeDescriptor _dctd = null;
        private PointSelectionMethodEnum _startPointSelectionMethod;
        private PointSelectionMethodEnum _endPointSelectionMethod;
        private ItemSetData _startPointItemSetData;
        private ItemSetData _endPointItemSetData;
        private double[] _startPoint;
        private double[] _endPoint;
        private double _angleDeg;
        private bool _copy;
        private bool _twoD;


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Operation")]
        [DescriptionAttribute("Select the move/copy operation.")]
        [Id(1, 1)]
        public bool Copy { get { return _copy; } set { _copy = value; } }
        //
        [Category("Start axis point coordinates")]
        [OrderedDisplayName(0, 10, "Selection method")]
        [DescriptionAttribute("Choose the selection method.")]
        [Id(1, 2)]
        public PointSelectionMethodEnum StartPointSelectionMethod
        {
            get { return _startPointSelectionMethod; }
            set
            {
                _startPointSelectionMethod = value;
                //
                if (_startPointSelectionMethod == PointSelectionMethodEnum.OnPoint)
                    _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
                else if (_startPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints)
                    _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectTwoPoints;
                else if (_startPointSelectionMethod == PointSelectionMethodEnum.CircleCenter)
                    _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectThreePoints;
                else throw new NotSupportedException();
            }
        }
        //
        [Category("Start axis point coordinates")]
        [OrderedDisplayName(1, 10, "By selection")]
        [DescriptionAttribute("Use selection for the definition of the start point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(2, 2)]
        public ItemSetData StartPointItemSet
        {
            get { return _startPointItemSetData; }
            set
            {
                if (value != _startPointItemSetData)
                    _startPointItemSetData = value;
            }
        }
        //
        [Category("Start axis point coordinates")]
        [OrderedDisplayName(2, 10, "X")]
        [Description("X coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 2)]
        public double X1
        {
            get { return _startPoint[0]; }
            set
            {
                _startPoint[0] = value;
                if (_twoD) _endPoint[0] = value;
            }
        }
        //
        [Category("Start axis point coordinates")]
        [OrderedDisplayName(3, 10, "Y")]
        [Description("Y coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 2)]
        public double Y1
        {
            get { return _startPoint[1]; }
            set
            {
                _startPoint[1] = value;
                if (_twoD) _endPoint[1] = value;
            }
        }
        //
        [Category("Start axis point coordinates")]
        [OrderedDisplayName(4, 10, "Z")]
        [Description("Z coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(5, 2)]
        public double Z1
        {
            get { return _startPoint[2]; }
            set
            {
                _startPoint[2] = value;
                if (_twoD) _startPoint[2] = 0;
            }
        }
        //
        [Category("End axis point coordinates")]
        [OrderedDisplayName(0, 10, "Selection method")]
        [DescriptionAttribute("Choose the selection method.")]
        [Id(1, 3)]
        public PointSelectionMethodEnum EndPointSelectionMethod
        {
            get { return _endPointSelectionMethod; }
            set
            {
                _endPointSelectionMethod = value;
                //
                if (_endPointSelectionMethod == PointSelectionMethodEnum.OnPoint)
                    _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
                else if (_endPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints)
                    _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectTwoPoints;
                else if (_endPointSelectionMethod == PointSelectionMethodEnum.CircleCenter)
                    _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectThreePoints;
                else throw new NotSupportedException();
            }
        }
        //
        [Category("End axis point coordinates")]
        [OrderedDisplayName(1, 10, "By selection ")]    // must be a different name than for the first point !!!
        [DescriptionAttribute("Use selection for the definition of the end point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(2, 3)]
        public ItemSetData EndPointItemSet
        {
            get { return _endPointItemSetData; }
            set
            {
                if (value != _endPointItemSetData) _endPointItemSetData = value;
            }
        }
        //
        [Category("End axis point coordinates")]
        [OrderedDisplayName(2, 10, "X")]
        [Description("X coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 3)]
        public double X2 { get { return _endPoint[0]; } set { _endPoint[0] = value; } }
        //
        [Category("End axis point coordinates")]
        [OrderedDisplayName(3, 10, "Y")]
        [Description("Y coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 3)]
        public double Y2 { get { return _endPoint[1]; } set { _endPoint[1] = value; } }
        //
        [Category("End axis point coordinates")]
        [OrderedDisplayName(4, 10, "Z")]
        [Description("Z coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(5, 3)]
        public double Z2
        {
            get { return _endPoint[2]; }
            set
            {
                _endPoint[2] = value;
                if (_twoD) _endPoint[2] = 1;
            }
        }
        //
        [Category("Rotation angle")]
        [OrderedDisplayName(0, 10, "Angle")]
        [Description("Rotation angle around the axis.")]
        [TypeConverter(typeof(StringAngleDegConverter))]
        [Id(1, 4)]
        public double AngleDeg { get { return _angleDeg; } set { _angleDeg = value; } }


        // Constructors                                                                                                             
        public RotateParameters(ModelSpaceEnum modelSpace)
        {
            Clear();
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            _startPointItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            _endPointItemSetData = new ItemSetData();   // needed to display ItemSetData.ToString()
            _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            //
            _dctd.RenameBooleanProperty(nameof(Copy), "Copy and rotate", "Rotate");
            //
            if (modelSpace == ModelSpaceEnum.ThreeD) { _twoD = false; }
            else if (modelSpace.IsTwoD())
            {
                _twoD = true;
                Z1 = 0;
                Z2 = 1;
            }
            else throw new NotSupportedException();
            //
            _dctd.GetProperty(nameof(Z1)).SetIsBrowsable(!_twoD);
            // End point
            _dctd.GetProperty(nameof(EndPointItemSet)).SetIsBrowsable(!_twoD);
            _dctd.GetProperty(nameof(X2)).SetIsBrowsable(!_twoD);
            _dctd.GetProperty(nameof(Y2)).SetIsBrowsable(!_twoD);
            _dctd.GetProperty(nameof(Z2)).SetIsBrowsable(!_twoD);
        }


        // Methods                                                                                                                  
        public void Clear()
        {
            _copy = false;
            _startPoint = new double[3];
            _endPoint = new double[] { 0, 0, 0};
            _angleDeg = 90;
            //
            if (_twoD)
            {
                Z1 = 0;
                Z2 = 1;
            }
        }
    }
}
