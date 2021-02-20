using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Exporter
{
    /// <summary>
    /// Options for export
    /// </summary>
    public class ExportOptions
    {
        public string OutputPath;
        public bool Overwrite = false;
        public bool Report = true;
        public string EmptyNodeName = "air";
        public Dictionary<string, ColorDefinition> ColorDefinitions = new Dictionary<string, ColorDefinition>();
    }

    /// <summary>
    /// Exports a grid of voxel data into XRAW; Currently support 256 palette
    /// </summary>
    public class XRAWExporter
    {
        public void Convert(ExportOptions o, DataGrid data)
        {
            if (File.Exists(o.OutputPath) && o.Overwrite == false)
                throw new IOException("Output file already exist and overwritting is not permitted.");

            // Data validation
            if (data.Region.Nodes.Length != data.Region.SizeX * data.Region.SizeY * data.Region.SizeZ)
                throw new InvalidDataException("Input data region node size doesn't match specified dimensions.");

            // Colors preprocessing
            if(o.Report)
                Console.WriteLine("Preprocessing referenced colors...");
            HashSet<string> uniqueNodes = data.Region.Nodes.Select(n => n.Name).Distinct().ToHashSet();
            Dictionary<string, ColorDefinition> referencedColors = o.ColorDefinitions.Where(c => uniqueNodes.Contains(c.Key)).ToDictionary(p => p.Key, p => p.Value);
            List<ColorDefinition> colorIndices = referencedColors.Values.ToList();
            Dictionary<string, int> colorIndexDict = colorIndices.Select((c, i) => new KeyValuePair<string, int>(c.Name, i))
                .ToDictionary(p => p.Key, p => p.Value);

            // Exception handling
            if (referencedColors.Count() > 255)
                throw new InvalidDataException("More than 255 colors are used.");
            // Missing color handling
            if (o.Report)
            {
                var missingColors = uniqueNodes.Where(n => !o.ColorDefinitions.ContainsKey(n)).ToList();
                if (referencedColors.Count == 0)
                {
                    Console.WriteLine("[Warning] Some of the node types are not recognized in color file, please fix the following:");
                    foreach (string item in uniqueNodes)
                        if (item != o.EmptyNodeName && item != "ignore")
                            Console.WriteLine($"\t{item}");
                }
                else if (missingColors.Count != 0)
                {
                    Console.WriteLine($"[Warning] {missingColors.Count} colors are not fonud in color presets, please fix the following:");
                    foreach (var name in missingColors)
                    {
                        if (name == o.EmptyNodeName)
                            Console.WriteLine($"  Node type \"{name}\" is not defined in color file - but it's also not needed; Will treat as empty (color index 0).");
                        else if (name == "ignore")
                            Console.WriteLine("  Exported region contains node type \"ignore\"; Will treat as empty (color index 0).");
                        else if (colorIndices.Count > 1)
                            Console.WriteLine($"  Node type \"{name}\" is not defined in color file; Will use `{colorIndices[1].Name}` instead (color index 1).");
                        else
                            Console.WriteLine($"  Node type \"{name}\" is not defined in color file; Will use color index value 1 instead.");
                    }
                }
            }

            // Insert an empty color index
            if(!colorIndexDict.ContainsKey(o.EmptyNodeName))
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
            if(o.Report)
                Console.WriteLine("Convert source to XRAW format...");
            using (BinaryWriter writer = new BinaryWriter(File.Open(o.OutputPath, FileMode.Create)))
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
                writer.Write((int)data.Region.SizeX);
                writer.Write((int)data.Region.SizeY);
                writer.Write((int)data.Region.SizeZ);
                writer.Write((int)256);

                // Voxel Buffer
                Random random = new Random();
                foreach (Node node in data.Region.Nodes)
                {
                    // Special handle empty node
                    if (node.Name == o.EmptyNodeName)
                        writer.Write((byte)0);
                    else if (colorIndexDict.ContainsKey(node.Name))
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
            if(o.Report)
                Console.WriteLine("Conversion finished.");
        }
    }
}
