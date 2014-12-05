using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO
{

    /*
     
DATABASE
	POINTS(PointNo, PointID, East, North, Elevation, Code, Date, CLASS)
		1900001,	"D1965",	6535026.390000,	6042802.380000,	0.000000,	"",		,	FIX;
		1900003,	"990",	6534991.920000,	6042798.240000,	0.000000,	"",		,	FIX;
		1900004,	"1002",	6535047.640000,	6042806.830000,	0.000000,	"",		,	FIX;
	END POINTS
END DATABASE

     */

    public class IDEXFileWriter : FileWriterBase
    {
        public IDEXFileWriter(Stream stm)
            : base(stm)
        {
            Writer.WriteLine("DATABASE");
            Writer.WriteLine("	POINTS(PointNo, PointID, East, North, Elevation, Code, Date, CLASS)");
        }

        private int NextPointNo = 1900001;

        private string ToString(double v)
        {
            var res = v.ToString("0.000000");
            return res.Replace(',', '.');
        }


        // 		1900001,	"D1965",	6535026.390000,	6042802.380000,	0.000000,	"",		,	FIX;
        private string PointLineTemplate = "		{0},	\"{1}\",	{2},	{3},	{4},	\"{5}\",		,	FIX;";
        public void AddPoint(string number, double easting, double northing, double height, string code)
        {
            var s = string.Format(PointLineTemplate, NextPointNo++, number, ToString(easting), ToString(northing), ToString(height), code);
            Writer.WriteLine(s);
        }



        protected override void BeforeDisposing()
        {
            Writer.WriteLine("	END POINTS");
            Writer.WriteLine("END DATABASE");
        }
    }
}
