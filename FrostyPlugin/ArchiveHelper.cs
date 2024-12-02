using System.IO;

namespace Frosty.Core
{
    public static class ArchiveHelper
    {
        public static string GetArchivePath(string fileName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                errorMessage = "Provided mod file name is empty.";
                return null;
            }

            if (!File.Exists(fileName))
            {
                errorMessage = $"Mod file '{fileName}' does not exists.";
                return null;
            }

            var fileInfo = new FileInfo(fileName);

            if (fileInfo.Extension.ToLower() != ".fbmod")
            {
                errorMessage = $"File '{fileName}' is not a proper mod file.";
                return null;
            }

            var fileShortName = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length);
            var archiveShortName = fileShortName + "_01.archive";

            var archivePath = Path.Combine(fileInfo.DirectoryName, archiveShortName);

            return archivePath;
        }
    }
}
