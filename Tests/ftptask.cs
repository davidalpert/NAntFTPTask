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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System.Globalization;
using System.IO;

using NUnit.Framework;

using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;

using NAnt.Core;

using NAnt.DotNet.Tasks;
using NAnt.DotNet.Types;

namespace Tests.NAnt.DotNet.Tasks {
    [TestFixture]
    public class CscTaskTest : BuildTestBase {

    	#region Private Instance Fields
        
    	private string _fileName;
        
    	#endregion Private Instance Fields

        #region Private Static Fields

        private const string _buildFormatStr = 
        	@"<?xml version='1.0'?>
			<project name='ftp tasks for nant' default='test'>
				<property name='debug' value='true' />			
				<target name='test1' >
					<ftp />
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
//            _fileName = Path.Combine(TempDirName, ".txt");
//            TempFile.CreateWithContents(_sourceCode, _fileName);
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        /// <summary>
        /// Test to make sure the ftp task exists
        /// </summary>
        [Test]
        public void Test_DebugBuild() {
            string result = RunBuild(FormatBuildFile(String.Empty));
            Assert.IsTrue(File.Exists(_sourceFileName + ".exe"),
                _sourceFileName + ".exe does not exists, program did compile.");
            // Comment this for now as its hard to know which framework was used to compile and it was mono there will be no pdb file.
            //Assert.IsTrue(File.Exists(_sourceFileName + ".pdb"), _sourceFileName + ".pdb does not exists, program did compile with debug switch.");
        }
	
        #endregion
        
        #region Private Instance Methods

        private string FormatBuildFile(string attributes) {
            return string.Format(CultureInfo.InvariantCulture, _buildFormatStr, 
                Path.GetFileName(_fileName), 
                Path.GetDirectoryName(_fileName), 
                attributes);
        }

        #endregion Private Instance Methods
    }
}
