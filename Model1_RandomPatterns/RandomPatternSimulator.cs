using Exporter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Model1_RandomPatterns
{
    internal class RandomPatternSimulator
    {
        #region Public Properties
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }
        public Region Region { get; set; }
        public Pattern[] Patterns { get; set; }
        #endregion

        #region Private Properties
        private Dictionary<string, ColorDefinition> Colors { get; set; }
        private static readonly Node Zero = new Node("Zero");
        private static readonly Node One = new Node("One");
        #endregion

        #region Constructor
        public RandomPatternSimulator(int sizeX, int sizeY, int sizeZ)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            Region = new Region(SizeX, SizeY, SizeZ);

            // Define internal colors
            Colors = new Dictionary<string, ColorDefinition>()
            {
                { "One", new ColorDefinition("One", 125, 125, 125, 255) },
                { "Zero", new ColorDefinition("Zero", 0, 0, 0, 0) },
            };
        }
        #endregion

        #region Simulation Interface
        /// <param name="maxPatternSize">Inclusive</param>
        public void GeneratePatterns(int patternCount, int maxPatternSize, int minPatternSize = 2, bool forceOddNumber = false, bool report = true)
        {
            // Notice the uniqueness of patterns are not checked in this implementaion; 
            // So during matching we will just find and use the first pattern that we found to match

            Pattern[] existingPatterns = Patterns;

            Random rand = new Random();
            Patterns = new Pattern[patternCount];
            for (int i = 0; i < patternCount; i++)
            {
                // Find pattern size
                int size = rand.Next(minPatternSize >=2 ? minPatternSize: 2, maxPatternSize + 1); // Size-1 patterns are not allowed currently because that can cause trouble during match
                // Force odd number
                if (forceOddNumber && size % 2 == 0)
                    size = size + 1;
                // Initialize match
                PatternDefinition[,,] match = new PatternDefinition[size, size, size];
                for (int x = 0; x < size; x++)
                    for (int y = 0; y < size; y++)
                        for (int z = 0; z < size; z++)
                            match[x, y, z] = (PatternDefinition)rand.Next(0, Enum.GetNames(typeof(PatternDefinition)).Length);
                // Randomize behavior
                PatternBehavior behavior = (PatternBehavior)rand.Next(0, Enum.GetNames(typeof(PatternBehavior)).Length);
                // Create pattern
                var pattern = new Pattern(i, size)
                {
                    Behavior = behavior,
                    Match = match
                };
                Patterns[i] = pattern;
                // Report
                if (report)
                    Trace.WriteLine($"New pattern: {pattern}");
            }

            if(existingPatterns != null)
            {
                var temp = new List<Pattern>(existingPatterns);
                temp.AddRange(Patterns);
                Patterns = temp.ToArray();
            }
        }
        public void InitializeGrid(double probability = 0.5)
        {
            // Generate random
            Random rand = new Random();
            int i = 0;
            for (int z = 0; z < SizeZ; z++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    for (int x = 0; x < SizeX; x++)
                    {
                        if (rand.NextDouble() > 1 - probability)
                            Region.Nodes[i] = One;
                        else
                            Region.Nodes[i] = Zero;
                        i++;
                    }
                }
            }
        }
        public void PerformSimulationStep(string ID, bool report = false, bool payload = false)
        {
            if (report)
                Payload.AppendLine($"Simulation Step (ID): {ID}");

            // Make copy of internal state
            Region copy = DeepClone<Region>(Region);

            // Perform update on the copied state
            Object locker  = new Object();
            int updateCount = 0;
            Parallel.For(0, SizeX, (x, state) =>
            {
                for (int y = 0; y < SizeY; y++)
                {
                    for (int z = 0; z < SizeZ; z++)
                    {
                        int index = x * SizeY * SizeZ + y * SizeZ + z;

                        Pattern match = TryFindMatch(x, y, z, Region.Nodes[index], Region);
                        // Perform modification
                        if (match != null)
                        {
                            // Payload
                            if (payload)
                            {
                                lock (locker)
                                {
                                    Payload.AppendLine($"Match found for index {index} ({x}x{y}x{z}) with [{Region.Nodes[index].Name}] for {{{match}}}");
                                }
                            }
                                
                            // Perform action
                            switch (match.Behavior)
                            {
                                case PatternBehavior.Zero:
                                    copy.Nodes[index] = Zero;
                                    break;
                                case PatternBehavior.One:
                                    copy.Nodes[index] = One;
                                    break;
                                case PatternBehavior.Toggle:
                                    if (Region.Nodes[index].Name == Zero.Name)
                                        copy.Nodes[index] = One;
                                    else
                                        copy.Nodes[index] = Zero;
                                    break;
                                default:
                                    throw new InvalidDataException("Unknown pattern behavior encountered.");
                            }
                            // Increment counter
                            lock(locker)
                            {
                                updateCount++;
                            }
                        }
                    }
                }
            });

            // Replace old state with new state
            Region = copy;

            // Report
            if(report)
            {
                Trace.WriteLine($"{updateCount} nodes are updated.");
            }
        }
        #endregion

        #region Additional
        public StringBuilder Payload = new StringBuilder();
        #endregion

        #region Data Interoperability
        public void Save(string folder, string ID)
        {
            string source = Path.Combine(folder, "source");
            string output = Path.Combine(folder, "output");
            string sourceFile = Path.Combine(source, $"{ID}.bin");
            string outputFile = Path.Combine(output, $"{ID}.xraw");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(output);

            // Save raw state
            WriteToBinaryFile(sourceFile, Region, false);

            // Perform conversion and save output
            ExportOptions options = new ExportOptions()
            {
                ColorDefinitions = Colors,
                OutputPath = outputFile,
                EmptyNodeName = Zero.Name, 
                Overwrite = false,
                Report = true
            };
            new XRAWExporter().Convert(options, new DataGrid(Region));
        }
        #endregion

        #region Private Methods
        private Pattern TryFindMatch(int x, int y, int z, Node node, Region region)
        {
            Pattern match = null;
            foreach (var pattern in Patterns)
            {
                // Compare each pattern element
                for (int px = 0; px < pattern.Size; px++)
                {
                    for (int py = 0; py < pattern.Size; py++)
                    {
                        for (int pz = 0; pz < pattern.Size; pz++)
                        {
                            if (pattern.Match[px, py, pz] == PatternDefinition.Any)
                                continue;

                            // Loop all boundary conditions
                            int tz = z + pz;
                            if (tz < 0)
                                tz += region.SizeZ;
                            else if (tz >= region.SizeZ)
                                tz -= region.SizeZ;

                            int ty = y + py;
                            if (ty < 0)
                                ty += region.SizeY;
                            else if (ty >= region.SizeY)
                                ty -= +region.SizeY;

                            int tx = x + px;
                            if (tx < 0)
                                tx += region.SizeX;
                            else if (tx >= region.SizeX)
                                tx -= region.SizeX;

                            // Get node to compare with; Notice the match starts with a corner rather than being centered
                            Node n = region.Nodes[tz * region.SizeY * region.SizeX + ty * region.SizeX + tx];
                            // Get node type
                            PatternDefinition nodeType = n == Zero ? PatternDefinition.Zero : PatternDefinition.One;
                            // Compare with node
                            if (pattern.Match[px, py, pz] != nodeType)
                                // Fail match
                                goto EndMatch;
                            else
                                // Continue matching
                                continue;
                        }
                    }
                }
                // Obtain a successful match
                match = pattern;
            EndMatch:
                // End matching when a successful match is found
                if (match != null)
                    break;
            }
            return match;
        }
        private T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the binary file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the binary file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the binary file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }
        #endregion
    }

    

    class Program
    {
        static void Main(string[] args)
        {
            // Simulation configurations
            int simulationSize = 128;
            bool payload = false;
            int maxPatternSize = 3;
            double probability = 0.75;
            int totalIterations = 1000; // Iterate total 100 simualtion steps
            int saveIncrement = 50;  // Save intermediate states and results every 10 iterations

            // Initiate output paths
            string folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string logFile = Path.Combine(folder, "log.txt");
            Directory.CreateDirectory(folder);
            // Std output redirection
            TextWriterTraceListener tr1 = new TextWriterTraceListener(Console.Out);
            Trace.Listeners.Add(tr1);
            TextWriterTraceListener tr2 = new TextWriterTraceListener(File.CreateText(logFile));
            Trace.Listeners.Add(tr2);

            // Create simulator
            Trace.WriteLine("Create simulator.");
            RandomPatternSimulator simulator = new RandomPatternSimulator(simulationSize, simulationSize, simulationSize);
            Trace.WriteLine($"Grid Size: {simulator.Region.SizeX} x {simulator.Region.SizeY} x {simulator.Region.SizeZ}");

            // Initialize simulator
            Trace.WriteLine("Generate random match patterns...");
            long estimateMax = (long)Math.Pow(2, maxPatternSize * maxPatternSize * maxPatternSize);
            int limitedCount = estimateMax > 1024 ? 1024 : (int)estimateMax;
            simulator.GeneratePatterns(/*limitedCount*/15, maxPatternSize, 2); // Small patterns
            simulator.GeneratePatterns(/*limitedCount*/3, maxPatternSize*2, maxPatternSize); // Larger patterns
            Trace.WriteLine("Populate initial state...");
            simulator.InitializeGrid(probability);
            Trace.WriteLine($"{simulator.Region.SizeX * simulator.Region.SizeY * simulator.Region.SizeZ} nodes are created.");

            // Save initial state
            int ID = 0;
            Trace.WriteLine("Save initial state...");
            simulator.Save(folder, ID.ToString());

            // Start simulation
            Trace.WriteLine($"Start simulation: {totalIterations}/{saveIncrement}.");
            for (int i = 1; i <= totalIterations; i++)
            {
                ID++;

                // Interrupt
                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.S)
                {
                    Trace.WriteLine("Simulation is interrupted. Exit.");
                    break;
                }

                // Report
                Trace.WriteLine($"Simulation iteration: {i}... (Press `S` to stop simulation)");

                // Progress simulation
                simulator.PerformSimulationStep(i.ToString(), true, payload);

                // Save states and results
                if (i % saveIncrement == 0)
                {
                    Trace.WriteLine("Save intermediate results...");
                    simulator.Save(folder, ID.ToString());
                }
            }

            // Done
            Trace.WriteLine("Simulation is finished.");
            Trace.WriteLine($"Output available at: {folder}");
            Process.Start(new ProcessStartInfo()
            {
                FileName = folder,
                UseShellExecute = true,
                Verb = "open"
            });

            // Save
            Trace.Flush();
            Trace.Close();
            // Additional
            if(payload)
            {
                File.AppendAllText(logFile, "\n\nDetailed report:\n");
                File.AppendAllText(logFile, simulator.Payload.ToString());
            }
        }
    }
}
