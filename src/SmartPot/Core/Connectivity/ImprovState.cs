namespace SmartPot.Core.Connectivity
{
    /// <summary>
    /// Improv provisioning state
    /// </summary>
    internal enum ImprovState : byte
    {
        /// <summary>
        /// Awaiting authorization via physical interaction.
        /// </summary>
        AuthorizationRequired = 1,

        /// <summary>
        /// Ready to accept credentials.
        /// </summary>
        Authorized = 2,

        /// <summary>
        /// Credentials received, attempt to connect.
        /// </summary>
        Provisioning = 3,

        /// <summary>
        /// Connection successful.
        /// </summary>
        Provisioned = 4
    }
}