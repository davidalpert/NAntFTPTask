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

namespace Sourceforge.NAnt.Ftp.Tasks {
	
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
		private const string SCRIPT_NODE_NAME = "nant:script";
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
		
		private bool	_debug		= false;
		
		private FTPConnectMode 	_connectMode = FTPConnectMode.PASV;
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
						
			return;
		} // FTPTask()
		#endregion

		#region /////////////////////// Task Properties

		public FTPClient Client { get {return _client;} }
		
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
		[TaskAttribute("connectmode", Required=false)]
		public string Mode {
			get {
				return _connectMode.ToString();
			} set {
				_connectMode = ParseConnectMode(value);
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
		[TaskAttribute("debug", Required=false)]
		[BooleanValidator()]
		public bool Debug {
			get {
				return _debug;
			} set {
					_debug = value;
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

#if false
		/// <summary>
        /// The items to include in the fileset.
        /// </summary>
        [BuildElementArray("include")]
        public Include[] IncludeElements {
            set {
                foreach (Include include in value) {
                    if (include.IfDefined && !include.UnlessDefined) {
                        if (include.AsIs) {
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including AsIs=", include.Pattern));
                            AsIs.Add(include.Pattern);
                        } else if (include.FromPath) {
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including FromPath=", include.Pattern));
                            PathFiles.Add(include.Pattern);
                        } else {
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including pattern", include.Pattern));
                            Includes.Add(include.Pattern);
                        }
                    }
                }
            }
        }
#endif

		[BuildElementArray("put")]
		public TransferFileSet[] PutSets {
            set {
//                foreach (TransferFileSet putset in value) {
//                    if (putset.IfDefined && !putset.UnlessDefined) {
//						_putSets.Add(putset);
//					}
//				}
//				this.Log(Level.Info, "Found {0} put sets.", _putSets.Count);				
			}
		}
		[BuildElementArray("get")]
		public TransferFileSet[] GetSets {
            set {
//                foreach (TransferFileSet getset in value) {
//                    if (getset.IfDefined && !getset.UnlessDefined) {
//						_getSets.Add(getset);
//					}
//				}
//				this.Log(Level.Info, "Found {0} get sets.", _getSets.Count);
			}
		}
		
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

			// grab the script block
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
			
			// init our local connection parameters
			getConnection();
			this.Log(Level.Verbose, "Connection set:\n\tserver\t- {0}\n\tuser  \t- {1}\n\tpass  \t- {2}",_server, _user, _password);
			
			if (ServerIsSet)
			{	
				try {
					ftpConnect();
					
					CWD(_remotePath);
				
					if (_showDirOnConnect) {
						ShowDir(".");
					}
									
					this.ExecuteChildTasks();
		
					DoTransfers();
					
		//				this.Log((Level.Debug, "{3}Binaries{0}{3}  up   {1}{0}{3}  down {2}", System.Environment.NewLine, _uploadBinary.FileNames.Count, _downloadBinary.Includes.Count, this.LogPrefix);
		//				this.Log(Level.Debug, "{3}Asciis{0}{3}  up   {1}{0}{3}  down {2}", System.Environment.NewLine, _uploadAscii.FileNames.Count, _downloadAscii.Includes.Count, this.LogPrefix);
		
						foreach(string fileName in _uploadBinary.FileNames) {
							try {
								uploadFile(fileName, FTPTransferType.BINARY);
							} catch {
								this.Log(Level.Info, "{0} could not be uploaded.", fileName);
							} // try
						} // foreach
		
						foreach(string fileName in _uploadAscii.FileNames) {
							try {
								uploadFile(fileName, FTPTransferType.ASCII);
							} catch {
								this.Log(Level.Info, "{0} could not be uploaded.", fileName);
							} // try
						} // foreach
		
						foreach(string fileName in _downloadBinary.Includes) {
							try {
								downloadFile(fileName, FTPTransferType.BINARY);
							} catch {
								this.Log(Level.Info, "{0} could not be downloaded.", fileName);
							} // try
						} // foreach
		
						foreach(string fileName in _downloadAscii.Includes) {
							try {
								downloadFile(fileName, FTPTransferType.ASCII);
							} catch {
								this.Log(Level.Info, "{0} could not be downloaded.", fileName);
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
					ftpDisconnect();
				}		
			} else {
				this.Log(Level.Error, "No server given");
				throw new BuildException("FTP Exception:\n\tNo server given",
				                         this.Location);
			} // if
			return;
		} // Work()
		#endregion

		#region ftp connect/disconnect functions

		/// <summary>Connect to server</summary>
		private void ftpConnect() {
			if (_debug) {
				this.Log(Level.Info, "-------------- Debugging the ftp query --------------");
			}
			else if (!IsConnected) {
				this.Log(Level.Info, "Connecting to '{0}' as '{1}' ...", _server, _user);
				this.Log(Level.Verbose, "Instantiating the FTPClient & opening the connection...");
				_client = new FTPClient(_server);
				
				this.Log(Level.Verbose, "Authenticating...");
				if (_password==null || _password==String.Empty || _password.ToUpper()=="PROMPT") {
					LoginWithPromptForPassword(_user);
				} else {
					_client.Login(_user, _password);
				}
				
				this.Log(Level.Verbose, "and setting the connection mode to passive.");
				_client.ConnectMode = _connectMode;
			}
			return;
		} // ftpConnect()

		/// <summary>Do a remote login while asking the user for a password through the console.</summary>
		/// <param name="username">the username to use when logging in.</param>
		private void LoginWithPromptForPassword(string username) {
			PasswordInputManager pim = new PasswordInputManager();
			bool success = false;
			bool abort = false;
			string pass = String.Empty;
			this.Log(Level.Info, "Please Enter Password:");
			while (!success && !abort) {
				//Console.Write(">> ");
				//pass = Console.ReadLine();
				pass = String.Empty;
				pim.PasswordInput(ref pass, 256);
				Console.WriteLine("");
				if (pass==String.Empty) {
					abort = true;
				} else {
					// todo: validate input
					try {
						_client.User(username);
						_client.Password(pass);
						success = true;
					} catch (FTPException ex) {
						this.Log(Level.Info, ex.Message);
						this.Log(Level.Info, "Please Re-Enter Password or Press Enter to Abort.");
					}
				}
			}
			if (abort) {
				throw new FTPException("Login Aborted by User");
			}
		}
		
		/// <summary>Disconnect from server</summary>
		private void ftpDisconnect() {
			if (IsConnected) {
				this.Log(Level.Info, "Disconnecting from '{0}'", _server);
				_client.Quit();
				_client = null;
			}
			return;
		} // ftpDisconnect()

		#endregion

		#region ftp directory functions
		
		public string PWD {
			get { 
				if (IsConnected) {
					return _client.Pwd();
				} else {
					return String.Empty;
				}
			}
		}
		
		public void CWD(string remotePath) {
			remotePath = remotePath.Replace(DOS_DIR_SEPERATOR, DIR_SEPERATOR);
			if (remotePath!=String.Empty) {
				this.Log(Level.Info, "Changing remote directory to '{0}'", remotePath);
				if (IsConnected) {
					this.Log(Level.Verbose, " + Attempting CWD...");
					_client.ChDir(remotePath);
					this.Log(Level.Verbose, " + CWD successful.");
				} else {
					this.Log(Level.Debug, "Not connected.");
				}
			}
		}

		public void CWD(string remotePath, bool createOnDemand) {
			if (createOnDemand) {
				try {
					CWD(remotePath);
				} catch (FTPException fex) {
					this.Log(Level.Info, fex.Message);
					this.Log(Level.Info, "Creating {0} remotely.", remotePath);
					MkDir(remotePath);
					CWD(remotePath);
				}
			}
		}

		public FTPFile[] DirDetails(string path) {
			if (IsConnected) {
				return _client.DirDetails(path);
			} else {
				FTPFile[] result = {};
				return result;
			}
		}
		public string[] DirDetails(string path, bool full) {
			if (IsConnected) {
				return _client.Dir(path, full);
			} else {
				string[] result = {};
				return result;
			}
		}
		
		public RemotePath[] GetDirs(string path) {
			RemotePath[] list = RemotePath.FromFTPFileArray(path, DirDetails(path));
			int dircount = 0;
			foreach (RemotePath rpath in list) {
				if (rpath.IsDir) {
					dircount++;
				}
			}
			if (dircount==0) {
				RemotePath[] result = {};
				return result;
			} else {
				RemotePath[] result = new RemotePath[dircount];
				int z=0;
				foreach(RemotePath rpath in list) {
					if (rpath.IsDir) {
						result[z++] = rpath;
//						result[z].File   = rpath.File;
//						result[z++].Path = rpath.Path;
					}
				}
				return result;
			}
			
		}
		public string ResolvePath(string rpath){
			string pwd = PWD;
			string result;
			try {
				CWD(rpath);
				result = PWD;
			} catch (FTPException fex) {
				fex.ToString();
				// path does not exist
				result = String.Empty;
			} finally {
				CWD(pwd);
			}
			return result;
		}

		public RemotePath[] GetFiles(string path) {
			RemotePath[] list = RemotePath.FromFTPFileArray(path, DirDetails(path));
			int filecount = 0;
			foreach (RemotePath rpath in list) {
				if (rpath.IsFile) {
					filecount++;
				}
			}
			if (filecount==0) {
				RemotePath[] result = {};
				return result;
			} else {
				RemotePath[] result = new RemotePath[filecount];
				int z=0;
				foreach(RemotePath rpath in list) {
					if (rpath.IsFile) {
						result[z++] = rpath;
//						result[z].File   = rpath.File;
//						result[z++].Path = rpath.Path;
					}
				}
				return result;
			}
			
		}
		
		public void ShowDir(string dir) {
			this.Log(Level.Info, "Remote Directory Listing:");
			if (IsConnected) {
				string[] dirlist = DirDetails(dir, true);
				foreach(string itemname in dirlist) {
					this.Log(Level.Info, " + : {0}",itemname);
				}
			} else {
				this.Log(Level.Debug, "Not connected.");
			}			
		}
		
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
			if (IsConnected) {
				string pwd = PWD;
				try {
					CWD(remoteDir);
				} catch (FTPException fex) {
					fex.ToString();
					exist = false;
				} finally {
					CWD(pwd);
				}
			} else {
				exist = false;
			}
			return exist;
		}
		
		#endregion
		
		#region ftp file functions

		// process the transferfilesets
		private void DoTransfers() {
			foreach (TransferFileSet tfs in _transferList) {
				this.Log(Level.Info, "Processing a {0}FileSet.", tfs.Direction);
				this.Log(Level.Info, " +    Local Path: "+tfs.LocalPath.ToString());
				this.Log(Level.Info, " +   Remote Path: "+tfs.RemotePathString);
				this.Log(Level.Info, " + Transfer Type: "+tfs.TransferType);
				
				tfs.Transfer(this);
			}
		}

		// put one file
		public void Put(string fileName,
		                 string localpath, 
		                 string remotepath,
		                 FTPTransferType FtpType, 
		                 bool flatten, 
		                 bool createDirsOnDemand) {

			char [] dirseps = {DOS_DIR_SEPERATOR, DIR_SEPERATOR};

			string localFilePath = String.Empty;	// path to 'fileName' locally, relative to 'localpath'
			string remoteFilePath = String.Empty;	// path to 'fileName' remotely, relative to 'remotepath'
			
			// convert fileName into relative paths...
			if (Path.GetDirectoryName(fileName).StartsWith(localpath)) {
				
				// our abs path is longer than localpath, 
				// so the relative path is simple.
			
				localFilePath = fileName.Replace(localpath, String.Empty).Remove(0,1);
			
			} else if (localpath.StartsWith(Path.GetDirectoryName(fileName))) {

				// our abs path is shorter than the localpath
				// so our relative path is preceded by '..' references.
				
				localpath = localpath.Replace(Path.GetDirectoryName(fileName), String.Empty);

				int z = -1;

				z = localpath.IndexOfAny(dirseps);
				while( z >-1 ) {
					localFilePath += ".."+Path.DirectorySeparatorChar;
					localpath = localpath.Remove(z,1);
					z = localpath.IndexOfAny(dirseps);
				}
				localFilePath += Path.GetFileName(fileName);
			}
			
			// mirror the remote file path from the local file path, flattening if requested
			if (flatten) {
				remoteFilePath = Path.GetFileName(localFilePath);
			} else {
				remoteFilePath = localFilePath;
			}
			
			remoteFilePath = remoteFilePath.Replace(DOS_DIR_SEPERATOR, DIR_SEPERATOR);
			
			this.Log(Level.Info, "{0}ting {1}\n     to {2} ...", 
								 "Put",
//			         			 localFilePath,
//			         			 remoteFilePath);
			         			 fileName,
			         			 remotepath+DIR_SEPERATOR+remoteFilePath);

			if (IsConnected) {
				// store the pwd
				string pwd = PWD;
				
				// change to the requested directory
				string[] dirs = Path.GetDirectoryName(remoteFilePath).Split(dirseps);
				foreach(string dir in dirs) {
					CWD(dir, createDirsOnDemand);
				}
				
				this.Log(Level.Info, "Putting the file as '{0}'", FtpType);
				_client.TransferType = FtpType;
				_client.Put(fileName, Path.GetFileName(remoteFilePath));
	
				if (PWD!=pwd) {
					this.Log(Level.Info, "Restoring the remote dir to {0}", pwd);
					CWD(pwd);
				}
			}
		}
		
		// get one file
		public void Get(string fileName,
		                 string localpath, 
		                 string remotepath,
		                 FTPTransferType FtpType, 
		                 bool flatten, 
		                 bool createDirsOnDemand) {

			// TODO: HERE!  woohoo!  the remote parsing works for one test-case!
			
			char [] dirseps = {DOS_DIR_SEPERATOR, DIR_SEPERATOR};

			string localFilePath = String.Empty;	// path to 'fileName' locally, relative to 'localpath'
			string remoteFilePath = String.Empty;	// path to 'fileName' remotely, relative to 'remotepath'
			
			// convert fileName into relative paths...
			if (Path.GetDirectoryName(fileName).StartsWith(localpath)) {
				
				// our abs path is longer than localpath, 
				// so the relative path is simple.
			
				localFilePath = fileName.Replace(localpath, String.Empty).Remove(0,1);
			
			} else if (localpath.StartsWith(Path.GetDirectoryName(fileName))) {

				// our abs path is shorter than the localpath
				// so our relative path is preceded by '..' references.
				
				localpath = localpath.Replace(Path.GetDirectoryName(fileName), String.Empty);

				int z = -1;

				z = localpath.IndexOfAny(dirseps);
				while( z >-1 ) {
					localFilePath += ".."+Path.DirectorySeparatorChar;
					localpath = localpath.Remove(z,1);
					z = localpath.IndexOfAny(dirseps);
				}
				localFilePath += Path.GetFileName(fileName);
			}
			
			// mirror the remote file path from the local file path, flattening if requested
			if (flatten) {
				remoteFilePath = Path.GetFileName(localFilePath);
			} else {
				remoteFilePath = localFilePath;
			}
			
			remoteFilePath = remoteFilePath.Replace(DOS_DIR_SEPERATOR, DIR_SEPERATOR);
			
			this.Log(Level.Info, "{0}ting {1}\n   from {2}\n     to {3} ...", 
								 "Get",
								 Path.GetFileName(fileName),
								 localFilePath,
			         			 remoteFilePath);

//			if (IsConnected) {
//				// store the pwd
//				string pwd = PWD;
//				
//				// change to the requested directory
//				string[] dirs = Path.GetDirectoryName(remoteFilePath).Split(dirseps);
//				foreach(string dir in dirs) {
//					CWD(dir, createDirsOnDemand);
//				}
//				
//				this.Log(Level.Info, "Putting the file as '{0}'", FtpType);
//				_client.TransferType = FtpType;
//				_client.Put(fileName, Path.GetFileName(remoteFilePath));
//	
//				if (PWD!=pwd) {
//					this.Log(Level.Info, "Restoring the remote dir to {0}", pwd);
//					CWD(pwd);
//				}
//			}
		}
		
		/// <summary>Download one file</summary>
		/// <param name="FileName">Download this filename</param>
		/// <param name="FtpType">using ascii or binary</param>
		private void downloadFile(string FileName, FTPTransferType FtpType) {
			string onlyFileName = EMPTY_STRING;
			string localRemoteFile = EMPTY_STRING;
			string downloadTo = EMPTY_STRING;
			FileSet workingFileset = null;

			if (FTPTransferType.ASCII == FtpType) {
				workingFileset = _downloadAscii;
			} else {
				workingFileset = _downloadBinary;
			} // if
			FileInfo fi = new FileInfo(FileName);
			onlyFileName = fi.Name;
			fi = null;

			if (!_remotePath.Equals(EMPTY_STRING)) {
				if (_remotePath.EndsWith(DIR_SEPERATOR.ToString())) {
					localRemoteFile = _remotePath + onlyFileName;
				} else {
					localRemoteFile = _remotePath + DIR_SEPERATOR + onlyFileName;
				} // if
				fi = null;
			} // if

			if (!workingFileset.BaseDirectory.FullName.EndsWith(DIR_SEPERATOR.ToString()) &&
			    !workingFileset.BaseDirectory.FullName.EndsWith(DOS_DIR_SEPERATOR.ToString())) {
				downloadTo = workingFileset.BaseDirectory.FullName.Replace("\\", "/") + DIR_SEPERATOR + onlyFileName;
			} else {
				downloadTo = workingFileset.BaseDirectory.FullName.Replace("\\", "/") + onlyFileName;
			} // if

			this.Log(Level.Verbose, "Downloading file {0} to {1}", localRemoteFile, downloadTo);

			_client.TransferType = FtpType;
			_client.Get(downloadTo, localRemoteFile);
			//_ftpConn.GetFile(localRemoteFile, downloadTo, FtpType);
			return;
		} // downloadFile()

		/// <summary>Upload one file</summary>
		/// <param name="FileName">the file to upload</param>
		/// <param name="FtpType">use this type (ascii/binary)</param>
		private void uploadFile(string FileName, FTPTransferType FtpType) {
			string localRemoteFile = EMPTY_STRING;
			if (!_remotePath.Equals(EMPTY_STRING)) {
				FileInfo fi = new System.IO.FileInfo(FileName);
				if (!fi.Exists) {
					this.Log(Level.Verbose, "Upload aborted: file does not exist");
					return;
				} // if
				if (_remotePath.EndsWith(DIR_SEPERATOR.ToString())) {
					localRemoteFile = _remotePath + fi.Name;
				} else {
					localRemoteFile = _remotePath + DIR_SEPERATOR + fi.Name;
				} // if
				fi = null;
			} else {
				FileInfo fi = new System.IO.FileInfo(FileName);
				if (!fi.Exists) {
					this.Log(Level.Verbose, "Upload aborted: file does not exist");
					return;
				} // if
				localRemoteFile = fi.Name;
				fi = null;
			} // if

			this.Log(Level.Verbose, "Uploading file {0} to {1}", FileName, localRemoteFile);
			_client.TransferType = FtpType;
			_client.Put(localRemoteFile, FileName);
			//_ftpConn.SendFile(FileName, localRemoteFile, FtpType);
			return;
		} // uploadFile()
		#endregion
		
		#region helper functions
		
		// parses connectmode attribute values, ensures that they are valid,
		// and returns the corresponding edtFTPnet enum value that can be 
		// passed directly to the edtFTPnet FTPClient.
		public static FTPConnectMode ParseConnectMode(string amode)
		{
			FTPConnectMode theMode;
			
			switch (amode.ToUpper()) {
				case "ACTIVE":
					theMode = FTPConnectMode.ACTIVE;
					break;
				case "PASSIVE":
					theMode = FTPConnectMode.PASV;
					break;
				default:
					throw new BuildException(String.Format("Invalid connectmode attribute '{0}'.\nMust be either 'active' or 'passive' (case-insensitive).", amode));
			}			
			
			return theMode;
		}

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
                // reference not found exception
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Connection '{0}' is not defined in the current project.", _connection.RefID), 
                    this.Location);
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
// scripting disabled for connecting to edtFTPnet-1.1.3
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
//			return;
		} // script()

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

		#endregion
		
	} // class
} // namespace

