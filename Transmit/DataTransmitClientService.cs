using ServerSuperIO.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerSuperIO.Data;
using ServerSuperIO.Device;
using ServerSuperIO.Business.Services;
using ServerSuperIO.Business.Model;
using ServerSuperIO.Base;
using System.Net.Sockets;
using ServerSuperIO.Service.Common;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;

namespace ServerSuperIO.Service.Transmit
{
    public class DataTransmitClientService : DataTransmitService
    {
        private Manager<string, Tag> _cache;
        private ConcurrentDictionary<TransmitClient, IList<TagConfig>> _transmits;
        private ChannelManager _channelManager;
        private ConcurrentDictionary<string, SocketAsyncEventArgs> _connDictionary;//标识正在连接

        private Thread _ConnectorThread;

        private List<Thread> _transmitTasks;

        public DataTransmitClientService():base()
        {
            _cache = new Manager<string, Tag>();
            _transmits = new ConcurrentDictionary<TransmitClient, IList<TagConfig>>();
            _channelManager = new ChannelManager();
            _connDictionary = new ConcurrentDictionary<string, SocketAsyncEventArgs>();
            _transmitTasks = new List<Thread>();

            using (TransmitClientService tcs = TransmitClientService.Instance)
            {
                IList<TransmitClient> tcList = tcs.GetAllTransmitClient();
                if (tcList.Count > 0)
                {
                    using (PublicTagService pts = PublicTagService.Instance)
                    {
                        foreach(TransmitClient tc in tcList)
                        {
                            IList<TagConfig> tagConfigs = pts.GetPublicTagsOfService(tc.Id);

                            foreach (TagConfig tag in tagConfigs)
                            {
                                if (TagConfigs.FirstOrDefault(t => t.Id == tag.Id) == null)
                                {
                                    TagConfigs.Add(tag);
                                }
                            }

                            _transmits.TryAdd(tc, tagConfigs);
                        }
                    }
                }
            }
        }

        public override string ServiceKey
        {
            get
            {
                return "DataTransmitClientService";
            }
        }

        public override string ServiceName
        {
            get
            {
                return "Data Transmit Client Service";
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
            StartConnector();

            StartDataTransmit();
        }

        private void StartConnector()
        {
            if (_ConnectorThread == null || !_ConnectorThread.IsAlive)
            {
                this._ConnectorThread = new Thread(new ThreadStart(RunConnector))
                {
                    IsBackground = true,
                    Name = "DataTransmitConnectorThread"
                };
                this._ConnectorThread.Start();
            }
        }

        private void StopConnector()
        {
            if (this._ConnectorThread != null && this._ConnectorThread.IsAlive)
            {
                if (this._ConnectorThread.IsAlive)
                {
                    try
                    {
                        _ConnectorThread.Abort();
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void StartDataTransmit()
        {
            foreach (KeyValuePair<TransmitClient, IList<TagConfig>> kv in _transmits)
            {
                Action<object> action = DataTransmit;
                Thread thread = new Thread(new ParameterizedThreadStart(DataTransmit));
                thread.IsBackground = true;
                thread.Name = kv.Key.TaskName;
                thread.Start(kv.Key);

                _transmitTasks.Add(thread);
            }
        }

        private void StopDataTransmit()
        {
            foreach(Thread t in _transmitTasks)
            {
                try
                {
                    t.Abort();
                }
                catch
                {

                }
            }
        }

        private void DataTransmit(object obj)
        {
            TransmitClient tc = (TransmitClient)obj;
            while(true)
            {
                string connectKey = String.Format("{0}:{1}", tc.RemoteIp, tc.RemotePort.ToString());
                try
                {
                    ChannelSession session = _channelManager.GetChannel(connectKey);
                    if (session != null)
                    {
                        IList<TagConfig> tagConfigs;
                        if (_transmits.TryGetValue(tc, out tagConfigs))
                        {
                            List<Tag> tagList = new List<Tag>();
                            foreach (TagConfig tagConf in tagConfigs)
                            {
                                Tag tag;
                                if (_cache.TryGetValue(tagConf.TagName, out tag))
                                {
                                    tagList.Add((Tag)tag.Clone());
                                }
                            }

                            if (tagList.Count > 0)
                            {
                                TransmitObject transObj = new TransmitObject();
                                transObj.SiteCode = tc.SiteCode;
                                transObj.Tags.AddRange(tagList);

                                string json = Newtonsoft.Json.JsonConvert.SerializeObject(transObj)+Environment.NewLine;
                                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

                                session.Send(data);

                                OnServiceLog("已经转发数据致>>" + connectKey);
                            }
                        }
                    } 
                }
                catch(Exception ex)
                {
                    OnServiceLog(ex.Message);
                }
                Thread.Sleep(tc.Interval);
            }
        }

        public override void StopService()
        {
            StopConnector();

            StopDataTransmit();
        }

        private void RunConnector()
        {
            while (true)
            {
                if(_transmits.Count<=0)
                {
                    Thread.Sleep(1000);
                }
                
                #region
                foreach(TransmitClient dt in _transmits.Keys)
                {
                    string connectKey = String.Format("{0}:{1}", dt.RemoteIp, dt.RemotePort.ToString());
                    if (!_channelManager.ContainsKey(connectKey))
                    {
                        StartConnect(connectKey,dt.RemoteIp, dt.RemotePort);
                    }
                }
                #endregion
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void StartConnect(string connectKey, string remoteIP, int remotePort)
        {
            if (!_connDictionary.ContainsKey(connectKey))
            {
                SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
                connectEventArgs.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                    ProtocolType.Tcp);
                connectEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
                connectEventArgs.Completed += ConnectSocketAsyncEventArgs_Completed;

                _connDictionary.TryAdd(connectKey, connectEventArgs);

                bool willRaiseEvent = connectEventArgs.AcceptSocket.ConnectAsync(connectEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessConnect(connectEventArgs);
                }
            }
        }

        private void ConnectSocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Connect)
            {
                ProcessConnect(e);
            }
        }

        private void ProcessConnect(SocketAsyncEventArgs connectEventArgs)
        {
            string connectKey = connectEventArgs.RemoteEndPoint.ToString();
            if (connectEventArgs.SocketError == SocketError.Success)
            {
                ChannelSession session = new ChannelSession(connectEventArgs.AcceptSocket);
                session.ChannelSessionClose += Session_ChannelSessionClose;
                _channelManager.AddChannel(session.Key, session);
            }
            else
            {
                ProcessConnectionError(connectEventArgs);
            }

            _connDictionary.TryRemove(connectKey, out connectEventArgs);
        }

        private void Session_ChannelSessionClose(object source, string key)
        {
            if(_channelManager.ContainChannel(key))
            {
                _channelManager.RemoveChannel(key);
            }
        }

        private void ProcessConnectionError(SocketAsyncEventArgs connectEventArgs)
        {
            if (connectEventArgs != null)
            {
                OnServiceLog("Connect Remote "+connectEventArgs.RemoteEndPoint.ToString()+ " Failed");
                connectEventArgs.AcceptSocket.Close();
                connectEventArgs.Dispose();
                connectEventArgs = null;
            }
        }

        public override void TagValueSubscriber(TagValuePublishedArgs args)
        {
            if (_transmits.Count <= 0) return;

            if (TagConfigs.FirstOrDefault(t => t.TagName == args.TagName) != null)
            {
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
                        if(oldValue==null)
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
        }

        public override void Dispose()
        {
            _channelManager.RemoveAllChannel();
            _cache.Clear();
            base.Dispose();
        }
    }
}
