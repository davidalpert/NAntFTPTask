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
using System.IO;
using System.Net;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

using EnterpriseDT.Net.Ftp;
using EDTLevel = EnterpriseDT.Util.Debug.Level;
using EDTLogger = EnterpriseDT.Util.Debug.Logger;

using PasswordInputManager = ConsolePasswordInput.ConsolePasswordInput;

using Sourceforge.NAnt.Ftp.Types;
using Sourceforge.NAnt.Ftp.Enum;
using Sourceforge.NAnt.Ftp.Util;
using Sourceforge.NAnt.Ftp.Tasks;

using Get = Sourceforge.NAnt.Ftp.Types.GetFileSet;
using Put = Sourceforge.NAnt.Ftp.Types.PutFileSet;

namespace Sourceforge.NAnt.Ftp.Tasks {
	
    /// <summary>
    /// Transfer files over an FTP connection.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   When you use an <see cref="FTPTask"/> task, you must specify 
    ///   connection details, either through including a <see cref="Connection"/> 
    ///   element as a child of the ftp task or through specifying in the <b>connection</b> attribute 
    ///   the <b>id</b> of a previously declared <see cref="Connection"/> element.
    ///   </para>
    ///   <para>
    ///   The <b>up-ascii</b>, <b>up-binary</b>, <b>down-ascii</b>, and <b>down-binary</b>
    ///   children have been depricated in favor of multiple <see cref="TransferFileSet" /> elements that are 
    ///   included through any number of <see cref="Get"/> and <see cref="Put"/> elements.
    ///   </para>
    ///   <para>
    ///   Transfer type (Ascii/Binary) is now specified as through the <b>Type</b> attribute of the <see cref="Get"/> and <see cref="Put"/> elements.
    ///   </para>
    ///   <h4>Include Scanning</h4>
    ///   <para>
    ///   <see cref="Put"/> elements are a derived version of <see cref="TransferFileSet"/> that scan
    ///   local directories just as a <see cref="FileSet"/> does.
    ///   </para>
    ///   <para>
    ///   <see cref="Get"/> elements are a derived version of <see cref="TransferFileSet"/> that process
    ///   include statements using the same algorithm as a <see cref="FileSet"/> only they scan
    ///   <b>remote</b> directories on the server.  This allows you to use NAnt's 
    ///   <see cref="FileSet"/> pattern matching to selectively get a batch of files from
    ///   a remote filesystem.
    ///   </para>
    ///   <note>
    ///   <see cref="Get"/> and <see cref="Put"/> children are processed in the order 
    ///   they are listed.  All paths in these elements and their include statements are
    ///   to the ftp node's localpath and remotepath attributes.
    ///   </note>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Put a single file to a remote host.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <connection id="myconn" server="upload.sourceforge.net" username="anonymous" password="david@spinthemoose.com" />
    /// <ftp  
    ///     connection="myconn"
    ///     remotedir="incoming"
    ///     localdir="c:\dev\nantftptask\trunk"
    ///     >
    ///     <put type="ascii">
    ///        <include name="readme.txt" />
    ///     </put>
    /// </ftp>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Put a set of binary files to a .</para>
    ///   <code>
    ///     <![CDATA[
    ///		<connection id="sourceforge 
    ///			    server="upload.sourceforge.net" 
    ///			    username="anonymous" 
    ///			    password="david@spinthemoose.com" />
    ///		<ftp connection="sourceforge" >
    ///			<put type="bin" localdir="." remotedir="incoming" flatten="true">
    ///				<include name="${ziproot}*.zip"/>
    ///			</put>
    ///		</ftp>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Debug a request to copy all php files from a remote directory.  This will list
    ///   the files included as the remote directories are scanned.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///		
    ///		    <connection id="myConn" server="${remotehost}" username="${username}" />
    ///	
    /// 	    <ftp debug="true"
    /// 	    	 connection="myConn"
    /// 	    	 remotedir="${remote-dir}"
    /// 	    	 showdironconnect="false" 
    /// 	    	 >
    ///				<get type="ascii" localdir="incoming" remotedir="." failonempty="true">
    ///					<include name="**\*.php" />
    ///				</get>
    ///		    </ftp>
    ///     ]]>
    ///   </code>
    ///   <note>
    ///   When a password is not supplied, the connection is established then 
    ///   logging output asks for password input from the console.  Any text
    ///   typed when answering this reques is masked thanks to <b>Lim Bio Liong's</b> 
    ///   CodeProject article <i>'.NET Console Password Input By Masking 
    ///   Keyed-In Characters'</i> 
    ///   (<see href="http://www.codeproject.com/dotnet/ConsolePasswordInput.asp">http://www.codeproject.com/dotnet/ConsolePasswordInput.asp</see>
    ///    - accessed on 12/18/2004) 
    ///   </note>
    /// </example>
	[TaskName("ftp")]
	public class FTPTask : TaskContainer {
	
