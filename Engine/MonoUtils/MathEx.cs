using System;

namespace MonoEngine.MonoUtils
{
    public static class MathFEx
    {
        public const float Deg2Rad = MathF.PI / 180.0f;
        public const float Rad2Deg = 180.0f / MathF.PI;

        public static float Clamp01(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }

        public static float Repeat(float t, float length)
        {
            return Clamp(t - (float)Math.Floor(t / length) * length, 0f, length);
        }

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta)
                return target;
            return current + MathF.Sign(target - current) * maxDelta;
        }

        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = MathF.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            float tempTarget = current - change;
            float temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            float output = tempTarget + (change + temp) * exp;
            if (target - current > 0f == output > target)
            {
                output = target;
                velocity = (output - target) / deltaTime;
            }
            return output;
        }

        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float deltaTime)
        {
            return SmoothDamp(current, target, ref velocity, smoothTime, float.PositiveInfinity, deltaTime);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static float InverseLerp(float a, float b, float value)
        {
            if (a != b)
                return Clamp01((value - a) / (b - a));
            return 0f;
        }

        public static float Remap(float iMin, float iMax, float oMin, float oMax, float value)
        {
            float t = Clamp01((value - iMin) / (iMax - iMin));
            return oMin + (oMax - oMin) * t;
        }

        public static float PingPong(float t, float length)
        {
            t = Repeat(t, length * 2f);
            return length - MathF.Abs(t - length);
        }

        public static float EaseQuadIn(float t)
        {
            return t * t;
        }

        public static float EaseQuadOut(float t)
        {
            return t * (2f - t);
        }

        public static float EaseQuadInOut(float t)
        {
            if (t < 0.5f)
                return 2f * t * t;
            return -1f + (4f - 2f * t) * t;
        }

        public static float EaseCubicIn(float t)
        {
            return t * t * t;
        }

        public static float EaseCubicOut(float t)
        {
            float f = t - 1f;
            return f * f * f + 1f;
        }

        public static float EaseCubicInOut(float t)
        {
            if (t < 0.5f)
                return 4f * t * t * t;
            float f = 2f * t - 2f;
            return 0.5f * f * f * f + 1f;
        }

        public static float EaseSineIn(float t)
        {
            return 1f - MathF.Cos(t * MathF.PI * 0.5f);
        }

        public static float EaseSineOut(float t)
        {
            return MathF.Sin(t * MathF.PI * 0.5f);
        }

        public static float EaseSineInOut(float t)
        {
            return 0.5f * (1f - MathF.Cos(MathF.PI * t));
        }

        public static float EaseBounceIn(float t)
        {
            return 1f - EaseBounceOut(1f - t);
        }

        public static float EaseBounceOut(float t)
        {
            if (t < 1f / 2.75f)
                return 7.5625f * t * t;
            else if (t < 2f / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }
            else if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / 2.75f;
                return 7.5625f * t * t + 0.984375f;
            }
        }

        public static float EaseBounceInOut(float t)
        {
            if (t < 0.5f)
                return EaseBounceIn(t * 2f) * 0.5f;
            return EaseBounceOut(t * 2f - 1f) * 0.5f + 0.5f;
        }

        public static float EaseElasticIn(float t)
        {
            if (t == 0f || t == 1f)
                return t;
            return -MathF.Pow(2f, 10f * (t - 1f)) * MathF.Sin((t - 1.075f) * (2f * MathF.PI) / 0.3f);
        }

        public static float EaseElasticOut(float t)
        {
            if (t == 0f || t == 1f)
                return t;
            return MathF.Pow(2f, -10f * t) * MathF.Sin((t - 0.075f) * (2f * MathF.PI) / 0.3f) + 1f;
        }

        public static float EaseElasticInOut(float t)
        {
            if (t == 0f || t == 1f)
                return t;
            if (t < 0.5f)
                return -0.5f * MathF.Pow(2f, 20f * t - 10f) * MathF.Sin((20f * t - 11.125f) * (2f * MathF.PI) / 0.45f);
            return 0.5f * MathF.Pow(2f, -20f * t + 10f) * MathF.Sin((20f * t - 11.125f) * (2f * MathF.PI) / 0.45f) + 1f;
        }

        public static float Damp(float current, float target, ref float velocity, float smoothTime, float deltaTime)
        {
            return SmoothDamp(current, target, ref velocity, smoothTime, deltaTime);
        }

        public static float AngleLerp(float from, float to, float t)
        {
            float delta = Repeat(to - from, 360f);
            if (delta > 180f) delta -= 360f;
            return from + delta * Clamp01(t);
        }

        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = Repeat(target - current, 360f);
            if (delta > 180f) delta -= 360f;
            if (MathF.Abs(delta) <= maxDelta)
                return target;
            return current + MathF.Sign(delta) * maxDelta;
        }
    }

    public static class MathEx
    {
        public const double Deg2Rad = Math.PI / 180.0f;
        public const double Rad2Deg = 180.0f / Math.PI;

        private static double Clamp01(double value)
        {
            return value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
        }

        private static double Repeat(double t, double length)
        {
            return Clamp(t - Math.Floor(t / length) * length, 0.0, length);
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        public static double MoveTowards(double current, double target, double maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
                return target;
            return current + Math.Sign(target - current) * maxDelta;
        }

        public static double SmoothDamp(double current, double target, ref double velocity, double smoothTime, double maxSpeed, double deltaTime)
        {
            smoothTime = Math.Max(0.0001, smoothTime);
            double omega = 2.0 / smoothTime;
            double x = omega * deltaTime;
            double exp = 1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x);
            double change = current - target;
            double maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            double tempTarget = current - change;
            double temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            double output = tempTarget + (change + temp) * exp;
            if (target - current > 0.0 == output > target)
            {
                output = target;
                velocity = (output - target) / deltaTime;
            }
            return output;
        }

        public static double SmoothDamp(double current, double target, ref double velocity, double smoothTime, double deltaTime)
        {
            return SmoothDamp(current, target, ref velocity, smoothTime, double.PositiveInfinity, deltaTime);
        }

        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static double InverseLerp(double a, double b, double value)
        {
            if (a != b)
                return Clamp01((value - a) / (b - a));
            return 0.0;
        }

        public static double Remap(double iMin, double iMax, double oMin, double oMax, double value)
        {
            double t = Clamp01((value - iMin) / (iMax - iMin));
            return oMin + (oMax - oMin) * t;
        }

        public static double PingPong(double t, double length)
        {
            t = Repeat(t, length * 2.0);
            return length - Math.Abs(t - length);
        }

        public static double EaseQuadIn(double t)
        {
            return t * t;
        }

        public static double EaseQuadOut(double t)
        {
            return t * (2.0 - t);
        }

        public static double EaseQuadInOut(double t)
        {
            if (t < 0.5)
                return 2.0 * t * t;
            return -1.0 + (4.0 - 2.0 * t) * t;
        }

        public static double EaseCubicIn(double t)
        {
            return t * t * t;
        }

        public static double EaseCubicOut(double t)
        {
            double f = t - 1.0;
            return f * f * f + 1.0;
        }

        public static double EaseCubicInOut(double t)
        {
            if (t < 0.5)
                return 4.0 * t * t * t;
            double f = 2.0 * t - 2.0;
            return 0.5 * f * f * f + 1.0;
        }

        public static double EaseSineIn(double t)
        {
            return 1.0 - Math.Cos(t * Math.PI * 0.5);
        }

        public static double EaseSineOut(double t)
        {
            return Math.Sin(t * Math.PI * 0.5);
        }

        public static double EaseSineInOut(double t)
        {
            return 0.5 * (1.0 - Math.Cos(Math.PI * t));
        }

        public static double EaseBounceIn(double t)
        {
            return 1.0 - EaseBounceOut(1.0 - t);
        }

        public static double EaseBounceOut(double t)
        {
            if (t < 1.0 / 2.75)
                return 7.5625 * t * t;
            else if (t < 2.0 / 2.75)
            {
                t -= 1.5 / 2.75;
                return 7.5625 * t * t + 0.75;
            }
            else if (t < 2.5 / 2.75)
            {
                t -= 2.25 / 2.75;
                return 7.5625 * t * t + 0.9375;
            }
            else
            {
                t -= 2.625 / 2.75;
                return 7.5625 * t * t + 0.984375;
            }
        }

        public static double EaseBounceInOut(double t)
        {
            if (t < 0.5)
                return EaseBounceIn(t * 2.0) * 0.5;
            return EaseBounceOut(t * 2.0 - 1.0) * 0.5 + 0.5;
        }

        public static double EaseElasticIn(double t)
        {
            if (t == 0.0 || t == 1.0)
                return t;
            return -Math.Pow(2.0, 10.0 * (t - 1.0)) * Math.Sin((t - 1.075) * (2.0 * Math.PI) / 0.3);
        }

        public static double EaseElasticOut(double t)
        {
            if (t == 0.0 || t == 1.0)
                return t;
            return Math.Pow(2.0, -10.0 * t) * Math.Sin((t - 0.075) * (2.0 * Math.PI) / 0.3) + 1.0;
        }

        public static double EaseElasticInOut(double t)
        {
            if (t == 0.0 || t == 1.0)
                return t;
            if (t < 0.5)
                return -0.5 * Math.Pow(2.0, 20.0 * t - 10.0) * Math.Sin((20.0 * t - 11.125) * (2.0 * Math.PI) / 0.45);
            return 0.5 * Math.Pow(2.0, -20.0 * t + 10.0) * Math.Sin((20.0 * t - 11.125) * (2.0 * Math.PI) / 0.45) + 1.0;
        }

        public static double Damp(double current, double target, ref double velocity, double smoothTime, double deltaTime)
        {
            return SmoothDamp(current, target, ref velocity, smoothTime, deltaTime);
        }

        public static double AngleLerp(double from, double to, double t)
        {
            double delta = Repeat(to - from, 360.0);
            if (delta > 180.0) delta -= 360.0;
            return from + delta * Clamp01(t);
        }

        public static double MoveTowardsAngle(double current, double target, double maxDelta)
        {
            double delta = Repeat(target - current, 360.0);
            if (delta > 180.0) delta -= 360.0;
            if (Math.Abs(delta) <= maxDelta)
                return target;
            return current + Math.Sign(delta) * maxDelta;
        }
    }
}