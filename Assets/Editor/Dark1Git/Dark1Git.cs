using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;


namespace Dark1Git {
	
	public class Dark1Git : EditorWindow {
		
		
		private string url = "";
		private string username = "";
		private string password = "";
		
		private bool isInitialized = false;
		
		private string path;
		
		private bool running = false;
		private bool dirtyGUI = true;
		private bool dirtyBranches = true;
		private bool dirtyFiles = true;
		private bool dirtyHistory = true;
		private int selectedHistory = -1;
		
		//GUI
		private Texture2D guiAtlas;
		private Texture2D stgdTexture;
		private Texture2D newTexture;
		private Texture2D modTexture;
		private Texture2D delTexture;
		private int layoutSwitch = 600;
		private Vector2 hitoryScrollPosition = Vector2.zero;
		private Vector2 unstagedFilesScrollPosition = Vector2.zero;
		private Vector2 stagedFilesScrollPosition = Vector2.zero;
		private GUISkin skin;
		
		//Process
		private Process bashProcess;
		private string newBranchName;
		private string commitMessage;
		private string newTreeHash;
		private string newCommitHash;
		private string headRef;
		
		//Branch data
		List<Branch> branches = null;
		int selectedBranchIndex;
		
		//Files data
		List<File> files = new List<File>();
		List<bool> selectedFiles = new List<bool>();
		
		//History data
		List<HistoryItem> history = new List<HistoryItem>();
		
		[MenuItem ("Window/Dark1Git", false, 111)]
		static void ShowWindow () {
			EditorWindow.GetWindow<Dark1Git>("Git", true, typeof(SceneView));
		}
		
//		private void OnDeleted(object source, FileSystemEventArgs e) {
//			dirtyFiles = true;
//			if(e.Name.EndsWith(".git")){
//				isInitialized = false;
//				dirtyGUI = true;
//			}
//		}
//		
//		private void OnCreated(object source, FileSystemEventArgs e) {
//			dirtyFiles = true;
//			if(e.Name.EndsWith(".git")){
//				isInitialized = true;
//				dirtyGUI = true;
//			}
//		}
//		
//		private void OnRenamed(object source, RenamedEventArgs e) {
//			if(e.Name.EndsWith(".git")) {
//				isInitialized = true;
//				dirtyGUI = true;
//			}
//			else if(e.OldName.EndsWith(".git")) {
//				isInitialized = false;
//				dirtyGUI = true;
//			}
//		}
		
		private void EnsureLineEndings() {
			Process p = initProcess("config core.autocrlf false"); 
			p.WaitForExit();
		}
		
		void OnEnable() {
			url = EditorPrefs.GetString("gitUrl");
			username = EditorPrefs.GetString("gitUsername");
			password = EditorPrefs.GetString("gitPassword");
			
			skin = Resources.Load<GUISkin>("GUISkins/Dark1Git");
			
			
			//initialize status textures from atlas
			guiAtlas = Resources.Load<Texture2D>("Images/guiAtlas");
			
			stgdTexture = new Texture2D(25, 12);
			newTexture = new Texture2D(25, 12);
			modTexture = new Texture2D(25, 12);
			delTexture = new Texture2D(25, 12);
			
			Color[] stgdPixels = guiAtlas.GetPixels(0, 36, 25, 12);
			Color[] newPixels = guiAtlas.GetPixels(0, 24, 25, 12);
			Color[] modPixels = guiAtlas.GetPixels(0, 12, 25, 12);
			Color[] delPixels = guiAtlas.GetPixels(0, 0, 25, 12);
			
			stgdTexture.SetPixels(stgdPixels);
			newTexture.SetPixels(newPixels);
			modTexture.SetPixels(modPixels);
			delTexture.SetPixels(delPixels);
			
			stgdTexture.Apply();
			newTexture.Apply();
			modTexture.Apply();
			delTexture.Apply();
			
			isInitialized = Directory.Exists(".git");
			
			if(isInitialized){
				EnsureLineEndings();
			}
			
			#if UNITY_EDITOR_WIN
			path = AssetDatabase.GetAssetPath( MonoScript.FromScriptableObject( this ) );
			path = path.Substring(0, path.LastIndexOf('/'));
			path = path.Replace('/', '\\');
			#endif
//			
//			watcher = new FileSystemWatcher(".");
//			watcher.Deleted += OnDeleted;
//			watcher.Created += OnCreated;
//			watcher.Renamed += OnRenamed;
//			//			watcher.IncludeSubdirectories = true;
//			watcher.EnableRaisingEvents = true;
		}
		
		
		void Update() {
			if(dirtyGUI){
				dirtyGUI = false;
				Repaint();
			}
			if(isInitialized){
				if(dirtyBranches){
					dirtyBranches = false;
					PopulateBranches();
				}
				if(dirtyFiles){
					dirtyFiles = false;
					PopulateFiles();
				}
				if(dirtyHistory){
					dirtyHistory = false;
					PopulateHistory();
				}
				
			}
		}
		
