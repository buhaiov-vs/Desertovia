using System.Net;
using System.Net.Sockets;

namespace Desertod.Api.Auth;

public static class ClientAddress
{
    /// <summary>
    /// The visitor's IPv4 address, or the /64 network of an IPv6 address: one household usually gets a whole /64
    /// and can switch addresses inside it at will. Behind the reverse proxy this relies on forwarded headers.
    /// </summary>
    public static string? Of(HttpContext http)
    {
        var address = http.Connection.RemoteIpAddress;
        if (address is null)
        {
            return null;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return $"{new IPAddress(bytes)}/64";
    }
}
