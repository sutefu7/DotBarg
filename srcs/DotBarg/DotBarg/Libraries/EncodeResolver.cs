using Hnx8.ReadJEnc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * how to get encoding
 * https://github.com/hnx8/ReadJEnc/blob/master/ReadJEnc_Readme.txt
 * 
 */

namespace DotBarg.Libraries
{
    public class EncodeResolver
    {
        public static Encoding GetEncoding(string targetFile)
        {
            var fi = new FileInfo(targetFile);
            using (var reader = new FileReader(fi))
            {
                var code = reader.Read(fi);
                var encode = code.GetEncoding();
                return encode;
            }
        }
    }
}
