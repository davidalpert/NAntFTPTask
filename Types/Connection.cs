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
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;
using NAnt.Core.Types;

using EnterpriseDT.Net.Ftp;
using PasswordInputManager = ConsolePasswordInput.ConsolePasswordInput;

using Sourceforge.NAnt.Ftp;
using Sourceforge.NAnt.Ftp.Enum;
using Sourceforge.NAnt.Ftp.Tasks;


namespace Sourceforge.NAnt.Ftp.Types {
	/// <summary>
	/// Extends the <see cref="Credential"/> type to provides connection details for connecting to a remote host.
	/// </summary>
	/// <remarks>This class now acts as a wrapper to insulate the third-party libraries.</remarks>
	[ElementName("connection")]
	public class Connection : Credential {
		
		const string PasswordFile = ".passwords";
        //private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
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
		
		#region attributes
        /// <summary>
        /// The server target for this connection.  Provides an alternate name for the <see cref="Domain"/> attribute.
        /// </summary>
        [TaskAttribute("server", Required=true)]
        public string Server {
            get { return base.Domain; }
            set { base.Domain = StringUtils.ConvertEmptyToNull(value); }
        }
		
        [TaskAttribute("domain", Required=false)]
        public new string Domain {
        	get { return base.Domain;}
        	set { base.Domain=value; }
        }
        #endregion
		
        #region private members
 		private FTPClient  	_client 	= null;	// reference into the FTP library
 		private FTPTask		_task		= null;	// reference used for logging
 		#endregion
        
        #region public access 
        #endregion
        #region connect / disconnect
        
		/// <summary>Gets a bool flag indicating whether or not the connection is open.</summary>
		/// <value><b>True</b> if the client exists; else <b>false</b>.</value>
		public bool IsConnected {
			get { return _client!=null;}
		}

		/// <summary>Open a connection to the specified server.</summary>
		/// <param name="task">The task using this connection</param>
		public void Open(Task task) {

			if (_task!=null) {
				throw new FTPTaskException("This connection is already being used by "+_task.ToString());
			}

			if (task==null) {
				throw new FTPTaskException("Must specify the task that will use this connection.");
			}

			if (IsConnected) {
				throw new FTPTaskException("This connection is already open.");
			}

			Validate();
			
			if (!_task.Exec) {
				_task.Log(Level.Info, "-------------- Debugging the ftp queries --------------");
				_task.Log(Level.Info, "Connection will be attempted to scan remotely for <get> sets");
				_task.Log(Level.Info, "but no transfers will be attempted in either direction");
				_task.Log(Level.Info, "and neither local nor remote file trees will be modified.");
				_task.Log(Level.Info, "-------------------------------------------------------\n");				
			}
			
			_task.Log(Level.Info, "Connecting to '{0}' as '{1}' . . .", Server, UserName);

			_task.Log(Level.Verbose, "Instantiating the FTPClient & opening the connection...");
			_client = new FTPClient(Server);
			
			_task.Log(Level.Verbose, "Authenticating...");
			
			Login(UserName, Password);
			
			_task.Log(Level.Verbose, "and setting the connection mode to passive.");
			_client.ConnectMode = ParseConnectMode(ParseConnectMode(_task.ConnectMode));

			return;
		} // Open()

		/// <summary>Do a remote login while asking the user for a password through the console.</summary>
		private void Login(string user, string pass) {
			bool success = false;
			bool abort = false;
			
			bool promptu = user.ToUpper()=="PROMPT";
			bool promptp = pass.ToUpper()=="PROMPT";
			
			string defaultuser = "anonymous";
			
			string username = user;
			string password = pass;
			
			PasswordInputManager pim = new PasswordInputManager();

			while (!success && !abort) {
				
				if (promptu) {
					_task.Log(Level.Info, "Please Enter Username:");
					Console.Write("Username ["+defaultuser+"]>");
					username = Console.ReadLine();
					if (username==String.Empty) {
						username = defaultuser;
					}
					defaultuser = username;
				}
				
				if (promptp) {
					_task.Log(Level.Info, "Please Enter Password:");
					Console.Write("Password [  abort  ]>");
					password = String.Empty;
					pim.PasswordInput(ref password, 256);
					Console.WriteLine(String.Empty);
				}
				
				if (password==String.Empty) {
					abort = true;
				} else {
					// todo: validate input
					try {
						_client.User(username);
						_client.Password(password);
						success = true;
					} catch (FTPException ex) {
						_task.Log(Level.Info, ex.Message);
						_task.Log(Level.Info, "Please Re-Enter Password or Press Enter to Abort.");
					}
				}
			}
			if (abort) {
				throw new FTPTaskException("Login Aborted by User");
			}
		} // Login
		
		/// <summary>Close a connection to a server</summary>
		public void Close() {
			if (IsConnected) {
				//throw new FTPTaskException("Connection is already closed");
				_task.Log(Level.Info, "Disconnecting from '{0}'", Server);
				_client.Quit();
				_client = null;
				_task = null;
			
			}

			return;
		} // ftpDisconnect()

        /// validates a connection instance
        private void Validate() {
        
        	if (Server==null || Server==String.Empty) {
        		throw new FTPTaskException("Must specify a server before opening a connection.");
        	}
        	
        	if (Password!=null && Password!=String.Empty && (UserName==null || UserName==String.Empty)) {
        		//throw new FTPTaskException("You cannot specify a password without a username.");       		
				UserName = "anonymous";
        	}

        	LoadPasswordFile();
        }
        
