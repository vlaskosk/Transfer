using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transfer.Domain;
using Xunit;

namespace Transfer.Tests
{
    public class CommandsProcessorTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task NoCommandReturnsAppropriateMessage(string command)
        {
            var copyTaskManager = Mock.Of<ICopyTaskManager>();
            var addCommandDirectoriesBuilder = Mock.Of<IAddCommandDirectoriesBuilder>();
            var commandsProcessor = new CommandsProcessor(copyTaskManager, addCommandDirectoriesBuilder);
            var result = await commandsProcessor.ExecuteCommand(command);
            result.isFinished.ShouldBeFalse();
            result.message.ShouldBe("No Command");
        }

        [Theory]
        [InlineData("a")]
        [InlineData("a a a")]
        [InlineData("add")]
        [InlineData("add a")]
        [InlineData("add a a a")]
        public async Task UnknownCommandReturnsAppropriateMessage(string command)
        {
            var copyTaskManager = Mock.Of<ICopyTaskManager>();
            var addCommandDirectoriesBuilder = Mock.Of<IAddCommandDirectoriesBuilder>();
            Mock.Get(addCommandDirectoriesBuilder).Setup(e => e.BuildDirectories(It.IsAny<string>())).Returns((null, null, false));
            var commandsProcessor = new CommandsProcessor(copyTaskManager, addCommandDirectoriesBuilder);
            var result = await commandsProcessor.ExecuteCommand(command);
            result.isFinished.ShouldBeFalse();
            result.message.ShouldBe("Unknown Command");
        }

        [Theory]
        [InlineData("add sourcedirectory destinationdirectory")]
        [InlineData("Add sourcedirectory destinationdirectory")]
        [InlineData("ADD sourcedirectory destinationdirectory")]
        public async Task AddCommandReturnsAppropriateMessage(string command)
        {
            var copyTaskManager = Mock.Of<ICopyTaskManager>();
            var addCommandDirectoriesBuilder = Mock.Of<IAddCommandDirectoriesBuilder>();
            Mock.Get(addCommandDirectoriesBuilder).Setup(e => e.BuildDirectories(It.IsAny<string>())).Returns(("sourcedirectory", "destinationdirectory", true));
            var commandsProcessor = new CommandsProcessor(copyTaskManager, addCommandDirectoriesBuilder);
            var result = await commandsProcessor.ExecuteCommand(command);
            result.isFinished.ShouldBeFalse();
            result.message.ShouldContain("sourcedirectory");
            result.message.ShouldContain("destinationdirectory");
            Mock.Get(copyTaskManager).Verify(e => e.AddDirectories("sourcedirectory", "destinationdirectory"), Times.Once);
        }

        [Theory]
        [InlineData("status")]
        [InlineData("Status")]
        [InlineData("STATUS")]
        public async Task StatusCommandReturnsAppropriateMessage(string command)
        {
            var copyTaskManager = Mock.Of<ICopyTaskManager>();
            Mock.Get(copyTaskManager).Setup(e => e.GetStatus()).Returns(new List<CopyTaskDetails> { new CopyTaskDetails("sourcefile", "destinationfile", "extension") });

            var addCommandDirectoriesBuilder = Mock.Of<IAddCommandDirectoriesBuilder>();
            var commandsProcessor = new CommandsProcessor(copyTaskManager, addCommandDirectoriesBuilder);
            var result = await commandsProcessor.ExecuteCommand(command);
            result.isFinished.ShouldBeFalse();
            result.message.ShouldContain("sourcefile");
            result.message.ShouldContain("destinationfile");
            Mock.Get(copyTaskManager).Verify(e => e.GetStatus(), Times.Once);
        }

        [Theory]
        [InlineData("exit")]
        [InlineData("Exit")]
        [InlineData("EXIT")]
        public async Task EndCommandReturnsAppropriateMessage(string command)
        {
            var copyTaskManager = Mock.Of<ICopyTaskManager>();

            var addCommandDirectoriesBuilder = Mock.Of<IAddCommandDirectoriesBuilder>();
            var commandsProcessor = new CommandsProcessor(copyTaskManager, addCommandDirectoriesBuilder);
            var result = await commandsProcessor.ExecuteCommand(command);
            result.isFinished.ShouldBeTrue();
            result.message.ShouldBe("Exited");
            Mock.Get(copyTaskManager).Verify(e => e.End(), Times.Once);
        }
    }
}
