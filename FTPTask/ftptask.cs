using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace FTPTask {
	/// <summary>FTP tasks</summary>
	[TaskName("ftp")]
	public class FTPTask : Task {
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
		/// <summary>how ia the name of the script block</summary>
		private const string SCRIPT_NODE_NAME = "nant:script";
		#endregion

		#region variables
		private string _remotePath = EMPTY_STRING;
		private string _user = ANONYMOUS_USER;
		private string _password = ANONYMOUS_PASS;
		private string _server = EMPTY_STRING;
		private FileSet _uploadAscii = new FileSet();
		private FileSet _uploadBinary = new FileSet();
		private FileSet _downloadAscii = new FileSet();
		private FileSet _downloadBinary = new FileSet();
		private int _port = DEFAULT_FTP_PORT;
		private Credential _credentials = new Credential();
		private FTPClient.FTPConnection _ftpConn = null;
		private string _script = EMPTY_STRING;
		#endregion

		#region constructor
		public FTPTask() {
			return;
		} // FTPTask()
		#endregion

		/////////////////////// Task Properties
		#region Task Properties
		[BuildElement("up-ascii")]
		public FileSet UploadAscii {
			get {
				return _uploadAscii;
			} set {
					_uploadAscii = value;
				}
		} // UploadAscii

		[BuildElement("up-binary")]
		public FileSet UploadBinary {
			get {
				return _uploadBinary;
			} set {
					_uploadBinary = value;
				}
		} // Upload

		[BuildElement("down-ascii")]
		public FileSet DowloadAscii {
			get {
				return _downloadAscii;
			} set {
					_downloadAscii = value;
				}
		} // DownloadAscii

		[BuildElement("down-binary")]
		public FileSet DowloadBinary {
			get {
				return _downloadBinary;
			} set {
					_downloadBinary = value;
				}
		} // DownloadBinary

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
		[TaskAttribute("server", Required=true)]
		public string server {
			get {
				return _server;
			} set {
					_server = value;
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
		} // basepath

		/// <summary>The network credentials used for authenticating the request with 
		/// the Internet resource.</summary>
		[BuildElement("credentials")]
		public Credential credentials {
			get { return _credentials; }
			set { _credentials = value; }
		} // credentials
		#endregion

		/////////////////////// Task functions
		#region task functions
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

			return;
		} // InitializeTask()


		protected override void ExecuteTask() {
			Work();
			return;
		} // ExecuteTask()
		#endregion
		
		/////////////////////// Implementation
		#region main function
		private void Work() {
			if (!_server.Equals(EMPTY_STRING)) {
				credentialCheck();
				ftpConnect();

				this.Log(Level.Debug, "{3}Binaries{0}{3}  up   {1}{0}{3}  down {2}", System.Environment.NewLine, _uploadBinary.FileNames.Count, _downloadBinary.Includes.Count, this.LogPrefix);
				this.Log(Level.Debug, "{3}Asciis{0}{3}  up   {1}{0}{3}  down {2}", System.Environment.NewLine, _uploadAscii.FileNames.Count, _downloadAscii.Includes.Count, this.LogPrefix);

				foreach(string fileName in _uploadBinary.FileNames) {
					try {
						uploadFile(fileName, FTPClient.FTPFileTransferType.Binary);
					} catch {
						this.Log(Level.Info, this.LogPrefix + "{0} could not be uploaded.", fileName);
					} // try
				} // foreach

				foreach(string fileName in _uploadAscii.FileNames) {
					try {
						uploadFile(fileName, FTPClient.FTPFileTransferType.ASCII);
					} catch {
						this.Log(Level.Info, this.LogPrefix + "{0} could not be uploaded.", fileName);
					} // try
				} // foreach

				foreach(string fileName in _downloadBinary.Includes) {
					try {
						downloadFile(fileName, FTPClient.FTPFileTransferType.Binary);
					} catch {
						this.Log(Level.Info, this.LogPrefix + "{0} could not be downloaded.", fileName);
					} // try
				} // foreach

				foreach(string fileName in _downloadAscii.Includes) {
					try {
						downloadFile(fileName, FTPClient.FTPFileTransferType.ASCII);
					} catch {
						this.Log(Level.Info, this.LogPrefix + "{0} could not be downloaded.", fileName);
					} // try
				} // foreach

				script();

				ftpDisconnect();

			} else {
				this.Log(Level.Error, this.LogPrefix + "No server given");
			} // if
			return;
		} // Work()
		#endregion

		#region file functions
		/// <summary>Download one file</summary>
		/// <param name="FileName">Download this filename</param>
		/// <param name="FtpType">using ascii or binary</param>
		private void downloadFile(string FileName, FTPClient.FTPFileTransferType FtpType) {
			string onlyFileName = EMPTY_STRING;
			string localRemoteFile = EMPTY_STRING;
			string downloadTo = EMPTY_STRING;
			FileSet workingFileset = null;

			if (FTPClient.FTPFileTransferType.ASCII == FtpType) {
				workingFileset = _downloadAscii;
			} else {
				workingFileset = _downloadBinary;
			} // if
			FileInfo fi = new FileInfo(FileName);
			onlyFileName = fi.Name;
			fi = null;

			if (!_remotePath.Equals(EMPTY_STRING)) {
				if (_remotePath.EndsWith(DIR_SEPERATOR)) {
					localRemoteFile = _remotePath + onlyFileName;
				} else {
					localRemoteFile = _remotePath + DIR_SEPERATOR + onlyFileName;
				} // if
				fi = null;
			} // if

			if (!workingFileset.BaseDirectory.FullName.EndsWith(DIR_SEPERATOR) && 
				!workingFileset.BaseDirectory.FullName.EndsWith(DOS_DIR_SEPERATOR)) {
				downloadTo = workingFileset.BaseDirectory.FullName.Replace("\\", "/") + DIR_SEPERATOR + onlyFileName;
			} else {
				downloadTo = workingFileset.BaseDirectory.FullName.Replace("\\", "/") + onlyFileName;
			} // if

			this.Log(Level.Verbose, this.LogPrefix + "Downloading file {0} to {1}", localRemoteFile, downloadTo);

			_ftpConn.GetFile(localRemoteFile, downloadTo, FtpType);
			return;
		} // downloadFile()

		/// <summary>Upload one file</summary>
		/// <param name="FileName">the file to upload</param>
		/// <param name="FtpType">use this type (ascii/binary)</param>
		private void uploadFile(string FileName, FTPClient.FTPFileTransferType FtpType) {
			string localRemoteFile = EMPTY_STRING;
			if (!_remotePath.Equals(EMPTY_STRING)) {
				FileInfo fi = new System.IO.FileInfo(FileName);
				if (!fi.Exists) {
					this.Log(Level.Verbose, this.LogPrefix + "Upload aborted: file does not exist");
					return;
				} // if
				if (_remotePath.EndsWith(DIR_SEPERATOR)) {
					localRemoteFile = _remotePath + fi.Name;
				} else {
					localRemoteFile = _remotePath + DIR_SEPERATOR + fi.Name;
				} // if
				fi = null;
			} else {
				FileInfo fi = new System.IO.FileInfo(FileName);
				if (!fi.Exists) {
					this.Log(Level.Verbose, this.LogPrefix + "Upload aborted: file does not exist");
					return;
				} // if
				localRemoteFile = fi.Name;
				fi = null;
			} // if

			this.Log(Level.Verbose, this.LogPrefix + "Uploading file {0} to {1}", FileName, localRemoteFile);
			_ftpConn.SendFile(FileName, localRemoteFile, FtpType);
			return;
		} // uploadFile()
		#endregion

		#region scripting
		private void script() {
			if (!_script.Equals(EMPTY_STRING)) {
				string [] lines = Cut(_script, Environment.NewLine);

				foreach(string line in lines) {
					if (!line.Trim().Equals(EMPTY_STRING)) {
						this.Log(Level.Verbose, "{0}Executing {1}", this.LogPrefix, line.Trim());
						ArrayList listReturn = _ftpConn.SendCommand(line.Trim());
						foreach(object returned in listReturn) {
							this.Log(Level.Info, "{0}{1}", this.LogPrefix, returned);
						} // foreach
						listReturn.Clear();
						listReturn = null;
					} // if
				} // foreach
			} // if
			return;
		} // script()
		#endregion

		#region helper
		/// <summary>Cut a text like Split but with a seperator longer than one char.</summary>
		/// <param name="Input">Cut this text</param>
		/// <param name="Splitter">using this seperator</param>
		/// <returns>and return the elements as array</returns>
		private static string [] Cut(string Input, string Splitter) {

			//////////////////////////////////////////////////
			// local variables
			//////////////////////////////////////////////////
		
			string [] retVal= null;
			int position = 0;
			StringCollection sc = new StringCollection();

			//////////////////////////////////////////////////
			// input testing
			//////////////////////////////////////////////////

			if (null == Splitter) {
				throw new Exception(EXCEPTION_NULL_STRING);
			} // if

			if (Splitter.Equals(EMPTY_STRING)) {
				throw new Exception(EXCEPTION_EMPTY_STRING);
			} // if

			//////////////////////////////////////////////////
			// Split
			//////////////////////////////////////////////////
      
			position = Input.IndexOf(Splitter);
		
			while(position > -1) {
				string untilSplitter = Input.Substring(0, position);
				Input = Input.Substring(position + Splitter.Length);
				position = Input.IndexOf(Splitter);
				if (untilSplitter.TrimStart().TrimEnd().Length > 0) {
					sc.Add(untilSplitter);
				} // if
			} // while
			sc.Add(Input);

			//////////////////////////////////////////////////
			// Transfer
			//////////////////////////////////////////////////
		
			retVal = new string [ sc.Count ];
			for(int run = 0; run < sc.Count; run++) {
				retVal[run] = sc[run];
			} // for
			sc = null;

			return retVal;
		} // Cut()

		/// <summary>Check what credentials to use</summary>
		private void credentialCheck() {
			if (!_credentials.UserName.Equals(EMPTY_STRING)) {
				_user = _credentials.UserName;
				_password = credentials.Password;
			} // if
			return;
		} // credentialCheck()()
		#endregion

		#region ftp functions
		/// <summary>Disconnect from server</summary>
		private void ftpDisconnect() {
			this.Log(NAnt.Core.Level.Verbose, this.LogPrefix + "Disconnecting ftp");
			_ftpConn.Close();
			_ftpConn = null;
			return;
		} // ftpDisconnect()

		/// <summary>Connect to server</summary>
		private void ftpConnect() {
			_ftpConn = new FTPClient.FTPConnection();
			this.Log(NAnt.Core.Level.Verbose, this.LogPrefix + "Connecting to " + _server);
			_ftpConn.Open(_server, _port, _user, _password, FTPClient.FTPMode.Passive);
			return;
		} // ftpConnect()
		#endregion
	} // class
} // namespace