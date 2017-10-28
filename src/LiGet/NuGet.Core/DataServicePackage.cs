using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NuGet
{
    public class DataServicePackage : IPackage
    {
        private CryptoHashProvider _hashProvider;
        private bool _usingMachineCache;
        private string _licenseNames;
        internal IPackage _package;

        internal IPackage Package { get { return _package; } }

        public string Id
        {
            get;
            set;
        }

        public string Version
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Authors
        {
            get;
            set;
        }

        public string Owners
        {
            get;
            set;
        }

        public string IconUrl
        {
            get;
            set;
        }

        public string LicenseUrl
        {
            get;
            set;
        }

        public string ProjectUrl
        {
            get;
            set;
        }

        public Uri ReportAbuseUrl
        {
            get;
            set;
        }

        public Uri GalleryDetailsUrl
        {
            get;
            set;
        }

        public string LicenseNames 
        {
            get { return _licenseNames; }
            set
            {
                _licenseNames = value;
                LicenseNameCollection = 
                    String.IsNullOrEmpty(value) ? new string[0] : value.Split(';').ToArray();
            }
        }

        public ICollection<string> LicenseNameCollection { get; private set; }

        public Uri LicenseReportUrl { get; set; }

        // public Uri DownloadUrl
        // {
        //     get
        //     {
        //         return Context.GetReadStreamUri(this);
        //     }
        // }

        public bool Listed
        {
            get;
            set;
        }

        public DateTimeOffset? Published
        {
            get;
            set;
        }

        public DateTimeOffset LastUpdated
        {
            get;
            set;
        }

        public int DownloadCount
        {
            get;
            set;
        }

        public bool RequireLicenseAcceptance
        {
            get;
            set;
        }

        public bool DevelopmentDependency
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public string Summary
        {
            get;
            set;
        }

        public string ReleaseNotes
        {
            get;
            set;
        }

        public string Language
        {
            get;
            set;
        }

        public string Tags
        {
            get;
            set;
        }

        public string Dependencies
        {
            get;
            set;
        }

        public string PackageHash
        {
            get;
            set;
        }

        public string PackageHashAlgorithm
        {
            get;
            set;
        }

        public bool IsLatestVersion
        {
            get;
            set;
        }

        public bool IsAbsoluteLatestVersion
        {
            get;
            set;
        }

        public string Copyright
        {
            get;
            set;
        }

        public string MinClientVersion
        {
            get;
            set;
        }

        private string OldHash { get; set; }

        // private IPackage Package
        // {
        //     get
        //     {
        //         EnsurePackage(MachineCache.Default);
        //         return _package;
        //     }
        // }

        // internal IDataServiceContext Context
        // {
        //     get;
        //     set;
        // }

        // internal PackageDownloader Downloader { get; set; }

        internal CryptoHashProvider HashProvider
        {
            get { return _hashProvider == null ? new CryptoHashProvider(PackageHashAlgorithm) : _hashProvider; }
            set { _hashProvider = value; }
        }

        bool IPackage.Listed
        {
            get
            {
                return Listed;
            }
        }

        public IEnumerable<PackageDependencyGroup> DependencySets
        {
            get
            {
                if (String.IsNullOrEmpty(Dependencies))
                {
                    return Enumerable.Empty<PackageDependencyGroup>();
                }

                return ParseDependencySet(Dependencies);
            }
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get 
            {
                return Package.PackageAssemblyReferences;
            }
        }

        SemanticVersion IPackageName.Version
        {
            get
            {
                if (Version != null)
                {
                    return SemanticVersion.Parse(Version);
                }
                return null;
            }
        }

        NuGetVersion IPackageMetadata.MinClientVersion
        {
            get
            {
                if (!String.IsNullOrEmpty(MinClientVersion))
                {
                    return new NuGetVersion(MinClientVersion);
                }

                return null;
            }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get
            {
                return Package.AssemblyReferences;
            }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get
            {
                return Package.FrameworkAssemblies;
            }
        }

        public virtual IEnumerable<NuGetFramework> GetSupportedFrameworks()
        {
            return Package.GetSupportedFrameworks();
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Package.GetFiles();
        }

        public Stream GetStream()
        {
            return Package.GetStream();
        }

        public void ExtractContents(IFileSystem fileSystem, string extractPath)
        {
            Package.ExtractContents(fileSystem, extractPath);
        }

        public override string ToString()
        {
            return this.GetFullName();
        }

        // [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        // internal void EnsurePackage(IPackageCacheRepository cacheRepository)
        // {
        //     // OData caches instances of DataServicePackage while updating their property values. As a result, 
        //     // the ZipPackage that we downloaded may no longer be valid (as indicated by a newer hash). 
        //     // When using MachineCache, once we've verified that the hashes match (which happens the first time around),
        //     // we'll simply verify the file exists between successive calls.
        //     IPackageMetadata packageMetadata = this;
        //     if (_package == null ||
        //             (_package is OptimizedZipPackage && !((OptimizedZipPackage)_package).IsValid) ||
        //             !String.Equals(OldHash, PackageHash, StringComparison.OrdinalIgnoreCase) ||
        //             (_usingMachineCache && !cacheRepository.Exists(Id, packageMetadata.Version)))
        //     {
        //         IPackage newPackage = null;
        //         bool inMemOnly = false;
        //         bool isValid = false;

        //         // If the package exists in the cache and has the correct hash then use it. Otherwise download it.
        //         if (TryGetPackage(cacheRepository, packageMetadata, out newPackage) && MatchPackageHash(newPackage))
        //         {
        //             isValid = true;
        //         }
        //         else
        //         {
        //             // We either do not have a package available locally or they are invalid. Download the package from the server.
        //             if (cacheRepository.InvokeOnPackage(packageMetadata.Id, packageMetadata.Version,
        //                 (stream) => Downloader.DownloadPackage(DownloadUrl, this, stream)))
        //             {
        //                 newPackage = cacheRepository.FindPackage(packageMetadata.Id, packageMetadata.Version);
        //                 Debug.Assert(newPackage != null);
        //             }
        //             else
        //             {
        //                 // this can happen when access to the %LocalAppData% directory is blocked, e.g. on Windows Azure Web Site build
        //                 using (var targetStream = new MemoryStream())
        //                 {
        //                     Downloader.DownloadPackage(DownloadUrl, this, targetStream);
        //                     targetStream.Seek(0, SeekOrigin.Begin);
        //                     newPackage = new ZipPackage(targetStream);
        //                 }

        //                 inMemOnly = true;
        //             }

        //             // Because of CDN caching, the hash returned in odata feed
        //             // can be out of sync with the hash of the file itself.
        //             // So for now, we cannot call MatchPackageHash(newPackage) to 
        //             // validate that the file downloaded has the right hash.
        //             isValid = true;
        //         }

        //         // apply the changes if the package hash was valid
        //         if (isValid)
        //         {
        //             _package = newPackage;

        //             // Make a note that the backing store for the ZipPackage is the machine cache.
        //             _usingMachineCache = !inMemOnly;

        //             OldHash = PackageHash;
        //         }
        //         else
        //         {
        //             // ensure package must end with a valid package, since we cannot load one we must throw.
        //             throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
        //                 NuGetResources.Error_InvalidPackage, Version, Id));
        //         }
        //     }
        // }

        /// <summary>
        /// True if the given package matches PackageHash
        /// </summary>
        private bool MatchPackageHash(IPackage package)
        {
            return package != null && package.GetHash(HashProvider).Equals(PackageHash, StringComparison.OrdinalIgnoreCase);
        }

        private static List<PackageDependencyGroup> ParseDependencySet(string value)
        {
            var dependencySets = new List<PackageDependencyGroup>();

            var dependencies = value.Split('|').Select(ParseDependency).ToList();

            // group the dependencies by target framework
            var groups = dependencies.GroupBy(d => d.Item3);

            dependencySets.AddRange(
                groups.Select(g => new PackageDependencyGroup(
                                           g.Key,   // target framework 
                                           g.Where(pair => !String.IsNullOrEmpty(pair.Item1))       // the Id is empty when a group is empty.
                                            .Select(pair => new PackageDependency(pair.Item1, pair.Item2)))));     // dependencies by that target framework
            return dependencySets;
        }

        /// <summary>
        /// Parses a dependency from the feed in the format:
        /// id or id:versionSpec, or id:versionSpec:targetFramework
        /// </summary>
        private static Tuple<string, VersionRange, NuGetFramework> ParseDependency(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // IMPORTANT: Do not pass StringSplitOptions.RemoveEmptyEntries to this method, because it will break 
            // if the version spec is null, for in that case, the Dependencies string sent down is "<id>::<target framework>".
            // We do want to preserve the second empty element after the split.
            string[] tokens = value.Trim().Split(new[] { ':' });

            if (tokens.Length == 0)
            {
                return null;
            }

            // Trim the id
            string id = tokens[0].Trim();
            
            VersionRange versionSpec = null;
            if (tokens.Length > 1)
            {
                // Attempt to parse the version
                VersionRange.TryParse(tokens[1], out versionSpec);
            }

            var targetFramework = (tokens.Length > 2 && !String.IsNullOrEmpty(tokens[2]))
                                    ? NuGetFramework.Parse(tokens[2])
                                    : null;

            return Tuple.Create(id, versionSpec, targetFramework);
        }

        // [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to return null if any error occurred while trying to find the package.")]
        // private static bool TryGetPackage(IPackageRepository repository, IPackageMetadata packageMetadata, out IPackage package)
        // {
        //     try
        //     {
        //         package = repository.FindPackage(packageMetadata.Id, packageMetadata.Version);
        //     }
        //     catch
        //     {
        //         // If the package in the repository is corrupted then return null
        //         package = null;
        //     }
        //     return package != null;
        // }
    }
}