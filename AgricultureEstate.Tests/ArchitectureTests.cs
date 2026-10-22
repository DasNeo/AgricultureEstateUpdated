using System.Reflection;
using AgricultureEstate.Application;
using AgricultureEstate.Domain;
using Xunit;

namespace AgricultureEstate.Tests
{
    public class ArchitectureTests
    {
        [Fact]
        public void DomainHasNoApplicationOrGameDependencies()
        {
            foreach (AssemblyName reference in typeof(EstateState).Assembly.GetReferencedAssemblies())
            {
                Assert.NotEqual("AgricultureEstate.Application", reference.Name);
                Assert.NotEqual("AgricultureEstate", reference.Name);
                Assert.False(IsGameDependency(reference.Name ?? ""));
            }
        }

        [Fact]
        public void ApplicationHasNoGameDependencies()
        {
            foreach (AssemblyName reference in typeof(EstateManagement).Assembly.GetReferencedAssemblies())
            {
                Assert.NotEqual("AgricultureEstate", reference.Name);
                Assert.False(IsGameDependency(reference.Name ?? ""));
            }
        }

        private static bool IsGameDependency(string name) =>
            name.StartsWith("TaleWorlds") || name.StartsWith("SandBox") || name.StartsWith("StoryMode") ||
            name.StartsWith("MCM") || name.Contains("Harmony");
    }
}
