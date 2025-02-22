﻿using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Forms.VisualStyles;

namespace CaeMesh
{
    [Serializable]
    public class VisualizationData
    {
        // vertex
        // edge     - parabolic edge [0, 1, 2] is visually discretized like 0----2----1
        // face     - cells limited by edges
        // surface  - can be arbitrary combination of cells

        // Variables                                                                                                                
        protected int[][] _cells;
        protected int[] _cellIds;
        protected int[][] _cellIdsByFace;        // could be hashSet<> but serialization could be bad
        protected double[] _faceAreas;
        protected GeomFaceType[] _faceTypes;
        protected int[][] _faceEdgeIds;
        protected int[][][] _cellIdCellIdEdgeNodeIds;
        protected int[][] _cellNeighboursOverCellEdge;
        protected int[][] _edgeCells;
        protected int[][] _edgeCellIdsByEdge;
        protected double[] _edgeLengths;
        protected GeomCurveType[] _edgeTypes;
        protected int[] _vertexNodeIds;
        //
        [NonSerialized]
        private Dictionary<int[], CellNeighbour> _cellNeighboursOverCell; // this are solid cell neighbours to extract surface


        // Properties                                                                                                               
        /// <summary>
        /// Cells
        /// [0...num. of cells][0...num. of nodes] -> global node id
        /// </summary>
        public int[][] Cells { get { return _cells; } set { _cells = value; } }

        /// <summary>
        /// CellIds
        /// [0...num. of cells] -> global element id
        /// </summary>
        public int[] CellIds { get { return _cellIds; } set { _cellIds = value; } }

        /// <summary>
        /// FaceCount
        /// Return number of faces
        /// </summary>
        public int FaceCount { get { return _cellIdsByFace != null ? _cellIdsByFace.Length : 0; } }

        /// <summary>
        /// CellIdsByFace
        /// [0...num. of faces][0...num. of face cells] -> local cell id
        /// </summary>
        public int[][] CellIdsByFace
        {
            get { return _cellIdsByFace; }
            set
            {
                _cellIdsByFace = value;
                if (_cellIdsByFace != null)
                {
                    // sort ids for binary search
                    for (int i = 0; i < _cellIdsByFace.Length; i++) Array.Sort(_cellIdsByFace[i]);
                }
            }
        }

        /// <summary>
        /// FaceAreas
        /// [0...num. of faces]
        /// </summary>
        public double[] FaceAreas
        {
            get { return _faceAreas; }
            set { _faceAreas = value; }
        }

        /// <summary>
        /// FaceTypes
        /// [0...num. of faces]
        /// </summary>
        public GeomFaceType[] FaceTypes
        {
            get { return _faceTypes; }
            set { _faceTypes = value; }
        }

        /// <summary>
        /// EdgeCount
        /// Return number of edges
        /// </summary>
        public int EdgeCount { get { return _edgeCellIdsByEdge.Length; } }

        /// <summary>
        /// FaceEdgeIds
        /// [0...num. of faces][0...num. of edges] -> local edge id
        /// </summary>
        public int[][] FaceEdgeIds { get { return _faceEdgeIds; } set { _faceEdgeIds = value; } }

        /// <summary>
        /// CellIdCellIdEdgeNodeIds
        /// [0...num. of cells][0...num. of neigh.] -> edge node ids
        /// </summary>
        public int[][][] CellIdCellIdEdgeNodeIds
        {
            get { return _cellIdCellIdEdgeNodeIds; }
            set { _cellIdCellIdEdgeNodeIds = value; }
        }

        /// <summary>
        /// CellNeighboursOverEdge
        /// [0...num. of cells][0...num. of neigh.] -> local cell id
        /// </summary>
        public int[][] CellNeighboursOverCellEdge
        {
            get { return _cellNeighboursOverCellEdge; }
            set { _cellNeighboursOverCellEdge = value; }
        }

        /// <summary>
        /// EdgeCells
        /// [0...num. of edge cells][0...num. of nodes] -> global node id
        /// </summary>
        public int[][] EdgeCells { get { return _edgeCells; } set { _edgeCells = value; } }

        /// <summary>
        /// EdgeCellIdsByEdge
        /// [0...num. of edges][0...num. of edge cells] -> local edge cell id
        /// </summary>
        public int[][] EdgeCellIdsByEdge { get { return _edgeCellIdsByEdge; } set { _edgeCellIdsByEdge = value; } }

        /// <summary>
        /// EdgeLengths
        /// [0...num. of edges]
        /// </summary>
        public double[] EdgeLengths
        {
            get { return _edgeLengths; }
            set { _edgeLengths = value; }
        }

        /// <summary>
        /// EdgeTypes
        /// [0...num. of edges]
        /// </summary>
        public GeomCurveType[] EdgeTypes
        {
            get { return _edgeTypes; }
            set { _edgeTypes = value; }
        }

        /// <summary>
        /// VertexNodeIds
        /// [0...num. of vertices] -> global node id (a vertice is a node where more than two edge cells meet)
        /// </summary>
        public int[] VertexNodeIds { get { return _vertexNodeIds; } set { _vertexNodeIds = value; } }


        // Constructors                                                                                                             
        public VisualizationData()
        {
            _cells = null;
            _cellIds = null;
            _cellIdsByFace = null;
            _faceAreas = null;
            _faceTypes = null;
            _faceEdgeIds = null;
            _cellNeighboursOverCellEdge = null;
            _edgeCells = null;
            _edgeCellIdsByEdge = null;
            _edgeLengths = null;
            _edgeTypes = null;
            _vertexNodeIds = null;
            ResetCellNeighboursOverCell();
        }
        public VisualizationData(VisualizationData visualization)
            : this()
        {
            if (visualization.Cells != null)
            {
                _cells = new int[visualization.Cells.Length][];
                for (int i = 0; i < _cells.Length; i++)
                    _cells[i] = visualization.Cells[i].ToArray();
            }

            _cellIds = visualization.CellIds != null ? visualization.CellIds.ToArray() : null;
            //
            if (visualization.CellIdsByFace != null)
            {
                _cellIdsByFace = new int[visualization.CellIdsByFace.Length][];
                for (int i = 0; i < _cellIdsByFace.Length; i++)
                    _cellIdsByFace[i] = visualization.CellIdsByFace[i].ToArray();
            }
            //
            _faceAreas = visualization.FaceAreas != null ? visualization.FaceAreas.ToArray() : null;
            // Bug fix - visualization.FaceTypes can become an array of integers that cannot be directly copied using ToArray
            // Probable reason is a PrePoMax crash and model rebuild
            if (visualization.FaceTypes != null && visualization.FaceTypes.Length > 0)
            {
                _faceTypes = new GeomFaceType[visualization.FaceTypes.Length];
                for (int i = 0; i < _faceTypes.Length; i++) _faceTypes[i] = visualization.FaceTypes[i];
            }
            else _faceTypes = null;
            //
            if (visualization.FaceEdgeIds != null)
            {
                _faceEdgeIds = new int[visualization.FaceEdgeIds.Length][];
                for (int i = 0; i < _faceEdgeIds.Length; i++)
                    _faceEdgeIds[i] = visualization.FaceEdgeIds[i].ToArray();
            }
            //
            if (visualization.CellIdCellIdEdgeNodeIds != null)
            {
                _cellIdCellIdEdgeNodeIds = new int[visualization.CellIdCellIdEdgeNodeIds.Length][][];
                for (int i = 0; i < _cellIdCellIdEdgeNodeIds.Length; i++)
                {
                    if (visualization.CellIdCellIdEdgeNodeIds[i] != null)
                    {
                        _cellIdCellIdEdgeNodeIds[i] = new int[visualization.CellIdCellIdEdgeNodeIds[i].Length][];
                        for (int j = 0; j < _cellIdCellIdEdgeNodeIds[i].Length; j++)
                        {
                            _cellIdCellIdEdgeNodeIds[i][j] = visualization.CellIdCellIdEdgeNodeIds[i][j].ToArray();
                        }
                    }
                }
            }
            //
            if (visualization.CellNeighboursOverCellEdge != null)
            {
                _cellNeighboursOverCellEdge = new int[visualization.CellNeighboursOverCellEdge.Length][];
                for (int i = 0; i < _cellNeighboursOverCellEdge.Length; i++)
                {
                    if (visualization.CellNeighboursOverCellEdge[i] != null)
                        _cellNeighboursOverCellEdge[i] = visualization.CellNeighboursOverCellEdge[i].ToArray();
                }
            }
            //
            if (visualization.EdgeCells != null)
            {
                _edgeCells = new int[visualization.EdgeCells.Length][];
                for (int i = 0; i < _edgeCells.Length; i++) _edgeCells[i] = visualization.EdgeCells[i].ToArray();
            }
            //
            if (visualization.EdgeCellIdsByEdge != null)
            {
                _edgeCellIdsByEdge = new int[visualization.EdgeCellIdsByEdge.Length][];
                for (int i = 0; i < _edgeCellIdsByEdge.Length; i++)
                    _edgeCellIdsByEdge[i] = visualization.EdgeCellIdsByEdge[i].ToArray();
            }
            //
            _edgeLengths = visualization.EdgeLengths != null ? visualization.EdgeLengths.ToArray() : null;
            //
            // Bug fix - visualization.EdgeTypes can become an array of integers that cannot be directly copied using ToArray
            // Probable reason is a PrePoMax crash and model rebuild
            if (visualization.EdgeTypes != null && visualization.EdgeTypes.Length > 0)
            {
                _edgeTypes = new GeomCurveType[visualization.EdgeTypes.Length];
                for (int i = 0; i < _edgeTypes.Length; i++) _edgeTypes[i] = visualization.EdgeTypes[i];
            }
            else _edgeTypes = null;
            //
            _vertexNodeIds = visualization.VertexNodeIds != null ? visualization.VertexNodeIds.ToArray() : null;
            // Reference exchange - for speed and memory - might be a problem
            _cellNeighboursOverCell = visualization._cellNeighboursOverCell != null ? visualization._cellNeighboursOverCell : null;
        }


