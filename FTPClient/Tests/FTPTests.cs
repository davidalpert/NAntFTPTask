/*
 * Created by SharpDevelop.
 * User: David
 * Date: 12/14/2004
 * Time: 1:47 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
#if DEBUG

using System;
using NUnit.Framework;

using FTPClient;

namespace FTPClient.Tests
{
	[TestFixture]
	public class FTPTests
	{
		#region setup/teardown
		[TestFixtureSetUp]
		public void Init()
		{
			// Init code.
		}
		
		[TestFixtureTearDown]
		public void Dispose()
		{
			// tear down code.
		}
		#endregion
		
		[Test]
		public void Constructor()
		{
			FTPConnection myConn = new FTPConnection();
			Assert.IsNotNull(myConn, "Connection object should not be null.");
		}
		[Test]
		public void Open()
		{
			FTPConnection myConn = new FTPConnection();
			myConn.Open("uploads.sourceforge.net",
			            "anonymous",
			            "david@spinthemoose.com");
			Assert.IsTrue(myConn.IsConnected, "IsConnected should return true.");
		}
		[Test]
		public void Close()
		{
			FTPConnection myConn = new FTPConnection();
			myConn.Open("uploads.sourceforge.net",
			            "anonymous",
			            "david@spinthemoose.com");
			myConn.Close();
			Assert.IsFalse(myConn.IsConnected, "IsConnected should return false.");
		}
	}
}
#endif