		#region constants
		/// <summary>Null argument</summary>
		private const string EXCEPTION_NULL_STRING = "Argument was null";
		/// <summary>Empty string</summary>
		private const string EXCEPTION_EMPTY_STRING = "Argument was empty string";
		/// <summary>""</summary>
		private const string EMPTY_STRING = "";
		/// <summary>/</summary>
		private const char DIR_SEPERATOR = '/';
		/// <summary>Dir seperator on dos/win</summary>
		private const char DOS_DIR_SEPERATOR = '\\';
		/// <summary>Standard ftp port</summary>
		private const int DEFAULT_FTP_PORT = 21;
		/// <summary>user for anon ftp</summary>
		private const string ANONYMOUS_USER = "anonymous";
		/// <summary>pass for anon ftp</summary>
		private const string ANONYMOUS_PASS = "anonymous@unknown.org";
		/// <summary>the name of the script block</summary>
		private const string SCRIPT_NODE_NAME = "script";
		/// <summary>the name of a put block</summary>
		private const string PUT_NODE_NAME = "nant:put";
		/// <summary>the name of a get block</summary>
		private const string GET_NODE_NAME = "nant:get";
		/// <summary>the name of a transfer block</summary>
		private const string TRANSFER_NODE_XPATH = "get | put";
		
		
		#endregion

		#region private member variables
		private string 	_server 	= EMPTY_STRING;
		private int 	_port 		= DEFAULT_FTP_PORT;
		
		private string 	_user 		= EMPTY_STRING;
		private string 	_password 	= EMPTY_STRING;
		
		private string 	_remotePath = EMPTY_STRING;
		private string 	_localPath 	= ".";
		private bool	_showDirOnConnect = false;
		private bool    _createDirsOnDemand = false;
		private bool	_logfiles	= true;
		private bool	_exec		= true;
		
		private ConnectMode 	_connectMode = 0;
		private Connection 		_connection	 = null;
		private FTPClient  		_client 	 = null;

		private ArrayList _putSets 		= new ArrayList();
		private ArrayList _getSets		= new ArrayList();
		private ArrayList _transferList = new ArrayList();
		
		private FileSet _uploadAscii 	= new FileSet();
		private FileSet _uploadBinary 	= new FileSet();
		private FileSet _downloadAscii 	= new FileSet();
		private FileSet _downloadBinary = new FileSet();
		
		private string _script = EMPTY_STRING;
		#endregion

		#region constructor
		public FTPTask() {
			// set default connection parameters
			_connection = new Connection(null, 
                                         ANONYMOUS_USER, 
                                         ANONYMOUS_PASS);
						
			ConnectMode = "passive";
			
			return;
		} // FTPTask()
		#endregion

		#region /////////////////////// Task Properties

		public FTPClient Client { get {return _client;} }
		
		/// <summary>
		/// The port number to connect to.  Default is 21.
		/// </summary>
		[TaskAttribute("port", Required=false)]
		[Int32Validator(1,65535)]
		public int port {
			get {
				return _port;
			} set {
					_port = value;
				}
		} // port

		/// <summary>
		/// The mode to connect as.  One of 'ACTIVE' or 'PASSIVE'.
		/// </summary>
		[TaskAttribute("connectmode", Required=false)]
		public string ConnectMode {
			get {
				return _connectMode.ToString();
			} set {
				_connectMode = Connection.ParseConnectMode(value);
			}
		} // port
		
		/// <summary>
		/// The id of a Connection element that specifies the details for this connection.
		/// </summary>
		[TaskAttribute("connection", Required=false)]
		public string ConnectionName {
			get {
				return _connection.RefID;
			} set {
				DereferenceConnectionAttribute(value);
			}
		} // server
		
		
		/// <summary>
		/// If <b>true</b> we create target directories as needed when saving files.  If <b>false</b>, attempting to access a directory that does not exist generates an exception.  Default is <b>true</b>.</summary>
		[TaskAttribute("createdirs", Required=false)]
		[BooleanValidator()]
		public bool createDirsOnDemand {
			get {
				return _createDirsOnDemand;
			} set {
					_createDirsOnDemand = value;
				}
		} // createDirsOnDemand

		/// <summary>The directory to use as a base path on the remote server.  If this path is relative, it is relative to the connection's remotepath.  Default is the default connection directory.</summary>
		[TaskAttribute("remotedir", Required=false)]
		public string remotepath {
			get {
				return _remotePath;
			} set {
				_remotePath = RPath.Clean(value);
			}
		} // remotepath
		
