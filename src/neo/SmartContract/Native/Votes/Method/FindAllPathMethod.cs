using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Native
{
    public class FindAllPathMethod
    {
        /// <summary>
        /// Node Set
        /// </summary>
        private List<int> nodesList = new List<int>();
        /// <summary>
        /// Vector Graphic,describe the connection between nodes and nodes
        /// </summary>
        private Dictionary<String, int> vectorGraphic = new Dictionary<String, int>();
        /// <summary>
        /// Edge Array,describe the connection between nodes and nodes
        /// </summary>
        private int[,] nodesEdgesMatrix;
        /// <summary>
        /// Path Set,store paths in vector graphic.
        /// </summary>
        private List<List<int>> path = new List<List<int>>();

        public FindAllPathMethod(List<int> nodesList, Dictionary<String, int> vectorGraphic)
        {
            this.nodesList = nodesList;
            this.vectorGraphic = vectorGraphic;
            CreateGraphMatrix();
        }

        /// <summary>
        /// create edge array
        /// </summary>
        private void CreateGraphMatrix()
        {
            int nodeCount = nodesList.Count;
            nodesEdgesMatrix = new int[nodeCount, nodeCount];
            for (int i = 0; i < nodeCount; i++)
                for (int j = 0; j < nodeCount; j++)
                {
                    nodesEdgesMatrix[i, j] = -1;
                }
            foreach (KeyValuePair<String, int> entry in vectorGraphic)
            {
                String[] temp = entry.Key.Split(new char[] { ',' });
                String startNode = temp[0];
                int startNodeIndex = System.Convert.ToInt32(temp[0]);
                String endNode = temp[1];
                int endNodeIndex = System.Convert.ToInt32(temp[1]);
                int weight = entry.Value;
                nodesEdgesMatrix[startNodeIndex, endNodeIndex] = weight;
            }
        }

        /// <summary>
        /// Find all paths from startNode to endNode
        /// </summary>
        /// <param name="startNode">startNode</param>
        /// <param name="endNode">endNode</param>
        /// <returns>path set</returns>
        public List<List<int>> FindPath(int startNode, int endNode)
        {
            path.Clear();
            List<int> tempUnUsedNodes = new List<int>();
            List<int> tempPath = new List<int>();
            tempUnUsedNodes.AddRange(nodesList);
            tempUnUsedNodes.Remove(startNode);
            CreatePath(tempUnUsedNodes, startNode, endNode, tempPath);
            return path;
        }

        /// <summary>
        /// Create all paths from current node to end node
        /// </summary>
        /// <param name="unUsedNodes">unused node set</param>
        /// <param name="currentNode">current node</param>
        /// <param name="endNode">end node</param>
        /// <param name="hasCreatedPath">the path has been created</param>
        private void CreatePath(List<int> unUsedNodes, int currentNode, int endNode,
                                      List<int> hasCreatedPath)
        {
            if (currentNode.Equals(endNode))
            {
                List<int> tempPath = new List<int>();
                tempPath.AddRange(hasCreatedPath);
                tempPath.Add(currentNode);
                path.Add(tempPath);
                return;
            }
            //calcualte the next node can be arrived
            int startNodeIndex = System.Convert.ToInt32(currentNode);
            List<int> achievedNodes = new List<int>();
            foreach (int unUsedNode in unUsedNodes)
            {
                int achievedNodeIndex = System.Convert.ToInt32(unUsedNode);
                if (nodesEdgesMatrix[startNodeIndex, achievedNodeIndex] > 0)
                {
                    achievedNodes.Add(unUsedNode);
                }
            }
            //Exit condition, reaching the end node
            if (achievedNodes.Count == 0)
            {
                return;
            }
            else
            {
                //Continue to traverse the node
                foreach (int achievedNode in achievedNodes)
                {
                    List<int> tempUnUsedNodes = new List<int>();
                    tempUnUsedNodes.AddRange(unUsedNodes);
                    tempUnUsedNodes.Remove(achievedNode);
                    List<int> tempPath = new List<int>();
                    tempPath.AddRange(hasCreatedPath);
                    tempPath.Add(currentNode);
                    CreatePath(tempUnUsedNodes, achievedNode, endNode, tempPath);
                }
            }
        }

        /// <summary>
        /// Find all paths in  vector graphic
        /// </summary>
        /// <returns>path set</returns>
        public List<List<int>> FindAllPath()
        {
            path.Clear();
            foreach (int i in nodesList)
            {
                foreach (int j in nodesList)
                {
                    if (!i.Equals(j))
                    {
                        List<int> tempUnUsedNodes = new List<int>();
                        List<int> tempPath = new List<int>();
                        tempUnUsedNodes.AddRange(nodesList);
                        tempUnUsedNodes.Remove(i);
                        CreatePath(tempUnUsedNodes, i, j, tempPath);
                    }
                }
            }
            return path;
        }

        /// <summary>
        /// Print edge array
        /// </summary>
        public void PrintEdgeArray()
        {
            Console.WriteLine("EdgeArray:行代表from,列代表to");
            Console.WriteLine("From/to ");
            nodesList.ForEach(p => Console.Write(p + "   "));
            Console.Write("\n");
            for (int i = 0; i < nodesEdgesMatrix.GetLength(0); i++)
            {
                Console.Write(nodesList[i] + "      ");
                for (int j = 0; j < nodesEdgesMatrix.GetLength(1); j++)
                {
                    Console.Write(nodesEdgesMatrix[i, j] > 0 ? nodesEdgesMatrix[i, j] + "   " : nodesEdgesMatrix[i, j] + "  ");
                }
                Console.WriteLine("\n");
            }
        }

        /// <summary>
        /// Print path from startNode to endNode
        /// </summary>
        /// <param name="startNode">startNode</param>
        /// <param name="endNode">endNode</param>
        public void PrintPath(int startNode, int endNode)
        {
            Console.WriteLine(String.Format("create path：{0}--->{1}", startNode, endNode));
            FindPath(startNode, endNode);
            if (path.Count == 0)
            {
                Console.WriteLine("No way");
                return;
            }
            foreach (List<int> entry in path)
            {
                Console.Write("Path:");
                foreach (int node in entry)
                {
                    Console.Write(node + ",");
                }
                Console.Write("\n");
            }
        }

        /// <summary>
        /// Print all paths
        /// </summary>
        public void PrintAllPath()
        {
            FindAllPath();
            foreach (List<int> entry in path)
            {
                Console.Write("Path:");
                foreach (int node in entry)
                {
                    Console.Write(node + ",");
                }
                Console.Write("\n");
            }
        }
    }
}