		Process initProcess(string args){
			return initProcess(args, null, null);
		}
		
		//PROCESSES
		Process initProcess(string args, DataReceivedEventHandler output, DataReceivedEventHandler error) {
			if(bashProcess != null && !bashProcess.HasExited)
				bashProcess.WaitForExit();
			
			Process newProcess = new Process();
			#if UNITY_EDITOR_WIN
			newProcess.StartInfo.FileName = path+"\\PortableGit\\cmd\\git.exe";
			#else
			newProcess.StartInfo.FileName = "git";
			#endif
			
			UnityEngine.Debug.Log(args);
			newProcess.StartInfo.Arguments = args;
			newProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			newProcess.StartInfo.UseShellExecute = false;
			newProcess.StartInfo.CreateNoWindow = true;
			newProcess.StartInfo.RedirectStandardOutput = true;
			newProcess.StartInfo.RedirectStandardError = true;
			newProcess.EnableRaisingEvents = true;
			if(output != null)
				newProcess.OutputDataReceived += output;
			if(error != null)
				newProcess.ErrorDataReceived += error;
			newProcess.Exited += processExited;
			running = true;
			dirtyGUI = true;
			try{
				newProcess.Start();
			}
			catch(System.Exception e){
				UnityEngine.Debug.Log (e.Message);
			}
			if(output != null)
				newProcess.BeginOutputReadLine();
			if(error != null)
				newProcess.BeginErrorReadLine();
			return newProcess;
		}
		
		void processExited(object sender, object e) {
			running = false;
			dirtyGUI = true;
		}
		
		
		//BRANCHING
		void PopulateBranches() {
			branches = new List<Branch>();
			bashProcess = initProcess("branch -v", BranchDataReceived, null);
		}
		
		private Branch ParseBranchData(string data){
			bool isActive = false;
			if(data[0] == '*'){
				isActive = true;
			}
			data = data.Substring(2);
			
			//Get name
			int spacepos = data.IndexOf(" ");
			string name = data.Substring(0, spacepos);
			data = data.Substring(spacepos+1);
			
			//Get hash
			spacepos = data.IndexOf(" ");
			string hash = data.Substring(0, spacepos);
			string commitMessage = data.Substring(spacepos+1);
			
			return new Branch(name, hash, commitMessage, isActive);
		}
		
		void BranchDataReceived(object sender, DataReceivedEventArgs e) {
			if(e.Data != null){
				branches.Add(ParseBranchData(e.Data));
			}
		} 
		
		void Checkout(string reference){
			bashProcess = initProcess("checkout "+reference);
		}
		
		//Rebuild file data
		private string _file_status;
		void PopulateFiles(){
			files = new List<File>();
			selectedFiles = new List<bool>();
			_file_status = "new";
			bashProcess = initProcess("ls-files -o --exclude-standard", FileDataReceived, null);
			bashProcess.WaitForExit();
			_file_status = "mod";
			bashProcess = initProcess("ls-files -m --exclude-standard", FileDataReceived, null);
			bashProcess.WaitForExit();
			_file_status = "del";
			bashProcess = initProcess("ls-files -d --exclude-standard", FileDataReceived, null);
			bashProcess.WaitForExit();
			_file_status = "stgd";
			bashProcess = initProcess("diff --cached --name-only ", FileDataReceived, null);
		}
		
		void FileDataReceived(object sender, DataReceivedEventArgs e){
			if(e.Data != null){
				selectedFiles.Add(false);
				files.Add(new File(e.Data, _file_status));
			}
		}
		