		/// <summary>The directory to use as a base path on the local filesystem.</summary>
		[TaskAttribute("localdir", Required=false)]
		public string localpath {
			get {
				return _localPath;
			} set {
					_localPath = value;
				}
		} // localpath

		/// <summary>If this is <b>true</b>, execute the file transfers.  If it is <b>false</b>, establish the remote connection but do not transfer any files or modify local or remote directory structures.  Instead, display a list of the files included in the transfer operation.  This is useful for debugging include patterns on either local or remote filesystems.  Default is true.</summary>
		[TaskAttribute("exec", Required=false)]
		[BooleanValidator()]
		public bool Exec {
			get {
				return _exec;
			} set {
					_exec = value;
				}
		} // localpath

		/// <summary>If this is <b>true</b>, each file transfer is individually recorded in logging output.  Default is <b>true</b>.</summary>
		[TaskAttribute("logfiles", Required=false)]
		[BooleanValidator()]
		public bool LogFiles {
			get {
				return _logfiles;
			} set {
					_logfiles = value;
				}
		} // localpath

		/// <summary>If this property is <b>true</b>, a directory listing of the remotepath is sent to the logging output after the connection is established.  Default is <b>false</b>.</summary>
		[TaskAttribute("showdironconnect", Required=false)]
		public bool ShowDirOnConnect {
			get {
				return _showDirOnConnect;
			} set {
					_showDirOnConnect = value;
				}
		} // localpath

		/// <summary>Gets a bool flag indicating whether or not the Server member has been set.</summary>
		/// <value><b>True</b> if the Server string is not null and is not empty; else <b>false</b>.</value>
		public bool ServerIsSet {
			get { return _server!=null && !_server.Equals(String.Empty); }
		}
		/// <summary>Gets a bool flag indicating whether or not the connection is open.</summary>
		/// <value><b>True</b> if the client exists; else <b>false</b>.</value>
		public bool IsConnected {
			get { return _connection.IsConnected;}
		}

		public Level LevelExec {
			get { 
				if (this.Exec) {
					// hide the extra logging info unless the user specifically wants it
					return Level.Verbose;
				} else {
					// we are debugging ftp includes so we need more logging info
					return Level.Info;
				}
			}
		}
		public Level LevelLogfiles {
			get { 
				if (this.LogFiles) {
					// we want more logging info
					return Level.Info;
				} else {
					// hide the extra logging info unless the user specifically wants it
					return Level.Verbose;
				}
			}
		}
		#endregion

		#region /////////////////////// Build Elements		
		
		[BuildElement("script")]
		public Text Script {
			get {Text t = new Text();
				 t.InnerText = _script;
				 return t;}
			set {_script = ((Text)value).InnerText;}
		}
		
		/// <summary>The network credentials used for authenticating the request with
		/// the Internet resource.</summary>
		[BuildElement("connection")]
		public Connection Connection {
			get { return _connection; }
			set { _connection = value; }
		} // credentials

		/// <summary>
		/// <para>
		/// A PutFileSet (derived from <see cref="TransferFileSet"/>) 
		/// that contains a list of <b>Include</b> statements that are 
		/// processed (scanned) against the local filesystem relative 
		/// to the <b>localpath</b> value.
		/// </para>
		/// <para>
		/// Multiple &lt;put&gt; elements may be included in an 
		/// &lt;ftp&gt; task.  They are processed with &lt;get&gt; 
		/// elements in the order of inclusion.
		/// </para>
		/// </summary>
		[BuildElementArray("put")]
		public PutFileSet[] PutSets {
            set {
// these accessors are ignored.  puts and gets are grabbed from the XML the InitTask routine.
//                foreach (TransferFileSet putset in value) {
//                    if (putset.IfDefined && !putset.UnlessDefined) {
//						_putSets.Add(putset);
//					}
//				}
//				this.Log(Level.Info, "Found {0} put sets.", _putSets.Count);				
			}
		}
		/// <summary>
		/// <para>
		/// A Get (derived from <see cref="TransferFileSet"/>) 
		/// that contains a list of <b>Include</b> statements that are 
		/// processed (scanned) against the remote filesystem relative 
		/// to the <b>remotepath</b> value.
		/// </para>
		/// <para>
		/// Multiple &lt;get&gt; elements may be included in an 
		/// &lt;ftp&gt; task.  They are processed with &lt;put&gt; 
		/// elements in the order of inclusion.
		/// </para>
		/// </summary>
		[BuildElementArray("get")]
		public GetFileSet[] GetSets {
            set {
// these accessors are ignored.  puts and gets are grabbed from the XML the InitTask routine.
//                foreach (TransferFileSet getset in value) {
//                    if (getset.IfDefined && !getset.UnlessDefined) {
//						_getSets.Add(getset);
//					}
//				}
//				this.Log(Level.Info, "Found {0} get sets.", _getSets.Count);
			}
		}
		/// <summary>Use &lt;put mode='ascii' /&gt; instead.</summary>
		[BuildElement("up-ascii")]
        [Obsolete("Use the <put mode='ascii' /> element instead.", false)]
		public FileSet UploadAscii {
			get {
				return _uploadAscii;
			} set {
					_uploadAscii = value;
				}
		} // UploadAscii

