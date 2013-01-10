//
// Copyright (C) 2007-2013 Kody Brown (kody@bricksoft.com).
// 
// MIT License:
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace SystemPathEditor
{
	public class Path
	{
		public static List<string> Split( string path )
		{
			if (null == path || 0 == path.Length) {
				return new List<string>();
			}
			string[] p = path.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			return new List<string>(p);
		}

		public static string Combine( string[] path )
		{
			if (null == path || 0 == path.Length) {
				return string.Empty;
			}
			return Combine(new List<string>(path));
		}

		public static string Combine( List<string> path )
		{
			if (null == path || 0 == path.Count) {
				return string.Empty;
			}
			StringBuilder pb = new StringBuilder();
			foreach (string p in path) {
				pb.Append(p).Append(";");
			}
			return pb.ToString();
		}
	}
}
