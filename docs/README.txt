FTPTask: an <ftp> task for NAnt 0.85-rc1
----------------------------------------

FTPTask is a set of elements for use in NAnt build files that enable basic FTP transfers.


1. License
----------

	- This project is distributed under the GPL, just like other NAnt code.



2. Installation
---------------

	- use a <loadtasks> element to load the 'ftptask.dll' assembly at runtime.
	- copy the assemblies from bin\release to your NAnt\bin\tasks directory for inclusion in every run.



3. Building the Source
----------------------

	- run 'nant build.debug' or 'nant build.release' on ftptask.build to compile this task assembly from the command line, or open the .cmbx combine file in SharpDevelop.



4. Documentation
----------------

	- help documentation is included as html in the docs\help directory in the same format as other NAnt docs.  

	- 'nant userdoc' will build/update the help documentation from the sourcecode using the NAnt custom documenter.

	- edtFTPnet-1.1.3 class library docs are included as well for reference.

	- Sascha Andres original posts to the NAnt-dev group are included for reference in the docs folder.



5. 3rd Party Libs
-----------------

	- edtFTPnet-1.1.3 is included as per the LGPL license

	- ConsolePasswordInput is included as released by Lim Bio Liong on CodeProject in the article '.NET Console Password Input By Masking Keyed-In Characters' (http://www.codeproject.com/dotnet/ConsolePasswordInput.asp - accessed on 12/18/2004)

	- Source code for 3rd Party Libs is available in the 'libsrc' zip file posted alongside this 'src' file.

6. TODO
-------

- clean up / rework the architecture of the FTPTask suite to be better organized.
- refactor the remotescanning code into a RemoteFileSet that can be used with other FTP commands like 'rdelete' or 'rcopy'.

 

7. Change Log
-------------

December 2004	- David Alpert updates the ftp task to use the LGPL edtFTPnet library to replace FTPClient as a more robust back-end.  He also modifies the <ftp> task to accept any number of <put type="ascii|bin"> and <get type="ascii|bin"> children, processed in order of inclusion.  Both <put> and <get> are extensions of <FileSet> and make use of their include features.  <get> has been extended to process includes against remote directory contents.

June 2004	- Sascha releases a third version, adding support for a <script> element.

June 2004	- Sascha releases the second version, replacing the two tasks with a single <ftp> task that accepts four cast FileSet elements (up-binary, up-ascii, down-binary and down-ascii).

June 2004	- Sascha releases the first version through his website and a post to the [NAnt-dev] mailing list.  This version supports two tasks (<ftp-up> and <ftp-down>) and uses the http://sourceforge.net/projects/dotnetftpclient/ FTP libraries.

Summer 2004	- Sascha Andres begins development of an <ftp> task for NAnt
