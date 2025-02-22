﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeMesh
{
    public class Graph<T> where T : IComparable<T>
    {
        // Variables                                                                                                                
        private NodeList<T> _nodeList;


        // Properties                                                                                                               
        public NodeList<T> Nodes { get { return _nodeList; } }
        public int Count { get { return _nodeList.Count; } }


        // Constructors                                                                                                             
        public Graph()
        {
            _nodeList = new NodeList<T>();
        }
        public Graph(Graph<T> graph)
            : this(graph.Nodes)
        { }
        public Graph(NodeList<T> nodeList)
        {
            _nodeList = new NodeList<T>();
            //
            if (nodeList != null)
            {
                Node<T> newNode;
                Dictionary<Node<T>, Node<T>> oldNewNode = new Dictionary<Node<T>, Node<T>>();
                // Create new nodes
                foreach (var oldNode in nodeList)
                {
                    newNode = new Node<T>(oldNode.Value);
                    AddNode(newNode);
                    oldNewNode.Add(oldNode, newNode);
                }
                // Add connections
                foreach (var oldNode in nodeList)
                {
                    foreach (var neighbour in oldNode.Neighbours)
                    {
                        AddDirectedEdge(oldNewNode[oldNode], oldNewNode[neighbour]);
                    }
                }
            }
        }
        

        // Methods                                                                                                                  
        public void AddNode(Node<T> node)
        {
            // Adds a node to the graph
            _nodeList.Add(node);
        }
        public bool Contains(T value)
        {
            return _nodeList.FindByValue(value) != null;
        }
        public bool Remove(T value)
        {
            // First remove the node from the nodeList
            Node<T> nodeToRemove = _nodeList.FindByValue(value);
            return Remove(nodeToRemove);
        }
        public bool Remove(Node<T> node)
        {
            if (node == null) return false;
            // Remove the node
            _nodeList.Remove(node);
            // Enumerate through each node in the nodeList, removing edges to this node
            foreach (Node<T> gnode in _nodeList)
            {
                int index = gnode.Neighbours.IndexOf(node);
                if (index != -1)
                {
                    // Remove the reference to the node and associated cost
                    gnode.Neighbours.RemoveAt(index);
                }
            }
            //
            return true;
        }
        public T[] GetValues()
        {
            int count = 0;
            T[] values = new T[_nodeList.Count];
            foreach (var item in _nodeList)
            {
                values[count++] = item.Value;
            }
            return values;
        }
        // Edges
        public void AddDirectedEdge(Node<T> from, Node<T> to)
        {
            from.Neighbours.Add(to);
        }
        public void AddUndirectedEdge(Node<T> from, Node<T> to)
        {
            from.Neighbours.Add(to);
            to.Neighbours.Add(from);
        }
        public void RemoveDirectedEdge(Node<T> from, Node<T> to)
        {
            from.Neighbours.Remove(to);
        }
        public void RemoveUndirectedEdge(Node<T> from, Node<T> to)
        {
            from.Neighbours.Remove(to);
            to.Neighbours.Remove(from);
        }
        //
        public bool IsGraphWithoutCycles()
        {
            Node<T> currentNode;
            Node<T> parentNode;
            Queue<Node<T>> queue = new Queue<Node<T>>();
            Queue<Node<T>> parentQueue = new Queue<Node<T>>();
            HashSet<Node<T>> visitedNodes = new HashSet<Node<T>>();
            //
            queue.Enqueue(_nodeList.First());
            parentQueue.Enqueue(_nodeList.First());
            //
            while (queue.Count() > 0)
            {
                currentNode = queue.Dequeue();
                parentNode = parentQueue.Dequeue();
                // Check for cycles
                if (visitedNodes.Add(currentNode))
                {
                    // Add all neighbours to the queue
                    foreach (var neighbour in currentNode.Neighbours)
                    {
                        if (neighbour != parentNode)
                        {
                            queue.Enqueue(neighbour);
                            parentQueue.Enqueue(currentNode);
                        }
                    }
                }
                else return false;
            }
            return true;
        }
        public bool IsGraphWithOneCycle()
        {
            Graph<T> graphCopy = new Graph<T>(_nodeList);
            List<T> singleConnectedItems = new List<T>();
            // Remove all open branches
            do
            {
                singleConnectedItems.Clear();
                //
                foreach (Node<T> node in graphCopy.Nodes)
                {
                    if (node.Neighbours.Count() <= 1) singleConnectedItems.Add(node.Value);
                }
                //
                foreach (var item in singleConnectedItems)
                {
                    graphCopy.Remove(item);
                }
            }
            while (singleConnectedItems.Count > 0);
            // Check for a single cycle
            foreach (Node<T> node in graphCopy.Nodes)
            {
                if (node.Neighbours.Count() > 2) return false;
            }
            return true;
        }
        public List<Graph<T>> GetConnectedSubgraphs()
        {
            Node<T> currentNode;
            HashSet<Node<T>> visitedNodes = new HashSet<Node<T>>();
            Queue<Node<T>> queue = new Queue<Node<T>>();
            NodeList<T> connectedNodes;
            List<Graph<T>> connectedSubgraphs = new List<Graph<T>>();
            // Sort
            try { _nodeList.Sort(); }
            catch { }
            //
            foreach (var node in _nodeList)
            {
                // Check if the node was already added
                if (!visitedNodes.Contains(node))
                {
                    // Create new set of connected nodes
                    connectedNodes = new NodeList<T>();
                    //
                    queue.Enqueue(node);
                    // Search for connected nodes
                    while (queue.Count() > 0)
                    {
                        currentNode = queue.Dequeue();
                        if (visitedNodes.Add(currentNode))
                        {
                            connectedNodes.Add(currentNode);
                            // Add all neighbours to the queue
                            foreach (var neighbour in currentNode.Neighbours)
                                queue.Enqueue(neighbour);
                        }
                    }
                    //
                    connectedSubgraphs.Add(new Graph<T>(connectedNodes));
                }
            }
            //
            return connectedSubgraphs;
        }
        // For unidirected graphs
        public List<T> GetIndependencyList()
        {
            Graph<T> graphCopy = new Graph<T>(_nodeList);
            List<T> childItems = new List<T>();
            List<T> independencyList = new List<T>();
            // Remove all independent items
            do
            {
                childItems.Clear();
                //
                foreach (Node<T> node in graphCopy.Nodes)
                {
                    if (node.Neighbours.Count() == 0) childItems.Add(node.Value);
                }
                //
                foreach (var item in childItems)
                {
                    graphCopy.Remove(item);
                }
                independencyList.AddRange(childItems);
            }
            while (childItems.Count > 0);
            //
            if (graphCopy.Count > 0) throw new NotSupportedException();
            //
            return independencyList;
        }
    }
}
