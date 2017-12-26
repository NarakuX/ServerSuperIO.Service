using ServerSuperIO.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSuperIO.Service.Transmit
{
    public class TransmitObject
    {
        public TransmitObject()
        {
            Tags = new List<Tag>();
        }

        public string SiteCode { get; set; }

        public List<Tag> Tags { get; set; }
    }
}
