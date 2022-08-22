// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Threading;

namespace Imazen.Profiling
{
    public class InstrumentedMemoryStream : MemoryStream
    {
        public InstrumentedMemoryStream(byte[] buffer) : base(buffer)
        {
            BytesRead = 0;
        }

        public long BytesRead { get; set; }

        public int? SleepMsPerReadCall { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (SleepMsPerReadCall != null && SleepMsPerReadCall.Value > 0) Thread.Sleep(SleepMsPerReadCall.Value);
            var read = base.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override int ReadByte()
        {
            if (SleepMsPerReadCall != null && SleepMsPerReadCall.Value > 0) Thread.Sleep(SleepMsPerReadCall.Value);
            BytesRead++;
            return base.ReadByte();
        }
    }
}