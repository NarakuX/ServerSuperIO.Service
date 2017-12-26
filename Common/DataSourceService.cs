using ServerSuperIO.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSuperIO.Service.Common
{
    /// <summary>
    /// 用于获得数据的服务
    /// </summary>
    public abstract class DataSourceService : ServerSuperIO.Service.Service
    {
        public DataSourceService():base()
        {
            Tags = new List<ITag>();
        }

        public IList<ITag> Tags { get; private set; }

        public override void Dispose()
        {
            base.Dispose();

            foreach (ITag t in this.Tags)
            {
                t.Dispose();
            }
            this.Tags.Clear();
        }
    }
}
