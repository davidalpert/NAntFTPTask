// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// David Alpert (david@spinthemoose.com)

using NAnt.Core;

namespace Sourceforge.NAnt.Ftp {
	
	public class FTPTaskException : BuildException {
		public FTPTaskException():base() {}
		public FTPTaskException(string msg):base(msg) {}
		public FTPTaskException(string msg, System.Exception InnerEx):base(msg,InnerEx) {}
		public FTPTaskException(string msg, Location loc):base(msg, loc) {}
		public FTPTaskException(string msg, Location loc, System.Exception InnerEx):base(msg, loc, InnerEx) {}
	}
}
