using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Extractor.Extract
{
    /// <summary>
    /// Get files from FTP server
    /// </summary>
    public class FTPFileGetter : IFileGetter
    {
        string _account, _psw;
        public FTPFileGetter(string account, string password)
        {
            _account = account;
            _psw = password;
        }

        /// <summary>
        /// Opening the stream of the target file.
        /// </summary>
        /// <param name="filePath">Full path of teh taget file.</param>
        /// <returns>File stream</returns>
        public Stream DownLoadFile(string filePath)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(filePath);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            request.Credentials = new NetworkCredential(_account, _psw);

            return ((FtpWebResponse)request.GetResponse()).GetResponseStream();
        }

        /// <summary>
        /// Get files Creation timeStamp, size, and path info of the specified destination.
        /// </summary>
        /// <param name="destination">Target site or folder.</param>
        /// <param name="searchOption">Determin search files whether loop into subdirectories.</param>
        /// <param name="fileExtention">The file extention which need to transform.</param>
        /// <returns>List of files with Creation timeStamp, size, and path info.</returns>
        public List<Tuple<DateTime, long, string>> GetFilesDetailInfo(string destination, SearchOption searchOption, string fileExtention = null)
        {
            var listDetail = ListDirectories(destination);
            List<string> dirs;
            var res = SplitDirAndFiles(destination, listDetail, out dirs, fileExtention);

            if (searchOption == SearchOption.AllDirectories)
            {
                foreach (var item in dirs)
                {
                    res.AddRange(GetFilesDetailInfo(Path.Combine(destination, item), SearchOption.AllDirectories, fileExtention));
                }
            }

            return res;
        }

        /// <summary>
        /// Parse FTP ListDirectoryDetails returns, then return directory list and file detail list
        /// </summary>
        /// <param name="listDetail">FTP ListDirectoryDetails returns, which are mixed files and directories.</param>
        /// <param name="dirs">out put directory list.</param>
        /// <param name="fileExtention">file extention to be filtered.</param>
        /// <returns></returns>
        private List<Tuple<DateTime, long, string>> SplitDirAndFiles(string baseSite, IEnumerable<string> listDetail, out List<string> dirs, string fileExtention = null)
        {
            dirs = new List<string>();
            var res = new List<Tuple<DateTime, long, string>>();
            foreach (var item in listDetail)
            {
                var detail = item.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (detail[2] == "<DIR>")
                {
                    detail.ToList().RemoveRange(0, 3);
                    dirs.Add(detail.Aggregate((l, r) => l + " " + r));
                }
                else
                {
                    if (fileExtention == null || item.EndsWith(fileExtention))
                    {
                        res.Add(new Tuple<DateTime, long, string>(
                        Convert.ToDateTime(detail[0] + " " + detail[1]),
                        Convert.ToInt64(detail[2]),
                        Path.Combine(baseSite, detail[3])));
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Get FTP ListDirectoryDetails response, and split by line.
        /// </summary>
        /// <param name="path">FTP server site</param>
        /// <returns>List of directory and file details.</returns>
        private IEnumerable<string> ListDirectories(string path)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(path);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            request.Credentials = new NetworkCredential(_account, _psw);

            var s = "";
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                s = reader.ReadToEnd();
            }

            return s.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    /// <summary>
    /// Windows platform based files getter
    /// </summary>
    public class WindowsFilesGetter : IFileGetter
    {
        public Stream DownLoadFile(string filePath)
        {
            return File.OpenRead(filePath);
        }

        public List<Tuple<DateTime, long, string>> GetFilesDetailInfo(string destination, SearchOption searchOption, string fileExtention = null)
        {
            var files = Directory.GetFiles(destination, fileExtention != null ? "*" + fileExtention : "*", searchOption);
            var res = new List<Tuple<DateTime, long, string>>();
            foreach (var item in files)
            {
                var info = new FileInfo(item);
                res.Add(new Tuple<DateTime, long, string>(
                    info.CreationTime,
                    info.Length,
                    info.FullName));
            }
            return res;
        }
    }

    public interface IFileGetter
    {
        /// <summary>
        /// Get files Creation timeStamp, size, and path info of the specified destination.
        /// </summary>
        /// <param name="destination">Target site or folder.</param>
        /// <param name="searchOption">Determin search files whether loop into subdirectories.</param>
        /// <param name="fileExtention">The file extention which need to transform.</param>
        /// <returns>List of files with Creation timeStamp, size, and path info.</returns>
        List<Tuple<DateTime, long, string>> GetFilesDetailInfo(string destination, SearchOption searchOption, string fileExtention = null);

        /// <summary>
        /// Opening the stream of the target file.
        /// </summary>
        /// <param name="filePath">Full path of teh taget file.</param>
        /// <returns>File stream</returns>
        Stream DownLoadFile(string filePath);
    }
}
