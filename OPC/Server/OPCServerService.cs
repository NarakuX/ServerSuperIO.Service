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
using ServerSuperIO.OPC.Server;
using ServerSuperIO.Base;
using ServerSuperIO.Service.Common;

namespace ServerSuperIO.Service.OPC.Server
{
    public class OPCServerService : DataTransmitService
    {
        private PublicOpcServer _posModel = null;

        private Manager<string,object> _cache = new Manager<string,object>();

        public OPCServerService():base()
        {
            try
            {
                using (PublicOpcServerService poss = PublicOpcServerService.Instance)
                {
                    _posModel = poss.GetPublicOpcServer();
                    if (_posModel != null)
                    {
                        using (PublicTagService ptc = PublicTagService.Instance)
                        {
                            IList<TagConfig> publicTags = ptc.GetPublicTagsOfService(_posModel.Id);
                            foreach(TagConfig tc in publicTags)
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
                return "OPCServerService";
            }
        }

        public override string ServiceName
        {
            get
            {
                return "OPC Server Service";
            }
        }

        public override void StartService()
        {
            if (_posModel == null) return;

            if (_posModel.IsStart)
            {
                if (!OPCServerInstance.IsRunning)
                {
                    OPCServerInstance.Initialize(_posModel.ServerName, _posModel.License);
                    OnServiceLog("启动OPC服务成功");
                }
            }
        }

        public override void StopService()
        {
            
        }

        public override void Dispose()
        {
            _cache.Clear();
            base.Dispose();
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

        public override void TagValueSubscriber(TagValuePublishedArgs args)
        {
            if (_posModel == null || !_posModel.IsStart) return;

            if (!OPCServerInstance.IsRunning)
            {
                OnServiceLog("OPC Server 没有启动");
                return;
            }

            if (TagConfigs.FirstOrDefault(t => t.TagName == args.TagName) != null)
            {
                string key = args.TagName;
                object val = args.NewValue;
                if(!_cache.ContainsKey(key))
                {
                    _cache.TryAdd(key, val);
                    OPCServerInstance.UpdateTag(key, val);
                }
                else
                {
                    object oldValue;
                    if(_cache.TryGetValue(key,out oldValue))
                    {
                        if (val != oldValue)
                        {
                            _cache.TryUpdate(key, val, oldValue);
                            OPCServerInstance.UpdateTag(key, val);
                        }
                    }
                }
            }
        }
    }
}
