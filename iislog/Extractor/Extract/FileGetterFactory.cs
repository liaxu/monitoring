using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor.Extract
{
    public class FileGetterFactory
    {
        public virtual IFileGetter GetFileGetterByName(string name, params string[] p)
        {
            IFileGetter res;
            switch (name)
            {
                case "Folder":
                    res = new WindowsFilesGetter();
                    break;
                case "FTPServer":
                    res = new FTPFileGetter(p[0], p[1]);
                    break;
                default:
                    throw new ArgumentException(string.Format("could not find specifed getter: {0}", name));
            }

            return res;
        }

        public virtual IFileGetter GetFileGetterByName(ResourceType resourceType, params string[] p)
        {
            return GetFileGetterByName(resourceType.ToString(), p);
        }
    }
}
