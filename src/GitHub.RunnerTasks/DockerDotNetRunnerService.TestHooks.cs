using System;

namespace GitHub.RunnerTasks
{
    // Test hooks remain to keep tests compiling; they are no-ops with local backing fields in the stub.
    public partial class DockerDotNetRunnerService
    {
        private string? __test_containerId;
        private string? __test_lastRegistrationToken;
        private string? __test_imageTagInUse;
        private string? __test_createdVolumeName;

        public void Test_SetInternalState(string? containerId, string? lastRegistrationToken)
        {
            __test_containerId = containerId;
            __test_lastRegistrationToken = lastRegistrationToken;
        }

        public (string? containerId, string? lastRegistrationToken) Test_GetInternalState()
        {
            return (__test_containerId, __test_lastRegistrationToken);
        }

        public void Test_SetImageTag(string? tag) => __test_imageTagInUse = tag;
        public void Test_SetCreatedVolumeName(string? name) => __test_createdVolumeName = name;
        public void Test_SetLogWaitTimeout(TimeSpan t) { /* no-op in stub */ }
    }
}
