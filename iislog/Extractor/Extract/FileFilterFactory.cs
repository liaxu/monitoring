using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor.Extract
{
    public class FileFilterFactory
    {
        public virtual IFileFilter GetFileFilterByName(string name, params string[] p)
        {
            IFileFilter res;
            switch (name)
            {
                case "FileUnitIncreamentation":
                    res = new FileUnitIncreatationFilter();
                    break;
                case "FileRewriteIncreamentation":
                case "InFileIncreamentation":
                    res = new InFileIncreatationFilter();
                    break;
                default:
                    throw new ArgumentException(string.Format("could not find specifed filter: {0}", name));
            }

            return res;
        }

        public virtual IFileFilter GetFileFilterByName(IncreamentationType increamentationType, params string[] p)
        {
            return GetFileFilterByName(increamentationType.ToString(), p);
        }
    }
}
