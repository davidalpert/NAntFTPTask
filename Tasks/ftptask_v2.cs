/*
 * Created by SharpDevelop.
 * User: David
 * Date: 12/15/2004
 * Time: 9:56 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

using EnterpriseDT.Net.Ftp;

namespace Sourceforge.NAnt.Ftp.Tasks
{
	/// <summary>An NAnt FTP Task</summary>
	[TaskName("ftp")]
	public class ftptask : Task
	{
		#region private members
		private FTPClient _client;
		private string _remoteHost;
		

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
		
		#region public properties
		/// <summary>The property</summary>
		[TaskAttribute("remotehost", Required=false)]
		public string remotehost {
			get {
				return _remoteHost;
			} set {
					_remoteHost = value;
				}
		} // basepath
		#endregion
		
		public ftptask()
		{
		}

		#region ///////////////////// task functions
		
		/// required to interface with the nant task system
		protected override void InitializeTask(System.Xml.XmlNode taskNode) {
			base.InitializeTask (taskNode);

//			XmlNodeList scriptList = taskNode.SelectNodes(SCRIPT_NODE_NAME, NamespaceManager);
//			if (0 == scriptList.Count) {
//				_script = EMPTY_STRING;
//			} else {
//				if (1 == scriptList.Count) {
//					_script = scriptList.Item(0).InnerText;
//				} else {
//					throw new BuildException("Only one <script> block allowed.", Location);
//				} // if
//			} // if
//			scriptList = null;

			return;
		} // InitializeTask()


		protected override void ExecuteTask() {
//			Work();
			return;
		} // ExecuteTask()
		#endregion
	}
}

