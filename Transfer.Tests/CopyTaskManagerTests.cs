using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Transfer.Domain;
using Xunit;

namespace Transfer.Tests
{
    public class CopyTaskManagerTests
    {
        [Fact]
        public async Task InitStartsWithNotPreviousUnfinishedTasks()
        {
            var fileOperations = Mock.Of<IFileOperations>();
            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails>());
            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            var result = copyTaskManager.GetStatus();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task InitStartsWithPreviousUnfinishedTasks()
        {
            var fileOperations = Mock.Of<IFileOperations>();
            var copyTaskDetail = new CopyTaskDetails("souce", "destination", "exension") {  TransferStatus = TransferStatus.Done };
            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails>{ copyTaskDetail });
            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            var result = copyTaskManager.GetStatus();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(1);
            result[0].ShouldBe(copyTaskDetail);
        }

        [Fact]
        public async Task EndWillFinish()
        {
            var fileOperations = Mock.Of<IFileOperations>();
            var copyTaskDetail = new CopyTaskDetails("souce", "destination", "exension") { TransferStatus = TransferStatus.Done };
            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails> { copyTaskDetail });
            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            var result = copyTaskManager.End().Wait(TimeSpan.FromSeconds(1));
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task AddWillFailIfAddingExceptionOccurs()
        {
            var exceptionMessage = "Exception message";
            var fileOperations = Mock.Of<IFileOperations>();
            Mock.Get(fileOperations).Setup(e => e.GetTasksFromDirectory(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception(exceptionMessage));
            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails>());
            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            copyTaskManager.AddDirectories("source", "destination");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            var result = copyTaskManager.GetStatus();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(1);
            result[0].TransferStatus.ShouldBe(TransferStatus.Error);
            result[0].ErrorMessage.ShouldNotBeNull();
            result[0].ErrorMessage.ShouldContain(exceptionMessage);
            Mock.Get(fileOperations).Verify(e => e.CopyFile(It.IsAny<CopyTaskDetails>()), Times.Never);
        }

        [Fact]
        public async Task AddWillSucceed()
        {
            var copyTaskDetail1 = new CopyTaskDetails("souce", "destination", "exension") { TransferStatus = TransferStatus.Done };
            var copyTaskDetail2 = new CopyTaskDetails("souce", "destination", "exension") { TransferStatus = TransferStatus.Done };
            var fileOperations = Mock.Of<IFileOperations>();
            Mock.Get(fileOperations)
                .Setup(e => e.GetTasksFromDirectory(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<CopyTaskDetails> { copyTaskDetail1, copyTaskDetail2});
            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails>());
            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            copyTaskManager.AddDirectories("source", "destination");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            var result = copyTaskManager.GetStatus();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(2);
            result.ShouldContain(copyTaskDetail1);
            result.ShouldContain(copyTaskDetail2);
        }


