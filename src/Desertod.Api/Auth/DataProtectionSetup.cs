using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace Desertod.Api.Auth;

public static class DataProtectionSetup
{
    public static IServiceCollection AddDesertodDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Desertod");

        var configuredKey = configuration["DataProtection:Key"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "DataProtection:Key is not configured; without it everyone is signed out on every restart. "
                    + $"Set it to a random base64 value of at least {ConfiguredKeyRepository.MinimumKeyBytes} bytes.");
            }

            return services;
        }

        byte[] masterKey;
        try
        {
            masterKey = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("DataProtection:Key must be base64.", exception);
        }

        if (masterKey.Length < ConfiguredKeyRepository.MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"DataProtection:Key must decode to at least {ConfiguredKeyRepository.MinimumKeyBytes} bytes; it has {masterKey.Length}.");
        }

        dataProtection.DisableAutomaticKeyGeneration();
        services.Configure<KeyManagementOptions>(options => options.XmlRepository = new ConfiguredKeyRepository(masterKey));
        return services;
    }
}
