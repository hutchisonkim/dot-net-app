using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace RunnerTasks.Tests.Fakes
{
    // StartAndAttachExec returns a stream that throws on ReadOutputAsync/ReadAsync to simulate attach read error
    public class FakeDockerClientWrapper_AttachReadError : FakeDockerClientWrapper
    {
        public override Task<Stream> StartAndAttachExecAsync(string execId, bool hijack, CancellationToken cancellationToken)
        {
            var stream = new ThrowingStream();
            return Task.FromResult<Stream>(stream);
        }

        private class ThrowingStream : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new IOException("read failed");
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