		void PopulateHistory(){
			history = new List<HistoryItem>();
			bashProcess = initProcess ("log --format=format:%H&%an&%ar&%s", HistoryDataReceived, null);
		}
		
		void HistoryDataReceived(object sender, DataReceivedEventArgs e){
//			UnityEngine.Debug.Log(e.Data);
			char[] delimiters = new char[]{'&'};
			if(e.Data != null){
				string[] parts = e.Data.Split(delimiters);
				HistoryItem item = new HistoryItem(parts[0], parts[1], parts[2], parts[3]);
				history.Add(item);
			}
		}
		
		//Index operations
		void Stage(List<File> files){
			string add = "";
			string mod = "";
			string del = "";
			foreach(File file in files){
				if(file.Status == "new"){
					add += " \"" + file.Path + "\"";
					if(add.Length > 20000){
						bashProcess = initProcess("update-index --add -q "+add);
						bashProcess.WaitForExit();
						add = "";
					}
				}
				else if(file.Status == "mod"){
					mod += " \"" + file.Path + "\"";
					if(mod.Length > 20000){
						bashProcess = initProcess("update-index --refresh -q "+mod);
						bashProcess.WaitForExit();
						mod = "";
					}
				}
				else if(file.Status == "del"){
					del += " \"" + file.Path + "\"";
					if(del.Length > 20000){
						bashProcess = initProcess("rm -q"+del);
						bashProcess.WaitForExit();
						del = "";
					}
				}
			}
			bashProcess = initProcess("update-index --add -q "+add);
			bashProcess = initProcess("update-index --refresh -q " + mod);
			bashProcess = initProcess("rm -q"+del);
			bashProcess.WaitForExit();
		}
		
		void Unstage(List<File> files){
			string paths = "";
			foreach(File file in files){
				paths += " \"" + file.Path + "\"";
				if(paths.Length > 20000){
					bashProcess = initProcess("reset HEAD "+paths);
					bashProcess.WaitForExit();
					paths = "";
				}
			}
			if(paths.Length>0){
				bashProcess = initProcess("reset HEAD "+paths);
				bashProcess.WaitForExit();
			}
		}
		
		//Commit plumbing
		void Commit() {
			bashProcess = initProcess("write-tree", TreeDataReceived, null);
			bashProcess.WaitForExit();
			string args = newTreeHash;
			if(branches != null && branches.Count > 0){
				args += " -p "+branches[selectedBranchIndex].CommitHash;
			}
			args += " -m \""+commitMessage.Replace("\"", " ")+"\"";
//			UnityEngine.Debug.Log("commit-tree " + args);
			bashProcess = initProcess("commit-tree " + args, CommitDataReceived, null);
			bashProcess.WaitForExit();
			bashProcess = initProcess("symbolic-ref HEAD", HeadRefDataReceived, null);
			bashProcess.WaitForExit();
//			UnityEngine.Debug.Log("update-ref "+headRef+" "+newCommitHash);
			bashProcess = initProcess("update-ref "+headRef+" "+newCommitHash);
			bashProcess.WaitForExit();
			dirtyBranches = true;
			dirtyFiles = true;
		}
		
		void TreeDataReceived(object sender, DataReceivedEventArgs e) {
			if(e.Data != null){
				newTreeHash = e.Data;
//				UnityEngine.Debug.Log("New Tree: "+newTreeHash);
			}
		}
		
		void CommitDataReceived(object sender, DataReceivedEventArgs e) {
			if(e.Data != null){
				newCommitHash = e.Data;
//				UnityEngine.Debug.Log("New Commit: "+newCommitHash);
			}
		}
		
		void HeadRefDataReceived(object sender, DataReceivedEventArgs e) {
			if(e.Data != null){
				headRef = e.Data;
//				UnityEngine.Debug.Log("Head ref: "+headRef);
			}
		}
		
		
		//Basic functions
		
		void Init() {
			bashProcess = initProcess("init");
			bashProcess = initProcess("branch master");
			EnsureLineEndings();
		}
		
		void Clone() {
			Init();
			
			System.Uri uri = new System.Uri(url);
			System.UriBuilder builder = new System.UriBuilder(uri);
			builder.UserName = username;
			builder.Password = password;
			
			bashProcess = initProcess("remote add --track master origin "+builder.Uri);
			bashProcess = initProcess("pull -q");
		}