        // Static methods                                                                                                           
        public static void WriteToBinaryStream(VisualizationData visualizationData, System.IO.BinaryWriter bw)
        {
            if (visualizationData == null)
            {
                bw.Write(-1);
            }
            else
            {
                bw.Write(1);    // must be here
                //
                ReadWrite.WriteToBinaryStream(visualizationData._cells, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._cellIds, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._cellIdsByFace, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._faceAreas, bw);
                WriteToBinaryStream((int[])(object)visualizationData._faceTypes, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._faceEdgeIds, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._cellNeighboursOverCellEdge, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._edgeCells, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._edgeCellIdsByEdge, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._edgeLengths, bw);
                WriteToBinaryStream((int[])(object)visualizationData._edgeTypes, bw);
                ReadWrite.WriteToBinaryStream(visualizationData._vertexNodeIds, bw);
            }
        }
        public static void ReadFromBinaryStream(out VisualizationData visualizationData, System.IO.BinaryReader br, int version)
        {
            int exists = br.ReadInt32();
            if (exists <= -1) visualizationData = null;
            else
            {
                int[] types;
                visualizationData = new VisualizationData();
                ReadWrite.ReadFromBinaryStream(out visualizationData._cells, br);
                ReadWrite.ReadFromBinaryStream(out visualizationData._cellIds, br);
                ReadWrite.ReadFromBinaryStream(out visualizationData._cellIdsByFace, br);
                ReadWrite.ReadFromBinaryStream(out visualizationData._faceAreas, br);
                ReadFromBinaryStream(out types, br);
                visualizationData._faceTypes = (GeomFaceType[])(object)types;
                ReadWrite.ReadFromBinaryStream(out visualizationData._faceEdgeIds, br);
                ReadWrite.ReadFromBinaryStream(out visualizationData._cellNeighboursOverCellEdge, br);
                ReadWrite.ReadFromBinaryStream(out visualizationData._edgeCells, br);
                ReadWrite.ReadFromBinaryStream(out visualizationData._edgeCellIdsByEdge, br);
                ReadWrite.ReadFromBinaryStream(out visualizationData._edgeLengths, br);
                if (version > 1_004_001)
                {
                    ReadFromBinaryStream(out types, br);
                    visualizationData._edgeTypes = (GeomCurveType[])(object)types;
                }
                ReadWrite.ReadFromBinaryStream(out visualizationData._vertexNodeIds, br);
            }
        }
        private static void WriteToBinaryStream(int[] data, System.IO.BinaryWriter bw)
        {
            if (data == null)
            {
                bw.Write(-1);
            }
            else
            {
                bw.Write(data.Length);
                for (int i = 0; i < data.Length; i++) bw.Write(data[i]);
            }
        }
        private static void ReadFromBinaryStream(out int[] data, System.IO.BinaryReader br)
        {
            int numOfEntries = br.ReadInt32();
            if (numOfEntries <= -1) data = null;
            else
            {
                data = new int[numOfEntries];
                for (int i = 0; i < data.Length; i++) data[i] = br.ReadInt32();
            }
        }


