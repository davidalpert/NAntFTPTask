<!DOCTYPE html PUBLIC "-//w3c//dtd xhtml 1.1 strict//en" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
    <head>
        <title>FTPTask - An FTP task for NAnt</title>
        <meta http-equiv="Content-Language" content="en-ca" />
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <link href="help/style.css" type="text/css" rel="stylesheet" />
    </head>
    <body>
        <table class="NavBar" cellspacing="0" cellpadding="2" width="100%" border="0">
            <tr>
                <td class="NavBar-Cell">
                    <b>FTPTask </b> : An FTP task for <a href="http://nant.sourceforge.net/">NAnt</a>
                </td>
            </tr>
        </table>
        <h1>
            <span class="N">FTPTask</span></h1>
        <p>
            FTPTask is an FTP Task for NAnt. <br />
          Don't know what
            NAnt is ? Then you need to go <a href="http://nant.sourceforge.net/">here.</a>        </p>
        <h2>News</h2>
        <p>
            <img alt=">" src="help/images/bullet.gif" />
            <span style="font-weight:bold; font-style:italic; color:red;">2004/12/24:
        </span><br />
&nbsp;        FTPTask v1.0.40.85 is available NOW !<br />
        &nbsp;&nbsp;A list of the new features is available <a href="releasenotes.html">here</a>. </p>
        <h2>Getting Started</h2>
        <p>
            <img alt=">" src="help/images/bullet.gif" />
        Download one of the following distribution files</p>
        <ul>
<?php
$d = dir(".");
while (false !== ($entry = $d->read())) {
    if (preg_match('/\.zip$/',$entry)>0) {
		echo "<li>".$entry."</li>\n";
	}
}
$d->close();
?>
        </ul>
        <p>&nbsp;&nbsp;(Source code for the 3rd party libraries is included in the 'libsrc.zip' files.) </p>
        <p>
            <img alt=">" src="help/images/bullet.gif" />
        Read the <a href="help/index.html">user documentation</a>.</p>
        <p>
            <img alt=">" src="help/images/bullet.gif" />
            Want to contribute or submit patches? Send email to <a href="mailto:ftptask@spinthemoose.com">ftptask@spinthemoose.com</a></p>
        <h2>User Information</h2>
        <ul>
            <li>
                <a href="license.html">License</a>
            </li>
        </ul>
        <h2>Contributors</h2>
        <ul>
          <li>David Alpert (<a href="mailto:ftptask@spinthemoose.com">ftptask@spinthemoose.com</a>)</li>
        <li>Sascha Andres </li>
        </ul>
        <p style="text-align: right;"><br />
    Last Updated Friday, December 24, 2004</p>
    </body>
</html>
