using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileCutter
{
    public class Process
    {
        private const string CUTTER_EXTENSION = ".cut";
        private const string SEACH_PATTERN = "*" + CUTTER_EXTENSION;
        private const string SEPARATOR = ".";

        public Process() { }

        private int GetFileSize(int chunkSize, SizeType sizeType)
        {
            switch (sizeType)
            {
                case SizeType.KBytes:
                    return chunkSize * 1024;
                case SizeType.MBytes:
                    return chunkSize * 1024 * 1024;
                case SizeType.GBytes:
                    return chunkSize * 1024 * 1024 * 1024;
                default:
                    return chunkSize;
            }
        }

        private string PreparePathFileName(string inputFile, int position)
        {
            var extension = Path.GetExtension(inputFile);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
            var zeros = position.ToString().PadLeft(1, '0');

            var fileName = $"{fileNameWithoutExtension}{SEPARATOR}{zeros}{extension}{CUTTER_EXTENSION}";

            return Path.Combine(Path.GetDirectoryName(inputFile), fileName);
        }

        private string[] GetFilesToMerge(string outPutPath)
        {
            var files = Directory.GetFiles(outPutPath, SEACH_PATTERN);

            return files.OrderBy(n => Regex.Replace(n, @"\d+", n => n.Value.PadLeft((files.Length).ToString().Length, '0'))).ToArray();
        }

        private string PrepareFileName(string file)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            return fileName.Substring(0, fileName.IndexOf(SEPARATOR)) + Path.GetExtension(fileName);
        }

        private void SplitterByNumberOfFiles(string inputFile, int chunkFiles)
        {
            using (var fileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                var sizeOfEachFile = (int)Math.Ceiling((double)fileStream.Length / chunkFiles);

                for (var i = 0; i <= chunkFiles; i++)
                {
                    var pathFileName = PreparePathFileName(inputFile, i);
                    var flag = false;

                    using (var outputFile = new FileStream(pathFileName, FileMode.Create, FileAccess.Write))
                    {
                        var bytesRead = 0;
                        var buffer = new byte[sizeOfEachFile];

                        if ((bytesRead = fileStream.Read(buffer, 0, sizeOfEachFile)) > 0)
                        {
                            flag = true;
                            outputFile.Write(buffer, 0, bytesRead);
                        }
                    }

                    if (!flag)
                        File.Delete(pathFileName);
                }
            }
        }

        private void SplitterBySizeOfFiles(string inputFile, int chunkSize, SizeType sizeType = SizeType.Bytes)
        {
            var fileSize = (sizeType == SizeType.Bytes ? chunkSize : GetFileSize(chunkSize, sizeType));

            using (var fileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                var position = 0;

                while (fileStream.Position < fileStream.Length)
                {

                    using (var outputFile = new FileStream(PreparePathFileName(inputFile, position), FileMode.Create, FileAccess.Write))
                    {
                        int remaining = fileSize, bytesRead;
                        var buffer = new byte[fileSize];

                        while (remaining > 0 &&
                              (bytesRead = fileStream.Read(buffer, 0, Math.Min(remaining, fileSize))) > 0)
                        {
                            outputFile.Write(buffer, 0, bytesRead);
                            remaining -= bytesRead;
                        }

                        position++;
                    }
                }
            }
        }

        public void Joiner(string outputPath, bool deleteCutterFiles = true)
        {
            string[] files = GetFilesToMerge(outputPath);

            if (files.Length > 0)
            {
                var fileName = PrepareFileName(files[0]);

                using (var outputFile = new FileStream(Path.Combine(outputPath, fileName), FileMode.OpenOrCreate, FileAccess.Write))
                {
                    foreach (var file in files)
                    {
                        var bytesRead = 0;
                        var buffer = new byte[1024];

                        using (var inputTempFile = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Read))
                            while ((bytesRead = inputTempFile.Read(buffer, 0, 1024)) > 0)
                                outputFile.Write(buffer, 0, bytesRead);

                        if (deleteCutterFiles)
                            File.Delete(file);
                    }
                }
            }
        }

        public void Splitter(string inputFile, int chunkFiles, SizeType sizeType = SizeType.None, bool deleteOriginFile = true)
        {
            if (sizeType == SizeType.None)
                SplitterByNumberOfFiles(inputFile, chunkFiles);
            else
                SplitterBySizeOfFiles(inputFile, chunkFiles, sizeType);

            if (deleteOriginFile)
                File.Delete(inputFile);
        }
    }
}
