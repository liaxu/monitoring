using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;

namespace Extractor.Extract
{
    /// <summary>
    /// Filter strategy of FileUnitIncreatation type
    /// </summary>
    public class FileUnitIncreatationFilter : IFileFilter
    {
        /// <summary>
        /// Filter our the Increamentation Files details by comparing marker and start time
        /// </summary>
        /// <param name="filesDetail">Files' detail.</param>
        /// <param name="StartTime">date time threshold used to idenfy whether a file is valid and extractable.</param>
        /// <param name="marker">Files marker.</param>
        /// <returns>Extractable files details</returns>
        public IEnumerable<Tuple<DateTime, long, string>> FilterIncreamentationFileDetails(IEnumerable<Tuple<DateTime, long, string>> filesDetail, DateTime StartTime, FileMarkerManager marker)
        {
            var res = new List<Tuple<DateTime, long, string>>();
            foreach (var detail in filesDetail)
            {
                if (!marker.Content.ContainsKey(detail.Item3) && detail.Item1 > StartTime)
                {
                    res.Add(detail);
                }
            }
            return res;
        }

        /// <summary>
        /// Get files' increament contents by comparing the splitIndex, and split them by splitPattern, then apply data.
        /// </summary>
        /// <param name="filesDetail">Files' detail.</param>
        /// <param name="getter">File getter.</param>
        /// <param name="marker">Files marker.</param>
        /// <param name="splitPattern">splitPattern used by regex.</param>
        /// <param name="applyNewData">User handled code, return true if apply succeed.</param>
        /// <returns>Error log for each file.</returns>
        public IEnumerable<String> GetFileIncreamentationContentThenApplyData(IEnumerable<Tuple<DateTime, long, string>> filesDetail, IFileGetter getter, FileMarkerManager marker, string splitPattern, Func<IEnumerable<string>, bool> applyNewData)
        {
            var errors = new List<string>();
            foreach (var detail in filesDetail)
            {
                try
                {
                    var s = string.Empty;
                    using (StreamReader reader = new StreamReader(getter.DownLoadFile(detail.Item3)))
                    {
                        s = reader.ReadToEnd();
                    }

                    var res = Merage(Regex.Split(s, splitPattern), splitPattern);
                    if (applyNewData(res))
                    {
                        // No need to use SplitIndex here
                        //
                        marker.Set(detail.Item3, detail.Item1, detail.Item2, 0);
                    }
                    else
                    {
                        errors.Add(string.Format("Applying error\t{0}", detail.Item3));
                    }
                }
                catch
                {
                    errors.Add(string.Format("Reading error\t{0}", detail.Item3));
                }
                GC.Collect();
            }

            return errors;
        }

        /// <summary>
        /// origin is {"aaa","\r\n","bbb","\n","ccc"} 
        /// and splitPatern is {"(\r\n|\n)"}, 
        /// then result is {"aaa","\r\nbbb","\nccc"}
        /// </summary>
        /// <param name="origin">{"aaa","\r\n","bbb","\n","ccc"}</param>
        /// <param name="splitPattern">{"(\r\n|\n)"}</param>
        /// <returns>{"aaa","\r\nbbb","\nccc"}</returns>
        private List<string> Merage(IEnumerable<string> origin, string splitPattern)
        {
            var res = new List<string>();
            var s = string.Empty;
            Regex x = new Regex(splitPattern);
            foreach (var item in origin)
            {
                if (x.IsMatch(item))
                {
                    if (!string.IsNullOrEmpty(s))
                        res.Add(s);

                    s = item;
                }
                else
                {
                    s += item;
                }
            }

            //TODO: last one could be romove here
            if (!string.IsNullOrEmpty(s))
                res.Add(s);

            return res;
        }
    }

    /// <summary>
    /// Filter strategy of InFileIncreatation type
    /// </summary>
    public class InFileIncreatationFilter : IFileFilter
    {
        /// <summary>
        /// Filter our the Increamentation Files details by comparing marker and start time
        /// </summary>
        /// <param name="filesDetail">Files' detail.</param>
        /// <param name="StartTime">date time threshold used to idenfy whether a file is valid and extractable.</param>
        /// <param name="marker">Files marker.</param>
        /// <returns>Extractable files details</returns>
        public IEnumerable<Tuple<DateTime, long, string>> FilterIncreamentationFileDetails(IEnumerable<Tuple<DateTime, long, string>> filesDetail, DateTime StartTime, FileMarkerManager marker)
        {
            var res = new List<Tuple<DateTime, long, string>>();
            foreach (var detail in filesDetail)
            {
                if (detail.Item1 < StartTime)
                {
                    continue;
                }

                if (!marker.Content.ContainsKey(detail.Item3))
                {
                    res.Add(detail);
                }
                // in file increamentation filter
                //
                else if(detail.Item2 > marker.Content[detail.Item3].Size)
                {
                    res.Add(detail);
                }
            }
            return res;
        }

