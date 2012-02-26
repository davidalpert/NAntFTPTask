Running the edtFTPnet demo.

1. Open a console.

2. Change to the demo directory (i.e. the directory where this readme file is).

3. Edit demo.bat if csc (the C# compiler) is not in your PATH

4. Run the test by:
     demo hostname username password
   where
     hostname is the address/name of the FTP server.
     username is the name of the user account on the FTP server.
     password is the password of the user account on the FTP server.

5. The expected output of the test will be something like:

     Connecting
     Logging in
     Directory before put:
     Putting file
     Directory after put
     -rw-rw-rw-   1 user     group           7 Nov 16 13:59 test.txt
     Getting file
     Deleting file
     Directory after delete
     Quitting client
     Test complete

   There should also be a file called "readme.txt.copy" in the local directory.
   
   