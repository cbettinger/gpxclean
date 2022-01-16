namespace gpxclean.gpx
{
	class GPXAuthor
	{
		public string Name { get; internal set; }
        public string EMail { get; internal set; }        
        public string Link { get; internal set; }

		public GPXAuthor() : this(string.Empty, string.Empty, string.Empty) { }

		public GPXAuthor(string name, string eMail, string link)
        {
			Name = name;
			EMail = eMail;
			Link = link;
        }
	}
}
