using Shouldly;
using Transfer.Domain;
using Xunit;

namespace Transfer.Tests
{
    public class AddCommandDirectoriesBuilderTests
    {
        [Theory]
        [InlineData("add c:\\source c:\\destination", "c:\\source", "c:\\destination")]
        [InlineData("add \"c:\\source\" \"c:\\destination\"", "c:\\source", "c:\\destination")]
        [InlineData("add \"c:\\source with space\" \"c:\\destination with space\"", "c:\\source with space", "c:\\destination with space")]
        [InlineData("add \"c:\\source with space\" c:\\destination", "c:\\source with space", "c:\\destination")]
        [InlineData("add c:\\source \"c:\\destination with space\"", "c:\\source", "c:\\destination with space")]
        public void CanBuildPath(string command, string expectedSourcePath, string expectedDestinationPath)
        {
            var builder = new AddCommandDirectoriesBuilder();
            var result = builder.BuildDirectories(command);
            result.okCommand.ShouldBeTrue();
            result.sourceDir.ShouldBe(expectedSourcePath);
            result.destinationDir.ShouldBe(expectedDestinationPath);
        }

        [Theory]
        [InlineData("add \"c:\\source c:\\destination\"")]
        [InlineData("add \"c:\\source c:\\destination")]
        [InlineData("add c:\\source with space\" c:\\destination with space")]
        [InlineData("add c:\\source c:\\destination\"")]
        public void CannotBuildPath(string command)
        {
            var builder = new AddCommandDirectoriesBuilder();
            var result = builder.BuildDirectories(command);
            result.okCommand.ShouldBeFalse();
        }
    }
}
