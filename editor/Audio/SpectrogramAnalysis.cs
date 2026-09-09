using System;
using System.Numerics;

internal sealed class SpectrogramAnalysis
{
    public const int WindowSize = 2048;
    public const int FrequencyBands = 1024;
    public const int RowsPerTile = 1024;
    public const double SecondsPerRow = 0.0025;
    public const double TileDuration = RowsPerTile * SecondsPerRow;
    private const int FftSize = WindowSize * 2;
    private const double MaximumFrequency = 16000.0;
    private const double MinimumDecibels = -80.0;

    private readonly Complex[] _left = new Complex[FftSize];
    private readonly Complex[] _right = new Complex[FftSize];
    private readonly double[] _window = new double[WindowSize];
    private readonly int[] _firstBins = new int[FrequencyBands];
    private readonly int[] _lastBins = new int[FrequencyBands];
    private readonly double _normalization;

    public SpectrogramAnalysis(double sampleRate)
    {
        if (!double.IsFinite(sampleRate) || sampleRate <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        var windowSum = 0.0;
        for (var index = 0; index < WindowSize; index++)
        {
            _window[index] = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * index / (WindowSize - 1));
            windowSum += _window[index];
        }

        _normalization = 4.0 / (windowSum * windowSum);
        var maximumFrequency = Math.Min(MaximumFrequency, sampleRate / 2.0);
        for (var band = 0; band < FrequencyBands; band++)
        {
            var low = maximumFrequency * band / FrequencyBands;
            var high = maximumFrequency * (band + 1) / FrequencyBands;
            _firstBins[band] = Math.Clamp((int)Math.Ceiling(low * FftSize / sampleRate), 1, FftSize / 2);
            _lastBins[band] = Math.Clamp((int)Math.Ceiling(high * FftSize / sampleRate) - 1, _firstBins[band], FftSize / 2);
        }
    }

    public static double GetRowCenterTime(int tileIndex, int row)
    {
        if (tileIndex < 0 || row < 0 || row >= RowsPerTile)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Tile and row must identify a valid time slice.");
        }

        // One absolute sample clock prevents time compression and seams between tiles.
        return ((double)tileIndex * RowsPerTile + row + 0.5) * SecondsPerRow;
    }

    public void WriteRow(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<byte> pixels)
    {
        if (left.Length != WindowSize || right.Length != WindowSize || pixels.Length != FrequencyBands * 3)
        {
            throw new ArgumentException("Expected a full stereo FFT window and one RGB frequency row.");
        }

        for (var index = 0; index < WindowSize; index++)
        {
            _left[index] = new Complex(left[index] * _window[index], 0.0);
            _right[index] = new Complex(right[index] * _window[index], 0.0);
        }

        // Zero-padding samples the spectrum more densely without widening the time window.
        Array.Clear(_left, WindowSize, FftSize - WindowSize);
        Array.Clear(_right, WindowSize, FftSize - WindowSize);
        Transform(_left);
        Transform(_right);
        for (var band = 0; band < FrequencyBands; band++)
        {
            var power = 0.0;
            for (var bin = _firstBins[band]; bin <= _lastBins[band]; bin++)
            {
                var leftBin = _left[bin];
                var rightBin = _right[bin];
                var binPower = (leftBin.Real * leftBin.Real + leftBin.Imaginary * leftBin.Imaginary
                    + rightBin.Real * rightBin.Real + rightBin.Imaginary * rightBin.Imaginary) / 2.0;
                power += binPower * _normalization;
            }

            power /= _lastBins[band] - _firstBins[band] + 1;
            var decibels = 10.0 * Math.Log10(Math.Max(power, 1e-12));
            var intensity = Math.Clamp((decibels - MinimumDecibels) / -MinimumDecibels, 0.0, 1.0);
            WriteColor(intensity, pixels.Slice(band * 3, 3));
        }
    }

    private static void WriteColor(double intensity, Span<byte> pixel)
    {
        // Dark navy -> violet -> orange -> yellow, with increasing energy.
        ReadOnlySpan<byte> palette = [4, 6, 18, 40, 24, 100, 155, 40, 110, 240, 100, 35, 255, 240, 120];
        var scaled = intensity * 4.0;
        var stop = Math.Min((int)scaled, 3);
        var blend = scaled - stop;
        for (var channel = 0; channel < 3; channel++)
        {
            var low = palette[stop * 3 + channel];
            var high = palette[(stop + 1) * 3 + channel];
            pixel[channel] = (byte)Math.Round(low + (high - low) * blend);
        }
    }

    private static void Transform(Complex[] samples)
    {
        for (int index = 1, reversed = 0; index < samples.Length; index++)
        {
            var bit = samples.Length >> 1;
            for (; (reversed & bit) != 0; bit >>= 1)
            {
                reversed ^= bit;
            }

            reversed ^= bit;
            if (index < reversed)
            {
                (samples[index], samples[reversed]) = (samples[reversed], samples[index]);
            }
        }

        for (var length = 2; length <= samples.Length; length <<= 1)
        {
            var rotation = Complex.FromPolarCoordinates(1.0, -2.0 * Math.PI / length);
            for (var start = 0; start < samples.Length; start += length)
            {
                var factor = Complex.One;
                for (var offset = 0; offset < length / 2; offset++)
                {
                    var even = samples[start + offset];
                    var odd = samples[start + offset + length / 2] * factor;
                    samples[start + offset] = even + odd;
                    samples[start + offset + length / 2] = even - odd;
                    factor *= rotation;
                }
            }
        }
    }
}
