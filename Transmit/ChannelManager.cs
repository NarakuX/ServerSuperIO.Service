using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerSuperIO.Base;
using ServerSuperIO.Communicate.COM;
using ServerSuperIO.Communicate.NET;
using ServerSuperIO.Log;

namespace ServerSuperIO.Service.Transmit
{
    public class ChannelManager : Manager<string, ChannelSession>
    {
        private object _SyncLock;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChannelManager()
        {
            _SyncLock=new object();
        }

        /// <summary>
        /// 同步对象
        /// </summary>
        public object SyncLock
        {
            get { return _SyncLock; }
        }

        /// <summary>
        /// 增加通道
        /// </summary>
        /// <param name="key"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public bool AddChannel(string key, ChannelSession channel)
        {
            return this.TryAdd(key, channel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ChannelSession GetChannel(string key)
        {
            if (this.ContainsKey(key))
            {
                ChannelSession val;
                if (this.TryGetValue(key, out val))
                {
                    return val;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获得关键字
        /// </summary>
        /// <returns></returns>
        public ICollection<string> GetKeys()
        {
            return this.Keys;
        }

        /// <summary>
        /// 是否包含
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainChannel(string key)
        {
            return this.ContainsKey(key);
        }

        /// <summary>
        /// 删除通道
        /// </summary>
        /// <param name="key"></param>
        public bool RemoveChannel(string key)
        {
            ChannelSession channel;
            if (this.TryRemove(key, out channel))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 删除所有通道
        /// </summary>
        public void RemoveAllChannel()
        {
            Parallel.ForEach(this, channel =>
            {
                try
                {
                    channel.Value.Dispose();
                }
                catch 
                {
                }
            });

            this.Clear();
        }


        public int ChannelCount
        {
            get { return this.Count; }
        }
    }
}
