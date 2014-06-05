Dark1git is a git client that works within Unity.
It features a user-friendly interface that makes it easy for beginners and experienced users to use git.

------------------------------
INSTALLATION
------------------------------

1. Import the package to your project.
2. Move all contents in the Assets/Root folder up one level, to the Assets folder.

WINDOWS
2. Download a PortableGit from https://code.google.com/p/msysgit/downloads/list - At the time of writing this document PortableGit-1.8.4-preview20130916.7z is the stable version used to develop this, however, all newer versions should work without issues.
3. Extract PortableGit into Assets/Editor/Dark1Git/PortableGit so this folder contains all the portablegit subfolders such as bin, cmd, doc...
4. Open command prompt, cd into the PortableGit/bin folder  (if you have never used cd, read this short tutorial: http://www.c3scripts.com/tutorials/msdos/cd.html)
5. Type the following into command prompt by replacing the dummy data with your data to configure git:
	git.exe config --global user.name "Your Name"
	git.exe config --global user.email your@email.com
5. Hide the PortableGit folder so that Unity does not try to import it.

MAC OS
2. Download and install git from http://git-scm.com/download/mac
3. Configure git by starting the terminal and running the following while replacing the dummy data with your data (If you haven't used git before on this installation):
	git config --global user.name "Your Name"
	git config --global user.email your@email.com


------------------------------
USAGE
------------------------------

To use Dark1git, go to Window->Dark1Git.  The git window should show up.
If you're starting a new project, click the Init button.
If you already have a project set up on a remote repository for which you have the access to, type in the URL (if using ssh/https), the username and password (if using https only)
The middle panel shows your changes to the local repository. 
To refresh the list of files, press Refresh.

To stage a change (prepare it to be pushed to the common, remote repository) click the >>> button (This will move the changed files to the right panel).

There are a few colored icons for each type of change to files:
NEW means the file was just created and did not exist in the repository before.
MOD means you have modified the file
DEL means the file has been deleted on the local repository
STGD means you have chosen to stage this file

The right panel shows the staged changes (Changes you have made and that are ready to be committed and pushed to the remote repository). 

To unstage files (If you don't want your changes to go to the remote repository), click the <<< button (This will move the selected files on the right panel to the middle panel).
If you think your change to a file that is not staged was bad (Perhaps you broke something, or you made something you don't like), you can revert the file to the original version before you changed it, select the files and click Checkout selected. This will remove the files from the middle panel and they will be restored to their original versions.
When you have staged all your changes and you want to commit them (Save the state of the project with the new changes), Type down your commit message and press Commit (Upper right).

After the commit is successfull you can Pull (Get all new changes others have made to the remote repository) and Push your changes to the remote so other team members can get it (Warning: the Pull feature is still in development, so merging works correctly only when there are no conflicts on binary files (Levels, images, audio etc). If this occurs you need to manually fix the conflicts by using git through command line or terminal).



------------------------------
CONTACT
------------------------------

If you have any question or issues email us at:
contact@dark-1.com