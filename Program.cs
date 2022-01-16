using gpxclean.dem;
using gpxclean.gpx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace gpxclean
{
    class Program
    {
        static void Main(string[] args)
        {            
            if (args.Length == 0)
            {
                Console.WriteLine("usage: gpxclean filename [-a AUTHOR;EMAIL;LINK] [-e SRTM_FOLDER]");
                return;
            }

			// parse filename argument
            string inputFile = args[0];
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("{0} does not exist.", inputFile);
				Environment.Exit(-1);
            }

			// parse author argument
			string authorName = string.Empty;
			string authorEMail = string.Empty;
			string authorLink = string.Empty;
			for (uint i = 1; i < args.Length; i++)
			{
				if (args[i] == "-a")
				{
					if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
					{
						Console.WriteLine("Arguments AUTHOR;EMAIL;LINK missing.");
						Environment.Exit(-1);
					}
					
					string[] authorParts = args[i + 1].Split(new char[] { ';' }, StringSplitOptions.None);

					authorName = authorParts.Length > 0 && !string.IsNullOrEmpty(authorParts[0]) ? authorParts[0] : string.Empty;
					authorEMail = authorParts.Length > 1 && !string.IsNullOrEmpty(authorParts[1]) ? authorParts[1] : string.Empty;
					authorLink = authorParts.Length > 2 && !string.IsNullOrEmpty(authorParts[2]) ? authorParts[2] : string.Empty;

					break;
				}
			}

			// parse elevation argument
			IElevationModel elevationModel = null;
			for (uint i = 1; i < args.Length; i++)
			{
				if (args[i] == "-e")
				{
					if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
					{
						Console.WriteLine("Argument SRTM_FOLDER missing.");
						Environment.Exit(-1);
					}

					string srtmFolder = args[i + 1];
					if (!Directory.Exists(srtmFolder))
					{
						Console.WriteLine("{0} does not exist.", srtmFolder);
						Environment.Exit(-1);
					}
					
					elevationModel = new SRTMElevationModel(srtmFolder);
					
					break;
				}
			}

			// set locale
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            // read gpx file
            XmlDocument gpxDocument = ReadGPXFile(inputFile);

            // write gpx file(s)
            foreach (GPXTrack track in GPXTrack.ParseGPX(gpxDocument))
            {
                // use file name as track name if there is no name within the file
                if (track.Name.Length == 0)
                {
                    track.Name = Path.GetFileNameWithoutExtension(inputFile);
                }

				if (authorName.Length > 0)
				{
					track.Author.Name = authorName;
				}

				if (authorEMail.Length > 0)
				{
					track.Author.EMail = authorEMail;
				}

				if (authorLink.Length > 0)
				{
					track.Author.Link = authorLink;
				}

				if (elevationModel != null)
                {
					track.ApplyElevationData(elevationModel);
                }
                
                WriteGPXFile(track);
            }            
        }

        static XmlDocument ReadGPXFile(String inputFile)
        {
            XmlDocument gpxDocument = new XmlDocument();
            try
            {
                gpxDocument.Load(inputFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to read {0}: {1}", inputFile, e.ToString());
                Environment.Exit(-1);
            }

            return gpxDocument;
        }

        static void WriteGPXFile(GPXTrack track)
        {
            String outputFile = String.Format("{0}.gpx", track.Name);
            
            // backup existing file
            if (File.Exists(outputFile))
            {
                string backupFile = String.Format("{0}.bak", outputFile);
                try
                {
                    File.Move(outputFile, backupFile);
                }
                catch (Exception e)
                {                    
                    Console.WriteLine("Unable to create {0}: {1}", backupFile, e.ToString());
                    return;
                }
            }
            
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(new FileStream(outputFile, FileMode.Create), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to create {0}: {1}", outputFile, e.ToString());
                return;
            }

            using (writer)
            {
                // write header
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<gpx xmlns=\"http://www.topografix.com/GPX/1/1\" version=\"1.1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd\" creator=\"gpxclean\">");
                
                // write metadata
                writer.WriteLine("<metadata>");
                writer.WriteLine("\t<name><![CDATA[{0}]]></name>", track.Name);
				if (track.Author.Name.Length > 0 || track.Author.EMail.Length > 0 || track.Author.Link.Length > 0)
                {
                    writer.WriteLine("\t<author>");
                }
				if (track.Author.Name.Length > 0)
				{
					writer.WriteLine("\t\t<name><![CDATA[{0}]]></name>", track.Author.Name);
				}
				if (track.Author.EMail.Length > 0)
				{
					writer.WriteLine("\t\t<email><![CDATA[{0}]]></email>", track.Author.EMail);
				}
				if (track.Author.Link.Length > 0)
				{
					writer.WriteLine("\t\t<link><![CDATA[{0}]]></link>", track.Author.Link);
				}
				if (track.Author.Name.Length > 0 || track.Author.EMail.Length > 0 || track.Author.Link.Length > 0)
				{
					writer.WriteLine("\t</author>");
				}
                writer.WriteLine("</metadata>");

                // write track
                writer.WriteLine("<trk>");
                writer.WriteLine("\t<name><![CDATA[{0}]]></name>", track.Name);
                foreach (List<GPXTrackPoint> trackSegment in track.TrackPoints)
                {
                    writer.WriteLine("\t<trkseg>");
                    foreach (GPXTrackPoint trackPoint in trackSegment)
                    {
                        writer.Write("\t\t<trkpt lat=\"{0}\" lon=\"{1}\">", trackPoint.Latitude, trackPoint.Longitude);
                        if (trackPoint.Elevation.HasValue)
                        {
                            writer.Write("\r\n\t\t\t<ele>{0}</ele>\r\n\t\t", trackPoint.Elevation);
                        }                        
                        writer.WriteLine("</trkpt>");
                    }
                    writer.WriteLine("\t</trkseg>");                    
                }
                writer.WriteLine("</trk>");

                writer.WriteLine("</gpx>");
            }
        }
    }
}