		void StartBash() {
			Process newProcess = new Process();
			#if UNITY_EDITOR_WIN
			newProcess.StartInfo.FileName = path+"\\PortableGit\\git-bash.bat";
			#else
			newProcess.StartInfo.FileName = "open";
			newProcess.StartInfo.Arguments = "Terminal";
			#endif

			try{
				newProcess.Start();
			}
			catch(System.Exception e){
				UnityEngine.Debug.Log (e.Message);
			}
		}
		
		void OnGUI() {
			
			if(Screen.width>layoutSwitch) {
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width*0.3f));
			}
			else{
				EditorGUILayout.BeginVertical();
			}
			if(!isInitialized) {
				
				//Empty repository
				EditorGUILayout.LabelField("Init empty repository");
				if(GUILayout.Button("Init")) {
					Init();
				}
				
				//Clone existing repository
				EditorGUILayout.LabelField("Clone repository");
				
				url = EditorGUILayout.TextField("URL:", url);
				username = EditorGUILayout.TextField("Username:", username);
				password = EditorGUILayout.PasswordField("Password:", password);
				
				if(GUILayout.Button("Clone")) {
					EditorPrefs.SetString("gitUrl", url);
					EditorPrefs.SetString("gitUsername", username);
					EditorPrefs.SetString("gitPassword", password);
					Clone();
				}
			}
			else{
				EditorGUILayout.PrefixLabel("Branches");
				List<string> list = new List<string>();
				if(branches != null){
					foreach(Branch branch in branches){
						list.Add(branch.Name);
					}
				}
				
				EditorGUILayout.BeginHorizontal();
				int oldIndex = selectedBranchIndex;
				selectedBranchIndex = EditorGUILayout.Popup("Branch", selectedBranchIndex, list.ToArray());
				if(selectedBranchIndex != oldIndex){
					Checkout(branches[selectedBranchIndex].Name);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				newBranchName= EditorGUILayout.TextField("New branch:", newBranchName);
				EditorGUILayout.Space();
				GUILayout.Button("Create branch!", GUILayout.Width(100));
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.LabelField("History");
				
				//History
				if(history != null) {
					
					hitoryScrollPosition = EditorGUILayout.BeginScrollView(hitoryScrollPosition, false, true);
					
					
					//Place all items in a string array
					string[] historyItemStrings = new string[history.Count];
					for(int i=0; i<history.Count; ++i){
						historyItemStrings[i] = history[i].Message;
					}
					
					//Selection grid for history items
					selectedHistory = GUILayout.SelectionGrid(selectedHistory, historyItemStrings, 1, skin.GetStyle("HistoryItem"));
					EditorGUILayout.EndScrollView();
					
					EditorGUILayout.BeginHorizontal();
					if(GUILayout.Button("Checkout at commit") && selectedHistory != -1){
						Checkout(history[selectedHistory].Hash);
					}
					if(GUILayout.Button("Revert to branch head")){
						Checkout(branches[selectedBranchIndex].Name);
					}
					EditorGUILayout.EndHorizontal();
				}
				
				EditorGUILayout.Space();
				
			}
			
			
			if(Screen.width>layoutSwitch) {
				EditorGUILayout.EndVertical();	
				EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width*0.33f));
				GUILayout.Space(10);
				
				EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button("all")){
					dirtyGUI = true;
					for(int i=0; i<selectedFiles.Count; ++i){
						if(files[i].Status != "stgd"){
							selectedFiles[i] = true;
						}
					}
				}
				
				if(GUILayout.Button("none")){
					dirtyGUI = true;
					for(int i=0; i<selectedFiles.Count; ++i){
						if(files[i].Status != "stgd"){
							selectedFiles[i] = false;
						}
					}
				}
				
				EditorGUILayout.EndHorizontal();
				unstagedFilesScrollPosition = EditorGUILayout.BeginScrollView(unstagedFilesScrollPosition, false, true);
				EditorGUILayout.BeginVertical();
				if(files != null) {
					for(int i=0; i<files.Count; ++i){
						if(files[i].Status == "stgd")
							continue;
						EditorGUILayout.BeginHorizontal();
						selectedFiles[i] = GUILayout.Toggle(selectedFiles[i],"", GUILayout.Width(15));
						switch(files[i].Status){
						case "new":
							GUILayout.Label(newTexture, GUILayout.ExpandWidth(false));
							break;
						case "mod":
							GUILayout.Label(modTexture, GUILayout.ExpandWidth(false));
							break;
						case "del":
							GUILayout.Label(delTexture, GUILayout.ExpandWidth(false));
							break;
						}
						EditorGUILayout.LabelField(files[i].Path);
						EditorGUILayout.EndHorizontal();
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
				
				
				EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width*0.03f));
				GUILayout.Space(30);
				if(GUILayout.Button("Refresh")){
					dirtyFiles = true;
				}
				if(GUILayout.Button("Pull")){
					bashProcess = initProcess("pull -q");
					bashProcess.WaitForExit();
				}
				if(GUILayout.Button("Push")){
					bashProcess = initProcess("push -q");
					bashProcess.WaitForExit();
				}
				if(GUILayout.Button("Checkout selected")){
					string filesLine = "";
					for(int i=0; i<selectedFiles.Count; ++i){
						if(selectedFiles[i] && files[i].Status!="stgd"){
							filesLine += "\""+files[i].Path+"\""+" ";
							if(filesLine.Length > 20000){
								Checkout(filesLine);
								filesLine = "";
							}
						}
					}
					if(filesLine.Length>0){
						Checkout(filesLine);
					}
					dirtyFiles = true;
				}
				GUILayout.FlexibleSpace();
				if(GUILayout.Button(">>>")){
					List<File> stageFiles = new List<File>();
					for(int i=0; i<this.selectedFiles.Count; ++i){
						if(files[i].Status != "stgd" && selectedFiles[i]){
							stageFiles.Add(files[i]);
						}
					}
					Stage (stageFiles);
					dirtyFiles = true;
				}
				if(GUILayout.Button("<<<")){
					List<File> unstageFiles = new List<File>();
					for(int i=0; i<this.selectedFiles.Count; i++){
						if(files[i].Status == "stgd" && selectedFiles[i]){
							unstageFiles.Add(files[i]);
						}
					}
					Unstage (unstageFiles);
					
					dirtyFiles = true;
				}
				
				GUILayout.FlexibleSpace();

				if(GUILayout.Button("Bash")){
					StartBash();
				}
				EditorGUILayout.EndVertical();
				
				
				EditorGUILayout.BeginVertical();
				GUILayout.Space(10);
				
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Commit message:", GUILayout.Width(120));
				commitMessage = EditorGUILayout.TextField(commitMessage);
				if(GUILayout.Button("Commit")){
					Commit ();
					dirtyGUI = true;
					dirtyHistory = true;
				}
				EditorGUILayout.EndHorizontal();
				
				EditorGUILayout.BeginHorizontal();
				
				if(GUILayout.Button("all")){
					dirtyGUI = true;
					for(int i=0; i<selectedFiles.Count; ++i){
						if(files[i].Status == "stgd"){
							selectedFiles[i] = true;
						}
					}
				}
				
				if(GUILayout.Button("none")){
					dirtyGUI = true;
					for(int i=0; i<selectedFiles.Count; ++i){
						if(files[i].Status == "stgd"){
							selectedFiles[i] = false;
						}
					}
				}
				
				EditorGUILayout.EndHorizontal();
				stagedFilesScrollPosition = EditorGUILayout.BeginScrollView(stagedFilesScrollPosition, false, true);
				EditorGUILayout.BeginVertical();
				if(files != null) {
					for(int i=0; i<files.Count; ++i){
						if(files[i].Status != "stgd")
							continue;
						EditorGUILayout.BeginHorizontal();
						selectedFiles[i] = GUILayout.Toggle(selectedFiles[i],"", GUILayout.Width(15));
						GUILayout.Label(stgdTexture, GUILayout.ExpandWidth(false));
						EditorGUILayout.LabelField(files[i].Path);
						EditorGUILayout.EndHorizontal();
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
			}
			
			if(Screen.width>layoutSwitch) {
				EditorGUILayout.EndHorizontal();
			}
			
			if(running) {
				GUIUtility.hotControl = 0;
				EditorGUI.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color32(20, 20, 20, 200));
				EditorGUI.ProgressBar(new Rect(0, Screen.height-40, Screen.width, 40), 0.3f, "Working");
			}
		}
	}
}