        // Methods                                                                                                                  
        public void ResetCellNeighboursOverCell()
        {
            _cellNeighboursOverCell = null;
        }
        //
        public void ExtractVisualizationCellsFromElements3D(Dictionary<int, FeElement> elements, int[] elementIds)
        {
            if (_cellNeighboursOverCell == null) ExtractCellNeighboursOverCell(elements, elementIds);
            //
            List<int[]> visualizationCells = new List<int[]>();
            List<int> visualizationCellsIds = new List<int>();
            // Extract free faces
            foreach (var entry in _cellNeighboursOverCell)
            {
                if (entry.Value.Id2 == -1)
                {
                    visualizationCells.Add(entry.Value.Cell1);
                    visualizationCellsIds.Add(entry.Value.Id1);
                }
            }
            // Save
            _cells = visualizationCells.ToArray();
            _cellIds = visualizationCellsIds.ToArray();
        }
        public void ExtractVisualizationCellsFromElements2D(Dictionary<int, FeElement> elements, int[] elementIds)
        {
            int count;
            int[][] visualizationCells = new int[elementIds.Length][];
            int[] visualizationCellsIds = new int[elementIds.Length];
            //
            count = 0;
            foreach (var id in elementIds)
            {
                visualizationCellsIds[count] = id;
                visualizationCells[count++] = elements[id].GetVtkNodeIds();
            }
            // Save
            _cells = visualizationCells.ToArray();
            _cellIds = visualizationCellsIds.ToArray();
        }
        private void ExtractCellNeighboursOverCell(Dictionary<int, FeElement> elements, int[] elementIds)
        {
            int[] sorted;
            CompareIntArray comparer = new CompareIntArray();
            CellNeighbour cellNeighbour;
            _cellNeighboursOverCell = new Dictionary<int[], CellNeighbour>(elementIds.Length, comparer);
            // Parallelizing this loop does not bring any speedup
            foreach (var id in elementIds)
            {
                foreach (int[] cell in ((FeElement3D)elements[id]).GetAllVtkCells())
                {
                    sorted = cell.ToArray();
                    Array.Sort(sorted);
                    //
                    if (_cellNeighboursOverCell.TryGetValue(sorted, out cellNeighbour)) cellNeighbour.Id2 = id;
                    else _cellNeighboursOverCell.Add(sorted, new CellNeighbour(id, -1, cell));
                }
            }
        }
        //
        public void RenumberNodes(Dictionary<int, int> newIds)
        {
            // Cells
            int id;
            if (_cells != null)
            {
                for (int i = 0; i < _cells.Length; i++)
                {
                    for (int j = 0; j < _cells[i].Length; j++)
                    {
                        if (newIds.TryGetValue(_cells[i][j], out id)) _cells[i][j] = id;
                    }
                }
            }
            // Edge cells
            if (_edgeCells != null)
            {
                for (int i = 0; i < _edgeCells.Length; i++)
                {
                    for (int j = 0; j < _edgeCells[i].Length; j++)
                    {
                        if (newIds.TryGetValue(_edgeCells[i][j], out id)) _edgeCells[i][j] = id;
                    }
                }
            }
            // Vertex ids
            if (_vertexNodeIds != null)
            {
                for (int i = 0; i < _vertexNodeIds.Length; i++)
                {
                    if (newIds.TryGetValue(_vertexNodeIds[i], out id)) _vertexNodeIds[i] = id;
                }
            }
            // Cell neighbours over cell
            bool error = false;
            if (_cellNeighboursOverCell != null)
            {
                int[] renumberedKey;
                int[] renumberedCell;
                CellNeighbour cellNeighbour;
                CompareIntArray comparer = new CompareIntArray();
                Dictionary<int[], CellNeighbour> renumberedNeighbours =
                    new Dictionary<int[], CellNeighbour>(_cellNeighboursOverCell.Count, comparer);
                //
                foreach (var entry in _cellNeighboursOverCell)
                {
                    renumberedKey = entry.Key.ToArray();           // not all nodes are renumbered
                    renumberedCell = entry.Value.Cell1.ToArray();   // not all nodes are renumbered
                    //
                    for (int i = 0; i < renumberedCell.Length; i++)
                    {
                        if (newIds.TryGetValue(entry.Key[i], out id)) renumberedKey[i] = id;
                        if (newIds.TryGetValue(entry.Value.Cell1[i], out id)) renumberedCell[i] = id;
                    }
                    //
                    cellNeighbour = entry.Value;
                    cellNeighbour.Cell1 = renumberedCell;
                    if (!renumberedNeighbours.ContainsKey(renumberedKey))
                        renumberedNeighbours.Add(renumberedKey, cellNeighbour);
                    else
                        error = true;
                }
                _cellNeighboursOverCell = renumberedNeighbours;
            }
            //
            if (System.Diagnostics.Debugger.IsAttached && error)
                MessageBoxes.ShowError("VisualizationData:RenumberNodes: This should not happen!");
        }
        public void RenumberElements(Dictionary<int, int> newIds)
        {
            int id;
            if (_cellIds != null)
            {
                for (int i = 0; i < _cellIds.Length; i++)
                {
                    if (newIds.TryGetValue(_cellIds[i], out id)) _cellIds[i] = id;
                }
            }
            // Cell neighbours over cell
            if (_cellNeighboursOverCell != null)
            {
                foreach (var entry in _cellNeighboursOverCell)
                {

                    if (entry.Value.Id1 != -1 && newIds.TryGetValue(entry.Value.Id1, out id)) entry.Value.Id1 = id;
                    if (entry.Value.Id2 != -1 && newIds.TryGetValue(entry.Value.Id2, out id)) entry.Value.Id2 = id;
                }
            }
        }
        public void RenumberSurfaces(int[] orderedSurfaceIds)
        {
            // Surface cells
            int[][] newCellIdsByFace = new int[_cellIdsByFace.Length][];
            for (int i = 0; i < orderedSurfaceIds.Length; i++)
            {
                newCellIdsByFace[i] = _cellIdsByFace[orderedSurfaceIds[i]];
            }
            _cellIdsByFace = newCellIdsByFace;
            // Areas
            double[] newFaceAreas = new double[_faceAreas.Length];
            for (int i = 0; i < orderedSurfaceIds.Length; i++)
            {
                newFaceAreas[i] = _faceAreas[orderedSurfaceIds[i]];
            }
            _faceAreas = newFaceAreas;
            // Types
            if (_faceTypes != null)
            {
                GeomFaceType[] newFaceTypes = new GeomFaceType[_faceTypes.Length];
                for (int i = 0; i < orderedSurfaceIds.Length; i++)
                {
                    newFaceTypes[i] = _faceTypes[orderedSurfaceIds[i]];
                }
                _faceTypes = newFaceTypes;
            }
            // Edges
            int[][] newFaceEdgeIds = new int[_faceEdgeIds.Length][];
            for (int i = 0; i < orderedSurfaceIds.Length; i++)
            {
                newFaceEdgeIds[i] = _faceEdgeIds[orderedSurfaceIds[i]];
            }
            _faceEdgeIds = newFaceEdgeIds;
        }
        public void RenumberEdges(int[] orderedEdgeIds)
        {
            // Inverse map
            int[] map = new int[orderedEdgeIds.Length];
            for (int i = 0; i < orderedEdgeIds.Length; i++)
            {
                map[orderedEdgeIds[i]] = i;
            }
            // Surface edges
            if (_faceEdgeIds != null)
            {
                int[][] newFaceEdgeIds = new int[_faceEdgeIds.Length][];
                for (int i = 0; i < _faceEdgeIds.Length; i++)
                {
                    newFaceEdgeIds[i] = new int[_faceEdgeIds[i].Length];
                    for (int j = 0; j < _faceEdgeIds[i].Length; j++)
                    {
                        newFaceEdgeIds[i][j] = map[_faceEdgeIds[i][j]];
                    }
                }
                _faceEdgeIds = newFaceEdgeIds;
            }
            // Edge cells
            int[][] newEdgeCellIdsByEdge = new int[_edgeCellIdsByEdge.Length][];
            for (int i = 0; i < orderedEdgeIds.Length; i++)
            {
                newEdgeCellIdsByEdge[i] = _edgeCellIdsByEdge[orderedEdgeIds[i]];
            }
            _edgeCellIdsByEdge = newEdgeCellIdsByEdge;
            // Lengths
            double[] newEdgeLengths = new double[_edgeLengths.Length];
            for (int i = 0; i < orderedEdgeIds.Length; i++)
            {
                newEdgeLengths[i] = _edgeLengths[orderedEdgeIds[i]];
            }
            _edgeLengths = newEdgeLengths;
            // Types
            if (_edgeTypes != null)
            {
                GeomCurveType[] newEdgeTypes = new GeomCurveType[_edgeTypes.Length];
                for (int i = 0; i < orderedEdgeIds.Length; i++)
                {
                    newEdgeTypes[i] = _edgeTypes[orderedEdgeIds[i]];
                }
                _edgeTypes = newEdgeTypes;
            }
        }
        public HashSet<int> GetNodeIdsForEdgeId(int edgeId)
        {
            HashSet<int> edgeNodeIds = new HashSet<int>();
            //
            if (_edgeCellIdsByEdge != null)
            {
                for (int i = 0; i < _edgeCellIdsByEdge[edgeId].Length; i++)
                {
                    edgeNodeIds.UnionWith(_edgeCells[_edgeCellIdsByEdge[edgeId][i]]);
                }
            }
            return edgeNodeIds;
        }
        public Dictionary<int, HashSet<int>> GetEdgeIdNodeIds()
        {
            HashSet<int> edgeNodeIds;
            Dictionary<int, HashSet<int>> edgeIdNodeIds = new Dictionary<int, HashSet<int>>();
            //
            if (_edgeCellIdsByEdge != null)
            {
                for (int i = 0; i < _edgeCellIdsByEdge.Length; i++)
                {
                    edgeNodeIds = new HashSet<int>();
                    for (int j = 0; j < _edgeCellIdsByEdge[i].Length; j++)
                    {
                        edgeNodeIds.UnionWith(_edgeCells[_edgeCellIdsByEdge[i][j]]);
                    }
                    edgeIdNodeIds.Add(i, edgeNodeIds);
                }
            }
            return edgeIdNodeIds;
        }
        public HashSet<int> GetNodeIdsForAllEdges()
        {
            HashSet<int> edgeNodeIds = new HashSet<int>();
            //
            if (_edgeCellIdsByEdge != null)
            {
                for (int i = 0; i < _edgeCellIdsByEdge.Length; i++)
                {
                    for (int j = 0; j < _edgeCellIdsByEdge[i].Length; j++)
                    {
                        edgeNodeIds.UnionWith(_edgeCells[_edgeCellIdsByEdge[i][j]]);
                    }
                }
            }
            return edgeNodeIds;
        }
        public int[] GetOrderedNodeIdsForEdgeId(int edgeId)
        {
            int[] edgeNodeIds;
            List<int> allEdgeNodeIds = new List<int>();
            for (int i = 0; i < _edgeCellIdsByEdge[edgeId].Length; i++)
            {
                edgeNodeIds = _edgeCells[_edgeCellIdsByEdge[edgeId][i]];
                //
                if (i == 0) allEdgeNodeIds.Add(edgeNodeIds[0]);     // add the first node only once
                if (edgeNodeIds.Length == 3) allEdgeNodeIds.Add(edgeNodeIds[2]);
                allEdgeNodeIds.Add(edgeNodeIds[1]);
            }
            //
            return allEdgeNodeIds.ToArray();
        }
        //
        public HashSet<int> GetNodeIds()
        {
            HashSet<int> nodeIds = new HashSet<int>();
            for (int i = 0; i < _cells.Length; i++) nodeIds.UnionWith(_cells[i]);
            return nodeIds;
        }
        public Dictionary<int, List<int[]>> GetVertexIdEdgeCells()
        {
            HashSet<int> vertexNodeIds = new HashSet<int>(_vertexNodeIds);
            List<int[]> edgeCells;
            Dictionary<int, List<int[]>> vertexIdEdgeCells = new Dictionary<int, List<int[]>>();
            //
            List<int> ids = new List<int>();
            int[] edgeCellNodeIds;
            foreach (var edgeCell in _edgeCells)
            {
                ids.Clear();
                if (vertexNodeIds.Contains(edgeCell[0])) ids.Add(0);
                if (vertexNodeIds.Contains(edgeCell[1])) ids.Add(1);
                //
                foreach (var id in ids)
                {
                    edgeCellNodeIds = new int[] { edgeCell[id], edgeCell[(id + 1) % 2] };
                    if (vertexIdEdgeCells.TryGetValue(edgeCell[id], out edgeCells)) edgeCells.Add(edgeCellNodeIds);
                    else vertexIdEdgeCells.Add(edgeCell[id], new List<int[]> { edgeCellNodeIds });
                }
            }
            //
            return vertexIdEdgeCells;
        }
        public Dictionary<int, HashSet<int>> GetVertexIdEdgeIds()
        {
            HashSet<int> edgeIds;
            Dictionary<int, HashSet<int>> edgeIdNodeIds = GetEdgeIdNodeIds();
            Dictionary<int, HashSet<int>> vertexIdEdgeIds = new Dictionary<int, HashSet<int>>();
            //
            foreach (int vertexId in _vertexNodeIds)
            {
                foreach (var entry in edgeIdNodeIds)
                {
                    if (entry.Value.Contains(vertexId))
                    {
                        if (vertexIdEdgeIds.TryGetValue(vertexId, out edgeIds)) edgeIds.Add(entry.Key);
                        else vertexIdEdgeIds.Add(vertexId, new HashSet<int> { entry.Key });
                    }
                }
            }
            //
            return vertexIdEdgeIds;
        }
        public HashSet<int> GetNodeIdsForSurfaceId(int surfaceId)
        {
            HashSet<int> surfaceNodeIds = new HashSet<int>();
            for (int i = 0; i < _cellIdsByFace[surfaceId].Length; i++)
            {
                surfaceNodeIds.UnionWith(_cells[_cellIdsByFace[surfaceId][i]]);
            }
            return surfaceNodeIds;
        }
        public Dictionary<int, HashSet<int>> GetSurfaceIdNodeIds()
        {
            HashSet<int> surfaceNodeIds;
            Dictionary<int, HashSet<int>> surfaceIdNodeIds = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < _cellIdsByFace.Length; i++)
            {
                surfaceNodeIds = new HashSet<int>();
                for (int j = 0; j < _cellIdsByFace[i].Length; j++)
                {
                    surfaceNodeIds.UnionWith(_cells[_cellIdsByFace[i][j]]);
                }
                surfaceIdNodeIds.Add(i, surfaceNodeIds);
            }
            return surfaceIdNodeIds;
        }
        public Dictionary<int, HashSet<int>> GetSurfaceIdElementIds()
        {
            HashSet<int> surfaceElementIds;
            Dictionary<int, HashSet<int>> surfaceIdElementIds = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < _cellIdsByFace.Length; i++)
            {
                surfaceElementIds = new HashSet<int>();
                for (int j = 0; j < _cellIdsByFace[i].Length; j++)
                {
                    surfaceElementIds.Add(_cellIds[_cellIdsByFace[i][j]]);
                }
                surfaceIdElementIds.Add(i, surfaceElementIds);
            }
            return surfaceIdElementIds;
        }
        public Dictionary<int, HashSet<int>> GetSurfaceIdSurfaceNeighbourIds()
        {
            Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds = new Dictionary<int, HashSet<int>>();
            HashSet<int> surfaceNeighbourIds;
            for (int i = 0; i < _faceEdgeIds.Length; i++)
            {
                for (int j = 0; j < _faceEdgeIds.Length; j++)
                {
                    if (i == j) continue;
                    //
                    if (_faceEdgeIds[i].Intersect(_faceEdgeIds[j]).Count() > 0)
                    {
                        if (surfaceIdSurfaceNeighbourIds.TryGetValue(i, out surfaceNeighbourIds)) surfaceNeighbourIds.Add(j);
                        else surfaceIdSurfaceNeighbourIds.Add(i, new HashSet<int>() { j });
                    }
                }
            }
            return surfaceIdSurfaceNeighbourIds;
        }
        public Dictionary<int, HashSet<int>> GetElementIdSurfaceIds()
        {
            int elementId;
            HashSet<int> elementSurfaceIds;
            Dictionary<int, HashSet<int>> elementIdSurfaceIds = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < _cellIdsByFace.Length; i++)
            {
                for (int j = 0; j < _cellIdsByFace[i].Length; j++)
                {
                    elementId = _cellIds[_cellIdsByFace[i][j]];
                    //
                    if (elementIdSurfaceIds.TryGetValue(elementId, out elementSurfaceIds)) elementSurfaceIds.Add(i);
                    else elementIdSurfaceIds.Add(elementId, new HashSet<int>() { i });
                }
            }
            return elementIdSurfaceIds;
        }
        public HashSet<int> GetVertexIdsForNodeIds(int[] nodeIds)
        {
            HashSet<int> nodeIdsHash = new HashSet<int>(nodeIds);
            HashSet<int> vertexNodeIds = new HashSet<int>();
            for (int i = 0; i < _vertexNodeIds.Length; i++)
            {
                if (nodeIdsHash.Contains(_vertexNodeIds[i])) vertexNodeIds.Add(i + 1);
            }
            return vertexNodeIds;
        }
        public int[] GetVertexNodeIdsForEdgeId(int edgeId)
        {
            HashSet<int> edgeNodeIds = new HashSet<int>(GetNodeIdsForEdgeId(edgeId));
            edgeNodeIds.IntersectWith(_vertexNodeIds);
            // Return start/end ids
            if (edgeNodeIds.Count == 1) return new int[] { edgeNodeIds.First(), edgeNodeIds.First() };
            else return edgeNodeIds.ToArray();
        }
        public HashSet<int> GetVertexNodeIdsForEdgeIds(int[] edgeIds)
        {
            HashSet<int> edgeNodeIds = new HashSet<int>();
            foreach (int edgeId in edgeIds) edgeNodeIds.UnionWith(GetNodeIdsForEdgeId(edgeId));
            return edgeNodeIds.Intersect(_vertexNodeIds).ToHashSet();
        }
        public int[] GetVertexNodeIdsForSurfaceId(int surfaceId)
        {
            HashSet<int> surfaceNodeIds = new HashSet<int>(GetNodeIdsForSurfaceId(surfaceId));
            return surfaceNodeIds.Intersect(_vertexNodeIds).ToArray();
        }
        public HashSet<int> GetVertexNodeIdsForSurfaceIds(int[] surfaceIds)
        {
            HashSet<int> surfaceNodeIds = new HashSet<int>();
            foreach (int surfaceId in surfaceIds) surfaceNodeIds.UnionWith(GetNodeIdsForSurfaceId(surfaceId));
            return surfaceNodeIds.Intersect(_vertexNodeIds).ToHashSet();
        }
        public HashSet<int> GetEdgeIdsForSurfaceIds(int[] surfaceIds)
        {
            HashSet<int> surfaceEdgeIds = new HashSet<int>();
            foreach (int surfaceId in surfaceIds) surfaceEdgeIds.UnionWith(_faceEdgeIds[surfaceId]);
            return surfaceEdgeIds;
        }
        public bool AreSurfacesConnected(int[] surfaceIds, Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds = null)
        {
            if (surfaceIds.Length == 1) return true;
            //
            HashSet<int> selectedSurfaceNeighboursIds = new HashSet<int>();
            if (surfaceIdSurfaceNeighbourIds == null) surfaceIdSurfaceNeighbourIds = GetSurfaceIdSurfaceNeighbourIds();
            //
            HashSet<int> surfaceNeighbours;
            foreach (var surfaceId in surfaceIds)
            {
                if (surfaceIdSurfaceNeighbourIds.TryGetValue(surfaceId, out surfaceNeighbours))
                    selectedSurfaceNeighboursIds.UnionWith(surfaceNeighbours);
                else return false;
            }
            // If surfaces are connected all of them are also their neighbours
            return selectedSurfaceNeighboursIds.Intersect(surfaceIds).Count() == surfaceIds.Count();
        }
        public HashSet<int> GetEdgeNodeIds()
        {
            HashSet<int> nodeIds = new HashSet<int>();
            foreach (var edgeCell in _edgeCells) nodeIds.UnionWith(edgeCell);
            return nodeIds;
        }
        // Free edges and nodes
        public HashSet<int> GetFreeEdgeIds()
        {
            int[] count;
            Dictionary<int, int[]> edgeIdCount = new Dictionary<int, int[]>();
            //
            foreach (var faceEdgeIds in _faceEdgeIds)
            {
                foreach (var edgeId in faceEdgeIds)
                {
                    if (edgeIdCount.TryGetValue(edgeId, out count)) count[0]++;
                    else edgeIdCount[edgeId] = new int[] { 1 };
                }
            }
            //
            HashSet<int> freeEdgeIds = new HashSet<int>();
            foreach (var entry in edgeIdCount)
            {
                if (entry.Value[0] == 1) freeEdgeIds.Add(entry.Key);
            }
            //
            return freeEdgeIds;
        }
        public HashSet<int> GetFreeEdgeNodeIds()
        {
            HashSet<int> freeEdgeIds = GetFreeEdgeIds();
            HashSet<int> freeNodeIds = new HashSet<int>();
            foreach (var edgeId in freeEdgeIds)
            {
                foreach (var edgeCellId in _edgeCellIdsByEdge[edgeId])
                {
                    freeNodeIds.UnionWith(_edgeCells[edgeCellId]);
                }
            }
            return freeNodeIds;
        }
        public int GetEdgeCellBaseCellIdSlow(int edgeCellId)
        {
            bool found;
            int[] nodeIds = _edgeCells[edgeCellId];
            //
            for (int i = 0; i < _cellNeighboursOverCellEdge.Length; i++)
            {
                for (int j = 0; j < _cellNeighboursOverCellEdge[i].Length; j++)
                {
                    if (_cellNeighboursOverCellEdge[i][j] == -1)
                    {
                        found = true;
                        for (int k = 0; k < nodeIds.Length; k++)
                        {
                            if (!_cells[i].Contains(nodeIds[k]))
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found) return i;
                        else break;
                    }
                }
            }
            return -1;
        }
        public int[] GetAllEdgeCellBaseCellIds()
        {
            bool found;
            int cellId;
            int[] nodeIds;
            int[] baseCellIds = new int[_edgeCells.Length];
            // Set initial value to -1
            for (int i = 0; i < baseCellIds.Length; i++) baseCellIds[i] = -1;
            //
            for (int k = 0; k < _edgeCells.Length; k++)
            {
                nodeIds = _edgeCells[k];
                // For cells
                for (int i = 0; i < _cellNeighboursOverCellEdge.Length; i++)
                {
                    // For cell neighbours
                    for (int j = 0; j < _cellNeighboursOverCellEdge[i].Length; j++)
                    {
                        if (_cellNeighboursOverCellEdge[i][j] == -1)
                        {
                            found = true;
                            for (int l = 0; l < nodeIds.Length; l++)
                            {
                                if (!_cells[i].Contains(nodeIds[l]))
                                {
                                    found = false;
                                    break;
                                }
                            }
                            if (found) baseCellIds[k] = i;
                            //
                            break;
                        }
                    }
                    // Check if edge cell is shared with another cell or is it really an free edge cell
                    if (baseCellIds[k] > -1)
                    {
                        for (int j = 0; j < _cellNeighboursOverCellEdge[i].Length; j++)
                        {
                            if (_cellNeighboursOverCellEdge[i][j] > -1)
                            {
                                found = true;
                                cellId = _cellNeighboursOverCellEdge[i][j];
                                for (int l = 0; l < nodeIds.Length; l++)
                                {
                                    if (!_cells[cellId].Contains(nodeIds[l]))
                                    {
                                        found = false;
                                        break;
                                    }
                                }
                                if (found)
                                {
                                    baseCellIds[k] = -1;    // it is not a free edge
                                    break;
                                }
                            }
                        }
                    }
                    //
                    if (baseCellIds[k] > -1) break;
                }
            }
            return baseCellIds;
        }
        //
        public Dictionary<int[], CellEdgeData> GetCellEdgeData(Func<int[], ElementFaceType, int[][]> GetVisualizationEdgeCells)
        {
            int[][] cells = _cells;
            CompareIntArray comparer = new CompareIntArray();
            Dictionary<int[], CellEdgeData> allEdges = new Dictionary<int[], CellEdgeData>(comparer);
            //
            int[] key;
            CellEdgeData data;
            int[][] cellEdges;
            //
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            // Get all edges
            for (int i = 0; i < cells.Length; i++)
            {
                cellEdges = GetVisualizationEdgeCells(cells[i], ElementFaceType.Face);
                //
                foreach (var cellEdge in cellEdges)
                {
                    key = cellEdge.ToArray();
                    Array.Sort(key);
                    //
                    if (key[0] == key[1] || (key.Length == 3 && key[1] == key[2]))
                    {
                        //manifoldGeometry
                        continue;
                    }
                    //
                    if (allEdges.TryGetValue(key, out data)) data.CellIds.Add(i);
                    else allEdges.Add(key, new CellEdgeData() { NodeIds = cellEdge, CellIds = new List<int>() { i } });
                }
            }
            watch.Stop();
            //
            return allEdges;
        }
        public Dictionary<int[], CellEdgeData> GetCellEdgeData1(Func<int[], ElementFaceType, int[][]> GetVisualizationEdgeCells)
        {
            int[][] cells = _cells;
            CompareIntArray comparer = new CompareIntArray();
            ConcurrentDictionary<int[], CellEdgeData> allEdges = new ConcurrentDictionary<int[], CellEdgeData>(comparer);
            //
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            // Get all edges
            Parallel.For(0, cells.Length, i =>
            {
                int[][] cellEdges = GetVisualizationEdgeCells(cells[i], ElementFaceType.Face);
                int[] key;
                CellEdgeData data;
                //
                foreach (var cellEdge in cellEdges)
                {
                    key = cellEdge.ToArray();
                    Array.Sort(key);
                    //
                    if (key[0] == key[1] || (key.Length == 3 && key[1] == key[2]))
                    {
                        //manifoldGeometry
                        continue;
                    }
                    //
                    if (allEdges.TryGetValue(key, out data)) data.CellIds.Add(i);
                    else allEdges.TryAdd(key, new CellEdgeData() { NodeIds = cellEdge, CellIds = new List<int>() { i } });
                }
            }
            );
            // Copy dictionary
            Dictionary<int[], CellEdgeData> edges = new Dictionary<int[], CellEdgeData>(comparer);
            foreach (var entry in allEdges) edges.Add(entry.Key, entry.Value);
            //
            watch.Stop();
            //
            return edges;
        }
        public Dictionary<int[], CellEdgeData> GetCellEdgeData3(Func<int[], ElementFaceType, int[][]> GetVisualizationEdgeCells)
        {
            CellEdgeData[][] cellEdgeData = new CellEdgeData[_cells.Length][];
            //
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            // Get all edges
            Parallel.For(0, _cells.Length, i =>
            {
                int[][] cellEdges = GetVisualizationEdgeCells(_cells[i], ElementFaceType.Face);
                int[] key;
                cellEdgeData[i] = new CellEdgeData[cellEdges.Length];
                //
                for (int j = 0; j < cellEdges.Length; j++)
                {
                    key = cellEdges[j].ToArray();
                    Array.Sort(key);
                    //
                    if (key[0] == key[1] || (key.Length == 3 && key[1] == key[2]))
                    {
                        // manifoldGeometry
                        continue;
                    }
                    //
                    cellEdgeData[i][j] = new CellEdgeData() // 8 % time
                    {
                        Key = key,
                        NodeIds = cellEdges[j],
                        CellIds = new List<int>() { i }     // 18 % time
                    };
                }
            }
            );
            //
            CellEdgeData data;
            CompareIntArray comparer = new CompareIntArray();
            Dictionary<int[], CellEdgeData> allEdges = new Dictionary<int[], CellEdgeData>(comparer);
            for (int i = 0; i < cellEdgeData.Length; i++)
            {
                for (int j = 0; j < cellEdgeData[i].Length; j++)
                {
                    if (cellEdgeData[i][j] != null) // check for manifold geometry
                    {
                        if (allEdges.TryGetValue(cellEdgeData[i][j].Key, out data)) data.CellIds.Add(i);
                        else allEdges.Add(cellEdgeData[i][j].Key, cellEdgeData[i][j]);
                    }
                }
            }
            watch.Stop();
            //
            return allEdges;
        }
        public int[] GetFreeEdgeCellIds(Func<int[], ElementFaceType, int[][]> GetVisualizationEdgeCells)
        {
            Dictionary<int[], CellEdgeData> allEdges = GetCellEdgeData3(GetVisualizationEdgeCells);
            //
            int[] key;
            int edgeCellId = 0;
            CompareIntArray comparer = new CompareIntArray();
            Dictionary<int[], int> edgeCellEdgeCellId = new Dictionary<int[], int>(comparer);
            foreach (var edgeCell in _edgeCells)
            {
                key = edgeCell.ToArray();
                Array.Sort(key);
                edgeCellEdgeCellId.Add(key, edgeCellId++);
            }
            //
            int freeEdgeCellId;
            List<int> freeEdgeCellIds = new List<int>();
            foreach (var entry in allEdges)
            {
                // Free edges
                if (entry.Value.CellIds.Count == 1)
                {
                    if (edgeCellEdgeCellId.TryGetValue(entry.Key, out freeEdgeCellId)) freeEdgeCellIds.Add(freeEdgeCellId);
                    else
                        freeEdgeCellId = freeEdgeCellId;
                }
            }
            //
            return freeEdgeCellIds.ToArray();
        }
        public int[] GetFreeVertexNodeIds(int[] freeEdgeCellIds)
        {
            HashSet<int> freeEdgeNodeIds = new HashSet<int>();
            foreach (var edgeCellId in freeEdgeCellIds) freeEdgeNodeIds.UnionWith(_edgeCells[edgeCellId]);
            List<int> freeVertexNodeIds = new List<int>();
            foreach (var nodeId in _vertexNodeIds)
            {
                if (freeEdgeNodeIds.Contains(nodeId)) freeVertexNodeIds.Add(nodeId);
            }
            return freeVertexNodeIds.ToArray();
        }
        //
        public VisualizationData DeepCopy()
        {
            return new VisualizationData(this);
        }
        // Topology
        public void CheckForErrorElementsInShellCADPart(out int[] errorEdgeCellIds, out int[] errorNodeIds)
        {
            // Shell parts
            int edgeId;
            int[] edgeCell;
            int[] edgeNodeIds;
            List<int> vertexEdgeIds;
            List<int> errorEdgeCellIdsList = new List<int>();
            List<int> errorNodeIdsList = new List<int>();
            HashSet<int> errorEdgeIds = new HashSet<int>();
            HashSet<int> vertexNodeIds = new HashSet<int>(_vertexNodeIds);
            // Build a map of all edges connected to a vertex
            Dictionary<int, List<int>>[] faceVertexEdgeIds = new Dictionary<int, List<int>>[_faceEdgeIds.Length];
            // For each surface
            for (int i = 0; i < _faceEdgeIds.Length; i++)
            {
                faceVertexEdgeIds[i] = new Dictionary<int, List<int>>();
                // For each surface edge
                for (int j = 0; j < _faceEdgeIds[i].Length; j++)
                {
                    edgeId = _faceEdgeIds[i][j];

                    //IsEdgeAClosedLoop(edgeId, out edgeNodeIds);
                    //if (edgeNodeIds != null && edgeId == 9)
                    //    errorNodeIdsList = new List<int>(edgeNodeIds);

                    // Skip edges that form a single edge loop
                    if (IsEdgeAClosedLoop(edgeId, out edgeNodeIds))
                        continue;
                    // For each edge cell
                    for (int k = 0; k < _edgeCellIdsByEdge[edgeId].Length; k++)
                    {
                        edgeCell = _edgeCells[_edgeCellIdsByEdge[edgeId][k]];
                        // For each node in edge cell
                        for (int l = 0; l < edgeCell.Length; l++)
                        {
                            if (vertexNodeIds.Contains(edgeCell[l]))    // is this node a vertex
                            {
                                if (faceVertexEdgeIds[i].TryGetValue(edgeCell[l], out vertexEdgeIds))
                                    vertexEdgeIds.Add(edgeId);
                                else faceVertexEdgeIds[i].Add(edgeCell[l], new List<int>() { edgeId });
                            }
                        }
                    }
                }
            }
            // From the map extract all end/start vertices and their open edge loops
            GeomFaceType faceType;
            // For each face
            Dictionary<int, List<int>> face_i_VertexEdgeIds;
            for (int i = 0; i < faceVertexEdgeIds.Length; i++)
            {
                face_i_VertexEdgeIds = faceVertexEdgeIds[i];
                //
                if (_faceTypes != null) faceType = _faceTypes[i];
                else faceType = GeomFaceType.Unknown;
                //
                RemoveEdgeLoops(face_i_VertexEdgeIds);
                //
                foreach (var entry in face_i_VertexEdgeIds)
                {
                    // Cylinder and toruses have a single edge along their axis which creates 3 edge vertices
                    if (entry.Value.Count % 2 == 1 && (faceType == GeomFaceType.Cylinder || faceType == GeomFaceType.Torus))
                        continue;
                    else
                    {
                        foreach (var remainingEdgeId in entry.Value)
                            errorEdgeIds.Add(remainingEdgeId);
                    }
                }
            }
            
            // Collect error edge cell ids
            foreach (var errorEdgeId in errorEdgeIds) errorEdgeCellIdsList.AddRange(_edgeCellIdsByEdge[errorEdgeId]);
            // Save
            if (errorEdgeCellIdsList.Count > 0) errorEdgeCellIds = errorEdgeCellIdsList.ToArray();
            else errorEdgeCellIds = null;
            //
            if (errorNodeIdsList.Count > 0) errorNodeIds = errorNodeIdsList.ToArray();
            else errorNodeIds = null;
        }
        public HashSet<int>[] GetFreeEdgeLoops(int[] surfaceIds)
        {
            int edgeId;
            int surfaceId;
            Dictionary<int, int> allEdgeIdCount = new Dictionary<int, int>();
            for (int i = 0; i < surfaceIds.Length; i++)
            {
                surfaceId = surfaceIds[i];
                for (int j = 0; j < _faceEdgeIds[surfaceId].Length; j++)
                {
                    edgeId = _faceEdgeIds[surfaceId][j];
                    //
                    if (allEdgeIdCount.ContainsKey(edgeId)) allEdgeIdCount[edgeId]++;
                    else allEdgeIdCount[edgeId] = 1;
                }
            }
            HashSet<int> outerEdgeIds = new HashSet<int>();
            foreach (var entry in allEdgeIdCount)
            {
                if (entry.Value == 1) outerEdgeIds.Add(entry.Key);
            }
            //
            Dictionary<int, HashSet<int>> vertexIdEdgeIds = GetVertexIdEdgeIds();
            // Split edges into connected groups
            Node<int> node;
            Graph<int> connections = new Graph<int>();
            Dictionary<int, Node<int>> outerEdgeIdNode = new Dictionary<int, Node<int>>();
            //
            foreach (var outerEdgeId in outerEdgeIds)
            {
                node = new Node<int>(outerEdgeId);
                connections.AddNode(node);
                outerEdgeIdNode.Add(outerEdgeId, node);
            }
            int[] edgeIds;
            foreach (var entry in vertexIdEdgeIds)
            {
                edgeIds = entry.Value.ToArray();
                for (int i = 0; i < edgeIds.Length - 1; i++)
                {
                    for (int j = i + 1; j < edgeIds.Length; j++)
                    {
                        if (outerEdgeIds.Contains(edgeIds[i]) && outerEdgeIds.Contains(edgeIds[j]))
                            connections.AddUndirectedEdge(outerEdgeIdNode[edgeIds[i]], outerEdgeIdNode[edgeIds[j]]);
                    }
                }
            }
            List<Graph<int>> loops = connections.GetConnectedSubgraphs();
            //
            int count = 0;
            HashSet<int>[] loopEdgeIds = new HashSet<int>[loops.Count];
            foreach (var loop in loops)
            {
                loopEdgeIds[count++] = new HashSet<int>(loop.GetValues());
            }
            //
            return loopEdgeIds;
        }
        public bool IsSurfaceACylinderLike(int surfaceId, out int directionEdgeId)
        {
            directionEdgeId = -1;
            if (_faceEdgeIds[surfaceId].Length == 3)
            {
                HashSet<int> closedLoopEdgeIds = new HashSet<int>();
                HashSet<int> directionEdgeIds = new HashSet<int>();
                for (int i = 0; i < _faceEdgeIds[surfaceId].Length; i++)
                {
                    if (IsEdgeAClosedLoop(_faceEdgeIds[surfaceId][i], out _))
                        closedLoopEdgeIds.Add(_faceEdgeIds[surfaceId][i]);
                    else
                        directionEdgeIds.Add(_faceEdgeIds[surfaceId][i]);
                }
                if (closedLoopEdgeIds.Count == 2 && directionEdgeIds.Count == 1)
                {
                    directionEdgeId = directionEdgeIds.First();
                    return true;
                }
            }
            return false;
        }
        public bool IsEdgeAClosedLoop(int edgeId, out int[] edgeNodeIds)
        {
            edgeNodeIds = null;
            int[] edgeCellIds = _edgeCellIdsByEdge[edgeId];
            if (edgeCellIds.Length > 0)
            {
                int count = 0;
                int[] edgeCell;
                HashSet<int> allNodeIds = new HashSet<int>();
                //
                for (int i = 0; i < edgeCellIds.Length; i++)
                {
                    edgeCell = _edgeCells[edgeCellIds[i]];
                    // Add only first and second node id
                    count += 2;
                    allNodeIds.Add(edgeCell[0]);
                    allNodeIds.Add(edgeCell[1]);
                }
                //
                edgeNodeIds = allNodeIds.ToArray();
                //
                if (allNodeIds.Count == 2) return false;
                else return allNodeIds.Count() * 2 == count;
            }
            else return false;
        }
        private void RemoveEdgeLoops(Dictionary<int, List<int>> vertexIdEdgeIds)
        {
            List<int> edgeLoop;
            List<int> verticesToRemove = new List<int>();
            //
            while (true)
            {
                edgeLoop = GetEdgeLoop(vertexIdEdgeIds);
                if (edgeLoop.Count > 0)
                {
                    verticesToRemove.Clear();
                    //
                    foreach (var entry in vertexIdEdgeIds)
                    {
                        foreach (var edgeId in edgeLoop)
                        {
                            entry.Value.Remove(edgeId);
                            if (entry.Value.Count == 0) verticesToRemove.Add(entry.Key);
                        }
                    }
                    //
                    foreach (var vertexId in verticesToRemove) vertexIdEdgeIds.Remove(vertexId);
                }
                else break;
            }
            return;
        }
        private List<int> GetEdgeLoop(Dictionary<int, List<int>> vertexIdEdgeIds)
        {
            List<int> loopEdgeIds = new List<int>();
            List<int> loopVertexIds = new List<int>();
            //
            int[] vertexIds;
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            foreach (var entry in vertexIdEdgeIds)
            {
                foreach (var edgeId in entry.Value)
                {
                    if (edgeIdVertexIds.TryGetValue(edgeId, out vertexIds)) vertexIds[1] = entry.Key;
                    else edgeIdVertexIds.Add(edgeId, new int[] { entry.Key, 0 });
                }
            }
            // First vertex must not be an end point
            foreach (var entry in vertexIdEdgeIds)
            {
                if (entry.Value.Count % 2 == 0)
                {
                    loopEdgeIds.Clear();
                    loopVertexIds.Clear();
                    AddNextEdgeToLoop(entry.Key, vertexIdEdgeIds, edgeIdVertexIds, loopVertexIds, loopEdgeIds);
                    if (loopEdgeIds.Count > 0) return loopEdgeIds;
                }
            }
            //
            return loopEdgeIds;
        }
        private bool AddNextEdgeToLoop(int vertexId, Dictionary<int, List<int>> vertexIdEdgeIds,
                                       Dictionary<int, int[]> edgeIdVertexIds, List<int> loopVertexIds, List<int> loopEdgeIds)
        {
            // Check if there is a loop of vertices before there is a loop of edges
            loopVertexIds.Add(vertexId);
            //
            bool closed = false;
            int newVertexId;
            int[] vertexIds;
            List<int> edgeIds = vertexIdEdgeIds[vertexId];
            //
            foreach (var edgeId in edgeIds)
            {
                if (loopEdgeIds.Count > 1 && loopEdgeIds[0] == edgeId &&
                    loopVertexIds.Count > 1 && loopVertexIds[0] == vertexId) return true;
                else
                {
                    if (loopEdgeIds.Count == 0 || !loopEdgeIds.Contains(edgeId))
                    {
                        // Add
                        loopEdgeIds.Add(edgeId);
                        //
                        vertexIds = edgeIdVertexIds[edgeId];
                        if (vertexId == vertexIds[0]) newVertexId = vertexIds[1];
                        else newVertexId = vertexIds[0];
                        //

                        closed = AddNextEdgeToLoop(newVertexId, vertexIdEdgeIds, edgeIdVertexIds, loopVertexIds, loopEdgeIds);
                        // Finish
                        if (closed) return true;
                        // Remove
                        else loopEdgeIds.Remove(edgeId);
                    }
                }
            }
            //
            loopVertexIds.Remove(vertexId);
            return false;
        }
        public bool IsNonManifold()
        {
            int[] edgeCount = new int[EdgeCount];
            for (int i = 0; i < _faceEdgeIds.Length; i++)
            {
                for (int j = 0; j < _faceEdgeIds[i].Length; j++)
                {
                    edgeCount[_faceEdgeIds[i][j]]++;
                    if (edgeCount[_faceEdgeIds[i][j]] > 2) return true;
                }
            }
            return false;
        }

        // Flip normals
        public void FlipTriangleNormals()
        {
            int tmp;
            foreach (var cell in _cells)
            {
                if (cell.Length == 3)
                {
                    tmp = cell[1];
                    cell[1] = cell[2];
                    cell[2] = tmp;
                }
            }
        }
        // Compute nodal average
        public AvgData GetAvgData()
        {
            int nodeId;
            int elementId;
            int[] cell;
            AvgData avgData = new AvgData();
            AvgNodalData avgNode;
            AvgNodalElementData avgElement;
            //
            for (int i = 0; i < _cells.Length; i++)
            {
                cell = _cells[i];
                elementId = _cellIds[i];
                //
                for (int j = 0; j < cell.Length; j++)
                {
                    nodeId = cell[j];
                    //
                    if (avgData.Nodes.TryGetValue(nodeId, out avgNode))
                    {
                        if (avgNode.Elements.TryGetValue(elementId, out avgElement))
                        {
                        }
                        else
                        {
                            avgElement = new AvgNodalElementData();
                            //
                            avgNode.Elements.Add(elementId, avgElement);
                        }
                    }
                    else
                    {
                        avgElement = new AvgNodalElementData();
                        //
                        avgNode = new AvgNodalData();
                        avgNode.Elements.Add(elementId, avgElement);
                        //
                        avgData.Nodes.Add(nodeId, avgNode);
                    }
                }
            }
            //
            return avgData;
        }
        //
        public double GetEqualEdgeLengthOrNegative(HashSet<int> edgeIds)
        {
            if (edgeIds.Count < 1) return -1;
            else
            {
                double l;
                double min = double.MaxValue;
                double max = -double.MaxValue;
                //
                foreach (var edgeId in edgeIds)
                {
                    l = _edgeLengths[edgeId];
                    if (l < min) min = l;
                    if (l > max) max = l;
                    //
                    if (min <= 0 || max <= 0) return -1;    // edge lengths must be positive
                    if (max - min > 1E-3 * max) return -1;
                }
                return (min + max) / 2;
            }
        }
        public double GetMaxEdgeCurvature(int edgeId, Dictionary<int, FeNode> nodes)
        {
            double curvature = 0;
            int[] nodeIds = GetOrderedNodeIdsForEdgeId(edgeId);
            //
            if (nodeIds.Length <= 2) return 0;
            else
            {
                Vec3D[] v = new Vec3D[nodeIds.Length];
                v[0] = new Vec3D(nodes[nodeIds[0]].Coor);
                v[1] = new Vec3D(nodes[nodeIds[1]].Coor);
                //
                double r;
                double tmpCurvature;
                for (int i = 0; i < nodeIds.Length - 2; i++)
                {
                    v[i + 2] = new Vec3D(nodes[nodeIds[i + 2]].Coor);
                    //
                    Vec3D.GetCircleR(v[i], v[i + 1], v[i + 2], out r);
                    if (r > 0) tmpCurvature = 1 / r;
                    else tmpCurvature = 0;
                    //
                    if (tmpCurvature > curvature) curvature = tmpCurvature;
                }
                return curvature;
            }
        }
        public bool IsEdgeRadiusConstant(HashSet<int> edgeIds, Dictionary<int, FeNode> nodes)
        {
            if (edgeIds.Count < 1) return false;
            else
            {
                double r;
                foreach (var edgeId in edgeIds)
                {
                    r = GetConstantEdgeRadiusOrNegative(edgeId, nodes);
                    if (r < 0) return false;
                }
                return true;
            }
        }
        public double GetConstantEdgeRadiusOrNegative(int edgeId, Dictionary<int, FeNode> nodes)
        {
            int[] nodeIds = GetOrderedNodeIdsForEdgeId(edgeId);
            //
            if (nodeIds.Length <= 2) return -1;
            else
            {
                Vec3D[] v = new Vec3D[nodeIds.Length];
                v[0] = new Vec3D(nodes[nodeIds[0]].Coor);
                v[1] = new Vec3D(nodes[nodeIds[1]].Coor);
                //
                double r;
                double min = double.MaxValue;
                double max = -double.MaxValue;
                //
                for (int i = 0; i < nodeIds.Length - 2; i++)
                {
                    v[i + 2] = new Vec3D(nodes[nodeIds[i + 2]].Coor);
                    //
                    Vec3D.GetCircleR(v[i], v[i + 1], v[i + 2], out r);
                    if (r < min) min = r;
                    if (r > max) max = r;
                    //
                    if (min <= 0 || max <= 0) return -1;    // edge radius must be positive
                    if (max - min > 1E-3 * max) return -1;
                }
                //
                return (min + max) / 2;
            }
        }
        public void GetArcEdgeDataForEdgeIds(HashSet<int> edgeIds, HashSet<int> startNodeIds, Dictionary<int, FeNode> nodes,
                                             out double r, out double arcAngleDeg,
                                             out double[] axisCenter, out double[] axisDirection)
        {
            r = -1;
            arcAngleDeg = -1;
            axisCenter = null;
            axisDirection = null;
            //
            HashSet<double> arcAnglesDeg = new HashSet<double>();
            HashSet<double> radii = new HashSet<double>();
            BoundingBox bbDirection = new BoundingBox();
            BoundingBox bbCenter = new BoundingBox();
            //
            if (edgeIds.Count < 1) return;
            else
            {
                int numLines = 0;
                foreach (var edgeId in edgeIds)
                {
                    GetArcEdgeDataForEdgeId(edgeId, startNodeIds, nodes, out r, out arcAngleDeg, out axisCenter, out axisDirection);
                    if (double.IsInfinity(r)) numLines++;
                    else
                    {
                        radii.Add(r);
                        arcAnglesDeg.Add(arcAngleDeg);
                        bbCenter.IncludeCoor(axisCenter);
                        bbDirection.IncludeCoor(axisDirection);
                    }
                }
                //
                bool equal = true;
                double min = double.MaxValue;
                double max = -double.MaxValue;
                //
                foreach (var angle in arcAnglesDeg)
                {
                    if (angle < min) min = angle;
                    if (angle > max) max = angle;
                    //
                    if (min <= 0 || max <= 0) equal = false;    // angles must be positive
                    if (max - min > 1E-3 * max) equal = false;
                    //
                    if (!equal) break;
                }
                //
                if (equal && bbDirection.GetDiagonal() < 0.0017) // approximately 0.1° difference
                {
                    double[] sortedR = radii.ToArray();
                    Array.Sort(sortedR);
                    r = (sortedR[0] + sortedR[sortedR.Length - 1]) / 2;
                    arcAngleDeg = (min + max) / 2;
                    axisCenter = bbCenter.GetCenter();
                    axisDirection = bbDirection.GetCenter();
                }
                else
                {
                    if (numLines == edgeIds.Count) r = double.PositiveInfinity;
                    else r = -1;
                    //
                    arcAngleDeg = -1;
                    axisCenter = null;
                    axisDirection = null;
                }
            }
        }
        public void GetArcEdgeDataForEdgeId(int edgeId, HashSet<int> startNodeIds, Dictionary<int, FeNode> nodes,
                                            out double r, out double arcAngleDeg,
                                            out double[] axisCenter, out double[] axisDirection)
        {
            r = -1;
            arcAngleDeg = -1;
            axisCenter = null;
            axisDirection = null;
            int[] nodeIds = GetOrderedNodeIdsForEdgeId(edgeId);
            //
            if (nodeIds.Length == 2) { r = double.PositiveInfinity; }
            else if (nodeIds.Length < 3) { }
            else
            {
                if (!startNodeIds.Contains(nodeIds[0])) Array.Reverse(nodeIds);
                //
                if (!startNodeIds.Contains(nodeIds[0])) return;
                else
                {
                    Vec3D v1 = new Vec3D(nodes[nodeIds[0]].Coor);
                    Vec3D v2 = new Vec3D(nodes[nodeIds[(nodeIds.Length + 1) / 2]].Coor);
                    Vec3D v3 = new Vec3D(nodes[nodeIds[nodeIds.Length - 1]].Coor);
                    Vec3D center, direction;
                    Vec3D.GetCircle(v1, v2, v3, out r, out arcAngleDeg, out center, out direction);
                    axisCenter = center.Coor;
                    axisDirection = direction.Coor;
                }
            }
        }
        // Sweep mesh

        // Section cut
        public void ApplySectionView(Dictionary<int, FeElement> elements, int[] elementIds, HashSet<int> frontNodes,
                                     HashSet<int> backNodes)
        {
            HashSet<int> visibleNodes;
            SectionCutCells(elements, frontNodes, backNodes, out visibleNodes);
            SectionCutEdgeCells(visibleNodes);
            //_cells = new int[0][];
            //_cellIds = new int[0];
            //_cellIdsByFace = new int[0][];
            CreateSectionPlaneCellsAndEdges(elements, elementIds, frontNodes, backNodes);
        }
        public void RemoveSectionView()
        {

        }
        private HashSet<int> SectionCutCells(Dictionary<int, FeElement> elements, HashSet<int> frontNodes, HashSet<int> backNodes,
                                             out HashSet<int> visibleNodes)
        {
            // Split visualization cells                                                    
            FeElement element;
            bool insert;
            int[] cellMap = new int[_cells.Length];
            List<int[]> splitCells = new List<int[]>();
            List<int> splitCellIds = new List<int>();
            visibleNodes = new HashSet<int>();
            for (int i = 0; i < _cells.Length; i++)
            {
                element = elements[CellIds[i]];
                insert = true;
                for (int j = 0; j < element.NodeIds.Length; j++)
                {
                    if (frontNodes.Contains(element.NodeIds[j]))
                    {
                        insert = false;
                        break;
                    }
                }
                if (insert)
                {
                    cellMap[i] = splitCells.Count;
                    splitCells.Add(_cells[i]);
                    splitCellIds.Add(_cellIds[i]);
                    visibleNodes.UnionWith(_cells[i]);
                }
                else
                {
                    cellMap[i] = -1;
                }
            }
            // Renumber cell ids by face
            int cellId;
            List<int> cellIds = new List<int>();
            for (int i = 0; i < _cellIdsByFace.Length; i++)
            {
                cellIds.Clear();
                for (int j = 0; j < _cellIdsByFace[i].Length; j++)
                {
                    cellId = cellMap[_cellIdsByFace[i][j]];
                    if (cellId != -1) cellIds.Add(cellId);
                }
                _cellIdsByFace[i] = cellIds.ToArray();
            }
            // Save
            _cells = splitCells.ToArray();
            _cellIds = splitCellIds.ToArray();

            return visibleNodes;
        }
        private void SectionCutEdgeCells(HashSet<int> visibleNodes)
        {
            // Split visualization edges                                                    
            bool insert;
            int[] cellMap = new int[_edgeCells.Length];
            List<int[]> splitEdgeCells = new List<int[]>();
            for (int i = 0; i < _edgeCells.Length; i++)
            {
                insert = true;
                for (int j = 0; j < _edgeCells[i].Length; j++)
                {
                    if (!visibleNodes.Contains(_edgeCells[i][j]))
                    {
                        insert = false;
                        break;
                    }
                }

                if (insert)
                {
                    cellMap[i] = splitEdgeCells.Count;
                    splitEdgeCells.Add(_edgeCells[i]);
                }
                else
                {
                    cellMap[i] = -1;
                }
            }
            // Renumber edge cell ids by edge
            int cellId;
            List<int> cellIds = new List<int>();
            for (int i = 0; i < _edgeCellIdsByEdge.Length; i++)
            {
                cellIds.Clear();
                for (int j = 0; j < _edgeCellIdsByEdge[i].Length; j++)
                {
                    cellId = cellMap[_edgeCellIdsByEdge[i][j]];
                    if (cellId != -1) cellIds.Add(cellId);
                }
                _edgeCellIdsByEdge[i] = cellIds.ToArray();
            }
            _edgeCells = splitEdgeCells.ToArray();
        }
        private void CreateSectionPlaneCellsAndEdges(Dictionary<int, FeElement> elements, int[] elementIds, HashSet<int> frontNodes,
                                                     HashSet<int> backNodes)
        {
            List<int[]> sectionPlaneCells;
            List<int> sectionPlaneCellIds;            

            GetSectionElementsAndIds(elements, elementIds, frontNodes, backNodes, out sectionPlaneCells, out sectionPlaneCellIds);
            AddSectionPlaneCellsAndIds(sectionPlaneCells, sectionPlaneCellIds);
            AddSectionPlaneEdgeCellsAndIds(sectionPlaneCells);
        }
        private void GetSectionElementsAndIds(Dictionary<int, FeElement> elements, int[] elementIds, 
                                              HashSet<int> frontNodes, HashSet<int> backNodes,
                                              out List<int[]> sectionPlaneCells, out List<int> sectionPlaneCellIds)
        {
            // Check if cell neighbours exists
            if (_cellNeighboursOverCell == null) ExtractCellNeighboursOverCell(elements, elementIds);

            // Get vtk cells on the plane                                                   
            int countFront;
            int countBack;
            bool insert;
            int id;
            int[] cell;
            int[] sorted;
            int[] sorted2;
            int[][] vtkCells;
            int[][] vtkCells2;
            CellNeighbour cellNeighbour;
            sectionPlaneCells = new List<int[]>();
            sectionPlaneCellIds = new List<int>();
            CompareIntArray comparer = new CompareIntArray();
            for (int i = 0; i < elementIds.Length; i++)
            {
                if (elements[elementIds[i]] is FeElement3D element)
                {
                    countFront = 0;
                    countBack = 0;
                    for (int j = 0; j < element.NodeIds.Length; j++)
                    {
                        if (frontNodes.Contains(element.NodeIds[j])) countFront++;
                        else if (backNodes.Contains(element.NodeIds[j])) countBack++;
                        if (countFront > 0 && countBack > 0)
                        {
                            // The element is on the plane
                            vtkCells = element.GetAllVtkCells();                            
                            for (int k = 0; k < vtkCells.Length; k++)
                            {
                                insert = true;
                                for (int l = 0; l < vtkCells[k].Length; l++)
                                {
                                    if (frontNodes.Contains(vtkCells[k][l]))
                                    {
                                        insert = false;
                                        break;
                                    }
                                }
                                if (insert)
                                {
                                    cell = null;
                                    sorted = vtkCells[k].ToArray();
                                    Array.Sort(sorted);
                                    cellNeighbour =_cellNeighboursOverCell[sorted];
                                    // Add only cells with elements on the other side
                                    if (cellNeighbour.Id2 != -1)
                                    {
                                        // Looking for the other element of the vtkCell where cell orientation is positive
                                        if (cellNeighbour.Id1 == element.Id)
                                        {
                                            id = cellNeighbour.Id2;
                                            // Find the appropriate element cell
                                            element = elements[id] as FeElement3D;
                                            vtkCells2 = element.GetAllVtkCells();
                                            for (int m = 0; m < vtkCells2.Length; m++)
                                            {
                                                sorted2 = vtkCells2[m].ToArray();
                                                Array.Sort(sorted2);

                                                if (comparer.Equals(sorted, sorted2))
                                                {
                                                    cell = vtkCells2[m];
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            id = cellNeighbour.Id1;
                                            cell = cellNeighbour.Cell1;
                                        }
                                        if (cell == null) throw new Exception("The vtk cell was not found.");
                                        // The vtk cell is in front of the plane
                                        sectionPlaneCells.Add(cell);
                                        sectionPlaneCellIds.Add(id);
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }
        private void AddSectionPlaneCellsAndIds(List<int[]> sectionPlaneCells, List<int> sectionPlaneCellIds)
        {
            // Add section face to visualization                                        
            List<int[]> splitCells = new List<int[]>(_cells);
            List<int> splitCellIds = new List<int>(_cellIds);
            int firstOnPlaneCellLocalId = splitCells.Count;
            splitCells.AddRange(sectionPlaneCells);
            splitCellIds.AddRange(sectionPlaneCellIds);

            // Cell ids by face
            int[][] cellIdsByFace = new int[_cellIdsByFace.Length + 1][];
            for (int i = 0; i < _cellIdsByFace.Length; i++) cellIdsByFace[i] = _cellIdsByFace[i];
            cellIdsByFace[_cellIdsByFace.Length] = new int[sectionPlaneCells.Count];
            for (int i = 0; i < sectionPlaneCells.Count; i++)
                cellIdsByFace[_cellIdsByFace.Length][i] = firstOnPlaneCellLocalId + i;
            
            // Save
            _cells = splitCells.ToArray();
            _cellIds = splitCellIds.ToArray();
            _cellIdsByFace = cellIdsByFace.ToArray();
        }
        private void AddSectionPlaneEdgeCellsAndIds(List<int[]> sectionPlaneCells)
        {
            // Add section edge to visualization                                        
            int[] sorted;
            int[][] cellEdges;
            CompareIntArray comparer = new CompareIntArray();
            Dictionary<int[], int[]> allEdgeCells = new Dictionary<int[], int[]>(comparer);
            foreach (int[] cell in sectionPlaneCells)
            {
                cellEdges = FeMesh.GetVisualizationEdgeCells(cell, ElementFaceType.Face);
                for (int i = 0; i < cellEdges.Length; i++)
                {
                    sorted = cellEdges[i].ToArray();
                    Array.Sort(sorted);
                    if (!allEdgeCells.Remove(sorted)) allEdgeCells.Add(sorted, cellEdges[i]);
                }
            }
            List<int[]> splitEdgeCells = new List<int[]>(_edgeCells);
            int firstOnPlaneEdgeCellLocalId = splitEdgeCells.Count;
            splitEdgeCells.AddRange(allEdgeCells.Values);

            // Edge cell ids by edge
            int[][] edgeCellIdsByEdge = new int[_edgeCellIdsByEdge.Length + 1][];
            for (int i = 0; i < _edgeCellIdsByEdge.Length; i++) edgeCellIdsByEdge[i] = _edgeCellIdsByEdge[i];
            edgeCellIdsByEdge[_edgeCellIdsByEdge.Length] = new int[allEdgeCells.Count];
            for (int i = 0; i < allEdgeCells.Count; i++)
                edgeCellIdsByEdge[_edgeCellIdsByEdge.Length][i] = firstOnPlaneEdgeCellLocalId + i;

            // Face edge ids
            List<int[]> faceEdgeIds = new List<int[]>(_faceEdgeIds);
            faceEdgeIds.Add(new int[] { edgeCellIdsByEdge.Length - 1 });

            // Save
            _edgeCells = splitEdgeCells.ToArray();
            _edgeCellIdsByEdge = edgeCellIdsByEdge.ToArray();
            _faceEdgeIds = faceEdgeIds.ToArray();
        }
    }
}
