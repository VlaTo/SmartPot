﻿using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
using nanoFramework.Networking;
using System;
using System.Device.Wifi;
using System.Diagnostics;
using System.Text;

#nullable enable

namespace SmartPot.Core.Connectivity
{
    internal sealed class ImprovManager
    {
        private static readonly Guid ServiceUuid = new("00467768-6228-2272-4663-277478268000");
        private static readonly Guid CurrentStateCharacteristicUuid = new("00467768-6228-2272-4663-277478268001");
        private static readonly Guid ErrorStateCharacteristicUuid = new("00467768-6228-2272-4663-277478268002");
        private static readonly Guid RpcCommandCharacteristicUuid = new("00467768-6228-2272-4663-277478268003");
        private static readonly Guid RpcResultCharacteristicUuid = new("00467768-6228-2272-4663-277478268004");
        private static readonly Guid CapsCharacteristicUuid = new("00467768-6228-2272-4663-277478268005");

        private bool started;

        private RpcResult rpcResult;
        private ImprovState currentState;
        private ImprovError errorState;

        private GattServiceProvider? serviceProvider;
        private GattLocalCharacteristic? currentStateCharacteristic;
        private GattLocalCharacteristic? errorStateCharacteristic;
        private GattLocalCharacteristic? rpcCommandCharacteristic;
        private GattLocalCharacteristic? rpcResultCharacteristic;
        private GattLocalCharacteristic? capsCharacteristic;

        public delegate void OnIdentifyEventDelegate(object sender, EventArgs e);
        public delegate void OnProvisionedEventDelegate(object sender, ProvisionedEventArgs e);
        public delegate void OnProvisioningCompleteEventDelegate(object sender, EventArgs e);

        public event OnIdentifyEventDelegate? OnIdentify;
        public event OnProvisionedEventDelegate? OnProvisioned;
        public event OnProvisioningCompleteEventDelegate? OnProvisioningComplete;

        /// <summary>
        /// Get current state.
        /// </summary>
        public ImprovState CurrentState
        {
            get => currentState;
            private set
            {
                if (value == currentState)
                {
                    return;
                }

                currentState = value;
                
                if (null != currentStateCharacteristic)
                {
                    var buffer = GetByteBuffer((byte)currentState);
                    currentStateCharacteristic.NotifyValue(buffer);
                }
            }
        }

        /// <summary>
        /// Get the current error state.
        /// </summary>
        public ImprovError ErrorState
        {
            get => errorState;
            set
            {
                if (value == errorState)
                {
                    return;
                }

                errorState = value;

                if (null != errorStateCharacteristic)
                {
                    var buffer = GetByteBuffer((byte)errorState);
                    errorStateCharacteristic.NotifyValue(buffer);
                }
            }
        }

        public RpcResult RpcResult
        {
            get => rpcResult;
            set
            {
                rpcResult = value;

                if (null == rpcResultCharacteristic)
                {
                    return;
                }

                if (0 < rpcResultCharacteristic.SubscribedClients.Length)
                {
                    var buffer = SetupRpcResult();
                    rpcResultCharacteristic.NotifyValue(buffer);
                    Debug.WriteLine("Notifying RPC result");
                }
                else
                {
                    Debug.WriteLine("Notify RPC: no clients");
                }
            }
        }

        /// <summary>
        /// Constructor for IMPROV service.
        /// </summary>
        public ImprovManager()
        {
            started = false;
            Initialise();
        }

        /// <summary>
        /// Start the Improv service.
        /// </summary>
        /// <param name="deviceName">Name of device in Bluetooth advert.</param>
        public void Start(string deviceName)
        {
            if (started)
            {
                return;
            }

            if (null != serviceProvider)
            {
                serviceProvider.StartAdvertising(
                    new GattServiceProviderAdvertisingParameters
                    {
                        DeviceName = deviceName,
                        IsConnectable = true,
                        IsDiscoverable = true
                    }
                );
                started = true;
            }
        }

