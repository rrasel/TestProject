using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using TCPIPListenerServer.SocketListener;
using System.Threading;
using System.Net;

namespace TCPIPListenerServer
{
    public partial class TCPIPServer : Form
    {
        public TCPIPServer()
        {
            InitializeComponent();
        }

        private void TCPIPServer_Load(object sender, EventArgs e)
        {
            txtIPAddress.Text = ConfigurationManager.AppSettings["IPAddress"];
            txtPort.Text = ConfigurationManager.AppSettings["PortNo"];
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        CancellationTokenSource cancel;
        private void btnStart_Click(object sender, EventArgs e)
        {
            txtInfo.Text = string.Empty;
            try
            {
                var ip = IPAddress.Parse(txtIPAddress.Text.Trim());
                var ipEndPoint = new IPEndPoint(ip, 9770);
                cancel = new CancellationTokenSource();
                new Thread(() => AsyncSocketListener.Instance.StartListening(ipEndPoint, cancel)).Start();
                AsyncSocketListener.Instance.MessageReceived -= ClientMessageReceived;
                AsyncSocketListener.Instance.MessageReceived += ClientMessageReceived;
                AsyncSocketListener.Instance.MessageSubmitted -= ServerMessageSubmitted;
                AsyncSocketListener.Instance.MessageSubmitted += ServerMessageSubmitted;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                txtInfo.Text = string.Format("Server start listening at - {0} {1}", txtPort.Text, Environment.NewLine);

            }
            catch { }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                cancel.Cancel();
                AsyncSocketListener.Instance.Dispose();
                txtInfo.Text = string.Format("Server stop listening");
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
            catch { }
        }

        #region Server Code
        void ClientMessageReceived(int id, string msg)
        {
            AsyncSocketListener.Instance.Send(id, string.Format("{0}", msg), false);
            //Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine($"Server: Client. {id}, Message No: {msg}");

            if (txtInfo.InvokeRequired)
            {
                txtInfo.Invoke(new MethodInvoker(delegate { txtInfo.Text += string.Format("Message received from Client[{0}]: {1} {2}", id, msg, Environment.NewLine); }));
            }
        }

        void ServerMessageSubmitted(int id, bool close)
        {
            if (close)
                AsyncSocketListener.Instance.Close(id);
        }
        #endregion

        private void TCPIPServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            cancel.Cancel();
            AsyncSocketListener.Instance.Dispose();
            Environment.Exit(0);
        }
    }
}

