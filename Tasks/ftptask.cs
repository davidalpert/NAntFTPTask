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

using Sourceforge.NAnt.Ftp.Types;

namespace FTPTask {
	/// <summary>FTP tasks</summary>
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
		private const string DIR_SEPERATOR = "/";
		/// <summary>Dir seperator on dos/win</summary>
		private const string DOS_DIR_SEPERATOR = "\\";
		/// <summary>Standard ftp port</summary>
		private const int DEFAULT_FTP_PORT = 21;
		/// <summary>user for anon ftp</summary>
		private const string ANONYMOUS_USER = "anonymous";
		/// <summary>pass for anon ftp</summary>
		private const string ANONYMOUS_PASS = "anonymous@unknown.org";
		/// <summary>the name of the script block</summary>
		private const string SCRIPT_NODE_NAME = "nant:script";
		/// <summary>the name of the script block</summary>
		private const string PUT_NODE_NAME = "nant:put";
		#endregion

		#region variables
		private string 	_server 	= EMPTY_STRING;
		private int 	_port 		= DEFAULT_FTP_PORT;
		
		private string 	_user 		= EMPTY_STRING;
		private string 	_password 	= EMPTY_STRING;
		
		private string 	_remotePath = EMPTY_STRING;
		private string 	_localPath 	= ".";
		private string 	_oldPath 	= ".";
		private bool	_showDirOnConnect = false;
		
		private Connection _connection	= null;
		private FTPClient _client 		= null;

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
			return;
		} // FTPTask()
		#endregion

		#region /////////////////////// Task Properties
		/// <summary>The property</summary>
		[TaskAttribute("port", Required=false)]
		[Int32Validator(1,65535)]
		public int port {
			get {
				return _port;
			} set {
					_port = value;
				}
		} // port

		/// <summary>The property</summary>
		[TaskAttribute("connection", Required=false)]
		public string ConnectionName {
			get {
				return _connection.RefID;
			} set {
				DereferenceConnectionAttribute(value);
			}
		} // server
		
		/// <summary>The property</summary>
		[TaskAttribute("remotepath", Required=false)]
		public string remotepath {
			get {
				return _remotePath;
			} set {
					_remotePath = value;
				}
		} // remotepath
		
		/// <summary>The property</summary>
		[TaskAttribute("localpath", Required=false)]
		public string localpath {
			get {
				return _localPath;
			} set {
					_localPath = value;
				}
		} // localpath

		/// <summary>The property</summary>
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
			get { return _client!=null;}
		}
		#endregion

		#region /////////////////////// Build Elements		
		/// <summary>The network credentials used for authenticating the request with
		/// the Internet resource.</summary>
		[BuildElement("connection")]
		public Connection Connection {
			get { return _connection; }
			set { _connection = value; }
		} // credentials

		[BuildElement("up-ascii")]
        [Obsolete("Use the <put mode='ascii' /> element instead.", false)]
		public FileSet UploadAscii {
			get {
				return _uploadAscii;
			} set {
					_uploadAscii = value;
				}
		} // UploadAscii

		[BuildElement("up-binary")]
        [Obsolete("Use the <put mode='bin' /> element instead.", false)]
		public FileSet UploadBinary {
			get {
				return _uploadBinary;
			} set {
					_uploadBinary = value;
				}
		} // Upload

		[BuildElement("down-ascii")]
        [Obsolete("Use the <get mode='ascii' /> element instead.", false)]
		public FileSet DowloadAscii {
			get {
				return _downloadAscii;
			} set {
					_downloadAscii = value;
				}
		} // DownloadAscii

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

			XmlNodeList scriptList = taskNode.SelectNodes(SCRIPT_NODE_NAME, NamespaceManager);
			if (0 == scriptList.Count) {
				_script = EMPTY_STRING;
			} else {
				if (1 == scriptList.Count) {
					_script = scriptList.Item(0).InnerText;
				} else {
					throw new BuildException("Only one <script> block allowed.", Location);
				} // if
			} // if
			scriptList = null;

			XmlNodeList putList = taskNode.SelectNodes(PUT_NODE_NAME, NamespaceManager);

			return;
		} // InitializeTask()


		protected override void ExecuteTask() {
			Work();
			return;
		} // ExecuteTask()
		#endregion
		
		#region /////////////////////// Implementation
		private void Work() {
			
			// init our local connection parameters
			setConnection();
			this.Log(Level.Verbose, "Connection set:\n\tserver\t- {0}\n\tuser  \t- {1}\n\tpass  \t- {2}",_server, _user, _password);
			
			if (ServerIsSet)
			{	
				try {
					ftpConnect();
					
					CWD(_remotePath);
				
					if (_showDirOnConnect) {
						string[] dirlist = _client.Dir(".", true);
						foreach(string itemname in dirlist) {
							this.Log(Level.Info, " + : {0}",itemname);
						}
					}
					this.ExecuteChildTasks();
		//
		////				this.Log((Level.Debug, "{3}Binaries{0}{3}  up   {1}{0}{3}  down {2}", System.Environment.NewLine, _uploadBinary.FileNames.Count, _downloadBinary.Includes.Count, this.LogPrefix);
		////				this.Log(Level.Debug, "{3}Asciis{0}{3}  up   {1}{0}{3}  down {2}", System.Environment.NewLine, _uploadAscii.FileNames.Count, _downloadAscii.Includes.Count, this.LogPrefix);
		//
		//				foreach(string fileName in _uploadBinary.FileNames) {
		//					try {
		//						uploadFile(fileName, FTPClient.FTPFileTransferType.Binary);
		//					} catch {
		//						this.Log(Level.Info, this.LogPrefix + "{0} could not be uploaded.", fileName);
		//					} // try
		//				} // foreach
		//
		//				foreach(string fileName in _uploadAscii.FileNames) {
		//					try {
		//						uploadFile(fileName, FTPClient.FTPFileTransferType.ASCII);
		//					} catch {
		//						this.Log(Level.Info, this.LogPrefix + "{0} could not be uploaded.", fileName);
		//					} // try
		//				} // foreach
		//
		//				foreach(string fileName in _downloadBinary.Includes) {
		//					try {
		//						downloadFile(fileName, FTPClient.FTPFileTransferType.Binary);
		//					} catch {
		//						this.Log(Level.Info, this.LogPrefix + "{0} could not be downloaded.", fileName);
		//					} // try
		//				} // foreach
		//
		//				foreach(string fileName in _downloadAscii.Includes) {
		//					try {
		//						downloadFile(fileName, FTPClient.FTPFileTransferType.ASCII);
		//					} catch {
		//						this.Log(Level.Info, this.LogPrefix + "{0} could not be downloaded.", fileName);
		//					} // try
		//				} // foreach
		//
		//				script();
		//
				}
				finally {
					ftpDisconnect();
				}		
			} else {
				this.Log(Level.Error, "No server given");
			} // if
			return;
		} // Work()
		#endregion

		#region file functions
