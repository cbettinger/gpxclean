namespace gpxclean.gpx
{
    class GPXTrackPoint
    {
        public double Latitude { get; internal set; }
        public double Longitude { get; internal set; }        
        public double? Elevation { get; internal set; }

        public GPXTrackPoint(double latitude, double longitude, double? elevation = null)
        {
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
        }
    }
}
