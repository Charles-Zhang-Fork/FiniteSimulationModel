using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Converter
{
    internal class Region
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }
        public Node[] Nodes { get; set; }
    }
    internal class Node
    {
        public string Name { get; set; }
    }

    internal class ColorDefinition
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

    class Program
    {
        static int Main(string[] args)
        {
            // Exception handling
            if (args.Length == 0)
            {
                Console.WriteLine("Not enough arguments.");
                return -1; 
            }

            // Extract parameters
            string sourcePath = args[0];
            string targetPath = args[1];
            Console.WriteLine($"Processing files: \n  Input - {sourcePath} \n  Output - {targetPath}");

            // Start extraction
            Console.WriteLine("Reading source region...");
            Region region = ParseRegion(File.ReadAllLines(sourcePath));
            Console.WriteLine($"{region.Nodes.Length} nodes loaded.");

            // Read colors
            Console.WriteLine("Read color definitions...");
            string workingDirectory = Directory.GetCurrentDirectory();
            string colorsFilePath = Path.Combine(workingDirectory, "Colors.txt");
            Dictionary<string, ColorDefinition> presetColors;
            if(File.Exists(colorsFilePath))
            {
                Console.WriteLine($"Custom color configuration file found ({colorsFilePath}), use custom colors.");
                presetColors = ParseColors(File.ReadAllText(colorsFilePath));
            }
            else
            {
                Console.WriteLine($"No custom color configuration file is found in current working directory ({workingDirectory}), use internal colors.");
                presetColors = ParseColors(ReadResource("Data.Colors.txt"));
            }

            // Colors preprocessing
            Console.WriteLine("Preprocessing referenced colors...");
            HashSet<string> uniqueNodes = region.Nodes.Select(n => n.Name).Distinct().ToHashSet();
            var referencedColors = presetColors.Where(c => uniqueNodes.Contains(c.Key)).ToDictionary(p => p.Key, p => p.Value);
            List<ColorDefinition> colorIndices = referencedColors.Values.ToList();
            Dictionary<string, int> colorIndexDict = colorIndices.Select((c, i) => new KeyValuePair<string, int>(c.Name, i))
                .ToDictionary(p => p.Key, p => p.Value);
            // Exception handling
            if(referencedColors.Count() > 255)
            {
                Console.WriteLine("[Error] More than 255 colors are used.");
                return -2;
            }
            // Missing color handling
            var missingColors = uniqueNodes.Where(n => !presetColors.ContainsKey(n)).ToList();
            if(referencedColors.Count == 0)
            {
                Console.WriteLine("[Warning] Some of the node types are not recognized in color file, please fix the following:");
                foreach (string item in uniqueNodes)
                    if(item != "air" && item != "ignore")
                        Console.WriteLine($"\t{item}");
            }
            else if(missingColors.Count != 0)
            {
                Console.WriteLine($"[Warning] {missingColors.Count} colors are not fonud in color presets, please fix the following:");
                foreach (var name in missingColors)
                {
                    if(name == "air")
                        Console.WriteLine($"  Node type \"{name}\" is not defined in color file - but it's also not needed; Will treat as empty (color index 0).");
                    else if(name == "ignore")
                        Console.WriteLine("  Exported region contains node type \"ignore\"; Will treat as empty (color index 0).");
                    else if (colorIndices.Count > 1)
                        Console.WriteLine($"  Node type \"{name}\" is not defined in color file; Will use `{colorIndices[1].Name}` instead (color index 1).");
                    else
                        Console.WriteLine($"  Node type \"{name}\" is not defined in color file; Will use color index value 1 instead.");
                }
            }

            // Insert an empty color index
            if (!colorIndexDict.ContainsKey(o.EmptyNodeName))
            {
                colorIndices.Add(new ColorDefinition(o.EmptyNodeName, 0, 0, 0, 0));
                colorIndexDict[o.EmptyNodeName] = colorIndices.Count - 1;
            }
            // Switch color index 0 with empty
            int replaceLoc = colorIndexDict[o.EmptyNodeName];
            ColorDefinition emptyColor = colorIndices[replaceLoc];
            ColorDefinition replaceColor = colorIndices[0];
            // Switch dict
            colorIndexDict[o.EmptyNodeName] = 0;
            colorIndexDict[replaceColor.Name] = replaceLoc;
            // Switch color
            colorIndices[0] = emptyColor;
            colorIndices[replaceLoc] = replaceColor;

            // Perform conversion
            Console.WriteLine("Convert source to XRAW format...");
            using (BinaryWriter writer = new BinaryWriter(File.Open(targetPath, FileMode.Create)))
            {
                // Header
                // Magic Number
                writer.Write(Encoding.ASCII.GetBytes("XRAW"));
                // Color Meta: unsigned RGBA 8-bit 256-color palette
                writer.Write((byte)0);
                writer.Write((byte)4);
                writer.Write((byte)8);
                writer.Write((byte)8);
                // Size: 2x2x1 - 256 Colors
                writer.Write((int)region.SizeX);
                writer.Write((int)region.SizeY);
                writer.Write((int)region.SizeZ);
                writer.Write((int)256);

                // Voxel Buffer
                Random random = new Random();
                foreach (Node node in region.Nodes)
                {
                    // Special handle air
                    if (node.Name == "air") 
                        writer.Write((byte)0);
                    else if(colorIndexDict.ContainsKey(node.Name))
                        writer.Write((byte)colorIndexDict[node.Name]);
                    else
                        writer.Write((byte)1); // Use index 1 for absent node types
                }

                // Color Palette Buffer
                for (int i = 0; i < 256; i++)
                {
                    // Real colors
                    if (i < colorIndices.Count)
                    {
                        writer.Write((byte)colorIndices[i].R); // R
                        writer.Write((byte)colorIndices[i].G); // G
                        writer.Write((byte)colorIndices[i].B); // B
                        writer.Write((byte)colorIndices[i].A); // A
                    }
                    // Padding
                    else
                    {
                        writer.Write((byte)i); // R
                        writer.Write((byte)i); // G
                        writer.Write((byte)i); // B
                        writer.Write((byte)255); // A
                    }
                }

                // Dispose
                writer.Flush();
                writer.Close();
            }
            Console.WriteLine("Conversion finished.");

            // Return success
            return 0;
        }

        public static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(Converter)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        /// <remarks>Use a custom implementation becauase a fully-featured YAML parser is slow</remarks>
        private static Region ParseRegion(string[] lines)
        {
            Region region = new Region();
            // Read SizeX
            if (lines[0].StartsWith("SizeX: "))
                region.SizeX = int.Parse(lines[0].Substring(7));
            else
                Console.WriteLine("The first line of source file must be \"SizeX\".");
            // Read SizeY
            if (lines[1].StartsWith("SizeY: "))
                region.SizeY = int.Parse(lines[1].Substring(7));
            else
                Console.WriteLine("The second line of source file must be \"SizeY\".");
            // Read SizeZ
            if (lines[2].StartsWith("SizeZ: "))
                region.SizeZ = int.Parse(lines[2].Substring(7));
            else
                Console.WriteLine("The third line of source file must be \"SizeZ\".");
            // Skip "Nodes:" line
            if (!lines[3].StartsWith("Nodes"))
                Console.WriteLine("The forth line of source file must be \"Nodes\".");
            // Count non-empty lines
            int nonempty = lines.Count(l => !string.IsNullOrWhiteSpace(l));
            // Read nodes
            region.Nodes = new Node[nonempty - 4];
            for (int i = 4; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                else
                {
                    region.Nodes[i - 4] = new Node { Name = line.Substring(line.IndexOf("- Name: ") + "- Name: ".Length) };
                }
            }
            return region;
        }

        private static Dictionary<string, ColorDefinition> ParseColors(string text)
        {
            Dictionary<string, ColorDefinition> colors = new Dictionary<string, ColorDefinition>();
            // Parse each line
            StringReader strReader = new StringReader(text);
            while (true)
            {
                string line  = strReader.ReadLine();
                // EOF
                if (line == null) break;

                // Skip comment lines
                if (line.StartsWith('#')) continue;
                // Skip empty lines
                else if (string.IsNullOrWhiteSpace(line)) continue;
                // Parse space delimited values
                else
                {
                    // Split
                    var values = line.Split(' ');
                    // Skip ill-formatted lines
                    if (values.Length != 4 && values.Length != 5)
                    {
                        Console.WriteLine($"Color definition line \"{line}\" is ill-formatted (`NodeName R G B (A)`); Skip.");
                        continue;
                    }

                    // Extract
                    string name = values[0];
                    int r = int.Parse(values[1]);
                    int g = int.Parse(values[2]);
                    int b = int.Parse(values[3]);
                    int a = values.Length == 5 ? int.Parse(values[4]) : byte.MaxValue;
                    // Save
                    colors[values[0]] = new ColorDefinition(name, r, g, b, a);
                }
            }
            return colors;
        }
    }
}