        /// <summary>
        /// Stop the Improv service.
        /// </summary>
        public void Stop()
        {
            if (started && null != serviceProvider)
            {
                serviceProvider.StopAdvertising();
                started = false;
            }
        }

        /// <summary>
        /// Authorise/UnAuthorise the Improv service.
        /// </summary>
        /// <param name="auth">True to Authorise</param>
        public void Authorize(bool auth)
        {
            if (auth)
            {
                if (ImprovState.AuthorizationRequired == CurrentState)
                {
                    Debug.WriteLine("Authorized");
                    CurrentState = ImprovState.Authorized;
                }
            }
            else
            {
                if (ImprovState.Authorized == CurrentState)
                {
                    Debug.WriteLine("Authorization requested");
                    CurrentState = ImprovState.AuthorizationRequired;
                }
            }
        }

        /// <summary>
        /// Connect to the Wifi
        /// </summary>
        /// <param name="ssid">SSID to connect to</param>
        /// <param name="password">password for connection</param>
        /// <returns></returns>
        public bool ConnectWiFi(string ssid, string? password)
        {
            // Make sure we are disconnected before we start connecting otherwise
            // ConnectDhcp will just return success instead of reconnecting.
            var adapter = GetWifiAdapter();

            if (null == adapter)
            {
                return false;
            }

            adapter.Disconnect();

            using var cts = new System.Threading.CancellationTokenSource(30000);
            var success = WifiNetworkHelper.ConnectDhcp(ssid, password, requiresDateTime: true, token: cts.Token);

            Console.WriteLine($"ConnectDHCP exit {success}");

            cts.Cancel();

            return success;
        }

        #region Properties


        #endregion

        /// <summary>
        /// Set up the Bluetooth Characteristics for Improv service.
        /// </summary>
        /// <returns>0 if no error</returns>
        private void Initialise()
        {
            currentState = ImprovState.AuthorizationRequired;
            errorState = ImprovError.NoError;
            rpcResult = RpcResult.Empty;

            var result = GattServiceProvider.Create(ServiceUuid);

            if (BluetoothError.Success != result.Error)
            {
                throw new Exception($"{result.Error}");
            }

            serviceProvider = result.ServiceProvider;

            currentStateCharacteristic = CreateCurrentStateCharacteristicConfig(serviceProvider.Service);
            currentStateCharacteristic.ReadRequested += OnCurrentStateReadRequested;
            
            errorStateCharacteristic = CreateErrorStateCharacteristicConfig(serviceProvider.Service);
            errorStateCharacteristic.ReadRequested += OnErrorStateReadRequested;

            rpcCommandCharacteristic = CreateRpcCommandCharacteristicConfig(serviceProvider.Service);
            rpcCommandCharacteristic.WriteRequested += OnRpcCommandWriteRequested;

            rpcResultCharacteristic = CreateRpcResultStateCharacteristicConfig(serviceProvider.Service);
            rpcResultCharacteristic.ReadRequested += OnRpcResultReadRequested;

            capsCharacteristic = CreateCapabilitiesCharacteristicConfig(serviceProvider.Service);
        }

        private static GattLocalCharacteristic CreateCurrentStateCharacteristicConfig(GattLocalService service)
        {
            var result = service.CreateCharacteristic(
                CurrentStateCharacteristicUuid,
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                    UserDescription = "Current State"
                }
            );

            if (BluetoothError.Success != result.Error)
            {
                // An error occurred.
                throw new Exception($"{result.Error}");
            }

            return result.Characteristic;
        }

        private static GattLocalCharacteristic CreateErrorStateCharacteristicConfig(GattLocalService service)
        {
            var result = service.CreateCharacteristic(
                ErrorStateCharacteristicUuid,
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                    UserDescription = "Error State"
                }
            );

            if (BluetoothError.Success != result.Error)
            {
                // An error occurred.
                throw new Exception($"{result.Error}");
            }

