/*
 * Created by SharpDevelop.
 * User: David
 * Date: 12/25/2004
 * Time: 5:02 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

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
using NAnt.Core;

namespace Sourceforge.NAnt.Ftp.Types {
	/// <summary>
	/// Provides a plain text node.
	/// </summary>
	[ElementName("text")]
	public class Text : DataTypeBase {

		private string _text = String.Empty;
		
		#region Public Instance Constructors

		public string InnerText {
			get {return _text;}
			set {_text = value;}
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Text" /> class.
		/// </summary>
		public Text() {
		}
		#endregion Public Instance Constructors		
	}
}
