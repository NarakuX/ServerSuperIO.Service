using ServerSuperIO.Business.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSuperIO.Service.Common
{
    /// <summary>
    /// 转发数据服务
    /// </summary>
    public abstract class DataTransmitService : ServerSuperIO.Service.Service
    {
        public DataTransmitService():base()
        {
            TagConfigs = new List<ServerSuperIO.Business.Model.TagConfig>();
        }

        public IList<ServerSuperIO.Business.Model.TagConfig> TagConfigs { get; }

        public override void Dispose()
        {
            base.Dispose();

            this.TagConfigs.Clear();
        }
    }
}
