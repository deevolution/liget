using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;
using NuGet.Packaging;

namespace NuGet
{
    public class PhysicalFileSystem : IFileSystem
    {
        private readonly string _root;

        public PhysicalFileSystem(string root)
        {
            if (String.IsNullOrEmpty(root))
            {
                throw new ArgumentNullException("root");
            }
            _root = root;
        }

        public string Root
        {
            get
            {
                return _root;
            }
        }

        public virtual string GetFullPath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return Root;
            }
            return Path.Combine(Root, path);
        }

        public virtual void AddFile(string path, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            AddFileCore(path, targetStream => stream.CopyTo(targetStream));
        }

        public virtual void AddFile(string path, Action<Stream> writeToStream)
        {
            if (writeToStream == null)
            {
                throw new ArgumentNullException("writeToStream");
            }

            AddFileCore(path, writeToStream);
        }

        public virtual void AddFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            throw new NotImplementedException("add files");
        }

        private void AddFileCore(string path, Action<Stream> writeToStream)
        {
            EnsureDirectory(Path.GetDirectoryName(path));

            string fullPath = GetFullPath(path);

            using (Stream outputStream = File.Create(fullPath))
            {
                writeToStream(outputStream);
            }
        }

        public virtual void DeleteFile(string path)
        {
            if (!FileExists(path))
            {
                return;
            }

            try
            {
                MakeFileWritable(path);
                path = GetFullPath(path);                
                File.Delete(path);
                string folderPath = Path.GetDirectoryName(path);
            }
            catch (FileNotFoundException)
            {

            }
        }

        public virtual void DeleteFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            throw new NotImplementedException("delete files");
        }

        public virtual void DeleteDirectory(string path)
        {
            DeleteDirectory(path, recursive: false);
        }

        public virtual void DeleteDirectory(string path, bool recursive)
        {
            if (!DirectoryExists(path))
            {
                return;
            }

            try
            {
                path = GetFullPath(path);
                Directory.Delete(path, recursive);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        public virtual IEnumerable<string> GetFiles(string path, bool recursive)
        {
            return GetFiles(path, null, recursive);
        }

        public virtual IEnumerable<string> GetFiles(string path, string filter, bool recursive)
        {
            path = PathUtility.EnsureTrailingSlash(GetFullPath(path));
            if (String.IsNullOrEmpty(filter))
            {
                filter = "*.*";
            }
            try
            {
                if (!Directory.Exists(path))
                {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateFiles(path, filter, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            return Enumerable.Empty<string>();
        }

        public virtual IEnumerable<string> GetDirectories(string path)
        {
            try
            {
                path = PathUtility.EnsureTrailingSlash(GetFullPath(path));
                if (!Directory.Exists(path))
                {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateDirectories(path)
                                .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            return Enumerable.Empty<string>();
        }

        public virtual DateTimeOffset GetLastModified(string path)
        {
            path = GetFullPath(path);
            if (File.Exists(path))
            {
                return File.GetLastWriteTimeUtc(path);
            }
            return Directory.GetLastWriteTimeUtc(path);
        }

        public DateTimeOffset GetCreated(string path)
        {
            path = GetFullPath(path);
            if (File.Exists(path))
            {
                return File.GetCreationTimeUtc(path);
            }
            return Directory.GetCreationTimeUtc(path);
        }

        public DateTimeOffset GetLastAccessed(string path)
        {
            path = GetFullPath(path);
            if (File.Exists(path))
            {
                return File.GetLastAccessTimeUtc(path);
            }
            return Directory.GetLastAccessTimeUtc(path);
        }

        public virtual bool FileExists(string path)
        {
            path = GetFullPath(path);
            return File.Exists(path);
        }

        public virtual bool DirectoryExists(string path)
        {
            path = GetFullPath(path);
            return Directory.Exists(path);
        }

        public virtual Stream OpenFile(string path)
        {
            path = GetFullPath(path);
            return File.OpenRead(path);
        }

        public virtual Stream CreateFile(string path)
        {
            string fullPath = GetFullPath(path);

            // before creating the file, ensure the parent directory exists first.
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory)) 
            {
                Directory.CreateDirectory(directory);
            }

            return File.Create(fullPath);
        }

        protected string MakeRelativePath(string fullPath)
        {
            return fullPath.Substring(Root.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        protected virtual void EnsureDirectory(string path)
        {
            path = GetFullPath(path);
            Directory.CreateDirectory(path);
        }

        public void MakeFileWritable(string path)
        {
            path = GetFullPath(path);
            FileAttributes attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
        }

        public virtual void MoveFile(string source, string destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            string srcFull = GetFullPath(source);
            string destFull = GetFullPath(destination);

            if (string.Equals(srcFull, destFull, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                File.Move(srcFull, destFull);
            }
            catch (IOException)
            {
                File.Delete(srcFull);
            }
        }

        public void MakeDirectoryForFile(string nupkgPath)
        {
            new FileInfo(nupkgPath).Directory.Create();            
        }
    }
}