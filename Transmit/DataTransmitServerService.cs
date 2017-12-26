using ServerSuperIO.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerSuperIO.Data;
using ServerSuperIO.Device;
using ServerSuperIO.Business.Model;
using ServerSuperIO.Business.Services;
using ServerSuperIO.Communicate;
using ServerSuperIO.Server;
using ServerSuperIO.Communicate.NET;
using ServerSuperIO.Base;
using System.Threading;

namespace ServerSuperIO.Service.Transmit
{
    public class DataTransmitServerService : DataTransmitService
    {
        private TransmitServer _transmitServer;
        private IServer _server;
        private Manager<string, Tag> _cache;
        private Thread _TransmitThread;

        public DataTransmitServerService():base()
        {
            _cache = new Manager<string, Tag>();

            try
            {
                using (TransmitServerConnectService tscs = TransmitServerConnectService.Instance)
                {
                    tscs.DeleteAllTransmitServerConnect();
                }

                using (TransmitServerService tss = TransmitServerService.Instance)
                {
                    _transmitServer = tss.GetTransmitServer();
                    if (_transmitServer != null)
                    {
                        using (PublicTagService ptc = PublicTagService.Instance)
                        {
                            IList<TagConfig> publicTags = ptc.GetPublicTagsOfService(_transmitServer.Id);
                            foreach (TagConfig tc in publicTags)
                            {
                                TagConfigs.Add(tc);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnServiceLog(ex.Message);
            }
        }

        public override string ServiceKey
        {
            get
            {
                return "DataTransmitServerService";
            }
        }

        public override string ServiceName
        {
            get
            {
                return "Data Transmit Server Service";
            }
        }

        public override void GlobalServerSubscriber(GlobalServerPublisherArgs args)
        {
     
        }

        public override void InternalServerSubscriber(InternalServerPublisherArgs args)
        {
           
        }

        public override void OnClick()
        {
          
        }

        public override void ServiceConnectorCallback(object obj)
        {
           
        }

        public override void ServiceConnectorCallbackError(Exception ex)
        {
           
        }

        public override void StartService()
        {
            string devId = "DataTransmitServerDriver";
            DataTransmitServerDriver dev = new DataTransmitServerDriver();
            dev.DeviceParameter.DeviceName = "转发数据服务驱动";
            dev.DeviceParameter.DeviceAddr = 0;
            dev.DeviceParameter.DeviceID = devId;
            dev.DeviceParameter.DeviceCode = "";
            dev.DeviceDynamic.DeviceID = devId;
            dev.DeviceParameter.NET.RemoteIP = "127.0.0.1";
            dev.DeviceParameter.NET.RemotePort = 9600;
            dev.DeviceParameter.NET.ControllerGroup = "LocalGroup";
            dev.CommunicateType = CommunicateType.NET;
            dev.Initialize(devId);

            _server = new ServerManager().CreateServer(new ServerSuperIO.Config.ServerConfig()
            {
                ServerName = "控制设备服务",
                ListenPort = _transmitServer==null ? 7007:_transmitServer.ListenPort,
                MaxConnects= _transmitServer == null ? 5 : _transmitServer.ConnectNum,
                ComReadTimeout = 1000,
                ComWriteTimeout = 1000,
                NetReceiveTimeout = 5000,
                NetSendTimeout = 5000,
                ControlMode = ControlMode.Singleton,
                SocketMode = SocketMode.Tcp,
                ReceiveDataFliter = false,
                ClearSocketSession = false,
                CheckPackageLength = false,
                CheckSameSocketSession = false,
            });

            _server.SocketConnected += server_SocketConnected;
            _server.SocketClosed += server_SocketClosed;
            _server.Start();

            _server.AddDevice(dev);

            StartTransmit();
        }

        private void StartTransmit()
        {
            if (_TransmitThread == null || !_TransmitThread.IsAlive)
            {
                this._TransmitThread = new Thread(new ThreadStart(RunTransmit))
                {
                    IsBackground = true,
                    Name = "DataTransmitConnectorThread"
                };
                this._TransmitThread.Start();
            }
        }

        private void RunTransmit()
        {
            while(true)
            {
                if (_server.ChannelManager.ChannelCount > 0)
                {
                    try
                    {
                        List<Tag> tagList = new List<Tag>();
                        foreach (TagConfig tagConf in TagConfigs)
                        {
                            Tag tag;
                            if (_cache.TryGetValue(tagConf.TagName, out tag))
                            {
                                tagList.Add((Tag)tag.Clone());
                            }
                        }

                        if (tagList.Count > 0)
                        {
                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(tagList)+Environment.NewLine;
                            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

                            ICollection<IChannel> ios = _server.ChannelManager.GetValues();
                            foreach (IChannel channel in ios)
                            {
                                ((TcpSocketSession)channel).StartSend(data, true, WebSocket.WebSocketFrameType.Text);
                            }

                            OnServiceLog("已经向客户端转发数据");
                        }
                    }
                    catch (Exception ex)
                    {
                        OnServiceLog(ex.Message);
                    }
                }

                Thread.Sleep(_transmitServer == null ? 1000 : _transmitServer.Interval);
            }
        }

        private void StopTransmit()
        {
            if (this._TransmitThread != null && this._TransmitThread.IsAlive)
            {
                if (this._TransmitThread.IsAlive)
                {
                    try
                    {
                        _TransmitThread.Abort();
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void server_SocketClosed(string serverSession, string ip, int port)
        {
            OnServiceLog("远程连接 [" + ip + ":" + port + "]");

            try
            {
                using (TransmitServerConnectService tscs = TransmitServerConnectService.Instance)
                {
                    tscs.DeleteTransmitServerConnect(ip, port);
                }
            }
            catch(Exception ex)
            {
                OnServiceLog(ex.Message);
            }
        }

        private void server_SocketConnected(string serverSession, string ip, int port)
        {
            OnServiceLog("远程断开 [" + ip + ":" + port + "]");

            try
            {
                using (TransmitServerConnectService tscs = TransmitServerConnectService.Instance)
                {
                    tscs.AddTransmitServerConnect(new TransmitServerConnect()
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        RemoteIp = ip,
                        RemotePort = port
                    });
                }
            }
            catch (Exception ex)
            {
                OnServiceLog(ex.Message);
            }
        }

        public override void StopService()
        {
            StopTransmit();
        }

        public override void TagValueSubscriber(TagValuePublishedArgs args)
        {
            if (TagConfigs.Count <= 0) return;

            string key = args.TagName;
            if (!_cache.ContainsKey(key))
            {
                Tag tag = new Tag()
                {
                    Timestamp = args.NewTimestamp,
                    TagId = args.TagId,
                    TagName = args.TagName,
                    TagValue = args.NewValue
                };
                _cache.TryAdd(key, tag);
            }
            else
            {
                Tag oldValue;
                if (_cache.TryGetValue(key, out oldValue))
                {
                    if (oldValue == null)
                    {
                        Tag tag = new Tag()
                        {
                            Timestamp = args.NewTimestamp,
                            TagId = args.TagId,
                            TagName = args.TagName,
                            TagValue = args.NewValue
                        };

                        _cache.TryAdd(key, tag);
                    }
                    else
                    {
                        if (oldValue.TagValue != args.NewValue)
                        {
                            oldValue.Timestamp = args.NewTimestamp;
                            oldValue.TagId = args.TagId;
                            oldValue.TagName = args.TagName;
                            oldValue.TagValue = args.NewValue;

                            //_cache.TryUpdate(key, tag, oldValue);
                        }
                    }
                }
            }
        }

        public override void Dispose()
        {
            _server.SocketConnected -= server_SocketConnected;
            _server.SocketClosed -= server_SocketClosed;
            _server.Stop();
            _server.Dispose();
            base.Dispose();
        }
    }
}
