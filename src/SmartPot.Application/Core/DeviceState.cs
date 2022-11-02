namespace SmartPot.Application.Core
{
    /// <summary>
    /// 
    /// </summary>
    public enum DeviceState : byte
    {
        /// <summary>
        /// 
        /// </summary>
        AuthorizationRequired = 1,
        
        /// <summary>
        /// 
        /// </summary>
        Authorized = 2,

        /// <summary>
        /// 
        /// </summary>
        Provisioning = 3,

        /// <summary>
        /// 
        /// </summary>
        Provisioned = 4
    }
}