using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*

 * This is GSI-16 implementation
 * http://www.leica-geosystems.com/media/new/product_solution/gsi_manual.pdf
 
 * 
GSI16 example showing a block sequence of three blocks, containing:
pointnumber (11), horizontal (21) and vertical (22) angle.
 
Example GSI16:
=============
110001+000000000PNC0055 21.002+0000000013384650 22.002+0000000005371500
110002+000000000PNC0056 21.002+0000000012802530 22.002+0000000005255000
110003+000000000PNC0057 21.002+0000000011222360 22.002+0000000005433800
110004+000000000PNC0058 21.002+0000000010573550 22.002+0000000005817600
110005+000000000PNC0059 21.002+0000000009983610 22.002+0000000005171400
-----------------------------------------------------------------------
123456789012345678901234
       |-- 16 char. --|
 

GSI16 Datablock Structure:
=========================
Pos.1-2: Word Index (WI) e.g. “11”; WI code
Pos.3-6: Information related to data e.g. “0002”; number of lines
Pos.7: Sign e.g. + or -
Pos.8-23: GSI16 data (16 digits) e.g. “000000000PNC0058”; Pointnumber
Pos.24: Blank (=separating character)
 
 
 
 
ASCI: Id North East Height
==========================
       D1965   6042802.38   6535026.39        0.000
         219   6042813.86   6535013.01        0.000
         990   6042798.24   6534991.92        0.000

GSI
===
*110000+00000000000D1965 81..10+0000006535026390 82..10+0000006042802380 83..10+0000000000000000 
*110001+0000000000000219 81..10+0000006535013010 82..10+0000006042813860 83..10+0000000000000000 
*110002+0000000000000990 81..10+0000006534991920 82..10+0000006042798240 83..10+0000000000000000 
 
 */


namespace System.IO
{
    /// <summary>
    /// GSI Word Index (WI)
    /// </summary>
    public enum GSIWI
    {
        PtNumber = 11, //Point number, e.g. 11....+00000H66  -> PtNo=“H66“
        StNumber = 16, // Station Pointnumber; PUT/16....+0000A100_<CR/LF> -> puts StNr “A100”
        HzAngle = 21, //Hz Angle, e.g. 21.102+17920860  -> Hz „179.086“ gon
        VAngle = 22, //Vertical Angle, e.g. 22.102+07567500  -> V: „75.675“ gon
        Distance = 31, //Slope distance; e.g. 31..00+00003387  ->  Sdist: „3.387“ m
        HzDistance = 32, //Horizontal distance; e.g. 32..00+00003198  -> Hdist: „3.198“ m
        HeightDifference = 33, //Height Difference; e.g. 33..00+00001119  -> Hdiff: „1.119“ m
        Code = 41, // Code-Block ID PUT/41....+0000TREE_<CR/LF> -> puts code value “TREE”
        PPM_PrismConstant = 51, // PPM and Prism  constant, e.g. 51… .+0220+002  ->  PPM „220“ and Prism const „2“ mm
        PrismConstant = 58, //e.g. 58..16+00000020  ->  Prism „2“ mm
        PPM = 59, //PPM; e.g. 59..16+02200000  ->  PPM „220“
        PtEasting = 81, // Target Easting (E), e.g. 81..00+01999507  ->  E: “1999.507”m
        PtNorthing = 82 , //Target Northing (N) GET/M/WI82; e.g. 82..00-00213159  ->  N: “-2139.159”m
        PtHeight = 83 , //Target Elevation (H); e.g. 83..00+00032881  ->  H: “32.881”m
        StEasting = 84, // Station Easting (E0); e.g. 84..11+00393700  ->  E: “393.700”m
        StNorthing = 85, // Station Northing (N0); e.g. 85..11+06561220  ->  N: “6561.220”m
        StHeight = 86, // Station Height (H0); e.g. 86..11+00065618  ->  H: “65.618”m
        ReflectorHeight = 87, // Reflector height (hr); e.g. 87..11+00001700  ->  hr: “1.700” m
        InstrumentHeight = 88 // Instrument height (hi); e.g. 88..11+00001550  ->  hi: “1.550” m
    }

    public enum GSIUnits
    {        
            Metre1mm = 0, // metre, last place 1mm.
            Metre0_1mm = 6, // metre, last place 0.1mm.
            Metre0_01mm = 8, //metre, last place 0.01mm (only DNA03)
            Foot0_001 = 1, // foot, last place 0.001ft.
            Foot0_0001 = 7 // foot, last place 0.0001ft (only DNA03)
    }