//		/// <summary>Download one file</summary>
//		/// <param name="FileName">Download this filename</param>
//		/// <param name="FtpType">using ascii or binary</param>
//		private void downloadFile(string FileName, FTPClient.FTPFileTransferType FtpType) {
//			string onlyFileName = EMPTY_STRING;
//			string localRemoteFile = EMPTY_STRING;
//			string downloadTo = EMPTY_STRING;
//			FileSet workingFileset = null;
//
//			if (FTPClient.FTPFileTransferType.ASCII == FtpType) {
//				workingFileset = _downloadAscii;
//			} else {
//				workingFileset = _downloadBinary;
//			} // if
//			FileInfo fi = new FileInfo(FileName);
//			onlyFileName = fi.Name;
//			fi = null;
//
//			if (!_remotePath.Equals(EMPTY_STRING)) {
//				if (_remotePath.EndsWith(DIR_SEPERATOR)) {
//					localRemoteFile = _remotePath + onlyFileName;
//				} else {
//					localRemoteFile = _remotePath + DIR_SEPERATOR + onlyFileName;
//				} // if
//				fi = null;
//			} // if
//
//			if (!workingFileset.BaseDirectory.FullName.EndsWith(DIR_SEPERATOR) && 
//				!workingFileset.BaseDirectory.FullName.EndsWith(DOS_DIR_SEPERATOR)) {
//				downloadTo = workingFileset.BaseDirectory.FullName.Replace("\\", "/") + DIR_SEPERATOR + onlyFileName;
//			} else {
//				downloadTo = workingFileset.BaseDirectory.FullName.Replace("\\", "/") + onlyFileName;
//			} // if
//
//			this.Log(Level.Verbose, this.LogPrefix + "Downloading file {0} to {1}", localRemoteFile, downloadTo);
//
//			_ftpConn.GetFile(localRemoteFile, downloadTo, FtpType);
//			return;
//		} // downloadFile()
//
//		/// <summary>Upload one file</summary>
//		/// <param name="FileName">the file to upload</param>
//		/// <param name="FtpType">use this type (ascii/binary)</param>
//		private void uploadFile(string FileName, FTPClient.FTPFileTransferType FtpType) {
//			string localRemoteFile = EMPTY_STRING;
//			if (!_remotePath.Equals(EMPTY_STRING)) {
//				FileInfo fi = new System.IO.FileInfo(FileName);
//				if (!fi.Exists) {
//					this.Log(Level.Verbose, this.LogPrefix + "Upload aborted: file does not exist");
//					return;
//				} // if
//				if (_remotePath.EndsWith(DIR_SEPERATOR)) {
//					localRemoteFile = _remotePath + fi.Name;
//				} else {
//					localRemoteFile = _remotePath + DIR_SEPERATOR + fi.Name;
//				} // if
//				fi = null;
//			} else {
//				FileInfo fi = new System.IO.FileInfo(FileName);
//				if (!fi.Exists) {
//					this.Log(Level.Verbose, this.LogPrefix + "Upload aborted: file does not exist");
//					return;
//				} // if
//				localRemoteFile = fi.Name;
//				fi = null;
//			} // if
//
//			this.Log(Level.Verbose, this.LogPrefix + "Uploading file {0} to {1}", FileName, localRemoteFile);
//			_ftpConn.SendFile(FileName, localRemoteFile, FtpType);
//			return;
//		} // uploadFile()
		#endregion

		#region scripting
		private void script() {
//			if (!_script.Equals(EMPTY_STRING)) {
//				string [] lines = Cut(_script, Environment.NewLine);
//
//				foreach(string line in lines) {
//					if (!line.Trim().Equals(EMPTY_STRING)) {
////						this.Log(Level.Verbose, "{0}Executing {1}", this.LogPrefix, line.Trim());
//						ArrayList listReturn = _ftpConn.SendCommand(line.Trim());
//						foreach(object returned in listReturn) {
//							this.Log(Level.Info, "{0}{1}", this.LogPrefix, returned);
//						} // foreach
//						listReturn.Clear();
//						listReturn = null;
//					} // if
//				} // foreach
//			} // if
			return;
		} // script()
		#endregion

		#region helper
