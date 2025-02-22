﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public class ParabolicQuadrilateralElement : FeElement2D
    {
        // Variables                                                                                                                
        private static readonly int vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_QUAD;
        private static readonly double a = 1.0 / 3.0;
        private static readonly double b = -1.0 / 12.0;
        private static readonly double c = 1.0 / 6.0;
        private static readonly double d = 4.0 / 6.0;


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ParabolicQuadrilateralElement(int id, int[] nodeIds)
         : base(id, nodeIds)
        {
        }
        public ParabolicQuadrilateralElement(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        public override int[] GetVtkNodeIds()
        {
            // return a copy -> ToArray
            return NodeIds.ToArray();
        }
        public override int[] GetGmshNodeIds()
        {
            // return a copy -> ToArray
            return NodeIds.ToArray();
        }
        public override int GetVtkCellType()
        {
            return vtkCellTypeInt;
        }
        public override FeFaceName GetFaceNameFromSortedNodeIds(int[] nodeIds)
        {
            throw new NotImplementedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // NEG S1 = 1-4-3-2-8-7-6-5 . 0-3-2-1-7-6-5-4
            // POS S2 = 1-2-3-4-5-6-7-8 . 0-1-2-3-4-5-6-7
            //     S3 = 1-2-5 . 0-1-4
            //     S4 = 2-3-6 . 1-2-5
            //     S5 = 3-4-7 . 2-3-6
            //     S6 = 4-1-8 . 3-0-7
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                       NodeIds[7], NodeIds[6], NodeIds[5], NodeIds[4] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3],
                                       NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[5] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[3], NodeIds[6] };
                case FeFaceName.S6:
                    return new int[] { NodeIds[3], NodeIds[0], NodeIds[7] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // NEG S1 = 1-4-3-2-8-7-6-5 . 0-3-2-1-7-6-5-4
            // POS S2 = 1-2-3-4-5-6-7-8 . 0-1-2-3-4-5-6-7
            //     S3 = 1-2-5 . 0-1-4
            //     S4 = 2-3-6 . 1-2-5
            //     S5 = 3-4-7 . 2-3-6
            //     S6 = 4-1-8 . 3-0-7
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                       NodeIds[7], NodeIds[6], NodeIds[5], NodeIds[4] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3],
                                       NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[5] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[3], NodeIds[6] };
                case FeFaceName.S6:
                    return new int[] { NodeIds[3], NodeIds[0], NodeIds[7] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[][] GetAllVtkCells()
        {
            int[][] cells = new int[6][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                       NodeIds[7], NodeIds[6], NodeIds[5], NodeIds[4] };
            cells[1] = new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3],
                                       NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7] };
            cells[2] = new int[] { NodeIds[0], NodeIds[1], NodeIds[4] };
            cells[3] = new int[] { NodeIds[1], NodeIds[2], NodeIds[5] };
            cells[4] = new int[] { NodeIds[2], NodeIds[3], NodeIds[6] };
            cells[5] = new int[] { NodeIds[3], NodeIds[0], NodeIds[7] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            // Check only first 4 nodes (as in linear element)
            int significantNodes = 4;
            bool[] faceNodeIds = new bool[significantNodes];
            //
            int count = 0;
            for (int i = 0; i < significantNodes; i++)
            {
                if (nodeSet.Contains(NodeIds[i]))
                {
                    faceNodeIds[i] = true;
                    count++;
                }
                // If three or more nodes were missed: break
                if (i + 1 - count >= 3) break;
            }
            //
            Dictionary<FeFaceName, double> faces = new Dictionary<FeFaceName, double>();
            //
            if (edgeFaces)
            {
                if (count >= 2)
                {
                    // S3 = 1-2-5 . 0-1-4
                    // S4 = 2-3-6 . 1-2-5
                    // S5 = 3-4-7 . 2-3-6
                    // S6 = 4-1-8 . 3-0-7
                    if (faceNodeIds[0] && faceNodeIds[1]) faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
                    if (faceNodeIds[1] && faceNodeIds[2]) faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
                    if (faceNodeIds[2] && faceNodeIds[3]) faces.Add(FeFaceName.S5, GetArea(FeFaceName.S5, nodes));
                    if (faceNodeIds[3] && faceNodeIds[0]) faces.Add(FeFaceName.S6, GetArea(FeFaceName.S6, nodes));
                }
            }
            else if (count == 4)
            {
                // POS S2 = 1-2-3 . 0-1-2
                if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[2] && faceNodeIds[3])
                    faces.Add(FeFaceName.S2, GetArea(FeFaceName.S2, nodes));
            }
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return new double[] { b, b, b, b, a, a, a, a };
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 ||
                     faceName == FeFaceName.S5 || faceName == FeFaceName.S6)
                return new double[] { c, c, d };
            else throw new NotSupportedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return GetEquivalentForces(typeof(ParabolicQuadrilateralElement), nodalValues);
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 ||
                     faceName == FeFaceName.S5 || faceName == FeFaceName.S6)
                return GetEquivalentForces(typeof(ParabolicBeamElement), nodalValues);
            else throw new NotSupportedException();
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 8)
                return GeometryTools.RectangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                                   nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]]);
            else if (cell.Length == 3)
                return GeometryTools.EdgeLength(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]]);
            else throw new NotSupportedException();
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 8)
                return GeometryTools.RectangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                                 nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]], out area);
            else if (cell.Length == 3)
                return GeometryTools.EdgeCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], out area);
            else throw new NotSupportedException();
        }
        public override FeElement DeepCopy()
        {
            return new ParabolicQuadrilateralElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
