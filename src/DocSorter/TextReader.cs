﻿using PdfSharp.Pdf.Content.Objects;
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
            return PdfTextExtractor.GetText(fullPath);
        }

        private static IEnumerable<string> ExtractText(CObject cObject)
        {
            var textList = new List<string>();
            if (cObject is COperator)
            {
                var cOperator = cObject as COperator;
                if (cOperator.OpCode.Name == OpCodeName.Tj.ToString() ||
                    cOperator.OpCode.Name == OpCodeName.TJ.ToString())
                {
                    foreach (var cOperand in cOperator.Operands)
                    {
                        textList.AddRange(ExtractText(cOperand));
                    }
                }
            }
            else if (cObject is CSequence)
            {
                var cSequence = cObject as CSequence;
                foreach (var element in cSequence)
                {
                    textList.AddRange(ExtractText(element));
                }
            }
            else if (cObject is CString)
            {
                var cString = cObject as CString;
                textList.Add(cString.Value);
            }
            return textList;
        }



    }

    internal enum FileTypes
    {
        txt,
        pdf
    }
}
