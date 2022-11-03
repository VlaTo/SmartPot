using System;
using System.Text;

namespace SmartPot.Application.Core
{
    public readonly struct RpcResult
    {
        public static readonly RpcResult Empty;

        public ImprovDevice.RpcCommand Command
        {
            get;
        }

        public string Status
        {
            get;
        }

        public RpcResult(ImprovDevice.RpcCommand command, string status)
        {
            Command = command;
            Status = status;
        }

        static RpcResult()
        {
            Empty = new RpcResult(ImprovDevice.RpcCommand.Unknown, String.Empty);
        }

        public static RpcResult From(byte[] bytes)
        {
            var payload = new PayloadReader(bytes);
            var command = payload.ReadByte();
            var packetLength = payload.ReadByte();
            var status = payload.ReadString(Encoding.UTF8);

            return new RpcResult((ImprovDevice.RpcCommand)command, status);
        }
    }
}