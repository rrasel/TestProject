using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace TCPIPListenerServer.Common
{
    public class StateObject : IStateObject
    {
        private const int Buffer_Size = 4096;
        private readonly byte[] buffer = new byte[Buffer_Size];
        private readonly Socket listener;
        private readonly int id;
        private StringBuilder sb;

        public StateObject(Socket listener, int id = -1)
        {
            this.listener = listener;
            this.id = id;
            this.Close = false;
            this.Reset();
        }

        public int BufferSize
        {
            get { return Buffer_Size; }
        }

        public int Id
        {
            get { return id; }
        }

        public bool Close { get; set; }

        public byte[] Buffer
        {
            get { return buffer; }
        }

        public Socket Listener
        {
            get { return listener; }
        }

        public string Text
        {
            get { return sb.ToString(); }
        }

        public void Append(string text)
        {
            sb.Append(text);
        }

        public void Reset()
        {
            sb = new StringBuilder();
        }
    }
}
