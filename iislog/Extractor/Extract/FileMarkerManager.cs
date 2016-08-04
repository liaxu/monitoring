using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Extractor.Extract
{
    /// <summary>
    /// Manager for file markers
    /// </summary>
    public class FileMarkerManager
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="name">Manager name which used to load and save.</param>
        private FileMarkerManager(int name)
        {
            this.Name = name;
            this.Content = new Dictionary<string, FileMarker>();
        }

        public FileMarkerManager() { }

        public Dictionary<string, FileMarker> Content { get; set; }

        public int Name { get; set; }

        public void Set(string fullPath, DateTime creationTime, long size, int splitIndex)
        {
            if (Content.ContainsKey(fullPath))
            {
                Content[fullPath] = new FileMarker()
                {
                    CreationTime = creationTime,
                    FullPath = fullPath,
                    Size = size,
                    SplitIndex = splitIndex
                };
            }
            else
            {
                Content.Add(fullPath, new FileMarker()
                {
                    CreationTime = creationTime,
                    FullPath = fullPath,
                    Size = size,
                    SplitIndex = splitIndex
                });
            }
        }

        /// <summary>
        /// Get file split index, return 0 if no record.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public int GetSplitIndex(string fullPath)
        {
            if (Content.ContainsKey(fullPath))
            {
                return Content[fullPath].SplitIndex;
            }

            return 0;
        }

        public static void Save(FileMarkerManager marker)
        {
            if (!Directory.Exists("aaasaves"))
            {
                Directory.CreateDirectory("aaasaves");
            }

            string path = "aaasaves/" + marker.Name;
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (StreamWriter writer = new StreamWriter(
                File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(marker));
            }
        }

        /// <summary>
        /// Load FileMarkerManager from local folder
        /// </summary>
        /// <param name="markerManagerName">Name used to identify saved filename.</param>
        /// <returns></returns>
        public static FileMarkerManager Load(int markerManagerName)
        {
            string path = "aaasaves/" + markerManagerName;
            if (!File.Exists(path))
            {
                return CreateNew(markerManagerName);
            }

            using (StreamReader reader = new StreamReader(File.OpenRead(path)))
            {
                return Newtonsoft.Json.JsonConvert
                    .DeserializeObject<FileMarkerManager>(reader.ReadToEnd());
            }
        }

        public static FileMarkerManager CreateNew(int name)
        {
            return new FileMarkerManager(name);
        }
    }

    public class FileMarker
    {
        /// <summary>
        /// Creation time of the file.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Long Size of the file.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Full path of the file.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// The count of the split pattern met int the file content.
        /// </summary>
        public int SplitIndex { get; set; }
    }

}
