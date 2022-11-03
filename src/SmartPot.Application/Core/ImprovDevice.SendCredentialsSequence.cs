
#nullable enable

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace SmartPot.Application.Core
{
    partial class ImprovDevice
    {
        private void SendCredentialsSequence(string ssid, string? password)
        {
            byte[]? data = null;
            ManualResetEventSlim? completed = null;

            bool CanProceed() => SequenceStage.RpcCompleted != stage && SequenceStage.Failed != stage;

            while (CanProceed())
            {
                switch (stage)
                {
                    case SequenceStage.Connected:
                    {
                        var payload = new PayloadWriter();

                        payload.WriteString(ssid, Encoding.UTF8);
                        payload.WriteString(password, Encoding.UTF8);
                        data = payload.Build(false);

                        stage = SequenceStage.SendRpcRequest;

                        break;
                    }

                    case SequenceStage.SendRpcRequest:
                    {
                        if (null != rpcCommandCharacteristic)
                        {
                            var payload = BuildPayload(RpcCommand.SendCredentials, data);
                            var manualResetEvent = new ManualResetEventSlim();
                            
                            completed = new ManualResetEventSlim();

                            waiters.Push(completed);
                            waiters.Push(manualResetEvent);
                            rpcCommandCharacteristic.SetValue(payload);

                            var success = bluetoothGatt!.WriteCharacteristic(rpcCommandCharacteristic);

                            if (success)
                            {
                                if (manualResetEvent.Wait(TimeSpan.FromSeconds(30.0d)))
                                {
                                    stage = SequenceStage.WaitRpcResult;
                                }
                                else
                                {
                                    Debug.WriteLine("RPC command timed out");
                                    stage = SequenceStage.Failed;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("RPC command send failed");
                                stage = SequenceStage.Failed;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("No RPC command characteristic");
                            stage = SequenceStage.Failed;
                        }

                        break;
                    }

                    case SequenceStage.WaitRpcResult:
                    {
                        if (null != completed)
                        {
                            if (completed.Wait(TimeSpan.FromMinutes(1.0d)))
                            {
                                stage = SequenceStage.RpcCompleted;
                            }
                            else
                            {
                                ;
                            }
                        }
                        else
                        {
                            stage = SequenceStage.Failed;
                        }

                        break;
                    }
                }   
            }

            if (SequenceStage.RpcCompleted == stage)
            {
                stage = SequenceStage.Connected;
            }

            RaiseRpcResultChangedEvent(EventArgs.Empty);
        }

        private static byte[] BuildPayload(RpcCommand command, byte[]? data)
        {
            var payload = new PayloadWriter()
                .WriteByte((byte)command)
                .WriteBytes(data);
            return payload.Build(true);
        }
    }
}

#nullable restore
