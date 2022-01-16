using gpxclean.dem;
using System;
using System.Collections.Generic;
using System.Xml;

namespace gpxclean.gpx
{
    class GPXTrack
    {
        public String Name { get; internal set; }
        public GPXAuthor Author { get; internal set; }
        public List<List<GPXTrackPoint>> TrackPoints { get; private set; }

        private GPXTrack()
        {
            Name = String.Empty;
			Author = new GPXAuthor();
            TrackPoints = new List<List<GPXTrackPoint>>();
        }

        public void ApplyElevationData(IElevationModel elevationModel)
        {
            foreach (List<GPXTrackPoint> trackSegment in TrackPoints)
            {
                foreach (GPXTrackPoint trackPoint in trackSegment)
                {
					trackPoint.Elevation = elevationModel.GetElevation(trackPoint.Latitude, trackPoint.Longitude);
                }                  
            }
        }

        public static List<GPXTrack> ParseGPX(XmlDocument gpx)
        {
            List<GPXTrack> result = new List<GPXTrack>();

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(gpx.NameTable);
            namespaceManager.AddNamespace("gpx", "http://www.topografix.com/GPX/1/1");

            XmlNodeList trkNodes = gpx.SelectNodes("/gpx:gpx/gpx:trk", namespaceManager);
            foreach (XmlNode trkNode in trkNodes)
            {
                GPXTrack track = new GPXTrack();
                result.Add(track);

                XmlNode metadataNameNode = gpx.SelectSingleNode("/gpx:gpx/gpx:metadata/gpx:name", namespaceManager);
                if (metadataNameNode != null)
                {
                    track.Name = metadataNameNode.InnerText;
                }

                // track name overrides metadata name
                XmlNode trackNameNode = trkNode.SelectSingleNode("./gpx:name", namespaceManager);
                if (trackNameNode != null)
                {
                    track.Name = trackNameNode.InnerText;
                }             

                XmlNode authorNameNode = gpx.SelectSingleNode("/gpx:gpx/gpx:metadata/gpx:author/gpx:name", namespaceManager);
                if (authorNameNode != null)
                {
                    track.Author.Name = authorNameNode.InnerText;
                }

				XmlNode authorEMailNode = gpx.SelectSingleNode("/gpx:gpx/gpx:metadata/gpx:author/gpx:email", namespaceManager);
				if (authorEMailNode != null)
				{
					track.Author.EMail = authorEMailNode.InnerText;
				}

				XmlNode authorLinkNode = gpx.SelectSingleNode("/gpx:gpx/gpx:metadata/gpx:author/gpx:link", namespaceManager);
				if (authorLinkNode != null)
				{
					track.Author.Link = authorLinkNode.InnerText;
				}

                XmlNodeList trksegNodes = trkNode.SelectNodes("./gpx:trkseg", namespaceManager);
                foreach (XmlNode trksegNode in trksegNodes)
                {
                    List<GPXTrackPoint> trackSegment = new List<GPXTrackPoint>();
                    track.TrackPoints.Add(trackSegment);

                    XmlNodeList trkptNodes = trksegNode.SelectNodes("./gpx:trkpt", namespaceManager);
                    foreach (XmlNode trkptNode in trkptNodes)
                    {
                        double lat = Double.Parse(trkptNode.Attributes["lat"].Value);
                        double lon = Double.Parse(trkptNode.Attributes["lon"].Value);

                        GPXTrackPoint trackPoint = new GPXTrackPoint(lat, lon);                                                
                        trackSegment.Add(trackPoint);

                        XmlNode eleNode = trkptNode.SelectSingleNode("./gpx:ele", namespaceManager);
                        if (eleNode != null)
                        {
                            trackPoint.Elevation = Double.Parse(eleNode.InnerText);
                        }
                    }                    
                }               
            }

            return result;
        }
    }
}
