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

#if DEBUG

using System;
using System.Globalization;
using System.IO;

using NUnit.Framework;

using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;

using NAnt.Core;

using Sourceforge.NAnt.Ftp.Tasks;
using Sourceforge.NAnt.Ftp.Types;

namespace Tests.Sourceforge.NAnt.Ftp.Tasks {
    [TestFixture]
    public class FtpTaskTest : BuildTestBase {

    	#region Private Instance Fields
        
    	private string _fileName;
        
    	#endregion Private Instance Fields

        #region Private Static Fields

        private const string _buildFormatStr = 
        	@"<?xml version='1.0'?>
			<project name='ftp tasks for nant' default='test'>
				<property name='debug' value='true' />
				<property name='server' value='spinthemoose.com' />
				<property name='username' value='spinthemoose' />
				<property name='password' value='media!moose' />
				
				<target name='test' >
				    <loadtasks assembly='c:\dev\nant_ftp_task\trunk\bin\debug\ftptask.dll' />
					{0}
				</target>			
			</project>
			";

        private const string _text1 = @"
 			Hello World! 
 			An ascii file.
			";

        private const string _text2 = @"
 			Goodbye World! 
 			Another ascii file.
			";

        #endregion Private Static Fields

        #region Override implementation of BuildTestBase

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _fileName = Path.Combine(TempDirName, ".txt");
            TempFile.CreateWithContents(_text1, _fileName);
            _fileName = Path.Combine(TempDirName, "2.txt");
            TempFile.CreateWithContents(_text2, _fileName);
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        /// <summary>Test to make sure the <see cref="FTPTask">FtpTask</see> exists</summary>
        /// <remarks>Throws a TestBuildException because no server has been defined.</remarks>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_EmptyFtpElement() {        	
        	string test = @"
				<ftp />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        /// <summary>Test to make sure the <see cref="Connection">Connection</see> type exists</summary>
        /// <remarks>Throws a TestBuildException because server is a required attribute of connection.</remarks>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ConnectionType() {        	
        	string test = @"
				<connection />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        /// <summary>Testing <see cref="Connection">Connection</see> with server attribute.</summary>
        /// <remarks>Throws a TestBuildException because id is a required attribute of connection.</remarks>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ConnectionWithServer() {        	
        	string test = @"
				<connection server='${server}' />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        /// <summary>Testing <see cref="Connection">Connection</see> with server and id attributes.</summary>
        [Test]
        public void Test_ConnectionWithIDServer() {        	
        	string test = @"
				<connection id='myconn' server='${server}' />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        /// <summary>Testing <see cref="Connection">Connection</see> with server, id, and username attributes.</summary>
        [Test]
        public void Test_ConnectionWithServerUsername() {        	
        	string test = @"
				<connection id='myconn' server='${server}' username='${username}' />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        /// <summary>Testing <see cref="Connection">Connection</see> with server, id, username, and password attributes.</summary>
        [Test]
        public void Test_ConnectionWithServerUsernamePassword() {        	
        	string test = @"
				<connection id='myconn' server='${server}' username='${username}' password='${password}' />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }


        /// <summary>Testing a connection with a <see cref="Connection">Connection</see> reference with server and id attributes.</summary>
        /// <remarks>Throws a TestBuildException because the connection with id='dummy' is not defined.</remarks>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ConnectingWithIDServerDummyConnection() {        	
        	string test = @"
				<connection id='myconn' server='${server}' />
				<ftp connection='dummy' />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        /// <summary>Testing a connection with a <see cref="Connection">Connection</see> reference with server and id attributes.</summary>
        [Test]
        public void Test_ConnectingWithIDServer() {        	
        	string test = @"
				<connection id='myconn' server='${server}' />
				<ftp connection='myconn' />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        /// <summary>Testing <see cref="Connection">Connection</see> with server, id, and username attributes.</summary>
        /// <remarks>Expect an FTPException because there is no user input to enter the password.</remarks>
        [Test]
        [ExpectedException(typeof(EnterpriseDT.Net.Ftp.FTPException))]
        public void Test_ConnectingWithServerUsername() {        	
        	string test = @"
				<connection id='myconn' server='${server}' username='${username}' />
				<ftp connection='myconn' />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        /// <summary>Testing <see cref="Connection">Connection</see> with server, id, username, and password attributes.</summary>
        [Test]
        public void Test_ConnectingWithServerUsernamePassword() {        	
        	string test = @"
				<connection id='myconn' server='${server}' username='${username}' password='${password}' />
				<ftp connection='myconn' />
				";
        	
        	runtest(test);
            Assert.IsTrue(true, "ensure we make it here.");
        }

        #endregion
        
        #region Private Instance Methods

        // pop the test xml into the build file xml and run the build
        private string runtest(string testxml) {
        	return RunBuild(FormatBuildFile(testxml));
        }
        
        // 
        private string FormatBuildFile(string testxml) {
            return string.Format(CultureInfo.InvariantCulture, _buildFormatStr, 
        	                     testxml);
        }

        #endregion Private Instance Methods
    }
}
#endif
