namespace gpxclean.dem
{
	interface IElevationModel
	{
		int GetElevation(double latitude, double longitude);
	}
}
