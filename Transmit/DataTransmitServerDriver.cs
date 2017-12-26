using ServerSuperIO.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerSuperIO.Communicate;
using ServerSuperIO.Device.Connector;
using ServerSuperIO.Protocol;
using ServerSuperIO.Service.Connector;
using ServerSuperIO.Protocol.Filter;
using ServerSuperIO.Persistence;

namespace ServerSuperIO.Service.Transmit
{
    public class DataTransmitServerDriver : RunDevice
    {
        private DataTransmitServerProtocol _pro;

        public DataTransmitServerDriver()
        {
            _pro = new DataTransmitServerProtocol();
        }

        public override DeviceType DeviceType
        {
            get
            {
                return DeviceType.Common;
            }
        }

        public override string ModelNumber
        {
            get
            {
                return "DataTransmitServerDriver";
            }
        }

        public override IProtocolDriver Protocol
        {
            get
            {
                return _pro;
            }
        }

        public override void ChannelStateChanged(ChannelState channelState)
        {
        }

        public override void Communicate(IResponseInfo info)
        {
        }

        public override void CommunicateError(IResponseInfo info)
        {
        }

        public override void CommunicateInterrupt(IResponseInfo info)
        {
        }

        public override void CommunicateNone()
        {
            
        }

        public override void CommunicateStateChanged(CommunicateState comState)
        {
            
        }

        public override void Delete()
        {
           
        }

        public override void DeviceConnectorCallback(object obj)
        {
           
        }

        public override void DeviceConnectorCallbackError(Exception ex)
        {
          
        }

        public override void Exit()
        {
           
        }

        public override IList<IRequestInfo> GetConstantCommand()
        {
            throw new NotImplementedException();
        }

        public override object GetObject()
        {
            throw new NotImplementedException();
        }

        public override void Initialize(object obj)
        {
        }

        public override IDeviceConnectorCallbackResult RunDeviceConnector(IFromDevice fromDevice, IDeviceToDevice toDevice, AsyncDeviceConnectorCallback asyncCallback)
        {
            throw new NotImplementedException();
        }

        public override IServiceConnectorCallbackResult RunServiceConnector(IFromService fromService, IServiceToDevice toDevice, AsyncServiceConnectorCallback asyncService)
        {
            throw new NotImplementedException();
        }

        public override void ShowContextMenu()
        {
            
        }

        public override void UnknownIO()
        {
         
        }
    }
}
