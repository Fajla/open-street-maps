using SpatialLite.Core;
using SpatialLite.Osm;
using SpatialLite.Osm.Geometries;
using SpatialLite.Osm.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenStreetMaps
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: <program> map_file_path tag_key tag_value");
                return;
            }

            string mapFilePath = args[0];
            string tagKey = args[1];
            string tagValue = args[2];

            Console.WriteLine("Started");
            Console.WriteLine("Searching ways with tag ...");

            if (File.Exists("temp.pbf"))
                File.Delete("temp.pbf");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            HashSet<int> nodesIds = new HashSet<int>();
            var readerSettings = new OsmReaderSettings { ReadMetadata = false };
            using (var reader = new PbfReader(mapFilePath, readerSettings))
            {
                while (true)
                {
                    var entity = reader.Read();
                    if (entity == null)
                        break;

                    if (entity is WayInfo && entity.Tags.Any(x => x.Key == tagKey && x.Value == tagValue))
                    {
                        var wayInfo = (WayInfo)entity;
                        nodesIds.UnionWith(wayInfo.Nodes);
                    }
                }
            }

            Console.WriteLine("Filtering data from map file ...");
            using (var reader = new PbfReader(mapFilePath, readerSettings))
            {
                using (var writer = new PbfWriter("temp.pbf", new PbfWriterSettings { WriteMetadata = false }))
                {
                    while (true)
                    {
                        var entity = reader.Read();
                        if (entity == null)
                            break;

                        if (entity is NodeInfo && nodesIds.Contains(entity.ID))
                        {
                            writer.Write(entity);
                        }

                        if (entity is WayInfo && entity.Tags.Any(x => x.Key == tagKey && x.Value == tagValue))
                        {
                            writer.Write(entity);
                        }
                    }

                }
            }



            Console.WriteLine("Filtering finished");
            Console.WriteLine("Loading filtered map data ...");

            OsmGeometryDatabase db;
            using (var reader = new PbfReader("temp.pbf", readerSettings))
            {
                db = OsmGeometryDatabase.Load(reader, true);
            }

            Console.WriteLine("Map data loaded");
            Console.WriteLine("Calculating total length ...");

            double totalLength = db.Ways.Sum(way => Measurements.Sphere2D.ComputeLength(way));
            totalLength /= 1000;

            Console.WriteLine("Finished");
            Console.WriteLine();

            Console.WriteLine("Total length: {0:0.###} km", totalLength);
            
            watch.Stop();

            var elapsedMs = watch.ElapsedMilliseconds;
            var minutes = elapsedMs / 1000 / 60;
            var seconds = elapsedMs / 1000 % 60;

            Console.WriteLine("{0:0} minutes {1:0} seconds", minutes, seconds);

            Console.ReadLine();
        }
    }
}
