﻿using System;
using System.Collections.Generic;
using System.Text;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace Orcus.Server.Service.Modules.PackageManagement
{
    public class GatherContext
    {
        public GatherContext()
        {
            // Defaults
            AllowDowngrades = true;
        }

        /// <summary>
        /// Project target framework
        /// </summary>
        public NuGetFramework TargetFramework { get; set; }

        /// <summary>
        /// Primary sources - Primary targets must exist here.
        /// </summary>
        public IReadOnlyList<SourceRepository> PrimarySources { get; set; }

        /// <summary>
        /// All sources - used for dependencies
        /// </summary>
        public IReadOnlyList<SourceRepository> AllSources { get; set; }

        /// <summary>
        /// Packages folder
        /// </summary>
        public SourceRepository PackagesFolderSource { get; set; }

        /// <summary>
        /// Target ids
        /// </summary>
        public IReadOnlyList<string> PrimaryTargetIds { get; set; }

        /// <summary>
        /// Targets with an id and version
        /// </summary>
        public IReadOnlyList<PackageIdentity> PrimaryTargets { get; set; }

        /// <summary>
        /// Already installed packages
        /// </summary>
        public IReadOnlyList<PackageIdentity> InstalledPackages { get; set; }

        /// <summary>
        /// If false dependencies from downgrades will be ignored.
        /// </summary>
        public bool AllowDowngrades { get; set; }

        /// <summary>
        /// Resolution context containing the GatherCache and DependencyBehavior.
        /// </summary>
        public ResolutionContext ResolutionContext { get; set; }

        /// <summary>
        /// If true, missing primary targets will be ignored.
        /// </summary>
        public bool IsUpdateAll { get; set; }

        public ILogger Log { get; set; }
    }
}
