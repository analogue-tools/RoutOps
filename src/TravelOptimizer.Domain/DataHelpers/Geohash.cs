namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>
/// Minimal geohash encoder. Precision-7 cells (~150m) are coarse enough to pool corridor samples
/// but fine enough to be meaningful (spec §2 Layer 1). CorridorKey = "{from7}:{to7}".
/// </summary>
public static class Geohash
{
    private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

    public static string Encode(double lat, double lng, int precision = 7)
    {
        double latMin = -90, latMax = 90, lngMin = -180, lngMax = 180;
        var geohash = new System.Text.StringBuilder(precision);
        bool even = true;
        int bit = 0, ch = 0;

        while (geohash.Length < precision)
        {
            if (even)
            {
                double mid = (lngMin + lngMax) / 2;
                if (lng >= mid) { ch = (ch << 1) | 1; lngMin = mid; }
                else { ch <<= 1; lngMax = mid; }
            }
            else
            {
                double mid = (latMin + latMax) / 2;
                if (lat >= mid) { ch = (ch << 1) | 1; latMin = mid; }
                else { ch <<= 1; latMax = mid; }
            }

            even = !even;
            if (++bit == 5)
            {
                geohash.Append(Base32[ch]);
                bit = 0;
                ch = 0;
            }
        }

        return geohash.ToString();
    }

    public static string CorridorKey(double fromLat, double fromLng, double toLat, double toLng) =>
        $"{Encode(fromLat, fromLng)}:{Encode(toLat, toLng)}";
}