		/// <summary>Use &lt;put mode='bin' /&gt; instead.</summary>
		[BuildElement("up-binary")]
        [Obsolete("Use the <put mode='bin' /> element instead.", false)]
		public FileSet UploadBinary {
			get {
				return _uploadBinary;
			} set {
					_uploadBinary = value;
				}
		} // Upload

		/// <summary>Use &lt;get mode='ascii' /&gt; instead.</summary>
		[BuildElement("down-ascii")]
        [Obsolete("Use the <get mode='ascii' /> element instead.", false)]
		public FileSet DowloadAscii {
			get {
				return _downloadAscii;
			} set {
					_downloadAscii = value;
				}
		} // DownloadAscii

		/// <summary>Use &lt;get mode='bin' /&gt; instead.</summary>
		[BuildElement("down-binary")]
        [Obsolete("Use the <get mode='bin' /> element instead.", false)]
		public FileSet DowloadBinary {
			get {
				return _downloadBinary;
			} set {
					_downloadBinary = value;
				}
		} // DownloadBinary

		#endregion
		
		#region /////////////////////// Task functions
		protected override void InitializeTask(System.Xml.XmlNode taskNode) {
			base.InitializeTask (taskNode);

			// grab the script block
			XmlNodeList scriptList = taskNode.SelectNodes(SCRIPT_NODE_NAME, NamespaceManager);
			if (0 == scriptList.Count) {
				_script = EMPTY_STRING;
			} else {
				if (1 == scriptList.Count) {
					_script = scriptList.Item(0).InnerText;
				} else {
					throw new BuildException("Only one <ftpscript> block allowed.", Location);
				} // if
			} // if
			scriptList = null;

			// grab the put and get filestatements
			XmlNodeList transferList = taskNode.SelectNodes(TRANSFER_NODE_XPATH, NamespaceManager);
			TransferFileSet aSet = null;
			this.Log(Level.Debug, "transferList.Count: {0}", transferList.Count);
			foreach (XmlNode node in transferList) {
				this.Log(Level.Debug, "- found a {0} set", node.Name);
				aSet = (TransferFileSet)TypeFactory.CreateDataType(node,this.Project);
	            aSet.Parent = this;
	            aSet.NamespaceManager = NamespaceManager;
	            aSet.Initialize(node);
	            if (aSet.IfDefined && !aSet.UnlessDefined) {
		            this._transferList.Add(aSet);
		            this.Log(Level.Debug, "  and added it to our _transferList (count now at {0})", _transferList.Count);
	            }
			}
			
			return;
		} // InitializeTask()		
		
		protected override void ExecuteTask() {
			Work();
			return;
		} // ExecuteTask()
		#endregion
		
