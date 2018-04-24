namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// Represents a TCP based health check request
    /// </summary>
    public class TcpAreWeUpRequest : NetworkAreWeUpRequest
    {
        #region Constructors

        /// <summary>
        /// Creates a generic TCP health check
        /// </summary>
        public TcpAreWeUpRequest() : base(Protocol.TCP) { }

        /// <summary>
        /// Creates a TCP based health check using the specified path and port
        /// </summary>
        /// <param name="path">The IP address or DNS host name to check</param>
        /// <param name="port">The port to use</param>
        public TcpAreWeUpRequest(string path, int port) : base(path, port, Protocol.TCP)
        { }

        #endregion
    }
}
