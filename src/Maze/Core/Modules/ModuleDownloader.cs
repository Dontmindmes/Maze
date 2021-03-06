using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.PackageExtraction;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using Maze.Core.Connection;
using Maze.ModuleManagement;
using Maze.ModuleManagement.PackageManagement;
using Maze.ModuleManagement.Server;
using Maze.Options;
using Maze.Server.Connection.Modules;
using NuGet.Configuration;

namespace Maze.Core.Modules
{
    public interface IModuleDownloader
    {
        Task Load(PackagesLock packagesLock, ServerConnection serverConnection, CancellationToken token);
    }

    public class ModuleDownloader : IModuleDownloader
    {
        private readonly IModulesDirectory _modulesDirectory;
        private readonly ModulesOptions _options;

        public ModuleDownloader(IModulesDirectory modulesDirectory, IOptions<ModulesOptions> options)
        {
            _modulesDirectory = modulesDirectory;
            _options = options.Value;
        }

        public async Task Load(PackagesLock packagesLock, ServerConnection serverConnection, CancellationToken token)
        {
            var serverRepo = new ServerRepository(new Uri(serverConnection.RestClient.BaseUri, "nuget"));

            var context = new PackageDownloadContext(new SourceCacheContext {DirectDownload = true, NoCache = true},
                Environment.ExpandEnvironmentVariables(_options.TempPath), true);

            var packageDownloadManager = new PackageDownloadManager(_modulesDirectory, serverRepo);
            var result =
                await packageDownloadManager.DownloadPackages(packagesLock, context, NullLogger.Instance, token);

            if (result.Any())
                try
                {
                    foreach (var preFetchResult in result.Values)
                        using (var downloadPackageResult = await preFetchResult.GetResultAsync())
                        {
                            // use the version exactly as specified in the nuspec file
                            var packageIdentity = await downloadPackageResult.PackageReader.GetIdentityAsync(token);

                            var packageExtractionContext = new PackageExtractionContext(
                                PackageSaveMode.Defaultv3,
                                PackageExtractionBehavior.XmlDocFileSaveMode,
                                ClientPolicyContext.GetClientPolicy(new NullSettings(), new NullLogger()),
                                new NullLogger());

                            downloadPackageResult.PackageStream.Position = 0;

                            await PackageExtractor.InstallFromSourceAsync(serverRepo.PackageSource.Source,
                                packageIdentity, stream => downloadPackageResult.PackageStream.CopyToAsync(stream),
                                _modulesDirectory.VersionFolderPathResolver, packageExtractionContext, token);
                        }
                }
                finally
                {
                    foreach (var fetcherResult in result.Values)
                    {
                        await fetcherResult.EnsureResultAsync();
                        fetcherResult.Dispose();
                    }
                }
        }
    }
}