            return result.Characteristic;
        }

        private static GattLocalCharacteristic CreateRpcCommandCharacteristicConfig(GattLocalService service)
        {
            var result = service.CreateCharacteristic(
                RpcCommandCharacteristicUuid,
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write,
                    UserDescription = "RPC command"
                }
            );

            if (BluetoothError.Success != result.Error)
            {
                // An error occurred.
                throw new Exception($"{result.Error}");
            }

            return result.Characteristic;
        }

        private static GattLocalCharacteristic CreateRpcResultStateCharacteristicConfig(GattLocalService service)
        {
            var result = service.CreateCharacteristic(
                RpcResultCharacteristicUuid,
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                    UserDescription = "RPC result"
                }
            );

            if (BluetoothError.Success != result.Error)
            {
                // An error occurred.
                throw new Exception($"{result.Error}");
            }

            return result.Characteristic;
        }

        private static GattLocalCharacteristic CreateCapabilitiesCharacteristicConfig(GattLocalService service)
        {
            var result = service.CreateCharacteristic(
                CapsCharacteristicUuid,
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Read,
                    UserDescription = "Capabilities",
                    StaticValue = GetByteBuffer(0x13)
                }
            );

            if (BluetoothError.Success != result.Error)
            {
                // An error occurred.
                throw new Exception($"{result.Error}");
            }

            return result.Characteristic;
        }

        #region Characteristic event handlers

        /// <summary>
        /// Handler for reading Current State
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCurrentStateReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs e)
        {
            var request = e.GetRequest();

            Debug.WriteLine($"CurrentState_ReadRequested {currentState}");

            request.RespondWithValue(GetByteBuffer((byte)currentState));
        }

        private void OnErrorStateReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs e)
        {
            var request = e.GetRequest();
            
            Debug.WriteLine($"ErrorState_ReadRequested {errorState}");
            
            request.RespondWithValue(GetByteBuffer((byte)errorState));
        }

        private void HandleRequest(GattWriteRequest request)
        {
            // Read data from buffer of required format
            var reader = DataReader.FromBuffer(request.Value);
            var command = reader.ReadByte();
            var length = reader.ReadByte();

            // Do something with received data
            Debug.WriteLine($"Rpc command {command} length:{length}");

            switch (command)
            {
                case RpcCommands.SetWifiSettings:
                {
                    ProvisionDevice(request, command, reader);
                    break;
                }

                case RpcCommands.IdentifyDevice:
                {
                    RaiseOnIdentify(EventArgs.Empty);
                    ErrorState = ImprovError.NoError;

                    break;
                }

                default:
                {
                    ErrorState = ImprovError.UnknownRpcPacket;
                    break;
                }
            }
        }

        private void ProvisionDevice(GattWriteRequest request, byte command, DataReader reader)
        {
            //  Send WiFi settings
            if (ImprovState.Authorized != CurrentState)
            {
                ErrorState = ImprovError.NotAuthorized;
                return;
            }

            var ssid = ReadSsid(reader);
            var password = ReadPassword(reader);
            var checksum = reader.ReadByte();

            // Respond if Write requires response
            if (GattWriteOption.WriteWithResponse == request.Option)
            {
                Debug.WriteLine("Respond to command request");
                request.Respond();
            }
            else
            {
                Debug.WriteLine("No Response to command required");
            }

            Debug.WriteLine($"Rpc Apply Wifi SSID:{ssid} Password:{password ?? "N/A"}");

            // Start provisioning
            ErrorState = ImprovError.NoError;
            CurrentState = ImprovState.Provisioning;

            // User handling provisioning ?
            if (null == OnProvisioned)
            {
                // No OnProvisioned user event so try to automatically connect to wifi
                if (ConnectWiFi(ssid, password))
                {
                    CurrentState = ImprovState.Provisioned;
                }
                else
                {
                    // Unable to connect, go back to authorised state so it can be retried
                    ErrorState = ImprovError.UnableConnect;
                    CurrentState = ImprovState.Authorized;
                }
            }
            else
            {
                // User provisioning, call event
                OnProvisioned.Invoke(this, new ProvisionedEventArgs(ssid, password));

                if (ImprovError.NoError == ErrorState)
                {
                    CurrentState = ImprovState.Provisioned;
                }
            }

            if (ImprovState.Provisioned == CurrentState)
            {
                RpcResult = new RpcResult(command, "success");
                ErrorState = ImprovError.NoError;

                RaiseOnProvisioningComplete(EventArgs.Empty);

                return;
            }

            RpcResult = new RpcResult(command, "unknown");
        }

        private void OnRpcCommandWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs e)
        {
            var request = e.GetRequest();

            Debug.WriteLine("RpcCommand_WriteRequested");

            // Check expected data length
            if (2 > request.Value.Length)
            {
                Debug.WriteLine("Error: RpcCommand length");
                request.RespondWithProtocolError((byte)BluetoothError.NotSupported);

                return;
            }

            HandleRequest(request);

            // Respond if Write requires response
            /*if (GattWriteOption.WriteWithResponse == request.Option)
            {
                request.Respond();
            }*/
        }

        private Buffer SetupRpcResult()
        {
            var dw = new DataWriter();
            var resultBytes = Encoding.UTF8.GetBytes(rpcResult.Status);

            dw.WriteByte(rpcResult.Command);                // command
            dw.WriteByte((byte)(resultBytes.Length + 1));   // data length
            dw.WriteByte((byte)resultBytes.Length);         // status length
            dw.WriteBytes(resultBytes);                     // actual status
            dw.WriteByte(0);                                // checksum

            return dw.DetachBuffer();
        }

        /*private void NotifyRpcResult(byte command)
        {
            // Notify change in value
            if (null != rpcResultCharacteristic)
            {
                Debug.WriteLine($"Notify rpc result:{rpcResult}");
                rpcResultCharacteristic.NotifyValue(SetupRpcResult(command));
            }
        }*/

        private void OnRpcResultReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs e)
        {
            Console.WriteLine($"RpcResult_ReadRequested: command: {rpcResult.Command}, status: {rpcResult.Status}");

            var request = e.GetRequest();
            var buffer = SetupRpcResult();
            
            request.RespondWithValue(buffer);
        }

        #endregion

        private void CheckSum(ref byte cs, byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                cs += b;
            }
        }

        /// <summary>
        /// Get current IP address. Only valid if successfully provisioned and connected
        /// </summary>
        /// <returns>IP address string</returns>
        /*public string GetCurrentIPAddress()
        {
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];

            // get first NI ( Wifi on ESP32 )
            return ni.IPv4Address.ToString();
        }*/

        private void RaiseOnProvisioningComplete(EventArgs e)
        {
            if (null != OnProvisioningComplete)
            {
                // Call OnProvisioningComplete to give user a chance to set the Provisioning URL before we notify result
                OnProvisioningComplete.Invoke(this, e);
            }
        }

        private void RaiseOnIdentify(EventArgs e)
        {
            if (null != OnIdentify)
            {
                OnIdentify.Invoke(this, e);
            }
        }

        private static Buffer GetByteBuffer(byte value) => new Buffer(new[] { value });

        private static string ReadSsid(DataReader reader)
        {
            var length = reader.ReadByte();
            var bssid = new byte[length];

            reader.ReadBytes(bssid);

            return Encoding.UTF8.GetString(bssid, 0, bssid.Length);
        }

        private static string? ReadPassword(DataReader reader)
        {
            var passwordLength = reader.ReadByte();

            if (0 == passwordLength)
            {
                return null;
            }

            var bpassword = new byte[passwordLength];

            reader.ReadBytes(bpassword);

            return Encoding.UTF8.GetString(bpassword, 0, bpassword.Length);
        }

        private static WifiAdapter? GetWifiAdapter()
        {
            var adapters = WifiAdapter.FindAllAdapters();
            return adapters is { Length: > 0 } ? adapters[0] : null;
        }

        #region RpcCommand

        private static class RpcCommands
        {
            public const byte SetWifiSettings = 1;
            public const byte IdentifyDevice = 2;
        }

        #endregion
    }
}
#nullable restore