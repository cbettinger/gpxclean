using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace gpxclean.dem
{
    class SRTMElevationModel : IElevationModel
    {
		private string _FileFolder = string.Empty;
		
		public string FileFolder
		{
			get { return _FileFolder; }
			set 
			{
				if (value.Length > 0 && !value.EndsWith(Path.DirectorySeparatorChar.ToString()))
				{
					value += Path.DirectorySeparatorChar;
				}
				_FileFolder = value;
			}
		}

		private Dictionary<string, BinaryReader> openFiles = new Dictionary<string, BinaryReader>();
        private Dictionary<string, SRTMResolution> openFileResolutions = new Dictionary<string, SRTMResolution>();

		public SRTMElevationModel() : this(string.Empty) { }

		public SRTMElevationModel(string fileFolder)
		{
			FileFolder = fileFolder;
			
			// close all opened files on process exit
			AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e)
			{
				foreach (BinaryReader reader in openFiles.Values)
				{
					reader.Close();
				}
			};
		}
		
		public int GetElevation(double latitude, double longitude)
        {        
	        // integer part of coordinates
	        int latDeg = Convert.ToInt32(Math.Abs(Math.Floor(latitude)));
	        int lonDeg = Convert.ToInt32(Math.Abs(Math.Floor(longitude)));
	
	        // build SRTM file name	
            string latStr = (latitude >= 0 ? "N" : "S") + latDeg.ToString("D2");
            string lonStr = (longitude >= 0 ? "E" : "W") + lonDeg.ToString("D3");

            string fileName = String.Format("{0}{1}.hgt", latStr, lonStr);
	
	        // fractional part in arc seconds 
	        double latSec = 3600 * (Math.Abs(latitude) - latDeg);
	        double lonSec = 3600 * (Math.Abs(longitude) - lonDeg);

            return ReadFile(FileFolder + fileName, latSec, lonSec);
        }

        private int ReadFile(string filename, double secLat, double secLon)
        {
            const int SRTM_NO_ELEVATION = -32768;
            const int SRTM_BYTES_PER_VALUE = 2;           
            
            const long SRTM1_FILE_SIZE = 3601 * 3601 * 2;
            const long SRTM3_FILE_SIZE = 1201 * 1201 * 2;            

            BinaryReader reader = null;

            SRTMResolution resolution = SRTMResolution.UNKNOWN;
	
	        // open file if necessary and add to pool
            if (!openFiles.ContainsKey(filename))
            {
                try
                {
                    reader = new BinaryReader(new FileStream(filename, FileMode.Open));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to read {0}: {1}", filename, e.ToString());
                    return SRTM_NO_ELEVATION; 
                }                
                                
                // determine resolution of file
                long fileSize = new FileInfo(filename).Length;

                switch (fileSize)
                {
                    case SRTM1_FILE_SIZE:
                        resolution = SRTMResolution.SRTM1;
                        break;
                    case SRTM3_FILE_SIZE:
                        resolution = SRTMResolution.SRTM3;
                        break;
                    default:
                        Console.WriteLine("Unable to read {0}: {1}", filename, "Unknown SRTM file format.");
                        return SRTM_NO_ELEVATION; 
                }

                openFiles.Add(filename, reader);
                openFileResolutions.Add(filename, resolution);
            }
            else
            {
                reader = openFiles[filename];
                resolution = openFileResolutions[filename];
            }
	
	        // calculate col and row index [1;1201] within SRTM elevation matrix
            int matrixLength = (3600 / (int)resolution) + 1;         

            int col = Convert.ToInt32(Math.Round(secLon / (int)resolution)) + 1;
			int row = matrixLength - Convert.ToInt32(Math.Round(secLat / (int)resolution));	

	        // read elevation value
            try
            {
                reader.BaseStream.Seek(((row - 1) * matrixLength + (col - 1)) * SRTM_BYTES_PER_VALUE, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to read {0}: {1}", filename, e.ToString());
                return SRTM_NO_ELEVATION;
            }

			// read 16 bit int, big endian			
			int elevation = reader.ReadByte();
			elevation = elevation << 8;  
			elevation += reader.ReadByte();

            return elevation;
        }
    }
}
