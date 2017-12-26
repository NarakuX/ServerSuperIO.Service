using ServerSuperIO.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerSuperIO.Communicate;
using ServerSuperIO.Common;

namespace ServerSuperIO.Service.Transmit
{
    public class DataTransmitServerProtocol : ProtocolDriver
    {
        public DataTransmitServerProtocol()
        {

        }

        public override bool CheckData(byte[] data)
        {
            return true;
        }

        public override int GetAddress(byte[] data)
        {
            return data[0];
        }

        public override byte[] GetCheckData(byte[] data)
        {
            return null;
        }

        public override string GetCode(byte[] data)
        {
            return String.Empty;
        }

        public override byte[] GetCommand(byte[] data)
        {
            return null;
        }

        public override byte[] GetEnd(byte[] data)
        {
            return null;
        }

        public override byte[] GetHead(byte[] data)
        {
            return null;
        }

        public override int GetPackageLength(byte[] data, IChannel channel, ref int readTimeout)
        {
            return 0;
        }
    }
}
