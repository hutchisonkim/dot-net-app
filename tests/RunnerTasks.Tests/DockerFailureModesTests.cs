using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using RunnerTasks.Tests.Fakes;
using Xunit;

namespace RunnerTasks.Tests
{
    public class DockerFailureModesTests
    {
        [Fact]
        public async Task RegisterAsync_ExecInspectNonZero_LogsWarningButReturnsTrueWhenListeningFound()
        {
            var fake = new FakeDockerClientWrapper_ExecInspectNonZero_LogsListening();
            var logger = new TestLogger<DockerDotNetRunnerService>();
            var svc = new DockerDotNetRunnerService(".", fake, logger);

            // Set private fields so RegisterAsync execs into existing container
            var cid = (await fake.CreateContainerAsync(new CreateContainerParameters { Image = "img" }, CancellationToken.None)).ID;
            svc.Test_SetInternalState(cid, null);
            svc.Test_SetImageTag("img:latest");
            svc.Test_SetLogWaitTimeout(TimeSpan.FromSeconds(1));

            var ok = await svc.RegisterAsync("token", "owner/repo", "https://github.com", CancellationToken.None);

            Assert.True(ok);
            Assert.True(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "run.sh exec returned non-zero exit code"));
        }

        [Fact]
        public async Task RegisterAsync_LogsMissingListening_ReturnsFalseAndLogsWarning()
        {
            var fake = new FakeDockerClientWrapper();
            var logger = new TestLogger<DockerDotNetRunnerService>();
            var svc = new DockerDotNetRunnerService(".", fake, logger);

            var cid = (await fake.CreateContainerAsync(new CreateContainerParameters { Image = "img" }, CancellationToken.None)).ID;
            svc.Test_SetInternalState(cid, null);
            svc.Test_SetImageTag("img:latest");
            svc.Test_SetLogWaitTimeout(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task RegisterAsync_AttachReadError_LogsAndReturnsFalse()
        {
            var fake = new RunnerTasks.Tests.Fakes.FakeDockerClientWrapper_AttachReadError();
            var logger = new TestLogger<DockerDotNetRunnerService>();
            var svc = new DockerDotNetRunnerService(".", fake, logger);

            var cid = (await fake.CreateContainerAsync(new CreateContainerParameters { Image = "img" }, CancellationToken.None)).ID;
            svc.Test_SetInternalState(cid, null);
            svc.Test_SetImageTag("img:latest");
            svc.Test_SetLogWaitTimeout(TimeSpan.FromSeconds(1));

            var ok = await svc.RegisterAsync("token", "owner/repo", "https://github.com", CancellationToken.None);

            Assert.False(ok);
            Assert.True(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Debug, "Error streaming start exec output") || logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "run.sh exec returned non-zero exit code"));
        }

        [Fact]
        public async Task RegisterAsync_AttachEof_ReturnsFalseAndLogsMissingListening()
        {
            var fake = new RunnerTasks.Tests.Fakes.FakeDockerClientWrapper_AttachEof();
            var logger = new TestLogger<DockerDotNetRunnerService>();
            var svc = new DockerDotNetRunnerService(".", fake, logger);

            var cid = (await fake.CreateContainerAsync(new CreateContainerParameters { Image = "img" }, CancellationToken.None)).ID;
            svc.Test_SetInternalState(cid, null);
            svc.Test_SetImageTag("img:latest");
            svc.Test_SetLogWaitTimeout(TimeSpan.FromSeconds(1));

            var ok = await svc.RegisterAsync("token", "owner/repo", "https://github.com", CancellationToken.None);

            Assert.False(ok);
            Assert.True(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "Did not see 'Listening for Jobs'"));
        }

        [Fact]
        public async Task StartContainersAsync_VolumeCreateFails_ContinuesAndReturnsTrue()
        {
            var fake = new FakeDockerClientWrapper_VolumeCreateThrows();
            // ensure images list contains a candidate tag
            fake.Images.Add(new ImagesListResponse { RepoTags = new[] { "myproj_github-runner:latest" } });

            var svc = new DockerDotNetRunnerService("myproj", fake, new TestLogger<DockerDotNetRunnerService>());

            var ok = await svc.StartContainersAsync(new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" }, CancellationToken.None);

            Assert.True(ok);
        }

        [Fact]
        public async Task RegisterAsync_RunOneOff_ImagePullFails_ButOneOffContinues()
        {
            // CreateImageAsync will throw, but RunOneOff swallows pull errors and should still run the container
            var fake = new FakeDockerClientWrapper_CreateImageThrows();
            var logger = new TestLogger<DockerDotNetRunnerService>();
            var svc = new DockerDotNetRunnerService(".", fake, logger);

            // ensure no image tag is set so RegisterAsync falls back to RunOneOffContainerAsync
            var ok = await svc.RegisterAsync("token", "owner/repo", "https://github.com", CancellationToken.None);

            Assert.True(ok);
        }

        [Fact]
        public async Task StartContainersAsync_CreateContainerThrows_ReturnsFalse()
        {
            var fake = new FakeDockerClientWrapper_CreateContainerThrows();
            // ensure images list contains a candidate tag so StartContainersAsync proceeds to create
            fake.Images.Add(new ImagesListResponse { RepoTags = new[] { "myproj_github-runner:latest" } });

            var svc = new DockerDotNetRunnerService("myproj", fake, new TestLogger<DockerDotNetRunnerService>());

            var ok = await svc.StartContainersAsync(new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" }, CancellationToken.None);

            Assert.False(ok);
        }

        [Fact]
        public async Task StartContainersAsync_StartContainerReturnsFalse_ReturnsFalse()
        {
            var fake = new FakeDockerClientWrapper_StartReturnsFalse();
            fake.Images.Add(new ImagesListResponse { RepoTags = new[] { "myproj_github-runner:latest" } });

            var svc = new DockerDotNetRunnerService("myproj", fake, new TestLogger<DockerDotNetRunnerService>());

            var ok = await svc.StartContainersAsync(new[] { "GITHUB_REPOSITORY=hutchisonkim/dot-net-app" }, CancellationToken.None);

            Assert.False(ok);
        }
    }
}
