using Frosty.Core;
using Frosty.Core.Windows;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RootInstanceEntiresPlugin
{
    public static class RootInstanceEbxEntryDb
    {
        public static bool IsLoaded { get; private set; }

        private const uint cacheVersion = 1;
        private static Dictionary<Guid, Guid> ebxRootInstanceGuidList = new Dictionary<Guid, Guid>();

        public static void LoadEbxRootInstanceEntries(FrostyTaskLogger logger)
        {
            ebxRootInstanceGuidList.Clear();

            if (!ReadCache(logger))
            {
                uint totalCount = App.AssetManager.GetEbxCount();
                uint index = 0;

                logger.Log("Collecting ebx root instance guids");

                foreach (EbxAssetEntry entry in App.AssetManager.EnumerateEbx())
                {
                    uint progress = (uint)((index / (float)totalCount) * 100);
                    logger.LogProgress(progress);

                    EbxAsset asset = App.AssetManager.GetEbx(entry);
                    ebxRootInstanceGuidList.Add(asset.RootInstanceGuid, entry.Guid);

                    index++;
                }

                WriteToCache(logger);
            }
            IsLoaded = true;
        }

        public static EbxAssetEntry GetEbxEntryByRootInstanceGuid(Guid guid)
        {
            return ebxRootInstanceGuidList.ContainsKey(guid) ? App.AssetManager.GetEbxEntry(ebxRootInstanceGuidList[guid]) : null;
        }

        public static bool ReadCache(FrostyTaskLogger logger)
        {
            if (!File.Exists($"{App.FileSystem.CacheName}_rootinstances.cache"))
                return false;

            logger.Log($"Loading Data ({App.FileSystem.CacheName}_rootinstances.cache)");

            using (NativeReader reader = new NativeReader(new FileStream($"{App.FileSystem.CacheName}_rootinstances.cache", FileMode.Open, FileAccess.Read)))
            {
                uint version = reader.ReadUInt();
                if (version != cacheVersion)
                    return false;

                int profileHash = reader.ReadInt();
                if (profileHash != Fnv1.HashString(ProfilesLibrary.ProfileName))
                    return false;

                int count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Guid rootInstanceGuid = reader.ReadGuid();
                    Guid fileGuid = reader.ReadGuid();

                    ebxRootInstanceGuidList.Add(rootInstanceGuid, fileGuid);
                }
            }

            return true;
        }

        public static void WriteToCache(FrostyTaskLogger logger)
        {
            FileInfo fi = new FileInfo($"{App.FileSystem.CacheName}_rootinstances.cache");
            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName);

            logger.Log("Caching data");

            using (NativeWriter writer = new NativeWriter(new FileStream(fi.FullName, FileMode.Create)))
            {
                writer.Write(cacheVersion);
                writer.Write(Fnv1.HashString(ProfilesLibrary.ProfileName));

                writer.Write(ebxRootInstanceGuidList.Count);
                foreach (KeyValuePair<Guid, Guid> kv in ebxRootInstanceGuidList)
                {
                    writer.Write(kv.Key); // Root Instance Guid
                    writer.Write(kv.Value); // File Guid
                }
            }
        }
    }
}
