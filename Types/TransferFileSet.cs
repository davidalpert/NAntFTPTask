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
	
using Sourceforge.NAnt.Ftp.Tasks;
	
namespace Sourceforge.NAnt.Ftp.Types {

	public enum TransferDirection {
		NONE = 0,
		PUT,
		GET
	}
	
	[ElementName("put")]
	public class PutFileSet : TransferFileSet {
		public PutFileSet() {
			this.Direction = TransferDirection.PUT;
		}
	}

	[ElementName("get")]
	public class GetFileSet : TransferFileSet {
		public GetFileSet() {
			this.Direction = TransferDirection.GET;
		}
	}

	/// <summary>
	/// Provides credentials for connecting to a remote host.
	/// </summary>
	[ElementName("transferfileset")]
	public class TransferFileSet : FileSet {

		#region Private Instance Methods
		private string _transferType;
		private bool _ifDefined=true;
		private bool _unlessDefined;
		private TransferDirection _transferDirection;
		#endregion
		
		#region Public Instance Constructors
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Connection" /> class.
		/// </summary>
		public TransferFileSet() {
			this.Direction = TransferDirection.NONE;
		}
		#endregion Public Instance Constructors
		       
		public TransferDirection Direction {
			get {return _transferDirection;}
			set {_transferDirection = value;}
		}

		#region Type Attributes
		
        /// <summary>
        /// The transfer type for this fileset.
        /// </summary>
        [TaskAttribute("transfertype", Required=false)]
        public string TransferType {
        	get { return _transferType; }
        	set { FTPTask.ParseTransferType(value);
        		  // if ParseTransferType does not throw an exception,
        		  // then this value is valid.
        		  _transferType = value; }
        }

        /// <summary>
        /// Short-form access to <see cref="TransferType">TransferType</see> for this fileset.
        /// </summary>
        [TaskAttribute("type", Required=false)]
        public string Type {
        	get { return this.TransferType; }
        	set { this.TransferType = value;}
        }
        
        #endregion

		#region If/Unless Attributes
        /// <summary>
        /// If <see langword="true" /> then the pattern will be included; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        /// <remarks>Copied from the <see cref="Include">Include</see> class in the NAnt-0.85-rc1 distribution.</remarks>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Opposite of <see cref="IfDefined" />. If <see langword="false" /> 
        /// then the pattern will be included; otherwise, skipped. The default 
        /// is <see langword="false" />.
        /// </summary>
        /// <remarks>Copied from the <see cref="Include">Include</see> class in the NAnt-0.85-rc1 distribution.</remarks>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion
	}
}