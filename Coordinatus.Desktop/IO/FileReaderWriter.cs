using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO
{


    public class FileWriterBase : IDisposable
    {
        public FileWriterBase(Stream stm)
        {
            this.Writer = new StreamWriter(stm, new UTF8Encoding(false));
        }

        protected StreamWriter Writer;

        protected virtual void BeforeDisposing()
        {
            
        }

        public void Dispose()
        {
            if (Writer == null)
                return;
            BeforeDisposing();
            Writer.Flush();
            Writer.Dispose();
            Writer = null;
            GC.SuppressFinalize(this);
        }
    }
}
