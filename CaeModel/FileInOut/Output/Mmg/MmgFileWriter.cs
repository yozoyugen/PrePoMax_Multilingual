﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CaeMesh;
using CaeGlobals;

namespace FileInOut.Output
{
    static public class MmgFileWriter
    {
        public static void Write(string fileName, BasePart part, FeMesh mesh, bool keepModelEdges, bool keepVertexEdges)
        {
            if (part.PartType == PartType.Solid) Write3D(fileName, part, mesh, keepModelEdges, keepVertexEdges);
            else if (part.PartType == PartType.Shell) Write2D(fileName, part, mesh, keepModelEdges, keepVertexEdges);
            else throw new NotSupportedException();
        }
        private static void Write2D(string fileName, BasePart part, FeMesh mesh, bool keepModelEdges, bool keepVertexEdges)
        {
            VisualizationData vis = part.Visualization;
            // File
            StringBuilder sb = new StringBuilder();
            WriteHeading(sb);
            // Vertices
            Dictionary<int, int> oldNodeIdNewId;
            List<double[]> nodeCoorNodeId = GetNodeCoorNodeId(part.NodeLabels, mesh);
            WriteVertices(sb, nodeCoorNodeId, out oldNodeIdNewId);
            // Corners
            List<int> cornerIds = new List<int>();
            for (int i = 0; i < vis.VertexNodeIds.Length; i++) cornerIds.Add(oldNodeIdNewId[vis.VertexNodeIds[i]]);
            WriteCorners(sb, cornerIds);
            // Triangles
            int elementId;
            FeElement element;
            List<int[]> elementNodeIdsSurfaceId = new List<int[]>();
            for (int i = 0; i < vis.CellIdsByFace.Length; i++)
            {
                for (int j = 0; j < vis.CellIdsByFace[i].Length; j++)
                {
                    elementId = vis.CellIds[vis.CellIdsByFace[i][j]];
                    element = mesh.Elements[elementId];
                    if (element is LinearTriangleElement)
                    {
                        elementNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                                                                oldNodeIdNewId[element.NodeIds[1]],
                                                                oldNodeIdNewId[element.NodeIds[2]],
                                                                i + 1 });
                    }
                    //else if (element is ParabolicTriangleElement)
                    //{
                    //    elementNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                    //                                            oldNodeIdNewId[element.NodeIds[1]],
                    //                                            oldNodeIdNewId[element.NodeIds[2]],
                    //                                            i + 1 });
                    //}
                    else throw new NotSupportedException();
                }
            }
            WriteTriangles(sb, elementNodeIdsSurfaceId);
            // Edges
            int id1, id2;
            int edgeId = 1;
            List<int> edgeIds;
            // Collect all edge cells connected to a vertex
            Dictionary<int, List<int>> vertexEdgeIds = new Dictionary<int, List<int>>();
            for (int i = 0; i < vis.VertexNodeIds.Length; i++)
                vertexEdgeIds.Add(oldNodeIdNewId[vis.VertexNodeIds[i]], new List<int>());
            //
            List<int[]> edgeNodeIdsEdgeId = new List<int[]>();
            for (int i = 0; i < vis.EdgeCellIdsByEdge.Length; i++)
            {
                for (int j = 0; j < vis.EdgeCellIdsByEdge[i].Length; j++)
                {
                    id1 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][0];
                    id2 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][1];
                    edgeNodeIdsEdgeId.Add(new int[] { oldNodeIdNewId[id1], oldNodeIdNewId[id2], i + 1 });
                    //
                    if (vertexEdgeIds.TryGetValue(id1, out edgeIds)) edgeIds.Add(edgeId);
                    if (vertexEdgeIds.TryGetValue(id2, out edgeIds)) edgeIds.Add(edgeId);
                    //
                    edgeId++;
                }
            }
            //
            if (keepModelEdges) WriteEdges(sb, edgeNodeIdsEdgeId);
            // Ridges - all edges are ridges
            int[] ridgeIds = new int[vis.EdgeCells.Length];
            for (int i = 0; i < ridgeIds.Length; i++) ridgeIds[i] = i + 1;
            WriteRidges(sb, ridgeIds);
            // Required edges - keep edge cells connected to the vertices with only 2 edge cells: on the outside of the rectangle
            HashSet<int> requiredEdgeIds = new HashSet<int>();
            foreach (var cornerEntry in vertexEdgeIds)
            {
                if (cornerEntry.Value.Count == 2) requiredEdgeIds.UnionWith(cornerEntry.Value);
            }
            if (keepVertexEdges) WriteRequiredEdges(sb, requiredEdgeIds.ToArray());
            // End
            WriteEnd(sb);
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        private static void Write3D(string fileName, BasePart part, FeMesh mesh, bool keepModelEdges, bool keepVertexEdges)
        {
            VisualizationData vis = part.Visualization;
            // File
            StringBuilder sb = new StringBuilder();
            WriteHeading(sb);
            // Vertices                                                             
            Dictionary<int, int> oldNodeIdNewId;
            List<double[]> nodeCoorNodeId = GetNodeCoorNodeId(part.NodeLabels, mesh);
            WriteVertices(sb, nodeCoorNodeId, out oldNodeIdNewId);
            // Corners                                                              
            List<int> cornerIds = new List<int>();
            for (int i = 0; i < vis.VertexNodeIds.Length; i++) cornerIds.Add(oldNodeIdNewId[vis.VertexNodeIds[i]]);
            WriteCorners(sb, cornerIds);
            // Edges                                                                
            int id1, id2;
            int edgeId = 1;
            List<int> edgeIds;
            // Collect all edge cells connected to a vertex
            Dictionary<int, List<int>> vertexEdgeIds = new Dictionary<int, List<int>>();
            for (int i = 0; i < vis.VertexNodeIds.Length; i++)
                vertexEdgeIds.Add(oldNodeIdNewId[vis.VertexNodeIds[i]], new List<int>());
            //
            List<int[]> edgeNodeIdsEdgeId = new List<int[]>();
            for (int i = 0; i < vis.EdgeCellIdsByEdge.Length; i++)
            {
                for (int j = 0; j < vis.EdgeCellIdsByEdge[i].Length; j++)
                {
                    id1 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][0];
                    id2 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][1];
                    edgeNodeIdsEdgeId.Add(new int[] { oldNodeIdNewId[id1], oldNodeIdNewId[id2], i + 1 });
                    //
                    if (vertexEdgeIds.TryGetValue(id1, out edgeIds)) edgeIds.Add(edgeId);
                    if (vertexEdgeIds.TryGetValue(id2, out edgeIds)) edgeIds.Add(edgeId);
                    //
                    edgeId++;
                }
            }
            //
            if (keepModelEdges) WriteEdges(sb, edgeNodeIdsEdgeId);
            // Ridges - all edges are ridges                                        
            int[] ridgeIds = new int[vis.EdgeCells.Length];
            for (int i = 0; i < ridgeIds.Length; i++) ridgeIds[i] = i + 1;
            WriteRidges(sb, ridgeIds);
            // Required edges - keep edge cells connected to the vertices with only 2 edge cells: on the outside of the rectangle
            HashSet<int> requiredEdgeIds = new HashSet<int>();
            foreach (var cornerEntry in vertexEdgeIds)
            {
                if (cornerEntry.Value.Count == 2) requiredEdgeIds.UnionWith(cornerEntry.Value);
            }
            if (keepVertexEdges) WriteRequiredEdges(sb, requiredEdgeIds.ToArray());
            // Triangles                                                            
            int[] cell;
            List<int[]> elementNodeIdsSurfaceId = new List<int[]>();
            for (int i = 0; i < vis.CellIdsByFace.Length; i++)
            {
                for (int j = 0; j < vis.CellIdsByFace[i].Length; j++)
                {
                    cell = vis.Cells[vis.CellIdsByFace[i][j]];
                    // Linear
                    if (cell.Length == 3 || cell.Length == 6)   // reduce parabolic elements to linear
                    {
                        elementNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[cell[0]],
                                                                oldNodeIdNewId[cell[1]],
                                                                oldNodeIdNewId[cell[2]],
                                                                i + 1 });
                    }
                    else throw new CaeException("Mmg meshes only support triangular faces.");
                }
            }
            WriteTriangles(sb, elementNodeIdsSurfaceId);
            // Tetrahedrons                                                         
            int elementId;
            FeElement element;
            elementNodeIdsSurfaceId.Clear();
            for (int i = 0; i < part.Labels.Length; i++)
            {
                elementId = part.Labels[i];
                element = mesh.Elements[elementId];
                if (element is LinearTetraElement || element is ParabolicTetraElement)
                {
                    elementNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                                                            oldNodeIdNewId[element.NodeIds[1]],
                                                            oldNodeIdNewId[element.NodeIds[2]],
                                                            oldNodeIdNewId[element.NodeIds[3]],
                                                            part.PartId });
                }
                else throw new CaeException("Mmg meshes only support tetrahedral volume elements.");
            }
            WriteTetrahedrons(sb, elementNodeIdsSurfaceId);
            // End                                                                  
            WriteEnd(sb);
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        public static void WriteSolution(string fileName, BasePart part, FeMesh mesh, Dictionary<int, double> nodeIdValue)
        {
            // File
            StringBuilder sb = new StringBuilder();
            WriteHeading(sb);
            // Vertices order
            List<double[]> nodeCoorNodeId = GetNodeCoorNodeId(part.NodeLabels, mesh);
            // Solution at vertices
            WriteSolutionAtVertices(sb, nodeCoorNodeId, nodeIdValue);
            // End
            WriteEnd(sb);
            //
            File.WriteAllText(fileName, sb.ToString());
        }
       
        public static void Write(string fileName, BasePart[] parts, FeMesh mesh, bool keepModelEdges, bool keepVertexEdges)
        {
            // File
            StringBuilder sb = new StringBuilder();
            WriteHeading(sb);
            // Vertices                                                             
            HashSet<int> allNodeIds = new HashSet<int>();
            foreach (var part in parts) allNodeIds.UnionWith(part.NodeLabels);
            List<double[]> nodeCoorNodeId = GetNodeCoorNodeId(allNodeIds.ToArray(), mesh);
            Dictionary<int, int> oldNodeIdNewId;
            WriteVertices(sb, nodeCoorNodeId, out oldNodeIdNewId);
            // Corners                                                              
            HashSet<int> cornerIds = new HashSet<int>();
            VisualizationData vis;
            foreach (var part in parts)
            {
                vis = part.Visualization;
                for (int i = 0; i < vis.VertexNodeIds.Length; i++) cornerIds.Add(oldNodeIdNewId[vis.VertexNodeIds[i]]);
            }
            WriteCorners(sb, cornerIds);


            // Edges                                                                
            int id1, id2;
            int edgeId = 1;
            int edgeSegmentId = 1;
            List<int> edgeIds;
            List<int> ridgeIds = new List<int>();
            // Collect all edge cells connected to a vertex
            Dictionary<int, List<int>> vertexEdgeIds = new Dictionary<int, List<int>>();
            List<int[]> edgeNodeIdsEdgeId = new List<int[]>();
            //
            foreach (var part in parts)
            {
                vis = part.Visualization;
                //
                for (int i = 0; i < vis.VertexNodeIds.Length; i++)
                {
                    if (!vertexEdgeIds.ContainsKey(oldNodeIdNewId[vis.VertexNodeIds[i]]))
                        vertexEdgeIds.Add(oldNodeIdNewId[vis.VertexNodeIds[i]], new List<int>());
                }
                //
                for (int i = 0; i < vis.EdgeCellIdsByEdge.Length; i++)
                {
                    for (int j = 0; j < vis.EdgeCellIdsByEdge[i].Length; j++)
                    {
                        id1 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][0];
                        id2 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][1];
                        edgeNodeIdsEdgeId.Add(new int[] { oldNodeIdNewId[id1], oldNodeIdNewId[id2], edgeId });
                        //
                        if (vertexEdgeIds.TryGetValue(id1, out edgeIds)) edgeIds.Add(edgeSegmentId);
                        if (vertexEdgeIds.TryGetValue(id2, out edgeIds)) edgeIds.Add(edgeSegmentId);
                        //
                        ridgeIds.Add(edgeSegmentId);
                        //
                        edgeSegmentId++;
                    }
                    edgeId++;
                }
                //
            }
            if (keepModelEdges) WriteEdges(sb, edgeNodeIdsEdgeId);
            // Ridges - all edges are ridges                                        
            WriteRidges(sb, ridgeIds.ToArray());
            // Required edges - keep edge cells connected to the vertices with only 2 edge cells: on the outside of the rectangle
            HashSet<int> requiredEdgeIds = new HashSet<int>();
            foreach (var cornerEntry in vertexEdgeIds)
            {
                if (cornerEntry.Value.Count == 2) requiredEdgeIds.UnionWith(cornerEntry.Value);
            }
            if (keepVertexEdges) WriteRequiredEdges(sb, requiredEdgeIds.ToArray());
            // Surface elements                                                     
            int surfId = 1;
            int[] cell;
            List<int[]> triangleNodeIdsSurfaceId = new List<int[]>();
            List<int[]> quadNodeIdsSurfaceId = new List<int[]>();
            foreach (var part in parts)
            {
                vis = part.Visualization;
                //
                for (int i = 0; i < vis.CellIdsByFace.Length; i++)
                {
                    for (int j = 0; j < vis.CellIdsByFace[i].Length; j++)
                    {
                        cell = vis.Cells[vis.CellIdsByFace[i][j]];
                        // Triangles
                        if (cell.Length == 3 || cell.Length == 6)       // reduce parabolic elements to linear
                        {
                            triangleNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[cell[0]],
                                                                     oldNodeIdNewId[cell[1]],
                                                                     oldNodeIdNewId[cell[2]],
                                                                     surfId });
                        }
                        // Quads
                        else if (cell.Length == 4 || cell.Length == 8)  // reduce parabolic elements to linear
                        {
                            quadNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[cell[0]],
                                                                 oldNodeIdNewId[cell[1]],
                                                                 oldNodeIdNewId[cell[2]],
                                                                 oldNodeIdNewId[cell[3]],
                                                                 surfId });
                        }
                        else throw new NotSupportedException();
                    }
                    //
                    surfId++;
                }
            }
            WriteTriangles(sb, triangleNodeIdsSurfaceId);
            WriteQuadrilaterals(sb, quadNodeIdsSurfaceId);
            // Volume elements                                                      
            int elementId;
            FeElement element;
            List<int[]> tetraNodeIdsSurfaceId = new List<int[]>();
            List<int[]> hexaNodeIdsSurfaceId = new List<int[]>();
            //
            foreach (var part in parts)
            {
                for (int i = 0; i < part.Labels.Length; i++)
                {
                    elementId = part.Labels[i];
                    element = mesh.Elements[elementId];
                    if (element is LinearTetraElement || element is ParabolicTetraElement)
                    {
                        tetraNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                                                              oldNodeIdNewId[element.NodeIds[1]],
                                                              oldNodeIdNewId[element.NodeIds[2]],
                                                              oldNodeIdNewId[element.NodeIds[3]],
                                                              part.PartId });
                    }
                    //else if (element is LinearHexaElement || element is ParabolicHexaElement)
                    //{
                    //    hexaNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                    //                                         oldNodeIdNewId[element.NodeIds[1]],
                    //                                         oldNodeIdNewId[element.NodeIds[2]],
                    //                                         oldNodeIdNewId[element.NodeIds[3]],
                    //                                         oldNodeIdNewId[element.NodeIds[4]],
                    //                                         oldNodeIdNewId[element.NodeIds[5]],
                    //                                         part.PartId });
                    //}
                    else throw new CaeException("Mmg meshes only support tetrahedral volume elements.");
                }
            }
            WriteTetrahedrons(sb, tetraNodeIdsSurfaceId);
            WriteHexahedrons(sb, hexaNodeIdsSurfaceId);
            // End                                                                  
            WriteEnd(sb);
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        public static void WriteSolution(string fileName, BasePart[] parts, FeMesh mesh, Dictionary<int, double> nodeIdValue)
        {
            // File
            StringBuilder sb = new StringBuilder();
            WriteHeading(sb);
            // Vertices order
            HashSet<int> nodeIds = new HashSet<int>();
            foreach (var part in parts) nodeIds.UnionWith(part.NodeLabels);
            List<double[]> nodeCoorNodeId = GetNodeCoorNodeId(nodeIds.ToArray(), mesh);
            // Solution at vertices
            WriteSolutionAtVertices(sb, nodeCoorNodeId, nodeIdValue);
            // End
            WriteEnd(sb);
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        public static void WriteMaterial(string fileName, BasePart[] parts)
        {
            // File
            StringBuilder sb = new StringBuilder();
            //
            sb.AppendLine("LSReferences");
            sb.AppendLine(parts.Length.ToString());
            sb.AppendLine();
            //
            int maxParId = -1;
            foreach (var part in parts) if (part.PartId > maxParId) maxParId = part.PartId;
            //
            foreach (var part in parts)
            {
                sb.AppendFormat("{0} {1} {2}{3}", part.PartId, maxParId + 1, maxParId + 2, Environment.NewLine);
                maxParId += 2;
            }
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        //
        public static void WriteShellElementsFix(string fileName, int[] elementIds, BasePart part, FeMesh mesh,
                                                 bool keepModelEdges)
        {
            VisualizationData vis = part.Visualization;
            // File
            StringBuilder sb = new StringBuilder();
            WriteHeading(sb);
            // Vertices
            Dictionary<int, int> oldNodeIdNewId;
            List<double[]> nodeCoorNodeId = GetNodeCoorNodeId(part.NodeLabels, mesh);
            WriteVertices(sb, nodeCoorNodeId, out oldNodeIdNewId);
            // Corners
            List<int> cornerIds = new List<int>();
            for (int i = 0; i < vis.VertexNodeIds.Length; i++) cornerIds.Add(oldNodeIdNewId[vis.VertexNodeIds[i]]);
            WriteCorners(sb, cornerIds);
            // Elements
            int elementId;
            FeElement element;
            List<int[]> triangleNodeIdsSurfaceId = new List<int[]>();
            List<int[]> quadNodeIdsSurfaceId = new List<int[]>();
            for (int i = 0; i < vis.CellIdsByFace.Length; i++)
            {
                for (int j = 0; j < vis.CellIdsByFace[i].Length; j++)
                {
                    elementId = vis.CellIds[vis.CellIdsByFace[i][j]];
                    element = mesh.Elements[elementId];
                    if (element is LinearTriangleElement || element is ParabolicTriangleElement)
                    {
                        triangleNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                                                                 oldNodeIdNewId[element.NodeIds[1]],
                                                                 oldNodeIdNewId[element.NodeIds[2]],
                                                                 i + 1 });
                    }
                    else if (element is LinearQuadrilateralElement || element is ParabolicQuadrilateralElement)
                    {
                        quadNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                                                             oldNodeIdNewId[element.NodeIds[1]],
                                                             oldNodeIdNewId[element.NodeIds[2]],
                                                             oldNodeIdNewId[element.NodeIds[3]],
                                                             i + 1 });
                    }
                    else throw new NotSupportedException();
                }
            }
            // Triangles
            WriteTriangles(sb, triangleNodeIdsSurfaceId);
            // Quadrilaterals
            WriteQuadrilaterals(sb, quadNodeIdsSurfaceId);
            // RequiredElements                                                                     
            int[] requiredTriangleIds = part.Labels.Except(elementIds).ToArray();
            WriteRequiredTriangles(sb, requiredTriangleIds);
            // Edges
            int id1, id2;
            List<int[]> edgeNodeIdsEdgeId = new List<int[]>();
            for (int i = 0; i < vis.EdgeCellIdsByEdge.Length; i++)
            {
                for (int j = 0; j < vis.EdgeCellIdsByEdge[i].Length; j++)
                {
                    id1 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][0];
                    id2 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][1];
                    edgeNodeIdsEdgeId.Add(new int[] { oldNodeIdNewId[id1], oldNodeIdNewId[id2], i + 1 });
                }
            }
            //
            if (keepModelEdges) WriteEdges(sb, edgeNodeIdsEdgeId);
            // Ridges - all edges are ridges
            int[] ridgeIds = new int[vis.EdgeCells.Length];
            for (int i = 0; i < ridgeIds.Length; i++) ridgeIds[i] = i + 1;
            WriteRidges(sb, ridgeIds);
            // Required edges - keep edge cells connected to the vertices with only 2 edge cells: on the outside of the rectangle
            //HashSet<int> requiredEdgeIds = new HashSet<int>();
            //foreach (var cornerEntry in vertexEdgeIds)
            //{
            //    if (cornerEntry.Value.Count == 2) requiredEdgeIds.UnionWith(cornerEntry.Value);
            //}
            //if (keepVetexEdges) WriteRequiredEdges(sb, requiredEdgeIds.ToArray());
            // End
            WriteEnd(sb);
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        public static void WriteShellElements(string fileName, int[] elementIds, BasePart part, FeMesh mesh, bool keepModelEdges,
                                              out Dictionary<int[], FeNode> midNodes)
        {
            CompareIntArray comparer = new CompareIntArray();
            midNodes = new Dictionary<int[], FeNode>(comparer);
            Dictionary<int[], FeNode> allMidNodes = new Dictionary<int[], FeNode>(comparer);
            VisualizationData vis = part.Visualization;
            // File                                                                                 
            StringBuilder sb = new StringBuilder();
            WriteHeading(sb);
            // Vertices                                                                             
            int nodeId;
            int numOfSignificantNodes;
            FeNode node;
            FeElement element;
            Dictionary<int, int> oldNodeIdNewId;
            HashSet<int> allNodeIds = new HashSet<int>();
            List<double[]> nodeCoorNodeId = new List<double[]>();
            // Use only vertices of the selected element set
            for (int i = 0; i < elementIds.Length; i++)
            {
                element = mesh.Elements[elementIds[i]];
                if (element is LinearTriangleElement || element is ParabolicTriangleElement)
                    numOfSignificantNodes = 3;
                else if (element is LinearQuadrilateralElement || element is ParabolicQuadrilateralElement)
                    numOfSignificantNodes = 4;
                else throw new NotSupportedException();
                //
                for (int j = 0; j < numOfSignificantNodes; j++)
                {
                    nodeId = element.NodeIds[j];
                    if (!allNodeIds.Contains(nodeId))
                    {
                        node = mesh.Nodes[nodeId];
                        nodeCoorNodeId.Add(new double[] { node.X, node.Y, node.Z, node.Id });
                        allNodeIds.Add(nodeId);
                    }
                }
            }
            WriteVertices(sb, nodeCoorNodeId, out oldNodeIdNewId);
            // Corners                                                                              
            List<int> cornerIds = new List<int>();
            for (int i = 0; i < vis.VertexNodeIds.Length; i++)
            {
                nodeId = vis.VertexNodeIds[i];
                if (allNodeIds.Contains(nodeId)) cornerIds.Add(oldNodeIdNewId[nodeId]);
            }
            WriteCorners(sb, cornerIds);
            // Elements                                                                             
            int elementId;
            int[] key;
            bool parabolic;
            HashSet<int> allElementIds = new HashSet<int>(elementIds); // for speedup
            List<int[]> triangleNodeIdsSurfaceId = new List<int[]>();
            List<int[]> quadNodeIdsSurfaceId = new List<int[]>();
            HashSet<int[]> allElementEdges = new HashSet<int[]>(comparer);
            for (int i = 0; i < vis.CellIdsByFace.Length; i++)
            {
                for (int j = 0; j < vis.CellIdsByFace[i].Length; j++)
                {
                    elementId = vis.CellIds[vis.CellIdsByFace[i][j]];
                    if (allElementIds.Contains(elementId))
                    {
                        element = mesh.Elements[elementId];
                        if (element is LinearTriangleElement || element is ParabolicTriangleElement)
                        {
                            triangleNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                                                                     oldNodeIdNewId[element.NodeIds[1]],
                                                                     oldNodeIdNewId[element.NodeIds[2]],
                                                                     i + 1 });
                            //
                            parabolic = element is ParabolicTriangleElement;
                            //
                            key = Tools.GetSortedKey(element.NodeIds[0], element.NodeIds[1]);
                            if (!allElementEdges.Contains(key))
                            {
                                allElementEdges.Add(key);
                                if (parabolic) allMidNodes.Add(key, mesh.Nodes[element.NodeIds[3]]);
                            }
                            key = Tools.GetSortedKey(element.NodeIds[1], element.NodeIds[2]);
                            if (!allElementEdges.Contains(key))
                            {
                                allElementEdges.Add(key);
                                if (parabolic) allMidNodes.Add(key, mesh.Nodes[element.NodeIds[4]]);
                            }
                            key = Tools.GetSortedKey(element.NodeIds[2], element.NodeIds[0]);
                            if (!allElementEdges.Contains(key))
                            {
                                allElementEdges.Add(key);
                                if (parabolic) allMidNodes.Add(key, mesh.Nodes[element.NodeIds[5]]);
                            }
                        }
                        else if (element is LinearQuadrilateralElement || element is ParabolicQuadrilateralElement)
                        {
                            triangleNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                                                                     oldNodeIdNewId[element.NodeIds[1]],
                                                                     oldNodeIdNewId[element.NodeIds[2]],
                                                                     i + 1 });
                            triangleNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                                                                     oldNodeIdNewId[element.NodeIds[2]],
                                                                     oldNodeIdNewId[element.NodeIds[3]],
                                                                     i + 1 });
                            //
                            parabolic = element is ParabolicQuadrilateralElement;
                            //
                            key = Tools.GetSortedKey(element.NodeIds[0], element.NodeIds[1]);
                            if (!allElementEdges.Contains(key))
                            {
                                allElementEdges.Add(key);
                                if (parabolic) allMidNodes.Add(key, mesh.Nodes[element.NodeIds[4]]);
                            }
                            key = Tools.GetSortedKey(element.NodeIds[1], element.NodeIds[2]);
                            if (!allElementEdges.Contains(key))
                            {
                                allElementEdges.Add(key);
                                if (parabolic) allMidNodes.Add(key, mesh.Nodes[element.NodeIds[5]]);
                            }
                            key = Tools.GetSortedKey(element.NodeIds[2], element.NodeIds[3]);
                            if (!allElementEdges.Contains(key))
                            {
                                allElementEdges.Add(key);
                                if (parabolic) allMidNodes.Add(key, mesh.Nodes[element.NodeIds[6]]);
                            }
                            key = Tools.GetSortedKey(element.NodeIds[3], element.NodeIds[0]);
                            if (!allElementEdges.Contains(key))
                            {
                                allElementEdges.Add(key);
                                if (parabolic) allMidNodes.Add(key, mesh.Nodes[element.NodeIds[7]]);
                            }
                            //quadNodeIdsSurfaceId.Add(new int[] { oldNodeIdNewId[element.NodeIds[0]],
                            //                                     oldNodeIdNewId[element.NodeIds[1]],
                            //                                     oldNodeIdNewId[element.NodeIds[2]],
                            //                                     oldNodeIdNewId[element.NodeIds[3]],
                            //                                     i + 1 });
                        }
                        else throw new NotSupportedException();
                    }
                }
            }
            // Triangles
            WriteTriangles(sb, triangleNodeIdsSurfaceId);
            // Quadrilaterals
            WriteQuadrilaterals(sb, quadNodeIdsSurfaceId);
            // Edges                                                                                
            int id1, id2;
            Dictionary<int[], int> edgeKeys = new Dictionary<int[], int>(comparer);
            List<int[]> edgeNodeIdsEdgeId = new List<int[]>();
            // Free edges
            HashSet<int[]> freeEdgeCells = GetFreeEdgeCells(part);
            // Model edges - first since they are numbered by the EdgeCellIdsByEdge
            if (keepModelEdges)
            {
                for (int i = 0; i < vis.EdgeCellIdsByEdge.Length; i++)
                {
                    for (int j = 0; j < vis.EdgeCellIdsByEdge[i].Length; j++)
                    {
                        id1 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][0];
                        id2 = vis.EdgeCells[vis.EdgeCellIdsByEdge[i][j]][1];
                        key = Tools.GetSortedKey(id1, id2);
                        if (!edgeKeys.ContainsKey(key) && allElementEdges.Contains(key))
                        {
                            edgeNodeIdsEdgeId.Add(new int[] { oldNodeIdNewId[id1], oldNodeIdNewId[id2], i + 1 });
                            edgeKeys.Add(key, edgeNodeIdsEdgeId.Count);
                        }
                    }
                }
            }
            // Other part nodeIds
            HashSet<int> otherVisualizationNodeIds = new HashSet<int>();
            foreach (var entry in mesh.Parts)
            {
                if (entry.Value != part)
                    otherVisualizationNodeIds.UnionWith(entry.Value.Visualization.GetNodeIds());
            }
            // Boundary edges - free edges of the new subPart
            int edgeId;
            int[] edgeCell;
            List<int> requiredEdgeIds = new List<int>();
            GeometryPart subPart = mesh.CreateGeometryPartFromElementIds(elementIds);
            for (int i = 0; i < subPart.FreeEdgeCellIds.Length; i++)
            {
                edgeCell = subPart.Visualization.EdgeCells[subPart.FreeEdgeCellIds[i]];
                id1 = edgeCell[0];
                id2 = edgeCell[1];
                key = Tools.GetSortedKey(id1, id2);
                // Add boundary edges to all edges
                if (!edgeKeys.TryGetValue(key, out edgeId))
                {
                    edgeNodeIdsEdgeId.Add(new int[] { oldNodeIdNewId[id1], oldNodeIdNewId[id2], 0 });
                    edgeId = edgeNodeIdsEdgeId.Count;
                    edgeKeys.Add(key, edgeId);
                }
                // Free part edge nodes must not be kept as midside nodes
                if (!freeEdgeCells.Contains(key))
                {
                    requiredEdgeIds.Add(edgeId);
                    if (allMidNodes.Count > 0) midNodes.Add(key, allMidNodes[key]);
                }
                // Keep free edge midside nodes if they belong to other assembly parts
                else
                {
                    requiredEdgeIds.Add(edgeId);
                    if (allMidNodes.Count > 0 && otherVisualizationNodeIds.Contains(allMidNodes[key].Id))
                        midNodes.Add(key, allMidNodes[key]);
                }
            }
            WriteEdges(sb, edgeNodeIdsEdgeId);
            // Ridges - sharp or triple edges between faces - all edges are ridges                  
            int[] ridgeIds = new int[edgeNodeIdsEdgeId.Count];
            for (int i = 0; i < ridgeIds.Length; i++) ridgeIds[i] = i + 1;
            WriteRidges(sb, ridgeIds);
            // Required edges                                                                       
            WriteRequiredEdges(sb, requiredEdgeIds.ToArray());
            // End                                                                                  
            WriteEnd(sb);
            //
            if (midNodes.Count == 0) midNodes = null;
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        //                                                                                                                          
        private static void WriteHeading(StringBuilder sb)
        {            
            sb.AppendLine("MeshVersionFormatted 2");
            sb.AppendLine("Dimension 3");
        }
        //
        private static List<double[]> GetNodeCoorNodeId(int[] nodeIds, FeMesh mesh)
        {
            // The same order is used for writing solution values
            FeNode node;
            List<double[]> nodeCoorNodeId = new List<double[]>(); 
            foreach (var nodeId in nodeIds)
            {
                node = mesh.Nodes[nodeId];
                nodeCoorNodeId.Add(new double[] { node.X, node.Y, node.Z, node.Id });
            }
            return nodeCoorNodeId;
        }
        private static void WriteVertices(StringBuilder sb, List<double[]> nodeCoorNodeId,
                                          out Dictionary<int, int> oldNodeIdNewId)
        {
            int count = 1;
            oldNodeIdNewId = new Dictionary<int, int>();
            //
            if (nodeCoorNodeId == null || nodeCoorNodeId.Count == 0) return;
            // Vertices
            sb.AppendLine();
            sb.AppendLine("Vertices");
            sb.AppendLine(nodeCoorNodeId.Count.ToString());
            //
            foreach (var nodeData in nodeCoorNodeId)
            {
                sb.AppendFormat("{0} {1} {2} {3}{4}", nodeData[0], nodeData[1], nodeData[2],
                                                      (int)(nodeData[3]), Environment.NewLine);
                oldNodeIdNewId.Add((int)nodeData[3], count);
                count++;
            }
        }
        private static void WriteCorners(StringBuilder sb, IEnumerable<int> cornerIds)
        {
            if (cornerIds == null || cornerIds.Count() == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("Corners");
            sb.AppendLine(cornerIds.Count().ToString());
            //
            foreach (var cornerId in cornerIds)
            {
                sb.AppendFormat("{0}{1}", cornerId, Environment.NewLine);
            }
        }
        private static void WriteSolutionAtVertices(StringBuilder sb, List<double[]> nodeCoorNodeId,
                                                    Dictionary<int, double> nodeIdValue)
        {
            if (nodeCoorNodeId == null || nodeCoorNodeId.Count == 0) return;
            // Vertices
            sb.AppendLine();
            sb.AppendLine("SolAtVertices");
            sb.AppendLine(nodeCoorNodeId.Count.ToString());
            sb.AppendLine("1 1");
            //
            int nodeId;
            double value;
            foreach (var nodeData in nodeCoorNodeId)
            {
                nodeId = (int)nodeData[3];
                value = (double)nodeIdValue[nodeId];
                //
                sb.AppendFormat("{0}{1}", value, Environment.NewLine);
            }
        }
        //
        private static void WriteEdges(StringBuilder sb, List<int[]> edgeCellEdgeId)
        {
            if (edgeCellEdgeId == null || edgeCellEdgeId.Count == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("Edges");
            sb.AppendLine(edgeCellEdgeId.Count.ToString());
            foreach (var edgeData in edgeCellEdgeId)
            {
                sb.AppendFormat("{0} {1} {2}{3}", edgeData[0],
                                                  edgeData[1],
                                                  edgeData[2],
                                                  Environment.NewLine);
            }
        }
        private static void WriteRidges(StringBuilder sb, int[] ridgeIds)
        {
            if (ridgeIds == null || ridgeIds.Length == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("Ridges");
            sb.AppendLine(ridgeIds.Length.ToString());
            for (int i = 0; i < ridgeIds.Length; i++)
            {
                sb.AppendFormat("{0}{1}", ridgeIds[i], Environment.NewLine);
            }
        }
        private static void WriteRequiredEdges(StringBuilder sb, int[] requiredEdgeIds)
        {
            if (requiredEdgeIds == null || requiredEdgeIds.Length == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("RequiredEdges");
            sb.AppendLine(requiredEdgeIds.Length.ToString());
            for (int i = 0; i < requiredEdgeIds.Length; i++)
            {
                sb.AppendFormat("{0}{1}", requiredEdgeIds[i], Environment.NewLine);
            }
        }
        //
        private static void WriteTriangles(StringBuilder sb, List<int[]> elementNodeIdsElementId)
        {
            if (elementNodeIdsElementId == null || elementNodeIdsElementId.Count == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("Triangles");
            sb.AppendLine(elementNodeIdsElementId.Count.ToString());
            //
            foreach (var elementData in elementNodeIdsElementId)
            {
                sb.AppendFormat("{0} {1} {2} {3}{4}", elementData[0],
                                                      elementData[1],
                                                      elementData[2],
                                                      elementData[3],
                                                      Environment.NewLine);
            }
        }
        private static void WriteQuadrilaterals(StringBuilder sb, List<int[]> elementNodeIdsElementId)
        {
            if (elementNodeIdsElementId == null || elementNodeIdsElementId.Count == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("Quadrilaterals");
            sb.AppendLine(elementNodeIdsElementId.Count.ToString());
            //
            foreach (var elementData in elementNodeIdsElementId)
            {
                sb.AppendFormat("{0} {1} {2} {3} {4}{5}", elementData[0],
                                                          elementData[1],
                                                          elementData[2],
                                                          elementData[3],
                                                          elementData[4],   //  id
                                                          Environment.NewLine);
            }
        }
        private static void WriteRequiredTriangles(StringBuilder sb, int[] requiredTrianglesIds)
        {
            if (requiredTrianglesIds == null || requiredTrianglesIds.Length == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("RequiredTriangles");
            sb.AppendLine(requiredTrianglesIds.Length.ToString());
            for (int i = 0; i < requiredTrianglesIds.Length; i++)
            {
                sb.AppendFormat("{0}{1}", requiredTrianglesIds[i], Environment.NewLine);
            }
        }
        private static void WriteTetrahedrons(StringBuilder sb, List<int[]> elementNodeIdsElementId)
        {
            if (elementNodeIdsElementId == null || elementNodeIdsElementId.Count == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("Tetrahedra");
            sb.AppendLine(elementNodeIdsElementId.Count.ToString());
            //
            foreach (var elementData in elementNodeIdsElementId)
            {
                sb.AppendFormat("{0} {1} {2} {3} {4}{5}", elementData[0],
                                                          elementData[1],
                                                          elementData[2],
                                                          elementData[3],
                                                          elementData[4],
                                                          Environment.NewLine);
            }
        }
        private static void WriteHexahedrons(StringBuilder sb, List<int[]> elementNodeIdsElementId)
        {
            if (elementNodeIdsElementId == null || elementNodeIdsElementId.Count == 0) return;
            //
            sb.AppendLine();
            sb.AppendLine("Hexahedra");
            sb.AppendLine(elementNodeIdsElementId.Count.ToString());
            //
            foreach (var elementData in elementNodeIdsElementId)
            {
                sb.AppendFormat("{0} {1} {2} {3} {4} {5} {6}{7}", elementData[0],
                                                                  elementData[1],
                                                                  elementData[2],
                                                                  elementData[3],
                                                                  elementData[4],
                                                                  elementData[5],
                                                                  elementData[6],
                                                                  Environment.NewLine);
            }
        }
        //
        private static void WriteEnd(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("End");
        }
        //
        private static HashSet<int[]> GetFreeEdgeCells(BasePart part)
        {
            int[] key;
            int[] edgeCell;
            CompareIntArray comparer = new CompareIntArray();
            HashSet<int[]> freeEdgeCells = new HashSet<int[]>(comparer);
            HashSet<int> freeEdgeIds = part.Visualization.GetFreeEdgeIds();
            //
            foreach (var edgeId in freeEdgeIds)
            {
                foreach (var edgeCellId in part.Visualization.EdgeCellIdsByEdge[edgeId])
                {
                    edgeCell = part.Visualization.EdgeCells[edgeCellId];
                    key = Tools.GetSortedKey(edgeCell[0], edgeCell[1]);
                    freeEdgeCells.Add(key);
                }
            }
            return freeEdgeCells;

            //int[][] cells = part.Visualization.Cells;
            //CompareIntArray comparer = new CompareIntArray();
            //Dictionary<int[], byte[]> allEdges = new Dictionary<int[], byte[]>(comparer);
            ////
            //int[] key;
            //byte[] count;
            //int[][] cellEdges;
            //// Get all edges
            //for (int i = 0; i < cells.Length; i++)
            //{
            //    cellEdges = FeMesh.GetVisualizationEdgeCells(cells[i], ElementFaceType.Face);
            //    //
            //    foreach (var cellEdge in cellEdges)
            //    {
            //        key = Tools.GetSortedKey(cellEdge[0], cellEdge[1]);
            //        if (allEdges.TryGetValue(key, out count)) count[0]++;
            //        else allEdges.Add(key, new byte[] { 1 });
            //    }
            //}
            ////
            //HashSet<int[]> freeEdges = new HashSet<int[]>(comparer);
            //foreach (var entry in allEdges)
            //{
            //    if (entry.Value[0] == 1) freeEdges.Add(entry.Key);
            //}
            ////
            //return freeEdges;
        }        
        private static FeNode GetOrCreateMidNode(FeNode n1, FeNode n2, ref Dictionary<int[], FeNode> midNodes, ref int maxNodeId)
        {
            int[] ids;
            if (n1.Id < n2.Id) ids = new int[] { n1.Id, n2.Id };
            else ids = new int[] { n2.Id, n1.Id };
            //
            FeNode newNode;
            if (!midNodes.TryGetValue(ids, out newNode))
            {
                maxNodeId++;
                newNode = new FeNode(maxNodeId, GetMidNodeCoor(n1, n2));
                midNodes.Add(ids, newNode);
            }
            return newNode;
        }
        private static double[] GetMidNodeCoor(FeNode n1, FeNode n2)
        {
            double[] coor = new double[3];
            coor[0] = 0.5 * (n1.X + n2.X);
            coor[1] = 0.5 * (n1.Y + n2.Y);
            coor[2] = 0.5 * (n1.Z + n2.Z);
            return coor;
        }
    }
}