		#region /////////////////////// Core Implementation
		private void Work() {
			
			this.Log(Level.Verbose, "Connection set:\n\tserver\t- {0}\n\tuser  \t- {1}\n\tpass  \t- {2}",_server, _user, _password);
			
			try {
				_connection.Open(this);
				
				string pwd = PWD;
				CWD(_remotePath);
		
				if (_showDirOnConnect) {
					_connection.ShowDir(".");
				}
				
				if (PWD!=pwd) {
					// we've changed a directory and done some output, so let's insert a line break.
					this.Log(Level.Info,"");
				}
								
				this.ExecuteChildTasks();
	
				DoTransfers();
				
	//				this.Log((Level.Debug, "{3}Binaries{0}{3}  up   {1}{0}{3}  down {2}", System.Environment.NewLine, _uploadBinary.FileNames.Count, _downloadBinary.Includes.Count, this.LogPrefix);
	//				this.Log(Level.Debug, "{3}Asciis{0}{3}  up   {1}{0}{3}  down {2}", System.Environment.NewLine, _uploadAscii.FileNames.Count, _downloadAscii.Includes.Count, this.LogPrefix);
	
					foreach(string fileName in _uploadBinary.FileNames) {
						try {
							uploadFile(fileName, FTPTransferType.BINARY);
						} catch (Exception ex) {
							this.Log(Level.Info, "{0} could not be uploaded.\n{1}", fileName, ex.Message);
						} // try
					} // foreach
	
					foreach(string fileName in _uploadAscii.FileNames) {
						try {
							uploadFile(fileName, FTPTransferType.ASCII);
						} catch (Exception ex) {
							this.Log(Level.Info, "{0} could not be uploaded.\n{1}", fileName, ex.Message);
						} // try
					} // foreach
	
					foreach(string fileName in _downloadBinary.Includes) {
						try {
							downloadFile(fileName, FTPTransferType.BINARY);
						} catch (Exception ex) {
							this.Log(Level.Info, "{0} could not be downloaded.\n{1}", fileName, ex.Message);
						} // try
					} // foreach
	
					foreach(string fileName in _downloadAscii.Includes) {
						try {
							downloadFile(fileName, FTPTransferType.ASCII);
						} catch (Exception ex) {
							this.Log(Level.Info, "{0} could not be downloaded.\n{1}", fileName, ex.Message);
						} // try
					} // foreach
	
					script();
	
			} catch (FTPException ex) {
				this.Log(Level.Error, ex.Message);
				throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
				                                       "---- FTP Exception ----"),
	            						 this.Location, 
	            						 ex);
			} finally {
				_connection.Close();
			}		
			return;
		} // Work()
		#endregion

		#region ftp connect/disconnect functions


		#endregion

		#region ftp directory functions
		
		public string PWD {
			get { 
				return _connection.PWD;
			}
		}
		
		public void CWD_Quiet(string path) {
			_connection.CWD(path, Level.Verbose);
		}
		public void CWD(string path, Level loglevel) {
			_connection.CWD(path, loglevel);
		}
		public void CWD(string path) {
			_connection.CWD(path, Level.Info);
		}
//		public void ChDir(string path, Level level) {
//			path = RPath.Clean(path);
//			if (path!=String.Empty && path!="." && path!=PWD) {
//				this.Log(level, " + Changing remote directory to '{0}'", path);
//				if (IsConnected) {
//					this.Log(Level.Verbose, " +   Attempting CWD...");
//					_client.ChDir(path);
//					this.Log(Level.Verbose, " +   CWD successful.");
//				} else {
//					this.Log(Level.Debug, " + Not connected.");
//				}
//			}
//		}

		public void CWD(string path, bool createOnDemand) {
			_connection.CWD(path, createOnDemand);
//			if (createOnDemand || this.createDirsOnDemand) {
//				try {
//					CWD(path, this.LevelExec);
//				} catch (FTPException fex) {
//					this.Log(this.LevelExec, fex.Message);
//					this.Log(this.LevelExec, " + Creating {0} remotely.", PWD+path);
//					MkDir(path);
//					CWD_Quiet(path);
//				}
//			} else {
//				CWD(path, this.LevelExec);
//			}
		}

//		public FTPFile[] DirDetails(string path) {
//			if (IsConnected) {
//				return _client.DirDetails(path);
//			} else {
//				FTPFile[] result = {};
//				return result;
//			}
//		}
//		public string[] DirDetails(string path, bool full) {
//			if (IsConnected) {
//				return _client.Dir(path, full);
//			} else {
//				string[] result = {};
//				return result;
//			}
//		}
//		
//		public RemotePath[] GetDirs(string path) {
//			RemotePath[] list = RemotePath.FromFTPFileArray(path, DirDetails(path));
//			int dircount = 0;
//			foreach (RemotePath rpath in list) {
//				if (rpath.IsDir) {
//					dircount++;
//				}
//			}
//			if (dircount==0) {
//				RemotePath[] result = {};
//				return result;
//			} else {
//				RemotePath[] result = new RemotePath[dircount];
//				int z=0;
//				foreach(RemotePath rpath in list) {
//					if (rpath.IsDir) {
//						result[z++] = rpath;
////						result[z].File   = rpath.File;
////						result[z++].Path = rpath.Path;
//					}
//				}
//				return result;
//			}
//			
//		}
		public string ResolvePath(string rpath){
			string pwd = PWD;
			string result;
			try {
				CWD_Quiet(rpath);
				result = PWD;
			} catch (FTPException fex) {
				fex.ToString();
				// path does not exist
				result = String.Empty;
			} finally {
				CWD_Quiet(pwd);
			}
			return result;
		}