        /// <summary>
        /// Get files' increament contents by comparing the splitIndex, and split them by splitPattern, then apply data.
        /// </summary>
        /// <param name="filesDetail">Files' detail.</param>
        /// <param name="getter">File getter.</param>
        /// <param name="marker">Files marker.</param>
        /// <param name="splitPattern">splitPattern used by regex.</param>
        /// <param name="applyNewData">User handled code, return true if apply succeed.</param>
        /// <returns>Error log for each file.</returns>
        public IEnumerable<String> GetFileIncreamentationContentThenApplyData(IEnumerable<Tuple<DateTime, long, string>> filesDetail, IFileGetter getter, FileMarkerManager marker, string splitPattern, Func<IEnumerable<string>, bool> applyNewData)
        {
            var errors = new List<string>();
            foreach (var detail in filesDetail)
            {
                try
                {
                    var slist = new List<string>();
                    using (var stream = getter.DownLoadFile(detail.Item3))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            while (reader.EndOfStream == false)
                            {
                                slist.Add(reader.ReadLine());
                            }
                        }
                    }

                    // Comparing with splitindex
                    //
                    var allLines = slist;
                    var splitIdx = allLines.Count;

                    // Counting increatation lines
                    //
                    var count = allLines.Count - marker.GetSplitIndex(detail.Item3);
                    if(count <= 0)
                    {
                        continue;
                    }

                    // get increatation lines
                    //
                    var res = allLines.GetRange(marker.GetSplitIndex(detail.Item3), count);
                    if (applyNewData(res))
                    {
                        marker.Set(detail.Item3, detail.Item1, detail.Item2, splitIdx);
                        FileMarkerManager.Save(marker);
                    }
                    else
                    {
                        errors.Add(string.Format("Applying error\t{0}", detail.Item3));
                    }
                }
                catch(Exception e)
                {
                    errors.Add(string.Format("Reading error\t{0}\t{1}", detail.Item3,e.Message));
                    Log(errors);
                    errors.Clear();
                }

                GC.Collect();
            }

            return errors;
        }

        public static void Log(IEnumerable<string> errors)
        {
            if (!Directory.Exists("aaasaves"))
            {
                Directory.CreateDirectory("aaasaves");
            }

            string path = "aaasaves/errors.log";
            using (StreamWriter writer = new StreamWriter(
                File.Open(path, FileMode.Append, FileAccess.Write)))
            {
                foreach (var line in errors)
                {
                    writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// origin is {"aaa","\r\n","bbb","\n","ccc"} 
        /// and splitPatern is {"(\r\n|\n)"}, 
        /// then result is {"aaa","\r\nbbb","\nccc"}
        /// </summary>
        /// <param name="origin">{"aaa","\r\n","bbb","\n","ccc"}</param>
        /// <param name="splitPattern">{"(\r\n|\n)"}</param>
        /// <returns>{"aaa","\r\nbbb","\nccc"}</returns>
        private List<string> Merage(IEnumerable<string> origin, string splitPattern)
        {
            var res = new List<string>();
            var s = string.Empty;
            Regex x = new Regex(splitPattern);
            foreach (var item in origin)
            {
                if (x.IsMatch(item))
                {
                    if (!string.IsNullOrEmpty(s))
                        res.Add(s);

                    s = item;
                }
                else
                {
                    s += item;
                }
            }

            //Last one will be romove here neturally

            return res;
        }
    }

    /// <summary>
    /// Used to filter out files and contents
    /// </summary>
    public interface IFileFilter
    {
        /// <summary>
        /// Filter our the Increamentation Files details by comparing marker and start time
        /// </summary>
        /// <param name="filesDetail">Files' detail.</param>
        /// <param name="StartTime">date time threshold used to idenfy whether a file is valid and extractable.</param>
        /// <param name="marker">Files marker.</param>
        /// <returns>Extractable files details</returns>
        IEnumerable<Tuple<DateTime, long, string>> FilterIncreamentationFileDetails(IEnumerable<Tuple<DateTime, long, string>> filesDetail, DateTime StartTime, FileMarkerManager marker);

        /// <summary>
        /// Get files' increament contents by comparing the splitIndex, and split them by splitPattern, then apply data.
        /// </summary>
        /// <param name="filesDetail">Files' detail.</param>
        /// <param name="getter">File getter.</param>
        /// <param name="marker">Files marker.</param>
        /// <param name="splitPattern">splitPattern used by regex.</param>
        /// <param name="applyNewData">User handled code, return true if apply succeed.</param>
        /// <returns>Error log for each file.</returns>
        IEnumerable<String> GetFileIncreamentationContentThenApplyData(IEnumerable<Tuple<DateTime, long, string>> filesDetail, IFileGetter getter, FileMarkerManager marker, string splitPattern, Func<IEnumerable<string>, bool> applyNewData);
    }
}
