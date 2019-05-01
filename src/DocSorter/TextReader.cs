using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DocSorter
{
    internal static class TextReader
    {
        internal static string ReadText(string fullPath, FileTypes fileType)
        {
            if (!String.IsNullOrWhiteSpace(fullPath))
            {
                if (fileType == FileTypes.txt)
                {

                }
                else if (fileType == FileTypes.pdf)
                {
                    return ReadPDF(fullPath);
                }
            }
            return "";
        }


        private static string ReadPDF(string fullPath)
        {
            var document = new IronPdf.PdfDocument(fullPath);
            return document.ExtractAllText();
        }


    }

    internal enum FileTypes
    {
        txt,
        pdf
    }
}
