﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeMesh.Meshing;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewGmshSetupItem : ViewMeshSetupItem
    {
        // Variables                                                                                                                
        protected GmshSetupItem _gmshSetupItem;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _gmshSetupItem.Name; } set { _gmshSetupItem.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the mesh setup item.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(1, 10, "2D meshing algorithm")]
        [DescriptionAttribute("Select the algorithm for the surface meshing.")]
        [Id(1, 3)]
        public GmshAlgorithmMesh2DEnum AlgorithmMesh2D
        {
            get { return _gmshSetupItem.AlgorithmMesh2D; }
            set { _gmshSetupItem.AlgorithmMesh2D = value; }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(2, 10, "3D meshing algorithm")]
        [DescriptionAttribute("Select the algorithm for the volume meshing.")]
        [Id(2, 3)]
        public GmshAlgorithmMesh3DEnum AlgorithmMesh3D
        {
            get { return _gmshSetupItem.AlgorithmMesh3D; }
            set { _gmshSetupItem.AlgorithmMesh3D = value; }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(3, 10, "Recombine algorithm")]
        [DescriptionAttribute("Select the algorithm for recombination of triangles into quads.")]
        [Id(3, 3)]
        public GmshAlgorithmRecombineEnum AlgorithmRecombine
        {
            get { return _gmshSetupItem.AlgorithmRecombine; }
            set { _gmshSetupItem.AlgorithmRecombine = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(4, 10, "Recombine min quality")]
        [DescriptionAttribute("Minimum quality for quadrangles generated by recombination.")]
        [Id(4, 3)]
        public double RecombineMinQuality
        {
            get { return _gmshSetupItem.RecombineMinQuality; }
            set { _gmshSetupItem.RecombineMinQuality = value; }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(5, 10, "Transfinite 3-sided faces")]
        [DescriptionAttribute("Use automatic transfinite meshing constraints on 3-sided model faces.")]
        [Id(5, 3)]
        public bool TransfiniteThreeSided
        {
            get { return _gmshSetupItem.TransfiniteThreeSided; }
            set { _gmshSetupItem.TransfiniteThreeSided = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(6, 10, "Transfinite 4-sided faces")]
        [DescriptionAttribute("Use automatic transfinite meshing constraints on 4-sided model faces.")]
        [Id(6, 3)]
        public bool TransfiniteFourSided
        {
            get { return _gmshSetupItem.TransfiniteFourSided; }
            set { _gmshSetupItem.TransfiniteFourSided = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Mesh settings")]
        [OrderedDisplayName(7, 10, "Transfinite threshold angle")]
        [DescriptionAttribute("Quadrangular faces with a corner angle larger than the threshold angle are ignored.")]
        [TypeConverter(typeof(StringAngleDegConverter))]
        [Id(7, 3)]
        public double TransfiniteAngleDeg
        {
            get { return _gmshSetupItem.TransfiniteAngleDeg; }
            set { _gmshSetupItem.TransfiniteAngleDeg = value; }
        }
        //
        [CategoryAttribute("Mesh optimization")]
        [OrderedDisplayName(0, 10, "First order elements")]
        [DescriptionAttribute("Select the optimization algorithm for the first order elements.")]
        [Id(1, 4)]
        public GmshOptimizeFirstOrderShellEnum OptimizeFirstOrderShell
        {
            get { return _gmshSetupItem.OptimizeFirstOrderShell; }
            set { _gmshSetupItem.OptimizeFirstOrderShell = value; }
        }
        //
        [CategoryAttribute("Mesh optimization")]
        [OrderedDisplayName(1, 10, "First order elements")]
        [DescriptionAttribute("Select the optimization algorithm for the first order elements.")]
        [Id(2, 4)]
        public GmshOptimizeFirstOrderSolidEnum OptimizeFirstOrderSolid
        {
            get { return _gmshSetupItem.OptimizeFirstOrderSolid; }
            set { _gmshSetupItem.OptimizeFirstOrderSolid = value; }
        }
        //
        [CategoryAttribute("Mesh optimization")]
        [OrderedDisplayName(2, 10, "Second order elements")]
        [DescriptionAttribute("Select the optimization algorithm for the second order elements.")]
        [Id(3, 4)]
        public GmshOptimizeHighOrderEnum OptimizeHighOrder
        {
            get { return _gmshSetupItem.OptimizeHighOrder; }
            set { _gmshSetupItem.OptimizeHighOrder = value; }
        }
        //
        [CategoryAttribute("Element size in feature direction")]
        [OrderedDisplayName(0, 10, "Defined by")]
        [DescriptionAttribute("Select how the element size is defined.")]
        [Id(1, 5)]
        public ElementSizeTypeEnum ElementSizeType
        {
            get { return _gmshSetupItem.ElementSizeType; }
            set { _gmshSetupItem.ElementSizeType = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Element size in feature direction")]
        [OrderedDisplayName(1, 10, "Scale factor")]
        [DescriptionAttribute("Enter the scale factor for the finite element size in the mesh construction direction.")]
        [Id(2, 5)]
        public double ElementScaleFactor
        {
            get { return _gmshSetupItem.ElementScaleFactor; }
            set { _gmshSetupItem.ElementScaleFactor = value; }
        }
        //
        [CategoryAttribute("Element size in feature direction")]
        [OrderedDisplayName(2, 10, "Number of elements")]
        [DescriptionAttribute("Enter the number of elements for the mesh construction.")]
        [Id(3, 5)]
        public int NumberOfElements
        {
            get { return _gmshSetupItem.NumberOfElements; }
            set { _gmshSetupItem.NumberOfElements = value; }
        }
        //
        [CategoryAttribute("Element size in feature direction")]
        [OrderedDisplayName(3, 10, "Relative layer sizes")]
        [DescriptionAttribute("Enter the relative layer sizes separated by the semicolon ';' character as 0.2; 0.6; 0.2;")]
        [Id(4, 5)]
        public string NormalizedLayerSizes
        {
            get
            {
                string layerSizes = "";
                for (int i = 0; i < _gmshSetupItem.LayerSizes.Length; i++)
                {
                    if (i != 0) layerSizes += " ";
                    layerSizes += _gmshSetupItem.LayerSizes[i] + ";";
                }
                return layerSizes;
            }
            set
            {
                double size;
                List<double> sizes = new List<double>();
                string[] tmp = value.Split(new string[] { ";", " " }, StringSplitOptions.RemoveEmptyEntries);
                double sum = 0;
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (double.TryParse(tmp[i], out size))
                    {
                        if (size <= 0) throw new CaeException("Each normalized layer size must by positive.");
                        sum += size;
                        sizes.Add(size);
                    }
                    else throw new CaeException("Not all layer sizes can be converted to a numeric value.");
                }
                //
                _gmshSetupItem.LayerSizes = sizes.ToArray();
            }
        }
        //
        [CategoryAttribute("Element size in feature direction")]
        [OrderedDisplayName(4, 10, "Elements per layer")]
        [DescriptionAttribute("Enter the number of elements per layer separated by the semicolon ';' character as 2; 4; 2;")]
        [Id(5, 5)]
        public string NumOfElementsPerLayer
        {
            get
            {
                string numElements = "";
                for (int i = 0; i < _gmshSetupItem.NumOfElementsPerLayer.Length; i++)
                {
                    if (i != 0) numElements += " ";
                    numElements += _gmshSetupItem.NumOfElementsPerLayer[i] + ";";
                }
                return numElements;
            }
            set
            {
                int number;
                List<int> numbers = new List<int>();
                string[] tmp = value.Split(new string[] { ";", " " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (int.TryParse(tmp[i], out number))
                    {
                        if (number < 1) number = 1;
                        numbers.Add(number);
                    }
                    else throw new CaeException("Not all numbers of elements can be converted to a numeric value.");
                }
                //
                _gmshSetupItem.NumOfElementsPerLayer = numbers.ToArray();
            }
        }


        // Constructors                                                                                                             
        public ViewGmshSetupItem()
        {
        }


        // Methods                                                                                                                  
        public void SetBase(GmshSetupItem gmshSetupItem)
        {
            _gmshSetupItem = gmshSetupItem;                         // 1 command
            _dctd = ProviderInstaller.Install(this);                // 2 command
            //
            InitializeRegion();
            //
            UpdateVisibility();
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(TransfiniteThreeSided));
            _dctd.RenameBooleanPropertyToYesNo(nameof(TransfiniteFourSided));
        }
        public override MeshSetupItem GetBase()
        {
            return _gmshSetupItem;
        }
        public virtual void UpdateVisibility()
        {
            // Recombine
            if (_dctd.GetProperty(nameof(AlgorithmRecombine)).IsBrowsable)
            {
                if (AlgorithmRecombine == GmshAlgorithmRecombineEnum.None)
                {
                    _dctd.GetProperty(nameof(RecombineMinQuality)).SetIsBrowsable(false);
                }
                else
                {
                    _dctd.GetProperty(nameof(RecombineMinQuality)).SetIsBrowsable(true);
                }
            }
            // Transfinite
            if (_dctd.GetProperty(nameof(TransfiniteFourSided)).IsBrowsable)
            {
                _dctd.GetProperty(nameof(TransfiniteAngleDeg)).SetIsBrowsable(false);
            }
            // Element size
            if (_dctd.GetProperty(nameof(ElementSizeType)).IsBrowsable)
            {
                if (_gmshSetupItem.ElementSizeType == ElementSizeTypeEnum.NumberOfElements)
                {
                    _dctd.GetProperty(nameof(ElementScaleFactor)).SetIsBrowsable(false);
                    _dctd.GetProperty(nameof(NumberOfElements)).SetIsBrowsable(true);
                    _dctd.GetProperty(nameof(NormalizedLayerSizes)).SetIsBrowsable(false);
                    _dctd.GetProperty(nameof(NumOfElementsPerLayer)).SetIsBrowsable(false);
                }
                else if (_gmshSetupItem.ElementSizeType == ElementSizeTypeEnum.ScaleFactor)
                {
                    _dctd.GetProperty(nameof(ElementScaleFactor)).SetIsBrowsable(true);
                    _dctd.GetProperty(nameof(NumberOfElements)).SetIsBrowsable(false);
                    _dctd.GetProperty(nameof(NormalizedLayerSizes)).SetIsBrowsable(false);
                    _dctd.GetProperty(nameof(NumOfElementsPerLayer)).SetIsBrowsable(false);
                }
                else if (_gmshSetupItem.ElementSizeType == ElementSizeTypeEnum.MultiLayerd)
                {
                    _dctd.GetProperty(nameof(ElementScaleFactor)).SetIsBrowsable(false);
                    _dctd.GetProperty(nameof(NumberOfElements)).SetIsBrowsable(false);
                    _dctd.GetProperty(nameof(NormalizedLayerSizes)).SetIsBrowsable(true);
                    _dctd.GetProperty(nameof(NumOfElementsPerLayer)).SetIsBrowsable(true);
                }
                else throw new NotImplementedException("ExtrudedElementSizeTypeEnumException");
            }
        }
    }
}


