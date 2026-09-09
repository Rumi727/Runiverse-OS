using Microsoft.CodeAnalysis;

namespace RuniOS.CodeAnalysis.Generators;

public static class LocationDataExtension
{
    public static LocationData ToLocationData(this Location location) => new LocationData(location);
}