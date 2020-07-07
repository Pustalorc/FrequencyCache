namespace Pustalorc.Libraries.FrequencyCache.Interfaces
{
    /// <summary>
    /// Represents an Identifiable element with a unique identifier defined as a string.
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// The unique identifier for this object.
        /// </summary>
        string UniqueIdentifier { get; }
    }
}