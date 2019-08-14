using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Native
{
    public class FindAllPathMethod
    {
        /// <summary>
        /// Node Set
        /// </summary>
        private List<String> nodesList = new List<String>();
        /// <summary>
        /// Vector Graphic,describe the connection between nodes and nodes
        /// </summary>
        private Dictionary<String, int> vectorGraphic = new Dictionary<String, int>();
        /// <summary>
        /// Edge Array,describe the connection between nodes and nodes
        /// </summary>
        private int[,] edgeArray;
        /// <summary>
        /// Path Set,store paths in vector graphic.
        /// </summary>
        private List<List<String>> path = new List<List<String>>();

        public FindAllPathMethod(List<String> nodesList, Dictionary<String, int> vectorGraphic)
        {
            this.nodesList = nodesList;
            this.vectorGraphic = vectorGraphic;
            CreateEdgeArray();
        }

        /// <summary>
        /// create edge array
        /// </summary>
        private void CreateEdgeArray()
        {
            int nodeCount = nodesList.Count;
            edgeArray = new int[nodeCount, nodeCount];
            for (int i = 0; i < nodeCount; i++)
                for (int j = 0; j < nodeCount; j++)
                {
                    edgeArray[i, j] = -1;
                }
            foreach (KeyValuePair<String, int> entry in vectorGraphic)
            {
                String[] temp = entry.Key.Split(new char[] { ',' });
                String startNode = temp[0];
                int startNodeIndex = System.Convert.ToInt32(temp[0]);
                String endNode = temp[1];
                int endNodeIndex = System.Convert.ToInt32(temp[1]);
                int weight = entry.Value;
                edgeArray[startNodeIndex, endNodeIndex] = weight;
            }
        }

        /// <summary>
        /// Find all paths from startNode to endNode
        /// </summary>
        /// <param name="startNode">startNode</param>
        /// <param name="endNode">endNode</param>
        /// <returns>path set</returns>
        public List<List<String>> FindPath(String startNode, String endNode)
        {
            path.Clear();
            List<String> tempUnUsedNodes = new List<String>();
            List<String> tempPath = new List<String>();
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
        private void CreatePath(List<String> unUsedNodes, String currentNode, String endNode,
                                      List<String> hasCreatedPath)
        {
            if (currentNode.Equals(endNode))
            {
                List<String> tempPath = new List<String>();
                tempPath.AddRange(hasCreatedPath);
                tempPath.Add(currentNode);
                path.Add(tempPath);
                return;
            }
            //calcualte the next node can be arrived
            int startNodeIndex = System.Convert.ToInt32(currentNode);
            List<String> achievedNodes = new List<String>();
            foreach (String unUsedNode in unUsedNodes)
            {
                int achievedNodeIndex = System.Convert.ToInt32(unUsedNode);
                if (edgeArray[startNodeIndex, achievedNodeIndex] > 0)
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
                foreach (String achievedNode in achievedNodes)
                {
                    List<String> tempUnUsedNodes = new List<String>();
                    tempUnUsedNodes.AddRange(unUsedNodes);
                    tempUnUsedNodes.Remove(achievedNode);
                    List<String> tempPath = new List<String>();
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
        public List<List<String>> FindAllPath()
        {
            path.Clear();
            foreach (String i in nodesList)
            {
                foreach (String j in nodesList)
                {
                    if (!i.Equals(j))
                    {
                        List<String> tempUnUsedNodes = new List<String>();
                        List<String> tempPath = new List<String>();
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
            for (int i = 0; i < edgeArray.GetLength(0); i++)
            {
                Console.Write(nodesList[i] + "      ");
                for (int j = 0; j < edgeArray.GetLength(1); j++)
                {
                    Console.Write(edgeArray[i, j] > 0 ? edgeArray[i, j] + "   " : edgeArray[i, j] + "  ");
                }
                Console.WriteLine("\n");
            }
        }

        /// <summary>
        /// Print path from startNode to endNode
        /// </summary>
        /// <param name="startNode">startNode</param>
        /// <param name="endNode">endNode</param>
        public void PrintPath(String startNode, String endNode)
        {
            Console.WriteLine(String.Format("create path：{0}--->{1}", startNode, endNode));
            FindPath(startNode, endNode);
            if (path.Count == 0)
            {
                Console.WriteLine("No way");
                return;
            }
            foreach (List<String> entry in path)
            {
                Console.Write("Path:");
                foreach (String node in entry)
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
            foreach (List<String> entry in path)
            {
                Console.Write("Path:");
                foreach (String node in entry)
                {
                    Console.Write(node + ",");
                }
                Console.Write("\n");
            }
        }
    }
}
