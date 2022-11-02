namespace SmartPot.Core.Connectivity
{
    /// <summary>
    /// Defines RPC operation result information.
    /// </summary>
    internal readonly struct RpcResult
    {
        /// <summary>
        /// Gets empty result.
        /// </summary>
        public static readonly RpcResult Empty;

        /// <summary>
        /// Gets RPC command.
        /// </summary>
        public byte Command
        {
            get;
        }

        /// <summary>
        /// Gets RPC command execution status.
        /// </summary>
        public string Status
        {
            get;
        }

        /// <summary>
        /// Initializes new instance of the <see cref="RpcResult" /> class with <paramref name="command" />
        /// and <paramref name="status" /> specified.
        /// </summary>
        /// <param name="command">The executed command.</param>
        /// <param name="status">The executed command status.</param>
        public RpcResult(byte command, string status)
        {
            Command = command;
            Status = status;
        }

        static RpcResult()
        {
            Empty = new RpcResult(0xFF, "");
        }
    }
}