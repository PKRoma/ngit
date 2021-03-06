/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.IO;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Util.IO
{
	[NUnit.Framework.TestFixture]
	public class UnionInputStreamTest
	{
		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestEmptyStream()
		{
			UnionInputStream u = new UnionInputStream();
			NUnit.Framework.Assert.IsTrue(u.IsEmpty());
			NUnit.Framework.Assert.AreEqual(-1, u.Read());
			NUnit.Framework.Assert.AreEqual(-1, u.Read(new byte[1], 0, 1));
			NUnit.Framework.Assert.AreEqual(0, u.Available());
			NUnit.Framework.Assert.AreEqual(0, u.Skip(1));
			u.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestReadSingleBytes()
		{
			UnionInputStream u = new UnionInputStream();
			NUnit.Framework.Assert.IsTrue(u.IsEmpty());
			u.Add(new ByteArrayInputStream(new byte[] { 1, 0, 2 }));
			u.Add(new ByteArrayInputStream(new byte[] { 3 }));
			u.Add(new ByteArrayInputStream(new byte[] { 4, 5 }));
			NUnit.Framework.Assert.IsFalse(u.IsEmpty());
			NUnit.Framework.Assert.AreEqual(3, u.Available());
			NUnit.Framework.Assert.AreEqual(1, u.Read());
			NUnit.Framework.Assert.AreEqual(0, u.Read());
			NUnit.Framework.Assert.AreEqual(2, u.Read());
			NUnit.Framework.Assert.AreEqual(0, u.Available());
			NUnit.Framework.Assert.AreEqual(3, u.Read());
			NUnit.Framework.Assert.AreEqual(0, u.Available());
			NUnit.Framework.Assert.AreEqual(4, u.Read());
			NUnit.Framework.Assert.AreEqual(1, u.Available());
			NUnit.Framework.Assert.AreEqual(5, u.Read());
			NUnit.Framework.Assert.AreEqual(0, u.Available());
			NUnit.Framework.Assert.AreEqual(-1, u.Read());
			NUnit.Framework.Assert.IsTrue(u.IsEmpty());
			u.Add(new ByteArrayInputStream(new byte[] { unchecked((byte)255) }));
			NUnit.Framework.Assert.AreEqual(255, u.Read());
			NUnit.Framework.Assert.AreEqual(-1, u.Read());
			NUnit.Framework.Assert.IsTrue(u.IsEmpty());
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestReadByteBlocks()
		{
			UnionInputStream u = new UnionInputStream();
			u.Add(new ByteArrayInputStream(new byte[] { 1, 0, 2 }));
			u.Add(new ByteArrayInputStream(new byte[] { 3 }));
			u.Add(new ByteArrayInputStream(new byte[] { 4, 5 }));
			byte[] r = new byte[5];
			NUnit.Framework.Assert.AreEqual(3, u.Read(r, 0, 5));
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(new byte[] { 1, 0, 2 }, Slice(r, 3)));
			NUnit.Framework.Assert.AreEqual(1, u.Read(r, 0, 5));
			NUnit.Framework.Assert.AreEqual(3, r[0]);
			NUnit.Framework.Assert.AreEqual(2, u.Read(r, 0, 5));
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(new byte[] { 4, 5 }, Slice(r, 2)));
			NUnit.Framework.Assert.AreEqual(-1, u.Read(r, 0, 5));
		}

		private static byte[] Slice(byte[] @in, int len)
		{
			byte[] r = new byte[len];
			System.Array.Copy(@in, 0, r, 0, len);
			return r;
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestArrayConstructor()
		{
			UnionInputStream u = new UnionInputStream(new ByteArrayInputStream(new byte[] { 1
				, 0, 2 }), new ByteArrayInputStream(new byte[] { 3 }), new ByteArrayInputStream(
				new byte[] { 4, 5 }));
			byte[] r = new byte[5];
			NUnit.Framework.Assert.AreEqual(3, u.Read(r, 0, 5));
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(new byte[] { 1, 0, 2 }, Slice(r, 3)));
			NUnit.Framework.Assert.AreEqual(1, u.Read(r, 0, 5));
			NUnit.Framework.Assert.AreEqual(3, r[0]);
			NUnit.Framework.Assert.AreEqual(2, u.Read(r, 0, 5));
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(new byte[] { 4, 5 }, Slice(r, 2)));
			NUnit.Framework.Assert.AreEqual(-1, u.Read(r, 0, 5));
		}

		[NUnit.Framework.Test]
		public virtual void TestMarkSupported()
		{
			UnionInputStream u = new UnionInputStream();
			NUnit.Framework.Assert.IsFalse(u.MarkSupported());
			u.Add(new ByteArrayInputStream(new byte[] { 1, 0, 2 }));
			NUnit.Framework.Assert.IsFalse(u.MarkSupported());
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSkip()
		{
			UnionInputStream u = new UnionInputStream();
			u.Add(new ByteArrayInputStream(new byte[] { 1, 0, 2 }));
			u.Add(new ByteArrayInputStream(new byte[] { 3 }));
			u.Add(new ByteArrayInputStream(new byte[] { 4, 5 }));
			NUnit.Framework.Assert.AreEqual(0, u.Skip(0));
			NUnit.Framework.Assert.AreEqual(3, u.Skip(3));
			NUnit.Framework.Assert.AreEqual(3, u.Read());
			NUnit.Framework.Assert.AreEqual(2, u.Skip(5));
			NUnit.Framework.Assert.AreEqual(0, u.Skip(5));
			NUnit.Framework.Assert.AreEqual(-1, u.Read());
			u.Add(new _ByteArrayInputStream_168(new byte[] { 20, 30 }));
			NUnit.Framework.Assert.AreEqual(2, u.Skip(8));
			NUnit.Framework.Assert.AreEqual(-1, u.Read());
		}

		private sealed class _ByteArrayInputStream_168 : ByteArrayInputStream
		{
			public _ByteArrayInputStream_168(byte[] baseArg1) : base(baseArg1)
			{
			}

			public override long Skip(long n)
			{
				return 0;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestAutoCloseDuringRead()
		{
			UnionInputStream u = new UnionInputStream();
			bool[] closed = new bool[2];
			u.Add(new _ByteArrayInputStream_182(closed, new byte[] { 1 }));
			u.Add(new _ByteArrayInputStream_187(closed, new byte[] { 2 }));
			NUnit.Framework.Assert.IsFalse(closed[0]);
			NUnit.Framework.Assert.IsFalse(closed[1]);
			NUnit.Framework.Assert.AreEqual(1, u.Read());
			NUnit.Framework.Assert.IsFalse(closed[0]);
			NUnit.Framework.Assert.IsFalse(closed[1]);
			NUnit.Framework.Assert.AreEqual(2, u.Read());
			NUnit.Framework.Assert.IsTrue(closed[0]);
			NUnit.Framework.Assert.IsFalse(closed[1]);
			NUnit.Framework.Assert.AreEqual(-1, u.Read());
			NUnit.Framework.Assert.IsTrue(closed[0]);
			NUnit.Framework.Assert.IsTrue(closed[1]);
		}

		private sealed class _ByteArrayInputStream_182 : ByteArrayInputStream
		{
			public _ByteArrayInputStream_182(bool[] closed, byte[] baseArg1) : base(baseArg1)
			{
				this.closed = closed;
			}

			public override void Close()
			{
				closed[0] = true;
			}

			private readonly bool[] closed;
		}

		private sealed class _ByteArrayInputStream_187 : ByteArrayInputStream
		{
			public _ByteArrayInputStream_187(bool[] closed, byte[] baseArg1) : base(baseArg1)
			{
				this.closed = closed;
			}

			public override void Close()
			{
				closed[1] = true;
			}

			private readonly bool[] closed;
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestCloseDuringClose()
		{
			UnionInputStream u = new UnionInputStream();
			bool[] closed = new bool[2];
			u.Add(new _ByteArrayInputStream_213(closed, new byte[] { 1 }));
			u.Add(new _ByteArrayInputStream_218(closed, new byte[] { 2 }));
			NUnit.Framework.Assert.IsFalse(closed[0]);
			NUnit.Framework.Assert.IsFalse(closed[1]);
			u.Close();
			NUnit.Framework.Assert.IsTrue(closed[0]);
			NUnit.Framework.Assert.IsTrue(closed[1]);
		}

		private sealed class _ByteArrayInputStream_213 : ByteArrayInputStream
		{
			public _ByteArrayInputStream_213(bool[] closed, byte[] baseArg1) : base(baseArg1)
			{
				this.closed = closed;
			}

			public override void Close()
			{
				closed[0] = true;
			}

			private readonly bool[] closed;
		}

		private sealed class _ByteArrayInputStream_218 : ByteArrayInputStream
		{
			public _ByteArrayInputStream_218(bool[] closed, byte[] baseArg1) : base(baseArg1)
			{
				this.closed = closed;
			}

			public override void Close()
			{
				closed[1] = true;
			}

			private readonly bool[] closed;
		}

		[NUnit.Framework.Test]
		public virtual void TestExceptionDuringClose()
		{
			UnionInputStream u = new UnionInputStream();
			u.Add(new _ByteArrayInputStream_236(new byte[] { 1 }));
			try
			{
				u.Close();
				NUnit.Framework.Assert.Fail("close ignored inner stream exception");
			}
			catch (IOException e)
			{
				NUnit.Framework.Assert.AreEqual("I AM A TEST", e.Message);
			}
		}

		private sealed class _ByteArrayInputStream_236 : ByteArrayInputStream
		{
			public _ByteArrayInputStream_236(byte[] baseArg1) : base(baseArg1)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				throw new IOException("I AM A TEST");
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestNonBlockingPartialRead()
		{
			InputStream errorReadStream = new _InputStream_251();
			UnionInputStream u = new UnionInputStream(new ByteArrayInputStream(new byte[] { 1
				, 2, 3 }), errorReadStream);
			byte[] buf = new byte[10];
			NUnit.Framework.Assert.AreEqual(3, u.Read(buf, 0, 10));
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(new byte[] { 1, 2, 3 }, Slice(buf, 3)
				));
			try
			{
				u.Read(buf, 0, 1);
				NUnit.Framework.Assert.Fail("Expected exception from errorReadStream");
			}
			catch (IOException e)
			{
				NUnit.Framework.Assert.AreEqual("Expected", e.Message);
			}
		}

		private sealed class _InputStream_251 : InputStream
		{
			public _InputStream_251()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read()
			{
				throw new IOException("Expected");
			}
		}
	}
}
