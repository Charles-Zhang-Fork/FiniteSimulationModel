using System;
using System.Collections.Generic;
using System.Text;

namespace Exporter
{
    /// <summary>
    /// Provides definition for data
    /// </summary>
    [Serializable]
    public class DataGrid
    {
        /// <summary>
        /// Store data as linear region
        /// </summary>
        public Region Region { get; set; }

        public DataGrid(Region region)
        {
            Region = region;
        }
    }

    [Serializable]
    public class Region
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }
        public Node[] Nodes { get; set; }

        public Region()
        {
        }

        public Region(int sizeX, int sizeY, int sizeZ)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            Nodes = new Node[sizeX * sizeY * sizeZ];
        }

        public Region(int sizeX, int sizeY, int sizeZ, Node[] nodes)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }
    }
    [Serializable]
    public class Node
    {
        public string Name { get; set; }

        public Node(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    public class ColorDefinition
    {
        public string Name { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int A { get; set; }

        public ColorDefinition()
        {
        }

        public ColorDefinition(string name, int r, int g, int b, int a)
        {
            Name = name;
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}
