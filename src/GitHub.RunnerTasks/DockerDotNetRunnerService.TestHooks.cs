using System;

namespace GitHub.RunnerTasks
{
    // Separated into its own partial to keep the main service lean; intended for tests only.
    public partial class DockerDotNetRunnerService
    {
        // Test helpers: allow tests to set internal state without reflection
        public void Test_SetInternalState(string? containerId, string? lastRegistrationToken)
        {
            _containerId = containerId;
            _lastRegistrationToken = lastRegistrationToken;
        }

        public (string? containerId, string? lastRegistrationToken) Test_GetInternalState()
        {
            return (_containerId, _lastRegistrationToken);
        }

        public void Test_SetImageTag(string? tag)
        {
            _imageTagInUse = tag;
        }

        // Test helper: allow tests to set the created volume name so StopContainersAsync attempts removal
        public void Test_SetCreatedVolumeName(string? name)
        {
            _createdVolumeName = name;
        }

        public void Test_SetLogWaitTimeout(TimeSpan t)
        {
            // allow tests to shorten the waiting period
            typeof(DockerDotNetRunnerService)
                .GetField("_logWaitTimeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(this, t);
        }
    }
}
