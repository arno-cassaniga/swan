﻿using System;

namespace Swan.Net.Dns
{
    /// <summary>
    /// An exception thrown when the DNS query fails.
    /// </summary>
    /// <seealso cref="Exception" />
    [Serializable]
    public class DnsQueryException : Exception
    {
        internal DnsQueryException()
            : base()
        {
            // placeholder
        }

        internal DnsQueryException(string message)
            : base(message)
        {
            // placeholder
        }

        internal DnsQueryException(string message, Exception e)
            : base(message, e)
        {
            // placeholder
        }

        internal DnsQueryException(DnsClient.IDnsResponse response)
            : this(response, Format(response))
        {
            // placeholder
        }

        internal DnsQueryException(DnsClient.IDnsResponse response, string message)
            : base(message)
        {
            Response = response;
        }

        internal DnsClient.IDnsResponse? Response { get; }

        private static string Format(DnsClient.IDnsResponse response) => $"Invalid response received with code {response.ResponseCode}";
    }
}