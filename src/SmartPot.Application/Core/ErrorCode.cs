namespace SmartPot.Application.Core
{
    /// <summary>
    /// 
    /// </summary>
    public enum ErrorCode : byte
    {
        /// <summary>
        /// This shows there is no current error state.
        /// </summary>
        NoError,

        /// <summary>
        /// RPC packet was malformed/invalid.
        /// </summary>
        InvalidRpcPacket,

        /// <summary>
        /// The command sent is unknown.
        /// </summary>
        UnknownRpcPacket,

        /// <summary>
        /// The credentials have been received and an attempt to connect to the network has failed.
        /// </summary>
        UnableConnect,

        /// <summary>
        /// Credentials were sent via RPC but the Improv service is not authorized.
        /// </summary>
        NotAuthorized,

        /// <summary>
        /// Unknown error
        /// </summary>
        UnknownError
    }
}