//		public RemotePath[] GetFiles(string path) {
//			RemotePath[] list = RemotePath.FromFTPFileArray(path, DirDetails(path));
//			int filecount = 0;
//			foreach (RemotePath rpath in list) {
//				if (rpath.IsFile) {
//					filecount++;
//				}
//			}
//			if (filecount==0) {
//				RemotePath[] result = {};
//				return result;
//			} else {
//				RemotePath[] result = new RemotePath[filecount];
//				int z=0;
//				foreach(RemotePath rpath in list) {
//					if (rpath.IsFile) {
//						result[z++] = rpath;
////						result[z].File   = rpath.File;
////						result[z++].Path = rpath.Path;
//					}
//				}
//				return result;
//			}
//			
//		}
//		
//		public void ShowDir(string dir) {
//			this.Log(Level.Info, "Remote Directory Listing:");
//			if (IsConnected) {
//				string[] dirlist = DirDetails(dir, true);
//				foreach(string itemname in dirlist) {
//					this.Log(Level.Info, " + : {0}",itemname);
//				}
//			} else {
//				this.Log(Level.Debug, "Not connected.");
//			}			
//		}
		
		public void MkDir(string dirname) {
			if (IsConnected) {
				_client.MkDir(dirname);
			}
		}
		
		public bool remoteFileExists(string remotePath) {
			bool exist = false;
			if (IsConnected) {
				FTPFile[] dirlist = _client.DirDetails(Path.GetDirectoryName(remotePath));
				foreach (FTPFile node in dirlist) {
					if (node.Dir==false && node.Name==Path.GetFileName(remotePath)) {
						exist = true;
					}
				}
			}
			return exist;
		}
		
		public bool remoteDirExists(string remoteDir) {
			bool exist = true;
			if (remoteDir!=".") {
				if (IsConnected) {
					string pwd = PWD;
					try {
						CWD_Quiet(remoteDir);
					} catch (FTPException fex) {
						fex.ToString();
						exist = false;
					} finally {
						CWD_Quiet(pwd);
					}
				} else {
					exist = false;
				}
			}
			return exist;
		}
		
		#endregion
		
		#region ftp file functions

		// process the transferfilesets
		private void DoTransfers() {
			foreach (TransferFileSet tfs in _transferList) {
				string dirtext = "transmitted";
				string dir = "Put";
				string from = tfs.LocalPath.ToString();
				string to = _server+":/"+RPath.Combine(PWD,tfs.RemotePathString);
				
				if (tfs.Direction==TransferDirection.GET) {
					dirtext = "received";
					dir = "Get";
					from = to;
					to = tfs.LocalPath.ToString();
				}
				
				if (!_exec) {
					dirtext = "included";
				}

				this.Log(Level.Info, "<{0}>.", dir);
				this.Log(Level.Info, "  from: "+from);
				this.Log(Level.Info, "    to: "+to);
				this.Log(Level.Info, "    as: "+FTPTask.ParseTransferType(tfs.TransferType));
				
				tfs.Transfer(this);
				
				this.Log(Level.Info, "</{0}>: {1} files {2}.\n", dir, tfs.NumFiles, dirtext);
			}
		}

