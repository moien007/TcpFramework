namespace TcpFramework
{
    /// <summary>
    /// <see cref="TcpService"/> Pools type
    /// </summary>
    public enum TcpServicePoolType
    {
        /// <summary>
        /// Indicates the type of pool that its items will create with its construction. 
        /// This pool type doesn't create any more items when pool count reached,
        /// and consumers have to wait for consumed items to return
        /// </summary>
        Cyclic,

        /// <summary>
        /// Same as <see cref="Cyclic"/> but items will create on demand
        /// </summary>
        DemandCyclic,

        /// <summary>
        /// Indicates the type of pool that its items will create with its construction. 
        /// This pool only pools a limited number of items and when pool count reached,
        /// it creates a new item but will not pool it on its return
        /// </summary>
        Cache,

        /// <summary>
        /// Same as <see cref="Cache"/> but items will create on demand
        /// </summary>
        DemandCache,
    }
}
