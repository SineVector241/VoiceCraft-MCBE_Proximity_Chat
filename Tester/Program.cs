using NAudio.Wave.Compression;
using NAudio.Wave;
using System.Text;

foreach (var driver in AcmDriver.EnumerateAcmDrivers())
{
    StringBuilder builder = new StringBuilder();
    builder.AppendFormat("Long Name: {0}\r\n", driver.LongName);
    builder.AppendFormat("Short Name: {0}\r\n", driver.ShortName);
    builder.AppendFormat("Driver ID: {0}\r\n", driver.DriverId);
    driver.Open();
    builder.AppendFormat("FormatTags:\r\n");
    foreach (AcmFormatTag formatTag in driver.FormatTags)
    {
        builder.AppendFormat("===========================================\r\n");
        builder.AppendFormat("Format Tag {0}: {1}\r\n", formatTag.FormatTagIndex, formatTag.FormatDescription);
        builder.AppendFormat("   Standard Format Count: {0}\r\n", formatTag.StandardFormatsCount);
        builder.AppendFormat("   Support Flags: {0}\r\n", formatTag.SupportFlags);
        builder.AppendFormat("   Format Tag: {0}, Format Size: {1}\r\n", formatTag.FormatTag, formatTag.FormatSize);
        builder.AppendFormat("   Formats:\r\n");
        foreach (AcmFormat format in driver.GetFormats(formatTag))
        {
            builder.AppendFormat("   ===========================================\r\n");
            builder.AppendFormat("   Format {0}: {1}\r\n", format.FormatIndex, format.FormatDescription);
            builder.AppendFormat("      FormatTag: {0}, Support Flags: {1}\r\n", format.FormatTag, format.SupportFlags);
            builder.AppendFormat("      WaveFormat: {0} {1}Hz Channels: {2} Bits: {3} Block Align: {4}, AverageBytesPerSecond: {5} ({6:0.0} kbps), Extra Size: {7}\r\n",
                format.WaveFormat.Encoding, format.WaveFormat.SampleRate, format.WaveFormat.Channels,
                format.WaveFormat.BitsPerSample, format.WaveFormat.BlockAlign, format.WaveFormat.AverageBytesPerSecond,
                (format.WaveFormat.AverageBytesPerSecond * 8) / 1000.0,
                format.WaveFormat.ExtraSize);
            if (format.WaveFormat is WaveFormatExtraData && format.WaveFormat.ExtraSize > 0)
            {
                WaveFormatExtraData wfed = (WaveFormatExtraData)format.WaveFormat;
                builder.Append("      Extra Bytes:\r\n      ");
                for (int n = 0; n < format.WaveFormat.ExtraSize; n++)
                {
                    builder.AppendFormat("{0:X2} ", wfed.ExtraData[n]);
                }
                builder.Append("\r\n");
            }
        }
    }
    driver.Close();
    Console.WriteLine(builder.ToString());
}