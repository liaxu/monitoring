using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Extractor.Extract
{
    /// <summary>
    /// Handling file extracting and marking.
    /// </summary>
    public class FileTaker
    {
        string _splitPattern;
        IFileGetter _getter;
        IFileFilter _filter;
        FileMarkerManager _marker;
        DateTime _startDate;

        /// <summary>
        /// ctor for file extracting and marking handler
        /// </summary>
        /// <param name="splitPattern">splite file pattern, such as "(/r/n|/n)"</param>
        /// <param name="getter">Used to get directory and file info</param>
        /// <param name="filter">Filter increamentation files and contents</param>
        /// <param name="marker">handle file and content markers, saved in local folder</param>
        /// <param name="startTime">date time threshold used to idenfy whether a file is valid and extractable.</param>
        /// <param name="applyExtractedData"></param>
        public FileTaker(string splitPattern, IFileGetter getter, IFileFilter filter, FileMarkerManager marker, DateTime startTime)
        {
            _getter = getter;
            _filter = filter;
            _marker = marker;
            _startDate = startTime;
            _splitPattern = splitPattern;
        }

        /// <summary>
        /// Extract data by the time when this method called
        /// </summary>
        /// <param name="applyExtractedData">User handled code, return true if apply data transform/saveing succeed.</param>
        /// <param name="destination">Resource base path to be extract from</param>
        /// <param name="searchOption">Determin search files whether loop into subdirectories.</param>
        /// <param name="fileExtention">The file extention which need to transform.</param>
        /// <returns></returns>
        public IEnumerable<string> ExtractOnce(Func<IEnumerable<string>, bool> applyExtractedData, string destination, SearchOption searchOption, string fileExtension = null)
        {
            var allFiles = _getter.GetFilesDetailInfo(destination, searchOption, fileExtension);
            var filesDetail = _filter.FilterIncreamentationFileDetails(allFiles, _startDate, _marker);
            var errors = _filter.GetFileIncreamentationContentThenApplyData(filesDetail, _getter, _marker, _splitPattern, applyExtractedData);

            return errors;
        }

    }

    /// <summary>
    /// The location type of files which to be extract from.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Resources from a ftp server.
        /// </summary>
        FTPServer,

        /// <summary>
        /// Resources from a base folder.
        /// </summary>
        Folder
    }

    public enum IncreamentationType
    {
        /// <summary>
        /// <para>The basic increamentation unit is a writtern file, </para>
        /// <para>the writtern file will not be append/rewrite contents by server.</para>
        /// </summary>
        FileUnitIncreamentation,

        /// <summary>
        /// Server delete the old file, use a new file with the same name instead,
        /// increament content also append in the new file.
        /// </summary>
        FileRewriteIncreamentation,

        /// <summary>
        /// <para>The basic increamentation unit is the contents which will be</para>
        /// <para>writtern into new files or appended into existing files by server.</para>
        /// </summary>
        InFileIncreamentation,
    }

    
}
