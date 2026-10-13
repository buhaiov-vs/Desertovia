using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Desertod.Api.Auth;

/// <summary>
/// Serves a single never-expiring key built from configuration, so every instance and every restart
/// signs and reads login cookies with the same key.
/// </summary>
public sealed class ConfiguredKeyRepository : IXmlRepository
{
    public const int MinimumKeyBytes = 32;

    private static readonly DateTimeOffset ActivationDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpirationDate = new(9000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly XElement key;

    public ConfiguredKeyRepository(byte[] masterKey)
    {
        if (masterKey.Length < MinimumKeyBytes)
        {
            throw new ArgumentException($"The key must be at least {MinimumKeyBytes} bytes.", nameof(masterKey));
        }

        var descriptor = new AuthenticatedEncryptorDescriptor(new AuthenticatedEncryptorConfiguration(), new Secret(masterKey))
            .ExportToXml();

        // Payloads carry the key id, so it must stay the same for the same key.
        var idHash = SHA256.HashData([.. Encoding.UTF8.GetBytes("Desertod data protection key id"), .. masterKey]);

        key = new XElement("key",
            new XAttribute("id", new Guid(idHash.AsSpan(0, 16))),
            new XAttribute("version", 1),
            new XElement("creationDate", XmlConvert.ToString(ActivationDate)),
            new XElement("activationDate", XmlConvert.ToString(ActivationDate)),
            new XElement("expirationDate", XmlConvert.ToString(ExpirationDate)),
            new XElement("descriptor",
                new XAttribute("deserializerType", descriptor.DeserializerType.AssemblyQualifiedName!),
                descriptor.SerializedDescriptorElement));
    }

    public IReadOnlyCollection<XElement> GetAllElements() => [new XElement(key)];

    public void StoreElement(XElement element, string friendlyName) =>
        throw new InvalidOperationException(
            "The data protection key comes from configuration (DataProtection:Key) and cannot be added to or revoked at runtime.");
}
