using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{

    public class InstrumentJob
    {

    }

    public class InstrumentSerialPort : SerialPort
    {
        public InstrumentSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
            : base(portName, baudRate, parity, dataBits, stopBits)
        {
            ReadTimeout = 4000;
            WriteTimeout = 1000;
            InstrumentName = "[Loading instrument info...]";
        }

        public virtual IList<InstrumentJob> GetJobs()
        {
            return new List<InstrumentJob>();
        }

        public virtual object DownloadFixedPoints(InstrumentJob job)
        {
            return null;
        }

        public virtual object DownloadMeasuredPoints(InstrumentJob job)
        {
            return null;
        }


        public virtual object DownloadMeasuredHzVDists(InstrumentJob job)
        {
            return null;
        }

        protected readonly int MaxAttemtCount = 9;

        protected string ReadResponse(int attemptNo)
        {
            string resp = "";
            try
            {
                resp = ReadLine();
            }
            catch (TimeoutException)
            {
                if (attemptNo < MaxAttemtCount)
                    return ReadResponse(attemptNo + 1);
                else
                    throw;
            }
            return resp;
        }

        protected virtual string GetResponse(string query, int attemptNo)
        {
            string resp = "";
            WriteLine(query);
            return resp = ReadResponse(1);
        }

        public virtual string GetResponse(string query)
        {
            var resp = GetResponse(query, 1);
            return resp;
        }

        public virtual void Beep()
        { }

        public virtual void Connect()
        { }

        public string InstrumentName { get; protected set; }
        public string InstrumentManufacturer { get; protected set; }

        public override string ToString()
        {
            return InstrumentManufacturer + " " + InstrumentName;
        }
    }

    public class GSISerialPort : InstrumentSerialPort
    {

        public GSISerialPort(string portName)
            : base(portName, 19200, Parity.None, 8, StopBits.One)
        {
            NewLine = "\r\n";
            InstrumentManufacturer = "Leica";
        }

        public string GetErrorMessage(string code)
        {
            switch (code)
            {
                case "@W100":
                    return "Instrument busy Any other device is still interfacing the instrument. Check interfacing priorities.";
                case "@W127":
                    return "Invalid command. The string sent to the TC could not be decoded properly or does not exist. Check the syntax";
                case "@W139":
                    return "EDM error The EDM could not proceed the requested measurement. No or weak signal; Check EDM mode and target.";
                case "@W158":
                    return "One of the instruments sensor corrections could not be assigned. Instrument is not stable or levelled; Tilt is out of range (e.g. when tilt sensor is out of range)";
                case "@E101":
                    return "Value out of range Check parameter range";
                case "@E103":
                    return "Invalid Value No valid value; Check parameter range.";
                case "@E112":
                    return "Battery low Low Battery. Check voltage.";
                case "@E114":
                    return "Invalid command No valid command; check the syntax";
                case "@E117":
                    return "Initialisation error Contact service";
                case "@E119":
                    return "Temperature out of range Refer to manual for temperature range";
                case "@E121":
                    return "Parity error Wrong parity set; check Com-Port settings";
                case "@E122":
                    return "RS232 time-out The instrument was waiting for a response for the last 2 seconds";
                case "@E124":
                    return "RS232 overflow RS232 overflow; check Com-Port settings";
                case "@E151":
                    return "Compensator error Inclination Error; check instrument setup or switch of the compensator";
                case "@E155":
                    return "EDM intensity Weak signal; target is most likely outside the field of view";
                case "@E156":
                    return "EDM system error Contact service";
                case "@E158":
                    return "One of the instruments sensor corrections could not be assigned. Instrument is not stable, not levelled or suffering of vibration. Tilt is out of range (e.g. when tilt sensor is out of range). Level instrument or switch off compensator";
                case "@E190":
                    return "General hardware error Contact service";
                case "@E197":
                    return "Initialization error Contact service";
                default:
                    return this.GetType().Name + " error: " + code;
            }
        }

        protected override string GetResponse(string query, int attemptNo)
        {
            if (query.Length > 100)
            {
                throw new Exception("Leica GSI input query buffer overflow (max. 100 characters)");
            }
            var resp = base.GetResponse(query, attemptNo);

            if ((resp == "@W127") && (attemptNo < MaxAttemtCount))
                return GetResponse(query, attemptNo + 1);
            else
                return resp;
        }


        public override string GetResponse(string query)
        {
            var resp = GetResponse(query, 1);
            if (resp.StartsWith("@"))
            {
                throw new Exception(GetErrorMessage(resp));
            }
            return resp;
        }

        public override void Beep()
        {
            GetResponse("BEEP/0");
        }

        public override void Connect()
        {
            if (!this.IsOpen)
                this.Open();

            var resp = GetResponse("c"); //ClearMeasurement
            if (resp != "?") {
                this.Close();
                throw new Exception(this.GetType().Name+" connection error: " + resp);
            }

            this.GetResponse("SET/137/1"); // GSI-16 format
            var instrResp = this.GetResponse("GET/I/WI13");
            if (!instrResp.StartsWith("*13....+"))
                throw new Exception(this.GetType().Name + " connection error: " + instrResp);

            this.InstrumentName = instrResp.Replace("*13....+", "").TrimStart("0".ToCharArray());

        }

    }
}