    public enum GSIDataFlag
    {
        Measured = 0, // measured; without earth-curvature correction.
        EnteredManually = 1, // entered manually; without earth-curvature correction.
        MeasuredCorrected = 2, // measured; with earth-curvature correction.
        EnteredManuallyCorrected = 5 // entered manually; with earth-curvature correction.
    }
    
    /// <summary>
    /// GSI16 Datablock
    /// </summary>
    public class GSIWord
    {

        private readonly int DataLength = 16;

        /*
         * 
        The flags at positions 5 to 6 in the data word are used for additional information.
        Example with a GSI-16 data word:
        Position: 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4
                  . . . . . . ± n n n n n n n n n n n n n n n n ×
             
        Position 1-3: Word index.
        Position 4: empty, marked as dot (.)
        Position 5: Information about the measurement and earth-curvature correction.
            0 = measured; without earth-curvature correction.
            1 = entered manually; without earth-curvature correction.
            2 = measured; with earth-curvature correction.
            5 = entered manually; with earth-curvature correction.
        Position 6: Units and decimal places. Note: The data is stored in the unit and resolution that is defined
        by the "Unit" and "Decimal" settings on the instrument during data export.
            6 = metre, last place 0.1mm.
            1 = foot, last place 0.001ft.
            0 = metre, last place 1mm.
            7 = foot, last place 0.0001ft (only DNA03).
            8 = metre, last place 0.01mm (only DNA03).
        Position 7-15: Measurement data (n)
        Position 16: Space character, ASCII-Code 32(×)
         * 
        */


        public GSIWord(GSIWI wi, int info, string data)
        {
            this.WI = wi;
            this.Info = info;
            this.Sign = true;
            this.DataValue = double.NaN;
            this.Data = data ?? "";
            InitText();
        }

        public GSIWord(GSIWI wi, double data)
        {
            // 1: entered manually (GSIDataFlag.EnteredManually)
            // 0: metre, last place 1mm (GSIUnits.Metre1mm)
            this.Info = 10; 

            this.WI = wi;
            this.Sign = data >= 0;
            this.DataValue = data;
            this.Data = Math.Abs(data * 1000).ToString("0");
            InitText();            
        }

        /// <summary>
        /// Word Index (WI), e.g. “11” (WI code)
        /// </summary>
        public GSIWI WI { get; private set; }

        /// <summary>
        /// Information related to data, e.g. “0002” (number of lines)
        /// </summary>
        public int Info { get; private set; }

        /// <summary>
        /// Sign. True == +; False == -;
        /// </summary>
        public bool Sign { get; private set; } 

        /// <summary>
        /// GSI16 data (16 digits), e.g. “000000000PNC0058” (Pointnumber)
        /// </summary>
        public string Data { get; private set; }
        public double DataValue { get; private set; }

        public string Text { get; private set; }

        private void InitText()
        {
            if (this.Info > 9999)
                throw new Exception("Invalid information part of GSI Block: " + this.Info.ToString());

            var infoChars = new String('.', 6).ToCharArray();
            if (this.Info > 0)
                infoChars = this.Info.ToString().PadLeft(6, '.').ToCharArray();       

            var wiChars = Convert.ToInt32(this.WI).ToString();
            if ((wiChars.Length < 2) || (wiChars.Length > 3))
            {
                throw new Exception("Invalid GSI Word Index (WI): " + this.WI.ToString());
            }
            for (int i = 0; i < wiChars.Length; i++)
            {
                infoChars[i] = wiChars[i];
            }


            if (this.Data.Length > DataLength)
            {
                throw new Exception("Invalid GSI data lentth: " + this.Data.Length.ToString() + " (" + this.Data + ")");
            }


            var textChars = new StringBuilder();
            textChars.Append(infoChars);
            if (!this.Sign)
                textChars.Append('-');
            else
                textChars.Append('+');
            textChars.Append(this.Data.PadLeft(DataLength, '0'));
            textChars.Append((char)32);

            this.Text = textChars.ToString();
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class GSIFileWriter : FileWriterBase
    {
        public GSIFileWriter(Stream stm) : base(stm)
        {
        }

        private string BlockTerminator = "\r\n"; // "\r"
        private string WordSeparator = " ";

        public void Add(params GSIWord[] blocks)
        {
            Writer.Write("*");
            foreach (var b in blocks)
            {
                Writer.Write(b.Text);
            }
            Writer.Write(BlockTerminator);
        }

        public void AddPoint(string number, double easting, double northing, double height)
        {
            var idWord = new GSIWord(GSIWI.PtNumber, -1, number);
            var eWord = new GSIWord(GSIWI.PtEasting, easting);
            var nWord = new GSIWord(GSIWI.PtNorthing, northing);
            var hWord = new GSIWord(GSIWI.PtHeight, height);
            Add(idWord, eWord, nWord, hWord);
        }
    }
}
