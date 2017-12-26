using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerSuperIO.Config;
using ServerSuperIO.DbContext;
using ServerSuperIO.Log;
using ServerSuperIO.Persistence;
using ServerSuperIO.Persistence.Sql;
using ServerSuperIO.OPC.Client.Config;
using ServerSuperIO.Server;
using ServerSuperIO.Data;
using ServerSuperIO.Device;
using ServerSuperIO.Service;
using ServerSuperIO.OPC.Client;
using ServerSuperIO.Service.Common;

namespace ServerSuperIO.Service.OPC.Client
{
    public class OPCClientService : DataSourceService
    {
        private OPCClient _client;

        private OPCClientConfig _config;

        private IDataPersistence _persistence;

        public OPCClientService() : base()
        {
            this.IsAutoStart = true;

            _config = Operation.GetOPCClientConfig();

            foreach(TagServer tagServer in _config.TagServers)
            {
                foreach(TagGroup tagGroup in tagServer.TagGroups)
                {
                    foreach(TagItem tagItem in tagGroup.TagItems)
                    {
                        Tags.Add(ConvertItemToTag(tagItem));
                    }
                }
            }
        }

        private ITag ConvertItemToTag(TagItem tagItem)
        {
            ITag tag = new Tag()
            {
                TagId = tagItem.TagID,
                TagName = tagItem.TagName,
                TagValue = 0.0d,
                Timestamp = DateTime.Now
            };
            return tag;
        }

        private void Client_OPCClientDataChangeEvent(object source, OPCClientDataChangeEventArgs e)
        {
            if (GlobalConfig.Global.PersistenceType == PersistenceType.Xml)
            {
                OnServiceLog("OPC读取数据不支持XML方式持久化");
                return;
            }

            if(_config==null)
            {
                OnServiceLog("配置信息为空");
                return;
            }

            List<ITag> tags = ConvertTagData(e);
            //CrossServerCache.TagCache.AddOrUpdateRange(tags);

            if (_config.OPCClientPersistence)
            {
                PersistenceType pt = GlobalConfig.Global.PersistenceType;
                if (pt == PersistenceType.CoreRT 
                    //|| pt == PersistenceType.Golden 
                    || pt == PersistenceType.eDNA)
                {
                    #region 写入实时数据库
                    IDbContext rdb = null;
                    try
                    {
                        rdb = DbContextPool.Pop();
                        if (rdb != null)
                        {
                            string tableName = "SSIOOpc";
                            rdb.WriteTags(tableName, e.TimeStamps, e.ItemNames, e.ItemValues);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnServiceLog(ex.Message);
                    }
                    finally
                    {
                        if (rdb != null)
                        {
                            DbContextPool.Push(rdb);
                        }
                    }

                    OnServiceLog("OPC Client>>写入实时数据库操作完成。共:"+e.NumItems.ToString ()+" 数据点。");
                    #endregion
                }
                else if (pt==PersistenceType.MySql 
                    || pt==PersistenceType.Oracle 
                    || pt==PersistenceType.SqlServer 
                    || pt==PersistenceType.Sqlite)
                {
                    #region 写到关系数据库
                    if (_persistence == null)
                    {
                        _persistence = DataPersistenceFactory.CreateDataPersistence(pt);
                    }

                   ((BaseSqlPersistence)_persistence).PersistenceData("", tags);

                    OnServiceLog("OPC Client>>写入关系数据库操作完成。共:" + e.NumItems.ToString() + " 数据点。");
                    #endregion
                }
            }
            else
            {
                string context = "OPC Client>>";
                for(int i=0;i<e.NumItems;i++)
                {
                    context += String.Format("序号:{0},时间:{1},标签:{2},值:{3};",i.ToString(),e.TimeStamps[i].ToString("yyyy-MM-dd HH:mm:ss"),e.ItemNames[i],e.ItemValues[i].ToString());
                }

                OnServiceLog(context);
            }
        }

        private List<ITag> ConvertTagData(OPCClientDataChangeEventArgs e)
        {
            List<ITag> listData = new List<ITag>();
            for (int i = 0; i < e.ItemNames.Length; i++)
            {
                //ITag td = new Tag()
                //{
                //    TagId = e.ItemIds[i],
                //    Timestamp = e.TimeStamps[i],
                //    TagName = e.ItemNames[i],
                //    TagValue = Convert.ToDouble(e.ItemValues[i]),
                //    TagDesc = ""
                //};
                
                //---临时保存，并触发订阅事件---//
                ITag tag = Tags.FirstOrDefault(t => t.TagId == e.ItemIds[i]);
                if(tag!=null)
                {
                    tag.Write(Convert.ToDouble(e.ItemValues[i]));
                    listData.Add(tag);
                }
            }
            return listData;
        }

        public override string ServiceKey {
            get { return "OPCClientService"; }
        }
        public override string ServiceName {
            get { return "OPC Client Service"; }
        }
        public override void OnClick()
        {
            OnServiceLog("没有实现单击事件");
        }

        //public override void UpdateDevice(string deviceId, object obj)
        //{
        //    return;
        //}

        //public override void RemoveDevice(string deviceId)
        //{
        //    return;
        //}

        public override void StartService()
        {
            try
            {
                if (_config == null)
                    return;
                
                if (_config.StartOpcClientService)
                {
                    if (_client == null)
                    {
                        _client = new OPCClient(_config);
                        _client.OPCClientDataChangeEvent += Client_OPCClientDataChangeEvent;
                    }

                    //ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncInitialize));

                    _client.Initialize();
                }
            }
            catch(Exception ex)
            {
                OnServiceLog(ex.Message);
            }
        }

        private void AsyncInitialize(object obj)
        {
            _client.Initialize();
        }

        public override void StopService()
        {
            if (_client != null)
            {
                _client.OPCClientDataChangeEvent -= Client_OPCClientDataChangeEvent;
                _client.Close();
            }

            Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();

            IsDisposed = true;
        }

        public override void ServiceConnectorCallback(object obj)
        {
            return;
        }

        public override void ServiceConnectorCallbackError(Exception ex)
        {
            return;
        }

        public override void InternalServerSubscriber(InternalServerPublisherArgs args)
        {
            //throw new NotImplementedException();
        }

        public override void GlobalServerSubscriber(GlobalServerPublisherArgs args)
        {
            //throw new NotImplementedException();
        }

        public override void TagValueSubscriber(TagValuePublishedArgs args)
        {
            //throw new NotImplementedException();
        }
    }
}
