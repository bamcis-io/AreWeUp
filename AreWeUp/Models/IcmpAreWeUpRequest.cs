namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// Represents and ICMP (Ping) health check request
    /// </summary>
    public class IcmpAreWeUpRequest : NetworkAreWeUpRequest
    {
        #region Constructors

        /// <summary>
        /// Creates a generic ping health check
        /// </summary>
        public IcmpAreWeUpRequest() : base(Protocol.ICMP) { }

        /// <summary>
        /// Creates an ICMP (Ping) health check using the specifed path and protocolNumber
        /// </summary>
        /// <param name="path">The IP address of DNS host name of the endpoint</param>
        /// <param name="protocolNumber">The protocol number representing the type of ICMP packets to send, use -1 for regular pings</param>
        public IcmpAreWeUpRequest(string path, int protocolNumber = -1) : base(path, protocolNumber, Protocol.ICMP) { }

        #endregion
    }
}
