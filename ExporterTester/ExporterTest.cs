using Exporter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ExporterTester
{
    class ExporterTest
    {
        static void Main(string[] args)
        {
            // Initiate output paths
            string folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string logFile = Path.Combine(folder, "log.txt");
            Directory.CreateDirectory(folder);
            // Std output redirection
            TextWriterTraceListener tr1 = new TextWriterTraceListener(Console.Out);
            Trace.Listeners.Add(tr1);
            TextWriterTraceListener tr2 = new TextWriterTraceListener(File.CreateText(logFile));
            Trace.Listeners.Add(tr2);

            // Generate regions of data
            int X = 32;
            int Y = 64;
            int Z = 128;
            Trace.WriteLine("Generate regions of data");
            Region region = new Region(X, Y, Z);
            Random rand = new Random();
            int i = 0;
            for (int z = 0; z < Z; z++)
            {
                for (int y = 0; y < Y; y++)
                {
                    for (int x = 0; x < X; x++)
                    {
                        if (x == 0 && y == 0)
                            region.Nodes[i] = new Node("Z");
                        else if(x == 0 && z == 0)
                            region.Nodes[i] = new Node("Y");
                        else if(y == 0 && z == 0)
                            region.Nodes[i] = new Node("X");
                        else if(rand.NextDouble() > 0.5)
                            region.Nodes[i] = new Node("fill"); 
                        else
                            region.Nodes[i] = new Node("air");
                        i++;
                    }
                }
            }

            // Initiate outputs
            string source = Path.Combine(folder, "source");
            string output = Path.Combine(folder, "output");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(output);

            // Perform conversion
            Trace.WriteLine("Perform conversion...");
            string file = Path.Combine(output, "output.xraw");
            Dictionary<string, ColorDefinition> colors = new Dictionary<string, ColorDefinition>() 
            { 
                { "X", new ColorDefinition("X", 125, 0, 0, 255) },
                { "Y", new ColorDefinition("Y", 0, 125, 0, 255) },
                { "Z", new ColorDefinition("Z", 0, 0, 125, 255) },
                { "fill", new ColorDefinition("fill", 125, 125, 125, 255) },
            };
            ExportOptions options = new ExportOptions()
            {
                ColorDefinitions = colors,
                OutputPath = file
            };
            new XRAWExporter().Convert(options, new DataGrid(region));

            // Done
            Trace.WriteLine($"Output available at: {folder}");
            Process.Start(new ProcessStartInfo()
            {
                FileName = folder,
                UseShellExecute = true,
                Verb = "open"
            });

            Trace.Flush();
        }
    }
}
