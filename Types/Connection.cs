// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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

using System;
using System.Collections;
using System.Net;

using NAnt.Core.Attributes;
using NAnt.Core.Util;
using NAnt.Core.Types;

namespace Sourceforge.NAnt.Ftp.Types {
	/// <summary>
	/// Extends the <see cref="Credential"/> type to provides connection details for connecting to a remote host.
	/// </summary>
	/// <remarks>This class now acts as a wrapper to insulate the third-party libraries.</remarks>
	[ElementName("connection")]
	public class Connection : Credential {
		
		#region Public Instance Constructors
		/// <overloads>
		/// <summary>
		/// Initializes a new instance of the <see cref="Connection" /> class.
		/// </summary>
		/// </overloads>
		/// <summary>Init an empty connection instance.</summary>
		public Connection() {
			Init(String.Empty, String.Empty, String.Empty);
		}
		/// <summary>Init a connection instance with a server name.</summary>
		public Connection(string host)
		{
			Init(host, String.Empty, String.Empty);
		}
		/// <summary>Init a connection instance with a server name and username.</summary>
		public Connection(string host, string user)
		{
			Init(host, user, String.Empty);
		}
		/// <summary>Init a connection instance with a server name, username, and password.</summary>
		public Connection(string host, string user, string pass)
		{
			Init(host, user, pass);
		}
		/// <summary>Internal routine to perform the instance initialization.</summary>
		private void Init(string host, string user, string pass)
		{
			this.Domain = host;
			this.UserName = user;
			this.Password = pass;
		}
		#endregion Public Instance Constructors
		
        /// <summary>
        /// The server target for this connection.
        /// </summary>
        [TaskAttribute("server", Required=true)]
        public string Server {
            get { return Domain; }
            set { Domain = StringUtils.ConvertEmptyToNull(value); }
        }
		
	}
}
