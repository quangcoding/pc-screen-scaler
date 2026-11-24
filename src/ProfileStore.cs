using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace ScreenProfileSwitcher
{
    [DataContract]
    public class ScreenProfile
    {
        [DataMember(Order = 1)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public string DeviceName { get; set; }

        [DataMember(Order = 4)]
        public int Width { get; set; }

        [DataMember(Order = 5)]
        public int Height { get; set; }

        /// <summary>
        /// 0 means Auto (pick highest supported refresh rate for this resolution).
        /// </summary>
        [DataMember(Order = 6)]
        public int RefreshRate { get; set; }

        [DataMember(Order = 7)]
        public int ScalePercent { get; set; }

        public string ResolutionText
        {
            get { return string.Format("{0} × {1}", Width, Height); }
        }

        public string RefreshText
        {
            get { return RefreshRate > 0 ? string.Format("{0} Hz", RefreshRate) : "Auto Hz"; }
        }

        public string SummaryText
        {
            get
            {
                string dev = string.IsNullOrEmpty(DeviceName) ? "Màn hình chính" : DeviceName.Replace(@"\\.\", "");
                return string.Format("{0} × {1}  •  Scale {2}%  •  {3}  •  {4}", Width, Height, ScalePercent, RefreshText, dev);
            }
        }
    }

    public static class ProfileStore
    {
        private const string FileName = "profiles.json";

        public static string GetStoragePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string localPath = Path.Combine(baseDir, FileName);
            if (CanWriteToDirectory(baseDir))
            {
                return localPath;
            }

            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScreenProfileSwitcher");
            Directory.CreateDirectory(appData);
            return Path.Combine(appData, FileName);
        }

        public static List<ScreenProfile> LoadOrCreateDefaults(string defaultDeviceName)
        {
            string path = GetStoragePath();
            if (File.Exists(path))
            {
                try
                {
                    using (var fs = File.OpenRead(path))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(List<ScreenProfile>));
                        var loaded = serializer.ReadObject(fs) as List<ScreenProfile>;
                        if (loaded != null)
                        {
                            foreach (var p in loaded)
                            {
                                if (string.IsNullOrEmpty(p.Id)) p.Id = Guid.NewGuid().ToString("N");
                                if (p.ScalePercent <= 0) p.ScalePercent = 100;
                            }
                            return loaded;
                        }
                    }
                }
                catch
                {
                    // Fall back to defaults if file is corrupted
                }
            }

            var defaults = CreateDefaultProfiles(defaultDeviceName);
            Save(defaults);
            return defaults;
        }

        public static void Save(List<ScreenProfile> profiles)
        {
            string path = GetStoragePath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var ms = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<ScreenProfile>));
                    serializer.WriteObject(writer, profiles ?? new List<ScreenProfile>());
                    writer.Flush();
                }

                File.WriteAllBytes(path, ms.ToArray());
            }
        }

        public static List<ScreenProfile> CreateDefaultProfiles(string deviceName)
        {
            string dev = string.IsNullOrEmpty(deviceName) ? @"\\.\DISPLAY1" : deviceName;
            return new List<ScreenProfile>
            {
                new ScreenProfile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "2K - Scale 100%",
                    DeviceName = dev,
                    Width = 2560,
                    Height = 1440,
                    RefreshRate = 0,
                    ScalePercent = 100
                },
                new ScreenProfile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "Full HD - Scale 100%",
                    DeviceName = dev,
                    Width = 1920,
                    Height = 1080,
                    RefreshRate = 0,
                    ScalePercent = 100
                },
                new ScreenProfile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "2K - Scale 125%",
                    DeviceName = dev,
                    Width = 2560,
                    Height = 1440,
                    RefreshRate = 0,
                    ScalePercent = 125
                }
            };
        }

        private static bool CanWriteToDirectory(string dir)
        {
            try
            {
                string testFile = Path.Combine(dir, ".write_test_" + Guid.NewGuid().ToString("N"));
                using (File.Create(testFile, 1, FileOptions.DeleteOnClose)) { }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
