namespace Sts2Speed.Core.Configuration;

public static class SpeedScaleMath
{
    private const double Epsilon = 0.0001;

    public static bool HasMeaningfulEffect(double multiplier)
    {
        return IsFinite(multiplier) && multiplier > Epsilon && Math.Abs(multiplier - 1.0) >= Epsilon;
    }

    public static float ApplyAnimationSpeedMultiplier(float value, double multiplier)
    {
        if (!IsFinite(multiplier) || multiplier <= Epsilon)
        {
            return value;
        }

        var scaled = value * multiplier;
        if (!IsFinite(scaled))
        {
            return value;
        }

        return scaled < 0f ? 0f : (float)scaled;
    }

    public static float ApplyDurationSpeedMultiplier(float value, double multiplier)
    {
        return (float)ApplyDurationSpeedMultiplier((double)value, multiplier);
    }

    public static double ApplyDurationSpeedMultiplier(double value, double multiplier)
    {
        if (!IsFinite(multiplier) || multiplier <= Epsilon)
        {
            return value;
        }

        var scaled = value / multiplier;
        if (!IsFinite(scaled))
        {
            return value;
        }

        return scaled < 0.0 ? 0.0 : scaled;
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
