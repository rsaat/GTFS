using System;
using System.Collections.Generic;
using System.Linq;

namespace GTFS.Sptrans.Tool.CustomizeDatabase
{
    public class ShapeCompressed
    {
        private int _latitude;
        private int _longitude;
        private int _distanceTraveled;

        public ShapeCompressed(double latitude,double longitude,double distanceTraveled)
        {
            _latitude = ConvertCoordinateToInteger(latitude);
            _longitude = ConvertCoordinateToInteger(longitude);
            _distanceTraveled = (int)Math.Round(distanceTraveled);
        }

        public ShapeCompressed(byte[] bytes)
        {
            if (sizeof(int) * 3 !=bytes.Length)
            {
                throw new ArgumentException("Wrong number of bytes ShapeCompressed. Bytes sent=" + bytes.Length);
            }

            byte[] latitudeBytes = bytes.Skip(0).Take(4).ToArray();
            byte[] longitudeBytes = bytes.Skip(4).Take(4).ToArray();
            byte[] distanceTraveledBytes = bytes.Skip(8).Take(4).ToArray();

            _latitude = ConvertBytesToInt(latitudeBytes);
            _longitude = ConvertBytesToInt(longitudeBytes);
            _distanceTraveled = ConvertBytesToInt(distanceTraveledBytes);
        }

        public double Latitude
        {
            get { return _latitude/1E5; }
        }

        public double Longitude
        {
            get { return _longitude / 1E5; }
        }

        public double DistanceTraveled
        {
            get { return _distanceTraveled; }
        }

        public byte[] ToBytes()
        {
            var data=new List<byte>();
            data.AddRange(ConvertIntToBytes(_latitude));
            data.AddRange(ConvertIntToBytes(_longitude));
            data.AddRange(ConvertIntToBytes(_distanceTraveled));
            return data.ToArray();
        }

        private byte[] ConvertIntToBytes(int value)
        {
            var bytes = new byte[4];
            bytes[0] = (byte)(value >> 0 & 0xFF);
            bytes[1] = (byte)(value >> 8 & 0xFF);
            bytes[2] = (byte)(value >> 16 & 0xFF);
            bytes[3] = (byte)(value >> 24 & 0xFF);
            return bytes;
        }

        private int ConvertBytesToInt(byte[] buffer)
        {
            int value = (buffer[3] << 24) | (buffer[2] << 16)| (buffer[1] << 8) | buffer[0];
            return value;
        }

        private int ConvertCoordinateToInteger(double coordinate)
        {
            return Convert.ToInt32(Math.Round(coordinate, 5) * 1E5);
        }
    }
}
