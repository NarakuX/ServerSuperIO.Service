using ServerSuperIO.Business.Model;
using ServerSuperIO.Business.Services;
using ServerSuperIO.OPC.Client;
using ServerSuperIO.OPC.Client.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerSuperIO.DbRepository;

namespace ServerSuperIO.Service.OPC.Client
{
    public class Operation
    {
        public static ServerSuperIO.OPC.Client.Config.OPCClientConfig GetOPCClientConfig()
        {
            OPCClientConfigService dbConfig = null;
            OPCServerConfigService dbServer = null;
            OPCGroupConfigService dbGroup = null;
            TagConfigService dbTag = null;
            try
            {
                dbConfig = OPCClientConfigService.Instance;
                dbServer = OPCServerConfigService.Instance;
                dbGroup = OPCGroupConfigService.Instance;
                dbTag = TagConfigService.Instance;

                ServerSuperIO.OPC.Client.Config.OPCClientConfig config = new ServerSuperIO.OPC.Client.Config.OPCClientConfig();

                Business.Model.OPCClientConfig configModel = dbConfig.GetOPCClientConfig();

                config.StartOpcClientService = configModel.StartOpcClientService;
                config.OPCClientPersistence = configModel.OPCClientPersistence;
                config.OPCClientReadInterval = configModel.OPCClientReadInterval;
                config.OPCClientReadMode = (OPCClientReadMode)Enum.Parse(typeof(OPCClientReadMode), configModel.OPCClientReadMode);

                IList<Business.Model.OPCServerConfig> serverModels = dbServer.GetAllOPCServerConfig();
                foreach (Business.Model.OPCServerConfig mServer in serverModels)
                {
                    TagServer tagServer = new TagServer();
                    tagServer.ServerID = mServer.Id;
                    tagServer.ServerName = mServer.ServerName;
                    tagServer.RemoteIP = mServer.RemoteIP;
                    config.TagServers.Add(tagServer);

                    IList<Business.Model.OPCGroupConfig> groupModels = dbGroup.GetOPCGroupConfigOfServer(tagServer.ServerID);
                    foreach (Business.Model.OPCGroupConfig mGroup in groupModels)
                    {
                        TagGroup tagGroup = new TagGroup();
                        tagGroup.GroupID = mGroup.Id;
                        tagGroup.GroupName = mGroup.GroupName;
                        tagGroup.DeadBand = mGroup.DeadBand;
                        tagGroup.IsActive = mGroup.IsActive;
                        tagGroup.TimeBias = mGroup.TimeBias;
                        tagGroup.UpdateRate = mGroup.UpdateRate;
                        tagServer.TagGroups.Add(tagGroup);

                        IList<Business.Model.TagConfig> tagModels = dbTag.GetTagConfigOfDevice(tagGroup.GroupID);
                        foreach (Business.Model.TagConfig mTag in tagModels)
                        {
                            TagItem tag = new TagItem();
                            tag.TagID = mTag.Id;
                            tag.TagName = mTag.TagName;
                            tag.TagType = mTag.TagDataType;
                            tag.TagRemark = mTag.Note;

                            tagGroup.TagItems.Add(tag);
                        }

                    }
                }

                return config;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (dbConfig != null) dbConfig.Dispose();
                if (dbServer != null) dbConfig.Dispose();
                if (dbGroup != null) dbGroup.Dispose();
                if (dbTag != null) dbTag.Dispose();
            }
        }

