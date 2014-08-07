namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using MefV1 = System.ComponentModel.Composition;

    public class ProjectSystemInvalidMocksTests
    {
        [MefFact(CompositionEngines.V3EmulatingV1AndV2AtOnce | CompositionEngines.V3AllowConfigurationWithErrors,
            typeof(PartThatBelongsInProjectButImportsConfiguredProject),
            typeof(ProjectService), typeof(Project), typeof(ConfiguredProject))]
        public void PartAtInappropriateScopeIsFlaggedAsRootCause(IContainer container)
        {
            // Verify that overall project system functionality still works.
            var projectService = container.GetExportedValue<ProjectService>();
            var project = projectService.CreateProject().Value;
            var configuredProject = project.CreateConfiguration().Value;

            // Verify that the bad part was rejected.
            var v3Container = (TestUtilities.V3ContainerWrapper)container;
            Assert.False(v3Container.Configuration.CompositionErrors.IsEmpty);
            var errors = v3Container.Configuration.CompositionErrors.Peek();
            Assert.Equal(1, errors.Count);
            Assert.Equal(1, errors.First().Parts.Count);
            Assert.Equal(typeof(PartThatBelongsInProjectButImportsConfiguredProject), errors.First().Parts.First().Definition.Type);

            // Verify that the ImportMany collection is empty.
            Assert.Equal(0, project.ProjectExports.Count);
        }

        [MefV1.Export("ProjectScopedExports")]
        public class PartThatBelongsInProjectButImportsConfiguredProject
        {
            [MefV1.Import]
            public ConfiguredProject ConfiguredProject { get; set; }
        }

        #region Common MEF parts

        [Export, Shared]
        public class ProjectService
        {
            [Import, SharingBoundary("Project")]
            public ExportFactory<Project> ProjectFactory { get; set; }

            public Export<Project> CreateProject()
            {
                var project = this.ProjectFactory.CreateExport();
                return project;
            }
        }

        [Export, Shared("Project")]
        public class Project
        {
            [Import]
            public ProjectService ProjectService { get; set; }

            [ImportMany("ProjectScopedExports")]
            public List<object> ProjectExports { get; set; }

            [Import, SharingBoundary("ConfiguredProject")]
            public ExportFactory<ConfiguredProject> ConfiguredProjectFactory { get; set; }

            public Export<ConfiguredProject> CreateConfiguration()
            {
                return this.ConfiguredProjectFactory.CreateExport();
            }
        }

        [Export, Shared("ConfiguredProject")]
        public class ConfiguredProject
        {
            [Import]
            public Project Project { get; set; }
        }

        #endregion
    }
}