        /// Loads username and password info from a file
        private void LoadPasswordFile() {
			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(PasswordFile);
				string xpth = "/passwords/ftp[@connection=\""+ID+"\"]";
				XmlNode n = doc.SelectNodes(xpth)[0];
				//_task.Log(Level.Info, e.Attributes["password"].InnerText);
				if (n.Attributes["username"]!=null) {
					UserName = n.Attributes["username"].Value;
				}
				if (n.Attributes["password"]!=null) {
					Password = n.Attributes["password"].Value;
				}
			} catch (Exception ex) {
        		ex.ToString();
        		//throw new FTPTaskException("Error reading password file.", ex);
			}
        }

        #endregion
        
        #region enum translation
		// parses connectmode attribute values, ensures that they are valid,
		// and returns the corresponding edtFTPnet enum value that can be 
		// passed directly to the edtFTPnet FTPClient.
		public static ConnectMode ParseConnectMode(string amode)
		{
			ConnectMode theMode;
			
			try {
				theMode = (ConnectMode)(ConnectMode.Parse(typeof(ConnectMode),amode.ToUpper()));
			} catch (Exception ex) {
				throw new FTPTaskException("'"+amode+"' is an invalid ConnectMode.", ex);
			}
			
			return theMode;	
//			switch (amode.ToUpper()) {
//				case "ACTIVE":
//					theMode = ConnectMode.ACTIVE;
//					break;
//				case "PASSIVE":
//					theMode = ConnectMode.PASSIVE;
//					break;
//				default:
//					throw new BuildException(String.Format("Invalid connectmode attribute '{0}'.\nMust be either 'active' or 'passive' (case-insensitive).", amode));
//			}			
//			
//			return theMode;
		}
		// parses connectmode attribute values, ensures that they are valid,
		// and returns the corresponding edtFTPnet enum value that can be 
		// passed directly to the edtFTPnet FTPClient.
		public static FTPConnectMode ParseConnectMode(ConnectMode amode)
		{
			FTPConnectMode theMode;
			
			switch (amode) {
				case ConnectMode.ACTIVE:
					theMode = FTPConnectMode.ACTIVE;
					break;
				case ConnectMode.PASSIVE:
					theMode = FTPConnectMode.PASV;
					break;
				default:
					throw new FTPTaskException(String.Format("Cannot parse ConnectionMode '{0}'.\nMust be either '{1}' or '{2}'.", amode, ConnectMode.ACTIVE, ConnectMode.PASSIVE));
			}			
			
			return theMode;
		}
        #endregion
        
        #region authentication
        #endregion
        
        #region navigation
        #endregion
        
        #region transfers
        #endregion
//        #region logging - copied from NAnt.Core.Task (lines 212-287)
//        /// <summary>
//        /// Logs a message with the given priority.
//        /// </summary>
//        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
//        /// <param name="message">The message to be logged.</param>
//        /// <remarks>
//        /// <para>
//        /// The actual logging is delegated to the project.
//        /// </para>
//        /// <para>
//        /// If the <see cref="Verbose" /> attribute is set on the task and a 
//        /// message is logged with level <see cref="Level.Verbose" />, the 
//        /// priority of the message will be increased to <see cref="Level.Info" />.
//        /// when the threshold of the build log is <see cref="Level.Info" />.
//        /// </para>
//        /// <para>
//        /// This will allow individual tasks to run in verbose mode while
//        /// the build log itself is still configured with threshold 
//        /// <see cref="Level.Info" />.
//        /// </para>
//        /// </remarks>
//        public override void Log(Level messageLevel, string message) {
//            if (!IsLogEnabledFor(messageLevel)) {
//                return;
//            }
//
//            if (_verbose && messageLevel == Level.Verbose && Project.Threshold == Level.Info) {
//                Project.Log(this, Level.Info, message);
//            } else {
//                Project.Log(this, messageLevel, message);
//            }
//        }
//
//        /// <summary>
//        /// Logs a formatted message with the given priority.
//        /// </summary>
//        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
//        /// <param name="message">The message to log, containing zero or more format items.</param>
//        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
//        /// <remarks>
//        /// <para>
//        /// The actual logging is delegated to the project.
//        /// </para>
//        /// <para>
//        /// If the <see cref="Verbose" /> attribute is set on the task and a 
//        /// message is logged with level <see cref="Level.Verbose" />, the 
//        /// priority of the message will be increased to <see cref="Level.Info" />.
//        /// when the threshold of the build log is <see cref="Level.Info" />.
//        /// </para>
//        /// <para>
//        /// This will allow individual tasks to run in verbose mode while
//        /// the build log itself is still configured with threshold 
//        /// <see cref="Level.Info" />.
//        /// </para>
//        /// </remarks>
//        public override void Log(Level messageLevel, string message, params object[] args) {
//            string logMessage = string.Format(CultureInfo.InvariantCulture, message, args);
//            Log(messageLevel, logMessage);
//        }
//
//        /// <summary>
//        /// Determines whether build output is enabled for the given 
//        /// <see cref="Level" />.
//        /// </summary>
//        /// <param name="messageLevel">The <see cref="Level" /> to check.</param>
//        /// <returns>
//        /// <see langword="true" /> if messages with the given <see cref="Level" />
//        /// will be output in the build log; otherwise, <see langword="false" />.
//        /// </returns>
//        public bool IsLogEnabledFor(Level messageLevel) {
//            if (_verbose && messageLevel == Level.Verbose && Project.Threshold == Level.Info) {
//                return Level.Info >= Threshold;
//            }
//
//            return (messageLevel >= Threshold) && (messageLevel >= Project.Threshold);
//        }
//        #endregion
	}
}
