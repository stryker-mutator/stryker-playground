﻿namespace Stryker.Core.Common
{
    public static class FilePathUtils
    {
        public static string NormalizePathSeparators(string filePath)
        {
            if (filePath == null)
            {
                return null;
            }

            if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
            {
                return filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
            else
            {
                // If the directory separator char and its alternative are the same, every valid path is already normalized.
                return filePath;
            }
        }

        public static string ToFullPath(this string path)
        {
            if (path == null)
            {
                return null;
            }

            return Path.GetFullPath(path);
        }
    }
}