        [Fact]
        public async Task WillProcessFile()
        {
            var copyTaskDetail1 = new CopyTaskDetails("souce", "destination", "exension");

            var fileOperations = Mock.Of<IFileOperations>();
            Mock.Get(fileOperations)
                .Setup(e => e.GetTasksFromDirectory(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<CopyTaskDetails> { copyTaskDetail1 });
            
            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails>());
            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            copyTaskManager.AddDirectories("source", "destination");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            var result = copyTaskManager.GetStatus();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(1);
            result.ShouldContain(e => e == copyTaskDetail1 && e.TransferStatus == TransferStatus.Done);
            copyTaskDetail1.HistoricalTransferStatuses.Count.ShouldBe(3);
            copyTaskDetail1.HistoricalTransferStatuses.ContainsKey(TransferStatus.Awaiting).ShouldBeTrue();
            copyTaskDetail1.HistoricalTransferStatuses.ContainsKey(TransferStatus.Copying).ShouldBeTrue();
            copyTaskDetail1.HistoricalTransferStatuses.ContainsKey(TransferStatus.Done).ShouldBeTrue();
            copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Awaiting].ShouldBeLessThan(copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Copying]);
            copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Copying].ShouldBeLessThan(copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Done]);
            Mock.Get(fileOperations).Verify(e => e.CopyFile(copyTaskDetail1), Times.Once);
        }

        [Fact]
        public async Task AddWillFailIfCopyExceptionOccurs()
        {
            var exceptionMessage = "Exception message";
            var copyTaskDetail1 = new CopyTaskDetails("souce", "destination", "exension");
            
            var fileOperations = Mock.Of<IFileOperations>();
            Mock.Get(fileOperations)
                .Setup(e => e.GetTasksFromDirectory(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<CopyTaskDetails> { copyTaskDetail1 });
            Mock.Get(fileOperations)
                .Setup(e => e.CopyFile(copyTaskDetail1)).Throws(new Exception(exceptionMessage));

            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails>());
            
            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            copyTaskManager.AddDirectories("source", "destination");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            var result = copyTaskManager.GetStatus();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(1);
            result[0].ShouldBe(copyTaskDetail1);
            result[0].TransferStatus.ShouldBe(TransferStatus.Error);
            result[0].ErrorMessage.ShouldNotBeNull();
            result[0].ErrorMessage.ShouldContain(exceptionMessage);
            copyTaskDetail1.HistoricalTransferStatuses.Count.ShouldBe(3);
            copyTaskDetail1.HistoricalTransferStatuses.ContainsKey(TransferStatus.Awaiting).ShouldBeTrue();
            copyTaskDetail1.HistoricalTransferStatuses.ContainsKey(TransferStatus.Copying).ShouldBeTrue();
            copyTaskDetail1.HistoricalTransferStatuses.ContainsKey(TransferStatus.Error).ShouldBeTrue();
            copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Awaiting].ShouldBeLessThan(copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Copying]);
            copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Copying].ShouldBeLessThan(copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Error]);
            Mock.Get(fileOperations).Verify(e => e.CopyFile(copyTaskDetail1), Times.Once);
        }


        [Fact]
        public async Task CanProcessTwoFilesOfDifferentExtensionAtSameTime()
        {
            var copyTaskDetail1 = new CopyTaskDetails("souce", "destination", "exension");
            var copyTaskDetail2 = new CopyTaskDetails("souce1", "destination1", "exension1");
            var fileOperations = Mock.Of<IFileOperations>();
            Mock.Get(fileOperations)
                .Setup(e => e.GetTasksFromDirectory(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<CopyTaskDetails> { copyTaskDetail1, copyTaskDetail2 });
            Mock.Get(fileOperations)
                .Setup(e => e.CopyFile(It.IsAny<CopyTaskDetails>())).Callback<CopyTaskDetails>(e =>
                {
                    Thread.Sleep(1000);
                });

            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails>());

            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            copyTaskManager.AddDirectories("source", "destination");
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            var result = copyTaskManager.GetStatus();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(2);
            result.ShouldContain(e => e == copyTaskDetail1 && e.TransferStatus == TransferStatus.Done);
            result.ShouldContain(e => e == copyTaskDetail2 && e.TransferStatus == TransferStatus.Done);
            Mock.Get(fileOperations).Verify(e => e.CopyFile(copyTaskDetail1), Times.Once);
            Mock.Get(fileOperations).Verify(e => e.CopyFile(copyTaskDetail2), Times.Once);
            (copyTaskDetail2.HistoricalTransferStatuses[TransferStatus.Copying] - copyTaskDetail1.HistoricalTransferStatuses[TransferStatus.Copying]).TotalMilliseconds.ShouldBeLessThan(1000);
        }

        [Fact]
        public async Task CannotProcessTwoFilesOfSameExtensionAtSameTime()
        {
            var copyTimes = new List<DateTime>();
            var copyTaskDetail1 = new CopyTaskDetails("souce", "destination", "exension") { TransferStatus = TransferStatus.Awaiting };
            var copyTaskDetail2 = new CopyTaskDetails("souce1", "destination1", "exension") { TransferStatus = TransferStatus.Awaiting };
            var fileOperations = Mock.Of<IFileOperations>();
            Mock.Get(fileOperations)
                .Setup(e => e.GetTasksFromDirectory(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<CopyTaskDetails> { copyTaskDetail1, copyTaskDetail2 });
            Mock.Get(fileOperations)
                .Setup(e => e.CopyFile(It.IsAny<CopyTaskDetails>())).Callback<CopyTaskDetails>(e => {
                    copyTimes.Add(DateTime.Now);
                    Thread.Sleep(1000);
                });

            var persist = Mock.Of<IPersistCopyTasks>();
            Mock.Get(persist).Setup(e => e.GetIncompleteCopyTasks()).Returns(new List<CopyTaskDetails>());
            var copyTaskManager = new CopyTaskManager(persist, fileOperations);
            await copyTaskManager.Start();
            copyTaskManager.AddDirectories("source", "destination");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            var result = copyTaskManager.GetStatus();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(2);
            result.ShouldContain(e => e == copyTaskDetail1 && e.TransferStatus != TransferStatus.Awaiting);
            result.ShouldContain(e => e == copyTaskDetail2 && e.TransferStatus == TransferStatus.Awaiting);
        }
    }
}
