using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerSuperIO.Service.Transmit
{
    public delegate void ChannelSessionCloseHandler(object source, string key);
    public class ChannelSession : IDisposable
    {
        /// <summary>
        /// 设置多长时间后检测网络状态
        /// </summary>
        private byte[] _KeepAliveOptionValues;

        /// <summary>
        /// 设置检测网络状态间隔时间
        /// </summary>
        private byte[] _KeepAliveOptionOutValues;
        public string Key { get; set; }
        public Socket Client { get; set; }

        public event ChannelSessionCloseHandler ChannelSessionClose;

        private bool _IsDisposed = false;

        private object SyncLock = new object();

        private byte[] _receiveBuffer;

        protected void OnChannelSessionClose()
        {
            if (ChannelSessionClose != null)
            {
                lock (SyncLock)
                {
                    if (ChannelSessionClose != null)
                    {
                        ChannelSessionClose(this, Key);
                        Dispose();
                    }
                }
            }
        }

        internal ChannelSession(Socket socket)
        {
            _receiveBuffer = new byte[1024];
            Key = socket.RemoteEndPoint.ToString();
            Client = socket;

            //-------------------初始化心跳检测---------------------//
            uint dummy = 0;
            _KeepAliveOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            _KeepAliveOptionOutValues = new byte[_KeepAliveOptionValues.Length];
            BitConverter.GetBytes((uint)1).CopyTo(_KeepAliveOptionValues, 0);
            BitConverter.GetBytes((uint)(2000)).CopyTo(_KeepAliveOptionValues, Marshal.SizeOf(dummy));

            uint keepAlive = 5000;

            BitConverter.GetBytes((uint)(keepAlive)).CopyTo(_KeepAliveOptionValues, Marshal.SizeOf(dummy) * 2);

            Client.IOControl(IOControlCode.KeepAliveValues, _KeepAliveOptionValues, _KeepAliveOptionOutValues);

            Client.NoDelay = true;
            Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            //----------------------------------------------------//

            Client.ReceiveTimeout = 5000;
            Client.SendTimeout = 5000;
            Client.ReceiveBufferSize = 512;
            Client.SendBufferSize = 2048;

            Client.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), this);
        }

        ~ChannelSession()
        {
            Dispose();
        }

        public void Send(byte[] data)
        {
            try
            {
                this.Client.Send(data, 0, data.Length, SocketFlags.None);
            }
            catch(SocketException)
            {
                OnChannelSessionClose();
            }
        }

        public void SendAsync(byte[] data)
        {
            try
            {
                this.Client.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendAsyncCallback), this);
            }
            catch (SocketException)
            {
                OnChannelSessionClose();
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (this.Client == null) return;

                int num=this.Client.EndReceive(ar);
                if(num<=0)
                {
                    OnChannelSessionClose();
                }
            }
            catch(SocketException)
            {
                OnChannelSessionClose();
            }
        }

        private void SendAsyncCallback(IAsyncResult ar)
        {
            try
            {
                if (this.Client == null) return;

                int num=this.Client.EndSend(ar);
                
                if(num<=0)
                {
                    OnChannelSessionClose();
                }
            }
            catch(SocketException)
            {
                OnChannelSessionClose();
            }
        }

        public void Dispose()
        {
            if (!_IsDisposed)
            {
                Client.Shutdown(SocketShutdown.Both);
                Client.Close();
                Client.Dispose();
                Client = null;
                ChannelSessionClose = null;
                _IsDisposed = true;
            }
        }
    }
}
