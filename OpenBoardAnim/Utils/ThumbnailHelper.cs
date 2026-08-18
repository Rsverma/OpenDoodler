using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OpenBoardAnim.Utils
{
    // Thumbnails are cached on disk keyed by a hash of the project's own file path, rather than
    // its DB ProjectID (unset on the RecentProjectModel created for a brand new project - see
    // CacheService.SaveNewProject) or a new DB column (avoiding an EF migration for what's
    // otherwise a pure side-channel, the same reasoning behind CacheService's autosave backup
    // file).
    public static class ThumbnailHelper
    {
        private static readonly string ThumbnailsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenBoardAnim", "Thumbnails");

        public static string GetThumbnailPath(string projectFilePath)
        {
            if (string.IsNullOrWhiteSpace(projectFilePath)) return null;
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(projectFilePath.ToLowerInvariant()));
            return Path.Combine(ThumbnailsDirectory, Convert.ToHexString(hash) + ".png");
        }
    }
}