        public static void SaveOPCClientConfig(ServerSuperIO.OPC.Client.Config.OPCClientConfig config)
        {
            OPCClientConfigService dbConfig = null;
            OPCServerConfigService dbServer = null;
            OPCGroupConfigService dbGroup = null;
            TagConfigService dbTag = null;
            try
            {
                dbConfig = OPCClientConfigService.Instance;
                dbServer = OPCServerConfigService.Instance;
                dbGroup = OPCGroupConfigService.Instance;
                dbTag = TagConfigService.Instance;

                Business.Model.OPCClientConfig configModel = dbConfig.GetOPCClientConfig();
                if (configModel == null) return;

                configModel.StartOpcClientService = config.StartOpcClientService;
                configModel.OPCClientPersistence = config.OPCClientPersistence;
                configModel.OPCClientReadMode = config.OPCClientReadMode.ToString();
                configModel.OPCClientReadInterval = config.OPCClientReadInterval;
                dbConfig.UpdateOPCClientConfig(configModel);

                #region 更新
                foreach (TagServer tagServer in config.TagServers)
                {
                    OPCServerConfig mServer = dbServer.Repository.Queryable.Where<OPCServerConfig>(t => t.Id == tagServer.ServerID).FirstOrDefault();
                    if (mServer == null)
                    {
                        mServer = new OPCServerConfig();
                        mServer.Id = tagServer.ServerID;
                        mServer.ServerName = tagServer.ServerName;
                        mServer.RemoteIP = tagServer.RemoteIP;
                        dbServer.AddOPCServerConfig(mServer);
                    }
                    else
                    {
                        mServer.ServerName = tagServer.ServerName;
                        mServer.RemoteIP = tagServer.RemoteIP;
                        dbServer.UpdateOPCServerConfig(mServer);
                    }

                    foreach (TagGroup tagGroup in tagServer.TagGroups)
                    {
                        OPCGroupConfig mGroup = dbGroup.Repository.Queryable.Where<OPCGroupConfig>(t => t.Id == tagGroup.GroupID).FirstOrDefault();
                        if (mGroup == null)
                        {
                            mGroup = new OPCGroupConfig();
                            mGroup.Id = tagGroup.GroupID;
                            mGroup.ServerId = tagServer.ServerID;
                            mGroup.GroupName = tagGroup.GroupName;
                            mGroup.DeadBand = tagGroup.DeadBand;
                            mGroup.IsActive = tagGroup.IsActive;
                            mGroup.TimeBias = tagGroup.TimeBias;
                            mGroup.UpdateRate = tagGroup.UpdateRate;
                            dbGroup.AddOPCGroupConfig(mGroup);
                        }
                        else
                        {
                            mGroup.GroupName = tagGroup.GroupName;
                            mGroup.DeadBand = tagGroup.DeadBand;
                            mGroup.IsActive = tagGroup.IsActive;
                            mGroup.TimeBias = tagGroup.TimeBias;
                            mGroup.UpdateRate = tagGroup.UpdateRate;
                            dbGroup.UpdateOPCGroupConfig(mGroup);
                        }

                        foreach (TagItem tagItem in tagGroup.TagItems)
                        {
                            TagConfig mTag = dbTag.Repository.Queryable.Where<TagConfig>(t => t.Id == tagItem.TagID).FirstOrDefault();
                            if (mTag == null)
                            {
                                mTag = new TagConfig();
                                mTag.Id = tagItem.TagID;
                                mTag.DeviceOrGroupId = tagGroup.GroupID;
                                mTag.TagName = tagItem.TagName;
                                mTag.TagDataType = tagItem.TagType;
                                mTag.TagNote = "";
                                mTag.SourceType = "opc";
                                mTag.DefaultValue = 0.0d;
                                mTag.UpLimitValue2 = 0.0d;
                                mTag.UpLimitValue1 = 0.0d;
                                mTag.DownLimitValue1 = 0.0d;
                                mTag.DownLimitValue2 = 0.0d;
                                mTag.MaxValue = 0.0d;
                                mTag.MinValue = 0.0d;
                                mTag.Note = "";
                                mTag.SlaveId = 0;
                                mTag.Function = 0x03;
                                mTag.Address = 1;
                                mTag.Quantity = 1;
                                mTag.Mode = "RTU";
                                dbTag.AddTagConfig(mTag);
                            }
                            else
                            {
                                mTag.TagName = tagItem.TagName;
                                mTag.TagDataType = tagItem.TagType;
                                dbTag.UpdateTagConfig(mTag);
                            }
                        }
                    }
                }
                #endregion

                #region 删除
                IList<OPCServerConfig> serverList = dbServer.GetAllOPCServerConfig();
                foreach (OPCServerConfig osc in serverList)
                {
                    TagServer tagServer = config.TagServers.FirstOrDefault(s => s.ServerID == osc.Id);
                    if (tagServer == null)
                    {
                        dbServer.DeleteOPCServerConfig(osc.Id);

                        IList<OPCGroupConfig> groupList = dbGroup.GetAllOPCGroupConfig();
                        foreach (OPCGroupConfig ogc in groupList)
                        {
                            dbGroup.DeleteOPCGroupConfig(ogc.Id);

                            dbTag.DeleteTagConfigOfDevice(ogc.Id);
                        }
                    }
                    else
                    {
                        IList<OPCGroupConfig> groupList = dbGroup.GetOPCGroupConfigOfServer(osc.Id);
                        foreach (OPCGroupConfig ogc in groupList)
                        {
                            TagGroup tagGroup = tagServer.TagGroups.FirstOrDefault(g => g.GroupID == ogc.Id);
                            if (tagGroup == null)
                            {
                                dbGroup.DeleteOPCGroupConfig(ogc.Id);

                                dbTag.DeleteTagConfigOfDevice(ogc.Id);
                            }
                            else
                            {
                                IList<TagConfig> tagList = dbTag.GetTagConfigOfDevice(ogc.Id);
                                foreach (TagConfig tc in tagList)
                                {
                                    TagItem tagItem = tagGroup.TagItems.FirstOrDefault(t => t.TagID == tc.Id);
                                    if (tagItem == null)
                                    {
                                        dbTag.DeleteTagConfig(tc.Id);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            catch
            {
                throw;
            }
            finally
            {
                if (dbConfig != null) dbConfig.Dispose();
                if (dbServer != null) dbConfig.Dispose();
                if (dbGroup != null) dbGroup.Dispose();
                if (dbTag != null) dbTag.Dispose();
            }
        }
    }
}