//		/// <summary>Cut a text like Split but with a seperator longer than one char.</summary>
//		/// <param name="Input">Cut this text</param>
//		/// <param name="Splitter">using this seperator</param>
//		/// <returns>and return the elements as array</returns>
//		private static string [] Cut(string Input, string Splitter) {
//
//			//////////////////////////////////////////////////
//			// local variables
//			//////////////////////////////////////////////////
//		
//			string [] retVal= null;
//			int position = 0;
//			StringCollection sc = new StringCollection();
//
//			//////////////////////////////////////////////////
//			// input testing
//			//////////////////////////////////////////////////
//
//			if (null == Splitter) {
//				throw new Exception(EXCEPTION_NULL_STRING);
//			} // if
//
//			if (Splitter.Equals(EMPTY_STRING)) {
//				throw new Exception(EXCEPTION_EMPTY_STRING);
//			} // if
//
//			//////////////////////////////////////////////////
//			// Split
//			//////////////////////////////////////////////////
//      
//			position = Input.IndexOf(Splitter);
//		
//			while(position > -1) {
//				string untilSplitter = Input.Substring(0, position);
//				Input = Input.Substring(position + Splitter.Length);
//				position = Input.IndexOf(Splitter);
//				if (untilSplitter.TrimStart().TrimEnd().Length > 0) {
//					sc.Add(untilSplitter);
//				} // if
//			} // while
//			sc.Add(Input);
//
//			//////////////////////////////////////////////////
//			// Transfer
//			//////////////////////////////////////////////////
//		
//			retVal = new string [ sc.Count ];
//			for(int run = 0; run < sc.Count; run++) {
//				retVal[run] = sc[run];
//			} // for
//			sc = null;
//
//			return retVal;
//		} // Cut()

		protected void CWD(string remotePath) {
			if (IsConnected && remotePath!=EMPTY_STRING) {
				try {
					this.Log(Level.Info, "Changing remote directory to " + _remotePath);
					_client.ChDir(remotePath);
				} catch (Exception ex) {
					throw new ApplicationException(ex.Message);					
				}
			}
		}
		
		// dereferences <ftp connction="fromRefID" /> and sets up the
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
                // reference not found exception
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Connection '{0}' is not defined in the current project.", _connection.RefID), 
                    this.Location);
            }
		}

		/// <summary>Load connection details</summary>
		protected void setConnection() {
			_server = _connection.Domain;
			if (!_connection.UserName.Equals(EMPTY_STRING)) {
				_user = _connection.UserName;
				_password = _connection.Password;
			} // if
			return;
		} // setConnection()()
		#endregion

		#region ftp functions
		/// <summary>Disconnect from server</summary>
		private void ftpDisconnect() {
			if (IsConnected) {
				this.Log(Level.Info, "Disconnecting from " + _server);
				_client.Quit();
				_client = null;
			}
			return;
		} // ftpDisconnect()

		/// <summary>Connect to server</summary>
		private void ftpConnect() {
			if (!IsConnected) {
				this.Log(Level.Info, "Connecting to " + _server);
				this.Log(Level.Verbose, "Instantiating the FTPClient...");
				_client = new FTPClient(_server);
				
				this.Log(Level.Verbose, "Authenticating with " + _user);
				_client.Login(_user, _password);
				
				this.Log(Level.Verbose, "and setting the connection mode to passive.");
				_client.ConnectMode = FTPConnectMode.PASV;
			}
			return;
		} // ftpConnect()
		#endregion
	} // class
} // namespace

