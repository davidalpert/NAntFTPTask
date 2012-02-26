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
// $Log: TestListings.cs,v $
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
	/// Tests the various commands that list directories
	/// </summary>
	/// <author>      Bruce Blackshaw
	/// </author>
	/// <version>     $Revision: 1.2 $
	/// </version>
	[TestFixture]
    [Category("edtFTPnet")]
	public class TestListings:FTPTestCase
	{
		/// <summary>  
		/// Get name of log file
		/// </summary>
		override internal string LogName
		{
			get
			{
				return "TestListings.log";
			}
		}
				
		/// <summary>  Test directory listings</summary>
		[Test]
		public virtual void Dir()
		{
			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			// list current dir
			string[] list = ftp.Dir();
			Print(list);
			
			// list current dir by name
			list = ftp.Dir(".");
			Print(list);
			
			// list empty dir by name
			list = ftp.Dir(remoteEmptyDir);
			Print(list);
			
			// non-existent file
			string randomName = GenerateRandomFilename();
			try
			{
				list = ftp.Dir(randomName);
				Print(list);
			}
			catch (FTPException ex)
			{
				if (ex.ReplyCode != 550)
					Assert.Fail("Dir(" + randomName + ") should throw 550 for non-existent dir");
			}
			
			ftp.Quit();
		}
		
		/// <summary>  Test full directory listings</summary>
		[Test]
		public virtual void DirFull()
		{
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			// list current dir by name
			string[] list = ftp.Dir(".", true);
			Print(list);
			
			log.Debug("******* DirDetails *******");
			FTPFile[] files = ftp.DirDetails(".");
			Print(files);
			log.Debug("******* end DirDetails *******");
			
			// list empty dir by name
			list = ftp.Dir(remoteEmptyDir, true);
			Print(list);
			
			// non-existent file. Some FTP servers don't send
			// a 450/450, but IIS does - so we catch it and
			// confirm it is a 550
			string randomName = GenerateRandomFilename();
			try
			{
				list = ftp.Dir(randomName, true);
				Print(list);
			}
			catch (FTPException ex)
			{
				if (ex.ReplyCode != 550)
					Assert.Fail("Dir(" + randomName + ") should throw 550 for non-existent dir");
			}
			
			ftp.Quit();
		}
		
		/// <summary>  Helper method for dumping a listing
		/// 
		/// </summary>
		/// <param name="list">  directory listing to print
		/// </param>
		private void Print(string[] list)
		{
			log.Debug("Directory listing:");
			for (int i = 0; i < list.Length; i++)
				log.Debug(list[i]);
			log.Debug("Listing complete");
		}
		
		/// <summary>  
		/// Helper method for dumping a listing
		/// </summary>
		/// <param name="list">  directory listing to print
		/// </param>
		private void Print(FTPFile[] list)
		{
			log.Debug("Directory listing:");
			for (int i = 0; i < list.Length; i++)
			{
				log.Debug(list[i].ToString());
			}
			log.Debug("Listing complete");
		}
	}
}