//		// put one file
//		public void Put(string fileName,
//		                 string localpath, 
//		                 string remotepath,
//		                 FTPTransferType FtpType, 
//		                 bool flatten, 
//		                 bool createDirsOnDemand,
//		                 bool updateOnly) {
//
//			char [] dirseps = {DOS_DIR_SEPERATOR, DIR_SEPERATOR};
//
//			string localFilePath = String.Empty;	// path to 'fileName' locally, relative to 'localpath'
//			string remoteFilePath = String.Empty;	// path to 'fileName' remotely, relative to 'remotepath'
//			
//			// convert fileName into relative paths...
//			if (Path.GetDirectoryName(fileName).Length==0) {
//				
//				localFilePath = Path.GetFileName(fileName);
//				
//			} else if (Path.GetDirectoryName(fileName).StartsWith(localpath)) {
//				
//				// our abs path is longer than localpath, 
//				// so the relative path is simple.
//			
//				localFilePath = fileName.Replace(localpath, String.Empty).Remove(0,1);
//			
//			} else if (localpath.StartsWith(Path.GetDirectoryName(fileName))) {
//
//				// our abs path is shorter than the localpath
//				// so our relative path is preceded by '..' references.
//				
//				localpath = localpath.Replace(Path.GetDirectoryName(fileName), String.Empty);
//
//				int z = -1;
//
//				z = localpath.IndexOfAny(dirseps);
//				while( z >-1 ) {
//					localFilePath += ".."+Path.DirectorySeparatorChar;
//					localpath = localpath.Remove(z,1);
//					z = localpath.IndexOfAny(dirseps);
//				}
//				localFilePath += Path.GetFileName(fileName);
//			}
//			
//			// mirror the remote file path from the local file path, flattening if requested
//			if (flatten) {
//				remoteFilePath = Path.GetFileName(localFilePath);
//			} else {
//				remoteFilePath = localFilePath;
//			}
//			
//			remoteFilePath = remoteFilePath.Replace(DOS_DIR_SEPERATOR, DIR_SEPERATOR);
//			
//			this.Log(this.LevelLogfiles, "  file: {0}", localFilePath);
//			this.Log(this.LevelExec,         "    to: {0}", Path.Combine(remotepath, remoteFilePath));
//
//			if (IsConnected) {
//				// store the pwd
//				string pwd = PWD;
//				
//				if (this.Exec) {
//					// change to the requested directory, creating paths as required && requested
//					string[] dirs = Path.GetDirectoryName(remoteFilePath).Split(dirseps);
//					foreach(string dir in dirs) {
//						CWD(dir, createDirsOnDemand);
//					}
//				}
//				if (updateOnly 
//				    && (DateTime.Compare(_client.ModTime(Path.GetFileName(remoteFilePath)),
//				                         (new FileInfo(fileName)).LastWriteTime
//				                        )>=0
//				       )
//				   ) {
//					this.Log(this.LevelLogfiles, "Remote file is newer.");
//				} else {
//					this.Log(Level.Verbose, "Putting the file as '{0}'", FtpType);
//					_client.TransferType = FtpType;
//					_client.Put(fileName, Path.GetFileName(remoteFilePath));
//				}
//	
//				if (PWD!=pwd) {
//					this.Log(Level.Verbose, "Restoring the remote dir to {0}", pwd);
//					CWD_Quiet(pwd);
//				}
//			}
//		}
//		
//		// get one file from the PWD to a localpath (PWD is arranged in Get.TransferFiles
//		public void Get(string fileName,
//		                 string localpath, 
//		                 string remotepath,
//		                 FTPTransferType FtpType, 
//		                 bool flatten, 
//		                 bool createDirsOnDemand,
//		                 bool updateOnly) {
//
//			// woohoo!  the remote parsing works for two test-cases!
//			
//			if (RPath.IsPathRooted(remotepath)) {
//				// strip the initial directoryseperatorchar off 
//				// this converts a remotepath rooted in the remote filesystem root
//				// to a local path relative to the local-dir attribute.
//				remotepath = remotepath.Remove(0,1);
//			}
//			
//			DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(localpath, remotepath));
//
//			if (flatten) {
//				dirInfo = new DirectoryInfo(localpath);
//			}
//	
//			this.Log(this.LevelLogfiles, "  file: {0}", RPath.Combine(PWD,fileName));
//			this.Log(this.LevelExec,         "    to: {0}", Path.Combine(dirInfo.FullName, fileName));
////			this.Log(this.LevelExec,      "Getting {0}", fileName);
////			this.Log(Level.Verbose,   "   from {0}", PWD);
////			this.Log(this.LevelExec,      "     to {0}",dirInfo.FullName);
//			
//			if (IsConnected) {
//
//				if (!dirInfo.Exists) {
//					if (!createDirsOnDemand) {
//						throw new FTPException("Local directory "+dirInfo.FullName+" does not exist to receive incoming FTP transfer.");
//					}
//					try {
//						dirInfo.Create();
//					} catch (Exception ex) {
//						throw new FTPException("Local directory "+dirInfo.FullName+" could not be created to receive incoming FTP transfer: ", ex.Message);
//					}
//				}
//				_client.Get(Path.Combine(dirInfo.FullName,fileName), fileName);
//			}
//		}
		
		/// <summary>Download one file</summary>
		/// <param name="FileName">Download this filename</param>
		/// <param name="FtpType">using ascii or binary</param>
		private void downloadFile(string FileName, FTPTransferType FtpType) {

			_connection.Get(Path.GetFileName(FileName),
			         		Path.GetDirectoryName(FileName),
					         ".",
					         FtpType,
			    		     false,
					         false,
					         false);
			       
			         
//			string onlyFileName = EMPTY_STRING;
//			string localRemoteFile = EMPTY_STRING;
//			string downloadTo = EMPTY_STRING;
//			FileSet workingFileset = null;
//
//			if (FTPTransferType.ASCII == FtpType) {
//				workingFileset = _downloadAscii;
//			} else {
//				workingFileset = _downloadBinary;
//			} // if
//			FileInfo fi = new FileInfo(FileName);
//			onlyFileName = fi.Name;
//			fi = null;
//
//			if (!_remotePath.Equals(EMPTY_STRING)) {
//				if (_remotePath.EndsWith(DIR_SEPERATOR.ToString())) {
//					localRemoteFile = _remotePath + onlyFileName;
//				} else {
//					localRemoteFile = _remotePath + DIR_SEPERATOR + onlyFileName;
//				} // if
//				fi = null;
//			} // if
//
//			if (!workingFileset.BaseDirectory.FullName.EndsWith(DIR_SEPERATOR.ToString()) &&
//			    !workingFileset.BaseDirectory.FullName.EndsWith(DOS_DIR_SEPERATOR.ToString())) {
//				downloadTo = workingFileset.BaseDirectory.FullName.Replace("\\", "/") + DIR_SEPERATOR + onlyFileName;
//			} else {
//				downloadTo = workingFileset.BaseDirectory.FullName.Replace("\\", "/") + onlyFileName;
//			} // if
//
//			this.Log(Level.Verbose, "Downloading file {0} to {1}", localRemoteFile, downloadTo);
//
//			_client.TransferType = FtpType;
//			_client.Get(downloadTo, localRemoteFile);
//			//_ftpConn.GetFile(localRemoteFile, downloadTo, FtpType);
			return;
		} // downloadFile()

		/// <summary>Upload one file</summary>
		/// <param name="FileName">the file to upload</param>
		/// <param name="FtpType">use this type (ascii/binary)</param>
		private void uploadFile(string FileName, FTPTransferType FtpType) {
			
			_connection.Put(Path.GetFileName(FileName),
					         Path.GetDirectoryName(FileName),
					         "",
			    		     FtpType,
					         false,
					         false,
			    		     false);
			         
//			
//			string localRemoteFile = EMPTY_STRING;
//			if (!_remotePath.Equals(EMPTY_STRING)) {
//				FileInfo fi = new System.IO.FileInfo(FileName);
//				if (!fi.Exists) {
//					this.Log(Level.Verbose, "Upload aborted: file does not exist");
//					return;
//				} // if
//				if (_remotePath.EndsWith(DIR_SEPERATOR.ToString())) {
//					localRemoteFile = _remotePath + fi.Name;
//				} else {
//					localRemoteFile = _remotePath + DIR_SEPERATOR + fi.Name;
//				} // if
//				fi = null;
//			} else {
//				FileInfo fi = new System.IO.FileInfo(FileName);
//				if (!fi.Exists) {
//					this.Log(Level.Verbose, "Upload aborted: file does not exist");
//					return;
//				} // if
//				localRemoteFile = fi.Name;
//				fi = null;
//			} // if
//
//			this.Log(Level.Verbose, "Uploading file {0} to {1}", FileName, localRemoteFile);
//			_client.TransferType = FtpType;
//			_client.Put(localRemoteFile, FileName);
//			//_ftpConn.SendFile(FileName, localRemoteFile, FtpType);
			return;
		} // uploadFile()
		#endregion
		
		#region helper functions
		

		// parses type attribute values, ensures that they are valid,
		// and returns the corresponding edtFTPnet enum value that can be 
		// passed directly to the edtFTPnet FTPClient.
		public static FTPTransferType ParseTransferType(string atype)
		{
			FTPTransferType theType;
			
			switch (atype.ToUpper()) {
				case "A":
				case "ASCII":
					theType = FTPTransferType.ASCII;
					break;
				case "B":
				case "BIN":
				case "BINARY":
				case "I":
				case "IMG":
				case "IMAGE":
					theType = FTPTransferType.BINARY;
					break;
				default:
					throw new BuildException(String.Format("Invalid 'transfertype' or 'type' attribute '{0}'.\nMust be one of 'a', 'asc', 'ascii', 'b', 'bin', 'binary', 'i', 'img', 'image' (case-insensitive).", atype));
			}			
			
			return theType;
		}

		// dereferences <ftp connection="fromRefID" /> and sets up the
		// internal Connection object accordingly.
		protected void DereferenceConnectionAttribute(string fromRefID) {
			_connection.RefID = fromRefID;
            
			// the following code block was modified from
			// from NAnt.Core\Element.cs:line 1247 {v0.85-rc1)
			if (Project.DataTypeReferences.Contains(_connection.RefID) 
			    && Project.DataTypeReferences[_connection.RefID] is Connection) {
				_connection = (Connection)Project.DataTypeReferences[_connection.RefID];
                // clear any instance specific state
                _connection.Reset();
			} else {
				
				//_connection = new Connection();
			
				// try to load the password from the file
				if (!_connection.LoadPasswords(_connection.RefID)) {

					// reference not found exception
	                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
	                    "Connection '{0}' is not defined in the current project.", _connection.RefID), 
	                    this.Location);
	            }
			}
		}

		/// <summary>Load connection details from the internal Connection object into the task object.</summary>
		protected void getConnection() {
			_server = _connection.Domain;
			if (_connection.UserName==null || _connection.UserName.Equals(EMPTY_STRING)) {
				_user = ANONYMOUS_USER;
				_password = null;
			} else {
				_user = _connection.UserName;
				_password = _connection.Password;
			} // if
			return;
		} // getConnection()()

		#endregion

		#region scripting
		
		private void script() {
			_connection.RunScript(_script);
		} // script()

		#endregion
		
	} // class
} // namespace

