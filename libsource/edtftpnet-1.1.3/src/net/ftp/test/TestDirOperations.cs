// edtFTPnet
// 
// Copyright (C) 2004 Enterprise Distributed Technologies Ltd
// 
// www.enterprisedt.com
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// Bug fixes, suggestions and comments should posted on 
// http://www.enterprisedt.com/forums/index.php
// 
// Change Log:
// 
// $Log: TestDirOperations.cs,v $
// Revision 1.2  2004/11/20 22:39:32  bruceb
// added to edtFTPnet test category
//
// Revision 1.1  2004/11/13 19:14:33  bruceb
// first cut of tests
//
//

using System;
using FTPException = EnterpriseDT.Net.Ftp.FTPException;
using NUnit.Framework;

namespace EnterpriseDT.Net.Ftp.Test
{
	
	/// <summary>  
    /// Tests directory navigation and directory creation/deletion functionality
	/// </summary>
	/// <author>          
    ///  Bruce Blackshaw
	/// </author>
	/// <version>         
    ///  $Revision: 1.2 $
	/// </version>
	[TestFixture]
    [Category("edtFTPnet")]
	public class TestDirOperations:FTPTestCase
	{
		/// <summary>  Get name of log file
		/// 
		/// </summary>
		/// <returns> name of file to log to
		/// </returns>
		override internal string LogName
		{
			get
			{
				return "TestDirOperations.log";
			}
		}
		
		/// <summary>
        /// Test we can make a directory, and change
		/// into it, and remove it
		/// </summary>
		[Test]
		public void Dir()
		{
			log.Debug("Testing Dir()");
			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			// mkdir
			string dir = GenerateRandomFilename();
			ftp.MkDir(dir);
			
			// chdir into new dir
			ftp.ChDir(dir);
			
			// pwd
			string wd = ftp.Pwd();
			log.Debug("PWD: " + wd);
			Assert.IsTrue(wd.IndexOf(dir) >= 0);
			
			// remove the dir
			ftp.ChDir("..");
			ftp.RmDir(dir);
			
			// check it doesn't exist
			try
			{
				ftp.ChDir(dir);
				Assert.Fail("ChDir(" + dir + ") should have failed!");
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			ftp.Quit();
		}
		
		
		/// <summary>Test renaming a dir</summary>
		[Test]
		public void RenameDir()
		{		
			log.Debug("Testing RenameDir()");
			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			// mkdir
			string dir1 = GenerateRandomFilename();
			ftp.MkDir(dir1);
			
			// chdir into new dir and out again
			ftp.ChDir(dir1);
			ftp.ChDir("..");
			
			// generate another name guaranteed to be different
			// and rename this dir
			char[] chars = dir1.ToCharArray();
			Array.Reverse(chars);
			string dir2 = new string(chars);
			ftp.Rename(dir1, dir2);
			ftp.ChDir(dir2);
			string wd = ftp.Pwd();
			Assert.IsTrue(wd.IndexOf(dir2) >= 0);
			
			// remove the dir
			ftp.ChDir("..");
			ftp.RmDir(dir2);
			
			// check it doesn't exist
			try
			{
				ftp.ChDir(dir2);
				Assert.Fail("chdir(" + dir2 + ") should have failed!");
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			ftp.Quit();
		}
	}
}
